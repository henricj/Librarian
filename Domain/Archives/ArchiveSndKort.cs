using LibrarianTool.Domain.Utils;
using Nyerguds.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace LibrarianTool.Domain.Archives
{
    public class ArchiveSndKort : Archive
    {
        public override string ShortTypeName => "KORT SND Archive";
        public override string ShortTypeDescription => "KORT SND";
        public override string[] FileExtensions { get { return new[] { "SND" }; } }

        const string BufferInfoFormat = "Buffer info: 0x{0:X8}";

        protected override List<ArchiveEntry> LoadArchiveInternal(Stream loadStream, string archivePath)
        {
            loadStream.Position = 0;
            ExtraInfo = string.Empty;
            Span<byte> filesCount = stackalloc byte[2];
            var amount = loadStream.Read(filesCount);
            var nrOfFiles = (int)ArrayUtils.ReadIntFromByteArray(filesCount, true);
            if (amount != 2)
                throw new FileTypeLoadException("Too short to be a " + ShortTypeDescription + " archive.");
            Span<byte> buffer = stackalloc byte[25];
            long firstPos = 2 + nrOfFiles * 25;
            var filesList = new List<ArchiveEntry>();
            while (loadStream.Position < firstPos)
            {
                amount = loadStream.Read(buffer);
                if (amount < buffer.Length)
                    throw new FileTypeLoadException("Header too small! Not a " + ShortTypeDescription + " archive.");
                var size = (uint)ArrayUtils.ReadIntFromByteArray(buffer[..4], true);
                var buff = (uint)ArrayUtils.ReadIntFromByteArray(buffer[4..8], true);
                var buffBytes = new byte[4];
                buffer.Slice(4, 4).CopyTo(buffBytes);
                var offset = (uint)ArrayUtils.ReadIntFromByteArray(buffer.Slice(8, 4), true);
                if (offset + size > loadStream.Length)
                    throw new FileTypeLoadException("Header refers to data outside the file! Not a " + ShortTypeDescription + " archive.");
                if (offset < firstPos)
                    throw new FileTypeLoadException("Header refers to data inside header! Not a " + ShortTypeDescription + " archive.");
                ReadOnlySpan<byte> filenameB = buffer[12..];
                var index = filenameB.IndexOf((byte)0);
                if (index >= 0)
                    filenameB = filenameB[..index];
                foreach (var c in filenameB)
                {
                    if (c <= 0x20 || c > 0x7F)
                        throw new FileTypeLoadException("Non-ASCII filename characters found in header! Not a " + ShortTypeDescription + " archive.");
                }

                var filename = Encoding.ASCII.GetString(filenameB);
                var archiveEntry = new ArchiveEntry(filename, archivePath, (int)offset, (int)size);
                var sbExtraInfo = new StringBuilder();
                sbExtraInfo.Append(string.Format(CultureInfo.InvariantCulture, BufferInfoFormat, buff));
                IdentifyType(loadStream, offset, size, sbExtraInfo);
                archiveEntry.ExtraInfo = sbExtraInfo.ToString();
                archiveEntry.ExtraInfoBin = buffBytes;
                filesList.Add(archiveEntry);
            }
            ExtraInfo = "WARNING - The unknown 'Buffer' value will only be preserved when REPLACING files.";
            return filesList;
        }

        protected static void IdentifyType(Stream loadStream, uint offset, uint size, StringBuilder sbExtraInfo)
        {
            var isVoc = false;
            var savedPos = loadStream.Position;
            if (size > 19u)
            {
                var buff = new byte[19];
                loadStream.Position = offset;
                loadStream.Read(buff, 0, buff.Length);
                if (Encoding.ASCII.GetString(buff).Equals("Creative Voice File", StringComparison.Ordinal))
                {
                    sbExtraInfo.Append("\nType: Creative Voice File");
                    isVoc = true;
                }
            }
            if (!isVoc && size > 8u)
            {
                var buff = new byte[8];
                loadStream.Position = offset;
                loadStream.Read(buff, 0, buff.Length);
                if (Encoding.ASCII.GetString(buff, 0, 4).Equals("CTMF", StringComparison.Ordinal))
                {
                    sbExtraInfo.Append("\nType: Creative Music Format");
                }
            }
            loadStream.Position = savedPos;
        }

        protected override ArchiveEntry InsertFileInternal(string filePath, string internalFilename, int foundIndex)
        {
            ArchiveEntry entry;
            byte[] extraInfoBin;
            var sb = new StringBuilder();
            if (foundIndex == -1)
            {
                entry = new ArchiveEntry(filePath, internalFilename);
                extraInfoBin = new byte[4];
            }
            else
            {
                entry = new ArchiveEntry(filePath, internalFilename);
                extraInfoBin = FilesList[foundIndex].ExtraInfoBin;
            }
            if (extraInfoBin != null && extraInfoBin.Length >= 4)
            {
                var buff = (uint)ArrayUtils.ReadIntFromByteArray(extraInfoBin.AsSpan(0, 4), true);
                sb.Append(string.Format(CultureInfo.InvariantCulture, BufferInfoFormat, buff));
            }
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                IdentifyType(fs, 0u, (uint)fs.Length, sb);
            }
            entry.ExtraInfo = sb.ToString();
            entry.ExtraInfoBin = extraInfoBin;
            if (foundIndex == -1)
                FilesList.Add(entry);
            else
                FilesList[foundIndex] = entry;
            return entry;
        }

        protected override void OrderFilesListInternal(List<ArchiveEntry> filesList)
        {
            var orderedList = filesList.OrderBy(x => x.FileName, new ExtensionSorter()).ToList();
            filesList.Clear();
            filesList.AddRange(orderedList);
        }

        public override bool SaveArchive(Archive archive, Stream saveStream, string savePath)
        {
            var filesList = archive.FilesList.ToList();
            OrderFilesListInternal(filesList);
            var nrOfFiles = filesList.Count;
            var firstFileOffset = 2 + 25 * nrOfFiles;
            var fileOffset = firstFileOffset;
            var enc = Encoding.GetEncoding(437);
            var nameBuffer = new byte[13];
            using (var bw = new BinaryWriter(saveStream, Encoding.UTF8, true))
            {
                bw.Write((ushort)nrOfFiles);
                foreach (var entry in filesList)
                {
                    var fileLength = entry.Length;
                    var curName = entry.FileName;
                    if (entry.PhysicalPath != null)
                    {
                        var fi = new FileInfo(entry.PhysicalPath);
                        if (!fi.Exists)
                            throw new FileNotFoundException("Cannot find file \"" + entry.PhysicalPath + "\" to write to archive!");
                        fileLength = (int)fi.Length;
                        curName = GetInternalFilename(entry.FileName);
                    }
                    bw.Write(fileLength);
                    var buff = 0u;
                    var extraInfoBin = entry.ExtraInfoBin;
                    if (extraInfoBin is { Length: >= 4 })
                        buff = (uint)ArrayUtils.ReadIntFromByteArray(extraInfoBin.AsSpan(0, 4), true);
                    bw.Write(buff);
                    bw.Write(fileOffset);
                    fileOffset += fileLength;
                    var copySize = curName.Length;
                    Array.Copy(enc.GetBytes(curName), 0, nameBuffer, 0, copySize);
                    for (var b = copySize; b < 13; b++)
                    {
                        nameBuffer[b] = 0;
                    }
                    bw.Write(nameBuffer, 0, nameBuffer.Length);
                }
            }
            if (firstFileOffset != saveStream.Position)
                throw new ArchiveException("Programmer error: write start offset does not match end of index.");
            foreach (var entry in filesList)
                CopyEntryContentsToStream(entry, saveStream);
            return true;
        }
    }
}
