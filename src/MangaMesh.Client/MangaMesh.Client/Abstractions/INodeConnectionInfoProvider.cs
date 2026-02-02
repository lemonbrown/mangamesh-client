using System;
using System.Threading.Tasks;

namespace MangaMesh.Client.Abstractions
{
    public interface INodeConnectionInfoProvider
    {
        Task<(string IP, int Port)> GetConnectionInfoAsync();
    }
}
