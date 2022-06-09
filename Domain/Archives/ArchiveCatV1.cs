using Nyerguds.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LibrarianTool.Domain.Archives
{
    class ArchiveCatV1 : Archive
    {
        protected const int FileEntryLength = 0x12;

        public override string ShortTypeName => "MPS Labs Catalog v1";
        public override string ShortTypeDescription => "MPS Labs Catalog v1";
        public override string[] FileExtensions { get { return new[] { "cat" }; } }

        protected override List<ArchiveEntry> LoadArchiveInternal(Stream loadStream, string archivePath)
        {
            var enc = Encoding.GetEncoding(437);
            var end = loadStream.Length;
            Span<byte> buffer = stackalloc byte[FileEntryLength];
            var filesList = new List<ArchiveEntry>();
            if (end - loadStream.Position < 0x02)
                throw new FileTypeLoadException("Not a CAT v1 Archive.");
            if (2 != loadStream.Read(buffer[..2]))
                throw new FileTypeLoadException("Not a CAT v1 Archive.");
            var fatlength = (int)ArrayUtils.ReadIntFromByteArray(buffer[..2], true);
            if (fatlength == 0 || end - loadStream.Position < fatlength)
                throw new FileTypeLoadException("Not a CAT v1 Archive.");
            if (fatlength % FileEntryLength != 0)
                throw new FileTypeLoadException("Not a CAT v1 Archive.");
            var nrOfFiles = fatlength / FileEntryLength;
            for (var i = 0; i < nrOfFiles; i++)
            {
                if (FileEntryLength != loadStream.Read(buffer[..FileEntryLength]))
                    throw new FileTypeLoadException("Not a CAT v1 Archive.");
                ReadOnlySpan<byte> curNameB = buffer[0x0C..];
                var index = curNameB.IndexOf((byte)0);
                if (index >= 0)
                    curNameB = curNameB[..index];
                if (curNameB.Length == 0)
                    break;
                foreach (var c in curNameB)
                {
                    if (c < 0x20 || c >= 0x7F)
                        throw new FileTypeLoadException("Filename contains nonstandard characters.");
                }
                var curName = enc.GetString(curNameB).Trim();
                var curEntryPos = (int)ArrayUtils.ReadIntFromByteArray(buffer.Slice(0x0C, 4), true);
                int curEntryLength = (short)ArrayUtils.ReadIntFromByteArray(buffer.Slice(0x10, 2), true);
                if (curEntryPos + curEntryLength > end)
                    throw new FileTypeLoadException("Archive entry outside file bounds.");
                if (curName.Length == 0 && curEntryLength == 0)
                    continue;
                filesList.Add(new ArchiveEntry(curName, archivePath, curEntryPos, curEntryLength));
            }
            return filesList;
        }

        protected override void OrderFilesListInternal(List<ArchiveEntry> filesList)
        {
            // do nothing
        }

        public override bool SaveArchive(Archive archive, Stream saveStream, string savePath)
        {
            var entries = archive.FilesList.ToArray();
            var buffer = new byte[FileEntryLength];
            var enc = Encoding.GetEncoding(437);
            var curEntryStart = entries.Length * buffer.Length;
            // Write files table size
            ArrayUtils.WriteIntToByteArray(buffer, 0, 2, true, (uint)curEntryStart);
            saveStream.Write(buffer, 0, 2);
            curEntryStart += 2;
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
                    if (fileLength > ushort.MaxValue)
                        throw new ArgumentException("The file \"" + entry.PhysicalPath + "\" is too large to write to this type of archive!");
                }
                var curName = this.GetInternalFilename(entry.FileName);
                var copySize = Math.Min(curName.Length, 12);
                Array.Copy(enc.GetBytes(curName), 0, buffer, 0, copySize);
                for (var b = copySize; b < 12; b++)
                    buffer[b] = 0;
                ArrayUtils.WriteIntToByteArray(buffer, 0x0C, 4, true, (uint)curEntryStart);
                ArrayUtils.WriteIntToByteArray(buffer, 0x10, 2, true, (uint)fileLength);
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