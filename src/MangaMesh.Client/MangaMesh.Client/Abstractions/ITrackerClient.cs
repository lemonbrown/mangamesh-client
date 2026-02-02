using MangaMesh.Client.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MangaMesh.Client.Abstractions
{
    public interface ITrackerClient
    {
        Task<bool> PingAsync(string nodeId, string ip, int port, string manifestSetHash, int manifestCount);
        Task<List<PeerInfo>> GetPeersForManifestAsync(string manifestHash);
        Task<PeerInfo?> GetPeerAsync(string seriesId, string chapterId, string manifestHash);

        Task AnnounceAsync(
            string nodeId,
            string ip,
            int port,
            List<string> manifestHashes);

        Task<bool> CheckNodeExistsAsync(string nodeId);

        public Task AnnounceManifestAsync(
         AnnounceManifestRequest announcement,
         CancellationToken ct = default);
    }
}
