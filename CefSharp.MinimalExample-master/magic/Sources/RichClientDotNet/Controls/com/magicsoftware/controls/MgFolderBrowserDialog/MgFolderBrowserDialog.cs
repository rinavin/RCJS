using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;
using System.Windows.Forms;
using com.magicsoftware.win32;

namespace com.magicsoftware.controls
{
   /// <summary>
   /// A dialog to browse for folders that wraps the unmanaged SHBrowseForFolder function.
   /// This is with the main differences being that it does something to address the bug in Win 7 (and above?) whereby the SHBrowseForFolder dialog won't
   /// always scroll to ensure visibility of the current selection.
   /// This class can not be inherited.
   /// </summary>
   [DefaultProperty("SelectedPath")]
   public sealed class MgFolderBrowserDialog : CommonDialog
   {
      #region Fields/Properties

      private NativeWindowCommon.BrowseCallbackProc callback;
      private string descriptionText;
      private Environment.SpecialFolder rootFolder;
      private string selectedPath;
      private bool selectedPathNeedsCheck;
      private bool showNewFolderButton;
      private bool doneScrolling;

      private static readonly int MAX_PATH = 260;

      /// <summary>
      /// Gets or sets the descriptive text displayed above the tree view control in the dialog box.
      /// </summary>
      public string Description
      {
         get
         {
            return this.descriptionText;
         }
         set
         {
            this.descriptionText = (value == null) ? string.Empty : value;
         }
      }
      /// <summary>
      /// Gets or sets the root node of the tree view selected by the user.
      /// </summary>
      public Environment.SpecialFolder RootFolder
      {
         get
         {
            return this.rootFolder;
         }
         set
         {
            if (!Enum.IsDefined(typeof(Environment.SpecialFolder), value))
            {
               throw new InvalidEnumArgumentException("value", (int)value, typeof(Environment.SpecialFolder));
            }
            this.rootFolder = value;
         }
      }

      /// <summary>
      /// Gets or sets the path selected by the user.
      /// </summary>
      public string SelectedPath
      {
         get
         {
            if (((this.selectedPath != null) && (this.selectedPath.Length != 0)) && this.selectedPathNeedsCheck)
            {
               new FileIOPermission(FileIOPermissionAccess.PathDiscovery, this.selectedPath).Demand();
            }
            return this.selectedPath;
         }
         set
         {
            this.selectedPath = (value == null) ? string.Empty : value;
            this.selectedPathNeedsCheck = false;
         }
      }

      /// <summary>
      /// Gets or sets whether to include the New Folder button in the dialog box.
      /// </summary>
      public bool ShowNewFolderButton
      {
         get
         {
            return this.showNewFolderButton;
         }
         set
         {
            this.showNewFolderButton = value;
         }
      }

      #endregion

      #region Constructor

      /// <summary>
      /// Constructor
      /// </summary>
      public MgFolderBrowserDialog()
      {
         this.Reset();
      }

      #endregion


      #region Helper methods
      /// <summary>
      /// Helper function that returns the IMalloc interface used by the shell.
      /// </summary>
      private static NativeWindowCommon.IMalloc GetSHMalloc()
      {
         NativeWindowCommon.IMalloc malloc;
         NativeWindowCommon.SHGetMalloc(out malloc);
         return malloc;
      }

      /// <summary>
      /// Creates flags for BROWSEINFO.ulFlags based on the values of boolean member properties.
      /// </summary>
      private int GetFlags()
      {
         int ret_val = (int)NativeWindowCommon.BffStyles.NewDialogStyle;
         if (!this.showNewFolderButton)
            ret_val |= (int)NativeWindowCommon.BffStyles.NoNewFolderButton;
         return ret_val;
      }

      /// <summary>
      /// Reset the fields to their default value.
      /// </summary>
      public override void Reset()
      {
         this.rootFolder = Environment.SpecialFolder.Desktop;
         this.descriptionText = string.Empty;
         this.selectedPath = string.Empty;
         this.selectedPathNeedsCheck = false;
         this.showNewFolderButton = true;
      }

