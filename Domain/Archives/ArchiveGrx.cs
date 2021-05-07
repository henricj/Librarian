using Nyerguds.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LibrarianTool.Domain.Archives
{
    public class ArchiveGrx : Archive
    {
        public override String ShortTypeName { get { return "Genus Microprogramming Archive"; } }
        public override String ShortTypeDescription { get { return "GRX Archive"; } }
        public override String[] FileExtensions { get { return new String[] { "grx" }; } }
        public override Boolean CanSave { get { return false; } }

        const String GRX_BANNER = "Copyright (c) Genus Microprogramming, Inc. 1988-93";
        const String GRX_BANNER_REGEX = "Copyright \\(c\\) Genus Microprogramming, Inc. \\d\\d\\d\\d-\\d\\d";

        protected override List<ArchiveEntry> LoadArchiveInternal(Stream loadStream, string archivePath)
        {
            UInt32 end = (UInt32)loadStream.Length;
            Encoding enc = new ASCIIEncoding();
            if (end < 0x81)
                throw new FileTypeLoadException("Archive not long enough for header.");
            Byte[] buffer = new Byte[GRX_BANNER.Length];
            loadStream.Position = 2;
            loadStream.Read(buffer, 0, GRX_BANNER.Length);
            String header = enc.GetString(buffer);
            if (!Regex.IsMatch(header, GRX_BANNER_REGEX))
                throw new FileTypeLoadException("Header does not match.");
            // start on first file
            Int32 curPos = 0x80;
            const Int32 bufLen = 0x1A;
            List<ArchiveEntry> filesList = new List<ArchiveEntry>();
            Regex splitFilename = new Regex("(\\w+) *(\\.\\w+)");
            Int32 firstFileStart = -1;
            do
            {
                buffer = new Byte[bufLen];
                loadStream.Position = curPos;
                if (curPos + bufLen >= end)
                    throw new FileTypeLoadException("Archive not long enough for file header.");
                loadStream.Read(buffer, 0, bufLen);
                Byte[] nameBuffer = new Byte[12];
                Array.Copy(buffer, 1, nameBuffer, 0, 12);
                String curName = enc.GetString(nameBuffer.TakeWhile(x => x != 0).ToArray());
                Match m = splitFilename.Match(curName);
                if (!m.Success)
                    break;
                curName = m.Groups[1].Value + m.Groups[2].Value;
                Int32 address = (Int32) ArrayUtils.ReadIntFromByteArray(buffer, 0x0E, 4, true);
                Int32 length = (Int32) ArrayUtils.ReadIntFromByteArray(buffer, 0x12, 4, true);
                UInt16 dosDate = (UInt16)ArrayUtils.ReadIntFromByteArray(buffer, 0x16, 2, true);
                UInt16 dosTime = (UInt16)ArrayUtils.ReadIntFromByteArray(buffer, 0x18, 2, true);
                DateTime dt;
                try
                {
                    dt = GeneralUtils.GetDosDateTime(dosTime, dosDate);
                }
                catch (ArgumentException argex)
                {
                    throw new FileTypeLoadException(argex.Message, argex);
                }
                String extraInfo = GeneralUtils.GetDateString(dt);
                ArchiveEntry curEntry = new ArchiveEntry(curName, archivePath, address, length, extraInfo);
                curEntry.ExtraInfoBin = buffer;
                curEntry.Date = dt;
                filesList.Add(curEntry);
                curPos += bufLen;
                firstFileStart = Math.Max(firstFileStart, address);
            } while (curPos < firstFileStart);
            return filesList;
        }

        /// <summary>Inserts a file into the archive. This can be overridden to add filtering on the input.</summary>
        /// <param name="filePath">Path of the file to load.</param>
        public override ArchiveEntry InsertFile(String filePath)
        {
            ArchiveEntry file = base.InsertFile(filePath);
            DateTime lastMod = file.Date ?? File.GetLastWriteTime(filePath);
            file.ExtraInfo = GeneralUtils.GetDateString(lastMod);
            return file;
        }

        public override bool SaveArchive(Archive archive, Stream saveStream, string savePath)
        {
            throw new NotImplementedException();
        }
    }
}
