using Nyerguds.GameData.Compression;
using Nyerguds.Util;
using System;

namespace Nyerguds.GameData.Dynamix
{
    /// <summary>
    /// Dynamix compression / decompression class. Offers functionality to decompress chunks using RLE or LZW decompression,
    /// and has functions to compress to RLE.
    /// </summary>
    public class DynamixCompression
    {

        public static byte[] EnrichFourBit(byte[] vgaData, byte[] binData)
        {
            var fullData = new byte[vgaData.Length * 2];
            // ENRICHED 4-BIT IMAGE LOGIC
            // Basic principle: The data in the VGA chunk is already perfectly viewable as 4-bit image. The colour palettes
            // are designed so each block of 16 colours consists of different tints of the same colour. The 16-colour palette
            // for the VGA chunk alone can be constructed by taking a palette slice where each colour is 16 entries apart.

            // This VGA data [AB] gets "ennobled" to 8-bit by adding detail data [ab] from the BIN chunk, to get bytes [Aa Bb].
            for (var i = 0; i < vgaData.Length; i++)
            {
                var offs = i * 2;
                // This can be written much simpler, but I expanded it to clearly show each step.
                var vgaPix = vgaData[i]; // 0xAB
                var binPix = binData[i]; // 0xab
                var vgaPixHi = (byte)((vgaPix & 0xF0) >> 4); // 0x0A
                var binPixHi = (byte)((binPix & 0xF0) >> 4); // 0x0a
                var finalPixHi = (byte)((vgaPixHi << 4) + binPixHi); // Aa
                var vgaPixLo = (byte)(vgaPix & 0x0F); // 0x0B
                var binPixLo = (byte)(binPix & 0x0F); // 0x0b
                var finalPixLo = (byte)((vgaPixLo << 4) + binPixLo); // Bb
                // Final result: AB + ab == [Aa Bb]
                fullData[offs] = finalPixHi;
                fullData[offs + 1] = finalPixLo;
            }
            return fullData;
        }

        public static void SplitEightBit(byte[] imageData, out byte[] vgaData, out byte[] binData)
        {
            vgaData = new byte[(imageData.Length + 1) / 2];
            binData = new byte[(imageData.Length + 1) / 2];
            for (var i = 0; i < imageData.Length; i++)
            {
                var pixData = imageData[i];
                var pixHi = pixData & 0xF0;
                var pixLo = pixData & 0x0F;
                if (i % 2 == 0)
                    pixLo <<= 4;
                else
                    pixHi >>= 4;
                var pixOffs = i / 2;
                vgaData[pixOffs] |= (byte)pixHi;
                binData[pixOffs] |= (byte)pixLo;
            }
        }

        /// <summary>
        /// Decompresses Dynamix chunk data. The chunk data should start with the compression
        /// type byte, followed by a 32-bit integer specifying the uncompressed length.
        /// </summary>
        /// <param name="chunkData">Chunk data to decompress. </param>
        /// <returns>The uncompressed data.</returns>
        public static byte[] DecodeChunk(byte[] chunkData)
        {
            if (chunkData.Length < 5)
                throw new FileTypeLoadException("Chunk is too short to read compression header!");
            var compression = chunkData[0];
            var uncompressedLength = (int)ArrayUtils.ReadIntFromByteArray(chunkData.AsSpan(1, 4), true);
            return Decode(chunkData, 5, null, compression, uncompressedLength);
        }

