using Splitio.Util.Zip.Compression.Streams;
using System.IO;
using System.IO.Compression;

namespace Splitio.Util
{
    public class DecompressionUtil
    {
        public static byte[] GZip(byte[] input)
        {
            try
            {
                using (var outputStream = new MemoryStream())
                {
                    using (var compressedInput = new MemoryStream(input))
                    {
                        using (var decompressor = new GZipStream(compressedInput, CompressionMode.Decompress))
                        {
                            decompressor.CopyTo(outputStream);
                        }
                    }

                    return outputStream.ToArray();
                }
            }
            catch { return new byte[0]; }
        }

        public static byte[] ZLib(byte[] input)
        {
            try
            {
                using (var outputStream = new MemoryStream())
                {
                    using (var compressedInput = new MemoryStream(input))
                    {
                        using (var inputStream = new InflaterInputStream(compressedInput))
                        {
                            inputStream.CopyTo(outputStream);
                            outputStream.Position = 0;
                        }
                    }

                    return outputStream.ToArray();
                }
            }
            catch { return new byte[0]; }
        }
    }
}
