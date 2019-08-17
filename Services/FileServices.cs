using System;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using MediaSync.Shared;
using MediaSync.Types;
using Microsoft.AspNetCore.StaticFiles;
using System.Collections.Generic;

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
        Task<AsyncTimedOperationResult<string[]>> GetFileNames(string[] extensions = null);
        Task<AsyncTimedOperationResult<long>> GetFileSize(string file);
        Task<AsyncTimedOperationResult<FileResult>> GetFile(string file);
        Task<AsyncTimedOperationResult<FileIndexEntry[]>> GetFileIndex(string[] extensions = null);
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
        public async Task<AsyncTimedOperationResult<long>> GetFileSize(string file)
        {
            return await AsyncTimedOperationResult<long>.GetResult(() => GetFileSizeSync(file));
        }

        private long GetFileSizeSync(string file)
        {
            if (!File.Exists(file))
                throw new Exception($"No such file '{file}'");
            return new FileInfo(file).Length;
        }

        public async Task<AsyncTimedOperationResult<string[]>> GetFileNames(string[] extensions = null)
        {
            return await AsyncTimedOperationResult<string[]>.GetResult(() => GetFileNamesSync(extensions));
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
            return await AsyncTimedOperationResult<FileResult>.GetResult(() => GetFileSync(file));
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

        public async Task<AsyncTimedOperationResult<FileIndexEntry[]>> GetFileIndex(string[] extensions = null)
        {
            return await AsyncTimedOperationResult<FileIndexEntry[]>.GetResult(() => GetFileIndexSync(extensions));
        }

        private FileIndexEntry[] GetFileIndexSync(string[] extensions = null)
        {
            List<FileIndexEntry> allEntries = new List<FileIndexEntry>();
            foreach(string file in GetFileNamesSync())
                allEntries.Add(new FileIndexEntry { name = file, size = GetFileSizeSync(file) } );

            var result = from entry in allEntries
                where entry.name.EndsWithAny(extensions)
                select entry;

            return result.ToArray();
        }
    }
}