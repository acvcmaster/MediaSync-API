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
        Task<AsyncTimedOperationResult<FileResult>> GetFile(string file, bool raw);
        Task<AsyncTimedOperationResult<FileResult>> GetFileTranscoded(string file, QualityPreset quality, bool changeContainersOnly, bool hardwareAcceleration);
        Task<AsyncTimedOperationResult<object>> GetDetails(string file);
        Task<AsyncTimedOperationResult<Stream>> GetThumbnail(string name, ThumbnailResolution? resolution);
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

        public async Task<AsyncTimedOperationResult<FileResult>> GetFile(string file, bool raw)
        {
            return await AsyncTimedOperationResult<FileResult>.GetResultFromSync(() => GetFileSync(file, raw));
        }

        private static FileResult GetFileSync(string file, bool raw)
        {
            if (!File.Exists(file))
                throw new Exception($"No such file '{file}'");

            FileResult result = new FileResult();
            string contentType = raw ? "application/octet-stream" : null;

            if (!raw)
            {
                if (!new FileExtensionContentTypeProvider().TryGetContentType(file, out contentType))
                    throw new Exception($"Content type not found for '{file}.'");
            }

            StreamReader fileReader = new StreamReader(file);

            result.ContentType = contentType;
            result.Data = fileReader.BaseStream;
            return result;
        }

        public async Task<AsyncTimedOperationResult<FileResult>> GetFileTranscoded(string file, QualityPreset quality, bool changeContainersOnly, bool hardwareAcceleration)
        {
            return await AsyncTimedOperationResult<FileResult>.GetResultFromSync(() => GetFileTranscodedSync(file, quality, changeContainersOnly, hardwareAcceleration));
        }

        private FileResult GetFileTranscodedSync(string file, QualityPreset quality, bool changeContainersOnly, bool hardwareAcceleration)
        {
            if (!File.Exists(file))
                throw new Exception($"No such file '{file}'");

            var ffmpeg = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = GetFfmpegParameters(file, changeContainersOnly, quality, hardwareAcceleration),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            ffmpeg.Start();
            return new FileResult
            {
                Data = ffmpeg.StandardOutput.BaseStream,
                ContentType = "video/mp4"
            };
        }

        private string GetFfmpegParameters(string file, bool changeContainersOnly, QualityPreset quality, bool hardwareAcceleration)
        {
            string crf = string.Empty;
            string scale = string.Empty;
            string bitrate = string.Empty;

            switch (quality)
            {
                case QualityPreset.Low:
                    crf = "-crf 35";
                    bitrate = "-b:v 4000k";
                    scale = "-vf scale=640:480";
                    break;
                case QualityPreset.Medium:
                    crf = "-crf 28";
                    bitrate = "-b:v 7500k";
                    scale = "-vf scale=1280:720";
                    break;
                default:
                    crf = "-crf 23";
                    bitrate = "-b:v 12000k";
                    break;
            }


#if ARM // On Raspberry Pi, change containers only (when hardware acceleration is disabled)
            return !hardwareAcceleration ? $"-i \"{file}\" -hide_banner -loglevel panic -v quiet -c:v copy -c:a copy -movflags frag_keyframe -strict -2 -f mp4  -" :
                $"-i \"{file}\" -hide_banner -loglevel panic -v quiet -c:v h264_omx {bitrate} -c:a aac -movflags frag_keyframe+empty_moov -f mp4  -";
#else
            if (!hardwareAcceleration)
            {
                return !changeContainersOnly ? $"-i \"{file}\" -hide_banner -loglevel panic -v quiet -c:v libx264 {crf} -preset veryfast -c:a aac -movflags frag_keyframe+empty_moov {scale} -f mp4  -"
                                        : $"-i \"{file}\" -hide_banner -loglevel panic -v quiet -c:v copy -c:a copy -movflags frag_keyframe -strict -2 -f mp4  -";
            }
            else return $"-hwaccel nvdec -i \"{file}\" -hide_banner -loglevel panic -v quiet -c:v h264_nvenc {bitrate} -preset medium -c:a aac -movflags frag_keyframe+empty_moov {scale} -f mp4  -";
#endif
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

        public async Task<AsyncTimedOperationResult<Stream>> GetThumbnail(string name, ThumbnailResolution? resolution)
        {
            return await AsyncTimedOperationResult<Stream>.GetResultFromSync(() => GetThumbnailSync(name, resolution));
        }

        private Stream GetThumbnailSync(string file, ThumbnailResolution? resolution)
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
            return ffmpeg.StandardOutput.BaseStream;
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