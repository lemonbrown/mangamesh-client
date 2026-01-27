using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MangaMesh.Client.Models
{
    public sealed class Manga
    {
        public Guid Id { get; init; } = Guid.NewGuid();

        public string ExternalSource { get; init; } = "mangadex";
        public string ExternalId { get; init; } = default!;

        public string Title { get; init; } = default!;
        public string? Description { get; init; }

        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    }

}
