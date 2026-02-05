using MangaMesh.Client.Models;
using System.Threading.Tasks;

namespace MangaMesh.Client.Abstractions
{
    public interface IStorageMonitorService
    {
        Task<StorageStats> GetStorageStatsAsync();
        Task EnsureStorageAvailable(long bytesRequired);
        void NotifyBlobWritten(long bytes);
    }
}
