using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Text;
using System.IO;
using RijndaelCryptography;

namespace OOF_Packer
{
    public class FileBuilder
    {
        private readonly uint BufferSize;
        public FileBuilder(string[] inputFiles) : this(inputFiles, 1048576, null) { }
        public FileBuilder(string[] inputFiles, uint BufferSize) : this(inputFiles, BufferSize, null) { }
        public FileBuilder(string[] inputFiles, CryptoClass crypto) : this(inputFiles, 1048576, crypto) { }
        public FileBuilder(string[] inputFiles, uint BufferSize, CryptoClass crypto)
        {
            this.inputFiles = inputFiles;
            this.BufferSize = BufferSize;
            this.crypto = crypto;
        }

        private readonly string[] inputFiles;
        private Stream fileStream;
        private readonly CryptoClass crypto;

        public async Task BuildFile(Stream outputStream, CancellationToken token)
        {
            if (EventRaiser.OnFileNameChange==null && EventRaiser.OnProgressChange == null)
            {
                await BuildFileNoEvent(outputStream, token);
                return;
            }
            await Task.Run(() =>
            {
                if (crypto!=null)
                    outputStream.Write(Header.HeadEncBegin, 0, Header.HeadEncBegin.Length);
                else
                    outputStream.Write(Header.HeadBegin, 0, Header.HeadBegin.Length);

                List<byte> headerBytes = NewHeaderBytes();
                List<byte> tailBytes = new List<byte>();
                
                long totalLength = TotalLength();
                double tempValue = 0;
                
                byte[] buffer = new byte[(int)BufferSize];
                byte[] compressedBuffer;
                int counter = 0;
                long processedBytes = 0;
                string tempFilePath = Path.Combine(Path.GetTempPath(), new Random().Next(1000, 10000) + ".dat");
                FileStream tempFile = new FileStream(tempFilePath, FileMode.CreateNew, FileAccess.ReadWrite);

                foreach (string curFile in inputFiles)
                {
                    EventRaiser.OnFileNameChange?.Invoke(curFile);
                    fileStream = new FileStream(curFile, FileMode.Open, FileAccess.Read);

                    headerBytes.AddRange(BitConverter.GetBytes(fileStream.Length));
                    headerBytes.AddRange(Encoding.ASCII.GetBytes(Path.GetFileName(curFile)));
                    headerBytes.Add(Header.Seperator);

                    int data;
                    while ((data = fileStream.ReadByte()) != -1)
                    {
                        if (token.IsCancellationRequested)
                        {
                            outputStream.Close();
                            tempFile.Close();
                            File.Delete(tempFilePath);
                            return;
                        }

                        buffer[counter] = (byte)data;
                        counter++;
                        double value = Math.Round((double)++processedBytes / totalLength * 100, 1);

                        if (tempValue != value)
                        {
                            tempValue = value;
                            EventRaiser.OnProgressChange?.Invoke(value);
                        }

                        if (counter >= buffer.Length)
                        {
                            compressedBuffer = GZipProvider.Compress(buffer);
                            if (crypto != null)
                                compressedBuffer = crypto.EncryptBytes(compressedBuffer);
                            int diff = buffer.Length - compressedBuffer.Length;
                            tailBytes.AddRange(BitConverter.GetBytes(diff));
                            
                            buffer = new byte[BufferSize];
                            counter = 0;
                            WriteToOutputStream(tempFile, compressedBuffer, 128);
                        }
                    }
                    fileStream.Dispose();
                }
                if (counter > 0)
                {
                    byte[] excessBuffer = new byte[counter];
                    for (int i = 0; i < counter; i++)
                        excessBuffer[i] = buffer[i];
                    
                    compressedBuffer = GZipProvider.Compress(excessBuffer);
                    if (crypto != null)
                        compressedBuffer = crypto.EncryptBytes(compressedBuffer);
                    uint diff = (uint)(buffer.Length - compressedBuffer.Length);
                    tailBytes.AddRange(BitConverter.GetBytes(diff));
                    counter = 0;
                    WriteToOutputStream(tempFile, compressedBuffer, 128);
                }
                headerBytes.AddRange(Header.HeadEnd);
                headerBytes.AddRange(tailBytes);
                byte[] compressedHead = GZipProvider.Compress(headerBytes.ToArray());
                WriteToOutputStream(outputStream, compressedHead, 0);
                tempFile.Position = 0 ;
                tempFile.CopyTo(outputStream);

                tempFile.Dispose();
                File.Delete(tempFilePath);
            });
        }

