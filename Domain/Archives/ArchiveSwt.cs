using Nyerguds.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibrarianTool.Domain.Archives
{
    public class ArchiveSwt : Archive
    {
        public override string ShortTypeName => "SelectWare Technologies Archive";
        public override string ShortTypeDescription => "SelectWare Archive";
        public override string[] FileExtensions { get { return new[] { "swt" }; } }
        public override bool CanSave => false;
        public override bool SupportsFolders => true;

        const string SWT_BANNER = "SelectWare Technologies demo file";

        protected override List<ArchiveEntry> LoadArchiveInternal(System.IO.Stream loadStream, string archivePath)
        {
            var end = (uint)loadStream.Length;
            Encoding enc = new ASCIIEncoding();
            if (end < SWT_BANNER.Length)
                throw new FileTypeLoadException("Archive not long enough for header.");
            var buffer = new byte[SWT_BANNER.Length];
            loadStream.Read(buffer, 0, SWT_BANNER.Length);
            var header = enc.GetString(buffer);
            if (header != SWT_BANNER)
                throw new FileTypeLoadException("Header does not match.");
            loadStream.Read(buffer, 0, 0x0B);
            // First 7 bytes should be [0A 1A 00 00 00 00 00]. Not going to check that though.
            // Next 4 bytes are unknown.
            // start on first file
            var curPos = 0x2C;
            const int bufLen = 46;
            var filesList = new List<ArchiveEntry>();
            var folders = new Dictionary<ushort, ArchiveEntry>();
            ushort? prevFolderId = null;
            //ArchiveEntry lastFolder = null;
            while (curPos < end)
            {
                buffer = new byte[bufLen];
                loadStream.Position = curPos;
                var address = curPos + bufLen;
                if (curPos + bufLen >= end)
                    throw new FileTypeLoadException("Archive not long enough for file header.");
                loadStream.Read(buffer, 0, bufLen);
                var entryFlags = (uint)ArrayUtils.ReadIntFromByteArray(buffer.AsSpan(0, 3), true);
                // 00000000 00000000 00000001
                var rootFile = (entryFlags & 0x000001) == 0;
                // 10000000 00000000 00000000
                var flag8 = (entryFlags & 0x800000) != 0;
                // 00000001 00000000 00000000
                var flag3_1 = (entryFlags & 0x010000) != 0;
                // 00000010 00000000 00000000
                var flag3_2 = (entryFlags & 0x020000) != 0;
                var index = (ushort)ArrayUtils.ReadIntFromByteArray(buffer.AsSpan(0x0F, 2), true);
                var folderId = (ushort)ArrayUtils.ReadIntFromByteArray(buffer.AsSpan(0x11, 2), true);
                // Detected switch to different folder ID; save this as indication to store a new folder id later.
                var newFolder = prevFolderId != folderId;
                prevFolderId = folderId;
                var unkn1 = (uint)ArrayUtils.ReadIntFromByteArray(buffer.AsSpan(0x13, 4), true);
                // buffer[0x17] = 0x20
                var dosTime = (ushort)ArrayUtils.ReadIntFromByteArray(buffer.AsSpan(0x18, 2), true);
                var dosDate = (ushort)ArrayUtils.ReadIntFromByteArray(buffer.AsSpan(0x1A, 2), true);
                DateTime dt;
                try
                {
                    dt = GeneralUtils.GetDosDateTime(dosTime, dosDate);
                }
                catch (ArgumentException argex)
                {
                    throw new FileTypeLoadException(argex.Message, argex);
                }
                var length = (int)ArrayUtils.ReadIntFromByteArray(buffer.AsSpan(0x1C, 4), true);
                var isFolder = length == 0;

                var curName = enc.GetString(buffer.Skip(0x20).TakeWhile(x => x != 0).ToArray());
                var curEntry = new ArchiveEntry(curName, archivePath, address, length)
                {
                    ExtraInfoBin = buffer,
                    Date = dt,
                    ExtraInfo = "Folder ID: " + folderId.ToString("X4") + ", file index: " + index + "\n"
                };

                if (newFolder && !folders.ContainsKey(folderId))
                {
                    var previous = filesList.LastOrDefault();
                    if (previous == null || previous.Length == 0)
                    {
                        folders[folderId] = previous;
                        if (previous != null)
                            previous.IsFolder = true;
                    }
                }
                var curFolder = folders[folderId];
                if (curFolder != null)
                {
                    curName = curFolder.FileName + "\\" + curName;
                    curEntry.FileName = curName;
                }
                filesList.Add(curEntry);
                curPos += bufLen + length;
            }
            return filesList;
        }

        protected override void OrderFilesListInternal(List<ArchiveEntry> filesList)
        {
            // Do nothing.
            // May adapt this later; if I fill ExtraInfoBin with all info regarding files' folder IDs, index and folders' own IDs,
            // sorting may be possible that way.
        }

        public override bool SaveArchive(Archive archive, System.IO.Stream saveStream, string savePath)
        {
            throw new NotImplementedException();
        }
    }
}
