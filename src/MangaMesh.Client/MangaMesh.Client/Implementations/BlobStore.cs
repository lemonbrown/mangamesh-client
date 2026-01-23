using MangaMesh.Client.Abstractions;
using MangaMesh.Client.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MangaMesh.Client.Implementations
{
    public sealed class BlobStore : IBlobStore
    {
        private readonly string _root;

        public BlobStore(string root)
        {
            _root = root;
            Directory.CreateDirectory(_root);
        }

        public async Task<BlobHash> PutAsync(Stream data)
        {
            using var sha = SHA256.Create();
            using var temp = new MemoryStream();

            await data.CopyToAsync(temp);
            temp.Position = 0;

            var hashBytes = sha.ComputeHash(temp);
            var hash = Convert.ToHexString(hashBytes).ToLowerInvariant();
            var blobHash = new BlobHash(hash);

            var path = GetPath(blobHash);
            if (File.Exists(path))
                return blobHash;

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            temp.Position = 0;
            var tmpPath = path + ".tmp";

            await using (var fs = File.Create(tmpPath))
                await temp.CopyToAsync(fs);

            File.Move(tmpPath, path, overwrite: false);

            return blobHash;
        }

        public async Task<Stream?> OpenReadAsync(BlobHash hash)
        {
            var path = GetPath(hash);
            if (!File.Exists(path))
                return null;

            return File.OpenRead(path);
        }

        public bool Exists(BlobHash hash)
            => File.Exists(GetPath(hash));

        private string GetPath(BlobHash hash)
        {
            var a = hash.Value[..2];
            var b = hash.Value[2..4];
            return Path.Combine(_root, a, b, hash.Value);
        }
    }

}
