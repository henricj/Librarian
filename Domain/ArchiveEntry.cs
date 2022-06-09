using System;
using System.IO;

namespace LibrarianTool.Domain
{
    public class ArchiveEntry : IEquatable<ArchiveEntry>
    {
        public string PhysicalPath { get; set; }
        public string FileName { get; set; }
        public string HashedFilename { get; set; }
        public HashType HashType { get; set; }
        public string ArchivePath { get; set; }
        public int StartOffset { get; set; }
        public int Length { get; set; }
        public string ExtraInfo { get; set; }
        public byte[] ExtraInfoBin { get; set; }
        public bool IsFolder { get; set; }
        public DateTime? Date { get; set; }

        public ArchiveEntry()
        {
            this.StartOffset = -1;
            this.Length = -1;
        }

        public ArchiveEntry(string physicalPath)
        {
            this.PhysicalPath = physicalPath;
            this.FileName = Path.GetFileName(physicalPath);
            this.StartOffset = -1;
            this.Length = -1;
        }

        public ArchiveEntry(string physicalPath, string storedFilename)
        {
            this.PhysicalPath = physicalPath;
            this.FileName = storedFilename;
            this.StartOffset = -1;
            this.Length = -1;
        }

        public ArchiveEntry(string physicalPath, string storedFilename, string extraInfo)
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
        public ArchiveEntry(string fileName, string archivePath, int startOffset, int length)
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
        public ArchiveEntry(string fileName, string archivePath, int startOffset, int length, string extraInfo)
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
        public ArchiveEntry(string hashedFilename, HashType hashType, string archivePath, int startOffset, int length)
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
        public ArchiveEntry(string fileName, string hashedFilename, HashType hashType, string archivePath, int startOffset, int length)
        {
            this.FileName = fileName;
            this.HashedFilename = hashedFilename;
            this.HashType = hashType;
            this.ArchivePath = archivePath;
            this.StartOffset = startOffset;
            this.Length = length;
        }

        public override string ToString()
        {
            return (this.PhysicalPath != null ? "[" : string.Empty) + (this.FileName ?? this.HashedFilename ?? Path.GetFileName(this.PhysicalPath)) + (this.PhysicalPath != null ? "]" : string.Empty);
        }

        public bool Equals(ArchiveEntry other)
        {
            if (other == null)
                return false;
            // Case issues in internal filenames should be normalised by the archive insert override.
            return this.FileName == other.FileName &&
                   this.HashedFilename == other.HashedFilename &&
                   this.HashType == other.HashType &&
                   string.Equals(this.ArchivePath, other.ArchivePath, StringComparison.InvariantCultureIgnoreCase) &&
                   this.StartOffset == other.StartOffset &&
                   this.Length == other.Length;
        }
    }
}