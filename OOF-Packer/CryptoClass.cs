using System;
using System.Text;
using System.Security.Cryptography;
using System.IO;

namespace RijndaelCryptography
{
    public enum HashType { MD5, SHA1, SHA256, SHA512 }

    public class CryptoClass
    {
        public event Action Written;
        public event Action<Exception> Error;

        private int KeySize;
        private HashType HashType;
        private string Key;
        private byte[] SaltKey;
        private byte[] VIKey;
        
        public CryptoClass(int KeySize, HashType HashType, byte[] SaltKey, byte[] VIKey, string InputFile, int EntryPoint)
        {
            this.KeySize = KeySize;
            this.HashType = HashType;
            this.SaltKey = SaltKey;
            this.VIKey = VIKey;
            Key = HashFile(InputFile, HashType, EntryPoint);
            keyBytes = new Rfc2898DeriveBytes(Key, SaltKey).GetBytes(KeySize / 8);
            RMCrypto = new RijndaelManaged() { KeySize = KeySize, Mode = CipherMode.CBC, Padding = PaddingMode.PKCS7 };
        }

        public CryptoClass(int KeySize, HashType HashType, byte[] SaltKey, byte[] VIKey, string Password)
        {
            this.KeySize = KeySize;
            this.HashType = HashType;
            this.SaltKey = SaltKey;
            this.VIKey = VIKey;
            Key = HashKey(Password, HashType);
            keyBytes = new Rfc2898DeriveBytes(Key, SaltKey).GetBytes(KeySize / 8);
            RMCrypto = new RijndaelManaged() { KeySize = KeySize, Mode = CipherMode.CBC, Padding = PaddingMode.PKCS7 };
        }

        public CryptoClass(string InputFile)
        {
            KeySize = 256;
            HashType = HashType.SHA256;
            SaltKey = new byte[8];
            VIKey = new byte[16];
            keyBytes = new byte[32];
            FSIn = new FileStream(InputFile, FileMode.Open, FileAccess.Read);
            FSIn.Read(SaltKey, 0, SaltKey.Length);
            FSIn.Read(VIKey, 0, VIKey.Length);
            FSIn.Read(keyBytes, 0, keyBytes.Length);
            FSIn.Dispose();
            RMCrypto = new RijndaelManaged() { KeySize = KeySize, Mode = CipherMode.CBC, Padding = PaddingMode.PKCS7 };
        }

        private FileStream FSIn;
        private FileStream FSOut;
        private CryptoStream CStream;
        private StringBuilder StringBuilder;
        private byte[] keyBytes;
        private RijndaelManaged RMCrypto;

        public string HashKey(string Password, HashType HashType)
        {
            StringBuilder Sb = new StringBuilder();
            byte[] result = new byte[0];
            switch ((int)HashType)
            {
                case 0:
                    result = MD5.Create().ComputeHash(Encoding.ASCII.GetBytes(Password));
                    break;
                case 1:
                    result = SHA1.Create().ComputeHash(Encoding.ASCII.GetBytes(Password));
                    break;
                case 2:
                    result = SHA256.Create().ComputeHash(Encoding.ASCII.GetBytes(Password));
                    break;
                case 3:
                    result = SHA512.Create().ComputeHash(Encoding.ASCII.GetBytes(Password));
                    break;
            }
            foreach (byte b in result)
                Sb.Append(b.ToString("x2"));
            return Sb.ToString();
        }

        public string HashFile(string inputFile, HashType HashType, int EntryPoint)
        {
            StringBuilder = new StringBuilder();
            FSIn = new FileStream(inputFile, FileMode.Open, FileAccess.Read) { Position = EntryPoint };
            byte[] result = new byte[0];
            switch ((int)HashType)
            {
                case 0:
                    result = MD5.Create().ComputeHash(FSIn);
                    break;
                case 1:
                    result = SHA1.Create().ComputeHash(FSIn);
                    break;
                case 2:
                    result = SHA256.Create().ComputeHash(FSIn);
                    break;
                case 3:
                    result = SHA512.Create().ComputeHash(FSIn);
                    break;
            }
            foreach (byte b in result)
                StringBuilder.Append(b.ToString("x2"));
            FSIn.Dispose();
            return StringBuilder.ToString();
        }

