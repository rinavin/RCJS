using System;
using System.Windows.Forms;
using System.Drawing;

namespace com.magicsoftware.unipaas.gui.low
{
   /// <summary>
   /// Responsible for handling print Preview commands
   /// </summary>
   internal sealed class PrintPreview
   {
      #region fields

      PrintPreviewData _printPreviewData;
      CommandType _commandType;
      Int64 _contextID;

      enum CommandType
      {
         PRINTPREVIEW_START,
         PRINTPREVIEW_UPDATE,
         CREATE_RICH_WINDOW,
         PRINTPREVIEW_CLOSE,
         CREATE_WINDOW,
         PRINTPREVIEW_SHOW,
         SHOW_PRINT_DIALOG,
         ACTIVATE_PRINT_PREVIEW,
         DESTROY_WINDOW,
         PRINTPREVIEW_GETACTIVEFORM,
         PRINTPREVIEW_SET_CURSOR
      }

      internal delegate void PrintPreviewDelegate();

      #endregion

      #region Ctors

      public PrintPreview()
      {
         _contextID = Manager.GetCurrentContextID();
      }

      #endregion

      /// <summary>
      /// Set cursor for Print Preview Form
      /// </summary>
      /// <param name="printPreviewDataPtr"></param>
      internal void SetCursor(IntPtr printPreviewDataPtr)
      {
         _commandType = CommandType.PRINTPREVIEW_SET_CURSOR;
         _printPreviewData = new PrintPreviewData();
         _printPreviewData.intPtr1 = printPreviewDataPtr;

         GUIMain.getInstance().invoke(new PrintPreviewDelegate(Run));
      }

      /// <summary>
      /// Creates Print Preview Form
      /// </summary>
      /// <param name="contextId">context id</param>
      /// <param name="ioPtr">pointer to current IORT object</param>
      /// <param name="copies">number of copies</param>
      /// <param name="enablePDlg">indicates whether to enable Print dialog</param>
      /// <param name="callerWindow">caller window of Print Preview</param>
      internal void Start(Int64 contextID, IntPtr ioPtr, int copies, bool enablePDlg, Form callerWindow)
      {
         _commandType = CommandType.PRINTPREVIEW_START;
         _printPreviewData = new PrintPreviewData();
         _printPreviewData.Int64Val = contextID;
         _printPreviewData.intPtr1 = ioPtr;
         _printPreviewData.boolVal = enablePDlg;
         _printPreviewData.number1 = copies;
         _printPreviewData.callerWindow = callerWindow;

         GUIMain.getInstance().invoke(new PrintPreviewDelegate(Run));
      }

      /// <summary>
      /// Update Print Preview
      /// </summary>
      /// <param name="prnPrevData">print preview data</param>
      internal void Update(IntPtr prnPrevData)
      {
         _commandType = CommandType.PRINTPREVIEW_UPDATE;
         _printPreviewData = new PrintPreviewData();
         _printPreviewData.intPtr1 = prnPrevData;
         GUIMain.getInstance().invoke(new PrintPreviewDelegate(Run));
      }

      /// <summary>
      /// Create Rich Edit
      /// </summary>
      /// <param name="contextId"></param>
      /// <param name="ctrlPtr"></param>
      /// <param name="prmPtr"></param>
      /// <param name="style"></param>
      /// <param name="dwExStyle"></param>
      internal void createRichWindow(Int64 contextID, IntPtr ctrlPtr, IntPtr prmPtr, uint style, uint dwExStyle)
      {
         _commandType = CommandType.CREATE_RICH_WINDOW;
         _printPreviewData = new PrintPreviewData();
         _printPreviewData.Int64Val = contextID;
         _printPreviewData.intPtr1 = ctrlPtr;
         _printPreviewData.intPtr2 = prmPtr;
         _printPreviewData.style = (int)style;
         _printPreviewData.number1 = (int)dwExStyle;
         GUIMain.getInstance().invoke(new PrintPreviewDelegate(Run));
      }

      /// <summary>
      /// Closes Print Preview form
      /// </summary>
      /// <param name="hWnd"> Print Preview data</param>
      /// <param name="hWnd"> Handle of Print Preview form</param>
      internal void Close(IntPtr printPreviewData, IntPtr hWnd)
      {
         _commandType = CommandType.PRINTPREVIEW_CLOSE;
         _printPreviewData = new PrintPreviewData();
         _printPreviewData.intPtr1 = hWnd;
         _printPreviewData.intPtr2 = printPreviewData;
         GUIMain.getInstance().invoke(new PrintPreviewDelegate(Run));
      }

      /// <summary>
      /// Returns active form
      /// </summary>
      /// <returns></returns>
      internal IntPtr GetActiveForm()
      {
         _commandType = CommandType.PRINTPREVIEW_GETACTIVEFORM;
         _printPreviewData = new PrintPreviewData();
         GUIMain.getInstance().invoke(new PrintPreviewDelegate(Run));
         return _printPreviewData.intPtr1;
      }

