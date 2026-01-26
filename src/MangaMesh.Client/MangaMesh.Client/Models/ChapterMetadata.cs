using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MangaMesh.Client.Models
{
    public sealed record ChapterMetadata
    {
        public required ReleaseLineId ReleaseLine { get; init; }
        public required int ChapterNumber { get; init; }
        public required string ManifestHash { get; init; }

        public string? Title { get; init; }
        public DateTime PublishedAtUtc { get; init; }

        // Optional trust info
        public string? SigningKeyId { get; init; }
        public string? Signature { get; init; }

        public ReleaseType ReleaseType { get; init; }

        public ReleaseIdentity Identity { get; init; } = new();
    }
}
