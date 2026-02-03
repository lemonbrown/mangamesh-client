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
        private readonly INodeIdentityService _nodeIdentity;

        public ImportChapterService(
            IBlobStore blobStore,
            IManifestStore manifestStore,
            ITrackerClient trackerClient,
            IKeyStore keyStore,
            INodeIdentityService nodeIdentity)
        {
            _blobStore = blobStore;

            _manifestStore = manifestStore;

            _trackerClient = trackerClient;

            _keyStore = keyStore;

            _nodeIdentity = nodeIdentity;
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

            // Step 2: save blobs and create chapter manifest
            var pageHashes = new List<BlobHash>();

            foreach (var file in files)
            {
                using var stream = File.OpenRead(file);
                var blobHash = await _blobStore.PutAsync(stream);
                pageHashes.Add(blobHash);
            }

            // Step 2.1: Register Series to get authoritative ID
            var seriesId = await _trackerClient.RegisterSeriesAsync(request.Source, request.ExternalMangaId);

            // Step 3: compute hash for the manifest

            var totalSize = files.Sum(f => new FileInfo(f).Length);


            var keyPair = await _keyStore.GetAsync();


            Shared.Models.ChapterManifest chapterManifest = new()
            {
                ChapterNumber = request.ChapterNumber,
                CreatedUtc = new DateTime(DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond * TimeSpan.TicksPerMillisecond, DateTimeKind.Utc),
                ChapterId = request.SeriesId + ":" + request.ChapterNumber.ToString(),
                Language = request.Language,
                SeriesId = request.SeriesId,
                ScanGroup = request.ScanlatorId,
                Title = request.DisplayName,
                TotalSize = totalSize,
                PublicKey = keyPair.PublicKeyBase64,
                SignedBy = "self"
            };

            var hash = ManifestHash.FromManifest(chapterManifest);

            var isManifestExisting = await _manifestStore.ExistsAsync(hash);

            if (!isManifestExisting)
            {
                // Step 4: save manifest
                await _manifestStore.SaveAsync(hash, chapterManifest);

                // Step 5: publish manifest to trackers

                byte[] privateKeyBytes = Convert.FromBase64String(keyPair.PrivateKeyBase64);

                var key = Key.Import(
                    SignatureAlgorithm.Ed25519,
                    privateKeyBytes,
                    KeyBlobFormat.RawPrivateKey);

                var signedManifest = ManifestSigningService.SignManifest(chapterManifest, key);

                // Step 5.2 publish to trackers

                await _trackerClient.AnnounceManifestAsync(new AnnounceManifestRequest
                {
                    NodeId = _nodeIdentity.NodeId,
                    ManifestHash = hash,
                    SeriesId = chapterManifest.SeriesId,
                    ChapterNumber = chapterManifest.ChapterNumber,
                    Language = chapterManifest.Language,
                    ScanlatorId = chapterManifest.ScanGroup,
                    ReleaseType = request.ReleaseType,
                    Source = request.Source,
                    ExternalMangaId = request.ExternalMangaId,

                    // Added fields for verification
                    ChapterId = chapterManifest.ChapterId,
                    Chapter = chapterManifest.ChapterNumber.ToString(),
                    Title = chapterManifest.Title,
                    ScanGroup = chapterManifest.ScanGroup,
                    TotalSize = chapterManifest.TotalSize,
                    CreatedUtc = chapterManifest.CreatedUtc,
                    Signature = signedManifest.Signature,
                    PublicKey = signedManifest.PublisherPublicKey
                });

                // Step 6: update manifest with signature details before returning/saving if needed
                // Ideally we should save the signed details back to the manifest store so we can re-announce later.
                // The current flow saves *before* signing (Step 4), which means the saved manifest lacks signature.
                // We should update the manifest with signature and save it again or save it after signing.

                // Let's update the local manifest object with signature details and re-save.
                // Note: The manifest stored is Client.Models.ChapterManifest.
                // We need to map the fields back.

                //manifest = manifest with
                //{
                //    ChapterId = chapterManifest.ChapterId,
                //    Chapter = chapterManifest.Chapter,
                //    Title = chapterManifest.Title,
                //    ScanGroup = chapterManifest.ScanGroup,
                //    TotalSize = chapterManifest.TotalSize,
                //    CreatedUtc = chapterManifest.CreatedUtc,
                //    Signature = signedManifest.Signature,
                //    PublicKey = signedManifest.PublisherPublicKey,
                //    SignedBy = "self" // or some logic
                //};

                // Re-save with signature
                chapterManifest = chapterManifest with
                {
                    Signature = signedManifest.Signature
                };

                await _manifestStore.SaveAsync(hash, chapterManifest);
            }

            // Step 6: return result
            return new ImportChapterResult
            {
                ManifestHash = hash,
                FileCount = files.Length,
                AlreadyExists = isManifestExisting
            };
        }

        public async Task ReannounceAsync(ManifestHash hash, string nodeId)
        {
            var manifest = await _manifestStore.GetAsync(hash);
            if (manifest == null)
                throw new FileNotFoundException($"Manifest {hash} not found");

            if (string.IsNullOrEmpty(manifest.Signature) || string.IsNullOrEmpty(manifest.PublicKey))
                throw new InvalidOperationException("Manifest does not contain signature data. Cannot re-announce.");

            await _trackerClient.AnnounceManifestAsync(new AnnounceManifestRequest
            {
                NodeId = nodeId,
                ManifestHash = hash,
                SeriesId = manifest.SeriesId,
                ChapterNumber = manifest.ChapterNumber,
                Language = manifest.Language,
                ScanlatorId = manifest.ScanGroup,
                ReleaseType = ReleaseType.VerifiedScanlation, // Assuming VerifiedScanlation for signed manifests

                // Verification fields
                ChapterId = manifest.ChapterId,
                Chapter = manifest.ChapterNumber.ToString(),
                Title = manifest.Title,
                ScanGroup = manifest.ScanGroup,
                TotalSize = manifest.TotalSize,
                CreatedUtc = manifest.CreatedUtc,
                Signature = manifest.Signature,
                PublicKey = manifest.PublicKey
            });
        }

        private static bool IsImageFile(string path)
        {
            var ext = Path.GetExtension(path)?.ToLowerInvariant();
            return ext is ".jpg" or ".jpeg" or ".png" or ".webp";
        }


    }
}
