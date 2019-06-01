using System;

namespace OOF_Packer
{
    public class NotAnOOFPackException : Exception
    {
        public NotAnOOFPackException()
        {

        }

        public NotAnOOFPackException(string message, Exception innerException) : base(message, innerException)
        {

        }
    }
}