      /// <summary>
      /// Shows the folder browser dialog box with the specified owner window.
      /// </summary>
      protected override bool RunDialog(IntPtr hWndOwner)
      {
         IntPtr pidlRoot = IntPtr.Zero;
         bool flag = false;

         // Get the IDL for the specific startLocation. e.g. Environment.SpecialFolder.Desktop
         NativeWindowCommon.SHGetSpecialFolderLocation(hWndOwner, (int)this.rootFolder, ref pidlRoot);

         if (pidlRoot == IntPtr.Zero)
         {
            // Get the IDL for the default startLocation.
            NativeWindowCommon.SHGetSpecialFolderLocation(hWndOwner, 0, ref pidlRoot);
            if (pidlRoot == IntPtr.Zero)
            {
               throw new InvalidOperationException();
            }
         }

         int flags = GetFlags();

         if ((flags & (int)NativeWindowCommon.BffStyles.NewDialogStyle) != 0)
         {
            if (System.Threading.ApartmentState.MTA == Application.OleRequired())
               flags = flags & (~(int)NativeWindowCommon.BffStyles.NewDialogStyle);
         }

         IntPtr pidl = IntPtr.Zero;
         IntPtr hglobal = IntPtr.Zero;

         try
         {
            hglobal = Marshal.AllocHGlobal((int)(MAX_PATH * Marshal.SystemDefaultCharSize));

            this.callback = new NativeWindowCommon.BrowseCallbackProc(this.FolderBrowserDialog_BrowseCallbackProc);
            NativeWindowCommon.BROWSEINFO lpbi = new NativeWindowCommon.BROWSEINFO
            {
               pidlRoot = pidlRoot,
               hwndOwner = hWndOwner,
               pszDisplayName = hglobal,
               lpszTitle = this.descriptionText,
               ulFlags = flags,
               lpfn = this.callback,
               lParam = IntPtr.Zero,
               iImage = 0
            };

            // Show the dialog.
            pidl = NativeWindowCommon.SHBrowseForFolder(ref lpbi);

            if (pidl != IntPtr.Zero)
            {
               // Retrieve the path from the IDList.
               StringBuilder sb = new StringBuilder(MAX_PATH * Marshal.SystemDefaultCharSize);

               NativeWindowCommon.SHGetPathFromIDList(pidl, sb);
               this.selectedPathNeedsCheck = true;
               this.selectedPath = sb.ToString();
               flag = true;
            }
         }
         finally
         {
            NativeWindowCommon.IMalloc malloc = GetSHMalloc();

            //Free the memory allocated for getting the ID list for the specific startLocation.
            malloc.Free(pidlRoot);

            if (pidl != IntPtr.Zero)
            {
               //Free the memory allocated for getting the ID list for the showing the SHBrowseForFolder.
               malloc.Free(pidl);
            }

            // Free the buffer you've allocated on the global heap.
            if (hglobal != IntPtr.Zero)
            {
               Marshal.FreeHGlobal(hglobal);
            }

            //Reset the callback function to null.
            this.callback = null;
         }

         return flag;
      }

      #endregion

      #region Callback method

      /// <summary>
      /// Callback method for the handling of messages to be sent to/from the folder browser dialog.
      /// </summary>
      /// <param name="hwnd"></param>
      /// <param name="msg"></param>
      /// <param name="lParam"></param>
      /// <param name="lpData"></param>
      /// <returns></returns>
      private int FolderBrowserDialog_BrowseCallbackProc(IntPtr hwnd, int msg, IntPtr lParam, IntPtr lpData)
      {
         switch (msg)
         {
            case NativeWindowCommon.BFFM_INITIALIZED:
               // We get in here when the dialog is first displayed. If an initial directory
               // has been specified we will make this the selection now, and also make sure
               // that directory is expanded.
               if (this.selectedPath.Length != 0)
               {
                  NativeWindowCommon.SendMessage(hwnd, NativeWindowCommon.BFFM_SETSELECTIONW, 1, this.selectedPath);
               }
               break;

            case NativeWindowCommon.BFFM_SELCHANGED:
               {
                  IntPtr pidl = lParam;
                  if (pidl != IntPtr.Zero)
                  {
                     StringBuilder sb = new StringBuilder(MAX_PATH * Marshal.SystemDefaultCharSize);
                     bool flag = NativeWindowCommon.SHGetPathFromIDList(pidl, sb);
                     NativeWindowCommon.SendMessage(hwnd, NativeWindowCommon.BFFM_ENABLEOK, 0, flag ? 1 : 0);
                  }

                  if (!doneScrolling)
                  {
                     IntPtr hbrowse = NativeWindowCommon.FindWindowEx(hwnd, IntPtr.Zero, "SHBrowseForFolder ShellNameSpace Control", null);
                     IntPtr htree = NativeWindowCommon.FindWindowEx(hbrowse, IntPtr.Zero, "SysTreeView32", null);
                     IntPtr htis = NativeHeader.SendMessage(htree, NativeWindowCommon.TVM_GETNEXTITEM, NativeWindowCommon.TVGN_CARET, IntPtr.Zero);

                     IntPtr htir = NativeHeader.SendMessage(htree, NativeWindowCommon.TVM_GETNEXTITEM, NativeWindowCommon.TVGN_ROOT, IntPtr.Zero);
                     IntPtr htic = NativeHeader.SendMessage(htree, NativeWindowCommon.TVM_GETNEXTITEM, NativeWindowCommon.TVGN_CHILD, htir);

                     //we actually go into the BFFM_SELCHANGED case three times when the dialog is first shown, and it's only on the third one that we actually scroll, 
                     //since before this htic is always zero, so you need to cope with this 
                     //- i.e. don't set 'doneScrolling' flag until htic has been non - zero(or until you've gone into the BFFM_SELCHANGED three times).
                     if ((int)htic != 0)
                     {
                        doneScrolling = true;
                        //The only solution I've found that works consistently is to send a TVM_ENSUREVISIBLE to the tree, using TVM_GETNEXTITEM/TVGN_CARET, from BFFM_SELCHANGED.
                        //That works. Every time. 
                        NativeHeader.SendMessage(htree, NativeWindowCommon.TVM_ENSUREVISIBLE, 0, htis);

                        //Active the tree view control window inside the folder browser dialog to set the focus on it.
                        NativeWindowCommon.SendMessage(htree, NativeWindowCommon.WM_ACTIVATE, 1, 0);
                     }
                  }
               }
               break;
         }
         return 0;
      }

      #endregion

   }
}
