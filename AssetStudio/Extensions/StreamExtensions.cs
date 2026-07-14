using System.IO;

namespace AssetStudio
{
    public static class StreamExtensions
    {
        private const int BufferSize = 81920;

        public static void CopyTo(this Stream source, Stream destination, long size)
        {
            // Stream.Read may legally return fewer bytes than requested (common on FileStream);
            // bailing out on a short read silently truncates the copy and corrupts everything
            // that is carved out of the destination afterwards.
            var buffer = new byte[BufferSize];
            var left = size;
            while (left > 0)
            {
                int toRead = BufferSize < left ? BufferSize : (int)left;
                int read = source.Read(buffer, 0, toRead);
                if (read == 0)
                {
                    throw new EndOfStreamException($"Copy ended early, {left} of {size} bytes were still expected.");
                }
                destination.Write(buffer, 0, read);
                left -= read;
            }
        }

        public static void AlignStream(this Stream stream)
        {
            stream.AlignStream(4);
        }

        public static void AlignStream(this Stream stream, int alignment)
        {
            var pos = stream.Position;
            var mod = pos % alignment;
            if (mod != 0)
            {
                var rem = alignment - mod;
                for (int _ = 0; _ < rem; _++)
                {
                    if (!stream.CanWrite)
                    {
                        throw new IOException("End of stream");
                    }

                    stream.WriteByte(0);
                }
            }
        }
    }
}
