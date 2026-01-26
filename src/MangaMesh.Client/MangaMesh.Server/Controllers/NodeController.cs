using MangaMesh.Server.Models;
using MangaMesh.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace MangaMesh.Server.Controllers
{
    [ApiController]
    [Route("api/node")]
    public class NodeController : ControllerBase
    {
        private readonly INodeStatusService _status;

        public NodeController(INodeStatusService status)
        {
            _status = status;
        }

        [HttpGet("status")]
        public async Task<ActionResult<NodeStatusDto>> GetStatus()
        {
            var status = await _status.GetStatusAsync();
            return Ok(status);
        }
    }

}
