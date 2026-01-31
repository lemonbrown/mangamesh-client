using MangaMesh.Client.Abstractions;
using MangaMesh.Client.Models;
using MangaMesh.Shared.Helpers;
using MangaMesh.Shared.Services;
using NSec.Cryptography;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace MangaMesh.Client.Services
{
    public class ImportChapterService : IImportChapterService
    {

        private readonly IBlobStore _blobStore;

        private readonly IManifestStore _manifestStore;

        private readonly ITrackerClient _trackerClient;

        private readonly IKeyStore _keyStore;

        public ImportChapterService(
            IBlobStore blobStore, 
            IManifestStore manifestStore, 
            ITrackerClient trackerClient,
            IKeyStore keyStore)
        {
            _blobStore = blobStore;

            _manifestStore = manifestStore;

            _trackerClient = trackerClient;

            _keyStore = keyStore;
        }

        /// <summary>
        /// Imports a chapter from a folder, creates a manifest, stores it, and publishes metadata.
        /// </summary>
        public async Task<ImportChapterResult> ImportAsync(ImportChapterRequest request, CancellationToken ct = default)
        {
            if (!Directory.Exists(request.SourceDirectory))
                throw new DirectoryNotFoundException($"Source path not found: {request.SourceDirectory}");

            // Step 1: enumerate files
            var files = Directory.GetFiles(request.SourceDirectory)
                .Where(f => IsImageFile(f))
                .OrderBy(f => f)
                .ToArray();

            if (files.Length == 0)
                throw new InvalidOperationException("No valid image files found in source folder.");

            // Step 2: create chapter manifest
            var manifest = new ChapterManifest
            {
                SeriesId = request.SeriesId,
                ReleaseLine = new ReleaseLineId(request.ScanlatorId, request.SeriesId, request.Language),
                Language = request.Language,
                ChapterNumber = request.ChapterNumber,
                FilePaths = files.ToList(),
            };

            // Step 3: compute hash for the manifest
            var hash = ManifestHash.FromManifest(manifest);

            var isManifestExisting = await _manifestStore.ExistsAsync(ManifestHash.FromManifest(manifest));

            if (!isManifestExisting)
            {
                // Step 4: save manifest
                await _manifestStore.SaveAsync(hash, manifest);

                // Step 5: publish manifest to trackers

                // Step 5.1 sign the manifest

                Shared.Models.ChapterManifest chapterManifest = new()
                {
                    Chapter = manifest.ChapterNumber.ToString(),
                    CreatedUtc = DateTime.UtcNow,
                    ChapterId =  request.SeriesId + ":" + manifest.ChapterNumber.ToString(),
                    Language = request.Language,
                    SeriesId = request.SeriesId,
                    ScanGroup = request.ScanlatorId,
                    Title = "",
                    TotalSize = 0,
                };

                var keyPair = await _keyStore.GetAsync();

                byte[] privateKeyBytes = Convert.FromBase64String(keyPair.PrivateKeyBase64);

                var key = Key.Import(
                    SignatureAlgorithm.Ed25519,
                    privateKeyBytes,
                    KeyBlobFormat.RawPrivateKey);

                var signedManifest = ManifestSigningService.SignManifest(chapterManifest, key);

                // Step 5.2 publish to trackers

                await _trackerClient.AnnounceManifestAsync(new ManifestAnnouncement
                {
                    NodeId = request.NodeId,
                    ManifestHash = hash,
                    SeriesId = manifest.SeriesId,
                    ChapterNumber = manifest.ChapterNumber,
                    Language = manifest.Language,
                    ScanlatorId = manifest.ReleaseLine.ScanlatorId,
                    ReleaseType = request.ReleaseType
                });
            }          

            // Step 6: return result
            return new ImportChapterResult
            {
                ManifestHash = hash,
                FileCount = files.Length,
                AlreadyExists = isManifestExisting
            };
        }

        private static bool IsImageFile(string path)
        {
            var ext = Path.GetExtension(path)?.ToLowerInvariant();
            return ext is ".jpg" or ".jpeg" or ".png" or ".webp";
        }


    }
}
