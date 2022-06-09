using LibrarianTool.Domain;
using Nyerguds.Util;
using Nyerguds.Util.UI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace LibrarianTool
{
    public sealed partial class FrmLibTool : Form
    {
        public delegate void InvokeDelegateReload(Archive newFile, bool asNew, bool resetZoom);
        public delegate DialogResult InvokeDelegateMessageBox(string message, MessageBoxButtons buttons, MessageBoxIcon icon);
        public delegate DialogResult InvokeDelegateMessageBoxDef(string message, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defButtons);
        public delegate void InvokeDelegateTwoArgs(object arg1, object arg2);
        public delegate void InvokeDelegateSingleArg(object value);
        public delegate void InvokeDelegateEnableControls(bool enabled, string processingLabel);

        const string PROG_NAME = "Librarian";
        const string PROG_AUTHOR = "Created by Nyerguds";

        readonly string m_ProgFolder = Path.GetDirectoryName(AppContext.BaseDirectory);
        string m_LastOpenedFolder;
        Archive m_LoadedArchive;
        List<ArchiveEntry> m_FilesListOrigState;
        readonly string argFile;

        public FrmLibTool(string[] args)
            : this()
        {
            if (args.Length > 0 && File.Exists(args[0]))
                argFile = args[0];
        }

        public FrmLibTool()
        {
            InitializeComponent();
            AddNewTypes();
            m_LastOpenedFolder = m_ProgFolder;
            Text = GetTitle(true, true);
        }

        public string GetTitle(bool withAuthor, bool withLoadedArchive)
        {
            var title = GetTitleBuilder(withAuthor);
            if (withLoadedArchive && m_LoadedArchive != null)
            {
                title.Append(" - ");
                if (m_LoadedArchive.FileName == null)
                    title.Append("New archive");
                else
                    title.Append('"').Append(Path.GetFileName(m_LoadedArchive.FileName)).Append('"');
                if (IsArchiveModified())
                    title.Append(" *");
                title.Append(" (").Append(m_LoadedArchive.ShortTypeDescription).Append(')');
            }
            return title.ToString();
        }

        public static string GetTitle(bool withAuthor)
        {
            return GetTitleBuilder(withAuthor).ToString();
        }

        public static StringBuilder GetTitleBuilder(bool withAuthor)
        {
            var title = new StringBuilder(PROG_NAME);
            title.Append(' ').Append(GeneralUtils.ProgramVersion());
            if (withAuthor)
                title.Append(" - ").Append(PROG_AUTHOR);
            return title;
        }

        void AddNewTypes()
        {
            foreach (var type in Archive.SupportedTypes)
            {
                Archive archInstance = null;
                try { archInstance = (Archive)Activator.CreateInstance(type); }
                catch { /* Ignore; programmer error. */ }
                if (archInstance is not { CanSave: true })
                    continue;
                var archtypeMenu = new ToolStripMenuItem
                {
                    Text = archInstance.ShortTypeDescription,
                    Tag = type
                };
                archtypeMenu.Click += NewFileClick;
                tsmiFileNew.DropDownItems.Add(archtypeMenu);
            }
        }

        void NewFileClick(object sender, EventArgs e)
        {
            if (sender is not ToolStripMenuItem { Tag: Type type })
                return;
            Archive archInstance;
            try
            {
                archInstance = (Archive)Activator.CreateInstance(type);
            }
            catch
            {
                return;
            }

            LoadArchive(archInstance, true);
        }


        void FrmLibTool_Shown(object sender, EventArgs e)
        {
            if (argFile != null)
                DetectArchive(argFile, true);
            else
                LoadArchive(null, false);
        }

        void Frm_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        void Frm_DragDrop(object sender, DragEventArgs e)
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length != 1)
                return;
            var path = files[0];
            m_LastOpenedFolder = Path.GetDirectoryName(path);
            var arch = DetectArchive(path, true);
            if (arch != null)
                LoadArchive(arch, true);
        }

        void Lv_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        void Lv_DragDrop(object sender, DragEventArgs e)
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length == 0)
                return;
            if (m_LoadedArchive == null)
            {
                const string message = "No archive has been opened.\n\n" +
                                       "To make a new archive, use the \"New archive\" function in the menu.\n\n" +
                                       "To open an archive, drop it into the area outside the files list.";
                Invoke(new InvokeDelegateMessageBox(ShowMessageBox), message, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
                AddFiles(files);
        }

        void AddFiles(string[] files)
        {
            if (files.Length == 0)
                return;
            if (m_LoadedArchive == null)
                return;
            // Disabled for now; if people add it, it's their responsibility.
            // Won't save until they remove 'em anyway.
            //if (!this.m_LoadedArchive.SupportsFolders)
            //{
            //    foreach (String file in files)
            //        this.m_LoadedArchive.InsertFile(file);
            //}
            //else
            {
                var filesList = new List<string>();
                var filesListRel = new List<string>();
                foreach (var file in files)
                {
                    if ((File.GetAttributes(file) & FileAttributes.Directory) != 0)
                    {
                        var containingFolder = Path.GetDirectoryName(file);
                        AddFilesRecursive(file, containingFolder, filesList, filesListRel);
                    }
                    else
                    {
                        filesList.Add(file);
                        filesListRel.Add(null);
                    }
                }
                for (var i = 0; i < filesList.Count; i++)
                {
                    if (filesListRel[i] == null)
                        m_LoadedArchive.InsertFile(filesList[i]);
                    else
                        m_LoadedArchive.InsertFile(filesList[i], filesListRel[i]);
                }
            }
            LoadArchive(m_LoadedArchive, false);
        }

        void AddFilesRecursive(string file, string basePath, List<string> filesList, List<string> filesListRelative)
        {
            var fullBasePath = Path.GetFullPath(basePath);
            var fullFilePath = Path.GetFullPath(file);
            var basePathLen = fullBasePath.Length + 1;
            filesList.Add(file);
            var filePathRel = fullFilePath[basePathLen..];
            filesListRelative.Add(filePathRel.Contains('\\') ? filePathRel : null);
            if ((File.GetAttributes(file) & FileAttributes.Directory) == 0)
                return;
            var files = Directory.GetFiles(file);
            filesList.AddRange(files);
            for (var i = 0; i < files.Length; i++)
                files[i] = files[i][basePathLen..];
            filesListRelative.AddRange(files);
            var subDirs = Directory.GetDirectories(file);
            foreach (var subDir in subDirs)
                AddFilesRecursive(subDir, basePath, filesList, filesListRelative);
        }

        Archive DetectArchive(string path, bool showErrors)
        {
            return DetectArchive(path, null, showErrors);
        }

        Archive DetectArchive(string path, Archive[] specificOpenTypes, bool showErrors)
        {
            try
            {
                Archive archive;
                List<FileTypeLoadException> loadErrors;
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                    archive = Archive.LoadArchiveAutodetect(fs, path, specificOpenTypes, specificOpenTypes != null, out loadErrors);
                if (archive != null)
                    return archive;
                if (loadErrors is { Count: > 0 } && showErrors)
                {
                    var errors = string.Join("\n", loadErrors.Select(er => er.AttemptedLoadedType + ": " + er.Message).ToArray());
                    var filename = path == null ? string.Empty : (" of \"" + Path.GetFileName(path) + "\"");
                    var message = "File type of " + filename + " could not be identified. Errors returned by all attempts:\n\n" + errors;
                    Invoke(new InvokeDelegateMessageBox(ShowMessageBox), message, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception e)
            {
                if (showErrors)
                    Invoke(new InvokeDelegateMessageBox(ShowMessageBox), e.ToString(), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            return null;
        }

        void LoadArchive(Archive archive, bool refreshState)
        {
            m_LoadedArchive = archive;
            var loaded = archive != null;
            m_LastOpenedFolder = loaded ? Path.GetDirectoryName(archive.FileName) ?? m_ProgFolder : m_ProgFolder;
            if (refreshState)
                m_FilesListOrigState = loaded ? archive.FilesList.ToList() : null;
            lblFileNameVal.Text = loaded ? Path.GetFileName(archive.FileName) : "No file loaded";
            lblArchiveTypeVal.Text = loaded ? archive.ShortTypeName : "-";
            lblFilesVal.Text = loaded ? archive.FilesList.Count.ToString(CultureInfo.InvariantCulture) : "-";
            lblExtraInfoVal.Text = loaded && archive.ExtraInfo != null ? archive.ExtraInfo : "-";
            lbFilesList.Items.Clear();
            tsmiFileSave.Enabled = loaded && archive.CanSave;
            tsmiFileSaveAs.Enabled = loaded;
            tsmiFileReload.Enabled = loaded;
            tsmiFileClose.Enabled = loaded;
            tsmiArchiveInsert.Enabled = loaded;
            tsmiArchiveInsertAs.Enabled = loaded;
            tsmiArchiveDelete.Enabled = false;
            tsmiArchiveExtract.Enabled = false;
            if (loaded)
                foreach (var entry in archive.FilesList)
                    lbFilesList.Items.Add(entry);
            RefreshSidebarFileInfo();
            Text = GetTitle(true, true);
        }

        bool IsArchiveModified()
        {
            if (m_LoadedArchive == null)
                return false;
            var curState = m_LoadedArchive.FilesList.OrderBy(x => x.FileName).ToList();
            var origState = m_FilesListOrigState.OrderBy(x => x.FileName).ToList();
            if (curState.Count != origState.Count)
                return true;
            for (var i = 0; i < curState.Count; i++)
                if (!curState[i].Equals(origState[i]))
                    return true;
            return false;
        }

        void lbFilesList_SelectedIndexChanged(object sender, EventArgs e)
        {
            RefreshSidebarFileInfo();
        }

        void RefreshSidebarFileInfo()
        {
            var selected = lbFilesList.SelectedIndices.Count;
            tsmiArchiveExtract.Enabled = selected > 0;
            tsmiArchiveDelete.Enabled = selected > 0;
            if (selected > 1)
                lblSelectedFileVal.Text = "Multiple selected (" + selected + ")";
            if (selected == 0)
                lblSelectedFileVal.Text = "Nothing selected";
            if (selected > 1 || selected == 0)
            {
                lblLocationVal.Text = "-";
                lblArchiveNameVal.Text = "-";
                lblStartOffsetVal.Text = "-";
                lblFileSizeVal.Text = "-";
                lblDateStampVal.Text = "-";
                lblIsDirectoryVal.Text = "-";
                lblEntryExtraInfoVal.Text = "-";
                return;
            }

            if (lbFilesList.SelectedItem is not ArchiveEntry entry)
                return;
            lblSelectedFileVal.Text = entry.FileName;
            var isInserted = entry.PhysicalPath != null;
            string lengthStr;
            var accessible = true;
            if (isInserted)
            {
                try
                {
                    if (entry.IsFolder && new DirectoryInfo(entry.PhysicalPath).Exists)
                        lengthStr = "0";
                    else
                        lengthStr = new FileInfo(entry.PhysicalPath).Length.ToString(CultureInfo.InvariantCulture);
                }
                catch
                {
                    lengthStr = "?";
                    accessible = false;
                }
            }
            else
                lengthStr = entry.Length.ToString(CultureInfo.InvariantCulture);
            lblLocationVal.Text = isInserted ? entry.PhysicalPath : "In archive";
            lblArchiveNameVal.Text = Path.GetFileName(entry.ArchivePath);
            lblStartOffsetVal.Text = isInserted ? (accessible ? "0" : "?") : entry.StartOffset.ToString(CultureInfo.InvariantCulture);
            lblFileSizeVal.Text = lengthStr;
            lblDateStampVal.Text = entry.Date.HasValue ? entry.Date.Value.ToString("yyyy-MM-dd, HH:mm:ss", CultureInfo.InvariantCulture) : "-";
            lblIsDirectoryVal.Text = entry.IsFolder ? "Yes" : "No";
            lblEntryExtraInfoVal.Text = entry.ExtraInfo;
            if (!accessible)
                DeleteFileFromArchive(entry.FileName + " appears to be missing! Remove entry from the list?", true);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (ActiveControl is not ListBox list || keyData != (Keys.Control | Keys.A))
                return base.ProcessCmdKey(ref msg, keyData);
            list.BeginUpdate();
            list.Select();
            SendKeys.Send("{Home}");
            SendKeys.Send("{End}");
            SendKeys.Send("+{Home}");
            list.EndUpdate();
            if (list.SelectedItems.Count == 0)
                for (var i = 0; i < list.Items.Count; i++)
                    list.SetSelected(i, true);
            return true;
        }

        void tsmiFileOpen_Click(object sender, EventArgs e)
        {
            //if (this.AbortForChangesAskSave(QUESTION_SAVEFILE_OPENNEW))
            //    return;
            var filename = FileDialogGenerator.ShowOpenFileDialog(this, GetTitle(false), Archive.SupportedTypes, m_LastOpenedFolder, "archives", null, true, out Archive selectedItem);
            if (filename == null)
                return;

            var preferredType = selectedItem == null ? null : new[] { selectedItem };
            var archive = DetectArchive(filename, preferredType, true);
            if (archive != null)
                LoadArchive(archive, true);
        }

        void tsmiFileSave_Click(object sender, EventArgs e)
        {
            SaveArchive();
        }

        void tsmiFileSaveAs_Click(object sender, EventArgs e)
        {
            SaveArchiveAs();
        }

        void tsmiFileReload_Click(object sender, EventArgs e)
        {
            if (m_LoadedArchive == null)
                return;
            if (m_LoadedArchive.FileName == null)
                return;
            var filename = m_LoadedArchive.FileName;

            using (var fs = new FileStream(filename, FileMode.Open))
                m_LoadedArchive.LoadArchive(fs, filename);
            LoadArchive(m_LoadedArchive, true);
        }

        void tsmiFileClose_Click(object sender, EventArgs e)
        {
            LoadArchive(null, true);
        }

        void tsmiFileExit_Click(object sender, EventArgs e)
        {
            Close();
        }

        void tsmiArchiveInsert_Click(object sender, EventArgs e)
        {
            var sfd = new OpenFileDialog { InitialDirectory = m_LastOpenedFolder };
            if (sfd.ShowDialog(this) == DialogResult.OK)
                AddFiles(new[] { sfd.FileName });
        }

        void tsmiArchiveInsertAs_Click(object sender, EventArgs e)
        {
            var sfd = new OpenFileDialog { InitialDirectory = m_LastOpenedFolder };
            if (sfd.ShowDialog(this) != DialogResult.OK)
                return;
            var path = InputBox.Show("Filename in archive:", "Give filename", Path.GetFileName(sfd.FileName));
            if (path == null)
                return;
            if (m_LoadedArchive == null)
                return;
            m_LoadedArchive.InsertFile(sfd.FileName, path);
            LoadArchive(m_LoadedArchive, false);
        }

        void tsmiArchiveExtract_Click(object sender, EventArgs e)
        {
            if (m_LoadedArchive == null || lbFilesList.SelectedItems.Count == 0)
                return;
            if (lbFilesList.SelectedItems.Count == 1)
            {
                if (lbFilesList.SelectedItem is not ArchiveEntry entry)
                    return;
                var sfd = new SaveFileDialog();
                var filename = entry.FileName;
                var folderSep = filename.LastIndexOf('\\');
                if (folderSep != -1)
                    filename = filename[(folderSep + 1)..];
                sfd.FileName = filename;
                sfd.InitialDirectory = m_LastOpenedFolder;
                if (sfd.ShowDialog(this) == DialogResult.OK)
                {
                    m_LastOpenedFolder = Path.GetDirectoryName(sfd.FileName);
                    m_LoadedArchive.ExtractFile(entry.FileName, sfd.FileName);
                }
            }
            else
            {
                var fbd = new FolderBrowserDialog
                {
                    SelectedPath = m_LastOpenedFolder,
                    ShowNewFolderButton = true
                };
                var res = FolderBrowserLauncher.ShowFolderBrowser(fbd, true, this);
                if (res == DialogResult.OK)
                {
                    var path = fbd.SelectedPath;
                    m_LastOpenedFolder = fbd.SelectedPath;
                    var filenames = lbFilesList.SelectedItems.Cast<ArchiveEntry>().Select(en => en.FileName).ToArray();
                    foreach (var filename in filenames)
                        m_LoadedArchive.ExtractFile(filename, Path.Combine(path, filename));
                }
            }
        }

        void tsmiArchiveDelete_Click(object sender, EventArgs e)
        {
            if (m_LoadedArchive == null || lbFilesList.SelectedItems.Count == 0)
                return;
            var question = "Remove ";
            if (lbFilesList.SelectedItems.Count == 1)
                question += "\"" + ((ArchiveEntry)lbFilesList.SelectedItem).FileName + "\"?";
            else
                question += lbFilesList.SelectedItems.Count + " items?";
            DeleteFileFromArchive(question, false);
        }

        void DeleteFileFromArchive(string question, bool useYesNo)
        {
            if (m_LoadedArchive == null || lbFilesList.SelectedItems.Count == 0)
                return;
            var dr = (DialogResult)Invoke(new InvokeDelegateMessageBox(ShowMessageBox),
                question, (useYesNo ? MessageBoxButtons.YesNo : MessageBoxButtons.OKCancel), MessageBoxIcon.Information);
            if ((useYesNo ? DialogResult.Yes : DialogResult.OK) != dr)
                return;
            foreach (ArchiveEntry entry in lbFilesList.SelectedItems)
                m_LoadedArchive.FilesList.Remove(entry);
            LoadArchive(m_LoadedArchive, false);
        }

        void SaveArchive()
        {
            if (m_LoadedArchive == null)
                return;
            if (m_LoadedArchive.FileName == null)
                SaveArchiveAs();
            else
                SaveArchive(m_LoadedArchive, m_LoadedArchive.FileName);
        }

        void SaveArchiveAs()
        {
            if (m_LoadedArchive == null)
                return;
            var suggestedfilename = m_LoadedArchive.FileName ?? Path.Combine(m_LastOpenedFolder, "archive." + (m_LoadedArchive.FileExtensions.FirstOrDefault() ?? "lib").ToLowerInvariant());
            var filename = FileDialogGenerator.ShowSaveFileFialog(this, m_LoadedArchive.GetType(), Archive.SupportedSaveTypes, m_LoadedArchive.GetType(), false, true, suggestedfilename, out Archive selectedItem);
            if (filename == null || selectedItem == null)
                return;
            SaveArchive(selectedItem, filename);
        }

        void SaveArchive(Archive archiveType, string filename)
        {
            if (!archiveType.CanSave)
            {
                Invoke(new InvokeDelegateMessageBox(ShowMessageBox), "Saving is not supported for this format. Sorry!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!archiveType.SupportsFolders)
            {
                foreach (var entry in m_LoadedArchive.FilesList)
                {
                    if (entry.IsFolder || entry.FileName.Contains('\\'))
                    {
                        Invoke(new InvokeDelegateMessageBox(ShowMessageBox), "Cannot save as this archive type; it does not support subfolders.", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }
            }
            try
            {
                var fi = new FileInfo(filename);
                if (fi.IsReadOnly)
                {
                    Invoke(new InvokeDelegateMessageBox(ShowMessageBox), "Cannot save to this file; it is read-only.", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            catch (Exception)
            {
                Invoke(new InvokeDelegateMessageBox(ShowMessageBox), "Could not access the file path.", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                archiveType.SaveArchive(m_LoadedArchive, filename);
            }
            catch (NotImplementedException)
            {
                Invoke(new InvokeDelegateMessageBox(ShowMessageBox), "Saving is not supported for this format. Sorry!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            catch (ArgumentException e)
            {
                // No stack trace; just show the message.
                Invoke(new InvokeDelegateMessageBox(ShowMessageBox), e.Message, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            catch (Exception e)
            {
                Invoke(new InvokeDelegateMessageBox(ShowMessageBox), e.ToString(), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (filename == m_LoadedArchive.FileName)
            {
                using (var fs = new FileStream(filename, FileMode.Open))
                    archiveType.LoadArchive(fs, filename);
                LoadArchive(archiveType, true);
            }
        }

        DialogResult ShowMessageBox(string message, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            return ShowMessageBox(message, buttons, icon, MessageBoxDefaultButton.Button1);
        }

        DialogResult ShowMessageBox(string message, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defButtons)
        {
            if (message == null)
                return buttons == MessageBoxButtons.YesNo ? DialogResult.No : (buttons == MessageBoxButtons.OK ? DialogResult.OK : DialogResult.Cancel);
            return MessageBox.Show(this, message, GetTitle(false), buttons, icon, defButtons);
        }

        void lbFilesList_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            var cm = new ContextMenuStrip();
            var cmInsert = new ToolStripMenuItem(tsmiArchiveInsert.Text, null, tsmiArchiveInsert_Click);
            var cmInsertAs = new ToolStripMenuItem(tsmiArchiveInsertAs.Text, null, tsmiArchiveInsertAs_Click);
            var cmExtract = new ToolStripMenuItem(tsmiArchiveExtract.Text, null, tsmiArchiveExtract_Click);
            var cmDelete = new ToolStripMenuItem(tsmiArchiveDelete.Text, null, tsmiArchiveDelete_Click);

            var loaded = m_LoadedArchive != null;
            var selected = lbFilesList.SelectedIndices.Count > 0;
            cmInsert.Enabled = loaded;
            cmInsertAs.Enabled = loaded;
            cmDelete.Enabled = selected;
            cmExtract.Enabled = selected;

            var ms = new MenuStrip();
            ms.Items.Add(cmInsert);
            ms.Items.Add(cmInsertAs);
            ms.Items.Add(cmExtract);
            ms.Items.Add(cmDelete);

            cm.Show(lbFilesList, e.Location);
        }

    }
}
