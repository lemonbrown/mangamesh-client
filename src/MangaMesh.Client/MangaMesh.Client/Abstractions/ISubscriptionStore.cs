using MangaMesh.Client.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MangaMesh.Client.Abstractions
{
    public interface ISubscriptionStore
    {
        Task<IReadOnlyList<ReleaseSubscription>> GetAllAsync();
        Task AddAsync(ReleaseSubscription subscription);
        Task RemoveAsync(ReleaseLineId releaseLine);
        Task<bool> ExistsAsync(ReleaseLineId releaseLine);
    }


}
