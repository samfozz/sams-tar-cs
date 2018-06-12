using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace SamsTarCS
{
    public class UnTar
    {
        /// <summary>
        /// Extract to directory
        /// </summary>
        /// <param name="stream">The stream</param>
        /// <param name="outputDir"></param>
        /// <param name="smallFileQueue">The shared buffer ( may be null to disable ) </param>
        /// <param name="smallFileSize">The max size of files for the shared queue, if in use.</param>
        /// <returns></returns>
        public static UnTarResult Extract(Stream stream, DirectoryInfo outputDir, AsyncFileWriteQueue smallFileQueue = null, int smallFileSize = -1)
        {
            if (smallFileSize < 0)
            {
                smallFileSize = -1;
            }

            if (smallFileSize > smallFileQueue.MaxSize)
            {
                throw new ArgumentException("'smallFileSize' should not be greater than 'smallFileQueue.MaxSize'");
            }

            return new UnTar().ExtractInternal(stream, outputDir, smallFileQueue, smallFileSize);
        }






        // Created directories are cached, because it's quite slow to check if they exist again via the FS...
        readonly HashSet<string> _createdDirs = new HashSet<string>();
        private int _dirsCreated = 0;
        private int _filesCreated = 0;

        private UnTarResult ExtractInternal(Stream stream, DirectoryInfo outputDir, AsyncFileWriteQueue smallFileQueue, int smallFileSize)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            var tarReader = new TarReader(stream);
            var skip = false;


            while (tarReader.MoveNext(skip)) // Moves pointer to the next file in the tar archive.
            {
                ExtractTarEntry(tarReader, outputDir.FullName, null, smallFileQueue, smallFileSize);

                //hack: skip paxheaders
                skip = tarReader.FileInfo.FileName.Contains("PaxHeader");
            }

            sw.Stop();

            return new UnTarResult(_filesCreated, _dirsCreated, sw.Elapsed, outputDir);
        }
        
        private void ExtractTarEntry(TarReader tarReader, string outputDirectory, string packName, AsyncFileWriteQueue smallFileQueue, int smallFileSize)
        {
            var relativePath = tarReader.FileInfo.FileName;

            if (relativePath.StartsWith("package/"))
            {
                relativePath = relativePath.Substring("package/".Length);
            }
            else if (relativePath.StartsWith(packName + "/"))
            {
                relativePath = relativePath.Substring((packName + "/").Length);
            }

            // Relative path can contain slash, not backslash.
            // Use Path.GetFullPath() method to convert path.
            var fullPath = Path.GetFullPath(Path.Combine(outputDirectory, relativePath));
            
            switch (tarReader.FileInfo.EntryType)
            {
                case EntryType.File:
                case EntryType.FileObsolete:

                    var file = new FileInfo(fullPath);

                    if (!_createdDirs.Contains(file.DirectoryName))
                    {
                        _dirsCreated++;
                        file.Directory.Create();
                        _createdDirs.Add(file.DirectoryName);
                    }

                    if (smallFileQueue == null || tarReader.FileInfo.SizeInBytes > smallFileQueue.MaxSize || tarReader.FileInfo.SizeInBytes > smallFileSize)
                    {
                        using (var outputStream = File.Create(fullPath))
                        {
                            _filesCreated++;
                            // Read data from a current file to a Stream.

                            tarReader.Read(outputStream);
                        }
                    }
                    else
                    {
                        using (var outputStream = new MemoryStream())
                        {
                            _filesCreated++;

                            tarReader.Read(outputStream);
                            
                            smallFileQueue.Enqueue(fullPath, outputStream.ToArray(), TimeSpan.FromMinutes(1)).Wait();
                        }
                    }


                    break;
                case EntryType.Directory:
                    Directory.CreateDirectory(fullPath);
                    break;
                default:
                    break;
                //     throw new NotSupportedException("Not supported entry type: " + tarReader.FileInfo.EntryType);
            }


        }


         

    }
}