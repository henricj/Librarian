using System;
using System.IO;

namespace Nyerguds.GameData.Compression
{
    /// <summary>
    /// LZW-based compressor/decompressor - basic algorithm used as described on Mark Nelson's website: http://marknelson.us
    /// Based on the C# translation by Github user pevillarreal: https://github.com/pevillarreal/LzwCompressor
    /// The code was adapted by Maarten Meuris aka Nyerguds to fix the 7-bit filter caused by using an ASCII stream reader.
    /// This version uses byte arrays as input and output, and uses an enum to switch to different supported bit lengths.
    /// </summary>
    public class LzwCompression
    {
        readonly int BITS; // maximum bits allowed to read
        readonly int HASHING_SHIFT; // hash bit to use with the hasing algorithm to find correct index
        readonly int MAX_VALUE; // max value allowed based on max bits
        readonly int MAX_CODE; // max code possible
        readonly int TABLE_SIZE; // must be bigger than the maximum allowed by maxbits and prime

        readonly int[] code_value; // code table
        readonly int[] prefix_code; // prefix table
        readonly int[] append_character; // character table

        ulong input_bit_buffer; // bit buffer to temporarily store bytes read from the files
        int input_bit_count; // counter for knowing how many bits are in the bit buffer

        public LzwCompression(LzwSize bitSize)
        {
            switch (bitSize)
            {
                case LzwSize.Size12Bit:
                case LzwSize.Size13Bit:
                case LzwSize.Size14Bit:
                    this.BITS = (int)bitSize;
                    break;
                default:
                    throw new ArgumentException("Unsupported bit size!", nameof(bitSize));
            }
            this.HASHING_SHIFT = this.BITS - 8; // hash bit to use with the hasing algorithm to find correct index
            this.MAX_VALUE = (1 << this.BITS) - 1; // max value allowed based on max bits
            this.MAX_CODE = this.MAX_VALUE - 1; // max code possible
            // TABLE_SIZE must be bigger than the maximum allowed by maxbits and prime
            switch (bitSize)
            {
                case LzwSize.Size12Bit:
                    this.TABLE_SIZE = 5021;
                    break;
                case LzwSize.Size13Bit:
                    this.TABLE_SIZE = 9029;
                    break;
                case LzwSize.Size14Bit:
                    this.TABLE_SIZE = 18041;
                    break;
            }
            this.code_value = new int[this.TABLE_SIZE]; // code table
            this.prefix_code = new int[this.TABLE_SIZE]; // prefix table
            this.append_character = new int[this.TABLE_SIZE]; // character table
        }

        void Initialize() // used to blank  out bit buffer incase this class is called to comprss and decompress from the same instance
        {
            this.input_bit_buffer = 0;
            this.input_bit_count = 0;
        }

        public byte[] Compress(byte[] inputBuffer)
        {
            byte[] outputBuffer;
            using var inStream = new MemoryStream(inputBuffer);
            using var outStream = new MemoryStream();
            try
            {
                this.Initialize();
                var next_code = 256;
                int character;
                for (var i = 0; i < this.TABLE_SIZE; i++) // blank out table
                    this.code_value[i] = -1;
                var string_code = inStream.ReadByte();
                while ((character = inStream.ReadByte()) != -1) // read until we reach end of file
                {
                    var index = this.FindMatch(string_code, character);
                    if (this.code_value[index] != -1) // set string if we have something at that index
                        string_code = this.code_value[index];
                    else // insert new entry
                    {
                        if (next_code <= this.MAX_CODE) // otherwise we insert into the tables
                        {
                            this.code_value[index] = next_code++; // insert and increment next code to use
                            this.prefix_code[index] = string_code;
                            this.append_character[index] = (byte)character;
                        }
                        this.OutputCode(outStream, string_code); // output the data in the string
                        string_code = character;
                    }
                }
                this.OutputCode(outStream, string_code); // output last code
                this.OutputCode(outStream, this.MAX_VALUE); // output end of buffer
                this.OutputCode(outStream, 0); // flush
                outputBuffer = outStream.ToArray();
            }
            catch (Exception)
            {
                return null;
            }

            return outputBuffer;
        }

