using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using tar_cs;

namespace BenchmarkingApp
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var testRuns = 2;
            
            var skip = false;

            var tmpDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
            
            
            Stopwatch sw = new Stopwatch();
            
            
         
            
            foreach (var testFile in _testDataDir.GetFiles("*.tgz"))
            {
             

                sw.Reset();
                sw.Start();
                
                for (int i = 0; i < testRuns; i++)
                {
                    createdDirs = new HashSet<string>();

                    dirsCreated = filesCreated = 0;

                    var testOutDir = tmpDir.CreateSubdirectory(testFile.Name.Replace(".", "_")+"__"+i);
                
                    using (var tarStream = new GZipStream(File.OpenRead(testFile.FullName), CompressionMode.Decompress))
                    {
                        
                        var tarReader = new TarReader(tarStream);
                        while (tarReader.MoveNext(skip)) // Moves pointer to the next file in the tar archive.
                        {
                            ExtractTarEntry(tarReader, testOutDir.FullName, null);

                            //hack: skip paxheaders
                            skip = tarReader.FileInfo.FileName.Contains("PaxHeader");
                        }
                        
                    }


                    Console.WriteLine($"\tTo unpack '{testFile.Name}' took '{sw.Elapsed}' - {filesCreated} files & {dirsCreated} dirs created.");
                }
                
                Console.WriteLine($"To unpack '{testFile.Name}' {testRuns} times took '{sw.Elapsed}' - avg - '{sw.Elapsed.TotalMinutes / testRuns} mins per go.'");
            }



            Directory.Delete(tmpDir.FullName, true);


            Console.ReadKey(true);
        }
                
        static DirectoryInfo _testDataDir = new DirectoryInfo("../../../../TestData");
     
        
        
        static HashSet<string> createdDirs = new HashSet<string>();


        private static int dirsCreated = 0;
        private static int filesCreated = 0;



        private static void ExtractTarEntry(TarReader tarReader, string outputDirectory, string packName)
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

                    if (!createdDirs.Contains(file.DirectoryName))
                    {
                        dirsCreated++;
                        file.Directory.Create();
                        createdDirs.Add(file.DirectoryName);
                    }

                    using (var outputStream = File.Create(fullPath))
                    {
                        filesCreated++;
                        // Read data from a current file to a Stream.
                        tarReader.Read(outputStream);
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