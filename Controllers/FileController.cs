using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MediaSync.Services;
using MediaSync.Types;
using Microsoft.AspNetCore.Http;

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

        [HttpGet]
        public IActionResult GetPath()
        {
            try
            {
                return Ok(FileService.Path);
            }
            catch (Exception error) { return BadRequest(error.Message); }
        }

#if DEBUG
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
#endif

        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> SaveFile(IFormFile file)
        {
            var result = await FileService.SaveFile(file);
            if (result.Failed)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteFile([FromQuery] string file)
        {
            var result = await FileService.DeleteFile(file);
            if (result.Failed)
                return BadRequest(result);
                
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetFileNames([FromQuery] string[] extensions = null)
        {
            var result = await FileService.GetFileNames(extensions);
            if (result.Failed)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetDetails(string file)
        {
            var result = await FileService.GetDetails(file);
            if (result.Failed)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet]
        [Produces("application/octet-stream", "application/json")]
        public async Task<IActionResult> GetFile([FromQuery] string file)
        {
            var result = await FileService.GetFile(file);
            if (result.Failed)
                return BadRequest(result);
            
            return File(result.Result.Data, result.Result.ContentType);
        }

        [HttpGet]
        [Produces("video/mp4", "application/json")]
        public async Task<IActionResult> GetFileTranscoded([FromQuery] string file)
        {
            var result = await FileService.GetFileTranscoded(file);
            if (result.Failed)
                return BadRequest(result);
            
            return File(result.Result.Data, result.Result.ContentType);
        }

        [HttpGet]
        [Produces("image/jpeg", "application/json")]
        public async Task<IActionResult> GetThumbnail([FromQuery] string file, [FromQuery] ThumbnailResolution? resolution = null)
        {
            var result = await FileService.GetThumbnail(file, resolution);
            if (result.Failed)
                return BadRequest(result);

            return File(result.Result, "image/jpeg");
        }

        [HttpGet]
        public async Task<IActionResult> GetMetadata([FromQuery] string file)
        {
            var result = await FileService.GetMetadata(file);
            if (result.Failed)
                return BadRequest(result);

            return Ok(result);
        }
    }
}
