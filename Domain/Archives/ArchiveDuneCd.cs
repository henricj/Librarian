using Nyerguds.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LibrarianTool.Domain.Archives
{
    public class ArchiveDuneCd : Archive
    {
        protected const Int32 FileEntryLength = 0x19;

        public override String ShortTypeName { get { return "Dune CD Archive"; } }
        public override String ShortTypeDescription { get { return "Dune CD Archive"; } }
        public override String[] FileExtensions { get { return new String[] { "dat" }; } }
        public override Boolean CanSave { get { return false; } }

        protected override List<ArchiveEntry> LoadArchiveInternal(Stream loadStream, String archivePath)
        {
            Encoding enc = Encoding.GetEncoding(437);
            Int64 end = loadStream.Length;
            Byte[] buffer = new Byte[FileEntryLength];
            List<ArchiveEntry> filesList = new List<ArchiveEntry>();
            if (end - loadStream.Position < 0x02)
                throw new FileTypeLoadException("Not a Dune CD Archive.");
            loadStream.Read(buffer, 0, 2);
            Int32 length = (Int32)ArrayUtils.ReadIntFromByteArray(buffer, 0, 2, true);
            if (end - loadStream.Position < (length * FileEntryLength))
                throw new FileTypeLoadException("Not a Dune CD Archive.");
            for (Int32 i = 0; i < length; i++)
            {
                loadStream.Read(buffer, 0, FileEntryLength);
                Byte[] curNameB = buffer.Take(0x10).TakeWhile(x => x != 0).ToArray();
                if (curNameB.Length == 0)
                    break;
                if (curNameB.Any(c => c < 0x20 || c >= 0x7F))
                    throw new FileTypeLoadException("Filename contains nonstandard characters.");
                String curName = enc.GetString(curNameB).Trim();
                Int32 curEntryLength = (Int32)ArrayUtils.ReadIntFromByteArray(buffer, 0x10, 4, true);
                Int32 curEntryPos = (Int32)ArrayUtils.ReadIntFromByteArray(buffer, 0x14, 4, true);
                if (curEntryPos + curEntryLength > end)
                    throw new FileTypeLoadException("Archive entry outside file bounds.");
                if (curName.Length == 0 && curEntryLength == 0)
                    continue;
                filesList.Add(new ArchiveEntry(curName, archivePath, curEntryPos, curEntryLength));
            }
            return filesList;
        }
        
        /// <summary>
        /// Converts the filename to the type supported internally. By default, this strips
        /// out all non-ascii characters, converts to uppercase, and limits the length to 8.3.
        /// Override if needed.
        /// </summary>
        /// <param name="filePath">Original file path.</param>
        /// <returns></returns>
        public override String GetInternalFilename(String filePath)
        {
            // TODO: Something still wrong in this code. Not sure what. Investigate later.
            String path = Path.GetDirectoryName(filePath) ?? String.Empty;
            path = path.Trim('\\');
            String filename = Path.GetFileNameWithoutExtension(filePath) ?? String.Empty;
            String extension = Path.GetExtension(filePath) ?? String.Empty;
            if (filename.Length > 8)
                filename = filename.Substring(0, 8);
            if (extension.Length > 4)
                extension = extension.Substring(0, 4);
            // 0x0E: 0x10 minus ending-0 minus space for backslash.
            Int32 availPathLength = 0x0E - filename.Length - extension.Length;
            if (path.Length > availPathLength)
                path = path.Substring(0, availPathLength);
            String newPath = (path + "\\" + filename + extension).TrimStart('\\');
            return new String(newPath.ToUpperInvariant().Replace(' ', '_').Where(x => x > 0x20 && x < 0x7F).ToArray());
        }

        public override Boolean SaveArchive(Archive archive, Stream saveStream, String savePath)
        {
            // TODO
            return false;
        }
    }
}