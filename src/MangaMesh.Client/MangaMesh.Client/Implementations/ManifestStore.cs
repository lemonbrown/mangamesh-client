using MangaMesh.Client.Abstractions;
using MangaMesh.Client.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MangaMesh.Client.Implementations
{
    public sealed class ManifestStore : IManifestStore
    {
        private readonly string _root;

        public ManifestStore(string rootDirectory)
        {
            _root = rootDirectory;
            Directory.CreateDirectory(_root);
        }

        public Task<IEnumerable<ManifestHash>> GetAllHashesAsync()
        {
            var files = Directory.GetFiles(_root, "*.json", SearchOption.TopDirectoryOnly);

            var hashes = files
                .Select(Path.GetFileNameWithoutExtension)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(ManifestHash.Parse)
                .ToList();

            return Task.FromResult<IEnumerable<ManifestHash>>(hashes);
        }

        public async Task SaveAsync(ManifestHash hash, ChapterManifest manifest)
        {
            var path = GetPath(hash);
            var json = JsonSerializer.Serialize(manifest, JsonOptions);
            await File.WriteAllTextAsync(path, json);
        }

        public Task<bool> ExistsAsync(ManifestHash hash)
            => Task.FromResult(File.Exists(GetPath(hash)));

        public async Task<ChapterManifest?> LoadAsync(ManifestHash hash)
        {
            var path = GetPath(hash);
            if (!File.Exists(path))
                return null;

            var json = await File.ReadAllTextAsync(path);
            return JsonSerializer.Deserialize<ChapterManifest>(json, JsonOptions);
        }

        private string GetPath(ManifestHash hash)
            => Path.Combine(_root, $"{hash.Value}.json");

        public Task<ManifestHash> PutAsync(ChapterManifest manifest)
        {
            throw new NotImplementedException();
        }

        public Task<ChapterManifest?> GetAsync(ManifestHash hash)
        {
            throw new NotImplementedException();
        }

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }

}
