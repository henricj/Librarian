using System;
using System.IO;

namespace Nyerguds.Util
{
    public static class StreamUtils
    {
        public static Boolean JumpToNextMatch(this Stream stream, Byte[] searchBytes)
        {
            if (searchBytes == null)
                throw new ArgumentNullException("searchBytes");
            return JumpToNextMatch(stream, -1, searchBytes, -1);
        }

        public static Boolean JumpToNextMatch(this Stream stream, Int32 position, Byte[] searchBytes)
        {
            if (searchBytes == null)
                throw new ArgumentNullException("searchBytes");
            return JumpToNextMatch(stream, position, searchBytes, -1);
        }

        public static Boolean JumpToNextMatch(this Stream stream, Byte[] searchBytes, Int32 searchBufferLength)
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
        public static Boolean JumpToNextMatch(this Stream stream, Int64 position, Byte[] searchBytes, Int32 searchBufferLength)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");
            if (position >= stream.Length)
                throw new ArgumentOutOfRangeException("position", "Offset beyond end of stream.");
            if (searchBytes == null)
                throw new ArgumentNullException("searchBytes");
            if (!stream.CanSeek)
                throw new ArgumentException("this stream does not allow seeking!");
            if (position != -1)
                stream.Position = position;
            Int64 currentIndex = stream.Position;
            Int32 matchLength = searchBytes.Length;
            if (matchLength == 0)
                return true;
            if (searchBufferLength == -1)
                searchBufferLength = Math.Max(0x10000, searchBytes.Length * 10);
            searchBufferLength = Math.Max(searchBufferLength, matchLength);
            Int32 buff2Len = matchLength - 1;
            Int32 skipLength = searchBufferLength - buff2Len;
            Byte[] buffer = new Byte[searchBufferLength];
            Byte[] buffer2 = new Byte[buff2Len];
            Int32 searchLength = stream.Read(buffer, 0, searchBufferLength);
            Byte startByte = searchBytes[0];
            while (searchLength >= matchLength)
            {
                for (Int32 i = 0; i < skipLength; i++)
                {
                    if (buffer[i] != startByte)
                        continue;
                    Int32 currentPos = i + 1;
                    Int32 foundIndex;
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
        public static Boolean MatchAtCurrentPos(this Stream stream, Byte[] toMatch)
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
        public static Boolean MatchAtPos(this Stream stream, Int64 position, Byte[] toMatch)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");
            Int64 streamLen = stream.Length;
            if (position >= streamLen)
                throw new ArgumentOutOfRangeException("position", "Position beyond end of stream.");
            if (position < -1)
                throw new ArgumentOutOfRangeException("position", "Position is a negative number.");
            if (toMatch == null)
                throw new ArgumentNullException("toMatch");
            if (position != -1)
                stream.Position = position;
            else
                position = stream.Position;
            Int32 matchLength = toMatch.Length;
            if (matchLength == 0)
                return true;
            if (position + matchLength >= streamLen)
                return false;
            Byte[] checkArr = new Byte[toMatch.Length];
            Int32 readLen = stream.Read(checkArr, 0, matchLength);
            stream.Position = position;
            if (readLen != matchLength)
                return false;
            for (Int32 i = 0; i < matchLength; ++i)
                if (checkArr[i] != toMatch[i])
                    return false;
            return true;
        }

    }
}