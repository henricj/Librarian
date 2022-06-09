using Nyerguds.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LibrarianTool.Domain.Archives
{
    public class ArchiveDynV1 : Archive
    {
        protected const int FileEntryLength = 0x11;

        public override string ShortTypeName => "Dynamix Archive v1";
        public override string ShortTypeDescription => "Dynamix Archive v1";
        public override string[] FileExtensions { get { return new[] { "000", "001", "002", "003", "004", "005", "006", "007", "008", "009" }; } }

        protected override List<ArchiveEntry> LoadArchiveInternal(Stream loadStream, string archivePath)
        {
            var enc = Encoding.GetEncoding(437);
            var end = loadStream.Length;
            Span<byte> buffer = stackalloc byte[FileEntryLength];
            var curPos = loadStream.Position;
            var filesList = new List<ArchiveEntry>();
            while (curPos < end)
            {
                loadStream.Position = curPos;
                if (FileEntryLength != loadStream.Read(buffer[..FileEntryLength]))
                    throw new FileTypeLoadException("Not a Dynamix v1 archive.");
                ReadOnlySpan<byte> curNameB = buffer[13..];
                var index = curNameB.IndexOf((byte)0);
                if (index >= 0)
                    curNameB = curNameB[..index];
                if (curNameB.Length is 0 or > 12)
                    throw new FileTypeLoadException("Not a Dynamix v1 archive.");
                foreach (var c in curNameB)
                {
                    if (c is < 0x20 or >= 0x7F)
                        throw new FileTypeLoadException("Filename contains nonstandard characters.");
                }
                var curName = enc.GetString(curNameB).Trim();
                var curEntryLength = (int)ArrayUtils.ReadIntFromByteArray(buffer.Slice(0x0D, 4), true);
                curPos = loadStream.Position + curEntryLength;
                if (curPos < 0 || curPos > end)
                    throw new FileTypeLoadException("Archive entry outside file bounds.");
                if (curName.Length == 0 && curEntryLength == 0)
                    continue;
                filesList.Add(new ArchiveEntry(curName, archivePath, (int)loadStream.Position, curEntryLength));
            }
            return filesList;
        }

        public override bool SaveArchive(Archive archive, Stream saveStream, string savePath)
        {
            var enc = Encoding.GetEncoding(437);
            var buffer = new byte[FileEntryLength];

            foreach (var entry in archive.FilesList)
            {
                var filename = this.GetInternalFilename(entry.FileName);
                enc.GetBytes(this.FileName, 0, 12, buffer, 0);
                for (var b = filename.Length; b <= 13; b++)
                    buffer[b] = 0;
                ArrayUtils.WriteIntToByteArray(buffer, 0x0D, 4, true, (ulong)entry.Length);
                saveStream.Write(buffer, 0, buffer.Length);
                CopyEntryContentsToStream(entry, saveStream);
            }
            return true;
        }
    }
}