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

        /// <summary>
        /// Announce this node and the manifests it hosts
        /// </summary>
        public async Task<bool> AnnounceAsync(string nodeId, string ip, int port, List<string> manifestHashes)
        {
            var request = new AnnounceRequest(nodeId, ip, port, manifestHashes);
            var response = await _httpClient.PostAsJsonAsync("/announce", request);
            response.EnsureSuccessStatusCode();

            return true;
        }

        public async Task AnnounceManifestAsync(
             ManifestAnnouncement announcement,
             CancellationToken ct = default)
        {
            var response = await _httpClient.PostAsJsonAsync(
                "/api/tracker/announce",
                announcement,
                ct);

            if (response.IsSuccessStatusCode)
                return;

            // Duplicate announcement is OK (idempotent)
            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                return;

            var error = await response.Content.ReadAsStringAsync(ct);

            throw new InvalidOperationException(
                $"Tracker announce failed ({response.StatusCode}): {error}");
        }
    }
}
