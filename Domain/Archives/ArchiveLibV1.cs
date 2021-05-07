using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Nyerguds.Util;

namespace LibrarianTool.Domain.Archives
{
    public class ArchiveLibV1 : Archive
    {
        // "LIB" + 0x1A
        private static readonly Byte[] IdBytesLib = { 0x4C, 0x49, 0x42, 0x1A };

        public override String ShortTypeName { get { return "Mythos LIB Archive v1"; } }
        public override String ShortTypeDescription { get { return "Mythos LIB v1"; } }
        public override String[] FileExtensions { get { return new String[] { "LIB" }; } }
        
        protected override List<ArchiveEntry> LoadArchiveInternal(Stream loadStream, String archivePath)
        {
            Int32 files = this.GetFilesCount(loadStream, IdBytesLib);
            return this.LoadLibArchive(loadStream, files, archivePath);
        }

        protected Int32 GetFilesCount(Stream loadStream, Byte[] idBytes)
        {
            loadStream.Position = 0;
            if (loadStream.Length < idBytes.Length + 2)
                throw new FileTypeLoadException("Too short to be a " + this.ShortTypeDescription + " archive.");
            Byte[] testArray = new Byte[idBytes.Length];
            loadStream.Read(testArray, 0, testArray.Length);
            if (!testArray.SequenceEqual(idBytes))
                throw new FileTypeLoadException("Not a " + this.ShortTypeDescription + " archive.");
            Int32 files = loadStream.ReadByte() | (loadStream.ReadByte() << 8);
            if (files == 0)
                throw new FileTypeLoadException("No files in archive.");
            return files;
        }

        protected List<ArchiveEntry> LoadLibArchive(Stream loadStream, Int32 files, String archivePath)
        {
            Int64 end = loadStream.Length;
            Int32 fileEntries = files + 1;
            Encoding enc = Encoding.GetEncoding(437);
            const Int32 fileEntryLength = 0x11;
            Byte[] buffer = new Byte[fileEntryLength];
            String previousEntryName = null;
            Int32 previousEntryStart = 0;
            List<ArchiveEntry> filesList = new List<ArchiveEntry>();
            //Console.Write("Reading archive entries...");
            for (Int32 i = 0; i < fileEntries; i++)
            {
                if (loadStream.Position + fileEntryLength > end)
                    throw new FileTypeLoadException("File too short for full header.");
                loadStream.Read(buffer, 0, fileEntryLength);
                String curName = enc.GetString(buffer.Take(13).TakeWhile(x => x != 0).ToArray()).Trim();
                Int32 curEntryStart = (buffer[0x0D]) | (buffer[0x0E] << 8) | (buffer[0x0F] << 0x10) | (buffer[0x10] << 0x18);
                Boolean isEnd = curEntryStart == end;
                if (curEntryStart > end || curEntryStart < 0)
                    throw new FileTypeLoadException("Archive entry outside file bounds!");
                if (!String.IsNullOrEmpty(previousEntryName) && previousEntryStart != 0)
                {
                    Int32 entryLength = curEntryStart - previousEntryStart;
                    ArchiveEntry archiveEntry = new ArchiveEntry(previousEntryName, archivePath, previousEntryStart, entryLength);
                    filesList.Add(archiveEntry);
                }
                else if (i != 0)
                    throw new FileTypeLoadException("Empty archive entry! Aborting");
                if (isEnd)
                    break;
                previousEntryName = curName;
                previousEntryStart = curEntryStart;
            }
            filesList = filesList.OrderBy(x => x.FileName).ToList();
            return filesList;
        }

        public override Boolean SaveArchive(Archive archive, Stream saveStream, String savePath)
        {
            this.SaveHeader(archive, saveStream, IdBytesLib);
            return this.SaveLibArchive(archive, saveStream);
        }

        protected void SaveHeader(Archive archive, Stream saveStream, Byte[] idBytes)
        {
            saveStream.Write(idBytes, 0, idBytes.Length);
            Byte[] numBuf = new Byte[2];
            ArrayUtils.WriteIntToByteArray(numBuf, 0, 2, true, (UInt32)archive.FilesList.Count);
            saveStream.Write(numBuf, 0, 2);
        }

        protected Boolean SaveLibArchive(Archive archive, Stream saveStream)
        {
            ArchiveEntry[] entries = archive.FilesList.ToArray();
            const Int32 fileEntryLength = 0x11;
            Byte[] buffer = new Byte[fileEntryLength];
            Encoding enc = Encoding.GetEncoding(437);
            Int32 curEntryStart = (Int32)saveStream.Position + (entries.Length + 1) * buffer.Length;
            foreach (ArchiveEntry entry in entries)
            {
                Int32 fileLength = entry.Length;
                if (entry.PhysicalPath != null)
                {
                    FileInfo fi = new FileInfo(entry.PhysicalPath);
                    if (!fi.Exists)
                        throw new FileNotFoundException("Cannot find file \"" + entry.PhysicalPath + "\" to write to archive!");
                    fileLength = (Int32) fi.Length;
                }
                String curName = this.GetInternalFilename(entry.FileName);
                Int32 copySize = Math.Min(curName.Length, 12);
                Array.Copy(enc.GetBytes(curName), 0, buffer, 0, copySize);
                for (Int32 b = copySize; b <= 13; b++)
                    buffer[b] = 0;
                ArrayUtils.WriteIntToByteArray(buffer, 13, 4, true, (UInt32) curEntryStart);
                curEntryStart += fileLength;
                saveStream.Write(buffer, 0, fileEntryLength);
            }
            for (Int32 b = 0; b < 13; b++)
                buffer[b] = 0;
            ArrayUtils.WriteIntToByteArray(buffer, 13, 4, true, (UInt32)curEntryStart);
            saveStream.Write(buffer, 0, fileEntryLength);
            foreach (ArchiveEntry entry in entries)
                CopyEntryContentsToStream(entry, saveStream);
            return true;
        }

    }
}