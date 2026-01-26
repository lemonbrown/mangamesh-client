using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MangaMesh.Client.Abstractions;
using MangaMesh.Client.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;


namespace MangaMesh.Client.Services
{
    public sealed class ReplicationService : BackgroundService
    {
        private readonly ITrackerClient _tracker;
        private readonly IPeerFetcher _fetcher;
        private readonly ISubscriptionStore _subscriptions;
        private readonly IManifestStore _manifests;
        private readonly IMetadataClient _metadata;
        private readonly ILogger<ReplicationService> _logger;

        private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);
        private readonly string _nodeId;
        private readonly string _publicIp;
        private readonly int _port;

        public ReplicationService(
            ITrackerClient tracker,
            IPeerFetcher fetcher,
            ISubscriptionStore subscriptions,
            IManifestStore manifests,
            IMetadataClient metadata,
            ILogger<ReplicationService> logger,
            string nodeId,
            string publicIp,
            int port)
        {
            _tracker = tracker;
            _fetcher = fetcher;
            _subscriptions = subscriptions;
            _manifests = manifests;
            _metadata = metadata;
            _logger = logger;

            _nodeId = nodeId;
            _publicIp = publicIp;
            _port = port;
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
            await _tracker.AnnounceAsync(
                nodeId: _nodeId,
                ip: _publicIp,
                port: _port,
                manifestHashes: manifestHashes.Select(m => m.Value).ToList()
            );

            _logger.LogInformation("Announced {count} manifests", manifestHashes.Count());
        }

        // 📚 Sync subscribed series
        private async Task SyncSubscriptionsAsync()
        {
            var subs = await _subscriptions.GetAllAsync();

            foreach (var sub in subs.Where(s => s.AutoFetch))
            {
                await SyncSeriesAsync(sub.SeriesId);
            }
        }

        // 📖 Sync one series
        private async Task SyncSeriesAsync(string seriesId)
        {
            var chapters = await _metadata.GetChaptersForSeriesAsync(seriesId);

            foreach (var chapter in chapters)
            {
                var manifestHash = new ManifestHash(chapter.ManifestHash);

                if (await _manifests.ExistsAsync(manifestHash))
                    continue;

                _logger.LogInformation(
                    "Fetching {series} Chapter {chapter}",
                    seriesId,
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
    }

}
