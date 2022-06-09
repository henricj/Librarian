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
            StartOffset = -1;
            Length = -1;
        }

        public ArchiveEntry(string physicalPath)
        {
            PhysicalPath = physicalPath;
            FileName = Path.GetFileName(physicalPath);
            StartOffset = -1;
            Length = -1;
        }

        public ArchiveEntry(string physicalPath, string storedFilename)
        {
            PhysicalPath = physicalPath;
            FileName = storedFilename;
            StartOffset = -1;
            Length = -1;
        }

        public ArchiveEntry(string physicalPath, string storedFilename, string extraInfo)
        {
            PhysicalPath = physicalPath;
            FileName = storedFilename;
            StartOffset = -1;
            Length = -1;
            ExtraInfo = extraInfo;
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
            FileName = fileName;
            ArchivePath = archivePath;
            StartOffset = startOffset;
            Length = length;
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
            FileName = fileName;
            ArchivePath = archivePath;
            StartOffset = startOffset;
            Length = length;
            ExtraInfo = extraInfo;
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
            HashedFilename = hashedFilename;
            HashType = hashType;
            ArchivePath = archivePath;
            StartOffset = startOffset;
            Length = length;
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
            FileName = fileName;
            HashedFilename = hashedFilename;
            HashType = hashType;
            ArchivePath = archivePath;
            StartOffset = startOffset;
            Length = length;
        }

        public override string ToString()
        {
            return (PhysicalPath != null ? "[" : string.Empty) + (FileName ?? HashedFilename ?? Path.GetFileName(PhysicalPath)) + (PhysicalPath != null ? "]" : string.Empty);
        }

        public bool Equals(ArchiveEntry other)
        {
            if (other == null)
                return false;
            // Case issues in internal filenames should be normalised by the archive insert override.
            return FileName == other.FileName &&
                   HashedFilename == other.HashedFilename &&
                   HashType == other.HashType &&
                   string.Equals(ArchivePath, other.ArchivePath, StringComparison.OrdinalIgnoreCase) &&
                   StartOffset == other.StartOffset &&
                   Length == other.Length;
        }

        public override bool Equals(object obj) => obj is ArchiveEntry other && Equals(other);

        public override int GetHashCode() => System.HashCode.Combine(FileName, HashedFilename, HashType, ArchivePath, StartOffset, Length);
    }
}