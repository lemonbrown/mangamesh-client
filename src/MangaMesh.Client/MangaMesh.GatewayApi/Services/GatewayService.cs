using MangaMesh.Client.Content;
using MangaMesh.Client.Node;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;
using System.Text;

namespace MangaMesh.GatewayApi.Services;

public class GatewayService
{
    private readonly IDhtNode _dhtNode;
    private readonly IMemoryCache _cache;
    private readonly ILogger<GatewayService> _logger;

    public GatewayService(IDhtNode dhtNode, IMemoryCache cache, ILogger<GatewayService> logger)
    {
        _dhtNode = dhtNode;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ManifestData?> GetManifestAsync(string contentHash)
    {
        // 1. Check Cache
        if (_cache.TryGetValue($"manifest:{contentHash}", out ManifestData? cached))
        {
            return cached;
        }

        var hashBytes = Encoding.UTF8.GetBytes(contentHash);

        // 2. DHT Lookup for providers
        var providers = await _dhtNode.FindValueWithAddressAsync(hashBytes);
        
        foreach (var provider in providers)
        {
            try
            {
                // 3. Request Manifest
                var request = new GetManifest { ContentHash = contentHash };
                // Timeout of 5 seconds for gateway responsiveness
                var response = await _dhtNode.SendContentRequestAsync(provider.Address, request, TimeSpan.FromSeconds(5));

                if (response is ManifestData data)
                {
                    // 4. Cache and Return
                    _cache.Set($"manifest:{contentHash}", data, TimeSpan.FromMinutes(30)); 
                    return data;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to fetch manifest from {provider.Address.Host}:{provider.Address.Port}: {ex.Message}");
            }
        }

        return null;
    }

    public async Task<byte[]?> GetPageAsync(string forceChapterId, int pageNumber)
    {
        // Placeholder for blob retrieval logic similar to manifest
        // Would need GetBlob message + BlobData response type
        return null;
    }
}
