using System;

namespace Nyerguds.GameData.Dynamix
{
    public class DynamixLzHuffDecoder
    {
        /**************************************************************
        lzhuf.c
        written by Haruyasu Yoshizaki 1988/11/20
        some minor changes 1989/04/06
        comments translated by Haruhiko Okumura 1989/04/07
        getbit and getbyte modified 1990/03/23 by Paul Edwards
          so that they would work on machines where integers are
          not necessarily 16 bits (although ANSI guarantees a
          minimum of 16).  This program has compiled and run with
          no errors under Turbo C 2.0, Power C, and SAS/C 4.5
          (running on an IBM mainframe under MVS/XA 2.2).  Could
          people please use YYYY/MM/DD date format so that everyone
          in the world can know what format the date is in?
        external storage of filesize changed 1990/04/18 by Paul Edwards to
          Intel's "little endian" rather than a machine-dependant style so
          that files produced on one machine with lzhuf can be decoded on
          any other.  "little endian" style was chosen since lzhuf
          originated on PC's, and therefore they should dictate the
          standard.
        initialization of something predicting spaces changed 1990/04/22 by
          Paul Edwards so that when the compressed file is taken somewhere
          else, it will decode properly, without changing ascii spaces to
          ebcdic spaces.  This was done by changing the ' ' (space literal)
          to 0x20 (which is the far most likely character to occur, if you
          don't know what environment it will be running on.
    **************************************************************/

        // Thanks to: NewRisingSun

        /********** LZSS compression **********/

        const int N = 4096; /* buffer size */
        const int F = 60; /* lookahead buffer size */
        const int Threshold = 2;
        const int Nil = N; /* leaf of tree */

        readonly byte[] _textBuf = new byte[N + F - 1];
        int _matchPosition;
        int _matchLength;
        readonly int[] _lson = new int[N + 1];
        readonly int[] _rson = new int[N + 257];
        readonly int[] _dad = new int[N + 1];

        void InitTree() /* initialize trees */
        {
            int i;

            for (i = N + 1; i <= N + 256; i++)
                this._rson[i] = Nil; /* root */
            for (i = 0; i < N; i++)
                this._dad[i] = Nil; /* node */
        }

        void InsertNode(int r) /* insert to tree */
        {
            var cmp = 1;
            var key = r;
            var p = N + 1 + this._textBuf[key];
            this._rson[r] = this._lson[r] = Nil;
            this._matchLength = 0;
            for (; ; )
            {
                if (cmp >= 0)
                {
                    if (this._rson[p] != Nil)
                        p = this._rson[p];
                    else
                    {
                        this._rson[p] = r;
                        this._dad[r] = p;
                        return;
                    }
                }
                else
                {
                    if (this._lson[p] != Nil)
                        p = this._lson[p];
                    else
                    {
                        this._lson[p] = r;
                        this._dad[r] = p;
                        return;
                    }
                }
                int i;
                for (i = 1; i < F; i++)
                    if ((cmp = this._textBuf[key + i] - this._textBuf[p + i]) != 0)
                        break;
                if (i > Threshold)
                {
                    if (i > this._matchLength)
                    {
                        this._matchPosition = ((r - p) & (N - 1)) - 1;
                        if ((this._matchLength = i) >= F)
                            break;
                    }
                    if (i == this._matchLength)
                    {
                        uint c;
                        if ((c = (uint)((r - p) & (N - 1)) - 1) < (uint)this._matchPosition)
                        {
                            this._matchPosition = (int)c;
                        }
                    }
                }
            }
            this._dad[r] = this._dad[p];
            this._lson[r] = this._lson[p];
            this._rson[r] = this._rson[p];
            this._dad[this._lson[p]] = r;
            this._dad[this._rson[p]] = r;
            if (this._rson[this._dad[p]] == p)
                this._rson[this._dad[p]] = r;
            else
                this._lson[this._dad[p]] = r;
            this._dad[p] = Nil; /* remove p */
        }

