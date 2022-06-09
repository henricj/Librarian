using Nyerguds.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LibrarianTool.Domain.Archives
{
    class ArchiveCatV2 : Archive
    {
        protected const int FileEntryLength = 0x18;

        public override string ShortTypeName => "MPS Labs Catalog v2";
        public override string ShortTypeDescription => "MPS Labs Catalog v2";
        public override string[] FileExtensions { get { return new[] { "cat" }; } }

        protected override List<ArchiveEntry> LoadArchiveInternal(Stream loadStream, string archivePath)
        {
            var enc = Encoding.GetEncoding(437);
            var end = loadStream.Length;
            Span<byte> buffer = stackalloc byte[FileEntryLength];
            var filesList = new List<ArchiveEntry>();
            if (end - loadStream.Position < 0x02)
                throw new FileTypeLoadException("Not a CAT v2 Archive.");
            if (2 != loadStream.Read(buffer[..2]))
                throw new FileTypeLoadException("Unable to read archive");
            var nrOfFiles = (int)ArrayUtils.ReadIntFromByteArray(buffer[..2], true);
            if (nrOfFiles == 0 || end - loadStream.Position < nrOfFiles * FileEntryLength)
                throw new FileTypeLoadException("Not a CAT v2 Archive.");
            for (var i = 0; i < nrOfFiles; i++)
            {
                if (FileEntryLength != loadStream.Read(buffer[..FileEntryLength]))
                    throw new FileTypeLoadException("Unable to read archive");
                ReadOnlySpan<byte> curNameB = buffer[0x0c..];
                var index = curNameB.IndexOf((byte)0);
                if (index >= 0)
                    curNameB = curNameB[..index];
                if (curNameB.Length == 0)
                    break;
                foreach (var c in curNameB)
                {
                    if (c is < 0x20 or >= 0x7F)
                        throw new FileTypeLoadException("Filename contains nonstandard characters.");
                }
                var curName = enc.GetString(curNameB).Trim();
                var dosTime = (ushort)ArrayUtils.ReadIntFromByteArray(buffer.Slice(0x0C, 2), true);
                var dosDate = (ushort)ArrayUtils.ReadIntFromByteArray(buffer.Slice(0x0E, 2), true);
                DateTime dt;
                try
                {
                    dt = GeneralUtils.GetDosDateTime(dosTime, dosDate);
                }
                catch (ArgumentException argex)
                {
                    throw new FileTypeLoadException(argex.Message, argex);
                }
                var extraInfo = GeneralUtils.GetDateString(dt);
                var curEntryLength = (int)ArrayUtils.ReadIntFromByteArray(buffer.Slice(0x10, 4), true);
                var curEntryPos = (int)ArrayUtils.ReadIntFromByteArray(buffer.Slice(0x14, 4), true);
                if (curEntryPos + curEntryLength > end)
                    throw new FileTypeLoadException("Archive entry outside file bounds.");
                if (curName.Length == 0 && curEntryLength == 0)
                    continue;
                var entry = new ArchiveEntry(curName, archivePath, curEntryPos, curEntryLength, extraInfo)
                {
                    Date = dt
                };
                filesList.Add(entry);
            }
            return filesList;
        }

        /// <summary>Inserts a file into the archive. This can be overridden to add filtering on the input.</summary>
        /// <param name="filePath">Path of the file to load.</param>
        public override ArchiveEntry InsertFile(string filePath)
        {
            var file = base.InsertFile(filePath);
            var lastMod = file.Date ?? File.GetLastWriteTime(filePath);
            file.ExtraInfo = GeneralUtils.GetDateString(lastMod);
            return file;
        }

        protected override void OrderFilesListInternal(List<ArchiveEntry> filesList)
        {
            // do nothing
        }

        public override bool SaveArchive(Archive archive, Stream saveStream, string savePath)
        {
            var writeDate = DateTime.Now;
            var entries = archive.FilesList.ToArray();
            var nrOfFiles = entries.Length;
            var buffer = new byte[FileEntryLength];
            var enc = Encoding.GetEncoding(437);
            var curEntryStart = nrOfFiles * buffer.Length + 2;
            // Write amount of files in table
            ArrayUtils.WriteIntToByteArray(buffer, 0, 2, true, (uint)nrOfFiles);
            saveStream.Write(buffer, 0, 2);
            // Write files table
            for (var i = 0; i < entries.Length; ++i)
            {
                var entry = entries[i];
                var fileLength = entry.Length;
                if (entry.PhysicalPath != null)
                {
                    var fi = new FileInfo(entry.PhysicalPath);
                    if (!fi.Exists)
                        throw new FileNotFoundException("Cannot find file \"" + entry.PhysicalPath + "\" to write to archive!");
                    fileLength = (int)fi.Length;
                }
                var curName = GetInternalFilename(entry.FileName);
                var copySize = Math.Min(curName.Length, 12);
                Array.Copy(enc.GetBytes(curName), 0, buffer, 0, copySize);
                for (var b = copySize; b < 12; b++)
                    buffer[b] = 0;
                var dt = entry.Date ?? writeDate;
                var time = GeneralUtils.GetDosTimeInt(dt);
                var date = GeneralUtils.GetDosDateInt(dt);
                ArrayUtils.WriteIntToByteArray(buffer, 0x0C, 2, true, time);
                ArrayUtils.WriteIntToByteArray(buffer, 0x0E, 2, true, date);
                ArrayUtils.WriteIntToByteArray(buffer, 0x10, 4, true, (uint)fileLength);
                ArrayUtils.WriteIntToByteArray(buffer, 0x14, 4, true, (uint)curEntryStart);
                curEntryStart += fileLength;
                saveStream.Write(buffer, 0, FileEntryLength);
            }
            // Write files
            foreach (var entry in entries)
                CopyEntryContentsToStream(entry, saveStream);
            return true;
        }

    }
}