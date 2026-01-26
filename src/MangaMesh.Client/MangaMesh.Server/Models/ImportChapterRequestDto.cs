namespace MangaMesh.Server.Models
{
    public record ImportChapterRequestDto(
        string SeriesId,
        string ScanlatorId,
        string Language,
        int ChapterNumber,
        string SourcePath,
        string DisplayName,
        string ReleaseType
    );

}
