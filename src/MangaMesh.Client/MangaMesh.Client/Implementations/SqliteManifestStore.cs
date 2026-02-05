using MangaMesh.Client.Abstractions;
using MangaMesh.Client.Data;
using MangaMesh.Client.Entities;
using MangaMesh.Client.Models;
using MangaMesh.Shared.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text;

namespace MangaMesh.Client.Implementations
{
    public sealed class SqliteManifestStore : IManifestStore
    {
        private readonly ClientDbContext _context;

        public SqliteManifestStore(ClientDbContext context)
        {
            _context = context;
        }

        public async Task<bool> ExistsAsync(ManifestHash manifestHash)
        {
            return await _context.Manifests.AnyAsync(m => m.Hash == manifestHash.Value);
        }

        public async Task DeleteAsync(ManifestHash hash)
        {
            var entity = await _context.Manifests.FindAsync(hash.Value);
            if (entity != null)
            {
                _context.Manifests.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<ManifestHash>> GetAllHashesAsync()
        {
            return await _context.Manifests
                .Select(m => new ManifestHash(m.Hash))
                .ToListAsync();
        }

        public async Task<ChapterManifest?> GetAsync(ManifestHash hash)
        {
            var entity = await _context.Manifests.FindAsync(hash.Value);
            if (entity == null) return null;

            return Deserialize(entity.DataJson);
        }

        public async Task<ChapterManifest?> GetBySeriesAndChapterIdAsync(string seriesId, string chapterId)
        {
            var entity = await _context.Manifests
                .FirstOrDefaultAsync(m => m.SeriesId == seriesId && m.ChapterId == chapterId);

            if (entity == null) return null;

            return Deserialize(entity.DataJson);
        }

        public async Task<(string SetHash, int Count)> GetSetHashAsync()
        {
            var hashes = await _context.Manifests
                .OrderBy(m => m.Hash)
                .Select(m => m.Hash)
                .ToListAsync();

            if (hashes.Count == 0)
                return (string.Empty, 0);

            var sb = new StringBuilder();
            foreach (var hash in hashes)
            {
                sb.Append(hash);
            }

            var inputBytes = Encoding.UTF8.GetBytes(sb.ToString());
            var hashBytes = SHA256.HashData(inputBytes);

            return (Convert.ToHexString(hashBytes).ToLowerInvariant(), hashes.Count);
        }

        public async Task<ManifestHash> PutAsync(ChapterManifest manifest)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };

            // Re-serialize to compute hash, ensuring we use canonical form if possible, 
            // but for storage ID we just need a stable hash. 
            // The original ManifestStore didn't implement PutAsync, but PeerFetcher uses it.
            // PeerFetcher passes in a manifest it just downloaded.

            // We'll mimic the "canonical" serialization or just robust serialization
            var normalize = manifest with
            {
                Files = manifest.Files.OrderBy(f => f.Path).ToArray()
            };
            var json = JsonSerializer.Serialize(normalize, options);
            var bytes = Encoding.UTF8.GetBytes(json);
            var hashBytes = SHA256.HashData(bytes);
            var hash = Convert.ToHexString(hashBytes).ToLowerInvariant();

            await SaveAsync(new ManifestHash(hash), manifest);

            return new ManifestHash(hash);
        }

        public async Task SaveAsync(ManifestHash hash, ChapterManifest manifest)
        {
            var exists = await _context.Manifests.AnyAsync(m => m.Hash == hash.Value);
            if (exists) return;

            var json = JsonSerializer.Serialize(manifest, JsonOptions);

            var entity = new ManifestEntity
            {
                Hash = hash.Value,
                SeriesId = manifest.SeriesId,
                ChapterId = manifest.ChapterId,
                DataJson = json,
                CreatedUtc = DateTime.UtcNow
            };

            _context.Manifests.Add(entity);
            await _context.SaveChangesAsync();
        }

        private static ChapterManifest? Deserialize(string json)
        {
            return JsonSerializer.Deserialize<ChapterManifest>(json, JsonOptions);
        }

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };
    }
}
