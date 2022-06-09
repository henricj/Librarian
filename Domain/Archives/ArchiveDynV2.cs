using Nyerguds.GameData.Dynamix;
using Nyerguds.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LibrarianTool.Domain.Archives
{
    public class ArchiveDynV2 : Archive
    {
        // "OPN:"
        static readonly byte[] DynamixIDBytes2 = { 0x4F, 0x50, 0x4E, 0x3A };
        static readonly Encoding Enc = Encoding.GetEncoding(437);

        public override string ShortTypeName => "Dynamix Archive v2";
        public override string ShortTypeDescription => "Dynamix Archive v2";
        public override string[] FileExtensions { get { return new[] { "000", "001", "002", "003", "004", "005", "006", "007", "008", "009" }; } }
        public override bool CanSave => false;
        public override bool SupportsFolders => true;

        protected override List<ArchiveEntry> LoadArchiveInternal(Stream loadStream, string archivePath)
        {
            var end = loadStream.Length;
            //Boolean isArchive = false;
            var filesList = new List<ArchiveEntry>();
            while (loadStream.Position < end)
            {
                var fe = ReadFileFromStream(loadStream, archivePath, -1, out var fileContents);
                filesList.Add(fe);
            }
            return filesList;
        }

        ArchiveEntry ReadFileFromStream(Stream loadStream, string archivePath, int currentChunkLength, out byte[] uncompressedData)
        {
            if (currentChunkLength == -1)
            {
                // Read chunk header
                Span<byte> idBuffer = stackalloc byte[DynamixIDBytes2.Length];
                var count = loadStream.Read(idBuffer);
                if (count != idBuffer.Length || !idBuffer.SequenceEqual(DynamixIDBytes2))
                    throw new FileTypeLoadException("Not a Dynamix v2 archive.");
                // Read chunk length
                Span<byte> lenBuffer = stackalloc byte[4];
                if (loadStream.Read(lenBuffer) != 4)
                    throw new FileTypeLoadException("Archive entry outside file bounds.");
                lenBuffer[3] &= 0x7F; // Remove archive bit
                currentChunkLength = (int)ArrayUtils.ReadIntFromByteArray(lenBuffer, true);
            }
            var currentChunkStart = (int)loadStream.Position;
            //Int32 currentChunkEnd = currentChunkStart + currentChunkLength;
            //Read compression:
            var currentChunk = new byte[currentChunkLength];
            if (loadStream.Read(currentChunk, 0, currentChunkLength) != currentChunkLength)
                throw new FileTypeLoadException("Archive entry outside file bounds.");
            var compression = currentChunk.Length == 0 ? (byte)0 : currentChunk[0];
            uncompressedData = DynamixCompression.DecodeChunk(currentChunk);
            var curName = Enc.GetString(uncompressedData.TakeWhile(x => x != 0).ToArray());
            var curNameLen = curName.Length + 1;
            var fe = new ArchiveEntry(curName, archivePath, currentChunkStart, currentChunkLength);
            string compressionStr;
            switch (compression)
            {
                case 0: compressionStr = "Uncompressed"; break;
                case 1: compressionStr = "RLE"; break;
                case 2: compressionStr = "LZW"; break;
                case 3: compressionStr = "LZSS"; break;
                default: compressionStr = "Unknown"; break;
            }
            fe.ExtraInfo = "Compression: " + compressionStr + "\nUncompressed size: " + (uncompressedData.Length - curNameLen);
            //loadStream.Position = currentChunkEnd;
            return fe;
        }


        /// <summary>Extracts the requested file from the _filesList list.</summary>
        /// <param name="entry">Name of the file to extract.</param>
        /// <param name="savePath">Path to save the file to.</param>
        /// <returns></returns>
        public override bool ExtractFile(ArchiveEntry entry, string savePath)
        {
            if (entry == null)
                return false;
            var folder = Path.GetDirectoryName(savePath);
            if (entry.IsFolder)
            {
                Directory.CreateDirectory(savePath);
            }
            else
            {
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);
                using var fs = new FileStream(savePath, FileMode.Create);
                ExtractContents(entry, fs);
            }
            return true;
        }

        void ExtractContents(ArchiveEntry entry, Stream saveStream)
        {
            string readFile;
            if (entry.PhysicalPath != null)
            {
                // Copy actual file
                readFile = entry.PhysicalPath;
                var fi = new FileInfo(entry.PhysicalPath);
                using var fs = new FileStream(readFile, FileMode.Open, FileAccess.Read);
                fs.Seek(0, SeekOrigin.Begin);
                CopyStream(fs, saveStream, fi.Length);
            }
            else
            {
                // Uncompress from archive.
                readFile = entry.ArchivePath;
                using var fs = new FileStream(readFile, FileMode.Open, FileAccess.Read);
                fs.Seek(entry.StartOffset, SeekOrigin.Begin);
                var fe = ReadFileFromStream(fs, string.Empty, entry.Length, out var uncompressedData);
                var uncStart = fe.FileName.Length + 1;
                var uncLength = uncompressedData.Length - uncStart;
                // Skip file name
                using var ms = new MemoryStream(uncompressedData, uncStart, uncLength);
                CopyStream(ms, saveStream, uncLength);
            }
        }

        public override bool SaveArchive(Archive archive, Stream saveStream, string savePath)
        {
            throw new NotImplementedException();
        }

    }
}