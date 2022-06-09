using System;

namespace LibrarianTool.Domain.Utils
{
    public class ArchiveException : Exception
    {
        public ArchiveException(string message) : base(message)
        { }
    }
}
