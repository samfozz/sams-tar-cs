using System;

namespace SamsTarCS
{
    internal class TarException : Exception
    {
        public TarException(string message) : base(message)
        {
        }
    }
}