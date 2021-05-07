using Nyerguds.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LibrarianTool.Domain.Archives
{
    class ArchiveCatV2 : Archive
    {
        protected const Int32 FileEntryLength = 0x18;

        public override String ShortTypeName { get { return "MPS Labs Catalog v2"; } }
        public override String ShortTypeDescription { get { return "MPS Labs Catalog v2"; } }
        public override String[] FileExtensions { get { return new String[] { "cat" }; } }

        protected override List<ArchiveEntry> LoadArchiveInternal(Stream loadStream, String archivePath)
        {
            Encoding enc = Encoding.GetEncoding(437);
            Int64 end = loadStream.Length;
            Byte[] buffer = new Byte[FileEntryLength];
            List<ArchiveEntry> filesList = new List<ArchiveEntry>();
            if (end - loadStream.Position < 0x02)
                throw new FileTypeLoadException("Not a CAT v2 Archive.");
            loadStream.Read(buffer, 0, 2);
            Int32 nrOfFiles = (Int32)ArrayUtils.ReadIntFromByteArray(buffer, 0, 2, true);
            if (nrOfFiles == 0 || end - loadStream.Position < nrOfFiles * FileEntryLength)
                throw new FileTypeLoadException("Not a CAT v2 Archive.");
            for (Int32 i = 0; i < nrOfFiles; i++)
            {
                loadStream.Read(buffer, 0, FileEntryLength);
                Byte[] curNameB = buffer.Take(0x0C).TakeWhile(x => x != 0).ToArray();
                if (curNameB.Length == 0)
                    break;
                if (curNameB.Any(c => c < 0x20 || c >= 0x7F))
                    throw new FileTypeLoadException("Filename contains nonstandard characters.");
                String curName = enc.GetString(curNameB).Trim();
                UInt16 dosTime = (UInt16)ArrayUtils.ReadIntFromByteArray(buffer, 0x0C, 2, true);
                UInt16 dosDate = (UInt16)ArrayUtils.ReadIntFromByteArray(buffer, 0x0E, 2, true);
                DateTime dt;
                try
                {
                    dt = GeneralUtils.GetDosDateTime(dosTime, dosDate);
                }
                catch (ArgumentException argex)
                {
                    throw new FileTypeLoadException(argex.Message, argex);
                }
                String extraInfo = GeneralUtils.GetDateString(dt);
                Int32 curEntryLength = (Int32)ArrayUtils.ReadIntFromByteArray(buffer, 0x10, 4, true);
                Int32 curEntryPos= (Int32)ArrayUtils.ReadIntFromByteArray(buffer, 0x14, 4, true);
                if (curEntryPos + curEntryLength > end)
                    throw new FileTypeLoadException("Archive entry outside file bounds.");
                if (curName.Length == 0 && curEntryLength == 0)
                    continue;
                ArchiveEntry entry = new ArchiveEntry(curName, archivePath, curEntryPos, curEntryLength, extraInfo);
                entry.Date = dt;
                filesList.Add(entry);
            }
            return filesList;
        }

        /// <summary>Inserts a file into the archive. This can be overridden to add filtering on the input.</summary>
        /// <param name="filePath">Path of the file to load.</param>
        public override ArchiveEntry InsertFile(String filePath)
        {
            ArchiveEntry file = base.InsertFile(filePath);
            DateTime lastMod = file.Date ?? File.GetLastWriteTime(filePath);
            file.ExtraInfo = GeneralUtils.GetDateString(lastMod);
            return file;
        }

        protected override void OrderFilesListInternal(List<ArchiveEntry> filesList)
        {
            // do nothing
        }

        public override Boolean SaveArchive(Archive archive, Stream saveStream, String savePath)
        {
            DateTime writeDate = DateTime.Now;
            ArchiveEntry[] entries = archive.FilesList.ToArray();
            Int32 nrOfFiles = entries.Length;
            Byte[] buffer = new Byte[FileEntryLength];
            Encoding enc = Encoding.GetEncoding(437);
            Int32 curEntryStart = nrOfFiles * buffer.Length + 2;
            // Write amount of files in table
            ArrayUtils.WriteIntToByteArray(buffer, 0, 2, true, (UInt32)nrOfFiles);
            saveStream.Write(buffer, 0, 2);
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
                    fileLength = (Int32)fi.Length;
                }
                String curName = this.GetInternalFilename(entry.FileName);
                Int32 copySize = Math.Min(curName.Length, 12);
                Array.Copy(enc.GetBytes(curName), 0, buffer, 0, copySize);
                for (Int32 b = copySize; b < 12; b++)
                    buffer[b] = 0;
                DateTime dt = entry.Date ?? writeDate;
                UInt16 time = GeneralUtils.GetDosTimeInt(dt);
                UInt16 date = GeneralUtils.GetDosDateInt(dt);
                ArrayUtils.WriteIntToByteArray(buffer, 0x0C, 2, true, time);
                ArrayUtils.WriteIntToByteArray(buffer, 0x0E, 2, true, date);
                ArrayUtils.WriteIntToByteArray(buffer, 0x10, 4, true, (UInt32)fileLength);
                ArrayUtils.WriteIntToByteArray(buffer, 0x14, 4, true, (UInt32)curEntryStart);
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