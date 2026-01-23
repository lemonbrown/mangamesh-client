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

        public TrackerClient(string trackerBaseUrl)
        {
            _httpClient = new HttpClient { BaseAddress = new Uri(trackerBaseUrl) };
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
    }
}
