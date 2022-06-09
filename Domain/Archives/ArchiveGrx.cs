using Nyerguds.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace LibrarianTool.Domain.Archives
{
    public class ArchiveGrx : Archive
    {
        public override string ShortTypeName => "Genus Microprogramming Archive";
        public override string ShortTypeDescription => "GRX Archive";
        public override string[] FileExtensions { get { return new[] { "grx" }; } }
        public override bool CanSave => false;

        const string GRX_BANNER = "Copyright (c) Genus Microprogramming, Inc. 1988-93";
        const string GRX_BANNER_REGEX = "Copyright \\(c\\) Genus Microprogramming, Inc. \\d\\d\\d\\d-\\d\\d";

        protected override List<ArchiveEntry> LoadArchiveInternal(Stream loadStream, string archivePath)
        {
            var end = (uint)loadStream.Length;
            Encoding enc = new ASCIIEncoding();
            if (end < 0x81)
                throw new FileTypeLoadException("Archive not long enough for header.");
            var buffer = new byte[GRX_BANNER.Length];
            loadStream.Position = 2;
            loadStream.Read(buffer, 0, GRX_BANNER.Length);
            var header = enc.GetString(buffer);
            if (!Regex.IsMatch(header, GRX_BANNER_REGEX))
                throw new FileTypeLoadException("Header does not match.");
            // start on first file
            var curPos = 0x80;
            const int bufLen = 0x1A;
            var filesList = new List<ArchiveEntry>();
            var splitFilename = new Regex("(\\w+) *(\\.\\w+)");
            var firstFileStart = -1;
            do
            {
                buffer = new byte[bufLen];
                loadStream.Position = curPos;
                if (curPos + bufLen >= end)
                    throw new FileTypeLoadException("Archive not long enough for file header.");
                if (buffer.Length != loadStream.Read(buffer))
                    throw new FileTypeLoadException("Archive not long enough for file header.");
                ReadOnlySpan<byte> nameBuffer = buffer.AsSpan(1, 12);
                var index = nameBuffer.IndexOf((byte)0);
                if (index >= 0)
                    nameBuffer = nameBuffer[..index];
                var curName = enc.GetString(nameBuffer);
                var m = splitFilename.Match(curName);
                if (!m.Success)
                    break;
                curName = m.Groups[1].Value + m.Groups[2].Value;
                var address = (int)ArrayUtils.ReadIntFromByteArray(buffer.AsSpan(0x0E, 4), true);
                var length = (int)ArrayUtils.ReadIntFromByteArray(buffer.AsSpan(0x12, 4), true);
                var dosDate = (ushort)ArrayUtils.ReadIntFromByteArray(buffer.AsSpan(0x16, 2), true);
                var dosTime = (ushort)ArrayUtils.ReadIntFromByteArray(buffer.AsSpan(0x18, 2), true);
                DateTime dt;
                try
                {
                    dt = GeneralUtils.GetDosDateTime(dosTime, dosDate);
                }
                catch (ArgumentException argex)
                {
                    throw new FileTypeLoadException(argex.Message, argex);
                }
                var extraInfo = GeneralUtils.GetDateString(dt);
                var curEntry = new ArchiveEntry(curName, archivePath, address, length, extraInfo)
                {
                    ExtraInfoBin = buffer,
                    Date = dt
                };
                filesList.Add(curEntry);
                curPos += bufLen;
                firstFileStart = Math.Max(firstFileStart, address);
            } while (curPos < firstFileStart);
            return filesList;
        }

        /// <summary>Inserts a file into the archive. This can be overridden to add filtering on the input.</summary>
        /// <param name="filePath">Path of the file to load.</param>
        public override ArchiveEntry InsertFile(string filePath)
        {
            var file = base.InsertFile(filePath);
            var lastMod = file.Date ?? File.GetLastWriteTime(filePath);
            file.ExtraInfo = GeneralUtils.GetDateString(lastMod);
            return file;
        }

        public override bool SaveArchive(Archive archive, Stream saveStream, string savePath)
        {
            throw new NotImplementedException();
        }
    }
}