      ///<summary>
      ///  Creates a window using win32 API from GUI Thread.
      ///</summary>
      ///<param name="exStyle">!!.</param>
      ///<param name="className">!!.</param>
      ///<param name="windowName">!!.</param>
      ///<param name="style">!!.</param>
      ///<param name="x">!!.</param>
      ///<param name="y">!!.</param>
      ///<param name="width">!!.</param>
      ///<param name="height">!!.</param>
      ///<param name="hwndParent">!!.</param>
      ///<param name="hMenu">!!.</param>
      ///<param name="hInstance">!!.</param>
      ///<param name="lParam">!!.</param>
      ///<returns>handle of window</returns>
      internal IntPtr CreateGuiWindow(uint exStyle, String className, String windowName, uint style, int x, int y, int width, int height,
                                       IntPtr hwndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lParam)
      {
         _commandType = CommandType.CREATE_WINDOW;
         _printPreviewData = new PrintPreviewData();
         _printPreviewData.extendedStyle = (int)exStyle;
         _printPreviewData.style = (int)style;
         _printPreviewData.className = className;
         _printPreviewData.windowName = windowName;
         _printPreviewData.number1 = x;
         _printPreviewData.number1 = y;
         _printPreviewData.number1 = width;
         _printPreviewData.number1 = height;
         _printPreviewData.intPtr1 = hwndParent;
         _printPreviewData.intPtr2 = hMenu;
         _printPreviewData.intPtr3 = hInstance;
         _printPreviewData.intPtr4 = lParam;

         GUIMain.getInstance().invoke(new PrintPreviewDelegate(Run));
         return _printPreviewData.intPtr1;
      }

      /// <summary>
      /// Destroys a window
      /// </summary>
      /// <param name="hWndPtr">handle of window</param>
      internal void DestroyGuiWindow(IntPtr hWndPtr)
      {
         _commandType = CommandType.DESTROY_WINDOW;
         _printPreviewData = new PrintPreviewData();
         _printPreviewData.intPtr1 = hWndPtr;
         GUIMain.getInstance().invoke(new PrintPreviewDelegate(Run));
      }

      /// <summary>
      ///  Shows Print Preview Form
      /// </summary>
      /// <param name="hWnd"> Handle of Print Preview form</param>
      internal void Show(IntPtr hWnd)
      {
         _commandType = CommandType.PRINTPREVIEW_SHOW;
         _printPreviewData = new PrintPreviewData();
         _printPreviewData.intPtr1 = hWnd;
         GUIMain.getInstance().invoke(new PrintPreviewDelegate(Run));
      }

      /// <summary>
      /// Show Print Dialog
      /// </summary>
      /// <param name="prnPrevData">print dialog info</param>
      internal int showPrintDialog(IntPtr gpd)
      {
         _commandType = CommandType.SHOW_PRINT_DIALOG;
         _printPreviewData = new PrintPreviewData();
         _printPreviewData.intPtr1 = gpd;
         GUIMain.getInstance().invoke(new PrintPreviewDelegate(Run));
         return _printPreviewData.number1;
      }

      /// <summary>
      /// Activates Print preview form
      /// </summary>
      internal void Activate()
      {
         _commandType = CommandType.ACTIVATE_PRINT_PREVIEW;
         GUIMain.getInstance().invoke(new PrintPreviewDelegate(Run));
      }

      /// <summary>
      /// Methods for various commands are called synchronously here
      /// </summary>
      internal void Run()
      {
         GuiCommandQueue.getInstance().Run();

         var contextIDGuard = new Manager.ContextIDGuard(_contextID);

         try
         {
            switch (_commandType)
            {
               case CommandType.PRINTPREVIEW_SET_CURSOR:
                  onSetCursor();
                  break;

               case CommandType.PRINTPREVIEW_START:
                  onStart();
                  break;

               case CommandType.PRINTPREVIEW_UPDATE:
                  onUpdate();
                  break;

               case CommandType.CREATE_RICH_WINDOW:
                  onCreateRichWindow();
                  break;

               case CommandType.PRINTPREVIEW_CLOSE:
                  onClose();
                  break;

               case CommandType.PRINTPREVIEW_GETACTIVEFORM:
                  onGetActiveForm();
                  break;

               case CommandType.CREATE_WINDOW:
                  onCreateWindow();
                  break;

               case CommandType.DESTROY_WINDOW:
                  onDestroyWindow();
                  break;

               case CommandType.PRINTPREVIEW_SHOW:
                  onShow();
                  break;

               case CommandType.SHOW_PRINT_DIALOG:
                  onShowPrintDialog();
                  break;
               case CommandType.ACTIVATE_PRINT_PREVIEW:
                  OnActivatePrintPreview();
                  break;
            }
         }
         finally
         {
            contextIDGuard.Dispose(); // Reset the current contextID.
         }

      }

