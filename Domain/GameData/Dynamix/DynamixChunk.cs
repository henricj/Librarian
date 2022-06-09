using Nyerguds.Util;
using System;
using System.Linq;
using System.Text;

namespace Nyerguds.GameData.Dynamix
{
    public class DynamixChunk
    {
        public string Identifier { get; private set; }
        public int Address { get; private set; }
        public bool IsContainer { get; set; }
        public byte[] Data
        {
            get => this.m_data;
            set
            {
                var dataCopy = new byte[value.Length];
                Array.Copy(value, 0, dataCopy, 0, value.Length);
                this.m_data = dataCopy;
            }
        }
        public int Length => this.Data.Length + 8;
        public int DataLength => this.Data.Length;

        byte[] m_data;

        /// <summary>
        /// Creates a chunk.
        /// </summary>
        /// <param name="identifier">Chunk identifier.</param>
        public DynamixChunk(string identifier)
        {
            if (identifier.Length != 3 || Encoding.UTF8.GetBytes(identifier).Length != 3)
                throw new ArgumentException("Identifier must be a 3 ASCII characters!", nameof(identifier));
            this.Identifier = identifier;
        }

        /// <summary>
        /// Creates a chunk with data in it.
        /// </summary>
        /// <param name="identifier">Chunk identifier.</param>
        /// <param name="data">Data to copy into the chunk data.</param>
        public DynamixChunk(string identifier, byte[] data)
            : this(identifier)
        {
            this.Data = data;
        }

        /// <summary>
        /// Creates a chunk with a 5-byte compression header. This compression header is written at the start of the data array.
        /// </summary>
        /// <param name="identifier">Chunk identifier.</param>
        /// <param name="compressionType">Compression type.</param>
        /// <param name="uncompressedSize">Size of the uncompressed data.</param>
        /// <param name="data">Compressed data to copy into the chunk data.</param>
        public DynamixChunk(string identifier, byte compressionType, uint uncompressedSize, ReadOnlySpan<byte> data)
            : this(identifier)
        {
            var fullData = new byte[data.Length + 5];
            fullData[0] = compressionType;
            ArrayUtils.WriteIntToByteArray(fullData, 1, 4, true, uncompressedSize);
            data.CopyTo(fullData.AsSpan(5));
            this.m_data = fullData;
        }

        /// <summary>
        /// Returns the full chunk as byte array.
        /// </summary>
        /// <returns>The full chunk as byte array.</returns>
        public byte[] WriteChunk()
        {
            var data = new byte[this.Length];
            this.WriteChunk(data, 0);
            return data;
        }

        /// <summary>
        /// Writes this chunk into a target array.
        /// </summary>
        /// <param name="target">Target array</param>
        /// <param name="offset">Offset in the target array</param>
        /// <returns>The offset right behind the written data in the target array.</returns>
        public int WriteChunk(byte[] target, int offset)
        {
            Array.Copy(Encoding.ASCII.GetBytes(this.Identifier + ":"), 0, target, offset, 4);
            offset += 4;
            ArrayUtils.WriteIntToByteArray(target, offset, 4, true, (uint)(this.DataLength));
            offset += 4;
            if (this.IsContainer)
                target[offset - 1] |= 0x80;
            Array.Copy(this.Data, 0, target, offset, this.DataLength);
            return offset + this.DataLength;
        }

        /// <summary>
        /// Builds a chunk container which has other chunks as its data. The other chunk
        /// objects are not preserved in this operation; they are just saved as bytes.
        /// </summary>
        /// <param name="chunkName">Chunk name</param>
        /// <param name="contents">Chunk contents</param>
        /// <returns>The new chunk</returns>
        public static DynamixChunk BuildChunk(string chunkName, params DynamixChunk[] contents)
        {
            var mainChunk = new DynamixChunk(chunkName);
            var fullDataSize = contents.Sum(x => x.Length);
            var fullData = new byte[fullDataSize];
            var offset = 0;
            foreach (var chunk in contents)
                offset = chunk.WriteChunk(fullData, offset);
            mainChunk.m_data = fullData;
            mainChunk.IsContainer = true;
            return mainChunk;
        }

        /// <summary>
        /// Finds and reads a chunk from data.
        /// </summary>
        /// <param name="data">The data to read from.</param>
        /// <param name="chunkName">The chunk to find.</param>
        /// <returns>The chunk as DynamixChunk object.</returns>
        public static DynamixChunk ReadChunk(ReadOnlySpan<byte> data, string chunkName)
        {
            var address = FindChunk(data, chunkName);
            if (address == -1)
                return null;
            var dc = new DynamixChunk(chunkName)
            {
                Address = address,
                IsContainer = (data[7] & 0x80) != 0
            };
            dc.Data = GetChunkData(data[dc.Address..]);
            return dc;
        }

        /// <summary>
        /// Finds the start of a chunk.
        /// It does not go over the first 8 bytes, but starts at the start of the header chunk.
        /// </summary>
        /// <param name="data">The bytes of the Dynamix file</param>
        /// <param name="chunkName">The name of the chunk to find.</param>
        /// <returns>The index of the start of the chunk, or -1 if the chunk was not found.</returns>
        public static int FindChunk(ReadOnlySpan<byte> data, string chunkName)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data), "No data given!");
            if (chunkName == null)
                throw new ArgumentNullException(nameof(chunkName), "No chunk name given!");
            // Using UTF-8 as extra check to make sure the name does not contain > 127 values.
            var chunkNamebytes = Encoding.UTF8.GetBytes(chunkName + ":");
            if (chunkName.Length != 3 || chunkNamebytes.Length != 4)
                throw new ArgumentException("Chunk name must be 3 ASCII characters!", nameof(chunkName));
            var offset = 0;
            var end = data.Length;
            // continue until either the end is reached, or there is not enough space behind it for reading a new header
            while (offset < end && offset + 8 < end)
            {
                if (data.Slice(offset, 4).SequenceEqual(chunkNamebytes))
                    return offset;
                var chunkLength = GetChunkDataLength(data[offset..]);
                if (chunkLength < 0)
                    return -1;
                offset += 8 + chunkLength;
                if (offset < 0)
                    return -1;
            }
            return -1;
        }

        public static int GetChunkDataLength(ReadOnlySpan<byte> data)
        {
            if (8 > data.Length)
                throw new FileTypeLoadException("Bad chunk size in Dynamix image.");
            // Don't want to use BitConverter; then you have to check platform endianness and all that mess.
            //Int32 length = data[offset + 3] + (data[offset + 2] << 8) + (data[offset + 1] << 16) + (data[offset] << 24);
            var length = (int)ArrayUtils.ReadIntFromByteArray(data.Slice(4, 4), true);
            // Sometimes has a byte 80 there? Some flag I guess...
            length = (int)((uint)length & 0x7FFFFFFF);
            if (length < 0 || length + 8 > data.Length)
                throw new FileTypeLoadException("Bad chunk size in Dynamix image.");
            return length;
        }

        public static byte[] GetChunkData(ReadOnlySpan<byte> data)
        {
            var dataLength = GetChunkDataLength(data);
            var returndata = new byte[dataLength];
            data.Slice(8, dataLength).CopyTo(returndata);
            return returndata;
        }
    }
}