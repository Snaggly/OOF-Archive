using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OOF_Packer
{
    public class FileUnpacker
    {
        public FileUnpacker(string filePathtoUnpack) : this(filePathtoUnpack, null, CancellationToken.None) { }
        public FileUnpacker(string filePathtoUnpack, CryptoClass crypto) : this(filePathtoUnpack, crypto, CancellationToken.None) { }
        public FileUnpacker(string filePathtoUnpack, CancellationToken ct) : this(filePathtoUnpack, null, ct) { }

        public FileUnpacker(string filePathtoUnpack, CryptoClass crypto, CancellationToken ct)
        {
            Token = ct;
            filePath = filePathtoUnpack;
            this.crypto = crypto;
            inputStream = new FileStream(filePathtoUnpack, FileMode.Open, FileAccess.Read);
            compressInfo = new CompressionInfo(inputStream, crypto);
            try
            {
                decompStream = new DecompressionStream(inputStream, compressInfo, crypto);
                decompStream.ReadByte();
                decompStream.Dispose();
            }
            catch(System.Security.Cryptography.CryptographicException)
            {
                inputStream.Dispose();
                throw new IncorrectKeyException();
            }

            fileDatas = compressInfo.Files;

            inputStream.Dispose();
        }

        private DecompressionStream decompStream;
        private readonly CryptoClass crypto;
        private readonly string filePath;
        public readonly CompressionInfo compressInfo;
        private Stream inputStream;
        public readonly List<FileData> fileDatas;
        public CancellationToken Token { get; set; }

        public async Task UnpackAsync(string outputPath)
        {
            long totalLength = 0;
            totalLength += (fileDatas[fileDatas.Count - 1].Position + fileDatas[fileDatas.Count - 1].Length);

            await Task.Run(() => Unpack(outputPath, fileDatas, totalLength));
        }

        public async Task UnpackAsync(string outputPath, List<FileData> localFileDatas)
        {
            long totalLength = 0;
            foreach (FileData file in localFileDatas)
                totalLength += file.Length;

            await Task.Run(() => Unpack(outputPath, localFileDatas, totalLength));
        }

        public void Unpack(string outputPath, List<FileData> fileDatas, long totalLength)
        {
            if (EventRaiser.OnFileNameChange == null && EventRaiser.OnProgressChange == null)
            {
                UnpackNoEvent(outputPath, fileDatas);
                return;
            }
            inputStream = new FileStream(filePath, FileMode.Open, FileAccess.Read)
            {
                Position = compressInfo.DataPos
            };
            long writtenLength = 0;
            double tempValue = 0;

            if (crypto != null)
                decompStream = new DecompressionStream(inputStream, compressInfo, crypto);
            else
                decompStream = new DecompressionStream(inputStream, compressInfo);

            foreach (FileData file in fileDatas)
            {
                EventRaiser.OnFileNameChange?.Invoke(file.FileName);
                FileStream outputFileStream = new FileStream(outputPath + "\\" + file.FileName, FileMode.Create, FileAccess.ReadWrite);
                if (decompStream.Position != file.Position)
                    decompStream.Position = file.Position;

                for (long i = 0; i < file.Length; i++)
                {
                    if (Token.IsCancellationRequested)
                    {
                        outputFileStream.Dispose();
                        return;
                    }

                    outputFileStream.WriteByte((byte)decompStream.ReadByte());
                    
                    writtenLength++;

                    if (EventRaiser.OnProgressChange != null)
                    {
                        double value = Math.Round((double)writtenLength / totalLength * 100, 1);

                        if (tempValue != value)
                        {
                            tempValue = value;
                            EventRaiser.OnProgressChange.Invoke(value);
                        }
                    }

                }
                outputFileStream.Dispose();
            }

            decompStream.Dispose();
            inputStream.Dispose();
        }

        public void UnpackNoEvent(string outputPath, List<FileData> fileDatas)
        {
            inputStream = new FileStream(filePath, FileMode.Open, FileAccess.Read)
            {
                Position = compressInfo.DataPos
            };
            long writtenLength = 0;
            if (crypto != null)
                decompStream = new DecompressionStream(inputStream, compressInfo, crypto);
            else
                decompStream = new DecompressionStream(inputStream, compressInfo);

            foreach (FileData file in fileDatas)
            {
                FileStream outputFileStream = new FileStream(outputPath + "\\" + file.FileName, FileMode.Create, FileAccess.ReadWrite);
                if (decompStream.Position != file.Position)
                    decompStream.Position = file.Position;

                for (long i = 0; i < file.Length; i++)
                {
                    if (Token.IsCancellationRequested)
                    {
                        outputFileStream.Dispose();
                        return;
                    }

                    outputFileStream.WriteByte((byte)decompStream.ReadByte());
                    writtenLength++;

                }
                outputFileStream.Dispose();
            }

            decompStream.Dispose();
            inputStream.Dispose();
        }

        public byte[] GetBytes(FileData file)
        {
            MemoryStream memory = new MemoryStream();
            UnpackStream(file).CopyTo(memory);
            return memory.ToArray();
        }

        public Stream UnpackStream(FileData file)
        {
            var inputStream = new FileStream(filePath, FileMode.Open, FileAccess.Read)
            {
                Position = compressInfo.DataPos
            };
            if (crypto != null)
                return new ResolveStream(new DecompressionStream(inputStream, compressInfo, crypto), file.Position, file.Length);
            else
                return new ResolveStream(new DecompressionStream(inputStream, compressInfo), file.Position, file.Length);
        }

        internal class ResolveStream : Stream
        {
            public ResolveStream(DecompressionStream inStream, long Position, long Length)
            {
                this.inStream = inStream;
                fileLength = Length;
                offset = Position;

                inStream.Position = Position;
            }

            #region Locals
            private readonly DecompressionStream inStream;
            private readonly long fileLength;
            private readonly long offset;
            #endregion
            #region StdOverrides
            public override bool CanRead => inStream.CanRead;

            public override bool CanSeek => inStream.CanSeek;

            public override bool CanWrite => false;

            public override long Length => fileLength;

            public override void Flush() { inStream.Flush(); }

            public override long Seek(long offset, SeekOrigin origin) { return inStream.Seek((this.offset + offset), origin); }

            public override void SetLength(long value) { throw new NotSupportedException(); }

            public override void Write(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }
            #endregion
            
            public override long Position
            {
                get
                {
                    return inStream.Position - offset;
                }
                set
                {
                    inStream.Position = value + offset;
                }
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                int i;
                for (i = 0; i < count && Position < Length; i++)
                {
                    buffer[i + offset] = (byte)inStream.ReadByte();
                }
                return i;
            }
        }
    }
}
