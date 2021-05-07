using Nyerguds.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LibrarianTool.Domain.Archives
{
    class ArchiveCatV1 : Archive
    {
        protected const Int32 FileEntryLength = 0x12;

        public override String ShortTypeName { get { return "MPS Labs Catalog v1"; } }
        public override String ShortTypeDescription { get { return "MPS Labs Catalog v1"; } }
        public override String[] FileExtensions { get { return new String[] { "cat" }; } }

        protected override List<ArchiveEntry> LoadArchiveInternal(Stream loadStream, String archivePath)
        {
            Encoding enc = Encoding.GetEncoding(437);
            Int64 end = loadStream.Length;
            Byte[] buffer = new Byte[FileEntryLength];
            List<ArchiveEntry> filesList = new List<ArchiveEntry>();
            if (end - loadStream.Position < 0x02)
                throw new FileTypeLoadException("Not a CAT v1 Archive.");
            loadStream.Read(buffer, 0, 2);
            Int32 fatlength = (Int32)ArrayUtils.ReadIntFromByteArray(buffer, 0, 2, true);
            if (fatlength == 0 || end - loadStream.Position < fatlength)
                throw new FileTypeLoadException("Not a CAT v1 Archive.");
            if (fatlength % FileEntryLength != 0)
                throw new FileTypeLoadException("Not a CAT v1 Archive.");
            Int32 nrOfFiles = fatlength / FileEntryLength;
            for (Int32 i = 0; i < nrOfFiles; i++)
            {
                loadStream.Read(buffer, 0, FileEntryLength);
                Byte[] curNameB = buffer.Take(0x0C).TakeWhile(x => x != 0).ToArray();
                if (curNameB.Length == 0)
                    break;
                if (curNameB.Any(c => c < 0x20 || c >= 0x7F))
                    throw new FileTypeLoadException("Filename contains nonstandard characters.");
                String curName = enc.GetString(curNameB).Trim();
                Int32 curEntryPos = (Int32)ArrayUtils.ReadIntFromByteArray(buffer, 0x0C, 4, true);
                Int32 curEntryLength = (Int16)ArrayUtils.ReadIntFromByteArray(buffer, 0x10, 2, true);
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

        public override Boolean SaveArchive(Archive archive, Stream saveStream, String savePath)
        {
            ArchiveEntry[] entries = archive.FilesList.ToArray();
            Byte[] buffer = new Byte[FileEntryLength];
            Encoding enc = Encoding.GetEncoding(437);
            Int32 curEntryStart = entries.Length * buffer.Length;
            // Write files table size
            ArrayUtils.WriteIntToByteArray(buffer, 0, 2, true, (UInt32)curEntryStart);
            saveStream.Write(buffer, 0, 2);
            curEntryStart += 2;
            // Write files table
            for (Int32 i = 0; i < entries.Length; ++i)
            {
                ArchiveEntry entry = entries[i];
                Int32 fileLength = entry.Length;
                if (entry.PhysicalPath != null)
                {
                    FileInfo fi = new FileInfo(entry.PhysicalPath);
                    if (!fi.Exists)
                        throw new FileNotFoundException("Cannot find file \"" + entry.PhysicalPath + "\" to write to archive!");
                    fileLength = (Int32) fi.Length;
                    if (fileLength > UInt16.MaxValue)
                        throw new ArgumentException("The file \"" + entry.PhysicalPath + "\" is too large to write to this type of archive!");
                }
                String curName = this.GetInternalFilename(entry.FileName);
                Int32 copySize = Math.Min(curName.Length, 12);
                Array.Copy(enc.GetBytes(curName), 0, buffer, 0, copySize);
                for (Int32 b = copySize; b < 12; b++)
                    buffer[b] = 0;
                ArrayUtils.WriteIntToByteArray(buffer, 0x0C, 4, true, (UInt32) curEntryStart);
                ArrayUtils.WriteIntToByteArray(buffer, 0x10, 2, true, (UInt32) fileLength);
                curEntryStart += fileLength;
                saveStream.Write(buffer, 0, FileEntryLength);
            }
            // Write files
            foreach (ArchiveEntry entry in entries)
                CopyEntryContentsToStream(entry, saveStream);
            return true;
        }
    }
}