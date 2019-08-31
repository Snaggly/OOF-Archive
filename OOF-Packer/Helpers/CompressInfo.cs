using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace OOF_Packer
{
    public class CompressionInfo
    {
        CancellationToken ct;
        public bool IsEncrypted { get; } = false;

        public CompressionInfo(Stream file, CryptoClass crypto, CancellationToken ct)
        {
            this.ct = ct;

            byte[] headBytes = new byte[Header.HeadBegin.Length];
            file.Position = 0;
            file.Read(headBytes, 0, headBytes.Length);

            if (crypto != null)
            {
                if (!headBytes.SequenceEqual(Header.HeadEncBegin))
                    throw new NotAnOOFPackException();
            }
            else
            {
                if (headBytes.SequenceEqual(Header.HeadEncBegin))
                    throw new EncryptedException();
                else if (!headBytes.SequenceEqual(Header.HeadBegin))
                    throw new NotAnOOFPackException();
            }

            HeadBytes = ResolveHead(file, out int offset);
            Diff = ResolveDiff(HeadBytes);
            BufferSize = ResolveBufferSize(HeadBytes);
            Files = ResolveFiles(HeadBytes);

            DataPos = Header.HeadBegin.Length + sizeof(int) + offset;

            PacketAmount = Diff.Count;

            foreach (uint i in Diff)
                TotalCompressRatio += ((BufferSize - i) / (BufferSize * (double)PacketAmount));

            foreach (FileData f in Files)
                TotalUncompressedSize += f.Length;
        }

        private List<FileData> ResolveFiles(byte[] HeadBytes)
        {
            int indexCounter = 0;
            List<FileData> resolvingData = new List<FileData>();
            long position = 0;
            for (int i = sizeof(int); i < TailOffset - Header.HeadEnd.Length; i++)
            {
                long length = BitConverter.ToInt64(HeadBytes, i);
                i += sizeof(long);
                string fileName = "";
                while (HeadBytes[i] != Header.Seperator)
                {
                    fileName += (char)HeadBytes[i];
                    i++;
                }
                resolvingData.Add(new FileData(fileName, length, position, indexCounter));
                position += length;
                indexCounter++;
            }

            return resolvingData;
        }

        private long ResolveDataPos(Stream stream, int offset)
        {
            stream.Position = Header.HeadBegin.Length + offset;
            byte[] buffer = new byte[Header.GZipObfHeader.Length];
            stream.Read(buffer, 0, buffer.Length);
            if (buffer.SequenceEqual(Header.GZipObfHeader))
                return offset;
            else
                return ResolveDataPos(stream, ++offset);
        }

        private uint ResolveBufferSize(byte[] HeadBytes)
        {
            return BitConverter.ToUInt32(HeadBytes, 0);
        }

        private byte[] ResolveHead(Stream stream, out int offset)
        {
            byte[] buffer = new byte[4];
            stream.Read(buffer, 0, buffer.Length);
            offset = BitConverter.ToInt32(buffer, 0);
            byte[] headBytes = new byte[offset];
            stream.Read(headBytes, 0, headBytes.Length);
            headBytes = GZipProvider.Decompress(headBytes);
            return headBytes;
        }

        int Search(byte[] src, byte[] pattern)
        {
            int c = src.Length - pattern.Length;
            if (c < 0)
                return -1;
            int j;
            for (int i = c; i>0; i--)
            {
                if (src[i] != pattern[0]) continue;
                for (j = pattern.Length - 1; j >= 1 && src[i + j] == pattern[j]; j--) ;
                if (j == 0) return i;
            }
            return -1;
        }

        private List<int> ResolveDiff(byte[] HeadBytes)
        {
            TailOffset = Search(HeadBytes, Header.HeadEnd);
            TailOffset += Header.HeadEnd.Length;
            List<int> Diff = new List<int>();
            for (int i = TailOffset; i < HeadBytes.Length; i += sizeof(int))
                Diff.Add(BitConverter.ToInt32(HeadBytes, i));
            
            return Diff;
        }

        private int TailOffset;

        public List<FileData> Files { get; }
        public List<int> Diff { get; }
        public int PacketAmount { get; }
        public uint BufferSize { get; }
        public double TotalCompressRatio { get; }
        public long DataPos { get; }
        public byte[] HeadBytes { get; }
        public long TotalUncompressedSize { get; }
    }
}
