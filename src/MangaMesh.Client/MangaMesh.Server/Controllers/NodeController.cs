using MangaMesh.Client.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace MangaMesh.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NodeController : ControllerBase
    {
        private readonly INodeIdentityService _nodeIdentity;
        private readonly ITrackerClient _trackerClient;

        public NodeController(
            INodeIdentityService nodeIdentity,
            ITrackerClient trackerClient)
        {
            _nodeIdentity = nodeIdentity;
            _trackerClient = trackerClient;
        }

        [HttpGet("status")]
        public async Task<IResult> GetStatus()
        {
            var isConnected = await _trackerClient.CheckNodeExistsAsync(_nodeIdentity.NodeId);
            _nodeIdentity.UpdateStatus(isConnected);

            return Results.Ok(new
            {
                _nodeIdentity.NodeId,
                IsConnected = isConnected,
                LastPingUtc = _nodeIdentity.LastPingUtc
            });
        }
    }
}
