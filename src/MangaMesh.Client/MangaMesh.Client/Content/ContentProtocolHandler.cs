using MangaMesh.Client.Transport;
using System;
using System.Threading.Tasks;

using MangaMesh.Client.Node;

namespace MangaMesh.Client.Content
{
    public class ContentProtocolHandler : IProtocolHandler
    {
        private readonly ITransport _transport;
        private readonly Func<string, byte[]?> _contentProvider;
        
        public IDhtNode? DhtNode { get; set; }

        public ContentProtocolHandler(ITransport transport, Func<string, byte[]?> contentProvider)
        {
            _transport = transport;
            _contentProvider = contentProvider;
        }

        public ProtocolKind Kind => ProtocolKind.Content;

        public Task HandleAsync(NodeAddress from, ReadOnlyMemory<byte> payload)
        {
            var msg = ContentMessage.Deserialize(payload);

            return msg switch
            {
                GetManifest m => HandleManifestAsync(from, m),
                GetBlob b => HandleBlobAsync(from, b),
                ManifestData d => HandleManifestDataAsync(from, d),
                _ => Task.CompletedTask
            };
        }

        private async Task HandleManifestAsync(NodeAddress from, GetManifest m)
        {
            var content = _contentProvider(m.ContentHash);
            if (content != null)
            {
                var response = new ManifestData
                {
                    ContentHash = m.ContentHash,
                    Data = content
                };
                
                var json = System.Text.Json.JsonSerializer.Serialize<ContentMessage>(response);
                var jsonBytes = System.Text.Encoding.UTF8.GetBytes(json);
                var payload = new byte[1 + jsonBytes.Length];
                payload[0] = (byte)ProtocolKind.Content;
                Array.Copy(jsonBytes, 0, payload, 1, jsonBytes.Length);

                // Use SenderPort from message if available (non-zero), otherwise fallback to source port
                var replyAddress = m.SenderPort > 0 
                    ? new NodeAddress(from.Host, m.SenderPort) 
                    : from;

                await _transport.SendAsync(replyAddress, new ReadOnlyMemory<byte>(payload));
            }
        }

        private Task HandleBlobAsync(NodeAddress from, GetBlob b)
        {
             Console.WriteLine($"[Content] Received GetBlob from {from.Host}:{from.Port} for hash {b.BlobHash}");
            return Task.CompletedTask;
        }

        private Task HandleManifestDataAsync(NodeAddress from, ManifestData d)
        {
            Console.WriteLine($"[Content] Received ManifestData from {from.Host}:{from.Port} for hash {d.ContentHash}. Size: {d.Data.Length} bytes");
            DhtNode?.HandleContentMessage(d);
            return Task.CompletedTask;
        }
    }
}
