using MangaMesh.Server.Models;
using MangaMesh.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace MangaMesh.Server.Controllers
{
    [ApiController]
    [Route("api/storage")]
    public class StorageController : ControllerBase
    {
        private readonly IStorageService _storage;

        public StorageController(IStorageService storage)
        {
            _storage = storage;
        }

        [HttpGet]
        public async Task<ActionResult<StorageDto>> Get()
            => Ok(await _storage.GetStatsAsync());
    }

}
