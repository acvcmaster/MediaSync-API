using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MediaSync.Services;
using Microsoft.AspNetCore.StaticFiles;
using MediaSync.Shared;

namespace MediaSync.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class FileController : ControllerBase
    {
        public IFileService FileService { get; }
        public FileController(IFileService fileService)
        {
            FileService = fileService;
        }

        [HttpPatch]
        public IActionResult SetPath(string path)
        {
            try
            {
                FileService.SetPath(path);
                return Ok();
            }
            catch (Exception error) { return BadRequest(error.Message); }
        }

        [HttpGet]
        public async Task<IActionResult> GetFiles([FromQuery] string[] extensions = null)
        {
            var result = await FileService.GetFiles();
            if (result.Failed)
                return BadRequest(result);

            result.Result = (from file in result.Result
                             where file.EndsWithAny(extensions)
                             select file)
                                .ToArray();
            return Ok(result);
        }

        [HttpGet]
        [Produces("application/octet-stream", "application/json")]
        public async Task<IActionResult> GetFile([FromQuery] string file)
        {
            var contentProvider = new FileExtensionContentTypeProvider();
            var result = await FileService.GetFile(file);
            if (result.Failed)
                return BadRequest(result);

            string contentType = default;
            if (!contentProvider.TryGetContentType(file, out contentType))
            {
                result.Error = $"Content type not found for '{file}.'";
                return BadRequest(result);
            }
            
            return File(result.Result, contentType);
        }

        [HttpGet]
        public async Task<IActionResult> GetFileSize([FromQuery] string file)
        {
            var result = await FileService.GetFileSize(file);
            if (result.Failed)
                return BadRequest(result);
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetFileIndex([FromQuery] string[] extensions = null)
        {
            var result = await FileService.GetFileIndex();
            if (result.Failed)
                return BadRequest(result);

            result.Result = (from file in result.Result
                             where file.name.EndsWithAny(extensions)
                             select file)
                .ToArray();
            return Ok(result);
        }
    }
}
