using Nyerguds.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LibrarianTool.Domain.Archives
{
    public class ArchiveLadyGl : Archive
    {
        const int FileNameLength = 0x0D;
        const int FileEntryLength = FileNameLength + 8;

        public override string ShortTypeName => "LadyLove GL/GLT Archive";

        public override string ShortTypeDescription => "LadyLove GL/GLT Archive";

        public override string[] FileExtensions { get { return new[] { "glt" }; } }

        protected override List<ArchiveEntry> LoadArchiveInternal(Stream loadStream, string archivePath)
        {
            if (archivePath == null)
                throw new FileTypeLoadException("Need path to identify this type.");
            var basePath = Path.GetDirectoryName(archivePath);
            var baseName = Path.Combine(basePath, Path.GetFileNameWithoutExtension(archivePath));
            var ext = Path.GetExtension(archivePath);
            var curStreamName = archivePath;
            string secondStreamName;
            bool secondStreamIsContent;
            if (".GLT".Equals(ext, StringComparison.OrdinalIgnoreCase))
            {
                secondStreamIsContent = true;
                secondStreamName = baseName + ".GL";
            }
            else if (".GL".Equals(ext, StringComparison.OrdinalIgnoreCase))
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
            var filesList = new List<ArchiveEntry>();
            using var secondStream = File.OpenRead(secondStreamName);
            var tableData = secondStreamIsContent ? loadStream : secondStream;
            var archiveData = secondStreamIsContent ? secondStream : loadStream;
            var archiveDataName = secondStreamIsContent ? secondStreamName : curStreamName;
            FileName = secondStreamIsContent ? curStreamName : secondStreamName;
            ExtraInfo = "Data archive: " + Path.GetFileName(archiveDataName);

            var tableLength = (int)tableData.Length;
            var dataLength = (int)archiveData.Length;
            if (tableLength % FileEntryLength != 0)
                throw new FileTypeLoadException("Table data does not exact amount of entries.");
            var frameNr = 0;
            var contentOverlapCheck = new List<int[]>();
            Span<byte> nameBuf = stackalloc byte[FileEntryLength];
            while (true)
            {
                var readAmount = tableData.Read(nameBuf);
                if (readAmount < nameBuf.Length)
                    break;
                ReadOnlySpan<byte> fileNameB = nameBuf[..FileNameLength];
                var index = nameBuf.IndexOf((byte)0);
                if (index >= 0)
                    fileNameB = fileNameB[..index];
                foreach (var c in fileNameB)
                {
                    if (c is <= 0x20 or > 0x7F)
                        throw new FileTypeLoadException("Non-ascii characters in internal filename.");
                }
                var fileName = Encoding.ASCII.GetString(fileNameB);
                var nameSplit = fileName.Split('.');
                var actualNameLen = fileName.Length;
                if (actualNameLen == 0 || actualNameLen > 12 || nameSplit[0].Length > 8 || nameSplit.Length > 2 || (nameSplit.Length == 2 && nameSplit[1].Length > 3))
                    throw new FileTypeLoadException("Internal filename does not match DOS 8.3 format.");
                var fileOffset = (int)ArrayUtils.ReadIntFromByteArray(nameBuf.Slice(FileNameLength, 4), true);
                var fileLength = (int)ArrayUtils.ReadIntFromByteArray(nameBuf.Slice(FileNameLength + 4, 4), true);
                if (fileOffset < 0 || fileLength < 0)
                    throw new FileTypeLoadException("Bad data in table.");
                var fileEnd = fileOffset + fileLength;

                for (var i = 0; i < frameNr; ++i)
                {
                    var prevFrameLen = contentOverlapCheck[i];
                    var prevStart = prevFrameLen[0];
                    var prevEnd = prevFrameLen[1];
                    if ((fileOffset >= prevStart && fileOffset < prevEnd) || (fileEnd >= prevStart && fileEnd < prevEnd))
                        throw new FileTypeLoadException("Overlapping files in table.");
                }
                contentOverlapCheck.Add(new[] { fileOffset, fileEnd });
                if (dataLength < fileEnd)
                    throw new FileTypeLoadException("Internal file does not fit in archive.");
                filesList.Add(new ArchiveEntry(fileName, archiveDataName, fileOffset, fileLength));
                frameNr++;
            }

            return filesList;
        }

        public override bool SaveArchive(Archive archive, Stream saveStream, string savePath)
        {
            if (savePath == null)
                throw new ArgumentException("This type needs a filename since it writes its data to an accompanying file.");
            if (".GL".Equals(Path.GetExtension(savePath), StringComparison.Ordinal))
                throw new ArgumentException("Suggested name cannot have extension \".gl\"; it is reserved for the data file.");
            var entries = archive.FilesList.ToArray();
            var nrOfEntries = entries.Length;
            var dataPath = Path.Combine(Path.GetDirectoryName(savePath), Path.GetFileNameWithoutExtension(savePath) + ".gl");

            var buffer = new byte[FileEntryLength];
            using var dataSaveStream = File.OpenWrite(dataPath);
            for (var i = 0; i < nrOfEntries; ++i)
            {
                var entry = entries[i];
                if (entry.FileName.Any(c => c <= 0x20 || c > 0x7F))
                    throw new ArgumentException("Filenames must be pure ASCII.");
                var fileName = entry.FileName;
                var nameSplit = fileName.Split('.');
                var actualNameLen = fileName.Length;
                if (actualNameLen == 0 || actualNameLen > 12 || nameSplit[0].Length > 8 || nameSplit.Length > 2 || (nameSplit.Length == 2 && nameSplit[1].Length > 3))
                    throw new FileTypeLoadException("Filenames must match DOS 8.3 format.");
                var nameBytes = Encoding.ASCII.GetBytes(entry.FileName);
                Array.Clear(buffer, 0, FileNameLength);
                Array.Copy(nameBytes, buffer, nameBytes.Length);
                ArrayUtils.WriteIntToByteArray(buffer, FileNameLength, 4, true, (uint)dataSaveStream.Position);
                ArrayUtils.WriteIntToByteArray(buffer, FileNameLength + 4, 4, true, (uint)entry.Length);
                saveStream.Write(buffer, 0, FileEntryLength);
                CopyEntryContentsToStream(entry, dataSaveStream);
            }

            return true;
        }
    }
}