      /// <summary>
      /// Sets cursor for Print Preview Form
      /// </summary>
      void onSetCursor()
      {
         Events.OnPrintPreviewSetCursor(_printPreviewData.intPtr1);
      }

      /// <summary>
      /// Creates Print Preview Form
      /// </summary>
      void onStart()
      {
         Form printPreviewForm = new Form();

         if (_printPreviewData.callerWindow != null)
         {
            Rectangle rect = Rectangle.Empty;
            Point location = _printPreviewData.callerWindow.Location;
            if (_printPreviewData.callerWindow.Parent != null)
            {
               // if caller window is MDI Child, convert to screen co-ordinates
               location = _printPreviewData.callerWindow.Parent.PointToScreen(location);
            }
            rect = new Rectangle(location, _printPreviewData.callerWindow.Size);

            if (_printPreviewData.callerWindow.WindowState != FormWindowState.Maximized)
            {
               printPreviewForm.StartPosition = FormStartPosition.Manual;
               printPreviewForm.Bounds = rect;
            }
         }

         IntPtr ppHwnd = printPreviewForm.Handle;
         Events.OnPrintPreviewStart(_printPreviewData.Int64Val, _printPreviewData.intPtr1, _printPreviewData.number1, _printPreviewData.boolVal, ppHwnd);

         // Defect 141583: Print Preview window was shown as per MDI FramWindow in unipaas 1.9 and not based on caller or runtime window.
         // Now, it will be seen as per state of MDI if exists or caller window if it exists or will be shown maximized.
         if (_printPreviewData.callerWindow != null)
            printPreviewForm.WindowState = _printPreviewData.callerWindow.WindowState;
         else
            printPreviewForm.WindowState = FormWindowState.Maximized;
      }

      /// <summary>
      /// Update Print Preview
      /// </summary>
      void onUpdate()
      {
         Events.OnPrintPreviewUpdate(_printPreviewData.intPtr1);
      }

      /// <summary>
      /// Create Rich Edit
      /// </summary>
      void onCreateRichWindow()
      {
         Events.OnCreateRichWindow(_printPreviewData.Int64Val, _printPreviewData.intPtr1, _printPreviewData.intPtr2,
                                   (uint)_printPreviewData.style, (uint)_printPreviewData.number1);
      }

      /// <summary>
      /// Show Print Dialog
      /// </summary>
      void onShowPrintDialog()
      {
         _printPreviewData.number1 = Events.OnShowPrintDialog(_printPreviewData.intPtr1);
      }

      /// <summary>
      /// Closes Print Preview form
      /// </summary>
      void onClose()
      {
#if !PocketPC
         Events.OnPrintPreviewClose(_printPreviewData.intPtr2);
         Control control = Control.FromHandle(_printPreviewData.intPtr1);
         if (control is Form)
            ((Form)control).Dispose();
         PrintPreviewFocusManager.GetInstance().ShouldPrintPreviewBeFocused = false;
         PrintPreviewFocusManager.GetInstance().PrintPreviewFormHandle = IntPtr.Zero;
#endif
      }

      /// <summary>
      /// Returns active form
      /// </summary>
      void onGetActiveForm()
      {
         _printPreviewData.intPtr1 = Form.ActiveForm != null ? Form.ActiveForm.Handle : IntPtr.Zero;
      }

      /// <summary>
      /// Creates a window
      /// </summary>
      void onCreateWindow()
      {
         _printPreviewData.intPtr1 = Events.OnCreateGuiWindow((uint)_printPreviewData.extendedStyle, _printPreviewData.className, _printPreviewData.windowName, (uint)_printPreviewData.style, 
                                                              _printPreviewData.number1, _printPreviewData.number2, _printPreviewData.number3, _printPreviewData.number4, 
                                                              _printPreviewData.intPtr1, _printPreviewData.intPtr2, _printPreviewData.intPtr3, _printPreviewData.intPtr4);
      }

      /// <summary>
      /// Destroys a window
      /// </summary>
      void onDestroyWindow()
      {
         Events.OnDestroyGuiWindow(_printPreviewData.intPtr1);
      }

      /// <summary>
      ///  Shows Print Preview Form
      /// </summary>
      void onShow()
      {
#if !PocketPC
         Control control = Control.FromHandle(_printPreviewData.intPtr1);
         if (control is Form)
         {
            // Save last print preview form to be activated when task aborts. This is later used to activate the form.
            PrintPreviewFocusManager.GetInstance().PrintPreviewFormHandle = _printPreviewData.intPtr1;
            PrintPreviewFocusManager.GetInstance().ShouldPrintPreviewBeFocused = true;
            ((Form)control).Show();
            ((Form)control).Focus();
         }
#endif
      }

      /// <summary>
      /// Activates Print Preview form
      /// </summary>
      void OnActivatePrintPreview()
      {
#if !PocketPC
         Form printPreviewForm = (Form)Control.FromHandle(PrintPreviewFocusManager.GetInstance().PrintPreviewFormHandle);
         printPreviewForm.Activate();
#endif
      }

   }
}
