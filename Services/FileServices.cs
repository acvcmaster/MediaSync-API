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
        Task<AsyncTimedOperation<string>> GetFileHash(string file);
        Task<AsyncTimedOperation<byte[]>> GetFile(string file);
        Task<AsyncTimedOperation<object[]>> GetFileIndex();
        bool CheckValidPath(string path);
    }

    public class FileService : IFileService
    {
        private SHA256 hashProvider = null;
        protected string _path = string.Empty;
        public string Path => _path;

        public FileService()
        {
            hashProvider = SHA256.Create();
        }
        public void SetPath(string path)
        {
            if(CheckValidPath(path))
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
        public async Task<AsyncTimedOperation<string>> GetFileHash(string file)
        {
            return await AsyncTimedOperation<string>.Start(() => GetFileHashSync(file));
        }

        private string GetFileHashSync(string file)
        {
            if (!File.Exists(file))
                throw new Exception($"No such file '{file}'");

            using (StreamReader fileReader = new StreamReader(file))
            {
                var hash = hashProvider.ComputeHash(fileReader.BaseStream);
                StringBuilder hashBuilder = new StringBuilder();
                foreach (byte hashByte in hash)
                    hashBuilder.Append(hashByte.ToString("X2"));
                return hashBuilder.ToString();
            }
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

        public async Task<AsyncTimedOperation<object[]>> GetFileIndex()
        {
            return await AsyncTimedOperation<object[]>.Start(() => GetFileIndexSync());
        }

        private object[] GetFileIndexSync()
        {
            var allFiles = GetFilesSync();
            var allHashes = allFiles.Select((file) => GetFileHashSync(file)).ToArray();
            var result = new object[allFiles.Length];
            for (int index = 0; index < allFiles.Length; index++)
                result[index] = new { file = allFiles[index], hash = allHashes[index] };
            return result;
        }
    }
}