        /// <summary>
        /// Decompresses Dynamix data.
        /// </summary>
        /// <param name="buffer">Buffer to decompress</param>
        /// <param name="startOffset">Start offset of the data in the buffer</param>
        /// <param name="endOffset">End offset of the data in the buffer</param>
        /// <param name="compression">Compression type: 0 for uncompressed, 1 for RLE, 2 for LZA</param>
        /// <param name="decompressedSize">Decompressed size.</param>
        /// <returns>The uncompressed data.</returns>
        public static byte[] Decode(byte[] buffer, int? startOffset, int? endOffset, int compression, int decompressedSize)
        {
            var start = startOffset ?? 0;
            var end = endOffset ?? buffer.Length;
            if (end < start)
                throw new ArgumentException("End offset cannot be smaller than start offset!", nameof(endOffset));
            if (start < 0 || start > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(startOffset));
            if (end < 0 || end > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(endOffset));
            switch (compression)
            {
                case 0:
                    var outBuff = new byte[decompressedSize];
                    var len = Math.Min(end - start, decompressedSize);
                    Array.Copy(buffer, start, outBuff, 0, len);
                    return outBuff;
                case 1:
                    return RleDecode(buffer, (uint)start, (uint)end, decompressedSize, true);
                case 2:
                    return LzwDecode(buffer, start, end, decompressedSize);
                case 3:
                    return LzssDecode(buffer, start, end, decompressedSize);
                default:
                    throw new ArgumentException("Unknown compression type: \"" + compression + "\".", nameof(compression));
            }
        }

        public static byte[] LzssDecode(byte[] buffer, int? startOffset, int? endOffset, int decompressedSize)
        {
            var lzhDec = new DynamixLzHuffDecoder();
            var outputBuffer = lzhDec.Decode(buffer, startOffset, endOffset, decompressedSize);
            if (decompressedSize < outputBuffer.Length)
                throw new ArgumentException("Decompression failed!");
            if (decompressedSize > outputBuffer.Length)
            {
                var output = new byte[decompressedSize];
                Array.Copy(outputBuffer, output, outputBuffer.Length);
                return output;
            }
            return outputBuffer;
        }

        public static byte[] LzwDecode(byte[] buffer, int? startOffset, int? endOffset, int decompressedSize)
        {
            var lzwDec = new DynamixLzwDecoder();
            var outputBuffer = new byte[decompressedSize];
            lzwDec.LzwDecode(buffer, startOffset, endOffset, outputBuffer);
            return outputBuffer;
        }

        public static byte[] RleDecode(byte[] buffer, uint? startOffset, uint? endOffset, int decompressedSize, bool abortOnError)
        {
            var outputBuffer = new byte[decompressedSize];
            // Uses standard RLE implementation.
            var rle = new RleCompressionHighBitRepeat();
            rle.RleDecodeData(buffer, startOffset, endOffset, ref outputBuffer, abortOnError);
            return outputBuffer;
        }

        /// <summary>
        /// Applies LZW Encoding to the given data.
        /// </summary>
        /// <param name="buffer">Input buffer</param>
        /// <returns>The run-length encoded data</returns>
        public static byte[] LzssEncode(byte[] buffer)
        {
            var enc = new DynamixLzHuffDecoder();
            return null; // enc.Encode(buffer, null, null);
        }


        /// <summary>
        /// Applies LZW Encoding to the given data.
        /// </summary>
        /// <param name="buffer">Input buffer</param>
        /// <returns>The run-length encoded data</returns>
        public static byte[] LzwEncode(byte[] buffer)
        {
            var enc = new DynamixLzwEncoder();
            return enc.Compress(buffer);
        }

        /// <summary>
        /// Applies Run-Length Encoding (RLE) to the given data.
        /// </summary>
        /// <param name="buffer">Input buffer</param>
        /// <returns>The run-length encoded data</returns>
        public static byte[] RleEncode(byte[] buffer)
        {
            // Uses standard RLE implementation.
            var rle = new RleCompressionHighBitRepeat();
            return rle.RleEncodeData(buffer);
        }

        /// <summary>Switches index 00 and FF on indexed image data, to compensate for this oddity in the MA8 chunks.</summary>
        /// <param name="imageData">Image data to process.</param>
        public static void SwitchBackground(byte[] imageData)
        {
            for (var i = 0; i < imageData.Length; i++)
            {
                if (imageData[i] == 0x00)
                    imageData[i] = 0xFF;
                else if (imageData[i] == 0xFF)
                    imageData[i] = 0x00;
            }
        }
    }
}