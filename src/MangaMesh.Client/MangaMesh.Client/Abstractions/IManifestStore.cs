using MangaMesh.Client.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MangaMesh.Client.Abstractions
{
    public interface IManifestStore
    {

        public Task SaveAsync(ManifestHash hash, ChapterManifest manifest);
        Task<ManifestHash> PutAsync(ChapterManifest manifest);
        Task<ChapterManifest?> GetAsync(ManifestHash hash);
        Task<bool> ExistsAsync(ManifestHash manifestHash);
        Task<IEnumerable<ManifestHash>> GetAllHashesAsync();
    }
}
