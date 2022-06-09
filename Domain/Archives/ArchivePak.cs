using Nyerguds.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LibrarianTool.Domain.Archives
{
    public class ArchivePakV1 : ArchivePak
    {
        public ArchivePakV1() : base(PakVersion.PakVersion1) { }
    }

    public class ArchivePakV2 : ArchivePak
    {
        public ArchivePakV2() : base(PakVersion.PakVersion2) { }
    }

    public class ArchivePakV3 : ArchivePak
    {
        public ArchivePakV3() : base(PakVersion.PakVersion3) { }
    }

    public abstract class ArchivePak : Archive
    {
        public override string ShortTypeName { get; }
        public override string ShortTypeDescription { get; }
        public override string[] FileExtensions { get; } = { "PAK" };
        protected PakVersion PakVer { get; }

        protected ArchivePak(PakVersion version)
        {
            PakVer = version;
            ShortTypeName = "Westwood PAK Archive v" + (int)this.PakVer;
            ShortTypeDescription = "Westwood PAK v" + (int)this.PakVer;
        }

        protected override List<ArchiveEntry> LoadArchiveInternal(Stream loadStream, string archivePath)
        {
            var end = (uint)loadStream.Length;
            if (end < 4)
                throw new FileTypeLoadException("Archive not long enough for a single entry.");
            Span<byte> addressBuffer = stackalloc byte[4];
            loadStream.Position = 0;
            this.ExtraInfo = string.Empty;
            ArchiveEntry curEntry = null;
            var minOffs = end;
            // Need at least the address plus one byte for a 0-terminated name.
            var foundEndAddress = false;
            var foundEndAddressEntryV3 = false;
            var foundNullAddress = false;
            var foundNullName = false;
            uint address = 0;
            var foundLongFileName = false;
            var filesList = new List<ArchiveEntry>();
            Span<byte> nameBuf = stackalloc byte[256];
            while (loadStream.Position < minOffs)
            {
                if (loadStream.Read(addressBuffer) < addressBuffer.Length)
                    throw new FileTypeLoadException("Archive not long enough to read file offset.");
                address = (uint)ArrayUtils.ReadIntFromByteArray(addressBuffer, true);
                if (address == 0)
                {
                    foundNullAddress = true;
                    if (curEntry is { Length: -1 } && this.PakVer == PakVersion.PakVersion2)
                        curEntry.Length = (int)end - curEntry.StartOffset;
                }
                if (address == end && this.PakVer == PakVersion.PakVersion1)
                {
                    foundEndAddress = true;
                    if (curEntry is { Length: -1 })
                        curEntry.Length = (int)end - curEntry.StartOffset;
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

                if (curEntry is { Length: -1 })
                    curEntry.Length = (int)address - curEntry.StartOffset;

                int curNamePos;
                for (curNamePos = 0; curNamePos < nameBuf.Length; curNamePos++)
                {
                    var curByte = loadStream.ReadByte();
                    if (curByte == -1)
                        throw new FileTypeLoadException("Archive not long enough to read file name.");
                    if (curByte == 0)
                        break;
                    if (curByte < 0x20)
                        throw new FileTypeLoadException("Illegal values in file name.");
                    nameBuf[curNamePos] = (byte)curByte;
                }
                if (curNamePos >= 13)
                {
                    foundLongFileName = true;
                    if (curNamePos >= nameBuf.Length)
                        throw new FileTypeLoadException("Bad file name.");
                }

                ReadOnlySpan<byte> roNameBuf = nameBuf[..curNamePos];
                var curName = Encoding.ASCII.GetString(roNameBuf);
                if (curName.Length == 0)
                {
                    foundNullName = true;
                    if ((curEntry == null || (curEntry.Length == -1 && address > curEntry.StartOffset)) && address != 0 && address <= end && this.PakVer == PakVersion.PakVersion3)
                    {
                        foundEndAddressEntryV3 = true;
                        if (curEntry != null)
                            curEntry.Length = (int)address - curEntry.StartOffset;
                    }
                }
                if (!foundEndAddress && !foundNullAddress && !foundNullName)
                {
                    curEntry = new ArchiveEntry(curName, archivePath, (int)address, -1);
                    minOffs = Math.Min(minOffs, address);
                    filesList.Add(curEntry);
                }
            }

            if (foundLongFileName)
                this.ExtraInfo = "File contains long file names.";

            switch (this.PakVer)
            {
                case PakVersion.PakVersion3 when (!foundNullName || !foundEndAddressEntryV3 || !foundNullAddress):
                    throw new FileTypeLoadException("This is not a v3 PAK file.");
                case PakVersion.PakVersion2 when (foundNullName || foundEndAddress || !foundNullAddress):
                    throw new FileTypeLoadException("This is not a v2 PAK file.");
                case PakVersion.PakVersion1 when (foundNullName || foundNullAddress):
                    throw new FileTypeLoadException("This is not a v1 PAK file.");
                case PakVersion.PakVersion1 when !foundEndAddress && loadStream.Position == minOffs && curEntry is { Length: -1 }:
                    // Seems to be a problem in some v1 pak files where the last entry is gibberish.
                    curEntry.Length = (int)end - curEntry.StartOffset;
                    var warning = "File has corrupted end offset: " + address.ToString("X8");
                    if (this.ExtraInfo == null)
                        this.ExtraInfo = warning;
                    else
                        this.ExtraInfo = ExtraInfo + " " + warning;
                    break;
            }

            // All cases should be handled.
            if (curEntry is { Length: -1 })
                throw new FileTypeLoadException("This is not a PAK file.");
            // Not gonna allow this. Too much chance on empty edge cases.
            if (filesList.Count == 0)
                throw new FileTypeLoadException("Not entries in PAK file.");
            return filesList;
        }

        public override bool SaveArchive(Archive archive, Stream saveStream, string savePath)
        {
            var entries = archive.FilesList.ToArray();
            // Filename lengths + version-dependent padding
            var firstFileOffset = entries.Sum(en => en.FileName.Length) + entries.Length * 5;
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
            var fileOffset = firstFileOffset;
            var enc = Encoding.GetEncoding(437);

            var buffer = new byte[13];
            using (var bw = new BinaryWriter(saveStream, Encoding.ASCII, true))
            {
                foreach (var entry in entries)
                {
                    var fileLength = entry.Length;
                    var curName = entry.FileName;
                    if (entry.PhysicalPath != null)
                    {
                        // To be 100% sure the index is OK, this is updated at the moment of writing.
                        var fi = new FileInfo(entry.PhysicalPath);
                        if (!fi.Exists)
                            throw new FileNotFoundException("Cannot find file \"" + entry.PhysicalPath + "\" to write to archive!");
                        fileLength = (int)fi.Length;
                        // not really necessary since the input method takes care of it, but, just to be sure.
                        curName = this.GetInternalFilename(entry.FileName);
                    }
                    bw.Write(fileOffset);
                    fileOffset += fileLength;
                    var copySize = curName.Length;
                    Array.Copy(enc.GetBytes(curName), 0, buffer, 0, copySize);
                    if (buffer.Length > copySize)
                        Array.Clear(buffer, copySize, buffer.Length - copySize);
                    bw.Write(buffer, 0, copySize + 1);
                }
                switch (this.PakVer)
                {
                    case PakVersion.PakVersion1:
                        bw.Write(fileOffset);
                        break;
                    case PakVersion.PakVersion2:
                        bw.Write(0);
                        break;
                    case PakVersion.PakVersion3:
                        bw.Write(fileOffset);
                        bw.Write(0);
                        bw.Write((byte)0);
                        break;
                }
            }
            if (firstFileOffset != saveStream.Position)
                throw new IndexOutOfRangeException("Programmer error: write start offset does not match end of index.");
            foreach (var entry in entries)
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