        public async Task BuildFileNoEvent(Stream outputStream, CancellationToken token)
        {
            await Task.Run(() =>
            {
                outputStream.Write(Header.HeadBegin, 0, Header.HeadBegin.Length);

                List<byte> headerBytes = NewHeaderBytes();
                List<byte> tailBytes = new List<byte>();

                long totalLength = TotalLength();

                byte[] buffer = new byte[(int)BufferSize];
                byte[] compressedBuffer;
                int counter = 0;
                string tempFilePath = Path.Combine(Path.GetTempPath(), new Random().Next(1000, 10000) + ".dat");
                FileStream tempFile = new FileStream(tempFilePath, FileMode.CreateNew, FileAccess.ReadWrite);

                foreach (string curFile in inputFiles)
                {
                    fileStream = new FileStream(curFile, FileMode.Open, FileAccess.Read);

                    headerBytes.AddRange(BitConverter.GetBytes(fileStream.Length));
                    headerBytes.AddRange(Encoding.ASCII.GetBytes(Path.GetFileName(curFile)));
                    headerBytes.Add(Header.Seperator);

                    int data;
                    while ((data = fileStream.ReadByte()) != -1)
                    {
                        if (token.IsCancellationRequested)
                        {
                            outputStream.Close();
                            tempFile.Close();
                            File.Delete(tempFilePath);
                            return;
                        }

                        buffer[counter] = (byte)data;
                        counter++;

                        if (counter >= buffer.Length)
                        {
                            compressedBuffer = GZipProvider.Compress(buffer);
                            if (crypto != null)
                                compressedBuffer = crypto.EncryptBytes(compressedBuffer);
                            int diff = buffer.Length - compressedBuffer.Length;
                            tailBytes.AddRange(BitConverter.GetBytes(diff));

                            buffer = new byte[BufferSize];
                            counter = 0;
                            WriteToOutputStream(tempFile, compressedBuffer, 128);
                        }
                    }
                    fileStream.Dispose();
                }
                if (counter > 0)
                {
                    byte[] excessBuffer = new byte[counter];
                    for (int i = 0; i < counter; i++)
                        excessBuffer[i] = buffer[i];

                    compressedBuffer = GZipProvider.Compress(excessBuffer);
                    if (crypto != null)
                        compressedBuffer = crypto.EncryptBytes(compressedBuffer);
                    uint diff = (uint)(buffer.Length - compressedBuffer.Length);
                    tailBytes.AddRange(BitConverter.GetBytes(diff));
                    counter = 0;
                    WriteToOutputStream(tempFile, compressedBuffer, 128);
                }
                headerBytes.AddRange(Header.HeadEnd);
                headerBytes.AddRange(tailBytes);
                byte[] compressedHead = GZipProvider.Compress(headerBytes.ToArray());
                WriteToOutputStream(outputStream, compressedHead, 0);
                tempFile.Position = 0;
                tempFile.CopyTo(outputStream);

                tempFile.Dispose();
                File.Delete(tempFilePath);
            });
        }

        private List<byte> NewHeaderBytes()
        {
            List<byte> headerBytes = new List<byte>();
            headerBytes.AddRange(BitConverter.GetBytes(BufferSize));
            return headerBytes;
        }

        private long TotalLength()
        {
            long totalLength = 0;
            foreach (string curFile in inputFiles)
                totalLength += new FileInfo(curFile).Length;
            return totalLength;
        }

        private void WriteToOutputStream(Stream outputStream, byte[] dataToWrite, int obfus)
        {
            foreach (byte b in dataToWrite)
                outputStream.WriteByte((byte)(b + obfus));
        }
    }
}
