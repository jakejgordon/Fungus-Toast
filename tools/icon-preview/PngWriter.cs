using System.IO.Compression;
using UnityEngine;

namespace FungusToast.Tools.IconPreview
{
    /// <summary>Minimal RGBA PNG encoder so the harness needs no imaging dependency.</summary>
    internal static class PngWriter
    {
        /// <param name="pixels">Texture2D layout: row 0 is the bottom row.</param>
        public static byte[] Encode(Color[] pixels, int width, int height)
        {
            var raw = new byte[(width * 4 + 1) * height];
            int offset = 0;
            for (int row = 0; row < height; row++)
            {
                raw[offset++] = 0;
                int sourceRow = height - 1 - row;
                for (int x = 0; x < width; x++)
                {
                    Color c = pixels[sourceRow * width + x];
                    raw[offset++] = ToByte(c.r);
                    raw[offset++] = ToByte(c.g);
                    raw[offset++] = ToByte(c.b);
                    raw[offset++] = ToByte(c.a);
                }
            }

            using var compressed = new MemoryStream();
            using (var zlib = new ZLibStream(compressed, System.IO.Compression.CompressionLevel.Optimal, leaveOpen: true))
            {
                zlib.Write(raw, 0, raw.Length);
            }

            using var output = new MemoryStream();
            output.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
            var header = new byte[13];
            WriteBigEndian(header, 0, width);
            WriteBigEndian(header, 4, height);
            header[8] = 8;
            header[9] = 6;
            WriteChunk(output, "IHDR", header);
            WriteChunk(output, "IDAT", compressed.ToArray());
            WriteChunk(output, "IEND", Array.Empty<byte>());
            return output.ToArray();
        }

        private static byte ToByte(float value) => (byte)Math.Clamp((int)MathF.Round(value * 255f), 0, 255);

        private static void WriteChunk(Stream output, string type, byte[] data)
        {
            var lengthBytes = new byte[4];
            WriteBigEndian(lengthBytes, 0, data.Length);
            output.Write(lengthBytes);
            var typeBytes = System.Text.Encoding.ASCII.GetBytes(type);
            output.Write(typeBytes);
            output.Write(data);
            uint crc = Crc32(typeBytes, data);
            var crcBytes = new byte[4];
            WriteBigEndian(crcBytes, 0, (int)crc);
            output.Write(crcBytes);
        }

        private static void WriteBigEndian(byte[] buffer, int offset, int value)
        {
            buffer[offset] = (byte)(value >> 24);
            buffer[offset + 1] = (byte)(value >> 16);
            buffer[offset + 2] = (byte)(value >> 8);
            buffer[offset + 3] = (byte)value;
        }

        private static readonly uint[] CrcTable = BuildCrcTable();

        private static uint[] BuildCrcTable()
        {
            var table = new uint[256];
            for (uint n = 0; n < 256; n++)
            {
                uint c = n;
                for (int k = 0; k < 8; k++)
                {
                    c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
                }

                table[n] = c;
            }

            return table;
        }

        private static uint Crc32(byte[] first, byte[] second)
        {
            uint crc = 0xFFFFFFFFu;
            foreach (byte b in first)
            {
                crc = CrcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
            }

            foreach (byte b in second)
            {
                crc = CrcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
            }

            return crc ^ 0xFFFFFFFFu;
        }
    }
}
