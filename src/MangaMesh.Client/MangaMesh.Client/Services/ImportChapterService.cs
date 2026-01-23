using MangaMesh.Client.Abstractions;
using MangaMesh.Client.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MangaMesh.Client.Services
{
    internal class ImportChapterService
    {

        private readonly IBlobStore _blobStore;

        private readonly IManifestStore _manifestStore;

        public ImportChapterService(IBlobStore blobStore, IManifestStore manifestStore)
        {
            _blobStore = blobStore;

            _manifestStore = manifestStore; 
        }

        public async Task<ManifestHash> ImportChapterAsync(
            string folderPath,
            ChapterManifest template)
        {
            var pages = new List<BlobHash>();

            var metadataService = new ChapterMetadataService();

            foreach (var file in Directory.GetFiles(folderPath).OrderBy(f => f))
            {
                if (file.Contains("json"))
                    continue;

                await using var fs = File.OpenRead(file);
                var hash = await _blobStore.PutAsync(fs);
                pages.Add(hash);
            }

            var manifest = template with { Pages = pages };

            return await _manifestStore.PutAsync(manifest);
        }

    }
}
