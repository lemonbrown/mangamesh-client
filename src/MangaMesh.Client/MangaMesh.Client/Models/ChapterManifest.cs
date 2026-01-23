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
        public string? SignedBy { get; init; }
        public string? Signature { get; init; }
    }

}
