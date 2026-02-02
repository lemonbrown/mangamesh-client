using MangaMesh.Client.Models;
using MangaMesh.Client.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MangaMesh.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SeriesController : ControllerBase
    {
        private readonly IManifestStore _manifestStore;
        private readonly ITrackerClient _trackerClient;
        private readonly IBlobStore _blobStore;

        public SeriesController(IManifestStore manifestStore, ITrackerClient trackerClient, IBlobStore blobStore)
        {
            _manifestStore = manifestStore;
            _trackerClient = trackerClient;
            _blobStore = blobStore;
        }

        [HttpGet("{seriesId}/chapter/{chapterId}/manifest/{manifestHash}/read")]
        public async Task<IResult> ReadChapter(string seriesId, string chapterId, string manifestHash)
        {
            //ask the central api for the peer node
            var peer = await _trackerClient.GetPeerAsync(seriesId, chapterId, manifestHash);

            if (peer == null)
            {
                return Results.NotFound("No peer found for this chapter manifest");
            }

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://" + peer.IP + ":" + peer.Port),
            };

            //request the peer manifest for this chapter
            var manifestResponse = await httpClient.GetAsync($"api/series/manifest/{manifestHash}");

            if (!manifestResponse.IsSuccessStatusCode)
            {
                return Results.NotFound("Peer did not have the manifest");
            }

            var manifest = await manifestResponse.Content.ReadFromJsonAsync<ChapterManifest>();

            if (manifest == null)
            {
                return Results.Problem("Failed to deserialize manifest from peer");
            }

            // Download pages
            foreach (var page in manifest.Pages)
            {
                if (_blobStore.Exists(page)) continue;

                var blobResponse = await httpClient.GetAsync($"api/blob/{page.Value}");
                if (blobResponse.IsSuccessStatusCode)
                {
                    using var stream = await blobResponse.Content.ReadAsStreamAsync();
                    await _blobStore.PutAsync(stream);
                }
            }

            // Save manifest 
            await _manifestStore.PutAsync(manifest);

            return Results.Ok(manifest);
        }

        [HttpGet("manifest/{manifestHash}")]
        public async Task<IResult> GetManifestByHash(string manifestHash)
        {
            var hash = new ManifestHash(manifestHash);
            var manifest = await _manifestStore.GetAsync(hash);

            if (manifest == null)
            {
                return Results.NotFound();
            }

            return Results.Ok(manifest);
        }
    }
}
