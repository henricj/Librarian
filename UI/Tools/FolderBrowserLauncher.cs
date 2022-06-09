using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Nyerguds.Util.UI
{
    public static class FolderBrowserLauncher
    {
        /// <summary>
        /// Using title text to look for the top level dialog window is fragile.
        /// In particular, this will fail in non-English applications.
        /// </summary>
        const string _topLevelSearchString = "Browse For Folder";

        /// <summary>
        /// These should be more robust.  We find the correct child controls in the dialog
        /// by using the GetDlgItem method, rather than the FindWindow(Ex) method,
        /// because the dialog item IDs should be constant.
        /// </summary>
        const int _dlgItemBrowseControl = 0;
        const int _dlgItemTreeView = 100;

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        static extern IntPtr GetDlgItem(IntPtr hDlg, int nIDDlgItem);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// Some of the messages that the Tree View control will respond to
        /// </summary>
        const int TV_FIRST = 0x1100;

        const int TVM_SELECTITEM = (TV_FIRST + 11);
        const int TVM_GETNEXTITEM = (TV_FIRST + 10);
        const int TVM_GETITEM = (TV_FIRST + 12);
        const int TVM_ENSUREVISIBLE = (TV_FIRST + 20);

        /// <summary>
        /// Constants used to identity specific items in the Tree View control
        /// </summary>
        const int TVGN_ROOT = 0x0;

        const int TVGN_NEXT = 0x1;
        const int TVGN_CHILD = 0x4;
        const int TVGN_FIRSTVISIBLE = 0x5;
        const int TVGN_NEXTVISIBLE = 0x6;
        const int TVGN_CARET = 0x9;


        /// <summary>
        /// Calling this method is identical to calling the ShowDialog method of the provided
        /// FolderBrowserDialog, except that an attempt will be made to scroll the Tree View
        /// to make the currently selected folder visible in the dialog window.
        /// </summary>
        /// <param name="dlg"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        public static DialogResult ShowFolderBrowser(FolderBrowserDialog dlg, bool selectTree, IWin32Window parent = null)
        {
            var result = DialogResult.Cancel;
            var retries = 10;

            using var t = new Timer();
            t.Tick += (s, a) =>
            {
                if (retries > 0)
                {
                    --retries;
                    var hwndDlg = FindWindow(null, _topLevelSearchString);
                    if (hwndDlg != IntPtr.Zero)
                    {
                        var hwndFolderCtrl = GetDlgItem(hwndDlg, _dlgItemBrowseControl);
                        if (hwndFolderCtrl != IntPtr.Zero)
                        {
                            var hwndTV = GetDlgItem(hwndFolderCtrl, _dlgItemTreeView);

                            if (hwndTV != IntPtr.Zero)
                            {
                                var item = SendMessage(hwndTV, TVM_GETNEXTITEM, new IntPtr(TVGN_CARET), IntPtr.Zero);
                                if (item != IntPtr.Zero)
                                {
                                    SendMessage(hwndTV, TVM_ENSUREVISIBLE, IntPtr.Zero, item);
                                    retries = 0;
                                    t.Stop();
                                    if (selectTree)
                                        SendKeys.Send("{TAB}{TAB}");

                                }
                            }
                        }
                    }
                }

                else
                {
                    //
                    //  We failed to find the Tree View control.
                    //
                    //  As a fall back (and this is an UberUgly hack), we will send
                    //  some fake keystrokes to the application in an attempt to force
                    //  the Tree View to scroll to the selected item.
                    //
                    t.Stop();
                    SendKeys.Send("{TAB}{TAB}{DOWN}{DOWN}{UP}{UP}");
                }
            };
            t.Interval = 10;
            t.Start();
            result = dlg.ShowDialog(parent);
            return result;
        }
    }
}