        // hasing function, tries to find index of prefix+char, if not found returns -1 to signify space available
        int FindMatch(int hash_prefix, int hash_character)
        {
            var index = (hash_character << this.HASHING_SHIFT) ^ hash_prefix;
            var offset = (index == 0) ? 1 : this.TABLE_SIZE - index;
            while (true)
            {
                if (this.code_value[index] == -1)
                    return index;
                if (this.prefix_code[index] == hash_prefix && this.append_character[index] == hash_character)
                    return index;
                index -= offset;
                if (index < 0)
                    index += this.TABLE_SIZE;
            }
        }

        public byte[] Decompress(byte[] inputBuffer, int startOffset, int length)
        {
            byte[] outputBuffer;
            using (var inStream = new MemoryStream(inputBuffer))
            using (var outStream = new MemoryStream())
            {
                try
                {
                    this.Initialize();
                    var next_code = 256;
                    var decode_stack = new byte[this.TABLE_SIZE];
                    inStream.Seek(startOffset, SeekOrigin.Begin);
                    var old_code = this.input_code(inStream);
                    var character = (byte)old_code;
                    outStream.WriteByte((byte)old_code); // write first Byte since it is plain ascii
                    var new_code = this.input_code(inStream);
                    while (new_code != this.MAX_VALUE) // read file all file
                    {
                        int code;
                        int iCounter;
                        if (new_code >= next_code)
                        {
                            // fix for prefix+chr+prefix+char+prefx special case
                            decode_stack[0] = character;
                            iCounter = 1;
                            code = old_code;
                        }
                        else
                        {
                            iCounter = 0;
                            code = new_code;
                        }
                        // decode_string
                        while (code > 255) // decode string by cycling back through the prefixes
                        {
                            decode_stack[iCounter] = (byte)this.append_character[code];
                            ++iCounter;
                            if (iCounter >= this.MAX_CODE)
                                throw new Exception("Decompression failed.");
                            code = this.prefix_code[code];
                        }
                        decode_stack[iCounter] = (byte)code;
                        character = decode_stack[iCounter]; // set last char used
                        while (iCounter >= 0) // write out decodestack
                        {
                            outStream.WriteByte(decode_stack[iCounter]);
                            --iCounter;
                        }
                        if (next_code <= this.MAX_CODE) // insert into tables
                        {
                            this.prefix_code[next_code] = old_code;
                            this.append_character[next_code] = character;
                            ++next_code;
                        }
                        old_code = new_code;
                        new_code = this.input_code(inStream);
                    }
                    outputBuffer = outStream.ToArray();
                }
                catch (EndOfStreamException)
                {
                    outputBuffer = outStream.ToArray();
                }
                catch (Exception)
                {
                    return null;
                }
            }
            if (outputBuffer.Length == length)
                return outputBuffer;
            var outputBuffer2 = new byte[length];
            Array.Copy(outputBuffer, 0, outputBuffer2, 0, Math.Min(outputBuffer.Length, outputBuffer2.Length));
            return outputBuffer2;
        }

        int input_code(MemoryStream pReader)
        {
            while (this.input_bit_count <= 24) // fill up buffer
            {
                this.input_bit_buffer |= (ulong)pReader.ReadByte() << (24 - this.input_bit_count); // insert Byte into buffer
                this.input_bit_count += 8; // increment counter
            }
            var return_value = (uint)this.input_bit_buffer >> (32 - this.BITS);
            this.input_bit_buffer <<= this.BITS; // remove it from buffer
            this.input_bit_count -= this.BITS; // decrement bit counter
            var temp = (int)return_value;
            return temp;
        }

        void OutputCode(MemoryStream output, int code)
        {
            this.input_bit_buffer |= (ulong)code << (32 - this.BITS - this.input_bit_count); // make space and insert new code in buffer
            this.input_bit_count += this.BITS; // increment bit counter
            while (this.input_bit_count >= 8) // write all the bytes we can
            {
                output.WriteByte((byte)((this.input_bit_buffer >> 24) & 255)); // write Byte from bit buffer
                this.input_bit_buffer <<= 8; // remove written Byte from buffer
                this.input_bit_count -= 8; // decrement counter
            }
        }
    }

    /// <summary>
    /// Bit lengths supported by the LZWCompression class.
    /// </summary>
    public enum LzwSize
    {
        Size12Bit = 12,
        Size13Bit = 13,
        Size14Bit = 14,
    }
}