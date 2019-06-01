using System;
using System.IO;

namespace RijndaelCryptography
{
    public class BytesGenerator
    {
        public event Action TaskDone;

        private readonly long Size;

        public BytesGenerator(long GenFileLength)
        {
            Size = GenFileLength;
        }

        public void KeyFileGenerator(string TargetFile, byte[] SettingsByte)
        {
            
            File.WriteAllBytes(TargetFile, SettingsByte);

            var Filestream = new FileStream(TargetFile, FileMode.Append);
            Random random = new Random();

            long loop = 0;
            byte bytevalue;
            while (loop < Size - 2)
            {
                bytevalue = (byte)random.Next(256);
                Filestream.WriteByte(bytevalue);
                loop++;
            }
            Filestream.Dispose();

            TaskDone?.Invoke();
            
        }

        public static void KeyFileGenerator(string FilePath)
        {
            Random random = new Random();
            byte[] keyFileBytes = new byte[random.Next(1024, 102401)];
            random.NextBytes(keyFileBytes);
            File.WriteAllBytes(FilePath, keyFileBytes);
        }

        public static void KeyFileGenerator(string FilePath, int size)
        {
            Random random = new Random();
            byte[] keyFileBytes = new byte[size];
            random.NextBytes(keyFileBytes);
            File.WriteAllBytes(FilePath, keyFileBytes);
        }
    }

}
