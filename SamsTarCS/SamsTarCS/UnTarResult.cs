using System;
using System.IO;

namespace SamsTarCS
{
    public class UnTarResult
    {
        public int FileCount { get; }

        public int DirectoryCount { get; }

        public TimeSpan Time { get; }

        public DirectoryInfo Output { get; }

        public UnTarResult(int fileCount, int directoryCount, TimeSpan time, DirectoryInfo output)
        {
            FileCount = fileCount;
            DirectoryCount = directoryCount;
            Time = time;
            Output = output;
        }
    }
}