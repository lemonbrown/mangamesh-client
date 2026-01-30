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
        Task<List<PeerInfo>> GetPeersForManifestAsync(string manifestHash);

        Task<bool> AnnounceAsync(string nodeId, string ip, int port, List<string> manifestHashes);

        public Task AnnounceManifestAsync(
         ManifestAnnouncement announcement,
         CancellationToken ct = default);
    }
}
