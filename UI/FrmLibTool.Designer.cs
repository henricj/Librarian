namespace LibrarianTool
{
    partial class FrmLibTool
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.tsmiFile = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiFileNew = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiFileOpen = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiFileSave = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiFileSaveAs = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiFileReload = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiFileClose = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiFileExit = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiArchive = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiArchiveInsert = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiArchiveInsertAs = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiArchiveExtract = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiArchiveDelete = new System.Windows.Forms.ToolStripMenuItem();
            this.lbFilesList = new System.Windows.Forms.ListBox();
            this.lblFileNameVal = new System.Windows.Forms.Label();
            this.lblArchiveTypeVal = new System.Windows.Forms.Label();
            this.lblSelectedFile = new System.Windows.Forms.Label();
            this.lblFilename = new System.Windows.Forms.Label();
            this.LblArschiveType = new System.Windows.Forms.Label();
            this.lblSelectedFileVal = new System.Windows.Forms.Label();
            this.lblLocation = new System.Windows.Forms.Label();
            this.lblLocationVal = new System.Windows.Forms.Label();
            this.lblStartOffset = new System.Windows.Forms.Label();
            this.lblStartOffsetVal = new System.Windows.Forms.Label();
            this.lblFileSize = new System.Windows.Forms.Label();
            this.lblFileSizeVal = new System.Windows.Forms.Label();
            this.lblFilesVal = new System.Windows.Forms.Label();
            this.lblFiles = new System.Windows.Forms.Label();
            this.lblExtraInfoVal = new System.Windows.Forms.Label();
            this.lblExtraInfo = new System.Windows.Forms.Label();
            this.lblArchiveName = new System.Windows.Forms.Label();
            this.lblArchiveNameVal = new System.Windows.Forms.Label();
            this.lblEntryExtraInfo = new System.Windows.Forms.Label();
            this.lblEntryExtraInfoVal = new System.Windows.Forms.Label();
            this.lblIsDirectory = new System.Windows.Forms.Label();
            this.lblDateStamp = new System.Windows.Forms.Label();
            this.lblDateStampVal = new System.Windows.Forms.Label();
            this.lblIsDirectoryVal = new System.Windows.Forms.Label();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmiFile,
            this.tsmiArchive});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(684, 24);
            this.menuStrip1.TabIndex = 1;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // tsmiFile
            // 
            this.tsmiFile.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmiFileNew,
            this.tsmiFileOpen,
            this.tsmiFileSave,
            this.tsmiFileSaveAs,
            this.tsmiFileReload,
            this.tsmiFileClose,
            this.tsmiFileExit});
            this.tsmiFile.Name = "tsmiFile";
            this.tsmiFile.Size = new System.Drawing.Size(37, 20);
            this.tsmiFile.Text = "&File";
            // 
            // tsmiFileNew
            // 
            this.tsmiFileNew.Name = "tsmiFileNew";
            this.tsmiFileNew.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.N)));
            this.tsmiFileNew.Size = new System.Drawing.Size(236, 22);
            this.tsmiFileNew.Text = "&New archive";
            // 
            // tsmiFileOpen
            // 
            this.tsmiFileOpen.Name = "tsmiFileOpen";
            this.tsmiFileOpen.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            this.tsmiFileOpen.Size = new System.Drawing.Size(236, 22);
            this.tsmiFileOpen.Text = "&Open archive...";
            this.tsmiFileOpen.Click += new System.EventHandler(this.tsmiFileOpen_Click);
            // 
            // tsmiFileSave
            // 
            this.tsmiFileSave.Name = "tsmiFileSave";
            this.tsmiFileSave.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
            this.tsmiFileSave.Size = new System.Drawing.Size(236, 22);
            this.tsmiFileSave.Text = "&Save archive...";
            this.tsmiFileSave.Click += new System.EventHandler(this.tsmiFileSave_Click);
            // 
            // tsmiFileSaveAs
            // 
            this.tsmiFileSaveAs.Name = "tsmiFileSaveAs";
            this.tsmiFileSaveAs.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.S)));
            this.tsmiFileSaveAs.Size = new System.Drawing.Size(236, 22);
            this.tsmiFileSaveAs.Text = "Save archive &As...";
            this.tsmiFileSaveAs.Click += new System.EventHandler(this.tsmiFileSaveAs_Click);
            // 
            // tsmiFileReload
            // 
            this.tsmiFileReload.Name = "tsmiFileReload";
            this.tsmiFileReload.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.R)));
            this.tsmiFileReload.Size = new System.Drawing.Size(236, 22);
            this.tsmiFileReload.Text = "&Reload archive";
            this.tsmiFileReload.Click += new System.EventHandler(this.tsmiFileReload_Click);
            // 
            // tsmiFileClose
            // 
            this.tsmiFileClose.Name = "tsmiFileClose";
            this.tsmiFileClose.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.L)));
            this.tsmiFileClose.Size = new System.Drawing.Size(236, 22);
            this.tsmiFileClose.Text = "C&lose archive";
            this.tsmiFileClose.Click += new System.EventHandler(this.tsmiFileClose_Click);
            // 
            // tsmiFileExit
            // 
            this.tsmiFileExit.Name = "tsmiFileExit";
            this.tsmiFileExit.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4)));
            this.tsmiFileExit.Size = new System.Drawing.Size(236, 22);
            this.tsmiFileExit.Text = "E&xit";
            this.tsmiFileExit.Click += new System.EventHandler(this.tsmiFileExit_Click);
            // 
            // tsmiArchive
            // 
            this.tsmiArchive.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmiArchiveInsert,
            this.tsmiArchiveInsertAs,
            this.tsmiArchiveExtract,
            this.tsmiArchiveDelete});
            this.tsmiArchive.Name = "tsmiArchive";
            this.tsmiArchive.Size = new System.Drawing.Size(59, 20);
            this.tsmiArchive.Text = "&Archive";
            // 
            // tsmiArchiveInsert
            // 
            this.tsmiArchiveInsert.Name = "tsmiArchiveInsert";
            this.tsmiArchiveInsert.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.I)));
            this.tsmiArchiveInsert.Size = new System.Drawing.Size(185, 22);
            this.tsmiArchiveInsert.Text = "&Insert file...";
            this.tsmiArchiveInsert.Click += new System.EventHandler(this.tsmiArchiveInsert_Click);
            // 
            // tsmiArchiveInsertAs
            // 
            this.tsmiArchiveInsertAs.Name = "tsmiArchiveInsertAs";
            this.tsmiArchiveInsertAs.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.T)));
            this.tsmiArchiveInsertAs.Size = new System.Drawing.Size(185, 22);
            this.tsmiArchiveInsertAs.Text = "Insert file as...";
            this.tsmiArchiveInsertAs.Click += new System.EventHandler(this.tsmiArchiveInsertAs_Click);
            // 
            // tsmiArchiveExtract
            // 
            this.tsmiArchiveExtract.Name = "tsmiArchiveExtract";
            this.tsmiArchiveExtract.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.E)));
            this.tsmiArchiveExtract.Size = new System.Drawing.Size(185, 22);
            this.tsmiArchiveExtract.Text = "&Extract file...";
            this.tsmiArchiveExtract.Click += new System.EventHandler(this.tsmiArchiveExtract_Click);
            // 
            // tsmiArchiveDelete
            // 
            this.tsmiArchiveDelete.Name = "tsmiArchiveDelete";
            this.tsmiArchiveDelete.ShortcutKeys = System.Windows.Forms.Keys.Delete;
            this.tsmiArchiveDelete.Size = new System.Drawing.Size(185, 22);
            this.tsmiArchiveDelete.Text = "&Delete file";
            this.tsmiArchiveDelete.Click += new System.EventHandler(this.tsmiArchiveDelete_Click);
            // 
            // lbFilesList
            // 
            this.lbFilesList.AllowDrop = true;
            this.lbFilesList.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.lbFilesList.FormattingEnabled = true;
            this.lbFilesList.Location = new System.Drawing.Point(12, 27);
            this.lbFilesList.Name = "lbFilesList";
            this.lbFilesList.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.lbFilesList.Size = new System.Drawing.Size(242, 420);
            this.lbFilesList.TabIndex = 2;
            this.lbFilesList.SelectedIndexChanged += new System.EventHandler(this.lbFilesList_SelectedIndexChanged);
            this.lbFilesList.DragDrop += new System.Windows.Forms.DragEventHandler(this.Lv_DragDrop);
            this.lbFilesList.DragEnter += new System.Windows.Forms.DragEventHandler(this.Lv_DragEnter);
            this.lbFilesList.MouseUp += new System.Windows.Forms.MouseEventHandler(this.lbFilesList_MouseUp);
            // 
            // lblFileNameVal
            // 
            this.lblFileNameVal.AutoSize = true;
            this.lblFileNameVal.Location = new System.Drawing.Point(339, 37);
            this.lblFileNameVal.Name = "lblFileNameVal";
            this.lblFileNameVal.Size = new System.Drawing.Size(72, 13);
            this.lblFileNameVal.TabIndex = 3;
            this.lblFileNameVal.Text = "No file loaded";
            // 
            // lblArchiveTypeVal
            // 
            this.lblArchiveTypeVal.AutoSize = true;
            this.lblArchiveTypeVal.Location = new System.Drawing.Point(339, 53);
            this.lblArchiveTypeVal.Name = "lblArchiveTypeVal";
            this.lblArchiveTypeVal.Size = new System.Drawing.Size(10, 13);
            this.lblArchiveTypeVal.TabIndex = 4;
            this.lblArchiveTypeVal.Text = "-";
            // 
            // lblSelectedFile
            // 
            this.lblSelectedFile.AutoSize = true;
            this.lblSelectedFile.Location = new System.Drawing.Point(260, 149);
            this.lblSelectedFile.Name = "lblSelectedFile";
            this.lblSelectedFile.Size = new System.Drawing.Size(26, 13);
            this.lblSelectedFile.TabIndex = 5;
            this.lblSelectedFile.Text = "File:";
            // 
            // lblFilename
            // 
            this.lblFilename.AutoSize = true;
            this.lblFilename.Location = new System.Drawing.Point(260, 37);
            this.lblFilename.Name = "lblFilename";
            this.lblFilename.Size = new System.Drawing.Size(46, 13);
            this.lblFilename.TabIndex = 3;
            this.lblFilename.Text = "Archive:";
            // 
            // LblArschiveType
            // 
            this.LblArschiveType.AutoSize = true;
            this.LblArschiveType.Location = new System.Drawing.Point(260, 53);
            this.LblArschiveType.Name = "LblArschiveType";
            this.LblArschiveType.Size = new System.Drawing.Size(31, 13);
            this.LblArschiveType.TabIndex = 4;
            this.LblArschiveType.Text = "Type";
            // 
            // lblSelectedFileVal
            // 
            this.lblSelectedFileVal.AutoSize = true;
            this.lblSelectedFileVal.Location = new System.Drawing.Point(339, 149);
            this.lblSelectedFileVal.Name = "lblSelectedFileVal";
            this.lblSelectedFileVal.Size = new System.Drawing.Size(10, 13);
            this.lblSelectedFileVal.TabIndex = 5;
            this.lblSelectedFileVal.Text = "-";
            // 
            // lblLocation
            // 
            this.lblLocation.AutoSize = true;
            this.lblLocation.Location = new System.Drawing.Point(260, 165);
            this.lblLocation.Name = "lblLocation";
            this.lblLocation.Size = new System.Drawing.Size(51, 13);
            this.lblLocation.TabIndex = 5;
            this.lblLocation.Text = "Location:";
            // 
            // lblLocationVal
            // 
            this.lblLocationVal.AutoSize = true;
            this.lblLocationVal.Location = new System.Drawing.Point(339, 165);
            this.lblLocationVal.Name = "lblLocationVal";
            this.lblLocationVal.Size = new System.Drawing.Size(10, 13);
            this.lblLocationVal.TabIndex = 5;
            this.lblLocationVal.Text = "-";
            // 
            // lblStartOffset
            // 
            this.lblStartOffset.AutoSize = true;
            this.lblStartOffset.Location = new System.Drawing.Point(260, 197);
            this.lblStartOffset.Name = "lblStartOffset";
            this.lblStartOffset.Size = new System.Drawing.Size(61, 13);
            this.lblStartOffset.TabIndex = 5;
            this.lblStartOffset.Text = "Start offset:";
            // 
            // lblStartOffsetVal
            // 
            this.lblStartOffsetVal.AutoSize = true;
            this.lblStartOffsetVal.Location = new System.Drawing.Point(339, 197);
            this.lblStartOffsetVal.Name = "lblStartOffsetVal";
            this.lblStartOffsetVal.Size = new System.Drawing.Size(10, 13);
            this.lblStartOffsetVal.TabIndex = 5;
            this.lblStartOffsetVal.Text = "-";
            // 
            // lblFileSize
            // 
            this.lblFileSize.AutoSize = true;
            this.lblFileSize.Location = new System.Drawing.Point(260, 213);
            this.lblFileSize.Name = "lblFileSize";
            this.lblFileSize.Size = new System.Drawing.Size(47, 13);
            this.lblFileSize.TabIndex = 5;
            this.lblFileSize.Text = "File size:";
            // 
            // lblFileSizeVal
            // 
            this.lblFileSizeVal.AutoSize = true;
            this.lblFileSizeVal.Location = new System.Drawing.Point(339, 213);
            this.lblFileSizeVal.Name = "lblFileSizeVal";
            this.lblFileSizeVal.Size = new System.Drawing.Size(10, 13);
            this.lblFileSizeVal.TabIndex = 5;
            this.lblFileSizeVal.Text = "-";
            // 
            // lblFilesVal
            // 
            this.lblFilesVal.AutoSize = true;
            this.lblFilesVal.Location = new System.Drawing.Point(339, 69);
            this.lblFilesVal.Name = "lblFilesVal";
            this.lblFilesVal.Size = new System.Drawing.Size(10, 13);
            this.lblFilesVal.TabIndex = 4;
            this.lblFilesVal.Text = "-";
            // 
            // lblFiles
            // 
            this.lblFiles.AutoSize = true;
            this.lblFiles.Location = new System.Drawing.Point(260, 69);
            this.lblFiles.Name = "lblFiles";
            this.lblFiles.Size = new System.Drawing.Size(28, 13);
            this.lblFiles.TabIndex = 4;
            this.lblFiles.Text = "Files";
            // 
            // lblExtraInfoVal
            // 
            this.lblExtraInfoVal.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblExtraInfoVal.Location = new System.Drawing.Point(339, 84);
            this.lblExtraInfoVal.Name = "lblExtraInfoVal";
            this.lblExtraInfoVal.Size = new System.Drawing.Size(333, 63);
            this.lblExtraInfoVal.TabIndex = 8;
            this.lblExtraInfoVal.Text = "-";
            // 
            // lblExtraInfo
            // 
            this.lblExtraInfo.AutoSize = true;
            this.lblExtraInfo.Location = new System.Drawing.Point(260, 84);
            this.lblExtraInfo.Name = "lblExtraInfo";
            this.lblExtraInfo.Size = new System.Drawing.Size(54, 13);
            this.lblExtraInfo.TabIndex = 9;
            this.lblExtraInfo.Text = "Extra info:";
            // 
            // lblArchiveName
            // 
            this.lblArchiveName.AutoSize = true;
            this.lblArchiveName.Location = new System.Drawing.Point(260, 181);
            this.lblArchiveName.Name = "lblArchiveName";
            this.lblArchiveName.Size = new System.Drawing.Size(75, 13);
            this.lblArchiveName.TabIndex = 5;
            this.lblArchiveName.Text = "Archive name:";
            // 
            // lblArchiveNameVal
            // 
            this.lblArchiveNameVal.AutoSize = true;
            this.lblArchiveNameVal.Location = new System.Drawing.Point(339, 181);
            this.lblArchiveNameVal.Name = "lblArchiveNameVal";
            this.lblArchiveNameVal.Size = new System.Drawing.Size(10, 13);
            this.lblArchiveNameVal.TabIndex = 5;
            this.lblArchiveNameVal.Text = "-";
            // 
            // lblEntryExtraInfo
            // 
            this.lblEntryExtraInfo.AutoSize = true;
            this.lblEntryExtraInfo.Location = new System.Drawing.Point(260, 260);
            this.lblEntryExtraInfo.Name = "lblEntryExtraInfo";
            this.lblEntryExtraInfo.Size = new System.Drawing.Size(54, 13);
            this.lblEntryExtraInfo.TabIndex = 12;
            this.lblEntryExtraInfo.Text = "Extra info:";
            // 
            // lblEntryExtraInfoVal
            // 
            this.lblEntryExtraInfoVal.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblEntryExtraInfoVal.Location = new System.Drawing.Point(339, 260);
            this.lblEntryExtraInfoVal.Name = "lblEntryExtraInfoVal";
            this.lblEntryExtraInfoVal.Size = new System.Drawing.Size(333, 63);
            this.lblEntryExtraInfoVal.TabIndex = 11;
            this.lblEntryExtraInfoVal.Text = "-";
            // 
            // lblIsDirectory
            // 
            this.lblIsDirectory.AutoSize = true;
            this.lblIsDirectory.Location = new System.Drawing.Point(260, 245);
            this.lblIsDirectory.Name = "lblIsDirectory";
            this.lblIsDirectory.Size = new System.Drawing.Size(61, 13);
            this.lblIsDirectory.TabIndex = 5;
            this.lblIsDirectory.Text = "Is directory:";
            // 
            // lblDateStamp
            // 
            this.lblDateStamp.AutoSize = true;
            this.lblDateStamp.Location = new System.Drawing.Point(260, 229);
            this.lblDateStamp.Name = "lblDateStamp";
            this.lblDateStamp.Size = new System.Drawing.Size(64, 13);
            this.lblDateStamp.TabIndex = 5;
            this.lblDateStamp.Text = "Date stamp:";
            // 
            // lblDateStampVal
            // 
            this.lblDateStampVal.AutoSize = true;
            this.lblDateStampVal.Location = new System.Drawing.Point(339, 229);
            this.lblDateStampVal.Name = "lblDateStampVal";
            this.lblDateStampVal.Size = new System.Drawing.Size(10, 13);
            this.lblDateStampVal.TabIndex = 5;
            this.lblDateStampVal.Text = "-";
            // 
            // lblIsDirectoryVal
            // 
            this.lblIsDirectoryVal.AutoSize = true;
            this.lblIsDirectoryVal.Location = new System.Drawing.Point(339, 245);
            this.lblIsDirectoryVal.Name = "lblIsDirectoryVal";
            this.lblIsDirectoryVal.Size = new System.Drawing.Size(10, 13);
            this.lblIsDirectoryVal.TabIndex = 5;
            this.lblIsDirectoryVal.Text = "-";
            // 
            // FrmLibTool
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(684, 461);
            this.Controls.Add(this.lblEntryExtraInfoVal);
            this.Controls.Add(this.lblEntryExtraInfo);
            this.Controls.Add(this.lblExtraInfoVal);
            this.Controls.Add(this.lblExtraInfo);
            this.Controls.Add(this.lblFileSizeVal);
            this.Controls.Add(this.lblStartOffsetVal);
            this.Controls.Add(this.lblIsDirectoryVal);
            this.Controls.Add(this.lblDateStampVal);
            this.Controls.Add(this.lblArchiveNameVal);
            this.Controls.Add(this.lblLocationVal);
            this.Controls.Add(this.lblSelectedFileVal);
            this.Controls.Add(this.lblFileSize);
            this.Controls.Add(this.lblStartOffset);
            this.Controls.Add(this.lblIsDirectory);
            this.Controls.Add(this.lblArchiveName);
            this.Controls.Add(this.lblDateStamp);
            this.Controls.Add(this.lblLocation);
            this.Controls.Add(this.lblSelectedFile);
            this.Controls.Add(this.lblFiles);
            this.Controls.Add(this.lblFilesVal);
            this.Controls.Add(this.LblArschiveType);
            this.Controls.Add(this.lblArchiveTypeVal);
            this.Controls.Add(this.lblFilename);
            this.Controls.Add(this.lblFileNameVal);
            this.Controls.Add(this.lbFilesList);
            this.Controls.Add(this.menuStrip1);
            this.Icon = global::LibrarianTool.Properties.Resources.LibrarianIcon;
            this.MainMenuStrip = this.menuStrip1;
            this.MinimumSize = new System.Drawing.Size(430, 230);
            this.Name = "FrmLibTool";
            this.Text = "Librarian";
            this.Shown += new System.EventHandler(this.FrmLibTool_Shown);
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.Frm_DragDrop);
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.Frm_DragEnter);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem tsmiFile;
        private System.Windows.Forms.ToolStripMenuItem tsmiFileOpen;
        private System.Windows.Forms.ToolStripMenuItem tsmiFileSave;
        private System.Windows.Forms.ToolStripMenuItem tsmiFileReload;
        private System.Windows.Forms.ToolStripMenuItem tsmiFileExit;
        private System.Windows.Forms.ToolStripMenuItem tsmiArchive;
        private System.Windows.Forms.ToolStripMenuItem tsmiArchiveInsert;
        private System.Windows.Forms.ToolStripMenuItem tsmiArchiveDelete;
        private System.Windows.Forms.ListBox lbFilesList;
        private System.Windows.Forms.Label lblFileNameVal;
        private System.Windows.Forms.Label lblArchiveTypeVal;
        private System.Windows.Forms.Label lblSelectedFile;
        private System.Windows.Forms.Label lblFilename;
        private System.Windows.Forms.Label LblArschiveType;
        private System.Windows.Forms.Label lblSelectedFileVal;
        private System.Windows.Forms.Label lblLocation;
        private System.Windows.Forms.Label lblLocationVal;
        private System.Windows.Forms.Label lblStartOffset;
        private System.Windows.Forms.Label lblStartOffsetVal;
        private System.Windows.Forms.Label lblFileSize;
        private System.Windows.Forms.Label lblFileSizeVal;
        private System.Windows.Forms.ToolStripMenuItem tsmiArchiveExtract;
        private System.Windows.Forms.ToolStripMenuItem tsmiFileSaveAs;
        private System.Windows.Forms.Label lblFilesVal;
        private System.Windows.Forms.Label lblFiles;
        private System.Windows.Forms.ToolStripMenuItem tsmiFileNew;
        private System.Windows.Forms.ToolStripMenuItem tsmiFileClose;
        private System.Windows.Forms.Label lblExtraInfoVal;
        private System.Windows.Forms.Label lblExtraInfo;
        private System.Windows.Forms.ToolStripMenuItem tsmiArchiveInsertAs;
        private System.Windows.Forms.Label lblArchiveName;
        private System.Windows.Forms.Label lblArchiveNameVal;
        private System.Windows.Forms.Label lblEntryExtraInfo;
        private System.Windows.Forms.Label lblEntryExtraInfoVal;
        private System.Windows.Forms.Label lblIsDirectory;
        private System.Windows.Forms.Label lblDateStamp;
        private System.Windows.Forms.Label lblDateStampVal;
        private System.Windows.Forms.Label lblIsDirectoryVal;
    }
}

