using System;
using System.Windows.Forms;
using com.magicsoftware.win32;
using System.Text;
using System.Reflection;
using System.Runtime.InteropServices;
using com.magicsoftware.richclient.mobile.util;
using Microsoft.WindowsCE.Forms;
using com.magicsoftware.unipaas.gui.low;
using System.Drawing;
using com.magicsoftware.unipaas.gui;
using com.magicsoftware.richclient.gui;
using util.com.magicsoftware.util;

namespace com.magicsoftware.richclient.mobile.gui
{
   internal partial class MainForm : Form
   {
#if !RCMobile_CF35
      internal Boolean IsDisposed 
      {
         get
         {
            System.Console.WriteLine("MainForm.IsDisposed() - not implemented (returns 'false')");
            return false;
         }
         set
         {
            System.Console.WriteLine(string.Format("MainForm.IsDisposed({0}) - not implemented", value));
         }
      }
#endif

      /* On .NetCF, only one instance of an application can run. This single instance behavior is achieved by 
       * reactivating a special window that belongs to the application. The developer is unaware of this window, 
       * and the window can not be accessed via other windows of the application. When we run in "hidden" mode, 
       * we need to know when the application is activated, in order to allow the worker thread to continue 
       * execution.
       * So, when we run in "hidden" mode, there are 2 window subclassing operations we need to perform. First, 
       * we need to subclass our initial form's window, so we can keep hiding it whenever the system tries to 
       * activate it. Secondly, we need to subclass the special window so we can intercept the activation message
       * and release the lock of the worker thread. This special window is found by enumerating windows and looking 
       * for the one with the specific class name, which is composed from a constant string and the executable name.
       */

      internal MainForm()
      {
         try
         {
            _inputPanel = new InputPanel();
            _inputPanel.EnabledChanged += new EventHandler(inputPanel_EnabledChanged);
         }
         catch { }

         subclassTopMostWindow();

         if (ClientManager.Instance.IsHidden)
         {
            // If we are hidden, lock the wait object and subclass the windows
            Monitor.Enter(ClientManager.Instance.getWaitHiddenObject());
            subclassThisForm(Handle);
         }
         InitializeComponent();
      }

      /// <summary>Set startup image for mobile. If there is custom startup image specifies
      /// in execution properties then that image will be used, otherwise use default image
      /// will be used from resources./// </summary>
      internal void SetStartupImage(String startupImageFileName)
      {
         System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));

