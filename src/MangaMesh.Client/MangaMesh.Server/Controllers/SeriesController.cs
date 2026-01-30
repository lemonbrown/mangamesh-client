using MangaMesh.Client.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MangaMesh.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SeriesController : ControllerBase
    {
        public SeriesController()
        {
            
        }

        [HttpGet("{seriesId}/chapter/{chapterId}/read")]
        public async Task<IResult> ReadChapter()
        {
            //ask the central api for the peer node
            //mock the node for now
            var peer = new PeerInfo("node-1", "https://localhost:7124", 1111, DateTime.Now);

            var httpClient = new HttpClient()
            {
                BaseAddress = new Uri(peer.IP + ":" + peer.Port),
            };

            var chapterId = "";

            //request the peer manifest for this chapter
            var manifestResponse = await httpClient.GetAsync($"chapters/{chapterId}/manifest");

            manifestResponse.Content.ReadFromJsonAsync<Manifest>

            return Results.Ok();
        }
    }
}
