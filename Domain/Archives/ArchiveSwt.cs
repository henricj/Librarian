using Nyerguds.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibrarianTool.Domain.Archives
{
    public class ArchiveSwt : Archive
    {
        public override String ShortTypeName { get { return "SelectWare Technologies Archive"; } }
        public override String ShortTypeDescription { get { return "SelectWare Archive"; } }
        public override String[] FileExtensions { get { return new String[] { "swt" }; } }
        public override Boolean CanSave { get { return false; } }
        public override Boolean SupportsFolders { get { return true; } }

        const String SWT_BANNER = "SelectWare Technologies demo file";

        protected override List<ArchiveEntry> LoadArchiveInternal(System.IO.Stream loadStream, string archivePath)
        {
            UInt32 end = (UInt32)loadStream.Length;
            Encoding enc = new ASCIIEncoding();
            if (end < SWT_BANNER.Length)
                throw new FileTypeLoadException("Archive not long enough for header.");
            Byte[] buffer = new Byte[SWT_BANNER.Length];
            loadStream.Read(buffer, 0, SWT_BANNER.Length);
            String header = enc.GetString(buffer);
            if (header != SWT_BANNER)
                throw new FileTypeLoadException("Header does not match.");
            loadStream.Read(buffer, 0, 0x0B);
            // First 7 bytes should be [0A 1A 00 00 00 00 00]. Not going to check that though.
            // Next 4 bytes are unknown.
            // start on first file
            Int32 curPos = 0x2C;
            const Int32 bufLen = 46;
            List<ArchiveEntry> filesList = new List<ArchiveEntry>();
            Dictionary<UInt16, ArchiveEntry> folders = new Dictionary<UInt16, ArchiveEntry>();
            UInt16? prevFolderId = null;
            ArchiveEntry lastFolder = null;
            while (curPos < end)
            {
                buffer = new Byte[bufLen];
                loadStream.Position = curPos;
                Int32 address = curPos + bufLen;
                if (curPos + bufLen >= end)
                    throw new FileTypeLoadException("Archive not long enough for file header.");
                loadStream.Read(buffer, 0, bufLen);
                UInt32 entryFlags = (UInt32)ArrayUtils.ReadIntFromByteArray(buffer, 0x00, 3, true);
                // 00000000 00000000 00000001
                Boolean rootFile = (entryFlags & 0x000001) == 0;
                // 10000000 00000000 00000000
                Boolean flag8 = (entryFlags & 0x800000) != 0;
                // 00000001 00000000 00000000
                Boolean flag3_1 = (entryFlags & 0x010000) != 0;
                // 00000010 00000000 00000000
                Boolean flag3_2 = (entryFlags & 0x020000) != 0;
                UInt16 index = (UInt16)ArrayUtils.ReadIntFromByteArray(buffer, 0x0F, 2, true);
                UInt16 folderId = (UInt16)ArrayUtils.ReadIntFromByteArray(buffer, 0x11, 2, true);
                // Detected switch to different folder ID; save this as indication to store a new folder id later.
                Boolean newFolder = prevFolderId != folderId;
                prevFolderId = folderId;
                UInt32 unkn1 = (UInt32)ArrayUtils.ReadIntFromByteArray(buffer, 0x13, 4, true);
                // buffer[0x17] = 0x20
                UInt16 dosTime = (UInt16)ArrayUtils.ReadIntFromByteArray(buffer, 0x18, 2, true);
                UInt16 dosDate = (UInt16)ArrayUtils.ReadIntFromByteArray(buffer, 0x1A, 2, true);
                DateTime dt;
                try
                {
                    dt = GeneralUtils.GetDosDateTime(dosTime, dosDate);
                }
                catch (ArgumentException argex)
                {
                    throw new FileTypeLoadException(argex.Message, argex);
                }
                Int32 length = (Int32)ArrayUtils.ReadIntFromByteArray(buffer, 0x1C, 4, true);
                Boolean isFolder = length == 0;

                String curName = enc.GetString(buffer.Skip(0x20).TakeWhile(x => x != 0).ToArray());
                ArchiveEntry curEntry = new ArchiveEntry(curName, archivePath, address, length);
                curEntry.ExtraInfoBin = buffer;
                curEntry.Date = dt;
                curEntry.ExtraInfo = "Folder ID: "+ folderId.ToString("X4") + ", file index: " + index + "\n";

                if (newFolder && !folders.ContainsKey(folderId))
                {
                    ArchiveEntry previous = filesList.LastOrDefault();
                    if (previous == null || previous.Length == 0)
                    {
                        folders[folderId] = previous;
                        if (previous != null)
                            previous.IsFolder = true;
                    }
                }
                ArchiveEntry curFolder = folders[folderId];
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
