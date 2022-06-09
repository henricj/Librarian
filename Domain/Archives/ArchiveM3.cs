using Nyerguds.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace LibrarianTool.Domain.Archives
{
    /// <summary>
    /// Interactive Girls Club .m3 / .slb archive format. 
    /// Very simple archive without file names. To handle internal order correctly, just name files accordingly.
    /// </summary>
    public class ArchiveM3 : Archive
    {
        protected const int FileEntryLength = 0x11;

        public override string ShortTypeName => "Interactive Girls Archive";
        public override string ShortTypeDescription => "Interactive Girls Archive";
        public override string[] FileExtensions { get { return new[] { "m3", "slb" }; } }

        protected override List<ArchiveEntry> LoadArchiveInternal(Stream loadStream, string archivePath)
        {
            loadStream.Position = 0;
            var streamLength = loadStream.Length;
            Span<byte> addressBuffer = stackalloc byte[4];
            if (loadStream.Read(addressBuffer) < addressBuffer.Length)
                throw new FileTypeLoadException("Archive not long enough to read file offset!");
            if (ArrayUtils.ReadIntFromByteArray(addressBuffer, true) != 0)
                throw new FileTypeLoadException("Not an IGC Archive!");
            var readOffs = 4;
            var minOffs = int.MaxValue;
            var indexOffs = 0;
            var filesList = new List<ArchiveEntry>();
            do
            {
                var prevIndexOffs = indexOffs;
                if (loadStream.Read(addressBuffer) < addressBuffer.Length)
                    throw new FileTypeLoadException("Archive not long enough to read file offset!");
                indexOffs = (int)ArrayUtils.ReadIntFromByteArray(addressBuffer, true);
                minOffs = Math.Min(minOffs, indexOffs);
                if (indexOffs > streamLength || prevIndexOffs >= indexOffs)
                    throw new FileTypeLoadException("Not an IGC archive!");
                if (prevIndexOffs != 0)
                {
                    var isImage = prevIndexOffs + 0x1B <= indexOffs;
                    var isScript = prevIndexOffs + 2 <= indexOffs;
                    if (isImage || isScript)
                    {
                        // Check for image                        
                        loadStream.Position = prevIndexOffs;
                        ushort script;
                        if (!isImage)
                        {
                            if (2 != loadStream.Read(addressBuffer[..2]))
                                throw new FileTypeLoadException("Archive not long enough to read script!");
                            script = (ushort)ArrayUtils.ReadIntFromByteArray(addressBuffer[..2], true);
                        }
                        else
                        {
                            if (addressBuffer.Length != loadStream.Read(addressBuffer))
                                throw new FileTypeLoadException("Archive not long enough to read magic!");
                            var magic01 = (uint)ArrayUtils.ReadIntFromByteArray(addressBuffer, true);
                            script = (ushort)ArrayUtils.ReadIntFromByteArray(addressBuffer[..2], true);
                            loadStream.Position = prevIndexOffs + 0x12;
                            if (addressBuffer.Length != loadStream.Read(addressBuffer))
                                throw new FileTypeLoadException("Archive not long enough to read magic2!");
                            var magic02 = (uint)ArrayUtils.ReadIntFromByteArray(addressBuffer, true);
                            isImage = magic01 == 0x01325847 && magic02 == 0x58465053;
                        }
                        isScript = !isImage && script == 0x7E7C;
                        loadStream.Position = readOffs;
                    }
                    var filename = filesList.Count.ToString("00000000", CultureInfo.InvariantCulture) + "." + (isImage ? "gx2" : (isScript ? "txt" : "dat"));
                    filesList.Add(new ArchiveEntry(filename, archivePath, prevIndexOffs, indexOffs - prevIndexOffs));
                }
                readOffs += 4;
                loadStream.Position = readOffs;
            } while (readOffs < minOffs && indexOffs < streamLength);
            if (readOffs != minOffs || indexOffs != streamLength)
                throw new FileTypeLoadException("Not an IGC archive!");
            return filesList;
        }

        public override string GetInternalFilename(string filePath)
        {
            return Path.GetFileName(filePath);
        }

        public override bool SaveArchive(Archive archive, Stream saveStream, string savePath)
        {
            var entries = archive.FilesList.ToArray();
            var firstFileOffset = (entries.Length + 2) * 4;
            var fileOffset = firstFileOffset;
            using (var bw = new BinaryWriter(saveStream, Encoding.UTF8, true))
            {
                bw.Write((uint)0);
                foreach (var entry in entries)
                {
                    var fileLength = entry.Length;
                    if (entry.PhysicalPath != null)
                    {
                        // To be 100% sure the index is OK, this is updated at the moment of writing.
                        var fi = new FileInfo(entry.PhysicalPath);
                        if (!fi.Exists)
                            throw new FileNotFoundException("Cannot find file \"" + entry.PhysicalPath + "\" to write to archive!");
                        fileLength = (int)fi.Length;
                    }
                    bw.Write(fileOffset);
                    fileOffset += fileLength;
                }
                bw.Write(fileOffset);
            }
            if (firstFileOffset != saveStream.Position)
                throw new ArgumentException("Programmer error: write start offset does not match end of index.");
            foreach (var entry in entries)
                CopyEntryContentsToStream(entry, saveStream);
            return true;
        }
    }
}