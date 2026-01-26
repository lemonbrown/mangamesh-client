using MangaMesh.Server.Models;
using MangaMesh.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace MangaMesh.Server.Controllers
{
    [ApiController]
    [Route("api/import")]
    public class ImportController : ControllerBase
    {
        private readonly IImportChapterService _importer;

        public ImportController(IImportChapterService importer)
        {
            _importer = importer;
        }

        [HttpPost("chapter")]
        public async Task<ActionResult<ImportResultDto>> ImportChapter(
            [FromBody] ImportChapterRequestDto request)
        {
            var result = await _importer.ImportAsync(request);
            return Ok(result);
        }
    }

}
