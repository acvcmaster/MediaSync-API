using System;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Security.Cryptography;
using MediaSync.Shared;

namespace MediaSync.Services
{
    public interface IFileService
    {
        /// <summary>
        /// Path of the library.
        /// </summary>
        /// <value></value>
        string Path { get; }
        void SetPath(string path);
        Task<AsyncTimedOperation<string[]>> GetFiles();
        Task<AsyncTimedOperation<long>> GetFileSize(string file);
        Task<AsyncTimedOperation<byte[]>> GetFile(string file);
        Task<AsyncTimedOperation<FileIndexEntry[]>> GetFileIndex();
        bool CheckValidPath(string path);
    }

    public class FileService : IFileService
    {
        protected string _path = string.Empty;
        public string Path => _path;

        public FileService(string defaultPath)
        {
            SetPath(defaultPath);
        }

        public void SetPath(string path)
        {
            if (CheckValidPath(path))
            {
                _path = path;
                Environment.CurrentDirectory = path;
            }
            else throw new Exception($"'{path}' is not a valid directory.");
        }

        public bool CheckValidPath(string path)
        {
            return Directory.Exists(path);
        }
        public async Task<AsyncTimedOperation<long>> GetFileSize(string file)
        {
            return await AsyncTimedOperation<long>.Start(() => GetFileSizeSync(file));
        }

        private long GetFileSizeSync(string file)
        {
            if (!File.Exists(file))
                throw new Exception($"No such file '{file}'");
            return new FileInfo(file).Length;
        }

        public async Task<AsyncTimedOperation<string[]>> GetFiles()
        {
            return await AsyncTimedOperation<string[]>.Start(() => GetFilesSync());
        }

        private string[] GetFilesSync()
        {
            return Directory.GetFiles(Path).Select((file) =>
                System.IO.Path.GetFileName(file)).ToArray();
        }

        public async Task<AsyncTimedOperation<byte[]>> GetFile(string file)
        {
            return await AsyncTimedOperation<byte[]>.Start(() => GetFileSync(file));
        }

        private static byte[] GetFileSync(string file)
        {
            if (!File.Exists(file))
                throw new Exception($"No such file '{file}'");
            return File.ReadAllBytes(file);
        }

        public async Task<AsyncTimedOperation<FileIndexEntry[]>> GetFileIndex()
        {
            return await AsyncTimedOperation<FileIndexEntry[]>.Start(() => GetFileIndexSync());
        }

        private FileIndexEntry[] GetFileIndexSync()
        {
            var allFiles = GetFilesSync();
            var allSizes = allFiles.Select((file) => GetFileSizeSync(file)).ToArray();
            var result = new FileIndexEntry[allFiles.Length];
            for (int index = 0; index < allFiles.Length; index++)
                result[index] = new FileIndexEntry { name = allFiles[index], size = allSizes[index] };
            return result;
        }
    }
}