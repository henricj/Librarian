using System;
using System.IO;

namespace LibrarianTool.Domain
{
    public class ArchiveEntry : IEquatable<ArchiveEntry>
    {
        public String PhysicalPath { get; set; }
        public String FileName { get; set; }
        public String HashedFilename { get; set; }
        public HashType HashType { get; set; }
        public String ArchivePath { get; set; }
        public Int32 StartOffset { get; set; }
        public Int32 Length { get; set; }
        public String ExtraInfo { get; set; }
        public Byte[] ExtraInfoBin { get; set; }
        public Boolean IsFolder { get; set; }
        public DateTime? Date { get; set; }

        public ArchiveEntry ()
        {
            this.StartOffset = -1;
            this.Length = -1;
        }

        public ArchiveEntry(String physicalPath)
        {
            this.PhysicalPath = physicalPath;
            this.FileName = Path.GetFileName(physicalPath);
            this.StartOffset = -1;
            this.Length = -1;
        }

        public ArchiveEntry(String physicalPath, String storedFilename)
        {
            this.PhysicalPath = physicalPath;
            this.FileName = storedFilename;
            this.StartOffset = -1;
            this.Length = -1;
        }

        public ArchiveEntry(String physicalPath, String storedFilename, String extraInfo)
        {
            this.PhysicalPath = physicalPath;
            this.FileName = storedFilename;
            this.StartOffset = -1;
            this.Length = -1;
            this.ExtraInfo = extraInfo;
        }

        /// <summary>
        /// For loading from an archive.
        /// </summary>
        /// <param name="fileName">filename in the archive</param>
        /// <param name="archivePath">Path of the archive</param>
        /// <param name="startOffset">Start offset</param>
        /// <param name="length">Length</param>
        public ArchiveEntry(String fileName, String archivePath, Int32 startOffset, Int32 length)
        {
            this.FileName = fileName;
            this.ArchivePath = archivePath;
            this.StartOffset = startOffset;
            this.Length = length;
        }

        /// <summary>
        /// For loading from an archive.
        /// </summary>
        /// <param name="fileName">filename in the archive</param>
        /// <param name="archivePath">Path of the archive</param>
        /// <param name="startOffset">Start offset</param>
        /// <param name="length">Length</param>
        public ArchiveEntry(String fileName, String archivePath, Int32 startOffset, Int32 length, String extraInfo)
        {
            this.FileName = fileName;
            this.ArchivePath = archivePath;
            this.StartOffset = startOffset;
            this.Length = length;
            this.ExtraInfo = extraInfo;
        }

        /// <summary>
        /// For loading from an archive with hashed names.
        /// </summary>
        /// <param name="hashedFilename">Hashed filename.</param>
        /// <param name="hashType">Hash type.</param>
        /// <param name="archivePath">Path of the archive</param>
        /// <param name="startOffset">Start offset</param>
        /// <param name="length">Length</param>
        public ArchiveEntry(String hashedFilename, HashType hashType, String archivePath, Int32 startOffset, Int32 length)
        {
            this.HashedFilename = hashedFilename;
            this.HashType = hashType;
            this.ArchivePath = archivePath;
            this.StartOffset = startOffset;
            this.Length = length;
        }

        /// <summary>
        /// For loading from an archive with hashed names, if the name could be recovered.
        /// </summary>
        /// <param name="hashedFilename">Hashed filename.</param>
        /// <param name="hashType">Hash type.</param>
        /// <param name="fileName">filename in the archive (if available)</param>
        /// <param name="archivePath">Path of the archive</param>
        /// <param name="startOffset">Start offset</param>
        /// <param name="length">Length</param>
        public ArchiveEntry(String fileName, String hashedFilename, HashType hashType, String archivePath, Int32 startOffset, Int32 length)
        {
            this.FileName = fileName;
            this.HashedFilename = hashedFilename;
            this.HashType = hashType;
            this.ArchivePath = archivePath;
            this.StartOffset = startOffset;
            this.Length = length;
        }

        public override String ToString()
        {
            return (this.PhysicalPath != null ? "[" : String.Empty) + (this.FileName ?? this.HashedFilename ?? Path.GetFileName(this.PhysicalPath)) + (this.PhysicalPath != null ? "]" : String.Empty);
        }

        public Boolean Equals(ArchiveEntry other)
        {
            if (other == null)
                return false;
            // Case issues in internal filenames should be normalised by the archive insert override.
            return this.FileName == other.FileName &&
                   this.HashedFilename == other.HashedFilename &&
                   this.HashType == other.HashType &&
                   String.Equals(this.ArchivePath, other.ArchivePath, StringComparison.InvariantCultureIgnoreCase) &&
                   this.StartOffset == other.StartOffset &&
                   this.Length == other.Length;
        }
    }
}