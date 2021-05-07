using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Nyerguds.Util;

namespace LibrarianTool.Domain.Archives
{
    public class ArchivePakV1 : ArchivePak
    {
        protected override PakVersion PakVer { get { return PakVersion.PakVersion1;} }
    }

    public class ArchivePakV2 : ArchivePak
    {
        protected override PakVersion PakVer { get { return PakVersion.PakVersion2; } }
    }

    public class ArchivePakV3 : ArchivePak
    {
        protected override PakVersion PakVer { get { return PakVersion.PakVersion3; } }
    }

    public abstract class ArchivePak : Archive
    {
        public override String ShortTypeName { get { return "Westwood PAK Archive v" + (Int32)this.PakVer; } }
        public override String ShortTypeDescription { get { return "Westwood PAK v" + (Int32)this.PakVer; } }
        public override String[] FileExtensions { get { return new String[] { "PAK" }; } }
        protected abstract PakVersion PakVer { get; }

        protected override List<ArchiveEntry> LoadArchiveInternal(Stream loadStream, String archivePath)
        {
            UInt32 end = (UInt32)loadStream.Length;
            Encoding enc = new ASCIIEncoding();
            if (end < 4)
                throw new FileTypeLoadException("Archive not long enough for a single entry.");
            Byte[] addressBuffer = new Byte[4];
            loadStream.Position = 0;
            this.ExtraInfo = String.Empty;
            ArchiveEntry curEntry = null;
            UInt32 minOffs = end;
            // Need at least the address plus one byte for a 0-terminated name.
            Boolean foundEndAddress = false;
            Boolean foundEndAddressEntryV3 = false;
            Boolean foundNullAddress = false;
            Boolean foundNullName = false;
            UInt32 address = 0;
            List<ArchiveEntry> filesList = new List<ArchiveEntry>();
            while (loadStream.Position < minOffs)
            {
                if (loadStream.Read(addressBuffer, 0, 4) < 4)
                    throw new FileTypeLoadException("Archive not long enough to read file offset.");
                address = (UInt32)ArrayUtils.ReadIntFromByteArray(addressBuffer, 0, 4, true);
                if (address == 0)
                {
                    foundNullAddress = true;
                    if (curEntry != null && curEntry.Length == -1 && this.PakVer == PakVersion.PakVersion2)
                        curEntry.Length = (Int32)end - curEntry.StartOffset;
                }
                if (address == end && this.PakVer == PakVersion.PakVersion1)
                {
                    foundEndAddress = true;
                    if (curEntry != null && curEntry.Length == -1)
                        curEntry.Length = (Int32)end - curEntry.StartOffset;
                }
                if (this.PakVer == PakVersion.PakVersion3 && foundNullName && foundEndAddressEntryV3 && foundNullAddress)
                    break;
                if (this.PakVer == PakVersion.PakVersion2 && foundNullAddress)
                    break;
                if (this.PakVer == PakVersion.PakVersion1 && foundEndAddress)
                    break;
                if (loadStream.Position == minOffs)
                    break;
                if (address > end)
                    throw new FileTypeLoadException("Archive entry outside file bounds.");

                if (curEntry != null && curEntry.Length == -1)
                    curEntry.Length = (Int32)address - curEntry.StartOffset;

                Byte[] nameBuf = new Byte[13];
                Int32 curNamePos;
                for (curNamePos = 0; curNamePos < nameBuf.Length; curNamePos++)
                {
                    Int32 curByte = loadStream.ReadByte();
                    if (curByte == -1)
                        throw new FileTypeLoadException("Archive not long enough to read file name.");
                    if (curByte == 0)
                        break;
                    if (curByte < 0x20)
                        throw new FileTypeLoadException("Illegal values in file name.");
                    nameBuf[curNamePos] = (Byte) curByte;
                }
                if (curNamePos == 13)
                    throw new FileTypeLoadException("Bad file name.");
                String curName = enc.GetString(nameBuf.TakeWhile(x => x != 0).ToArray());
                if (curName.Length == 0)
                {
                    foundNullName = true;
                    if ((curEntry == null || (curEntry.Length == -1 && address > curEntry.StartOffset)) && address != 0 && address <= end && this.PakVer == PakVersion.PakVersion3)
                    {
                        foundEndAddressEntryV3 = true;
                        if (curEntry != null)
                            curEntry.Length = (Int32)address - curEntry.StartOffset;
                    }
                }
                if (!foundEndAddress && !foundNullAddress && !foundNullName)
                {
                    curEntry = new ArchiveEntry(curName, archivePath, (Int32) address, -1);
                    minOffs = Math.Min(minOffs, address);
                    filesList.Add(curEntry);
                }
            }

            if (this.PakVer == PakVersion.PakVersion3 && (!foundNullName || !foundEndAddressEntryV3 || !foundNullAddress))
                throw new FileTypeLoadException("This is not a v3 PAK file.");
            if (this.PakVer == PakVersion.PakVersion2 && (foundNullName || foundEndAddress || !foundNullAddress))
                throw new FileTypeLoadException("This is not a v2 PAK file.");
            if (this.PakVer == PakVersion.PakVersion1 && (foundNullName || foundNullAddress))
                throw new FileTypeLoadException("This is not a v1 PAK file.");
            if (this.PakVer == PakVersion.PakVersion1 && !foundEndAddress && loadStream.Position == minOffs && curEntry != null && curEntry.Length == -1)
            {
                // Seems to be a problem in some v1 pak files where the last entry is gibberish.
                curEntry.Length = (Int32)end - curEntry.StartOffset;
                this.ExtraInfo = "File has corrupted end offset: " + address.ToString("X8");
            }
            // All cases should be handled.
            if (curEntry != null && curEntry.Length == -1)
                throw new FileTypeLoadException("This is not a PAK file.");
            // Not gonna allow this. Too much chance on empty edge cases.
            if (filesList.Count == 0)
                throw new FileTypeLoadException("Not entries in PAK file.");
            return filesList;
        }