         if (!String.IsNullOrEmpty(startupImageFileName))
         {
            try
            {
               this.pictureBox1.Image = new Bitmap(startupImageFileName);
            }
            catch
            {
               Logger.Instance.WriteErrorToLog("Failed to create image from file " + startupImageFileName);         
            }
         }
         if (this.pictureBox1.Image == null)
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
      }

      /// <summary> Catch the enable/disable of the soft keyboard. When the keyboard state changes,
      /// resize the active form and scroll to the control in focus
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      void inputPanel_EnabledChanged(object sender, EventArgs e)
      {
         // Get the active form
         ClientManager cm = ClientManager.Instance;
         tasks.Task task = cm.getLastFocusedTask();
         if (task == null)
            return;
         MgForm mgForm = (MgForm)cm.getLastFocusedTask().getForm();
         if (mgForm == null)
            return;
         mgForm = (MgForm)mgForm.getTopMostForm();
         Form form = GuiCommandQueue.getInstance().mgFormToForm(mgForm);

         // If form size changed
         if (form != null && form.Bounds != _inputPanel.VisibleDesktop)
         {
            Size changeFormSizeTo;

            if (_inputPanel.Enabled || _previousSize == null)
            {
               // remember the form's size before the soft keyboard was opened
               _previousSize = form.Size;
               changeFormSizeTo = _inputPanel.VisibleDesktop.Size;
            }
            else
            {
               // use the size from before the soft keyboard was opened - the VisibleDesktop ignores
               // the menu bar size
               changeFormSizeTo = _previousSize;
            }

            // Set the new size. The .Net way of changing the bounds does not work in this case,
            // so lets do it via win32 functions
            NativeWindowCommon.SetWindowPos(form.Handle, IntPtr.Zero,
               0, 0, changeFormSizeTo.Width, changeFormSizeTo.Height,
               NativeWindowCommon.SWP_NOMOVE | NativeWindowCommon.SWP_NOZORDER);
            
            // Find the control in focus and scroll to it
            MapData mapData = ((TagData)form.Tag).LastFocusedMapData;
            GuiMgControl guiMgcontrol = mapData.getControl();
            if (guiMgcontrol == null)
               return;
            object o = ControlsMap.getInstance().object2Widget(guiMgcontrol, mapData.getIdx());
            Control control = null;
            LogicalControl logicalControl = null;

            if (o is LogicalControl)
            {
               logicalControl = (LogicalControl)o;
               control = logicalControl.getEditorControl();
            }
            else
               control = (Control)o;

            GuiUtils.scrollToControl(control , logicalControl);
         }
      }

      /// <summary> If the soft keyboard is open, close it
      /// </summary>
      internal void closeSoftKeyboard()
      {
         if (_inputPanel != null && _inputPanel.Enabled)
            _inputPanel.Enabled = false;
      }

      internal void CreateControl()
      {
         System.Console.WriteLine("CreateControl()");
      }

      private InputPanel _inputPanel;
      private Size _previousSize;     // form size before the soft keyboard opened

      #region special window
      internal Boolean NeedResume { private get; set; }

      /// <summary> 
      /// Subclass the window the system talks to
      /// </summary>
      internal void subclassTopMostWindow()
      {
         NativeWindowCommon.EnumWindows(EnumWindowsProc, 0);
      }

      /// <summary> Look for this application's window, and subclass it
      /// 
      /// </summary>
      /// <param name="hwnd"></param>
      /// <param name="lParam"></param>
      /// <returns></returns>
      private int EnumWindowsProc(IntPtr hwnd, int lParam)
      {
         StringBuilder classNameBuilder = new StringBuilder(256);

         // Get the window's class name
         NativeWindowCommon.GetClassName(hwnd, classNameBuilder, classNameBuilder.Capacity);
         String className = classNameBuilder.ToString();
         
         // If it is our mystery window, subclass it and stop searching
         if (className == "#NETCF_AGL_PARK_" + Assembly.GetExecutingAssembly().ManifestModule.FullyQualifiedName)
         {
            subclassWindow(hwnd);
            return 0;
         }
         return 1;
      }

      // A delegate for our wndproc
      internal delegate int MobileWndProc(IntPtr hwnd, uint msg, uint wParam, int lParam);

      private IntPtr _origWndProc; // Original wndproc
      private MobileWndProc _proc; // object's delegate

      /// <summary> subclass the window
      /// </summary>
      void subclassWindow(IntPtr hwnd)
      {
         _proc = new MobileWndProc(WindowProc);
         _origWndProc = (IntPtr)NativeWindowCommon.SetWindowLong(hwnd, NativeWindowCommon.GWL_WNDPROC,
                         Marshal.GetFunctionPointerForDelegate(_proc).ToInt32());
      }

      // Our wndproc
      private int WindowProc(IntPtr hwnd, uint msg, uint wParam, int lParam)
      {
         if (msg == 0x8001 && ClientManager.Instance.IsHidden) // Special activation message
         {
            Monitor.Exit(ClientManager.Instance.getWaitHiddenObject());
         }

         int ret = NativeWindowCommon.CallWindowProc(_origWndProc, hwnd, msg, wParam, lParam);

         // If we hid all forms and hibernated the context, we need to resume the context and show the forms
         if (msg == 0x8001 && NeedResume)
            GUIManager.Instance.restoreHiddenForms();

         return ret;
      }
      #endregion

      #region form window

      private IntPtr _origFormWndProc; // Original wndproc
      private MobileWndProc _formProc; // object's delegate

      /// <summary>
      ///  subclass the window
      /// </summary>
      void subclassThisForm(IntPtr hwnd)
      {
         _formProc = new MobileWndProc(formWindowProc);
         _origFormWndProc = (IntPtr)NativeWindowCommon.SetWindowLong(hwnd, NativeWindowCommon.GWL_WNDPROC,
                         Marshal.GetFunctionPointerForDelegate(_formProc).ToInt32());
      }

      // Our wndproc
      private int formWindowProc(IntPtr hwnd, uint msg, uint wParam, int lParam)
      {
         // Do not activate this form - leave it hidden
         if (msg == NativeWindowCommon.WM_ACTIVATE)
         {
            Hide();
            return 0;
         }

         return NativeWindowCommon.CallWindowProc(_origFormWndProc, hwnd, msg, wParam, lParam);
      }
      #endregion
   }
}
