using MangaMesh.Client.Models;
using MangaMesh.Server.Models;
using MangaMesh.Server.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace MangaMesh.Server.Controllers
{
    [ApiController]
    [Route("api/import")]
    public class ImportController : ControllerBase
    {
        private readonly IImportChapterService _importer;
        private readonly string _inputDirectory = Path.Combine(AppContext.BaseDirectory, "input");
        private readonly string _importedChaptersFile;

        public ImportController(IImportChapterService importer)
        {
            _importer = importer;
            _importedChaptersFile = Path.Combine(_inputDirectory, "imported_chapters.json");
        }

        [HttpPost("chapter")]
        public async Task<ActionResult<ImportResultDto>> ImportChapter(
            [FromBody] ImportChapterRequestDto request)
        { 

            var result = await _importer.ImportAsync(request);

            if (!Directory.Exists(_inputDirectory))
            {
                Directory.CreateDirectory(_inputDirectory);
            }

            var importedChapters = new List<ImportChapterRequestDto>();
            if (System.IO.File.Exists(_importedChaptersFile))
            {
                var json = await System.IO.File.ReadAllTextAsync(_importedChaptersFile);
                importedChapters = JsonSerializer.Deserialize<List<ImportChapterRequestDto>>(json);
            }

            importedChapters.Add(request);
            var newJson = JsonSerializer.Serialize(importedChapters, new JsonSerializerOptions { WriteIndented = true });
            await System.IO.File.WriteAllTextAsync(_importedChaptersFile, newJson);

            return Ok(result);
        }

        [HttpGet("chapters")]
        public async Task<ActionResult<IEnumerable<ImportChapterRequestDto>>> GetImportedChapters()
        {
            if (!System.IO.File.Exists(_importedChaptersFile))
            {
                return Ok(new List<ImportChapterRequestDto>());
            }

            var json = await System.IO.File.ReadAllTextAsync(_importedChaptersFile);
            var chapters = JsonSerializer.Deserialize<IEnumerable<ImportChapterRequestDto>>(json);

            return Ok(chapters);
        }
    }
}
