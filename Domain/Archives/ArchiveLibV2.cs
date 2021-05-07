using System;
using System.IO;
using System.Collections.Generic;
using Nyerguds.Util;

namespace LibrarianTool.Domain.Archives
{
    public class ArchiveLibV2 : ArchiveLibV1
    {
        // "LIC" + 0x1A
        private static readonly Byte[] IdBytesLic = { 0x4C, 0x49, 0x43, 0x1A };

        public override String ShortTypeName { get { return "Mythos LIB Archive v2"; } }
        public override String ShortTypeDescription { get { return "Mythos LIB v2"; } }

        protected override List<ArchiveEntry> LoadArchiveInternal(Stream loadStream, String archivePath)
        {
            Int32 files = this.GetFilesCount(loadStream, IdBytesLic);
            Int32 skip = 8 * (files + 1);
            if (loadStream.Position + skip >= loadStream.Length)
                throw new FileTypeLoadException("File too short for full header.");
            // Load of junk. No idea what it is.
            loadStream.Position += skip;
            return this.LoadLibArchive(loadStream, files, archivePath);
        }

        public override Boolean SaveArchive(Archive archive, Stream saveStream, String savePath)
        {
            this.SaveHeader(archive, saveStream, IdBytesLic);
            saveStream.Position += 8 * (archive.FilesList.Count + 1);
            return this.SaveLibArchive(archive, saveStream);
        }
    }
}