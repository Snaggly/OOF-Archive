using System;

namespace OOF_Packer
{
    public class InvalidHeaderException : Exception
    {
        public InvalidHeaderException()
        {

        }

        public InvalidHeaderException(string message, Exception innerException) : base(message, innerException)
        {

        }
    }
}
