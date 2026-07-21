using System;
using System.IO;
using System.Linq;

namespace AssetStudio
{
    public class FileReader : EndianBinaryReader
    {
        public string FullPath;
        public string FileName;
        public FileType FileType;

        private static readonly byte[] gzipMagic = { 0x1f, 0x8b };
        private static readonly byte[] brotliMagic = { 0x62, 0x72, 0x6F, 0x74, 0x6C, 0x69 };
        private static readonly byte[] zipMagic = { 0x50, 0x4B, 0x03, 0x04 };
        private static readonly byte[] zipSpannedMagic = { 0x50, 0x4B, 0x07, 0x08 };


        public FileReader(string path) : this(path, File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) { }

        public FileReader(string path, Stream stream, bool leaveOpen = false) : base(stream, EndianType.BigEndian, leaveOpen)
        {
            FullPath = Path.GetFullPath(path);
            FileName = Path.GetFileName(path);
            FileType = CheckFileType();
            Logger.Verbose($"File {path} type is {FileType}");
        }

        private FileType CheckFileType()
        {
            var signature = this.ReadStringToNull(20);
            Position = 0;
            Logger.Verbose($"Parsed signature is {signature}");
            switch (signature)
            {
                case "UnityWeb":
                case "UnityRaw":
                case "UnityArchive":
                case "UnityFS":
                    return FileType.BundleFile;
                case "UnityWebData1.0":
                    return FileType.WebFile;
                default:
                    {
                        Logger.Verbose("signature does not match any of the supported string signatures, attempting to check bytes signatures");
                        byte[] magic = ReadBytes(2);
                        Position = 0;
                        Logger.Verbose($"Parsed signature is {Convert.ToHexString(magic)}");
                        if (gzipMagic.SequenceEqual(magic))
                        {
                            return FileType.GZipFile;
                        }
                        Logger.Verbose($"Parsed signature does not match with expected signature {Convert.ToHexString(gzipMagic)}");
                        Position = 0x20;
                        magic = ReadBytes(6);
                        Position = 0;
                        Logger.Verbose($"Parsed signature is {Convert.ToHexString(magic)}");
                        if (brotliMagic.SequenceEqual(magic))
                        {
                            return FileType.BrotliFile;
                        }
                        Logger.Verbose($"Parsed signature does not match with expected signature {Convert.ToHexString(brotliMagic)}");
                        if (IsSerializedFile())
                        {
                            return FileType.AssetsFile;
                        }
                        magic = ReadBytes(4);
                        Position = 0;
                        Logger.Verbose($"Parsed signature is {Convert.ToHexString(magic)}");
                        if (zipMagic.SequenceEqual(magic) || zipSpannedMagic.SequenceEqual(magic))
                        {
                            return FileType.ZipFile;
                        }
                        Logger.Verbose($"Parsed signature does not match with expected signature {Convert.ToHexString(zipMagic)} or {Convert.ToHexString(zipSpannedMagic)}");
                        Logger.Verbose($"Parsed signature does not match any of the supported signatures, assuming resource file");
                        return FileType.ResourceFile;
                    }
            }
        }

        private bool IsSerializedFile()
        {
            Logger.Verbose($"Attempting to check if the file is serialized file...");

            var fileSize = BaseStream.Length;
            if (fileSize < 20)
            {
                Logger.Verbose($"File size 0x{fileSize:X8} is too small, minimal acceptable size is 0x14, aborting...");
                return false;
            }
            var m_MetadataSize = ReadUInt32();
            long m_FileSize = ReadUInt32();
            var m_Version = ReadUInt32();
            long m_DataOffset = ReadUInt32();
            var m_Endianess = ReadByte();
            var m_Reserved = ReadBytes(3);
            if (m_Version >= 22)
            {
                if (fileSize < 48)
                {
                    Logger.Verbose($"File size 0x{fileSize:X8} for version {m_Version} is too small, minimal acceptable size is 0x30, aborting...");
                    Position = 0;
                    return false;
                }
                m_MetadataSize = ReadUInt32();
                m_FileSize = ReadInt64();
                m_DataOffset = ReadInt64();
            }
            Position = 0;
            if (m_FileSize != fileSize)
            {
                Logger.Verbose($"Parsed file size 0x{m_FileSize:X8} does not match stream size {fileSize}, file might be corrupted, aborting...");
                return false;
            }
            if (m_DataOffset > fileSize)
            {
                Logger.Verbose($"Parsed data offset 0x{m_DataOffset:X8} is outside the stream of the size {fileSize}, file might be corrupted, aborting...");
                return false;
            }
            Logger.Verbose($"Valid serialized file !!");
            return true;
        }
    }

    public static class FileReaderExtensions
    {
        public static FileReader PreProcessing(this FileReader reader, Game game)
        {
            Logger.Verbose("No preprocessing is needed");
            return reader;
        }
    }
}
