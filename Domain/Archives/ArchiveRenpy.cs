using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Nyerguds.Util;

namespace LibrarianTool.Domain.Archives
{
    public class ArchiveRenpy : Archive
    {
        // "LIB" + 0x1A
        private static readonly Byte[] IdBytesLib = Encoding.ASCII.GetBytes("RPA-3.0 ");
        private static readonly Byte[] SeparatorBytesLib = Encoding.ASCII.GetBytes("Made with Ren'Py.");
        private static readonly Byte[] Rpc2IdBytes = Encoding.ASCII.GetBytes("RENPY RPC2");

        public override String ShortTypeName { get { return "Ren'Py Archive"; } }
        public override String ShortTypeDescription { get { return "Ren'Py Archive"; } }
        public override String[] FileExtensions { get { return new String[] { "rpa" }; } }
        public override Boolean CanSave { get { return false; } }

        protected override List<ArchiveEntry> LoadArchiveInternal(Stream loadStream, String archivePath)
        {
            loadStream.Position = 0;
            if (loadStream.Length < IdBytesLib.Length)
                throw new FileTypeLoadException("Too short to be a Ren'Py archive.");
            Byte[] testArray = new Byte[IdBytesLib.Length];
            loadStream.Read(testArray, 0, testArray.Length);
            if (!testArray.SequenceEqual(IdBytesLib))
                throw new FileTypeLoadException("Not a Ren'Py archive.");
            Int32 sepLen = SeparatorBytesLib.Length;
            String baseName = Path.GetFileNameWithoutExtension(archivePath);
            Boolean separatorFound = loadStream.JumpToNextMatch(SeparatorBytesLib, SeparatorBytesLib.Length);
            if (!separatorFound)
                throw new FileTypeLoadException("Not a Ren'Py archive.");
            Int64 startIndex = loadStream.Position + sepLen;
            Int32 fileNamecounter = 0;
            List<ArchiveEntry> filesList = new List<ArchiveEntry>();
            do
            {
                loadStream.Position = startIndex; // skip the separator
                Boolean isRpc2 = loadStream.MatchAtCurrentPos(Rpc2IdBytes);
                String filename = baseName + fileNamecounter.ToString("0000");
                ++fileNamecounter;
                String extension = isRpc2 ? "rpyc" : MimeTypeDetector.GetMimeType(loadStream)[0];
                separatorFound = loadStream.JumpToNextMatch(SeparatorBytesLib, 0x80);
                Int64 endIndex = separatorFound ? loadStream.Position : loadStream.Length;
                Int32 entryLength = (Int32)(endIndex - startIndex);

                ArchiveEntry archiveEntry = new ArchiveEntry(filename + "." + extension, archivePath, (Int32)startIndex, entryLength);
                filesList.Add(archiveEntry);
                startIndex = endIndex + sepLen;
            }
            while (separatorFound);
            return filesList;
        }

        public override String GetInternalFilename(String filePath)
        {
            return Path.GetFileName(filePath);
        }
        
        public override Boolean SaveArchive(Archive archive, Stream saveStream, String savePath)
        {
            throw new NotImplementedException();
        }
    }
}