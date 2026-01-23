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

        public ManifestStore(string root)
        {
            _root = root;
            Directory.CreateDirectory(_root);
        }

        public async Task<ManifestHash> PutAsync(ChapterManifest manifest)
        {
            var json = JsonSerializer.Serialize(
                manifest,
                new JsonSerializerOptions { WriteIndented = false });

            var bytes = Encoding.UTF8.GetBytes(json);
            var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
            var manifestHash = new ManifestHash(hash);

            var path = Path.Combine(_root, $"{hash}.json");
            if (File.Exists(path))
                return manifestHash;

            await File.WriteAllTextAsync(path, json);
            return manifestHash;
        }

        public async Task<ChapterManifest?> GetAsync(ManifestHash hash)
        {
            var path = Path.Combine(_root, $"{hash.Value}.json");
            if (!File.Exists(path))
                return null;

            var json = await File.ReadAllTextAsync(path);
            return JsonSerializer.Deserialize<ChapterManifest>(json);
        }
    }

}
