using MangaMesh.Client.Abstractions;
using MangaMesh.Client.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MangaMesh.Client.Implementations
{
    public sealed class FileSubscriptionStore : ISubscriptionStore
    {
        private readonly string _path;
        private readonly SemaphoreSlim _lock = new(1, 1);

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        public FileSubscriptionStore(string path)
        {
            _path = path;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        }

        public async Task<IReadOnlyList<ReleaseSubscription>> GetAllAsync()
        {
            await _lock.WaitAsync();
            try
            {
                return (await LoadAsync()).AsReadOnly();
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task AddAsync(ReleaseSubscription subscription)
        {
            await _lock.WaitAsync();
            try
            {
                var subs = await LoadAsync();

                if (subs.Any(s => SameReleaseLine(s.ReleaseLine, subscription.ReleaseLine)))
                    return;

                subs.Add(subscription);
                await SaveAsync(subs);
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task RemoveAsync(ReleaseLineId releaseLine)
        {
            await _lock.WaitAsync();
            try
            {
                var subs = await LoadAsync();
                subs.RemoveAll(s => SameReleaseLine(s.ReleaseLine, releaseLine));
                await SaveAsync(subs);
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task<bool> ExistsAsync(ReleaseLineId releaseLine)
        {
            await _lock.WaitAsync();
            try
            {
                var subs = await LoadAsync();
                return subs.Any(s => SameReleaseLine(s.ReleaseLine, releaseLine));
            }
            finally
            {
                _lock.Release();
            }
        }

        private async Task<List<ReleaseSubscription>> LoadAsync()
        {
            if (!File.Exists(_path))
                return new List<ReleaseSubscription>();

            var json = await File.ReadAllTextAsync(_path);
            var container = JsonSerializer.Deserialize<Container>(json, JsonOptions);
            return container?.Subscriptions ?? new List<ReleaseSubscription>();
        }

        private async Task SaveAsync(List<ReleaseSubscription> subs)
        {
            var container = new Container { Subscriptions = subs };
            var json = JsonSerializer.Serialize(container, JsonOptions);
            await File.WriteAllTextAsync(_path, json);
        }

        private static bool SameReleaseLine(ReleaseLineId a, ReleaseLineId b) =>
            a.SeriesId == b.SeriesId &&
            a.ScanlatorId == b.ScanlatorId &&
            a.Language == b.Language;

        private sealed class Container
        {
            public List<ReleaseSubscription> Subscriptions { get; set; } = new();
        }
    }

}
