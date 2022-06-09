using Nyerguds.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace LibrarianTool.Domain.Archives
{
    public class ArchiveRenpy : Archive
    {
        // "LIB" + 0x1A
        static readonly byte[] IdBytesLib = Encoding.ASCII.GetBytes("RPA-3.0 ");
        static readonly byte[] SeparatorBytesLib = Encoding.ASCII.GetBytes("Made with Ren'Py.");
        static readonly byte[] Rpc2IdBytes = Encoding.ASCII.GetBytes("RENPY RPC2");

        public override string ShortTypeName => "Ren'Py Archive";
        public override string ShortTypeDescription => "Ren'Py Archive";
        public override string[] FileExtensions { get { return new[] { "rpa" }; } }
        public override bool CanSave => false;

        protected override List<ArchiveEntry> LoadArchiveInternal(Stream loadStream, string archivePath)
        {
            loadStream.Position = 0;
            if (loadStream.Length < IdBytesLib.Length)
                throw new FileTypeLoadException("Too short to be a Ren'Py archive.");
            var testArray = new byte[IdBytesLib.Length];
            loadStream.Read(testArray, 0, testArray.Length);
            if (!testArray.SequenceEqual(IdBytesLib))
                throw new FileTypeLoadException("Not a Ren'Py archive.");
            var sepLen = SeparatorBytesLib.Length;
            var baseName = Path.GetFileNameWithoutExtension(archivePath);
            var separatorFound = loadStream.JumpToNextMatch(SeparatorBytesLib, SeparatorBytesLib.Length);
            if (!separatorFound)
                throw new FileTypeLoadException("Not a Ren'Py archive.");
            var startIndex = loadStream.Position + sepLen;
            var fileNamecounter = 0;
            var filesList = new List<ArchiveEntry>();
            do
            {
                loadStream.Position = startIndex; // skip the separator
                var isRpc2 = loadStream.MatchAtCurrentPos(Rpc2IdBytes);
                var filename = baseName + fileNamecounter.ToString("0000", CultureInfo.InvariantCulture);
                ++fileNamecounter;
                var extension = isRpc2 ? "rpyc" : MimeTypeDetector.GetMimeType(loadStream)[0];
                separatorFound = loadStream.JumpToNextMatch(SeparatorBytesLib, 0x80);
                var endIndex = separatorFound ? loadStream.Position : loadStream.Length;
                var entryLength = (int)(endIndex - startIndex);

                var archiveEntry = new ArchiveEntry(filename + "." + extension, archivePath, (int)startIndex, entryLength);
                filesList.Add(archiveEntry);
                startIndex = endIndex + sepLen;
            }
            while (separatorFound);
            return filesList;
        }

        public override string GetInternalFilename(string filePath)
        {
            return Path.GetFileName(filePath);
        }

        public override bool SaveArchive(Archive archive, Stream saveStream, string savePath)
        {
            throw new NotImplementedException();
        }
    }
}