        public string HashStream(Stream inputFile, HashType HashType)
        {
            StringBuilder = new StringBuilder();
            byte[] result = new byte[0];
            switch ((int)HashType)
            {
                case 0:
                    result = MD5.Create().ComputeHash(inputFile);
                    break;
                case 1:
                    result = SHA1.Create().ComputeHash(inputFile);
                    break;
                case 2:
                    result = SHA256.Create().ComputeHash(inputFile);
                    break;
                case 3:
                    result = SHA512.Create().ComputeHash(inputFile);
                    break;
            }
            foreach (byte b in result)
                StringBuilder.Append(b.ToString("x2"));
            FSIn.Dispose();
            return StringBuilder.ToString();
        }

        public void EncryptFile(string inputFile, string outputFile)
        {
            try
            {
                FSIn = new FileStream(inputFile, FileMode.Open, FileAccess.Read);
                FSOut = new FileStream(outputFile, FileMode.Create);
                
                var encryptor = RMCrypto.CreateEncryptor(keyBytes, VIKey);
                CStream = new CryptoStream(FSOut, encryptor, CryptoStreamMode.Write);

                int data;
                while ((data = FSIn.ReadByte()) != -1)
                    CStream.WriteByte((byte)data);
                CStream.Dispose();
                FSIn.Dispose();
                FSOut.Dispose();
                Written?.Invoke();
            }
            catch (Exception e)
            {
                FSIn?.Dispose();
                FSOut?.Dispose();
                File.Delete(outputFile);
                Error?.Invoke(e);
            }
        }

        public byte[] EncryptBytes(byte[] inputByes)
        {
            MemoryStream ms = new MemoryStream(inputByes);
            var encryptor = RMCrypto.CreateEncryptor(keyBytes, VIKey);
            CryptoStream cryptoStream = new CryptoStream(ms, encryptor, CryptoStreamMode.Read);
            MemoryStream encryptedMemory = new MemoryStream();
            cryptoStream.CopyTo(encryptedMemory);
            cryptoStream.Dispose();
            return encryptedMemory.ToArray();
        }

        public CryptoStream EncryptStream(Stream outputStream)
        {
            var encryptor = RMCrypto.CreateEncryptor(keyBytes, VIKey);
            return new CryptoStream(outputStream, encryptor, CryptoStreamMode.Write);
        }

        public void DecryptFile(string inputFile, string outputFile)
        {
            try
            {
                FSIn = new FileStream(inputFile, FileMode.Open, FileAccess.Read);
                FSOut = new FileStream(outputFile, FileMode.Create);
                var decryptor = RMCrypto.CreateDecryptor(keyBytes, VIKey);
                CStream = new CryptoStream(FSIn, decryptor, CryptoStreamMode.Read);

                int data;
                while ((data = CStream.ReadByte()) != -1)
                    FSOut.WriteByte((byte)data);
                CStream.Dispose();
                FSIn.Dispose();
                FSOut.Dispose();
                Written?.Invoke();
            } catch (Exception e)
            {
                FSIn?.Dispose();
                FSOut?.Dispose();
                File.Delete(outputFile);
                Error?.Invoke(e);
            }
        }

        public byte[] DecryptBytes(byte[] inputByes)
        {
            MemoryStream ms = new MemoryStream(inputByes);
            var decryptor = RMCrypto.CreateDecryptor(keyBytes, VIKey);
            CryptoStream cryptoStream = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            MemoryStream decryptedMemory = new MemoryStream();
            cryptoStream.CopyTo(decryptedMemory);
            cryptoStream.Dispose();
            return decryptedMemory.ToArray();
        }

        public CryptoStream DecryptStream(Stream inputStream)
        {
            var decryptor = RMCrypto.CreateDecryptor(keyBytes, VIKey);
            return new CryptoStream(inputStream, decryptor, CryptoStreamMode.Read);
        }
    }
}