        void DeleteNode(int p) /* remove from tree */
        {
            int q;

            if (this._dad[p] == Nil)
                return; /* not registered */
            if (this._rson[p] == Nil)
                q = this._lson[p];
            else if (this._lson[p] == Nil)
                q = this._rson[p];
            else
            {
                q = this._lson[p];
                if (this._rson[q] != Nil)
                {
                    do
                    {
                        q = this._rson[q];
                    } while (this._rson[q] != Nil);
                    this._rson[this._dad[q]] = this._lson[q];
                    this._dad[this._lson[q]] = this._dad[q];
                    this._lson[q] = this._lson[p];
                    this._dad[this._lson[p]] = q;
                }
                this._rson[q] = this._rson[p];
                this._dad[this._rson[p]] = q;
            }
            this._dad[q] = this._dad[p];
            if (this._rson[this._dad[p]] == p)
                this._rson[this._dad[p]] = q;
            else
                this._lson[this._dad[p]] = q;
            this._dad[p] = Nil;
        }

        /* Huffman coding */

        const int NChar = (256 - Threshold + F);
        /* kinds of characters (character code = 0..N_CHAR-1) */
        const int T = (NChar * 2 - 1); /* size of table */
        const int R = (T - 1); /* position of root */
        const int MaxFreq = 0x8000; /* updates tree when the */

        /* table for decoding the upper 6 bits of position */

        /* for decoding */

        readonly byte[] _dCode =
                {
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
            0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
            0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02,
            0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02,
            0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03,
            0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03,
            0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04,
            0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05,
            0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06,
            0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07,
            0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08,
            0x09, 0x09, 0x09, 0x09, 0x09, 0x09, 0x09, 0x09,
            0x0A, 0x0A, 0x0A, 0x0A, 0x0A, 0x0A, 0x0A, 0x0A,
            0x0B, 0x0B, 0x0B, 0x0B, 0x0B, 0x0B, 0x0B, 0x0B,
            0x0C, 0x0C, 0x0C, 0x0C, 0x0D, 0x0D, 0x0D, 0x0D,
            0x0E, 0x0E, 0x0E, 0x0E, 0x0F, 0x0F, 0x0F, 0x0F,
            0x10, 0x10, 0x10, 0x10, 0x11, 0x11, 0x11, 0x11,
            0x12, 0x12, 0x12, 0x12, 0x13, 0x13, 0x13, 0x13,
            0x14, 0x14, 0x14, 0x14, 0x15, 0x15, 0x15, 0x15,
            0x16, 0x16, 0x16, 0x16, 0x17, 0x17, 0x17, 0x17,
            0x18, 0x18, 0x19, 0x19, 0x1A, 0x1A, 0x1B, 0x1B,
            0x1C, 0x1C, 0x1D, 0x1D, 0x1E, 0x1E, 0x1F, 0x1F,
            0x20, 0x20, 0x21, 0x21, 0x22, 0x22, 0x23, 0x23,
            0x24, 0x24, 0x25, 0x25, 0x26, 0x26, 0x27, 0x27,
            0x28, 0x28, 0x29, 0x29, 0x2A, 0x2A, 0x2B, 0x2B,
            0x2C, 0x2C, 0x2D, 0x2D, 0x2E, 0x2E, 0x2F, 0x2F,
            0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37,
            0x38, 0x39, 0x3A, 0x3B, 0x3C, 0x3D, 0x3E, 0x3F,
        };

        readonly byte[] _dLen =
                {
            0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03,
            0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03,
            0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03,
            0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03,
            0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04,
            0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04,
            0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04,
            0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04,
            0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04,
            0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04,
            0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05,
            0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05,
            0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05,
            0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05,
            0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05,
            0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05,
            0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05,
            0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05,
            0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06,
            0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06,
            0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06,
            0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06,
            0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06,
            0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06,
            0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07,
            0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07,
            0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07,
            0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07,
            0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07,
            0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07,
            0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08,
            0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08,
        };

