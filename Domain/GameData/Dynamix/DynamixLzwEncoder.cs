using Nyerguds.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nyerguds.GameData.Dynamix
{
    /// <summary>
    /// LZW compression class. Experimental.
    /// </summary>
    public class DynamixLzwEncoder
    {
        readonly List<byte[]> dictKeys = new();
        readonly List<int> dictCodes = new();


        int GetCode(byte[] sequence)
        {
            var seqLen = sequence.Length;
            if (seqLen == 1)
                return sequence[0];
            // 256 is not used; it's the "reset" code.
            for (var i = 257; i < dictKeys.Count; i++)
            {
                var check = dictKeys[i];
                if (seqLen != check.Length)
                    continue;
                var noMatch = false;
                for (var bi = 0; bi < check.Length; bi++)
                {
                    if (sequence[bi] == check[bi])
                        continue;
                    noMatch = true;
                    break;
                }
                if (noMatch)
                    continue;
                return i;
            }
            return -1;
        }

        bool ContainsCode(byte[] sequence)
        {
            return GetCode(sequence) != -1;
        }

        public DynamixLzwEncoder()
        {
            for (var i = 0; i < 256; i++)
            {
                dictKeys.Add(new[] { (byte)i });
                dictCodes.Add(i);
            }
            // Reset code
            dictKeys.Add(null);
            dictCodes.Add(256);
        }

        public byte[] Compress(ReadOnlySpan<byte> buffer)
        {
            var codeLen = 9;
            var bitIndex = 0;
            var outbuffSize = buffer.Length * 2;
            var outbuff = new byte[outbuffSize];
            var addedSize = 0;
            for (var i = 0; i < buffer.Length; i++)
            {
                var b = buffer[i];
                // increase code length to amount of bits needed by intCode.
                ArrayUtils.WriteBitsToByteArray(outbuff, bitIndex, codeLen, b);
                bitIndex += codeLen;
                if (((i + addedSize + 1) % 24) != 23)
                    continue;
                ArrayUtils.WriteBitsToByteArray(outbuff, bitIndex, codeLen, 0x100);
                bitIndex += codeLen;
                addedSize++;
            }
            var bufSize = (bitIndex + 7) / 8;
            var outbuf2 = new byte[bufSize];
            Array.Copy(outbuff, outbuf2, bufSize);
            return outbuf2;
        }


        public int[] CompressToInts(byte[] buffer)
        {
            var match = Array.Empty<byte>();
            var compressed = new int[(buffer.Length * 2) / 3];
            var index = 0;
            foreach (var b in buffer)
            {
                var oldLen = match.Length;
                var nextMatch = new byte[oldLen + 1];
                nextMatch[oldLen] = b;
                if (ContainsCode(nextMatch))
                    match = nextMatch;
                else
                {
                    var code = GetCode(match);
                    // Add current code to list
                    compressed[index++] = dictCodes[code];
                    // new sequence; add it to the dictionary
                    dictKeys.Add(nextMatch.ToArray());
                    dictCodes.Add(dictCodes.Count);
                    match = new[] { b };
                }
            }
            // write remaining output if necessary
            if (match.Length > 0)
            {
                var code = GetCode(match);
                compressed[index++] = dictCodes[code];
            }
            var finalCodes = new int[index];
            Array.Copy(compressed, 0, finalCodes, 0, index);
            return finalCodes;
        }
    }

}