using MangaMesh.Client.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MangaMesh.Client.Abstractions
{
    public interface IBlobStore
    {
        Task<BlobHash> PutAsync(Stream data);
        Task<Stream?> OpenReadAsync(BlobHash hash);
        bool Exists(BlobHash hash);
    }
}
