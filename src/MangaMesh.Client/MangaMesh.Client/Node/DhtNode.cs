using MangaMesh.Client.Helpers;
using MangaMesh.Client.Keys;
using MangaMesh.Client.Transport;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NSec.Cryptography;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MangaMesh.Client.Node
{

    public class DhtNode : IDhtNode
    {
        private readonly TimeSpan _refreshInterval = TimeSpan.FromMinutes(15);
        private readonly TimeSpan _reannounceInterval = TimeSpan.FromMinutes(30);
        private readonly TimeSpan _pingInterval = TimeSpan.FromMinutes(5);

        private CancellationTokenSource? _maintenanceToken;

        private IKeyPairService _keypairService;

        private IKeyStore _keyStore;

        public INodeIdentity Identity { get; private set; }
        public ITransport Transport { get; private set; }
        public IDhtStorage Storage { get; private set; }
        public List<KBucket> RoutingTable { get; private set; } = new();
        private bool _running = false;


        public DhtNode(INodeIdentity identity, ITransport transport, IDhtStorage storage, IKeyPairService keyPairService, IKeyStore keyStore)
        {
            _keypairService = keyPairService;
            Identity = identity;
            Transport = transport;
            Storage = storage;
            _keyStore = keyStore;
            // Initialize 256 k-buckets for 256-bit node IDs
            for (int i = 0; i < 256; i++)
                RoutingTable.Add(new KBucket());
        }


        /// <summary>
        /// Start the DHT node and maintenance loops.
        /// </summary>
        public void StartWithMaintenance(bool enableBootstrap = true)
        {
            Start(enableBootstrap);
            _maintenanceToken = new CancellationTokenSource();
            Task.Run(() => MaintenanceLoopAsync(_maintenanceToken.Token));
        }

        /// <summary>
        /// Stop the DHT node and maintenance loops.
        /// </summary>
        public void StopWithMaintenance()
        {
            Stop();
            _maintenanceToken?.Cancel();
        }

        /// <summary>
        /// Periodic maintenance loop:
        /// - Re-announce stored content
        /// - Ping stale nodes
        /// - Refresh k-buckets
        /// </summary>
        private async Task MaintenanceLoopAsync(CancellationToken token)
        {
            var lastReannounce = DateTime.UtcNow;
            var lastPing = DateTime.UtcNow;

            while (!token.IsCancellationRequested)
            {
                var now = DateTime.UtcNow;

                // ---------------------------
                // Re-announce content
                // ---------------------------
                if (now - lastReannounce > _reannounceInterval)
                {
                    foreach (var content in Storage.GetAllContentHashes())
                    {
                        await StoreAsync(content);
                    }
                    lastReannounce = now;
                }

                // ---------------------------
                // Ping stale nodes
                // ---------------------------
                if (now - lastPing > _pingInterval)
                {
                    foreach (var bucket in RoutingTable)
                    {
                        var staleNodes = bucket.Entries
                            .Where(e => (now - e.LastSeenUtc) > _pingInterval)
                            .ToList();

                        foreach (var node in staleNodes)
                        {
                            await PingAsync(node);
                            // Optionally remove if no response after retries
                        }
                    }
                    lastPing = now;
                }

                // ---------------------------
                // Bucket refresh (optional)
                // ---------------------------
                //foreach (var bucket in RoutingTable)
                //{
                //    // if bucket empty, optionally query random IDs in range
                //    if (bucket.Entries.Count == 0)
                //    {
                //        var randomId = Crypto.RandomNodeId();
                //        _ = FindNodeAsync(randomId);
                //    }
                //}

                await Task.Delay(TimeSpan.FromSeconds(10), token);
            }
        }

        /// <summary>
        /// Start listening to incoming messages from the transport.
        /// </summary>
        public void Start(bool enableBootstrap = true)
        {
            if (_running) return;

            //bootstrap nodes
            var bootstrapNodes = new List<RoutingEntry>();

            try
            {
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "bootstrap_nodes.yml");
                if (File.Exists(configPath))
                {
                    var yaml = File.ReadAllText(configPath);
                    var deserializer = new YamlDotNet.Serialization.DeserializerBuilder()
                        .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.CamelCaseNamingConvention.Instance)
                        .Build();

                    var nodes = deserializer.Deserialize<List<BootstrapNodeConfig>>(yaml);

                    if (nodes != null)
                    {
                        foreach (var node in nodes)
                        {
                            bootstrapNodes.Add(new RoutingEntry
                            {
                                NodeId = Convert.FromHexString(node.NodeId),
                                Address = new NodeAddress(node.Address.Host, node.Address.Port),
                                LastSeenUtc = DateTime.UtcNow
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load bootstrap nodes: {ex.Message}");
            }

            //ensure keys are available
            var keys = _keyStore.GetAsync().Result;

            if (keys == null)
                _keypairService.GenerateKeyPairBase64Async().Wait();

            _running = true;

            Task.Run(MessageLoopAsync);

            if (enableBootstrap)
            {
                Task.Run(async () => await BootstrapAsync(bootstrapNodes));
            }
        }

        public async Task StoreAsync(byte[] contentHash)
        {
            // Find k closest nodes (simplified)
            var closestNodes = FindClosestNodes(contentHash, 20);
            foreach (var node in closestNodes)
            {
                var message = new DhtMessage
                {
                    Type = DhtMessageType.Store,
                    SenderNodeId = Identity.NodeId,
                    Payload = contentHash,
                    TimestampUtc = DateTime.UtcNow,
                    Signature = Identity.Sign(contentHash),
                    SenderPort = Transport.Port
                };
                await Transport.SendAsync(node.Address, message);
            }
            // Also store locally
            Storage.StoreContent(contentHash, Identity.NodeId);
        }

        public async Task<List<byte[]>> FindValueAsync(byte[] contentHash)
        {
            // simplified iterative lookup
            var resultNodes = new List<byte[]>();
            var closestNodes = FindClosestNodes(contentHash, 20);

            foreach (var node in closestNodes)
            {
                var message = new DhtMessage
                {
                    Type = DhtMessageType.FindValue,
                    SenderNodeId = Identity.NodeId,
                    Payload = contentHash,
                    TimestampUtc = DateTime.UtcNow,
                    Signature = Identity.Sign(contentHash),
                    SenderPort = Transport.Port
                };
                await Transport.SendAsync(node.Address, message);
                // response handling omitted for skeleton
            }

            // Include local store if present
            resultNodes.AddRange(Storage.GetNodesForContent(contentHash));
            return resultNodes;
        }

        public async Task<List<RoutingEntry>> FindNodeAsync(byte[] nodeId, RoutingEntry? bootstrap = null)
        {
            // simplified iterative lookup
            var closestNodes = new List<RoutingEntry>();

            if (bootstrap != null)
            {
                // Start from bootstrap node
                closestNodes.Add(bootstrap);
            }
            else
            {
                // Use closest nodes from current routing table
                closestNodes.AddRange(FindClosestNodes(nodeId, 20));
            }

            foreach (var node in closestNodes)
            {
                Console.WriteLine($"Looking for node [{node.NodeId}] - [{node.Address.Host}:{node.Address.Port}]");
             
                var message = new DhtMessage
                {
                    Type = DhtMessageType.FindNode,
                    SenderNodeId = Identity.NodeId,
                    Payload = nodeId,
                    TimestampUtc = DateTime.UtcNow,
                    Signature = Identity.Sign(nodeId),
                    SenderPort = Transport.Port
                };

                await Transport.SendAsync(node.Address, message);
                
                Console.WriteLine($"Found node [{node.NodeId}] - [{node.Address.Host}:{node.Address.Port}]");

                // response handling omitted
            }
            return closestNodes;
        }

        public async Task PingAsync(RoutingEntry node)
        {
            var message = new DhtMessage
            {
                Type = DhtMessageType.Ping,
                SenderNodeId = Identity.NodeId,
                Payload = Array.Empty<byte>(),
                TimestampUtc = DateTime.UtcNow,
                Signature = Identity.Sign(Array.Empty<byte>()),
                SenderPort = Transport.Port
            };
            await Transport.SendAsync(node.Address, message);
        }


        /// <summary>
        /// Stop the DHT node
        /// </summary>
        public void Stop()
        {
            _running = false;
        }

        public async Task BootstrapAsync(IEnumerable<RoutingEntry> bootstrapNodes)
        {
            foreach (var bootstrap in bootstrapNodes)
            {
                // Use a random node ID for initial queries
                var randomId = Crypto.RandomNodeId();

                try
                {
                    var foundNodes = await FindNodeAsync(randomId, bootstrap);
                    foreach (var node in foundNodes)
                    {
                        if (node.NodeId != null)
                            UpdateRoutingTable(node.NodeId, node.Address);
                    }
                    
                    if (foundNodes.Count > 0)
                    {
                         break;
                    }
                }
                catch
                {
                    // ignore unreachable bootstrap nodes
                }
            }
        }


        private async Task MessageLoopAsync()
        {
            while (_running)
            {
                var message = await Transport.ReceiveAsync();
                if (message == null) continue;

                // Update routing table with sender
                // Update routing table with sender
                // Use ComputedSenderIp (from TCP connection) and SenderPort (from message payload)
                if (!string.IsNullOrEmpty(message.ComputedSenderIp) && message.SenderPort > 0)
                {
                     UpdateRoutingTable(message.SenderNodeId, new NodeAddress(message.ComputedSenderIp, message.SenderPort));
                }
                else
                {
                     // Fallback or log warning? For now, keep dummy if missing info, or just skip?
                     // Keeping dummy might pollute routing table. Better to skip or log.
                     // But for existing tests/logic, maybe fallback to dummy if local?
                     // Let's assume we only update if we have info.
                     Console.WriteLine($"Warning: Received message without valid address info. IP: {message.ComputedSenderIp}, Port: {message.SenderPort}");
                }

                Console.WriteLine($"Received message [{message.Type}]");

                // Handle message asynchronously
                _ = Task.Run(() => HandleMessageAsync(message));
            }
        }

        private async Task HandleMessageAsync(DhtMessage message)
        {
            switch (message.Type)
            {
                case DhtMessageType.Ping:
                    await HandlePingAsync(message);
                    break;
                case DhtMessageType.Pong:
                    // optional: mark node as alive
                    break;
                case DhtMessageType.FindNode:
                    await HandleFindNodeAsync(message);
                    break;
                case DhtMessageType.Nodes:
                    // optional: merge routing info
                    break;
                case DhtMessageType.Store:
                    await HandleStoreAsync(message);
                    break;
                case DhtMessageType.FindValue:
                    await HandleFindValueAsync(message);
                    break;
                case DhtMessageType.Value:
                    // optional: merge results
                    break;
                default:
                    break;
            }
        }

        // ======================================================
        // Handlers
        // ======================================================

        private async Task HandlePingAsync(DhtMessage message)
        {
            var pong = new DhtMessage
            {
                Type = DhtMessageType.Pong,
                SenderNodeId = Identity.NodeId,
                Payload = Array.Empty<byte>(),
                TimestampUtc = DateTime.UtcNow,
                Signature = Identity.Sign(Array.Empty<byte>()),
                SenderPort = Transport.Port
            };

            var senderAddress = GetAddressForNode(message.SenderNodeId);
            if (senderAddress != null)
                await Transport.SendAsync(senderAddress, pong);
        }

        private async Task HandleStoreAsync(DhtMessage message)
        {
            // Payload = content hash
            Storage.StoreContent(message.Payload, message.SenderNodeId);
            await Task.CompletedTask; // placeholder
        }

        private async Task HandleFindNodeAsync(DhtMessage message)
        {
            var targetId = message.Payload;
            var closestNodes = FindClosestNodes(targetId, 20);

            var nodesPayload = SerializeNodes(closestNodes);

            var nodesMessage = new DhtMessage
            {
                Type = DhtMessageType.Nodes,
                SenderNodeId = Identity.NodeId,
                Payload = nodesPayload,
                TimestampUtc = DateTime.UtcNow,
                Signature = Identity.Sign(nodesPayload),
                SenderPort = Transport.Port
            };

            var senderAddress = GetAddressForNode(message.SenderNodeId);
            if (senderAddress != null)
                await Transport.SendAsync(senderAddress, nodesMessage);
        }

        private async Task HandleFindValueAsync(DhtMessage message)
        {
            var contentHash = message.Payload;
            var nodesWithContent = Storage.GetNodesForContent(contentHash);

            DhtMessage reply;
            if (nodesWithContent.Count > 0)
            {
                reply = new DhtMessage
                {
                    Type = DhtMessageType.Value,
                    SenderNodeId = Identity.NodeId,
                    Payload = SerializeNodeIds(nodesWithContent),
                    TimestampUtc = DateTime.UtcNow,
                    Signature = Identity.Sign(SerializeNodeIds(nodesWithContent)),
                    SenderPort = Transport.Port
                };
            }
            else
            {
                // Return closest nodes instead
                var closestNodes = FindClosestNodes(contentHash, 20);
                reply = new DhtMessage
                {
                    Type = DhtMessageType.Nodes,
                    SenderNodeId = Identity.NodeId,
                    Payload = SerializeNodes(closestNodes),
                    TimestampUtc = DateTime.UtcNow,
                    Signature = Identity.Sign(SerializeNodes(closestNodes)),
                    SenderPort = Transport.Port
                };
            }

            var senderAddress = GetAddressForNode(message.SenderNodeId);
            if (senderAddress != null)
                await Transport.SendAsync(senderAddress, reply);
        }

        // ======================================================
        // Helpers
        // ======================================================

        private void UpdateRoutingTable(byte[] nodeId, NodeAddress address)
        {
            if (nodeId == null || nodeId.Length == 0) return;

            Console.WriteLine($"Adding node [{address.Host}:{address.Port}] to routing table");
            int bucketIndex = GetBucketIndex(nodeId);
            var entry = new RoutingEntry
            {
                NodeId = nodeId,
                Address = address,
                LastSeenUtc = DateTime.UtcNow
            };
            RoutingTable[bucketIndex].AddOrUpdate(entry);
        }

        private int GetBucketIndex(byte[] nodeId)
        {
            // XOR distance first differing bit
            var distance = Crypto.XorDistance(Identity.NodeId, nodeId);
            return (int)Math.Min(255, distance.BitLength() - 1);
        }

        private NodeAddress? GetAddressForNode(byte[] nodeId)
        {
            foreach (var bucket in RoutingTable)
            {
                var entry = bucket.Entries.Find(e => e.NodeId.AsSpan().SequenceEqual(nodeId));
                if (entry != null) return entry.Address;
            }
            return null;
        }

        private byte[] SerializeNodes(List<RoutingEntry> nodes)
        {
            // naive JSON serialization
            var addresses = nodes.ConvertAll(n => new { Host = n.Address.Host, Port = n.Address.Port });
            return System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(addresses);
        }

        private byte[] SerializeNodeIds(List<byte[]> nodeIds)
        {
            return System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(nodeIds);
        }

        private List<RoutingEntry> FindClosestNodes(byte[] targetNodeId, int count)
        {
            // naive linear scan; XOR distance
            var allEntries = new List<RoutingEntry>();
            foreach (var bucket in RoutingTable)
                allEntries.AddRange(bucket.Entries);

            allEntries.Sort((a, b) =>
            {
                var distA = Crypto.XorDistance(a.NodeId, targetNodeId);
                var distB = Crypto.XorDistance(b.NodeId, targetNodeId);
                return distA.CompareTo(distB);
            });

            return allEntries.GetRange(0, Math.Min(count, allEntries.Count));
        }
    }

    public class BootstrapNodeConfig
    {
        public string NodeId { get; set; } = string.Empty;
        public BootstrapAddressConfig Address { get; set; } = new();
    }

    public class BootstrapAddressConfig
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
    }
}
