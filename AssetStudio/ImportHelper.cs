using Org.Brotli.Dec;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace AssetStudio
{
    public static class ImportHelper
    {
        public static void MergeSplitAssets(string path, bool allDirectories = false)
        {
            Logger.Verbose($"Processing split assets (.splitX) prior to loading files...");
            var splitFiles = Directory.GetFiles(path, "*.split0", allDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            Logger.Verbose($"Found {splitFiles.Length} split files, attempting to merge...");
            foreach (var splitFile in splitFiles)
            {
                var destFile = Path.GetFileNameWithoutExtension(splitFile);
                var destPath = Path.GetDirectoryName(splitFile);
                var destFull = Path.Combine(destPath, destFile);
                if (!File.Exists(destFull))
                {
                    var splitParts = Directory.GetFiles(destPath, destFile + ".split*");
                    Logger.Verbose($"Creating {destFull} where split files will be combined");
                    using (var destStream = File.Create(destFull))
                    {
                        for (int i = 0; i < splitParts.Length; i++)
                        {
                            var splitPart = destFull + ".split" + i;
                            using (var sourceStream = File.OpenRead(splitPart))
                            {
                                sourceStream.CopyTo(destStream);
                                Logger.Verbose($"{splitPart} has been combined into {destFull}");
                            }
                        }
                    }
                }
            }
        }

        public static string[] ProcessingSplitFiles(List<string> selectFile)
        {
            Logger.Verbose("Filter out paths that has .split and has the same name");
            var splitFiles = selectFile.Where(x => x.Contains(".split"))
                .Select(x => Path.Combine(Path.GetDirectoryName(x), Path.GetFileNameWithoutExtension(x)))
                .Distinct()
                .ToList();
            selectFile.RemoveAll(x => x.Contains(".split"));
            foreach (var file in splitFiles)
            {
                if (File.Exists(file))
                {
                    selectFile.Add(file);
                }
            }
            return selectFile.Distinct().ToArray();
        }

        public static FileReader DecompressGZip(FileReader reader)
        {
            Logger.Verbose($"Decompressing GZip file {reader.FileName} into memory");
            using (reader)
            {
                var stream = new MemoryStream();
                using (var gs = new GZipStream(reader.BaseStream, CompressionMode.Decompress))
                {
                    gs.CopyTo(stream);
                }
                stream.Position = 0;
                return new FileReader(reader.FullPath, stream);
            }
        }

        public static FileReader DecompressBrotli(FileReader reader)
        {
            Logger.Verbose($"Decompressing Brotli file {reader.FileName} into memory");
            using (reader)
            {
                var stream = new MemoryStream();
                using (var brotliStream = new BrotliInputStream(reader.BaseStream))
                {
                    brotliStream.CopyTo(stream);
                }
                stream.Position = 0;
                return new FileReader(reader.FullPath, stream);
            }
        }
    }
}
