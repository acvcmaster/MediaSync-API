using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MediaSync.Services;
using Microsoft.AspNetCore.StaticFiles;

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
            catch(Exception error) { return BadRequest(error.Message); }
        }
        
        [HttpGet]
        public async Task<IActionResult> GetFiles()
        {
            try
            {
                var result = await FileService.GetFiles();
                return Ok(result);
            }
            catch(Exception error) { return BadRequest(error.Message); }
        }

        [HttpGet]
        [Produces("application/octet-stream")]
        public async Task<IActionResult> GetFile([FromQuery] string file)
        {
            try
            {
                var contentProvider = new FileExtensionContentTypeProvider();
                var result = await FileService.GetFile(file);
                string contentType = default;
                if(!contentProvider.TryGetContentType(file, out contentType))
                    throw new Exception($"Content type not found for '{file}.'");
                return File(result.Result, contentType);
            }
            catch(Exception error) { return BadRequest(error.Message); }
        }

        [HttpGet]
        public async Task<IActionResult> GetFileHash([FromQuery] string file)
        {
            try
            {
                var result = await FileService.GetFileHash(file);
                return Ok(result);
            }
            catch(Exception error) { return BadRequest(error.Message); }
        }
    }
}
