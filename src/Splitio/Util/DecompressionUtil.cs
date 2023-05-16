using System;
using System.IO;
using System.IO.Compression;

namespace Splitio.Util
{
    public class DecompressionUtil
    {
        private static readonly int kZlibHeaderSize = 2;

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
                input = RemoveHeader(input);

                using (var outputStream = new MemoryStream())
                {
                    using (var compressedStream = new MemoryStream(input))
                    {
                        using (var deflateStream = new DeflateStream(compressedStream, CompressionMode.Decompress))
                        {
                            deflateStream.CopyTo(outputStream);
                        }
                    }

                    return outputStream.ToArray();
                }
            }
            catch { return new byte[0]; }
        }

        private static byte[] RemoveHeader(byte[] input)
        {
            // Create a new array
            byte[] toReturn = new byte[input.Length - kZlibHeaderSize];

            // Copy the elements after the range
            Array.Copy(input, kZlibHeaderSize, toReturn, 0, input.Length - kZlibHeaderSize);

            return toReturn;
        }
    }
}
