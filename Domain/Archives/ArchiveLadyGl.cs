using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Nyerguds.Util;

namespace LibrarianTool.Domain.Archives
{
    public class ArchiveLadyGl : Archive
    {
        const Int32 FileNameLength = 0x0D;
        const Int32 FileEntryLength = FileNameLength + 8;

        public override String ShortTypeName { get { return "LadyLove GL/GLT Archive"; } }

        public override String ShortTypeDescription { get { return "LadyLove GL/GLT Archive"; } }

        public override String[] FileExtensions { get { return new String[] {"glt"}; } }

        protected override List<ArchiveEntry> LoadArchiveInternal(Stream loadStream, String archivePath)
        {
            if (archivePath == null)
                throw new FileTypeLoadException("Need path to identify this type.");
            String basePath = Path.GetDirectoryName(archivePath);
            String baseName = Path.Combine(basePath, Path.GetFileNameWithoutExtension(archivePath));
            String ext = Path.GetExtension(archivePath);
            String curStreamName = archivePath;
            String secondStreamName;
            Boolean secondStreamIsContent;
            if (".GLT".Equals(ext, StringComparison.InvariantCultureIgnoreCase))
            {
                secondStreamIsContent = true;
                secondStreamName = baseName + ".GL";
            }
            else if (".GL".Equals(ext, StringComparison.InvariantCultureIgnoreCase))
            {
                secondStreamIsContent = false;
                secondStreamName = baseName + ".GLT";
            }
            else
            {
                if (File.Exists(baseName + ".GL"))
                {
                    secondStreamIsContent = true;
                    secondStreamName = baseName + ".GL";
                }
                else if (File.Exists(baseName + ".GLT"))
                {
                    secondStreamIsContent = false;
                    secondStreamName = baseName + ".GLT";
                }
                else
                    throw new FileTypeLoadException("Cannot find accompanying file.");
            }
            if (!File.Exists(secondStreamName))
                throw new FileTypeLoadException("Cannot find accompanying file.");
            List<ArchiveEntry> filesList = new List<ArchiveEntry>();
            using (FileStream secondStream = File.OpenRead(secondStreamName))
            {
                Stream tableData = secondStreamIsContent ? loadStream : secondStream;
                Stream archiveData = secondStreamIsContent ? secondStream : loadStream;
                String archiveDataName = secondStreamIsContent ? secondStreamName : curStreamName;
                this.FileName = secondStreamIsContent ? curStreamName : secondStreamName;
                this.ExtraInfo = "Data archive: " + Path.GetFileName(archiveDataName);

                Int32 tableLength = (Int32) tableData.Length;
                Int32 dataLength = (Int32) archiveData.Length;
                if (tableLength % FileEntryLength != 0)
                    throw new FileTypeLoadException("Table data does not exact amount of entries.");
                Int32 frameNr = 0;
                List<Int32[]> contentOverlapCheck = new List<Int32[]>();
                while (true)
                {
                    Byte[] nameBuf = new Byte[FileEntryLength];
                    Int32 readAmount = tableData.Read(nameBuf, 0, FileEntryLength);
                    if (readAmount < FileEntryLength)
                        break;
                    String fileName = new String(nameBuf.TakeWhile(b => b != 0).Select(c => (Char) (c <= 0x20 || c > 0x7F ? 0 : c)).ToArray());
                    if (fileName.Contains('\0'))
                        throw new FileTypeLoadException("Non-ascii characters in internal filename.");
                    String[] nameSplit = fileName.Split('.');
                    Int32 actualNameLen = fileName.Length;
                    if (actualNameLen == 0 || actualNameLen > 12 || nameSplit[0].Length > 8 || nameSplit.Length > 2 || (nameSplit.Length == 2 && nameSplit[1].Length > 3))
                        throw new FileTypeLoadException("Internal filename does not match DOS 8.3 format.");
                    Int32 fileOffset = (Int32) ArrayUtils.ReadIntFromByteArray(nameBuf, FileNameLength, 4, true);
                    Int32 fileLength = (Int32) ArrayUtils.ReadIntFromByteArray(nameBuf, FileNameLength + 4, 4, true);
                    if (fileOffset < 0 || fileLength < 0)
                        throw new FileTypeLoadException("Bad data in table.");
                    Int32 fileEnd = fileOffset + fileLength;

                    for (Int32 i = 0; i < frameNr; ++i)
                    {
                        Int32[] prevFrameLen = contentOverlapCheck[i];
                        Int32 prevStart = prevFrameLen[0];
                        Int32 prevEnd = prevFrameLen[1];
                        if ((fileOffset >= prevStart && fileOffset < prevEnd) || (fileEnd >= prevStart && fileEnd < prevEnd))
                            throw new FileTypeLoadException("Overlapping files in table.");
                    }
                    contentOverlapCheck.Add(new Int32[] {fileOffset, fileEnd});
                    if (dataLength < fileEnd)
                        throw new FileTypeLoadException("Internal file does not fit in archive.");
                    filesList.Add(new ArchiveEntry(fileName, archiveDataName, fileOffset, fileLength));
                    frameNr++;
                }
            }
            return filesList;
        }

        public override Boolean SaveArchive(Archive archive, Stream saveStream, String savePath)
        {
            if (savePath == null)
                throw new ArgumentException("This type needs a filename since it writes its data to an accompanying file.");
            if ( ".GL".Equals(Path.GetExtension(savePath)))
                throw new ArgumentException("Suggested name cannot have extension \".gl\"; it is reserved for the data file.");
            ArchiveEntry[] entries = archive.FilesList.ToArray();
            Int32 nrOfEntries = entries.Length;
            String dataPath = Path.Combine(Path.GetDirectoryName(savePath), Path.GetFileNameWithoutExtension(savePath) + ".gl");

            Byte[] buffer = new Byte[FileEntryLength];
            using (FileStream dataSaveStream = File.OpenWrite(dataPath))
            {
                for (Int32 i = 0; i < nrOfEntries; ++i)
                {
                    ArchiveEntry entry = entries[i];
                    if (entry.FileName.Any(c => c <= 0x20 || c > 0x7F))
                        throw new ArgumentException("Filenames must be pure ASCII.");
                    String fileName = entry.FileName;
                    String[] nameSplit = fileName.Split('.');
                    Int32 actualNameLen = fileName.Length;
                    if (actualNameLen == 0 || actualNameLen > 12 || nameSplit[0].Length > 8 || nameSplit.Length > 2 || (nameSplit.Length == 2 && nameSplit[1].Length > 3))
                        throw new FileTypeLoadException("Filenames must match DOS 8.3 format.");
                    Byte[] nameBytes = Encoding.ASCII.GetBytes(entry.FileName);
                    Array.Clear(buffer, 0, FileNameLength);
                    Array.Copy(nameBytes, buffer, nameBytes.Length);
                    ArrayUtils.WriteIntToByteArray(buffer, FileNameLength, 4, true, (UInt32)dataSaveStream.Position);
                    ArrayUtils.WriteIntToByteArray(buffer, FileNameLength + 4, 4, true, (UInt32) entry.Length);
                    saveStream.Write(buffer, 0, FileEntryLength);
                    CopyEntryContentsToStream(entry, dataSaveStream);
                }
            }
            return true;
        }
    }
}