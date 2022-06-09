using Nyerguds.Util;
using System;

namespace Nyerguds.GameData.Dynamix
{
    /// <summary>
    /// The Dynamix LZW decompression class.
    /// </summary>
    public class DynamixLzwDecoder
    {
        // Current code's string
        readonly byte[] codeCur = new byte[256];
        // Length of the current code
        int codeLen;
        // Amount of bits in the current code
        int codeSize;
        // cache chunks; 8 times the code size. Unsure what uses this except the reset.
        int cacheBits;

        // The "strings" of the dictionary table
        byte[][] dictTableStr;
        // lengths of the "strings" in the dictionary table
        byte[] dictTableLen;

        // Current dictionary size
        int dictSize;
        // Current dictionary maximum before the codeSize needs to be increased.
        int dictMax;
        // True if no more codes can be added.
        bool dictFull;

        void LzwReset()
        {
            dictTableStr = new byte[0x4000][];
            dictTableLen = new byte[0x4000];
            //for (Int32 i = 256; i < this.dictTableStr.Length; i++)
            //    dictTableStr[i] = new Byte[100];
            for (var lcv = 0; lcv < 256; lcv++)
            {
                dictTableLen[lcv] = 1;
                dictTableStr[lcv] = new[] { (byte)lcv };
            }
            // 00-FF = ASCII
            // 100 = reset
            dictSize = 0x101;
            dictMax = 0x200;
            dictFull = false;
            // start = 9 bit codes
            codeSize = 9;
            codeLen = 0;
            // 9-12 byte cache chunks
            cacheBits = 0;
        }

        public void LzwDecode(ReadOnlySpan<byte> buffer, int? startOffset, int? endOffset, Span<byte> bufferOut)
        {
            var inPtr = startOffset ?? 0;
            var bitIndex = inPtr * 8;
            var inPtrEnd = endOffset.HasValue ? Math.Min(endOffset.Value, buffer.Length) : buffer.Length;
            var outPtr = 0;
            LzwReset();
            cacheBits = 0;
            while (outPtr < bufferOut.Length)
            {
                // get next code
                //Int32 code = GetBitsRight(this.codeSize, buffer, inPtrEnd, ref inPtr);
                var code = ArrayUtils.ReadBitsFromByteArray(buffer, ref bitIndex, codeSize, inPtrEnd);

                if (code == -1)
                    return;
                // refresh data cache
                cacheBits += codeSize;
                if (cacheBits >= codeSize * 8)
                    cacheBits -= codeSize * 8;
                // reset: used when the codes are full and new codes are needed?
                if (code == 0x100)
                {
                    // Dynamix: dump data cache
                    if (cacheBits > 0)
                    {
                        var ignoreBits = codeSize * 8 - cacheBits;
                        ArrayUtils.ReadBitsFromByteArray(buffer, ref bitIndex, ignoreBits, inPtrEnd);
                    }
                    LzwReset();
                    continue;
                }
                // special case: expand for new entry
                if (code >= dictSize && !dictFull)
                {
                    codeCur[codeLen++] = codeCur[0];
                    // write output - future expanded string
                    for (uint codelen = 0; codelen < codeLen; codelen++)
                        //for (lastCodeValue = 0; lastCodeValue < this.codeLen; lastCodeValue++)
                        bufferOut[outPtr++] = codeCur[codelen];
                }
                else
                {
                    // write output
                    int len = dictTableLen[code];
                    for (uint codelen = 0; codelen < len; codelen++)
                        //for (lastCodeValue = 0; lastCodeValue < len; lastCodeValue++)
                        bufferOut[outPtr++] = dictTableStr[code][codelen];
                    // expand current string
                    codeCur[codeLen++] = dictTableStr[code][0];
                }
                if (codeLen < 2)
                    continue;
                // add to dictionary (2+ bytes only)
                if (!dictFull)
                {
                    int lastCodeValue;
                    // check full condition
                    if (dictSize == dictMax && codeSize == 12)
                    {
                        dictFull = true;
                        lastCodeValue = dictSize;
                    }
                    else
                    {
                        lastCodeValue = dictSize++;
                        cacheBits = 0;
                    }
                    // expand dictionary (adaptive LZW)
                    if (dictSize == dictMax && codeSize < 12)
                    {
                        dictMax *= 2;
                        codeSize++;
                    }
                    // add new entry
                    dictTableStr[lastCodeValue] = new byte[codeLen];
                    for (uint codelen = 0; codelen < codeLen; codelen++)
                        dictTableStr[lastCodeValue][codelen] = codeCur[codelen];
                    dictTableLen[lastCodeValue] = (byte)codeLen;
                }
                // reset to current code.
                for (uint codelen = 0; codelen < dictTableLen[code]; codelen++)
                    //for (lastCodeValue = 0; lastCodeValue < this.dictTableLen[code]; lastCodeValue++)
                    codeCur[codelen] = dictTableStr[code][codelen];
                codeLen = dictTableLen[code];
            }
        }
    }
}