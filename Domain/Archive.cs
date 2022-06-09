using LibrarianTool.Domain.Archives;
using Nyerguds.Util;
using Nyerguds.Util.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LibrarianTool.Domain
{
    public abstract class Archive : IFileTypeBroadcaster
    {
        public abstract string ShortTypeName { get; }
        public abstract string ShortTypeDescription { get; }
        public abstract string[] FileExtensions { get; }
        public virtual string[] DescriptionsForExtensions => Enumerable.Repeat(this.ShortTypeDescription, this.FileExtensions.Length).ToArray();
        public virtual string FileExtension { get; set; }
        /// <summary>Supported types can always be loaded, but this indicates if save functionality to this type is also available.</summary>
        public virtual bool CanSave => true;

        public virtual bool SupportsFolders => false;

        protected List<ArchiveEntry> _filesList = new();
        public List<ArchiveEntry> FilesList => this._filesList;
        public string FileName { get; protected set; }
        public virtual string ExtraInfo { get; protected set; }

        /// <summary>Reads the file, and fills in the _filesList list;</summary>
        /// <param name="loadPath">Path to load the file from.</param>
        /// <returns>True if loading succeeded.</returns>
        public void LoadArchive(string loadPath)
        {
            this.FileName = loadPath;
            List<ArchiveEntry> filesList;
            using (var fs = new FileStream(loadPath, FileMode.Open, FileAccess.Read))
                filesList = this.LoadArchiveInternal(fs, loadPath);
            filesList ??= new List<ArchiveEntry>();
            this._filesList = filesList;
        }

        /// <summary>Reads the stream, and fills in the _filesList list;</summary>
        /// <param name="loadStream">Stream to load the file from.</param>
        /// <param name="archivePath">Path of the loaded archive.</param>
        /// <returns>True if loading succeeded.</returns>
        public void LoadArchive(Stream loadStream, string archivePath)
        {
            this.FileName = archivePath;
            var filesList = this.LoadArchiveInternal(loadStream, archivePath) ?? new List<ArchiveEntry>();
            this._filesList = filesList;
        }

        /// <summary>Reads the bytes, and fills in the _filesList list;</summary>
        /// <param name="loadData">Data to load the file from.</param>
        /// <param name="archivePath">Path of the loaded archive.</param>
        /// <returns>True if loading succeeded.</returns>
        public void LoadArchive(byte[] loadData, string archivePath)
        {
            this.FileName = archivePath;
            List<ArchiveEntry> filesList;
            using (var ms = new MemoryStream(loadData))
                filesList = this.LoadArchiveInternal(ms, archivePath);
            filesList ??= new List<ArchiveEntry>();
            this._filesList = filesList;
        }

        /// <summary>Reads the stream, and fills in the _filesList list;</summary>
        /// <param name="loadStream">Stream to load the file from.</param>
        /// <param name="archivePath">Path of the loaded archive.</param>
        /// <returns>True if loading succeeded.</returns>
        protected abstract List<ArchiveEntry> LoadArchiveInternal(Stream loadStream, string archivePath);

        /// <summary>Saves the given archive as this specific type</summary>
        /// <param name="archive">Archive to save as this type.</param>
        /// <param name="saveStream">Stream to save the archive to.</param>
        /// <param name="savePath">Path that the file will be saved to. Sometimes needed for saving accompanying files.</param>
        /// <returns></returns>
        public abstract bool SaveArchive(Archive archive, Stream saveStream, string savePath);

        /// <summary>Extracts the requested file from the _filesList list.</summary>
        /// <param name="filename">Name of the file to extract.</param>
        /// <param name="savePath">Path to save the file to.</param>
        /// <returns></returns>
        public bool ExtractFile(string filename, string savePath)
        {
            return this.ExtractFile(this.FindFile(filename, out _), savePath);
        }

        /// <summary>Extracts the requested file from the _filesList list.</summary>
        /// <param name="entry">Name of the file to extract.</param>
        /// <param name="savePath">Path to save the file to.</param>
        /// <returns></returns>
        public virtual bool ExtractFile(ArchiveEntry entry, string savePath)
        {
            if (entry == null)
                return false;
            var folder = Path.GetDirectoryName(savePath);
            if (entry.IsFolder)
            {
                Directory.CreateDirectory(savePath);
            }
            else
            {
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);
                using var fs = new FileStream(savePath, FileMode.Create);
                CopyEntryContentsToStream(entry, fs);
            }
            if (entry.Date.HasValue)
            {
                try
                {
                    if (entry.IsFolder)
                        Directory.SetLastWriteTime(savePath, entry.Date.Value);
                    else
                        File.SetLastWriteTime(savePath, entry.Date.Value);
                }
                catch (IOException) { /* Ignore. It's just the time stamp. */ }
            }
            return true;
        }

        /// <summary>
        /// Find file entry in archive. Override this for things like hash lookups.
        /// </summary>
        /// <param name="filePath">path of a file of which to look up whether a file with that name exists in the archive.</param>
        /// <param name="index">Index at which this file was found.</param>
        /// <returns>the found entry</returns>
        public virtual ArchiveEntry FindFile(string filePath, out int index)
        {
            var filename = this.GetInternalFilename(filePath);
            for (var i = 0; i < this._filesList.Count; i++)
            {
                var current = this._filesList[i];
                if (current.FileName.Equals(filename, StringComparison.InvariantCultureIgnoreCase))
                {
                    index = i;
                    return current;
                }
            }
            index = -1;
            return null;
        }

        /// <summary>Inserts a file into the archive. This can be overridden to add filtering on the input.</summary>
        /// <param name="filePath">Path of the file to load.</param>
        public virtual ArchiveEntry InsertFile(string filePath)
        {
            var isFolder = (File.GetAttributes(filePath) & FileAttributes.Directory) != 0;
            var internalFilename = this.GetInternalFilename(Path.GetFileName(filePath));
            this.FindFile(internalFilename, out var foundIndex);
            var retEntry = this.InsertFileInternal(filePath, internalFilename, foundIndex);
            retEntry.IsFolder = isFolder;
            retEntry.Date = File.GetLastWriteTime(filePath);
            this.OrderFilesListInternal(this._filesList);
            return retEntry;
        }

        /// <summary>Inserts a file into the archive. This can be overridden to add filtering on the input.</summary>
        /// <param name="filePath">Path of the file to load.</param>
        /// <param name="internalFilename">Filename as it is stored in the archive.</param>
        public virtual ArchiveEntry InsertFile(string filePath, string internalFilename)
        {
            var isFolder = (File.GetAttributes(filePath) & FileAttributes.Directory) != 0;
            internalFilename = this.GetInternalFilename(internalFilename);
            this.FindFile(internalFilename, out var foundIndex);
            var retEntry = this.InsertFileInternal(filePath, internalFilename, foundIndex);
            retEntry.IsFolder = isFolder;
            this.OrderFilesListInternal(this._filesList);
            return retEntry;
        }

        protected virtual ArchiveEntry InsertFileInternal(string filePath, string internalFilename, int foundIndex)
        {
            ArchiveEntry entry;
            if (foundIndex == -1)
                this._filesList.Add(entry = new ArchiveEntry(filePath, internalFilename));
            else
                this._filesList[foundIndex] = (entry = new ArchiveEntry(filePath, internalFilename, this._filesList[foundIndex].ExtraInfo));
            return entry;
        }

        protected virtual void OrderFilesListInternal(List<ArchiveEntry> filesList)
        {
            var orderedList = this.FilesList.OrderBy(x => x.FileName).ToList();
            filesList.Clear();
            filesList.AddRange(orderedList);
        }

        /// <summary>
        /// Converts the filename to the type supported internally. By default, this strips
        /// out all non-ascii characters, converts to uppercase, and limits the length to 8.3.
        /// Override if needed.
        /// </summary>
        /// <param name="filePath">Original file path.</param>
        /// <returns></returns>
        public virtual string GetInternalFilename(string filePath)
        {
            var fileDir = Path.GetDirectoryName(filePath) ?? string.Empty;
            if (fileDir.Length > 0)
            {
                var fileDirs = fileDir.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
                for (var i = 0; i < fileDirs.Length; i++)
                {
                    fileDirs[i] = GeneralUtils.GetDos83FileName(fileDirs[i]);
                }
                fileDir = string.Join("\\", fileDirs);
            }
            var finalName = GeneralUtils.GetDos83FileName(filePath);
            if (fileDir.Length > 0)
                finalName = fileDir + "\\" + finalName;
            return finalName;
        }

        public virtual void RemoveFiles(string[] filenames)
        {
            if (filenames == null || filenames.Length == 0)
                return;
            var toRemove = new List<ArchiveEntry>();
            foreach (var entry in this._filesList)
            {
                var inList = false;
                foreach (var filename in filenames)
                {
                    if (!entry.FileName.Equals(filename, StringComparison.InvariantCultureIgnoreCase))
                        continue;
                    inList = true;
                    break;
                }
                if (!inList)
                    continue;
                toRemove.Add(entry);
                break;
            }
            foreach (var entry in toRemove)
                this._filesList.Remove(entry);
            this._filesList = this.FilesList.OrderBy(x => x.FileName).ToList();
        }

        public bool SaveArchive(Archive archive, string savePath)
        {
            using var ms = new MemoryStream();
            // Cannot be done straight to the FileStream since unmodified entries may be read from the original file.
            if (!this.SaveArchive(archive, ms, savePath))
                return false;
            ms.Position = 0;
            using var fs = new FileStream(savePath, FileMode.Create);
            CopyStream(ms, fs, ms.Length);
            return true;
        }

        public byte[] SaveArchive(Archive archive)
        {
            using var ms = new MemoryStream();
            if (!this.SaveArchive(archive, ms, null))
                return null;
            return ms.ToArray();
        }

        protected static void CopyEntryContentsToStream(ArchiveEntry entry, Stream saveStream)
        {
            string readFile;
            int start;
            int length;
            if (entry.PhysicalPath != null)
            {
                readFile = entry.PhysicalPath;
                start = 0;
                var fi = new FileInfo(entry.PhysicalPath);
                length = (int)fi.Length;
            }
            else
            {
                readFile = entry.ArchivePath;
                start = entry.StartOffset;
                length = entry.Length;
            }

            using var fs = new FileStream(readFile, FileMode.Open, FileAccess.Read);
            fs.Seek(start, SeekOrigin.Begin);
            CopyStream(fs, saveStream, length);
        }

        public static void CopyStream(Stream input, Stream output, long length)
        {
            var remainder = length;
            var buffer = new byte[Math.Min(length, 0x8000)];
            int read;
            while (remainder > 0 && (read = input.Read(buffer, 0, (int)Math.Min(remainder, buffer.Length))) > 0)
            {
                output.Write(buffer, 0, read);
                remainder -= read;
            }
        }

        /// <summary>
        /// Autodetects the file type from the given list, and if that fails, from the full autodetect list.
        /// </summary>
        /// <param name="fileStream">File dat to load file from.</param>
        /// <param name="path">File path, used for extension filtering and file initialisation. Not for reading as bytes; fileData is used for that.</param>
        /// <param name="preferredTypes">List of the most likely types it can be.</param>
        /// <param name="loadErrors">Returned list of occurred errors during autodetect.</param>
        /// <param name="onlyGivenTypes">True if only the possibleTypes list is processed to autodetect the type.</param>
        /// <returns>The detected type, or null if detection failed.</returns>
        public static Archive LoadArchiveAutodetect(Stream fileStream, string path, Archive[] preferredTypes, bool onlyGivenTypes, out List<FileTypeLoadException> loadErrors)
        {
            loadErrors = new List<FileTypeLoadException>();
            // See which extensions match, and try those first.
            if (preferredTypes == null)
                preferredTypes = FileDialogGenerator.IdentifyByExtension<Archive>(AutoDetectTypes, path);
            else if (onlyGivenTypes)
            {
                // Try extension-filtering first, then the rest.
                var preferredTypesExt = FileDialogGenerator.IdentifyByExtension(preferredTypes, path);
                foreach (var typeObj in preferredTypesExt)
                {
                    try
                    {
                        fileStream.Position = 0;
                        typeObj.LoadArchive(fileStream, path);
                        return typeObj;
                    }
                    catch (FileTypeLoadException e)
                    {
                        e.AttemptedLoadedType = typeObj.ShortTypeName;
                        loadErrors.Add(e);
                    }
                    preferredTypes = preferredTypes.Where(tp => preferredTypesExt.All(tpe => tpe.GetType() != tp.GetType())).ToArray();
                }
            }
            foreach (var typeObj in preferredTypes)
            {
                try
                {
                    fileStream.Position = 0;
                    typeObj.LoadArchive(fileStream, path);
                    return typeObj;
                }
                catch (FileTypeLoadException e)
                {
                    e.AttemptedLoadedType = typeObj.ShortTypeName;
                    loadErrors.Add(e);
                }
            }
            if (onlyGivenTypes)
                return null;
            foreach (var type in AutoDetectTypes)
            {
                // Skip entries on the already-tried list.
                if (preferredTypes.Any(x => x.GetType() == type))
                    continue;
                Archive objInstance = null;
                try { objInstance = (Archive)Activator.CreateInstance(type); }
                catch { /* Ignore; programmer error. */ }
                if (objInstance == null)
                    continue;
                try
                {
                    objInstance.LoadArchive(fileStream, path);
                    return objInstance;
                }
                catch (FileTypeLoadException e)
                {
                    // objInstance should not be disposed here since it never succeeded in initializing,
                    // and should not contain any loaded images at that point.
                    e.AttemptedLoadedType = objInstance.ShortTypeName;
                    loadErrors.Add(e);
                    //objInstance.Dispose();
                }
            }
            return null;
        }

        public static Type[] SupportedSaveTypes
        {
            get
            {
                var saveTypes = new List<Type>();
                foreach (var type in SupportedTypes)
                {
                    Archive objInstance = null;
                    try
                    {
                        objInstance = (Archive)Activator.CreateInstance(type);
                    }
                    catch
                    {
                        /* Ignore; programmer error. */
                    }
                    if (objInstance == null)
                        continue;
                    if (objInstance.CanSave)
                        saveTypes.Add(type);
                }
                return saveTypes.ToArray();
            }
        }

        /// <summary>
        /// List of supported file types, to be used UI listings of file types
        /// that can be opened. Whether the type can be saved can be checked
        /// by creating an object of the type and requesting its "CanSave"
        /// property.
        /// </summary>
        public static readonly Type[] SupportedTypes =
        {
            typeof(ArchiveDynV1),
            typeof(ArchiveDynV2),
            typeof(ArchiveLibV1),
            typeof(ArchiveLibV2),
            typeof(ArchiveM3),
            typeof(ArchiveDuneCd),
            typeof(ArchivePakV1),
            typeof(ArchivePakV2),
            typeof(ArchivePakV3),
            typeof(ArchiveRenpy),
            typeof(ArchiveSndKort),
            typeof(ArchiveSwt),
            typeof(ArchiveGrx),
            typeof(ArchiveCatV1),
            typeof(ArchiveCatV2),
        };

        /// <summary>
        /// List used for the auto-detection of archive types. This is
        /// generally not the same order as the SupportedTypes one, since
        /// auto-detect should be done from most complex to least complex file
        /// type, to avoid false positives and ensure accurate detection.
        /// </summary>
        public static readonly Type[] AutoDetectTypes =
        {
            typeof(ArchiveRenpy),
            typeof(ArchiveLibV1),
            typeof(ArchiveLibV2),
            typeof(ArchiveM3),
            typeof(ArchiveDuneCd),
            typeof(ArchivePakV3),
            typeof(ArchivePakV2),
            typeof(ArchivePakV1),
            typeof(ArchiveDynV1),
            typeof(ArchiveDynV2),
            typeof(ArchiveCatV1),
            typeof(ArchiveCatV2),
            typeof(ArchiveSndKort),
            typeof(ArchiveSwt),
            typeof(ArchiveGrx),
        };

    }
}