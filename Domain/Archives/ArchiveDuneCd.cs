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
        protected const int FileEntryLength = 0x19;

        public override string ShortTypeName => "Dune CD Archive";
        public override string ShortTypeDescription => "Dune CD Archive";
        public override string[] FileExtensions { get { return new[] { "dat" }; } }
        public override bool CanSave => false;

        protected override List<ArchiveEntry> LoadArchiveInternal(Stream loadStream, string archivePath)
        {
            var enc = Encoding.GetEncoding(437);
            var end = loadStream.Length;
            Span<byte> buffer = stackalloc byte[FileEntryLength];
            var filesList = new List<ArchiveEntry>();
            if (end - loadStream.Position < 0x02)
                throw new FileTypeLoadException("Not a Dune CD Archive.");
            if (2 != loadStream.Read(buffer[..2]))
                throw new FileTypeLoadException("Not a Dune CD Archive.");
            var length = (int)ArrayUtils.ReadIntFromByteArray(buffer[..2], true);
            if (end - loadStream.Position < (length * FileEntryLength))
                throw new FileTypeLoadException("Not a Dune CD Archive.");
            for (var i = 0; i < length; i++)
            {
                if (FileEntryLength != loadStream.Read(buffer[..FileEntryLength]))
                    throw new FileTypeLoadException("Not a Dune CD Archive.");
                ReadOnlySpan<byte> curNameB = buffer[0x10..];
                var index = curNameB.IndexOf((byte)0);
                if (index >= 0)
                    curNameB = curNameB[..index];
                if (curNameB.Length == 0)
                    break;
                foreach (var c in curNameB)
                {
                    if (c is < 0x20 or >= 0x7F)
                        throw new FileTypeLoadException("Filename contains nonstandard characters.");
                }
                var curName = enc.GetString(curNameB).Trim();
                var curEntryLength = (int)ArrayUtils.ReadIntFromByteArray(buffer.Slice(0x10, 4), true);
                var curEntryPos = (int)ArrayUtils.ReadIntFromByteArray(buffer.Slice(0x14, 4), true);
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
        public override string GetInternalFilename(string filePath)
        {
            // TODO: Something still wrong in this code. Not sure what. Investigate later.
            var path = Path.GetDirectoryName(filePath) ?? string.Empty;
            path = path.Trim('\\');
            var filename = Path.GetFileNameWithoutExtension(filePath) ?? string.Empty;
            var extension = Path.GetExtension(filePath) ?? string.Empty;
            if (filename.Length > 8)
                filename = filename[..8];
            if (extension.Length > 4)
                extension = extension[..4];
            // 0x0E: 0x10 minus ending-0 minus space for backslash.
            var availPathLength = 0x0E - filename.Length - extension.Length;
            if (path.Length > availPathLength)
                path = path[..availPathLength];
            var newPath = (path + "\\" + filename + extension).TrimStart('\\');
            return new string(newPath.ToUpperInvariant().Replace(' ', '_').Where(x => x > 0x20 && x < 0x7F).ToArray());
        }

        public override bool SaveArchive(Archive archive, Stream saveStream, string savePath)
        {
            // TODO
            return false;
        }
    }
}