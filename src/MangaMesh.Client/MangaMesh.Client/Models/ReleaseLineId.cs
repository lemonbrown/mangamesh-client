using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MangaMesh.Client.Models
{
    public record ReleaseLineId(
        string SeriesId,
        string ScanlatorId,
        string Language
    );

}
