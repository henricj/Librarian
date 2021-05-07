using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using LibrarianTool.Domain;
using Nyerguds.Util;
using Nyerguds.Util.UI;
using System.ComponentModel;

namespace LibrarianTool
{
    public partial class FrmLibTool : Form
    {
        public delegate void InvokeDelegateReload(Archive newFile, Boolean asNew, Boolean resetZoom);
        public delegate DialogResult InvokeDelegateMessageBox(String message, MessageBoxButtons buttons, MessageBoxIcon icon);
        public delegate DialogResult InvokeDelegateMessageBoxDef(String message, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defButtons);
        public delegate void InvokeDelegateTwoArgs(Object arg1, Object arg2);
        public delegate void InvokeDelegateSingleArg(Object value);
        public delegate void InvokeDelegateEnableControls(Boolean enabled, String processingLabel);

        private const String PROG_NAME = "Librarian";
        private const String PROG_AUTHOR = "Created by Nyerguds";

        protected readonly String m_ProgFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        protected String m_LastOpenedFolder;
        protected Archive m_LoadedArchive;
        protected List<ArchiveEntry> m_FilesListOrigState;
        protected String argFile;

        public FrmLibTool(String[] args)
            :this()
        {
            if (args.Length > 0 && File.Exists(args[0]))
                argFile = args[0];
        }

        public FrmLibTool()
        {
            this.InitializeComponent();
            AddNewTypes();
            m_LastOpenedFolder = m_ProgFolder;
            this.Text = this.GetTitle(true, true);
        }

        public String GetTitle(Boolean withAuthor, Boolean withLoadedArchive)
        {
            StringBuilder title = GetTitleBuilder(withAuthor);
            if (withLoadedArchive && m_LoadedArchive != null)
            {
                title.Append(" - ");
                if (m_LoadedArchive.FileName == null)
                    title.Append("New archive");
                else
                    title.Append("\"").Append(Path.GetFileName(m_LoadedArchive.FileName)).Append("\"");
                if (IsArchiveModified())
                    title.Append(" *");
                title.Append(" (").Append(m_LoadedArchive.ShortTypeDescription).Append(")");
            }
            return title.ToString();
        }

        public static String GetTitle(Boolean withAuthor)
        {
            return GetTitleBuilder(withAuthor).ToString();
        }

        public static StringBuilder GetTitleBuilder(Boolean withAuthor)
        {
            StringBuilder title = new StringBuilder(PROG_NAME);
            title.Append(" ").Append(GeneralUtils.ProgramVersion());
            if (withAuthor)
                title.Append(" - ").Append(PROG_AUTHOR);
            return title;
        }

        private void AddNewTypes()
        {
            foreach (Type type in Archive.SupportedTypes)
            {
                Archive archInstance = null;
                try { archInstance = (Archive)Activator.CreateInstance(type); }
                catch { /* Ignore; programmer error. */ }
                if (archInstance == null || !archInstance.CanSave)
                    continue;
                ToolStripMenuItem archtypeMenu = new ToolStripMenuItem();
                archtypeMenu.Text = archInstance.ShortTypeDescription;
                archtypeMenu.Tag = type;
                archtypeMenu.Click += this.NewFileClick;
                tsmiFileNew.DropDownItems.Add(archtypeMenu);
            }
        }

        private void NewFileClick(Object sender, EventArgs e)
        {
            ToolStripMenuItem tsmi = sender as ToolStripMenuItem;
            Type type;
            if (tsmi == null || (type = tsmi.Tag as Type) == null)
                return;
            Archive archInstance;
            try { archInstance = (Archive)Activator.CreateInstance(type); }
            catch { return; }
            LoadArchive(archInstance, true);
        }


        private void FrmLibTool_Shown(Object sender, EventArgs e)
        {
            if (argFile != null)
                this.DetectArchive(argFile, true);
            else
                LoadArchive(null, false);
        }

