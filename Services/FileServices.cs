using System;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using MediaSync.Shared;
using MediaSync.Types;
using Microsoft.AspNetCore.StaticFiles;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace MediaSync.Services
{
    public interface IFileService
    {
        /// <summary>
        /// Path of the library.
        /// </summary>
        /// <value></value>
        string Path { get; }
        void SetPath(string path, bool aggressivePaths = true);
        Task<AsyncTimedOperationResult<string[]>> GetFileNames(string[] extensions = null);
        Task<AsyncTimedOperationResult<FileResult>> GetFile(string file);
        Task<AsyncTimedOperationResult<object>> GetDetails(string file);
        Task<AsyncTimedOperationResult<byte[]>> GetThumbnail(string name, ThumbnailResolution? resolution);
        Task<AsyncTimedOperationResult<object>> SaveFile(IFormFile file);
        Task<AsyncTimedOperationResult<object>> DeleteFile(string file);
        Task<AsyncTimedOperationResult<string[]>> GetMetadata(string file);
    }

    public class FileService : IFileService
    {
        protected string _path = string.Empty;
        public string Path => _path;
        protected Random rng = new Random();

        public FileService(string defaultPath, bool aggressivePaths = true)
        {
            SetPath(defaultPath, aggressivePaths);
        }

        public void SetPath(string path, bool aggressivePaths = true)
        {
            if (path == string.Empty || path == null)
                return;

            if (path.StartsWith("~"))
                path = Environment.CurrentDirectory + path.Substring(1);

            if (!Directory.Exists(path))
            {
                if (aggressivePaths)
                    Directory.CreateDirectory(path);
                else throw new Exception($"Path '{path}' not found!");
            }

            Environment.CurrentDirectory = path;
            _path = Environment.CurrentDirectory;
        }

        public async Task<AsyncTimedOperationResult<string[]>> GetFileNames(string[] extensions = null)
        {
            return await AsyncTimedOperationResult<string[]>.GetResultFromSync(() => GetFileNamesSync(extensions));
        }

        private string[] GetFileNamesSync(string[] extensions = null)
        {
            var result = from file in Directory.GetFiles(Path)
                         where file.EndsWithAny(extensions)
                         select System.IO.Path.GetFileName(file);

            return result.ToArray();
        }

        public async Task<AsyncTimedOperationResult<FileResult>> GetFile(string file)
        {
            return await AsyncTimedOperationResult<FileResult>.GetResultFromSync(() => GetFileSync(file));
        }

        private static FileResult GetFileSync(string file)
        {
            if (!File.Exists(file))
                throw new Exception($"No such file '{file}'");

            FileResult result = new FileResult();
            string contentType;
            if (!new FileExtensionContentTypeProvider().TryGetContentType(file, out contentType))
                throw new Exception($"Content type not found for '{file}.'");
            result.ContentType = contentType;
            result.Data = File.ReadAllBytes(file);
            return result;
        }

        public async Task<AsyncTimedOperationResult<object>> GetDetails(string file)
        {
            return await AsyncTimedOperationResult<object>.GetResultFromSync(() => GetDetailsSync(file));
        }

        private object GetDetailsSync(string file)
        {
            if (!File.Exists(file))
                throw new Exception($"No such file '{file}'");

            FileInfo fileInfo = new FileInfo(file);
            return new
            {
                fileInfo.CreationTime,
                fileInfo.LastAccessTime,
                fileInfo.LastWriteTime,
                fileInfo.Length,
                fileInfo.Extension
            };
        }

        public async Task<AsyncTimedOperationResult<byte[]>> GetThumbnail(string name, ThumbnailResolution? resolution)
        {
            return await AsyncTimedOperationResult<byte[]>.GetResultFromSync(() => GetThumbnailSync(name, resolution));
        }

        private byte[] GetThumbnailSync(string file, ThumbnailResolution? resolution)
        {
            if (!File.Exists(file))
                throw new Exception($"No such file '{file}'");

            int X = 320, Y = 180;
            if (resolution.HasValue)
            {
                switch (resolution.Value)
                {
                    case ThumbnailResolution.Medium:
                        X = 854;
                        Y = 480;
                        break;
                    case ThumbnailResolution.Large:
                        X = 1920;
                        Y = 1080;
                        break;
                    default:
                        break;
                }
            }

            var ffmpeg = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $"-ss 00:01:30 -i \"{file}\" -hide_banner -loglevel panic -v quiet -f image2 -vframes 1 -vf scale={X}:{Y} -",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            ffmpeg.Start();
            byte[] result = ffmpeg.StandardOutput.ReadBytesToEnd();
            ffmpeg.WaitForExit();
            return result;
        }

        public async Task<AsyncTimedOperationResult<object>> SaveFile(IFormFile file)
        {
            return await AsyncTimedOperationResult<object>.GetResultFromSync(() => SaveFileSync(file));
        }

        private object SaveFileSync(IFormFile file)
        {
            if (File.Exists(file.FileName))
                throw new Exception($"File '{file.FileName}' exists already.");

            using (FileStream fileStream = File.Create(file.FileName))
            {
                var uploadStream = file.OpenReadStream();
                uploadStream.CopyTo(fileStream);
                fileStream.Close();
            }
            return new { SavedFile = file.FileName, TimeStamp = DateTime.Now };
        }

        public async Task<AsyncTimedOperationResult<string[]>> GetMetadata(string file)
        {
            return await AsyncTimedOperationResult<string[]>.GetResultFromSync(() => GetMetadataSync(file));
        }

        private string[] GetMetadataSync(string file)
        {
            List<string> result = new List<string>();
            var ffprobe = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffprobe",
                    Arguments = $"\"{file}\" -hide_banner",
                    UseShellExecute = false,
                    RedirectStandardError = true
                }
            };

            if (!ffprobe.Start())
                throw new Exception("Failed to start ffprobe.");

            var outputStream = ffprobe.StandardError;
            outputStream.ReadLine();
            while (!outputStream.EndOfStream)
                result.Add(outputStream.ReadLine());
            ffprobe.WaitForExit();
            return result.ToArray();
        }

        public async Task<AsyncTimedOperationResult<object>> DeleteFile(string file)
        {
            return await AsyncTimedOperationResult<object>.GetResultFromSync(() => DeleteFileSync(file));
        }

        private object DeleteFileSync(string file)
        {
            if (!File.Exists(file))
                throw new Exception($"No such file '{file}'.");

            File.Delete(file);
            return new { DeletedFile = file, TimeStamp = DateTime.Now };
        }
    }
}