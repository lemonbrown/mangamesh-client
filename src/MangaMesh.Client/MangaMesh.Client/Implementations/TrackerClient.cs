using MangaMesh.Client.Abstractions;
using MangaMesh.Client.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace MangaMesh.Client.Implementations
{
    public sealed class TrackerClient : ITrackerClient
    {
        private readonly HttpClient _httpClient;

        public TrackerClient(HttpClient http)
        {
            _httpClient = http;
        }

        public async Task<bool> PingAsync(string nodeId, string ip, int port, string manifestSetHash, int manifestCount)
        {
            try
            {
                var request = new PingRequest(nodeId, ip, port, manifestSetHash, manifestCount);
                // POST /ping matches the implementation plan and controller expectation
                var response = await _httpClient.PostAsJsonAsync("/ping", request);

                // 200 OK = Synced
                // 409 Conflict = Sync Needed
                return response.StatusCode == System.Net.HttpStatusCode.OK;
            }
            catch
            {
                // Fallback to sync needed on error
                return false;
            }
        }

        /// <summary>
        /// Query tracker for peers hosting a manifest
        /// </summary>
        public async Task<List<PeerInfo>> GetPeersForManifestAsync(string manifestHash)
        {
            var response = await _httpClient.GetAsync($"/manifest/{manifestHash}/peers");
            response.EnsureSuccessStatusCode();

            var peers = await response.Content.ReadFromJsonAsync<List<PeerInfo>>();
            return peers ?? new List<PeerInfo>();
        }

        public async Task<PeerInfo?> GetPeerAsync(string seriesId, string chapterId, string manifestHash)
        {
            try
            {
                var query = $"?seriesId={Uri.EscapeDataString(seriesId)}&chapterId={Uri.EscapeDataString(chapterId)}&manifestHash={Uri.EscapeDataString(manifestHash)}";
                var response = await _httpClient.GetAsync($"/peer{query}");

                if (!response.IsSuccessStatusCode)
                    return null;

                return await response.Content.ReadFromJsonAsync<PeerInfo>();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Announce this node and the manifests it hosts
        /// </summary>
        public async Task AnnounceAsync(string nodeId, string ip, int port, List<string> manifestHashes)
        {
            var request = new AnnounceRequest(nodeId, ip, port, manifestHashes);
            var response = await _httpClient.PostAsJsonAsync("/announce", request);
            response.EnsureSuccessStatusCode();
        }

        public async Task AnnounceManifestAsync(
             AnnounceManifestRequest announcement,
             CancellationToken ct = default)
        {
            var nodeId = string.IsNullOrEmpty(announcement.NodeId)
                ? Guid.NewGuid().ToString("N")
                : announcement.NodeId;

            dynamic content = new
            {
                NodeId = nodeId,
                ManifestHash = announcement.ManifestHash.Value,
                SeriesId = announcement.SeriesId,
                ChapterId = announcement.ChapterId,
                Chapter = announcement.Chapter,
                Volume = announcement.Volume,
                Source = announcement.Source,
                ExternalMangaId = announcement.ExternalMangaId,
                Title = announcement.Title,
                Language = announcement.Language,
                ScanGroup = announcement.ScanGroup,
                TotalSize = announcement.TotalSize,
                CreatedUtc = announcement.CreatedUtc,
                AnnouncedAt = announcement.AnnouncedAt,
                Signature = announcement.Signature,
                PublicKey = announcement.PublicKey
            };

            var httpContent = JsonContent.Create(content);

            var response = await _httpClient.PostAsync("/api/announce/manifest", httpContent);

            if (response.IsSuccessStatusCode)
                return;

            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                throw new InvalidOperationException("Manifest already exists on the tracker.");
            }

            var error = await response.Content.ReadAsStringAsync(ct);

            throw new InvalidOperationException(
                $"Tracker announce failed ({response.StatusCode}): {error}");
        }
        public async Task<bool> CheckNodeExistsAsync(string nodeId)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Head, $"/nodes/{nodeId}");
                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> RegisterSeriesAsync(MangaMesh.Shared.Models.ExternalMetadataSource source, string externalMangaId)
        {
            var request = new
            {
                Source = source,
                ExternalMangaId = externalMangaId
            };

            var response = await _httpClient.PostAsJsonAsync("/api/series/register", request);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<RegisterSeriesResponse>();
            return result?.SeriesId ?? throw new InvalidOperationException("Failed to register series: Empty response");
        }

        private class RegisterSeriesResponse
        {
            public string SeriesId { get; set; } = "";
            public string Title { get; set; } = "";
        }
        public async Task<IEnumerable<SeriesSummaryResponse>> SearchSeriesAsync(string query, string? sort = null)
        {
            var url = $"/api/series?q={Uri.EscapeDataString(query ?? "")}";
            if (!string.IsNullOrEmpty(sort))
            {
                url += $"&sort={Uri.EscapeDataString(sort)}";
            }
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IEnumerable<SeriesSummaryResponse>>()
                   ?? Enumerable.Empty<SeriesSummaryResponse>();
        }

        public async Task<TrackerStats> GetStatsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/stats");
                if (!response.IsSuccessStatusCode) return new TrackerStats();
                
                return await response.Content.ReadFromJsonAsync<TrackerStats>() ?? new TrackerStats();
            }
            catch
            {
                return new TrackerStats();
            }
        }
    }
}
