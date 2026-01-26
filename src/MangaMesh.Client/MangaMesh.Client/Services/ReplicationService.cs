using MangaMesh.Client.Abstractions;
using MangaMesh.Client.Implementations;
using MangaMesh.Client.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace MangaMesh.Client.Services
{
    public sealed class ReplicationService : BackgroundService
    {
        private readonly ITrackerClient _tracker;
        private readonly IPeerFetcher _fetcher;
        private readonly ISubscriptionStore _subscriptionStore;
        private readonly IManifestStore _manifests;
        private readonly IMetadataClient _metadata;
        private readonly ILogger<ReplicationService> _logger;

        private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);
        private readonly string _nodeId;
        private readonly string _publicIp;
        private readonly int _port;

        private readonly ConcurrentDictionary<string, SeriesSubscription> _subscriptions
            = new(StringComparer.OrdinalIgnoreCase);

        // Key: Node ID, Value: Peer info (IP, Port, LastSeen, etc.)
        private readonly ConcurrentDictionary<string, PeerInfo> _connectedPeers
            = new();

        public string NodeId { get; } = Guid.NewGuid().ToString();

        public ReplicationService(
            ITrackerClient tracker,
            IPeerFetcher fetcher,
            ISubscriptionStore subscriptionStore,
            IManifestStore manifests,
            IMetadataClient metadata,
            ILogger<ReplicationService> logger,
            string nodeId,
            string publicIp,
            int port)
        {
            _tracker = tracker;
            _fetcher = fetcher;
            _subscriptionStore = subscriptionStore;
            _manifests = manifests;
            _metadata = metadata;
            _logger = logger;

            _nodeId = nodeId;
            _publicIp = publicIp;
            _port = port;
        }

        // Example: method called when a peer connects
        public void AddPeer(PeerInfo peer)
        {
            _connectedPeers[peer.NodeId] = peer;
        }

        // Example: method called when a peer disconnects
        public void RemovePeer(string nodeId)
        {
            _connectedPeers.TryRemove(nodeId, out _);
        }

        // ✅ The method your UI / NodeStatusService needs
        public int GetConnectedPeersCount()
        {
            // Optionally, filter out stale peers based on LastSeen
            var now = DateTime.UtcNow;
            return _connectedPeers.Values.Count(p => (now - p.LastSeen).TotalSeconds < 120);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ReplicationService started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await AnnounceAsync();
                    await SyncSubscriptionsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Replication loop error");
                }

                await Task.Delay(_interval, stoppingToken);
            }
        }

        // 🔊 Announce manifests we host
        private async Task AnnounceAsync()
        {
            var manifestHashes = await _manifests.GetAllHashesAsync();
            try
            {
                await _tracker.AnnounceAsync(
                    nodeId: _nodeId,
                    ip: _publicIp,
                    port: _port,
                    manifestHashes: manifestHashes.Select(m => m.Value).ToList()
                );

                _logger.LogInformation("Announced {count} manifests", manifestHashes.Count());
            }
            catch (HttpRequestException exception)
            {
                _logger.LogError($"Unable to annouce: {exception.Message}");
            }

        }

        // 📚 Sync subscribed series
        private async Task SyncSubscriptionsAsync()
        {
            var subs = await _subscriptionStore.GetAllAsync();

            foreach (var sub in subs.Where(s => s.AutoFetch))
            {
                await SyncSeriesAsync(sub.ReleaseLine);
            }
        }

        // 📖 Sync one series
        private async Task SyncSeriesAsync(ReleaseLineId releaseLineId)
        {
            var chapters = await _metadata.GetChaptersAsync(releaseLineId);

            foreach (var chapter in chapters)
            {
                var manifestHash = new ManifestHash(chapter.ManifestHash);

                if (await _manifests.ExistsAsync(manifestHash))
                    continue;

                _logger.LogInformation(
                    "Fetching {series} Chapter {chapter}",
                    releaseLineId.SeriesId,
                    chapter.ChapterNumber
                );

                try
                {
                    await _fetcher.FetchManifestAsync(chapter.ManifestHash);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to fetch chapter");
                }
            }
        }

        // ✅ Count of manifests this node is currently seeding
        public async Task<int> GetSeededManifestCountAsync(CancellationToken ct = default)
        {
            var hashes = await _manifests.GetAllHashesAsync();
            return hashes.Count();
        }

        // Add a subscription
        public bool SubscribeToReleaseLine(string seriesId, string language)
        {
            var key = GetKey(seriesId, language);
            return _subscriptions.TryAdd(key,new SeriesSubscription()
            {
                Language = language,
                SeriesId = seriesId
            });
        }

        // Remove a subscription
        public bool UnsubscribeFromReleaseLine(string seriesId, string language)
        {
            var key = GetKey(seriesId, language);
            return _subscriptions.TryRemove(key, out _);
        }

        // ✅ Get all subscriptions
        public IEnumerable<SeriesSubscription> GetSubscriptions()
            => _subscriptions.Values;

        // Generate a unique key for dictionary
        private static string GetKey(string seriesId, string language)
            => $"{seriesId.ToLowerInvariant()}|{language.ToLowerInvariant()}";

    }

}
