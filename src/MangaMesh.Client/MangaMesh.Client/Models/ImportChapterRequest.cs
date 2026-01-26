using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MangaMesh.Client.Models
{
    /// <summary>
    /// Represents a request to import a single manga chapter into the node.
    /// </summary>
    public class ImportChapterRequest
    {
        /// <summary>
        /// Series identifier (e.g., "one-piece")
        /// </summary>
        public string SeriesId { get; init; } = string.Empty;

        /// <summary>
        /// Scanlator identifier (e.g., "tcb-scans")
        /// </summary>
        public string ScanlatorId { get; init; } = string.Empty;

        /// <summary>
        /// Language code (e.g., "en", "jp")
        /// </summary>
        public string Language { get; init; } = string.Empty;

        /// <summary>
        /// Chapter number
        /// </summary>
        public int ChapterNumber { get; init; }

        /// <summary>
        /// Path to the folder containing chapter image files
        /// </summary>
        public string SourceDirectory { get; init; } = string.Empty;

        /// <summary>
        /// Optional: override for release line, if needed
        /// </summary>
        public string? ReleaseLineOverride { get; init; }

        public ReleaseType ReleaseType { get; set; }

        public string DisplayName { get; set; } = "";
    }

}
