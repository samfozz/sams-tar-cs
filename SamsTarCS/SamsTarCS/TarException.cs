using System;

namespace SamsTarCS
{
    public class TarException : Exception
    {
        public TarException(string message) : base(message)
        {
        }
    }
}