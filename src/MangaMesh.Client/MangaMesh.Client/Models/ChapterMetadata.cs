using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MangaMesh.Client.Models
{
    public sealed record ChapterMetadata
    {
        public string SeriesId { get; init; } = "";
        public int ChapterNumber { get; init; }
        public string Language { get; init; } = "en";
        public string? ClaimedScanGroup { get; init; }
        public List<string> PageFiles { get; init; } = new(); // Optional: filenames override auto-scan
    }
}
