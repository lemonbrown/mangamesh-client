using MangaMesh.Client.Abstractions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MangaMesh.Client.Services
{
    public class NodeIdentityService : INodeIdentityService
    {
        public string NodeId { get; }
        public bool IsConnected { get; private set; }
        public DateTime? LastPingUtc { get; private set; }

        public NodeIdentityService(ILogger<NodeIdentityService> logger)
        {
            NodeId = Guid.NewGuid().ToString("N");
            logger.LogInformation("Generated in-memory NodeId: {NodeId}", NodeId);
        }

        public void UpdateStatus(bool isConnected)
        {
            IsConnected = isConnected;
            if (isConnected)
            {
                LastPingUtc = DateTime.UtcNow;
            }
        }
    }
}
