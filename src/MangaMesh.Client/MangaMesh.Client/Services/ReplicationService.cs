using MangaMesh.Client.Abstractions;
using MangaMesh.Client.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace MangaMesh.Client.Services
{
    public sealed class ReplicationService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ReplicationService> _logger;

        private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);
        private readonly string _nodeId;
        private readonly string _publicIp;
        private readonly int _port;

        private readonly ConcurrentDictionary<string, PeerInfo> _connectedPeers
            = new();

        public string NodeId => _nodeId;

        public ReplicationService(
            IServiceScopeFactory scopeFactory,
            ILogger<ReplicationService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;

            _nodeId = Guid.NewGuid().ToString();
            _publicIp = "1.2.3.4";
            _port = 5000;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ReplicationService started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();

                    var tracker = scope.ServiceProvider.GetRequiredService<ITrackerClient>();
                    var fetcher = scope.ServiceProvider.GetRequiredService<IPeerFetcher>();
                    var subscriptionStore = scope.ServiceProvider.GetRequiredService<ISubscriptionStore>();
                    var manifests = scope.ServiceProvider.GetRequiredService<IManifestStore>();
                    var metadata = scope.ServiceProvider.GetRequiredService<IMetadataClient>();

                    await AnnounceAsync(tracker, manifests);
                    await SyncSubscriptionsAsync(
                        subscriptionStore,
                        manifests,
                        metadata,
                        fetcher
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Replication loop error");
                }

                await Task.Delay(_interval, stoppingToken);
            }
        }

        // 🔊 Announce manifests we host
        private async Task AnnounceAsync(
            ITrackerClient tracker,
            IManifestStore manifests)
        {
            var manifestHashes = await manifests.GetAllHashesAsync();

            try
            {
                await tracker.AnnounceAsync(
                    nodeId: _nodeId,
                    ip: _publicIp,
                    port: _port,
                    manifestHashes: manifestHashes.Select(m => m.Value).ToList()
                );

                _logger.LogInformation("Announced {count} manifests", manifestHashes.Count());
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Unable to announce");
            }
        }

        // 📚 Sync subscribed series
        private async Task SyncSubscriptionsAsync(
            ISubscriptionStore subscriptionStore,
            IManifestStore manifests,
            IMetadataClient metadata,
            IPeerFetcher fetcher)
        {
            var subs = await subscriptionStore.GetAllAsync();

            foreach (var sub in subs.Where(s => s.AutoFetch))
            {
                await SyncSeriesAsync(
                    sub.SeriesId,
                    sub.Language,
                    manifests,
                    metadata,
                    fetcher
                );
            }
        }

        // 📖 Sync one series
        private async Task SyncSeriesAsync(
            string seriesId,
            string language,
            IManifestStore manifests,
            IMetadataClient metadata,
            IPeerFetcher fetcher)
        {
            var chapters = await metadata.GetChaptersAsync(seriesId, language);

            foreach (var chapter in chapters)
            {
                var manifestHash = new ManifestHash(chapter.ManifestHash);

                if (await manifests.ExistsAsync(manifestHash))
                    continue;

                _logger.LogInformation(
                    "Fetching {series} Chapter {chapter}",
                    seriesId,
                    chapter.ChapterNumber
                );

                try
                {
                    await fetcher.FetchManifestAsync(chapter.ManifestHash);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to fetch chapter");
                }
            }
        }

        public int GetConnectedPeersCount()
        {
            var now = DateTime.UtcNow;
            return _connectedPeers.Values.Count(p => (now - p.LastSeen).TotalSeconds < 120);
        }

    }
}
