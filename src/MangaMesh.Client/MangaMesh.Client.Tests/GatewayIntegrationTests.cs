using MangaMesh.Client.Content;
using MangaMesh.Client.Node;
using MangaMesh.Client.Transport;
using MangaMesh.GatewayApi.Config;
using MangaMesh.GatewayApi.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net;
using System.Text.Json;
using System.Text;
using MangaMesh.Client.Keys;
using Moq;

namespace MangaMesh.Client.Tests
{
    [TestClass]
    public class GatewayIntegrationTests
    {
        private WebApplicationFactory<Program> _factory;
        private HttpClient _client;
        private IDhtNode _gatewayNode;
        private DhtNode _peerNode; // Node B
        private TcpTransport _peerTransport;
        private int _gatewayPort = 15001; // Avoid conflict with default 5001/8080
        private int _peerPort = 15002;

        [TestInitialize]
        public async Task Setup()
        {
            // Setup Gateway (Node A) via WebApplicationFactory
            _factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureServices(services =>
                    {
                        // Override Gateway Config to set a specific port
                        var config = services.FirstOrDefault(d => d.ServiceType == typeof(GatewayConfig));
                        if (config != null) services.Remove(config);
                        
                        services.AddSingleton(new GatewayConfig 
                        { 
                            Enabled = true, 
                            Port = _gatewayPort,
                            CacheTtlMinutes = 5
                        });
                    });
                });

            _client = _factory.CreateClient(); // This starts the app

            var mockKeyPairService = new Mock<IKeyPairService>();
            
            // Get access to the Gateway's internal DhtNode for verification/bootstrapping
            _gatewayNode = _factory.Services.GetRequiredService<IDhtNode>();

            // Setup Peer Node (Node B)
            // We need a real DHT node running to respond to the Gateway
            _peerTransport = new TcpTransport(_peerPort);
            var identity = new NodeIdentity(mockKeyPairService.Object);
            //await identity.InitializeAsync();
            
            var storage = new InMemoryDhtStorage(); // Using Client internal storage if accessible? No, likely public or we use same trick
                                                    // InMemoryDhtStorage is in MangaMesh.Client.Node namespace
            
            var mockKeyStore = new Mock<IKeyStore>();

            _peerNode = new DhtNode(identity, _peerTransport, storage, mockKeyPairService.Object, mockKeyStore.Object);

            // Wire up protocol handlers for Peer Node
            var router = new ProtocolRouter();
            var dhtHandler = new DhtProtocolHandler(_peerNode);
            router.Register(dhtHandler);
            
            // Content Handler for Peer
            var contentHandler = new ContentProtocolHandler(_peerTransport, (hash) => 
            {
                // Simple content provider for test
                if (hash == "test-hash") return Encoding.UTF8.GetBytes("{\"title\":\"Test Series\"}");
                return null;
            });
            contentHandler.DhtNode = _peerNode;
            router.Register(contentHandler);
            
            _peerTransport.OnMessage += router.RouteAsync;
            
            _peerNode.StartWithMaintenance(enableBootstrap: false);
            
            // Bootstrap Peer to Gateway
            // Gateway is at 127.0.0.1:_gatewayPort
            // We need to ensure Gateway is running and listening on TCP
            // The GatewayService starts DhtNode.StartWithMaintenance, which opens TcpTransport
        }

        [TestCleanup]
        public void Cleanup()
        {
            _peerNode?.StopWithMaintenance();
            _factory?.Dispose();
        }

        [TestMethod]
        public async Task TestGatewayFetchesManifestFromPeer()
        {
            // 1. Peer announces content
            var manifestContent = Encoding.UTF8.GetBytes("{\"title\":\"Test Series\"}");
            var manifestHash = "test-hash";
            
            // Store locally on peer so it can answer GetManifest
            // (ContentProtocolHandler above handles the response)
            
            // We also need Peer to be findable by Gateway.
            // Bootstrap Peer -> Gateway
            var gatewayAddress = new NodeAddress("127.0.0.1", _gatewayPort);

            await _peerNode.PingAsync(new RoutingEntry { Address = gatewayAddress, NodeId = _gatewayNode.Identity.NodeId }); // Ping to introduce
                                  
            // Let's force Peer into Gateway's routing table via Ping
            // Gateway needs to be running.
            await Task.Delay(1000); // Wait for Gateway start                       
            
            // Problem: We might not know Gateway's NodeID easily if it's generated on fly.
            // _gatewayNode.Identity.NodeId should be available.
            
            var entry = new RoutingEntry 
            { 
                 Address = gatewayAddress, 
                 NodeId = _gatewayNode.Identity.NodeId 
            };
            
            await _peerNode.PingAsync(entry);
            
            // Give time for Ping to complete and RT update
            await Task.Delay(500);
            
            // Now Peer should be in Gateway's RoutingTable.
            
            // So: Peer must Store "test-hash" on Gateway.
            var hashBytes = Encoding.UTF8.GetBytes(manifestHash);
            await _peerNode.StoreAsync(hashBytes);
            
            await Task.Delay(500); // Wait for Store to propagate

            // 2. Gateway API Request
            // GET /api/manifests/test-hash
            var response = await _client.GetAsync($"/api/manifests/{manifestHash}");
            
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            
            var json = await response.Content.ReadAsStringAsync();
            var manifest = JsonSerializer.Deserialize<ManifestData>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            Assert.IsNotNull(manifest);
            Assert.AreEqual(manifestHash, manifest.ContentHash);
            
            var contentStr = Encoding.UTF8.GetString(manifest.Data);
            Assert.IsTrue(contentStr.Contains("Test Series"));
        }
    }
}
