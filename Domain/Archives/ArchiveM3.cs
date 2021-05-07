using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Nyerguds.Util;

namespace LibrarianTool.Domain.Archives
{
    /// <summary>
    /// Interactive Girls Club .m3 / .slb archive format. 
    /// Very simple archive without file names. To handle internal order correctly, just name files accordingly.
    /// </summary>
    public class ArchiveM3 : Archive
    {
        protected const Int32 FileEntryLength = 0x11;

        public override String ShortTypeName { get { return "Interactive Girls Archive"; } }
        public override String ShortTypeDescription { get { return "Interactive Girls Archive"; } }
        public override String[] FileExtensions { get { return new String[] { "m3", "slb" }; } }

        protected override List<ArchiveEntry> LoadArchiveInternal(Stream loadStream, String archivePath)
        {
            loadStream.Position = 0;
            Int64 streamLength = loadStream.Length;
            Byte[] addressBuffer = new Byte[4];
            if (loadStream.Read(addressBuffer, 0, 4) < 4)
                throw new FileTypeLoadException("Archive not long enough to read file offset!");
            if (ArrayUtils.ReadIntFromByteArray(addressBuffer, 0, 4, true) != 0)
                throw new FileTypeLoadException("Not an IGC Archive!");
            Int32 readOffs = 4;
            Int32 minOffs = Int32.MaxValue;
            Int32 indexOffs = 0;
            List<ArchiveEntry> filesList = new List<ArchiveEntry>();
            do
            {
                Int32 prevIndexOffs = indexOffs;
                if (loadStream.Read(addressBuffer, 0, 4) < 4)
                    throw new FileTypeLoadException("Archive not long enough to read file offset!");
                indexOffs = (Int32)ArrayUtils.ReadIntFromByteArray(addressBuffer, 0, 4, true);
                minOffs = Math.Min(minOffs, indexOffs);
                if (indexOffs > streamLength || prevIndexOffs >= indexOffs)
                    throw new FileTypeLoadException("Not an IGC archive!");
                if (prevIndexOffs != 0)
                {
                    Boolean isImage = prevIndexOffs + 0x1B <= indexOffs;
                    Boolean isScript = prevIndexOffs + 2 <= indexOffs;
                    if (isImage || isScript)
                    {
                        // Check for image                        
                        loadStream.Position = prevIndexOffs;
                        UInt16 script;
                        if (!isImage)
                        {
                            loadStream.Read(addressBuffer, 0, 2);
                            script = (UInt16)ArrayUtils.ReadIntFromByteArray(addressBuffer, 0, 2, true);
                        }
                        else
                        {
                            loadStream.Read(addressBuffer, 0, 4);
                            UInt32 magic01 = (UInt32) ArrayUtils.ReadIntFromByteArray(addressBuffer, 0, 4, true);
                            script = (UInt16)ArrayUtils.ReadIntFromByteArray(addressBuffer, 0, 2, true);
                            loadStream.Position = prevIndexOffs + 0x12;
                            loadStream.Read(addressBuffer, 0, 4);
                            UInt32 magic02 = (UInt32) ArrayUtils.ReadIntFromByteArray(addressBuffer, 0, 4, true);
                            isImage = magic01 == 0x01325847 && magic02 == 0x58465053;
                        }
                        isScript = !isImage && script == 0x7E7C;
                        loadStream.Position = readOffs;
                    }
                    String filename = filesList.Count.ToString("00000000") + "." + (isImage ? "gx2" : (isScript? "txt" : "dat"));
                    filesList.Add(new ArchiveEntry(filename, archivePath, prevIndexOffs, indexOffs - prevIndexOffs));
                }
                readOffs += 4;
                loadStream.Position = readOffs;
            } while (readOffs < minOffs && indexOffs < streamLength);
            if (readOffs != minOffs || indexOffs != streamLength)
                throw new FileTypeLoadException("Not an IGC archive!");
            return filesList;
        }

        public override String GetInternalFilename(String filePath)
        {
            return Path.GetFileName(filePath);
        }

        public override Boolean SaveArchive(Archive archive, Stream saveStream, String savePath)
        {
            ArchiveEntry[] entries = archive.FilesList.ToArray();
            Int32 firstFileOffset = (entries.Length + 2) * 4;
            Int32 fileOffset = firstFileOffset;
            using (BinaryWriter bw = new BinaryWriter(new NonDisposingStream(saveStream)))
            {
                bw.Write((UInt32)0);
                foreach (ArchiveEntry entry in entries)
                {
                    Int32 fileLength = entry.Length;
                    if (entry.PhysicalPath != null)
                    {
                        // To be 100% sure the index is OK, this is updated at the moment of writing.
                        FileInfo fi = new FileInfo(entry.PhysicalPath);
                        if (!fi.Exists)
                            throw new FileNotFoundException("Cannot find file \"" + entry.PhysicalPath + "\" to write to archive!");
                        fileLength = (Int32)fi.Length;
                    }
                    bw.Write(fileOffset);
                    fileOffset += fileLength;
                }
                bw.Write(fileOffset);
            }
            if (firstFileOffset != saveStream.Position)
                throw new IndexOutOfRangeException("Programmer error: write start offset does not match end of index.");
            foreach (ArchiveEntry entry in entries)
                CopyEntryContentsToStream(entry, saveStream);
            return true;
        }
    }
}