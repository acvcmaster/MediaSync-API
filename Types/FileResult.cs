using System.IO;

namespace MediaSync.Types
{
    public class FileResult
    {
        public Stream Data { get; set; }
        public string ContentType { get; set; }
    }
}