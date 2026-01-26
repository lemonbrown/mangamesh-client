using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MangaMesh.Client.Models
{
    public sealed class ManifestAnnouncement
    {
        // Who is announcing
        public string NodeId { get; init; } = string.Empty;

        // What is being announced
        public ManifestHash ManifestHash { get; init; } = default!;

        // Canon reference (for indexing & querying)
        public string SeriesId { get; init; } = string.Empty;
        public int ChapterNumber { get; init; }

        // Release-specific metadata
        public string Language { get; init; } = string.Empty;
        public string? ScanlatorId { get; init; }
        public ReleaseType ReleaseType { get; init; }
        
        // Timestamp (useful for rough ordering, not truth)
        public DateTimeOffset AnnouncedAt { get; init; } = DateTimeOffset.UtcNow;
    }
}
