using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MangaMesh.Client.Transport
{
    using System.Net.Sockets;
    using System.Net;
    using System.Text.Json;

    public class TcpTransport : ITransport
    {
        private readonly int _listenPort;
        private readonly TcpListener _listener;

        private readonly Queue<DhtMessage> _incomingMessages = new();

        public TcpTransport(int listenPort)
        {
            _listenPort = listenPort;
            _listener = new TcpListener(IPAddress.Parse("0.0.0.0"), _listenPort);
            _listener.Start();

            Task.Run(AcceptLoopAsync);
        }

        private async Task AcceptLoopAsync()
        {
            while (true)
            {
                var client = await _listener.AcceptTcpClientAsync();
                _ = Task.Run(() => HandleClientAsync(client));
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            try
            {
                using var stream = client.GetStream();
                using var reader = new StreamReader(stream);
                using var writer = new StreamWriter(stream) { AutoFlush = true };

                var json = await reader.ReadLineAsync();
                if (json != null)
                {
                    var message = JsonSerializer.Deserialize<DhtMessage>(json);
                    if (message != null)
                    {
                        Console.WriteLine($"[TCP] Received {message.Type} from {Convert.ToHexString(message.SenderNodeId)[..8]}...");
                        lock (_incomingMessages)
                            _incomingMessages.Enqueue(message);
                    }
                }
            }
            catch { /* ignore broken connections */ }
            finally { client.Close(); }
        }

        public async Task SendAsync(NodeAddress address, DhtMessage message)
        {
            try
            {
                Console.WriteLine($"[TCP] Sending {message.Type} to {address.Host}:{address.Port}...");
                using var client = new TcpClient();
                await client.ConnectAsync(address.Host, address.Port);
                using var stream = client.GetStream();
                using var writer = new StreamWriter(stream) { AutoFlush = true };

                var json = JsonSerializer.Serialize(message);
                await writer.WriteLineAsync(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TCP] Failed to send to {address.Host}:{address.Port}: {ex.Message}");
            }
        }

        public async Task<DhtMessage> ReceiveAsync()
        {
            while (true)
            {
                lock (_incomingMessages)
                {
                    if (_incomingMessages.Count > 0)
                        return _incomingMessages.Dequeue();
                }
                await Task.Delay(100);
            }
        }
    }

}
