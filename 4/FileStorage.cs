using System;
using System.IO;

namespace CommandLineCalculator
{
    public sealed class FileStorage : Storage
    {
        private readonly string path;

        public FileStorage(string path)
        {
            this.path = path;
        }

        private string TemporaryPath => $"{path}_tmp";

        public override byte[] Read()
        {
            if (File.Exists(path))
            {
                return ReadContent();
            }

            if (File.Exists(TemporaryPath))
            {
                Replace();
                return ReadContent();
            }

            return Array.Empty<byte>();
        }

        private byte[] ReadContent()
        {
            using (var file = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read))
            using (var buffer = new MemoryStream())
            {
                file.CopyTo(buffer);
                return buffer.ToArray();
            }
        }

        public override void Write(byte[] content)
        {
            using (var file = new FileStream(TemporaryPath, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                file.Write(content, 0, content.Length);
            }

            File.Delete(path);
            Replace();
        }

        private void Replace()
        {
            File.Move(TemporaryPath, path);
        }
    }
}