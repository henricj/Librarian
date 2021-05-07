using Nyerguds.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LibrarianTool.Domain.Archives
{
	public class ArchiveSndKort : Archive
	{
        public override String ShortTypeName { get { return "KORT SND Archive"; } }
        public override String ShortTypeDescription { get { return "KORT SND"; } }
        public override String[] FileExtensions { get { return new String[] { "SND" }; } }
        
        protected const String BufferInfoFormat = "Buffer info: 0x{0:X8}";
        
		protected override List<ArchiveEntry> LoadArchiveInternal(Stream loadStream, String archivePath)
		{
			loadStream.Position = 0;
			this.ExtraInfo = String.Empty;
			Byte[] filesCount = new Byte[2];
			Int32 amount = loadStream.Read(filesCount, 0, 2);
			Int32 nrOfFiles = (Int32)ArrayUtils.ReadIntFromByteArray(filesCount, 0, 2, true);
		    if (amount != 2)
		        throw new FileTypeLoadException("Too short to be a " + this.ShortTypeDescription + " archive.");
		    Byte[] buffer = new Byte[25];
			Int64 firstPos = 2 + nrOfFiles * 25;
            List<ArchiveEntry> filesList = new List<ArchiveEntry>();
			while (loadStream.Position < firstPos)
			{
				amount = loadStream.Read(buffer, 0, 25);
			    if (amount < 25)
			        throw new FileTypeLoadException("Header too small! Not a " + this.ShortTypeDescription + " archive.");
			    UInt32 size = (UInt32)ArrayUtils.ReadIntFromByteArray(buffer, 0, 4, true);
				UInt32 buff = (UInt32)ArrayUtils.ReadIntFromByteArray(buffer, 4, 4, true);
                Byte[] buffBytes = new Byte[4];
                Array.Copy(buffer, 4, buffBytes, 0, 4);
				UInt32 offset = (UInt32)ArrayUtils.ReadIntFromByteArray(buffer, 8, 4, true);
			    if (offset + size > loadStream.Length)
			        throw new FileTypeLoadException("Header refers to data outside the file! Not a " + this.ShortTypeDescription + " archive.");
			    if (offset < firstPos)
			        throw new FileTypeLoadException("Header refers to data inside header! Not a " + this.ShortTypeDescription + " archive.");
                String filename = new String(buffer.Skip(12).TakeWhile(b => b != 0).Select(c => (Char)(c <= 0x20 || c > 0x7F ? 0 : c)).ToArray());
			    if (filename.Contains('\0'))
			        throw new FileTypeLoadException("Non-ASCII filename characters found in header! Not a " + this.ShortTypeDescription + " archive.");
			    ArchiveEntry archiveEntry = new ArchiveEntry(filename, archivePath, (Int32)offset, (Int32)size);
				StringBuilder sbExtraInfo = new StringBuilder();
                sbExtraInfo.Append(String.Format(BufferInfoFormat, buff));
				this.IdentifyType(loadStream, offset, size, sbExtraInfo);
				archiveEntry.ExtraInfo = sbExtraInfo.ToString();
                archiveEntry.ExtraInfoBin = buffBytes;
				filesList.Add(archiveEntry);
			}
			this.ExtraInfo = "WARNING - The unknown 'Buffer' value will only be preserved when REPLACING files.";
		    return filesList;
		}

		protected void IdentifyType(Stream loadStream, UInt32 offset, UInt32 size, StringBuilder sbExtraInfo)
		{
			Boolean isVoc = false;
			Int64 savedPos = loadStream.Position;
			if (size > 19u)
			{
				Byte[] buff = new Byte[19];
				loadStream.Position = offset;
				loadStream.Read(buff, 0, buff.Length);
				if (Encoding.ASCII.GetString(buff).Equals("Creative Voice File"))
				{
					sbExtraInfo.Append("\nType: Creative Voice File");
					isVoc = true;
				}
			}
			if (!isVoc && size > 8u)
			{
				Byte[] buff = new Byte[8];
				loadStream.Position = offset;
				loadStream.Read(buff, 0, buff.Length);
				if (Encoding.ASCII.GetString(buff, 0, 4).Equals("CTMF"))
				{
                    sbExtraInfo.Append("\nType: Creative Music Format");
				}
			}
			loadStream.Position = savedPos;
		}

        protected override ArchiveEntry InsertFileInternal(String filePath, String internalFilename, Int32 foundIndex)
		{
            ArchiveEntry entry;
            Byte[] extraInfoBin;
            StringBuilder sb = new StringBuilder();
            if (foundIndex == -1)
            {
                entry = new ArchiveEntry(filePath, internalFilename);
                extraInfoBin = new Byte[4];
            }
            else
            {
                entry = new ArchiveEntry(filePath, internalFilename);
                extraInfoBin = this._filesList[foundIndex].ExtraInfoBin;
            }
            if (extraInfoBin != null && extraInfoBin.Length >= 4)
            {
                UInt32 buff = (UInt32) ArrayUtils.ReadIntFromByteArray(extraInfoBin, 0, 4, true);
                sb.Append(String.Format(BufferInfoFormat, buff));
            }
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
		    {
		        this.IdentifyType(fs, 0u, (UInt32) fs.Length, sb);
            }
            entry.ExtraInfo = sb.ToString();
            entry.ExtraInfoBin = extraInfoBin;
            if (foundIndex == -1)
                this._filesList.Add(entry);
            else
		        this._filesList[foundIndex] = entry;
            return entry;
		}

	    protected override void OrderFilesListInternal(List<ArchiveEntry> filesList)
		{
			List<ArchiveEntry> orderedList = filesList.OrderBy(x => x.FileName, new ExtensionSorter()).ToList();
			filesList.Clear();
			filesList.AddRange(orderedList);
		}

		public override Boolean SaveArchive(Archive archive, Stream saveStream, String savePath)
		{
			List<ArchiveEntry> filesList = archive.FilesList.ToList();
			this.OrderFilesListInternal(filesList);
			Int32 nrOfFiles = filesList.Count;
			Int32 firstFileOffset = 2 + 25 * nrOfFiles;
			Int32 fileOffset = firstFileOffset;
			Encoding enc = Encoding.GetEncoding(437);
			Byte[] nameBuffer = new Byte[13];
			using (BinaryWriter bw = new BinaryWriter(new NonDisposingStream(saveStream)))
			{
				bw.Write((UInt16)nrOfFiles);
				foreach (ArchiveEntry entry in filesList)
				{
					Int32 fileLength = entry.Length;
					String curName = entry.FileName;
					if (entry.PhysicalPath != null)
					{
						FileInfo fi = new FileInfo(entry.PhysicalPath);
						if (!fi.Exists)
							throw new FileNotFoundException("Cannot find file \"" + entry.PhysicalPath + "\" to write to archive!");
						fileLength = (Int32)fi.Length;
						curName = this.GetInternalFilename(entry.FileName);
					}
					bw.Write(fileLength);
					UInt32 buff = 0u;
				    Byte[] extraInfoBin = entry.ExtraInfoBin;
                    if (extraInfoBin != null && extraInfoBin.Length >= 4)
                        buff = (UInt32)ArrayUtils.ReadIntFromByteArray(extraInfoBin, 0, 4, true);
					bw.Write(buff);
					bw.Write(fileOffset);
					fileOffset += fileLength;
					Int32 copySize = curName.Length;
					Array.Copy(enc.GetBytes(curName), 0, nameBuffer, 0, copySize);
					for (Int32 b = copySize; b < 13; b++)
					{
						nameBuffer[b] = 0;
					}
					bw.Write(nameBuffer, 0, nameBuffer.Length);
				}
			}
		    if (firstFileOffset != saveStream.Position)
		        throw new IndexOutOfRangeException("Programmer error: write start offset does not match end of index.");
		    foreach (ArchiveEntry entry in filesList)
				CopyEntryContentsToStream(entry, saveStream);
			return true;
		}
	}
}
