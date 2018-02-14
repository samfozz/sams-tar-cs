using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using tar_cs;
using Xunit;

namespace Tests
{
    public class UnitTest1
    {
        
        DirectoryInfo _testDataDir = new DirectoryInfo("../../../../../TestData");
        
        
        [Fact]
        public void CanUnpack()
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
                }
                
                Debug.WriteLine($"To unpack '{testFile.Name}' {testRuns} times took '{sw.Elapsed}' - avg - '{sw.Elapsed / testRuns} per go.'");
            }
            Directory.Delete(tmpDir.FullName, true);
        
            
         
            
        }
        
        
        
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

                    if (!file.Directory.Exists)
                    {
                        file.Directory.Create();
                    }

                    using (var outputStream = File.Create(fullPath))
                    {
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