        public override Boolean SaveArchive(Archive archive, Stream saveStream, String savePath)
        {
            ArchiveEntry[] entries = archive.FilesList.ToArray();
            // Filename lengths + version-dependent padding
            Int32 firstFileOffset = entries.Sum(en => en.FileName.Length) + entries.Length * 5;
            switch (this.PakVer)
            {
                case PakVersion.PakVersion1:
                case PakVersion.PakVersion2:
                    // v1: added dword with end
                    // v2: added dword with zero
                    firstFileOffset += 4;
                    break;
                case PakVersion.PakVersion3:
                    // v3: added dword with end, added byte for empty filename, added dword with zero.
                    firstFileOffset += 9;
                    break;
            }
            Int32 fileOffset = firstFileOffset;
            Encoding enc = Encoding.GetEncoding(437);

            Byte[] buffer = new Byte[13];
            using (BinaryWriter bw = new BinaryWriter(new NonDisposingStream(saveStream)))
            {
                foreach (ArchiveEntry entry in entries)
                {
                    Int32 fileLength = entry.Length;
                    String curName = entry.FileName;
                    if (entry.PhysicalPath != null)
                    {
                        // To be 100% sure the index is OK, this is updated at the moment of writing.
                        FileInfo fi = new FileInfo(entry.PhysicalPath);
                        if (!fi.Exists)
                            throw new FileNotFoundException("Cannot find file \"" + entry.PhysicalPath + "\" to write to archive!");
                        fileLength = (Int32) fi.Length;
                        // not really necessary since the input method takes care of it, but, just to be sure.
                        curName = this.GetInternalFilename(entry.FileName);
                    }
                    bw.Write(fileOffset);
                    fileOffset += fileLength;
                    Int32 copySize = curName.Length;
                    Array.Copy(enc.GetBytes(curName), 0, buffer, 0, copySize);
                    for (Int32 b = copySize; b < 13; b++)
                        buffer[b] = 0;
                    bw.Write(buffer, 0, copySize + 1);
                }
                switch (this.PakVer)
                {
                    case PakVersion.PakVersion1:
                        bw.Write(fileOffset);
                        break;
                    case PakVersion.PakVersion2:
                        bw.Write((Int32)0);
                        break;
                    case PakVersion.PakVersion3:
                        bw.Write(fileOffset);
                        bw.Write(0);
                        bw.Write((Byte)0);
                        break;
                }
            }
            if (firstFileOffset != saveStream.Position)
                throw new IndexOutOfRangeException("Programmer error: write start offset does not match end of index.");
            foreach (ArchiveEntry entry in entries)
                CopyEntryContentsToStream(entry, saveStream);
            return true;
        }

        protected enum PakVersion
        {
            PakVersion1 = 1,
            PakVersion2 = 2,
            PakVersion3 = 3,
        }

    }
}