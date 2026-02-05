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

        Task<(string SeriesId, string Title)> RegisterSeriesAsync(MangaMesh.Shared.Models.ExternalMetadataSource source, string externalMangaId);

        Task<IEnumerable<SeriesSummaryResponse>> SearchSeriesAsync(string query, string? sort = null, string[]? ids = null);
        Task<TrackerStats> GetStatsAsync();

        Task<KeyChallengeResponse> CreateChallengeAsync(string publicKeyBase64);
        Task<KeyVerificationResponse> VerifyChallengeAsync(string publicKeyBase64, string challengeId, string signatureBase64);
        Task AuthorizeManifestAsync(AuthorizeManifestRequest request);
    }

    public class TrackerStats
    {
        public int NodeCount { get; set; }
    }
}