        readonly uint[] _freq = new uint[T + 1]; /* frequency table */

        readonly int[] _prnt = new int[T + NChar]; /* pointers to parent nodes, except for the */
        /* elements [T..T + N_CHAR - 1] which are used to get */
        /* the positions of leaves corresponding to the codes. */

        readonly int[] _son = new int[T]; /* pointers to child nodes (son[], son[] + 1) */

        uint _getbuf;
        byte _getlen;

        int GetBit() /* get one bit */
        {
            uint i;
            while (this._getlen <= 8)
            {
                if ((int)(i = this.get_bits_left(8)) < 0)
                    i = 0;
                this._getbuf |= (i << (8 - this._getlen));
                this._getlen += 8;
            }
            i = this._getbuf;
            this._getbuf <<= 1;
            this._getlen--;
            return (int)((i & 0x8000) >> 15);
        }

        int GetByte() /* get one byte */
        {
            uint i;

            while (this._getlen <= 8)
            {
                if ((int)(i = this.get_bits_left(8)) < 0) i = 0;
                this._getbuf |= i << (8 - this._getlen);
                this._getlen += 8;
            }
            i = this._getbuf;
            this._getbuf <<= 8;
            this._getlen -= 8;
            return (int)((i & 0xff00) >> 8);
        }


        /* initialization of tree */

        void StartHuff()
        {


            for (var index = 0; index < NChar; index++)
            {
                this._freq[index] = 1;
                this._son[index] = index + T;
                this._prnt[index + T] = index;
            }
            var i = 0;
            var j = NChar;
            while (j <= R)
            {
                this._freq[j] = this._freq[i] + this._freq[i + 1];
                this._son[j] = i;
                this._prnt[i] = this._prnt[i + 1] = j;
                i += 2;
                j++;
            }
            this._freq[T] = 0xffff;
            this._prnt[R] = 0;
        }


        /* reconstruction of tree */

        void Reconst()
        {
            /* collect leaf nodes in the first half of the table */
            /* and replace the freq by (freq + 1) / 2. */

            var j = 0;
            int k;
            for (var i = 0; i < T; i++)
            {
                if (this._son[i] >= T)
                {
                    this._freq[j] = (this._freq[i] + 1) / 2;
                    this._son[j] = this._son[i];
                    j++;
                }
            }
            /* begin constructing tree by connecting sons */
            j = NChar;
            for (var i = 0; j < T; i += 2, j++)
            {
                k = i + 1;
                var f = this._freq[j] = this._freq[i] + this._freq[k];
                for (k = j - 1; f < this._freq[k]; k--) ;
                k++;
                var l = (uint)(j - k) * 2;
                Array.Copy(this._freq, k, this._freq, k + 1, l);
                this._freq[k] = f;
                Array.Copy(this._son, k, this._son, k + 1, l);
                this._son[k] = i;
            }
            /* connect prnt */
            for (var i = 0; i < T; i++)
            {
                if ((k = this._son[i]) >= T)
                {
                    this._prnt[k] = i;
                }
                else
                {
                    this._prnt[k] = this._prnt[k + 1] = i;
                }
            }
        }

        /* increment frequency of given code by one, and update tree */

        void Update(int c)
        {
            if (this._freq[R] == MaxFreq)
            {
                this.Reconst();
            }
            c = this._prnt[c + T];
            do
            {
                var k = (int)(++this._freq[c]);
                /* if the order is disturbed, exchange nodes */
                int l;
                if ((uint)k <= this._freq[l = c + 1])
                    continue;
                while ((uint)k > this._freq[++l]) ;
                l--;
                this._freq[c] = this._freq[l];
                this._freq[l] = (uint)k;

                var i = this._son[c];
                this._prnt[i] = l;
                if (i < T) this._prnt[i + 1] = l;

                var j = this._son[l];
                this._son[l] = i;

                this._prnt[j] = c;
                if (j < T) this._prnt[j + 1] = c;
                this._son[c] = j;

                c = l;
            } while ((c = this._prnt[c]) != 0); /* repeat up to root */
        }


