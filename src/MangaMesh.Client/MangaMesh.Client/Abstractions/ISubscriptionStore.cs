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
        Task<List<SeriesSubscription>> GetAllAsync();
        Task AddAsync(SeriesSubscription sub);
    }

}
