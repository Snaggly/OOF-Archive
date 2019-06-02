using System;
using System.IO;
using System.Threading;

namespace OOF_Packer
{
    class DecompressionStream : Stream
    {
        public DecompressionStream(Stream toDecompressStream) : this(toDecompressStream, null, null)
        {
            ci = new CompressionInfo(toDecompressStream, null, CancellationToken.None);
        }

        public DecompressionStream(Stream toDecompressStream, CryptoClass crypto) : this(toDecompressStream, null, crypto)
        {
            ci = new CompressionInfo(toDecompressStream, null, CancellationToken.None);
        }

        public DecompressionStream(Stream toDecompressStream, CompressionInfo compressionInfo) : this(toDecompressStream, compressionInfo, null) { }

        public DecompressionStream(Stream toDecompressStream, CompressionInfo compressionInfo, CryptoClass crypto)
        {
            this.crypto = crypto;
            inputStream = toDecompressStream;
            ci = compressionInfo;
            Position = 0;
        }

        private readonly Stream inputStream;
        public readonly CryptoClass crypto;
        public readonly CompressionInfo ci;

        #region Standard override
        public override bool CanRead => inputStream.CanRead;

        public override bool CanSeek => inputStream.CanSeek;

        public override bool CanWrite => false;

        public override long Length => ci.TotalUncompressedSize;

        public override void Flush() { inputStream.Flush(); }

        public override long Seek(long offset, SeekOrigin origin) { return inputStream.Seek(offset, origin); }

        public override void SetLength(long value) { inputStream.SetLength(value); }

        public override void Write(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }
        #endregion

        private long CompressedPositionLookup(int atPacket)
        {
            long bufferMultiplex = ci.BufferSize * atPacket;
            long diffOffsets = 0;
            for (int i = 0; i < atPacket; i++)
                diffOffsets += ci.Diff[i];

            return bufferMultiplex - diffOffsets;
        }

        private long position;
        private long localPosition;
        private int previousPacket = -1;
        byte[] compressedBuffer = new byte[0];
        byte[] decompressedbuffer = new byte[0];
        int currentPacket = -1;
        public override long Position
        {
            get
            {
                return position;
            }
            set
            {
                currentPacket = (int)(value / ci.BufferSize);

                if (previousPacket != currentPacket)
                {
                    previousPacket = currentPacket;
                    compressedBuffer = new byte[ci.BufferSize - ci.Diff[currentPacket]];
                    inputStream.Position = CompressedPositionLookup(currentPacket) + ci.DataPos;

                    decompressedbuffer = GZipProvider.Decompress(ReadFromInputStream(inputStream, compressedBuffer));
                    
                }

                localPosition = value - (ci.BufferSize * currentPacket);
                position = value;
            }
        }
        public override int ReadByte()
        {
            if (localPosition >= decompressedbuffer.LongLength)
                return -1;

            int result = decompressedbuffer[localPosition];
            Position++;
            return result;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int i;
            for (i = 0; i < count && Position < Length; i++)
            {
                buffer[i+offset] = (byte)ReadByte();
            }
            
            return i;
        }

        private byte[] ReadFromInputStream(Stream inputStream, byte[] buffer)
        {
            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = (byte)(inputStream.ReadByte()+128);

            if (crypto != null)
                buffer = crypto.DecryptBytes(buffer);

            return buffer;
        }
    }
}