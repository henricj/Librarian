using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Nyerguds.GameData.Dynamix;
using Nyerguds.Util;

namespace LibrarianTool.Domain.Archives
{
    public class ArchiveDynV2 : Archive
    {
        // "OPN:"
        private static readonly Byte[] DynamixIDBytes2 = { 0x4F, 0x50, 0x4E, 0x3A };
        private static Encoding Enc = Encoding.GetEncoding(437);

        public override String ShortTypeName { get { return "Dynamix Archive v2"; } }
        public override String ShortTypeDescription { get { return "Dynamix Archive v2"; } }
        public override String[] FileExtensions { get { return new String[] { "000", "001", "002", "003", "004", "005", "006", "007", "008", "009" }; } }
        public override Boolean CanSave { get { return false; } }
        public override Boolean SupportsFolders { get { return true; } }

        protected override List<ArchiveEntry> LoadArchiveInternal(Stream loadStream, String archivePath)
        {
            Int64 end = loadStream.Length;
            //Boolean isArchive = false;
            List<ArchiveEntry> filesList = new List<ArchiveEntry>();
            while (loadStream.Position < end)
            {
                Byte[] fileContents;
                ArchiveEntry fe = ReadFileFromStream(loadStream, archivePath, -1, out fileContents);
                filesList.Add(fe);
            }
            return filesList;
        }

        private ArchiveEntry ReadFileFromStream(Stream loadStream, String archivePath, Int32 currentChunkLength, out Byte[] uncompressedData)
        {
            if (currentChunkLength == -1)
            {
                // Read chunk header
                Byte[] idBuffer = new Byte[DynamixIDBytes2.Length];
                Int32 count = loadStream.Read(idBuffer, 0, idBuffer.Length);
                if (count != idBuffer.Length || !DynamixIDBytes2.SequenceEqual(idBuffer))
                    throw new FileTypeLoadException("Not a Dynamix v2 archive.");
                // Read chunk length
                Byte[] lenBuffer = new Byte[4];
                if (loadStream.Read(lenBuffer, 0, 4) != 4)
                    throw new FileTypeLoadException("Archive entry outside file bounds.");
                lenBuffer[3] &= 0x7F; // Remove archive bit
                currentChunkLength = (Int32) ArrayUtils.ReadIntFromByteArray(lenBuffer, 0, 4, true);
            }
            Int32 currentChunkStart = (Int32)loadStream.Position;
            //Int32 currentChunkEnd = currentChunkStart + currentChunkLength;
            //Read compression:
            Byte[] currentChunk = new Byte[currentChunkLength];
            if (loadStream.Read(currentChunk, 0, currentChunkLength) != currentChunkLength)
                throw new FileTypeLoadException("Archive entry outside file bounds.");
            Byte compression = currentChunk.Length == 0 ? (Byte)0 : currentChunk[0];
            uncompressedData = DynamixCompression.DecodeChunk(currentChunk);
            String curName = Enc.GetString(uncompressedData.TakeWhile(x => x != 0).ToArray());
            Int32 curNameLen = curName.Length + 1;
            ArchiveEntry fe = new ArchiveEntry(curName, archivePath, currentChunkStart, currentChunkLength);
            String compressionStr;
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
        public override Boolean ExtractFile(ArchiveEntry entry, String savePath)
        {
            if (entry == null)
                return false;
            String folder = Path.GetDirectoryName(savePath);
            if (entry.IsFolder)
            {
                Directory.CreateDirectory(savePath);
            }
            else
            {
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);
                using (FileStream fs = new FileStream(savePath, FileMode.Create))
                    ExtractContents(entry, fs);
            }
            return true;
        }

        private void ExtractContents(ArchiveEntry entry, Stream saveStream)
        {
            String readFile;
            if (entry.PhysicalPath != null)
            {
                // Copy actual file
                readFile = entry.PhysicalPath;
                FileInfo fi = new FileInfo(entry.PhysicalPath);
                using (FileStream fs = new FileStream(readFile, FileMode.Open, FileAccess.Read))
                {
                    fs.Seek(0, SeekOrigin.Begin);
                    CopyStream(fs, saveStream, fi.Length);
                }
            }
            else
            {
                // Uncompress from archive.
                readFile = entry.ArchivePath;
                using (FileStream fs = new FileStream(readFile, FileMode.Open, FileAccess.Read))
                {
                    fs.Seek(entry.StartOffset, SeekOrigin.Begin);
                    Byte[] uncompressedData;
                    ArchiveEntry fe = ReadFileFromStream(fs, String.Empty, entry.Length, out uncompressedData);
                    Int32 uncStart = fe.FileName.Length + 1;
                    Int32 uncLength = uncompressedData.Length - uncStart;
                    // Skip file name
                    using (MemoryStream ms = new MemoryStream(uncompressedData, uncStart, uncLength))
                        CopyStream(ms, saveStream, uncLength);
                }
            }
        }

        public override Boolean SaveArchive(Archive archive, Stream saveStream, String savePath)
        {
            throw new NotImplementedException();
        }

    }
}