        private void Frm_DragEnter(Object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        private void Frm_DragDrop(Object sender, DragEventArgs e)
        {
            String[] files = (String[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length != 1)
                return;
            String path = files[0];
            this.m_LastOpenedFolder = Path.GetDirectoryName(path);
            Archive arch = this.DetectArchive(path, true);
            if (arch != null)
                this.LoadArchive(arch, true);
        }

        private void Lv_DragEnter(Object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        private void Lv_DragDrop(Object sender, DragEventArgs e)
        {
            String[] files = (String[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length == 0)
                return;
            if (this.m_LoadedArchive == null)
            {
                const String message = "No archive has been opened.\n\n" +
                                       "To make a new archive, use the \"New archive\" function in the menu.\n\n" +
                                       "To open an archive, drop it into the area outside the files list.";
                this.Invoke(new InvokeDelegateMessageBox(this.ShowMessageBox), message, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
                this.AddFiles(files);
        }

        private void AddFiles(String[] files)
        {
            if (files.Length == 0)
                return;
            if (this.m_LoadedArchive == null)
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
                List<String> filesList = new List<String>();
                List<String> filesListRel = new List<String>();
                foreach (String file in files)
                {
                    if ((File.GetAttributes(file) & FileAttributes.Directory) != 0)
                    {
                        String containingFolder = Path.GetDirectoryName(file);
                        AddFilesRecursive(file, containingFolder, filesList, filesListRel);
                    }
                    else
                    {
                        filesList.Add(file);
                        filesListRel.Add(null);
                    }
                }
                for (int i = 0; i < filesList.Count; i++)
                {
                    if (filesListRel[i] == null)
                        this.m_LoadedArchive.InsertFile(filesList[i]);
                    else
                        this.m_LoadedArchive.InsertFile(filesList[i], filesListRel[i]);
                }
            }
            this.LoadArchive(this.m_LoadedArchive, false);
        }

        private void AddFilesRecursive(String file, String basePath, List<String> filesList, List<String> filesListRelative)
        {
            String fullBasePath = Path.GetFullPath(basePath);
            String fullFilePath = Path.GetFullPath(file);
            Int32 basePathLen = fullBasePath.Length + 1;
            filesList.Add(file);
            String filePathRel = fullFilePath.Substring(basePathLen);
            filesListRelative.Add(filePathRel.Contains('\\') ? filePathRel : null);
            if ((File.GetAttributes(file) & FileAttributes.Directory) == 0)
                return;
            String[] files = Directory.GetFiles(file);
            filesList.AddRange(files);
            for (Int32 i = 0; i < files.Length; i++)
                files[i] = files[i].Substring(basePathLen);
            filesListRelative.AddRange(files);
            String[] subDirs = Directory.GetDirectories(file);
            foreach (String subDir in subDirs)
                AddFilesRecursive(subDir, basePath, filesList, filesListRelative);
        }

        private Archive DetectArchive(String path, Boolean showErrors)
        {
            return DetectArchive(path, null, showErrors);
        }

        private Archive DetectArchive(String path, Archive[] specificOpenTypes, Boolean showErrors)
        {
            try
            {
                Archive archive;
                List<FileTypeLoadException> loadErrors;
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                    archive = Archive.LoadArchiveAutodetect(fs, path, specificOpenTypes, specificOpenTypes != null, out loadErrors);
                if (archive != null)
                    return archive;
                if (loadErrors != null && loadErrors.Count > 0 && showErrors)
                {
                    String errors = String.Join("\n", loadErrors.Select(er => er.AttemptedLoadedType + ": " + er.Message).ToArray());
                    String filename = path == null ? String.Empty : (" of \"" + Path.GetFileName(path) + "\"");
                    String message = "File type of " + filename + " could not be identified. Errors returned by all attempts:\n\n" + errors;
                    this.Invoke(new InvokeDelegateMessageBox(this.ShowMessageBox), message, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception e)
            {
                if (showErrors)
                    this.Invoke(new InvokeDelegateMessageBox(this.ShowMessageBox), e.ToString(), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            return null;
        }

        private void LoadArchive(Archive archive, Boolean refreshState)
        {
            this.m_LoadedArchive = archive;
            Boolean loaded = archive != null;
            this.m_LastOpenedFolder = loaded ? Path.GetDirectoryName(archive.FileName) ?? m_ProgFolder : m_ProgFolder;
            if (refreshState)
                m_FilesListOrigState = loaded ? archive.FilesList.ToList() : null;
            this.lblFileNameVal.Text = loaded ? Path.GetFileName(archive.FileName) : "No file loaded";
            this.lblArchiveTypeVal.Text = loaded ? archive.ShortTypeName : "-";
            this.lblFilesVal.Text = loaded ? archive.FilesList.Count.ToString() : "-";
            this.lblExtraInfoVal.Text = loaded && archive.ExtraInfo != null ? archive.ExtraInfo : "-";
            this.lbFilesList.Items.Clear();
            this.tsmiFileSave.Enabled = loaded && archive.CanSave;
            this.tsmiFileSaveAs.Enabled = loaded;
            this.tsmiFileReload.Enabled = loaded;
            this.tsmiFileClose.Enabled = loaded;
            this.tsmiArchiveInsert.Enabled = loaded;
            this.tsmiArchiveInsertAs.Enabled = loaded;
            this.tsmiArchiveDelete.Enabled = false;
            this.tsmiArchiveExtract.Enabled = false;
            if (loaded)
                foreach (ArchiveEntry entry in archive.FilesList)
                    this.lbFilesList.Items.Add(entry);
            this.RefreshSidebarFileInfo();
            this.Text = this.GetTitle(true, true);
        }

        private Boolean IsArchiveModified()
        {
            if (m_LoadedArchive == null)
                return false;
            List<ArchiveEntry> curState = m_LoadedArchive.FilesList.OrderBy(x => x.FileName).ToList();
            List<ArchiveEntry> origState = m_FilesListOrigState.OrderBy(x => x.FileName).ToList();
            if (curState.Count != origState.Count)
                return true;
            for (Int32 i = 0; i < curState.Count; i++)
                if (!curState[i].Equals(origState[i]))
                    return true;
            return false;
        }

        private void lbFilesList_SelectedIndexChanged(Object sender, EventArgs e)
        {
            RefreshSidebarFileInfo();
        }

        private void RefreshSidebarFileInfo()
        {
            Int32 selected = this.lbFilesList.SelectedIndices.Count;
            tsmiArchiveExtract.Enabled = selected > 0;
            tsmiArchiveDelete.Enabled = selected > 0;
            if (selected > 1)
                this.lblSelectedFileVal.Text = "Multiple selected (" + selected + ")";
            if (selected == 0)
                this.lblSelectedFileVal.Text = "Nothing selected";
            if (selected > 1 || selected == 0)
            {
                this.lblLocationVal.Text = "-";
                this.lblArchiveNameVal.Text = "-";
                this.lblStartOffsetVal.Text = "-";
                this.lblFileSizeVal.Text = "-";
                this.lblDateStampVal.Text = "-";
                this.lblIsDirectoryVal.Text = "-";
                this.lblEntryExtraInfoVal.Text = "-";
                return;
            }
            ArchiveEntry entry = this.lbFilesList.SelectedItem as ArchiveEntry;
            if (entry == null)
                return;
            this.lblSelectedFileVal.Text = entry.FileName;
            Boolean isInserted = entry.PhysicalPath != null;
            String lengthStr;
            Boolean accessible = true;
            if (isInserted)
            {
                try
                {
                    if (entry.IsFolder && new DirectoryInfo(entry.PhysicalPath).Exists)
                        lengthStr = "0";
                    else
                        lengthStr = new FileInfo(entry.PhysicalPath).Length.ToString();
                }
                catch
                {
                    lengthStr = "?";
                    accessible = false;
                }
            }
            else
                lengthStr = entry.Length.ToString();
            this.lblLocationVal.Text = isInserted ? entry.PhysicalPath : "In archive";
            this.lblArchiveNameVal.Text = Path.GetFileName(entry.ArchivePath);
            this.lblStartOffsetVal.Text = isInserted ? (accessible ? "0" : "?") : entry.StartOffset.ToString();
            this.lblFileSizeVal.Text = lengthStr;
            this.lblDateStampVal.Text = entry.Date.HasValue ? entry.Date.Value.ToString("yyyy-MM-dd, HH:mm:ss") : "-";
            this.lblIsDirectoryVal.Text = entry.IsFolder ? "Yes" : "No";
            this.lblEntryExtraInfoVal.Text = entry.ExtraInfo;
            if(!accessible)
                this.DeleteFileFromArchive(entry.FileName + " appears to be missing! Remove entry from the list?", true);
        }

        protected override Boolean ProcessCmdKey(ref Message msg, Keys keyData)
        {
            ListBox list = this.ActiveControl as ListBox;
            if (list == null || keyData != (Keys.Control | Keys.A))
                return base.ProcessCmdKey(ref msg, keyData);
            list.BeginUpdate();
            list.Select();
            SendKeys.Send("{Home}");
            SendKeys.Send("{End}");
            SendKeys.Send("+{Home}");
            list.EndUpdate();
            if (list.SelectedItems.Count == 0)
                for (Int32 i = 0; i < list.Items.Count; i++)
                    list.SetSelected(i, true);
            return true;
        }

        private void tsmiFileOpen_Click(Object sender, EventArgs e)
        {
            //if (this.AbortForChangesAskSave(QUESTION_SAVEFILE_OPENNEW))
            //    return;
            Archive selectedItem;
            String filename = FileDialogGenerator.ShowOpenFileFialog(this, GetTitle(false), Archive.SupportedTypes, this.m_LastOpenedFolder, "archives", null, true, out selectedItem);
            if (filename == null)
                return;
            
            Archive[] preferredType = selectedItem == null ? null : new Archive[] {selectedItem};
            Archive archive = this.DetectArchive(filename, preferredType, true);
            if (archive != null)
                this.LoadArchive(archive, true);
        }

        private void tsmiFileSave_Click(Object sender, EventArgs e)
        {
            this.SaveArchive();
        }

        private void tsmiFileSaveAs_Click(Object sender, EventArgs e)
        {
            this.SaveArchiveAs();
        }

        private void tsmiFileReload_Click(Object sender, EventArgs e)
        {
            if (this.m_LoadedArchive == null)
                return;
            if (this.m_LoadedArchive.FileName == null)
                return;
            String filename = this.m_LoadedArchive.FileName;

            using (FileStream fs = new FileStream(filename, FileMode.Open))
                this.m_LoadedArchive.LoadArchive(fs, filename);
            this.LoadArchive(this.m_LoadedArchive, true);
        }

        private void tsmiFileClose_Click(Object sender, EventArgs e)
        {
            LoadArchive(null, true);
        }

        private void tsmiFileExit_Click(Object sender, EventArgs e)
        {
            this.Close();
        }

        private void tsmiArchiveInsert_Click(Object sender, EventArgs e)
        {
            OpenFileDialog sfd = new OpenFileDialog();
            sfd.InitialDirectory = m_LastOpenedFolder;
            if (sfd.ShowDialog(this) == DialogResult.OK)
                AddFiles(new String[] {sfd.FileName});
        }

        private void tsmiArchiveInsertAs_Click(Object sender, EventArgs e)
        {
            OpenFileDialog sfd = new OpenFileDialog();
            sfd.InitialDirectory = m_LastOpenedFolder;
            if (sfd.ShowDialog(this) != DialogResult.OK)
                return;
            String path = InputBox.Show("Filename in archive:", "Give filename", Path.GetFileName(sfd.FileName));
            if (path == null)
                return;
            if (this.m_LoadedArchive == null)
                return;
            this.m_LoadedArchive.InsertFile(sfd.FileName, path);
            this.LoadArchive(this.m_LoadedArchive, false);
        }

        private void tsmiArchiveExtract_Click(Object sender, EventArgs e)
        {
            if (m_LoadedArchive == null || lbFilesList.SelectedItems.Count == 0)
                return;
            if (lbFilesList.SelectedItems.Count == 1)
            {
                ArchiveEntry entry = this.lbFilesList.SelectedItem as ArchiveEntry;
                if (entry == null)
                    return;
                SaveFileDialog sfd = new SaveFileDialog();
                String filename = entry.FileName;
                Int32 folderSep = filename.LastIndexOf('\\');
                if (folderSep != -1)
                    filename = filename.Substring(folderSep + 1);
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
                FolderBrowserDialog fbd = new FolderBrowserDialog();
                fbd.SelectedPath = m_LastOpenedFolder;
                fbd.ShowNewFolderButton = true;
                DialogResult res = FolderBrowserLauncher.ShowFolderBrowser(fbd, true, this);
                if (res == DialogResult.OK)
                {
                    String path = fbd.SelectedPath;
                    m_LastOpenedFolder = fbd.SelectedPath;
                    String[] filenames = this.lbFilesList.SelectedItems.Cast<ArchiveEntry>().Select(en => en.FileName).ToArray();
                    foreach (String filename in filenames)
                        m_LoadedArchive.ExtractFile(filename, Path.Combine(path, filename));
                }
            }
        }

        private void tsmiArchiveDelete_Click(Object sender, EventArgs e)
        {
            if (m_LoadedArchive == null || lbFilesList.SelectedItems.Count == 0)
                return;
            String question = "Remove ";
            if (lbFilesList.SelectedItems.Count == 1)
                question += "\"" + ((ArchiveEntry) lbFilesList.SelectedItem).FileName + "\"?";
            else
                question += lbFilesList.SelectedItems.Count + " items?";
            this.DeleteFileFromArchive(question, false);
        }
        
        private void DeleteFileFromArchive(String question, Boolean useYesNo)
        {
            if (m_LoadedArchive == null || lbFilesList.SelectedItems.Count == 0)
                return;
            DialogResult dr = (DialogResult)this.Invoke(new InvokeDelegateMessageBox(this.ShowMessageBox),
                question, (useYesNo ? MessageBoxButtons.YesNo : MessageBoxButtons.OKCancel), MessageBoxIcon.Information);
            if ((useYesNo ? DialogResult.Yes : DialogResult.OK) != dr)
                return;
            foreach (ArchiveEntry entry in lbFilesList.SelectedItems)
                this.m_LoadedArchive.FilesList.Remove(entry);
            this.LoadArchive(this.m_LoadedArchive, false);
        }

        private void SaveArchive()
        {
            if (this.m_LoadedArchive == null)
                return;
            if (this.m_LoadedArchive.FileName == null)
                this.SaveArchiveAs();
            else
                this.SaveArchive(this.m_LoadedArchive, this.m_LoadedArchive.FileName);
        }

        private void SaveArchiveAs()
        {
            if (this.m_LoadedArchive == null)
                return;
            Archive selectedItem;
            String suggestedfilename = this.m_LoadedArchive.FileName ?? Path.Combine(m_LastOpenedFolder, "archive." + (this.m_LoadedArchive.FileExtensions.FirstOrDefault() ?? "lib").ToLowerInvariant());
            String filename = FileDialogGenerator.ShowSaveFileFialog(this, this.m_LoadedArchive.GetType(), Archive.SupportedSaveTypes, m_LoadedArchive.GetType(), false, true, suggestedfilename, out selectedItem);
            if (filename == null || selectedItem == null)
                return;
            this.SaveArchive(selectedItem, filename);
        }

        private void SaveArchive(Archive archiveType, String filename)
        {
            if (!archiveType.CanSave)
            {
                this.Invoke(new InvokeDelegateMessageBox(this.ShowMessageBox), "Saving is not supported for this format. Sorry!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!archiveType.SupportsFolders)
            {
                foreach (ArchiveEntry entry in this.m_LoadedArchive.FilesList)
                {
                    if (entry.IsFolder || entry.FileName.Contains("\\"))
                    {
                        this.Invoke(new InvokeDelegateMessageBox(this.ShowMessageBox), "Cannot save as this archive type; it does not support subfolders.", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }
            }
            try
            {
                FileInfo fi = new FileInfo(filename);
                if (fi.IsReadOnly)
                {
                    this.Invoke(new InvokeDelegateMessageBox(this.ShowMessageBox), "Cannot save to this file; it is read-only.", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            catch (Exception)
            {
                this.Invoke(new InvokeDelegateMessageBox(this.ShowMessageBox), "Could not access the file path.", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                archiveType.SaveArchive(this.m_LoadedArchive, filename);
            }
            catch (NotImplementedException)
            {
                this.Invoke(new InvokeDelegateMessageBox(this.ShowMessageBox), "Saving is not supported for this format. Sorry!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            catch (ArgumentException e)
            {
                // No stack trace; just show the message.
                this.Invoke(new InvokeDelegateMessageBox(this.ShowMessageBox), e.Message, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            catch (Exception e)
            {
                this.Invoke(new InvokeDelegateMessageBox(this.ShowMessageBox), e.ToString(), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (filename == this.m_LoadedArchive.FileName)
            {
                using (FileStream fs = new FileStream(filename, FileMode.Open))
                    archiveType.LoadArchive(fs, filename);
                this.LoadArchive(archiveType, true);
            }
        }

        private DialogResult ShowMessageBox(String message, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            return ShowMessageBox(message, buttons, icon, MessageBoxDefaultButton.Button1);
        }

        private DialogResult ShowMessageBox(String message, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defButtons)
        {
            if (message == null)
                return buttons == MessageBoxButtons.YesNo ? DialogResult.No : (buttons == MessageBoxButtons.OK ? DialogResult.OK : DialogResult.Cancel);
            return MessageBox.Show(this, message, GetTitle(false), buttons, icon, defButtons);
        }
        
        private void lbFilesList_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;
            ContextMenu cm = new ContextMenu();
            MenuItem cmInsert = new MenuItem(tsmiArchiveInsert.Text, tsmiArchiveInsert_Click);
            MenuItem cmInsertAs = new MenuItem(tsmiArchiveInsertAs.Text, tsmiArchiveInsertAs_Click);
            MenuItem cmExtract = new MenuItem(tsmiArchiveExtract.Text, tsmiArchiveExtract_Click);
            MenuItem cmDelete = new MenuItem(tsmiArchiveDelete.Text, tsmiArchiveDelete_Click);

            Boolean loaded = this.m_LoadedArchive != null;
            Boolean selected = this.lbFilesList.SelectedIndices.Count > 0;
            cmInsert.Enabled = loaded;
            cmInsertAs.Enabled = loaded;
            cmDelete.Enabled = selected;
            cmExtract.Enabled = selected;
            cm.MenuItems.Add(cmInsert);
            cm.MenuItems.Add(cmInsertAs);
            cm.MenuItems.Add(cmDelete);
            cm.MenuItems.Add(cmExtract);
            cm.Show(lbFilesList, e.Location);
        }

    }
}
