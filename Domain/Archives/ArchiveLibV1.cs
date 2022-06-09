using Nyerguds.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LibrarianTool.Domain.Archives
{
    public class ArchiveLibV1 : Archive
    {
        // "LIB" + 0x1A
        static readonly byte[] IdBytesLib = { 0x4C, 0x49, 0x42, 0x1A };

        public override string ShortTypeName => "Mythos LIB Archive v1";
        public override string ShortTypeDescription => "Mythos LIB v1";
        public override string[] FileExtensions { get { return new[] { "LIB" }; } }

        protected override List<ArchiveEntry> LoadArchiveInternal(Stream loadStream, string archivePath)
        {
            var files = this.GetFilesCount(loadStream, IdBytesLib);
            return LoadLibArchive(loadStream, files, archivePath);
        }

        protected int GetFilesCount(Stream loadStream, byte[] idBytes)
        {
            loadStream.Position = 0;
            if (loadStream.Length < idBytes.Length + 2)
                throw new FileTypeLoadException("Too short to be a " + this.ShortTypeDescription + " archive.");
            var testArray = new byte[idBytes.Length];
            loadStream.Read(testArray, 0, testArray.Length);
            if (!testArray.SequenceEqual(idBytes))
                throw new FileTypeLoadException("Not a " + this.ShortTypeDescription + " archive.");
            var files = loadStream.ReadByte() | (loadStream.ReadByte() << 8);
            if (files == 0)
                throw new FileTypeLoadException("No files in archive.");
            return files;
        }

        protected static List<ArchiveEntry> LoadLibArchive(Stream loadStream, int files, string archivePath)
        {
            var end = loadStream.Length;
            var fileEntries = files + 1;
            var enc = Encoding.GetEncoding(437);
            const int fileEntryLength = 0x11;
            var buffer = new byte[fileEntryLength];
            string previousEntryName = null;
            var previousEntryStart = 0;
            var filesList = new List<ArchiveEntry>();
            //Console.Write("Reading archive entries...");
            for (var i = 0; i < fileEntries; i++)
            {
                if (loadStream.Position + fileEntryLength > end)
                    throw new FileTypeLoadException("File too short for full header.");
                loadStream.Read(buffer, 0, fileEntryLength);
                var curName = enc.GetString(buffer.Take(13).TakeWhile(x => x != 0).ToArray()).Trim();
                var curEntryStart = (buffer[0x0D]) | (buffer[0x0E] << 8) | (buffer[0x0F] << 0x10) | (buffer[0x10] << 0x18);
                var isEnd = curEntryStart == end;
                if (curEntryStart > end || curEntryStart < 0)
                    throw new FileTypeLoadException("Archive entry outside file bounds!");
                if (!string.IsNullOrEmpty(previousEntryName) && previousEntryStart != 0)
                {
                    var entryLength = curEntryStart - previousEntryStart;
                    var archiveEntry = new ArchiveEntry(previousEntryName, archivePath, previousEntryStart, entryLength);
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

        public override bool SaveArchive(Archive archive, Stream saveStream, string savePath)
        {
            SaveHeader(archive, saveStream, IdBytesLib);
            return this.SaveLibArchive(archive, saveStream);
        }

        protected static void SaveHeader(Archive archive, Stream saveStream, byte[] idBytes)
        {
            saveStream.Write(idBytes, 0, idBytes.Length);
            var numBuf = new byte[2];
            ArrayUtils.WriteIntToByteArray(numBuf, 0, 2, true, (uint)archive.FilesList.Count);
            saveStream.Write(numBuf, 0, 2);
        }

        protected bool SaveLibArchive(Archive archive, Stream saveStream)
        {
            var entries = archive.FilesList.ToArray();
            const int fileEntryLength = 0x11;
            var buffer = new byte[fileEntryLength];
            var enc = Encoding.GetEncoding(437);
            var curEntryStart = (int)saveStream.Position + (entries.Length + 1) * buffer.Length;
            foreach (var entry in entries)
            {
                var fileLength = entry.Length;
                if (entry.PhysicalPath != null)
                {
                    var fi = new FileInfo(entry.PhysicalPath);
                    if (!fi.Exists)
                        throw new FileNotFoundException("Cannot find file \"" + entry.PhysicalPath + "\" to write to archive!");
                    fileLength = (int)fi.Length;
                }
                var curName = this.GetInternalFilename(entry.FileName);
                var copySize = Math.Min(curName.Length, 12);
                Array.Copy(enc.GetBytes(curName), 0, buffer, 0, copySize);
                for (var b = copySize; b <= 13; b++)
                    buffer[b] = 0;
                ArrayUtils.WriteIntToByteArray(buffer, 13, 4, true, (uint)curEntryStart);
                curEntryStart += fileLength;
                saveStream.Write(buffer, 0, fileEntryLength);
            }
            for (var b = 0; b < 13; b++)
                buffer[b] = 0;
            ArrayUtils.WriteIntToByteArray(buffer, 13, 4, true, (uint)curEntryStart);
            saveStream.Write(buffer, 0, fileEntryLength);
            foreach (var entry in entries)
                CopyEntryContentsToStream(entry, saveStream);
            return true;
        }

    }
}