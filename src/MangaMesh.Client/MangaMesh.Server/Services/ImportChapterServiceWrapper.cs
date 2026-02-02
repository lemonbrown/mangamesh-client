using MangaMesh.Client.Models;
using MangaMesh.Client.Services;
using MangaMesh.Server.Models;

namespace MangaMesh.Server.Services
{
    public class ImportChapterServiceWrapper : IImportChapterService
    {
        private readonly ImportChapterService _importService;

        public ImportChapterServiceWrapper(ImportChapterService importService)
        {
            _importService = importService;
        }

        public async Task<ImportResultDto> ImportAsync(ImportChapterRequestDto request, CancellationToken ct = default)
        {
            var result = await _importService.ImportAsync(new ImportChapterRequest
            {
                SeriesId = request.SeriesId,
                ScanlatorId = request.ScanlatorId,
                Language = request.Language,
                ChapterNumber = request.ChapterNumber,
                SourceDirectory = request.SourcePath,
                ReleaseType = request.ReleaseType switch
                {
                    "VerifiedScanlation" => ReleaseType.VerifiedScanlation,
                    "UnverifiedScanlation" => ReleaseType.UnverifiedScanlation,
                    "RoughTranslation" => ReleaseType.RoughTranslation,
                    "Raw" => ReleaseType.Raw,
                    _ => ReleaseType.Unknown
                },
                DisplayName = request.DisplayName
            }, ct);

            return new ImportResultDto(
                ManifestHash: result.ManifestHash.Value,
                FilesImported: result.FileCount
            );
        }

        public async Task ReannounceAsync(MangaMesh.Client.Models.ManifestHash hash, string nodeId)
        {
            await _importService.ReannounceAsync(hash, nodeId);
        }
    }

}
