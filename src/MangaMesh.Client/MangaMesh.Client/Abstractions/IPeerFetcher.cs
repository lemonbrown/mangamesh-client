using MangaMesh.Client.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MangaMesh.Client.Abstractions
{
    public interface IPeerFetcher
    {
        Task<ManifestHash> FetchManifestAsync(string manifestHash);
    }
}
