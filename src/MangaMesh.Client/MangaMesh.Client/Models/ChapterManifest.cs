using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MangaMesh.Client.Models
{
    public sealed record ChapterManifest
    {
        public int ManifestVersion { get; init; } = 1;
        public string SeriesId { get; init; } = "";
        public int ChapterNumber { get; init; }
        public string Language { get; init; } = "";
        public string? ClaimedScanGroup { get; init; }
        public IReadOnlyList<BlobHash> Pages { get; init; } = [];
        
        // Fields for signature verification and re-announcement
        public string ChapterId { get; init; } = "";
        public string Chapter { get; init; } = "";
        public string Title { get; init; } = "";
        public string ScanGroup { get; init; } = "";
        public long TotalSize { get; init; }
        public DateTime CreatedUtc { get; init; }

        public string? SignedBy { get; init; }
        public string? Signature { get; init; }
        public string? PublicKey { get; init; }
        public ReleaseLineId ReleaseLine { get; set; }
        public List<string> FilePaths { get; internal set; }
    }
}
