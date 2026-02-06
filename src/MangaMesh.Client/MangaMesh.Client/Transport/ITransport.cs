using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MangaMesh.Client.Transport
{
    public interface ITransport
    {
        Task SendAsync(NodeAddress address, DhtMessage message);
        Task<DhtMessage> ReceiveAsync();
    }

}
