using System;
using System.IO;
using System.IO.Compression;

namespace OOF_Packer
{
    class GZipProvider
    {
        public static Stream Decompressor(Stream inputStream)
        {
            return new GZipStream(inputStream, CompressionMode.Decompress);
        }

        public static Stream Compressor(Stream inputStream)
        {
            return new GZipStream(inputStream, CompressionMode.Compress);
        }

        public static byte[] Compress(byte[] data)
        {
            using (var compressedStream = new MemoryStream())
            using (var zipStream = Compressor(compressedStream))
            {
                zipStream.Write(data, 0, data.Length);
                zipStream.Close();

                byte[] fullResult = compressedStream.ToArray();
                byte[] shortResult = new byte[fullResult.Length - 8];
                Array.Copy(fullResult, shortResult, shortResult.Length);
                return shortResult;
            }
        }

        public static byte[] Decompress(byte[] data)
        {
            using (var compressedStream = new MemoryStream(data))
            using (var zipStream = Decompressor(compressedStream))
            using (var resultStream = new MemoryStream())
            {
                zipStream.CopyTo(resultStream);
                return resultStream.ToArray();
            }
        }
    }
}
