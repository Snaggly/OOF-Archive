using System.IO;

namespace OOF_Packer
{
    class DataStreamResolver : Stream
    {
        public DataStreamResolver(Stream stream)
        {
            this.inStream = stream;
        }
        private Stream inStream;

        public override int Read(byte[] array, int offset, int count)
        {
            int i = -1;
            if (Position >= Length)
                return i;

            for (i = 0; i < count && Position < Length; i++)
            {
                array[i + offset] = (byte)ReadByte();
            }

            return i;
        }

        public override int ReadByte()
        {
            if (Position >= Length)
                return -1;

            return (inStream.ReadByte() + 128);
        }

        public override long Position
        {
            get
            {
                return inStream.Position;
            }
            set
            {
                inStream.Position = value;
            }
        }
        public override long Length => inStream.Length;

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override void Flush()
        {
            inStream.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return inStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            inStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new System.NotSupportedException();
        }
    }
}