using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MangaMesh.Client.Models
{
    public record SeriesSubscription
    {
        public string SeriesId { get; init; } = "";
        public bool AutoFetch { get; init; } = true;
    }

}
