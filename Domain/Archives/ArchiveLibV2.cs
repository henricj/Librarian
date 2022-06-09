using Nyerguds.Util;
using System.Collections.Generic;
using System.IO;

namespace LibrarianTool.Domain.Archives
{
    public class ArchiveLibV2 : ArchiveLibV1
    {
        // "LIC" + 0x1A
        static readonly byte[] IdBytesLic = { 0x4C, 0x49, 0x43, 0x1A };

        public override string ShortTypeName => "Mythos LIB Archive v2";
        public override string ShortTypeDescription => "Mythos LIB v2";

        protected override List<ArchiveEntry> LoadArchiveInternal(Stream loadStream, string archivePath)
        {
            var files = this.GetFilesCount(loadStream, IdBytesLic);
            var skip = 8 * (files + 1);
            if (loadStream.Position + skip >= loadStream.Length)
                throw new FileTypeLoadException("File too short for full header.");
            // Load of junk. No idea what it is.
            loadStream.Position += skip;
            return LoadLibArchive(loadStream, files, archivePath);
        }

        public override bool SaveArchive(Archive archive, Stream saveStream, string savePath)
        {
            SaveHeader(archive, saveStream, IdBytesLic);
            saveStream.Position += 8 * (archive.FilesList.Count + 1);
            return this.SaveLibArchive(archive, saveStream);
        }
    }
}