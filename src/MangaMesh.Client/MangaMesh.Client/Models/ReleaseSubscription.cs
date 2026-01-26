using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MangaMesh.Client.Models
{
    public sealed record ReleaseSubscription
    {
        public required ReleaseLineId ReleaseLine { get; init; }
        public bool AutoFetch { get; init; } = true;
        public DateTime SubscribedAtUtc { get; init; } = DateTime.UtcNow;
    }

}
