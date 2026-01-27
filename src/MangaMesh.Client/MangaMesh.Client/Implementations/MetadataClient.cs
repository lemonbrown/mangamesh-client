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
    public sealed class HttpMetadataClient : IMetadataClient
    {
        private readonly HttpClient _http;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public HttpMetadataClient(HttpClient http)
        {
            _http = http;
        }

        public async Task<IReadOnlyList<ChapterMetadata>> GetChaptersAsync(
            ReleaseLineId releaseLine,
            CancellationToken ct = default)
        {
            var url =
                $"/api/releases/" +
                $"{releaseLine.SeriesId}/" +
                $"{releaseLine.ScanlatorId}/" +
                $"{releaseLine.Language}/chapters";

            var response = await _http.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            var chapters = JsonSerializer.Deserialize<List<ChapterMetadata>>(json, JsonOptions);

            return chapters ?? new List<ChapterMetadata>();
        }

        //public async Task PublishAsync(ChapterMetadata metadata, CancellationToken ct = default)
        //{
        //    if (metadata == null) throw new ArgumentNullException(nameof(metadata));

        //    // Announce to tracker
        //    var json = JsonSerializer.Serialize(metadata);
        //    var content = new StringContent(json, Encoding.UTF8, "application/json");

        //    // POST to tracker endpoint
        //    var response = await _http.PostAsync("api/tracker/announce", content, ct);
        //    response.EnsureSuccessStatusCode();
        //}
    }

}
