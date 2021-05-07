using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Nyerguds.Util;

namespace LibrarianTool.Domain.Archives
{
    public class ArchiveDynV1 : Archive
    {
        protected const Int32 FileEntryLength = 0x11;

        public override String ShortTypeName { get { return "Dynamix Archive v1"; } }
        public override String ShortTypeDescription { get { return "Dynamix Archive v1"; } }
        public override String[] FileExtensions { get { return new String[] { "000", "001", "002", "003", "004", "005", "006", "007", "008", "009" }; } }

        protected override List<ArchiveEntry> LoadArchiveInternal(Stream loadStream, String archivePath)
        {
            Encoding enc = Encoding.GetEncoding(437);
            Int64 end = loadStream.Length;
            Byte[] buffer = new Byte[FileEntryLength];
            Int64 curPos = loadStream.Position;
            List<ArchiveEntry> filesList = new List<ArchiveEntry>();
            while (curPos < end)
            {
                loadStream.Position = curPos;
                loadStream.Read(buffer, 0, FileEntryLength);
                Byte[] curNameB = buffer.Take(13).TakeWhile(x => x != 0).ToArray();
                if (curNameB.Length == 0 || curNameB.Length > 12)
                    throw new FileTypeLoadException("Not a Dynamix v1 archive.");
                if (curNameB.Any(c => c < 0x20 || c >= 0x7F))
                    throw new FileTypeLoadException("Filename contains nonstandard characters.");
                String curName = enc.GetString(curNameB).Trim();
                Int32 curEntryLength = (Int32)ArrayUtils.ReadIntFromByteArray(buffer, 0x0D, 4, true);
                curPos = loadStream.Position + curEntryLength;
                if (curPos < 0 || curPos > end)
                    throw new FileTypeLoadException("Archive entry outside file bounds.");
                if (curName.Length == 0 && curEntryLength == 0)
                    continue;
                filesList.Add(new ArchiveEntry(curName, archivePath, (Int32)loadStream.Position, curEntryLength));
            }
            return filesList;
        }

        public override Boolean SaveArchive(Archive archive, Stream saveStream, String savePath)
        {
            Encoding enc = Encoding.GetEncoding(437);
            Byte[] buffer = new Byte[FileEntryLength];

            foreach (ArchiveEntry entry in archive.FilesList)
            {
                String filename = this.GetInternalFilename(entry.FileName);
                enc.GetBytes(this.FileName, 0, 12, buffer, 0);
                for (Int32 b = filename.Length; b <= 13; b++)
                    buffer[b] = 0;
                ArrayUtils.WriteIntToByteArray(buffer, 0x0D, 4, true, (UInt64)entry.Length);
                saveStream.Write(buffer, 0, buffer.Length);
                CopyEntryContentsToStream(entry, saveStream);
            }
            return true;
        }
    }
}