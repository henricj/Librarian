using System;
using System.IO;

namespace Nyerguds.Util
{
    public static class StreamUtils
    {
        public static bool JumpToNextMatch(this Stream stream, byte[] searchBytes)
        {
            if (searchBytes == null)
                throw new ArgumentNullException(nameof(searchBytes));
            return JumpToNextMatch(stream, -1, searchBytes, -1);
        }

        public static bool JumpToNextMatch(this Stream stream, int position, byte[] searchBytes)
        {
            if (searchBytes == null)
                throw new ArgumentNullException(nameof(searchBytes));
            return JumpToNextMatch(stream, position, searchBytes, -1);
        }

        public static bool JumpToNextMatch(this Stream stream, byte[] searchBytes, int searchBufferLength)
        {
            return JumpToNextMatch(stream, -1, searchBytes, searchBufferLength);
        }

        /// <summary>
        /// Locates the next match of a byte sequence in a stream.
        /// When the sequence is located, the position of the stream is put at the start of the found match.
        /// If it is not located, the stream will be read to the end.
        /// </summary>
        /// <param name="stream">The stream to search.</param>
        /// <param name="position">Start position to search from. Use -1 to keep the original position.</param>
        /// <param name="searchBytes">Bytes to find.</param>
        /// <param name="searchBufferLength">Search buffer length. When -1 is given, this defaults to 64k, or 10 times the length of <ref>searchBytes</ref> if that is larger.</param>
        /// <returns>The location of the next patch in the stream, or -1 if no match was found.</returns>
        public static bool JumpToNextMatch(this Stream stream, long position, byte[] searchBytes, int searchBufferLength)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (position >= stream.Length)
                throw new ArgumentOutOfRangeException(nameof(position), "Offset beyond end of stream.");
            if (searchBytes == null)
                throw new ArgumentNullException(nameof(searchBytes));
            if (!stream.CanSeek)
                throw new ArgumentException("this stream does not allow seeking!");
            if (position != -1)
                stream.Position = position;
            var currentIndex = stream.Position;
            var matchLength = searchBytes.Length;
            if (matchLength == 0)
                return true;
            if (searchBufferLength == -1)
                searchBufferLength = Math.Max(0x10000, searchBytes.Length * 10);
            searchBufferLength = Math.Max(searchBufferLength, matchLength);
            var buff2Len = matchLength - 1;
            var skipLength = searchBufferLength - buff2Len;
            var buffer = new byte[searchBufferLength];
            var buffer2 = new byte[buff2Len];
            var searchLength = stream.Read(buffer, 0, searchBufferLength);
            var startByte = searchBytes[0];
            while (searchLength >= matchLength)
            {
                for (var i = 0; i < skipLength; i++)
                {
                    if (buffer[i] != startByte)
                        continue;
                    var currentPos = i + 1;
                    int foundIndex;
                    for (foundIndex = 1; foundIndex < matchLength; foundIndex++)
                        if (buffer[currentPos++] != searchBytes[foundIndex])
                            break;
                    if (foundIndex != matchLength)
                        continue;
                    stream.Position = currentIndex + i;
                    return true;
                }
                if (searchLength < searchBufferLength)
                    break;
                Array.Copy(buffer, skipLength, buffer2, 0, buff2Len);
                Array.Copy(buffer2, 0, buffer, 0, buff2Len);
                currentIndex += skipLength;
                searchLength = stream.Read(buffer, buff2Len, skipLength) + buff2Len;
            }
            return false;
        }

        /// <summary>
        /// Checks if the bytes following the current position in a stream match the given byte array.
        /// When the check is done, the position of the stream is reset to the start position.
        /// This operation requires the stream to be seekable.
        /// </summary>
        /// <param name="stream">Stream to check.</param>
        /// <param name="toMatch">Bytes to match.</param>
        /// <returns>True if the bytes following the current position match the bytes in the given array.</returns>
        public static bool MatchAtCurrentPos(this Stream stream, byte[] toMatch)
        {
            return MatchAtPos(stream, -1, toMatch);
        }

        /// <summary>
        /// Checks if the bytes following the given position in a stream match the given byte array.
        /// When the check is done, the position of the stream is reset to the start position.
        /// This operation requires the stream to be seekable.
        /// </summary>
        /// <param name="stream">The stream to check.</param>
        /// <param name="position">Position. Use -1 to keep the original position.</param>
        /// <param name="toMatch">Bytes to match.</param>
        /// <returns>True if the bytes following the specified position match the bytes in the given array.</returns>
        public static bool MatchAtPos(this Stream stream, long position, byte[] toMatch)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            var streamLen = stream.Length;
            if (position >= streamLen)
                throw new ArgumentOutOfRangeException(nameof(position), "Position beyond end of stream.");
            if (position < -1)
                throw new ArgumentOutOfRangeException(nameof(position), "Position is a negative number.");
            if (toMatch == null)
                throw new ArgumentNullException(nameof(toMatch));
            if (position != -1)
                stream.Position = position;
            else
                position = stream.Position;
            var matchLength = toMatch.Length;
            if (matchLength == 0)
                return true;
            if (position + matchLength >= streamLen)
                return false;
            var checkArr = new byte[toMatch.Length];
            var readLen = stream.Read(checkArr, 0, matchLength);
            stream.Position = position;
            if (readLen != matchLength)
                return false;
            for (var i = 0; i < matchLength; ++i)
                if (checkArr[i] != toMatch[i])
                    return false;
            return true;
        }

    }
}