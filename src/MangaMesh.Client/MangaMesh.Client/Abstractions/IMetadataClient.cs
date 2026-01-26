using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MangaMesh.Client.Abstractions
{
    public interface IMetadataClient
    {
        Task<List<ChapterEntry>> GetChaptersForSeriesAsync(string seriesId);
    }
}
