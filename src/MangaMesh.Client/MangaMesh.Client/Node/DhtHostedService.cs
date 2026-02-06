using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MangaMesh.Client.Node
{
    using Microsoft.Extensions.Hosting;
    using System.Threading;
    using System.Threading.Tasks;

    public class DhtHostedService : IHostedService
    {
        private readonly IDhtNode _dhtNode;

        public DhtHostedService(IDhtNode dhtNode)
        {
            _dhtNode = dhtNode;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Start message loop + maintenance
            _dhtNode.StartWithMaintenance();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _dhtNode.StopWithMaintenance();
            return Task.CompletedTask;
        }
    }

}