        int DecodeByte()
        {
            var c = (uint)this._son[R];

            /* travel from root to leaf, */
            /* choosing the smaller child node (son[]) if the read bit is 0, */
            /* the bigger (son[]+1} if 1 */
            while (c < T)
            {
                c += (uint)this.GetBit();
                c = (uint)this._son[c];
            }
            c -= T;
            this.Update((int)c);
            return (int)c;
        }


        int DecodePosition()
        {
            uint i, j, c;

            /* recover upper 6 bits from table */
            i = (uint)this.GetByte();
            c = (uint)this._dCode[i] << 6;
            j = this._dLen[i];

            /* read lower 6 bits verbatim */
            j -= 2;
            while (j-- != 0)
            {
                i = (uint)((i << 1) + this.GetBit());
            }
            return (int)(c | (i & 0x3f));
        }


        void Reset()
        {
            this.InitTree();
            this._getlen = 0;
            this._getbuf = 0;
        }


        public byte[] Decode(byte[] input, int? startOffset, int? endOffset, int decompressedSize)
        {
            this.buf_in = input;
            this.buf_ptr = startOffset ?? 0;
            this.buf_end = endOffset ?? input.Length;
            this.bits_size = 0;
            this.bits_data = 0;


            uint outPtr = 0;
            var len = (uint)decompressedSize;
            var bufOut = new byte[decompressedSize];
            if (len == 0)
                return Array.Empty<byte>();
            this.Reset();
            this.StartHuff();
            for (var i = 0; i < N - F; i++)
                this._textBuf[i] = 0x20;
            var r = N - F;
            for (uint count = 0; count < len;)
            {
                if (outPtr >= decompressedSize)
                    return bufOut;
                var c = this.DecodeByte();
                if (c < 256)
                {
                    bufOut[outPtr++] = (byte)c;

                    this._textBuf[r++] = (byte)c;
                    r &= (N - 1);
                    count++;
                }
                else
                {
                    var i = (r - this.DecodePosition() - 1) & (N - 1);
                    var j = c - 255 + Threshold;
                    int k;
                    for (k = 0; k < j; k++)
                    {
                        c = this._textBuf[(i + k) & (N - 1)];
                        if (outPtr >= decompressedSize)
                            return bufOut;
                        bufOut[outPtr++] = (byte)c;
                        this._textBuf[r++] = (byte)c;
                        r &= (N - 1);
                        count++;
                    }
                }
            }
            return bufOut;
        }

        byte[] buf_in;
        int buf_ptr;
        int buf_end;
        byte bits_size;
        byte bits_data;

        uint get_bits_left(uint totalBits)
        {
            byte[] bitsMask =
            {
                0x00, 0x01, 0x03, 0x07, 0x0f,
                0x1f, 0x3f, 0x7f, 0xff
            };

            var numBits = totalBits;
            uint data = 0;

            while (numBits > 0)
            {
                // ERROR!
                if (this.buf_ptr >= this.buf_end)
                    return uint.MaxValue;

                // 8-bit buffer
                if (this.bits_size == 0)
                {
                    this.bits_size = 8;
                    this.bits_data = this.buf_in[this.buf_ptr++];
                }
                // consume cached bits
                var useBits = numBits;
                if (useBits > 8) useBits = 8;
                if (useBits > this.bits_size)
                    useBits = this.bits_size;

                // tack on bits
                data <<= (int)useBits;
                data |= (uint)((this.bits_data >> (int)(this.bits_size - useBits)) & bitsMask[useBits]);

                // update cache data
                numBits -= useBits;
                this.bits_size -= (byte)useBits;
            }
            return data;
        }
    }
}