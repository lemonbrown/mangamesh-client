using MangaMesh.Client.Transport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MangaMesh.Client.Node
{
    public interface IDhtNode
    {
        INodeIdentity Identity { get; }
        ITransport Transport { get; }

        Task StoreAsync(byte[] contentHash);
        Task<List<byte[]>> FindValueAsync(byte[] contentHash);
        Task<List<RoutingEntry>> FindNodeAsync(byte[] nodeId, RoutingEntry? bootstrapNode = null);
        Task PingAsync(RoutingEntry node);
        void StartWithMaintenance();
        void StopWithMaintenance();
    }
}
