using System;

namespace OOF_Packer
{
    public class EncryptedException : Exception
    {
        public EncryptedException()
        {

        }

        public EncryptedException(string message, Exception innerException) : base(message, innerException)
        {

        }
    }
}
