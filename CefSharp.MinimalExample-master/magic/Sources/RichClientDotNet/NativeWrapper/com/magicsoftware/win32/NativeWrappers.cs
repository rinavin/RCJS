////////////////////////////////////////////////////////////////////////////////////
//  File:   NativeWrappers.cs
//  Author: Sergei Pavlovsky
//
//  Copyright (c) 2004 by Sergei Pavlovsky (sergei_vp@hotmail.com, sergei_vp@ukr.net)
//
//	This file is provided "as is" with no expressed or implied warranty.
//	The author accepts no liability if it causes any damage whatsoever.
// 
//  This code is free and may be used in any way you desire. If the source code in 
//  this file is used in any commercial application then a simple email would be 
//	nice.
////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.IO;
using System.Drawing;
using System.Text;
#if !PocketPC
using Microsoft.Win32.SafeHandles;
#else
using SafeFileHandle=System.IntPtr;
#endif

namespace com.magicsoftware.win32
{
   /// <summary>
   /// InitCommonControlsHelper class
   /// </summary>
   public class InitCommonControlsHelper
   {
      /// <summary>
      /// Constants: Platform
      /// </summary>
      const int ICC_LISTVIEW_CLASSES = 0x00000001;
      const int ICC_TREEVIEW_CLASSES = 0x00000002;
      const int ICC_BAR_CLASSES = 0x00000004;
      const int ICC_TAB_CLASSES = 0x00000008;
      const int ICC_UPDOWN_CLASS = 0x00000010;
      const int ICC_PROGRESS_CLASS = 0x00000020;
      const int ICC_HOTKEY_CLASS = 0x00000040;
      const int ICC_ANIMATE_CLASS = 0x00000080;
      const int ICC_WIN95_CLASSES = 0x000000FF;
      const int ICC_DATE_CLASSES = 0x00000100;
      const int ICC_USEREX_CLASSES = 0x00000200;
      const int ICC_COOL_CLASSES = 0x00000400;
      // IE 4.0
      const int ICC_INTERNET_CLASSES = 0x00000800;
      const int ICC_PAGESCROLLER_CLASS = 0x00001000;
      const int ICC_NATIVEFNTCTL_CLASS = 0x00002000;
      // WIN XP
      const int ICC_STANDARD_CLASSES = 0x00004000;
      const int ICC_LINK_CLASS = 0x00008000;

      /// <summary>
      /// Types
      /// </summary>
      [Flags]
      public enum Classes : int
      {
         ListView = ICC_LISTVIEW_CLASSES,
         TreeView = ICC_TREEVIEW_CLASSES,
         Header = ICC_LISTVIEW_CLASSES,
         ToolBar = ICC_BAR_CLASSES,
         StatusBar = ICC_BAR_CLASSES,
         TrackBar = ICC_BAR_CLASSES,
         ToolTips = ICC_BAR_CLASSES,
         TabControl = ICC_TAB_CLASSES,
         UpDown = ICC_UPDOWN_CLASS,
         Progress = ICC_PROGRESS_CLASS,
         HotKey = ICC_HOTKEY_CLASS,
         Animate = ICC_ANIMATE_CLASS,
         Win95 = ICC_WIN95_CLASSES,
         DateTimePicker = ICC_DATE_CLASSES,
         ComboBoxEx = ICC_USEREX_CLASSES,
         Rebar = ICC_COOL_CLASSES,
         Internet = ICC_INTERNET_CLASSES,
         PageScroller = ICC_PAGESCROLLER_CLASS,
         NativeFont = ICC_NATIVEFNTCTL_CLASS,
         Standard = ICC_STANDARD_CLASSES,
         Link = ICC_LINK_CLASS
      };

      /// <summary>
      /// Types: Platform
      /// </summary>

      //Pack attribute is not supported in StructLayout for Compact Framework.
#if !PocketPC
      [StructLayout(LayoutKind.Sequential, Pack = 1)]
#else
      [StructLayout(LayoutKind.Sequential)]
#endif

      private struct INITCOMMONCONTROLSEX
      {
         public int cbSize;
         public int nFlags;

         public INITCOMMONCONTROLSEX(int cbSize, int nFlags)
         {
            this.cbSize = cbSize;
            this.nFlags = nFlags;
         }
      }

      [DllImport("comctl32.dll")]
      private static extern bool InitCommonControlsEx(ref INITCOMMONCONTROLSEX icc);

      /// <summary>
      /// Operations
      /// </summary>

      /// <summary>
      /// <code>void Init(Classes fClasses)</code>
      /// <para>Initializes common controls.</para>
      /// </summary>
      /// <param name="fClasses"> Bit flags defining classes to be initialized</param>
      static public void Init(Classes fClasses)
      {
         INITCOMMONCONTROLSEX icc =
            new INITCOMMONCONTROLSEX(Marshal.SizeOf(typeof(INITCOMMONCONTROLSEX)),
                               (int)fClasses);

         bool bResult = InitCommonControlsEx(ref icc);
         Debug.Assert(bResult);
         if (!bResult)
         {
            throw new SystemException("Failture initializing common controls.");
         }
      }

   } // InitCommonControlsHelper class


   /// <summary>
   /// NativeWindowCommon class
   /// </summary>
   public class NativeWindowCommon
   {
#if !PocketPC
      protected const String userdll = "user32.dll";
      protected const String gdidll = "gdi32.dll";
      protected const String kerneldll = "Kernel32.dll";
      protected const String shelldll = "Shell32.Dll";
#else
      protected const String userdll = "coredll.dll";
      protected const String gdidll = "coredll.dll";
      protected const String kerneldll = "coredll.dll";
#endif
      /// <summary>
      /// Constants: Window Styles
      /// </summary>
      public const int WS_OVERLAPPED = 0x00000000;

      public const int WS_POPUP = unchecked((int)0x80000000);
      public const int WS_CHILD = 0x40000000;
      public const int WS_MINIMIZE = 0x20000000;
      public const int WS_VISIBLE = 0x10000000;
      public const int WS_DISABLED = 0x08000000;
      public const int WS_CLIPSIBLINGS = 0x04000000;
      public const int WS_CLIPCHILDREN = 0x02000000;
      public const int WS_MAXIMIZE = 0x01000000;
      public const int WS_CAPTION = 0x00C00000;  // WS_BORDER|WS_DLGFRAME
      public const int WS_BORDER = 0x00800000;
      public const int WS_DLGFRAME = 0x00400000;
      public const int WS_VSCROLL = 0x00200000;
      public const int WS_HSCROLL = 0x00100000;
      public const int WS_SYSMENU = 0x00080000;
      public const int WS_THICKFRAME = 0x00040000;
      public const int WS_GROUP = 0x00020000;
      public const int WS_TABSTOP = 0x00010000;
      public const int WS_MINIMIZEBOX = 0x00020000;
      public const int WS_MAXIMIZEBOX = 0x00010000;

      public const int WS_TILED = WS_OVERLAPPED;
      public const int WS_ICONIC = WS_MINIMIZE;
      public const int WS_SIZEBOX = WS_THICKFRAME;
      public const int WS_TILEDWINDOW = WS_OVERLAPPEDWINDOW;

      public const int WS_OVERLAPPEDWINDOW = WS_OVERLAPPED | WS_CAPTION |
                                    WS_SYSMENU | WS_THICKFRAME |
                                    WS_MINIMIZEBOX | WS_MAXIMIZEBOX;

      public const int WS_POPUPWINDOW = WS_POPUP | WS_BORDER | WS_SYSMENU;
      public const int WS_CHILDWINDOW = WS_CHILD;

      public const int WS_SHOWWINDOW = 0x18;

      /// <summary>
      /// Constants: Extended Window Styles
      /// </summary>
      public const int WS_EX_DLGMODALFRAME = 0x00000001;
      public const int WS_EX_NOPARENTNOTIFY = 0x00000004;
      public const int WS_EX_TOPMOST = 0x00000008;
      public const int WS_EX_ACCEPTFILES = 0x00000010;
      public const int WS_EX_TRANSPARENT = 0x00000020;

      public const int WS_EX_MDICHILD = 0x00000040;
      public const int WS_EX_TOOLWINDOW = 0x00000080;
      public const int WS_EX_WINDOWEDGE = 0x00000100;
      public const int WS_EX_CLIENTEDGE = 0x00000200;
      public const int WS_EX_CONTEXTHELP = 0x00000400;

      public const int WS_EX_RIGHT = 0x00001000;
      public const int WS_EX_LEFT = 0x00000000;
      public const int WS_EX_RTLREADING = 0x00002000;
      public const int WS_EX_LTRREADING = 0x00000000;
      public const int WS_EX_LEFTSCROLLBAR = 0x00004000;
      public const int WS_EX_RIGHTSCROLLBAR = 0x00000000;

      public const int WS_EX_CONTROLPARENT = 0x00010000;
      public const int WS_EX_STATICEDGE = 0x00020000;
      public const int WS_EX_APPWINDOW = 0x00040000;

      public const int WS_EX_OVERLAPPEDWINDOW = WS_EX_WINDOWEDGE | WS_EX_CLIENTEDGE;
      public const int WS_EX_PALETTEWINDOW = WS_EX_WINDOWEDGE | WS_EX_TOOLWINDOW |
         WS_EX_TOPMOST;

      public const int WS_EX_LAYERED = 0x00080000;
      public const int WS_EX_NOINHERITLAYOUT = 0x00100000;
      public const int WS_EX_LAYOUTRTL = 0x00400000;




      public const int WS_EX_COMPOSITED = 0x02000000;
      public const int WS_EX_NOACTIVATE = 0x08000000;

      // Common control shared messages
      public const int CCM_FIRST = 0x00002000;
      public const int CCM_LAST = CCM_FIRST + 0x200;
      public const int CCM_SETBKCOLOR = CCM_FIRST + 1;
      public const int CCM_SETCOLORSCHEME = CCM_FIRST + 2;
      public const int CCM_GETCOLORSCHEME = CCM_FIRST + 3;
      public const int CCM_GETDROPTARGET = CCM_FIRST + 4;
      public const int CCM_SETUNICODEFORMAT = CCM_FIRST + 5;
      public const int CCM_GETUNICODEFORMAT = CCM_FIRST + 6;


      // Common messages
      public const int WM_SIZE = 0x0005;
      public const int WM_ACTIVATE = 0x0006;
      public const int WM_SETFOCUS = 0x0007;
      public const int WM_KILLFOCUS = 0x0008;
      public const int WM_CLOSE = 0x0010;
      public const int WM_CHILDACTIVATE = 0x0022;
      public const int WM_MOUSEACTIVATE = 0x21;
      public const int WM_SETREDRAW = 0x000B;
      public const int WM_CANCELMODE = 0x001F;

      public const int WM_MDINEXT = 0x0224;
      public const int WM_NCACTIVATE = 0x0086;
      public const int WM_ACTIVATEAPP = 0x001C;
      public const int WM_NCMOUSEACTIVATE = 0x0021;
      public const int WM_KEYDOWN = 0x100;
      public const int WM_KEYUP = 0x101;
      public const int WM_CHAR = 0x0102;
      public const int WM_SYSKEYDOWN = 0x104;
      public const int WM_SYSKEYUP = 0x105;
      public const int WM_SYSCHAR = 0x106;
      public const int WM_IME_CHAR = 0x0286;
      public const int WM_CUT = 0x0300;
      public const int WM_COPY = 0x0301;
      public const int WM_PASTE = 0x0302;
      public const int WM_CLEAR = 0x0303;
      public const int WM_UNDO = 0x0304;

      public const int CB_SHOWDROPDOWN = 0x014F;

      public const int WM_MOUSELAST = 0x20a;
      public const int WM_MOUSEMOVE = 0x200;
      public const int WM_MOUSEHOVER = 0x02A1;
      public const int WM_MOUSELEAVE = 0x02A3;
      public const int WM_LBUTTONDOWN = 0x201;
      public const int WM_LBUTTONUP = 0x202;
      public const int WM_LBUTTONDBLCLK = 0x0203;
      public const int WM_RBUTTONDOWN = 0x0204;
      public const int WM_PARENTNOTIFY = 0x0210;
      public const int WM_SIZING = 0x0214;
      public const int WM_WINDOWPOSCHANGING = 0x0046;
      public const int WM_COPYDATA = 0x004A;

      public const int WM_MENUCHAR = 0x120;

      public const int WM_NCHITTEST = 0x0084;

      public const int WM_SETCURSOR = 0x0020;
      public const int WM_TIMER = 0x0113;
      public const int WM_SYSTIMER = 0x0118;
      public const int WM_HSCROLL = 0x114;
      public const int WM_VSCROLL = 0x115;
      public const int WM_NCLBUTTONDOWN = 0x00A1;
      public const int WM_NCLBUTTONUP = 0x00A2;
      public const int WM_NCLBUTTONDBLCLK = 0x00A3;
      public const int WM_NCRBUTTONDOWN = 0x00A4;
      public const int WM_CONTEXTMENU = 0x007b;

      public const int WM_NCCALCSIZE = 0x83;   // WM_NCCALCSIZE message
      public const int WM_NCPAINT = 0x0085;    // WM_NCPAINT message
      public const int WM_ERASEBKGND = 0x0014; // WM_ERASEBKGND message
      public const int WM_PAINT = 0x000F;      // WM_PAINT message
      

      public const int WM_NOTIFY = 0x4e;
      public const int WM_COMMAND = 0x111;
      public const int WM_SYSCOMMAND = 274;

      public const int WM_USER = 0x0400;
      public const int WM_REFLECT = WM_USER + 0x1C00;
      public const int OCM__BASE = WM_USER + 0x1c00;

      public const int MG_IME_STARTCOMPOSITION = WM_USER + 0x3001;
      public const int MG_IME_ENDCOMPOSITION = WM_USER + 0x3002;
      public const int MG_IME_COMPOSITION = WM_USER + 0x3003;
      public const int MG_IME_CHAR = WM_USER + 0x3004;
      public const int MG_KEYDOWN = WM_USER + 0x3005;
      public const int MG_CHAR = WM_USER + 0x3006;

      public const int BFFM_ENABLEOK = WM_USER + 101;
      public const int BFFM_SETSELECTIONW = WM_USER + 103;

      public const int EM_CHARFROMPOS = 0x00D7;
      public const int EM_GETSEL = 0x00B0;
      public const int EM_GETLINECOUNT = 0x00BA;
      public const int EM_LINEINDEX = 0x00BB;

      public const int CBN_DROPDOWN = 7;
      public const int CB_GETCOMBOBOXINFO = 0x0164;

      public const int MA_ACTIVATE = 1;
      public const int MA_ACTIVATEANDEAT = 2;
      public const int MA_NOACTIVATE = 3;
      public const int MA_NOACTIVATEANDEAT = 4;

      public const int HTCAPTION = 2;
      public const int HTCLOSE = 20;



      public const int SC_MOVE = 0xF010;
      public const int SC_MINIMIZE = 0xF020;
      public const int SC_RESTORE = 0xF120;

      public const int BM_CLICK = 0x00F5;

      public const int WM_CHANGEUISTATE = 0x0127;
      public const int WM_UPDATEUISTATE = 0x0128;
      public const int WM_QUERYUISTATE = 0x0129;

      public const int PM_REMOVE = 0x0001;

      public const int UIS_SET = 1;
      public const int UIS_CLEAR = 2;
      public const int UIS_INITIALIZE = 3;

      public const int UISF_HIDEFOCUS = 0x1;
      public const int UISF_HIDEACCEL = 0x2;
      public const int WM_GETMINMAXINFO = 0x0024;

      public const int WM_SETFONT = 0x30;
      public const int WM_FONTCHANGE = 0x1d;

      public const int WM_CREATE = 0x0001;
      public const int WM_DESTROY = 0x0002;
      public const int WM_NCDESTROY = 0x0082;

      /*  wParam for WM_SIZING message  */
      public enum SizingEdges
      {
         WMSZ_LEFT = 1,
         WMSZ_RIGHT = 2,
         WMSZ_TOP = 3,
         WMSZ_TOPLEFT = 4,
         WMSZ_TOPRIGHT = 5,
         WMSZ_BOTTOM = 6,
         WMSZ_BOTTOMLEFT = 7,
         WMSZ_BOTTOMRIGHT = 8,
         /* WINVER >= 0x0400 */
      }


      public enum HitTestValues
      {
         HTERROR = -2,
         HTTRANSPARENT = -1,
         HTNOWHERE = 0,
         HTCLIENT = 1,
         HTCAPTION = 2,
         HTSYSMENU = 3,
         HTGROWBOX = 4,
         HTMENU = 5,
         HTHSCROLL = 6,
         HTVSCROLL = 7,
         HTMINBUTTON = 8,
         HTMAXBUTTON = 9,
         HTLEFT = 10,
         HTRIGHT = 11,
         HTTOP = 12,
         HTTOPLEFT = 13,
         HTTOPRIGHT = 14,
         HTBOTTOM = 15,
         HTBOTTOMLEFT = 16,
         HTBOTTOMRIGHT = 17,
         HTBORDER = 18,
         HTOBJECT = 19,
         HTCLOSE = 20,
         HTHELP = 21
      }


      /// <summary>
      /// Constants for SetWindowPos
      /// </summary>
      public const int SWP_NOSIZE = 0x0001;
      public const int SWP_NOMOVE = 0x0002;
      public const int SWP_NOZORDER = 0x0004;
      public const int SWP_NOREDRAW = 0x0008;
      public const int SWP_NOACTIVATE = 0x0010;
      public const int SWP_FRAMECHANGED = 0x0020;
      public const int SWP_SHOWWINDOW = 0x0040;
      public const int SWP_HIDEWINDOW = 0x0080;
      public const int SWP_NOCOPYBITS = 0x0100;
      public const int SWP_NOOWNERZORDER = 0x0200;
      public const int SWP_NOSENDCHANGING = 0x0400;

      public const int SWP_DRAWFRAME = SWP_FRAMECHANGED;
      public const int SWP_NOREPOSITION = SWP_NOOWNERZORDER;
      public const int SWP_DEFERERASE = 0x2000;
      public const int SWP_ASYNCWINDOWPOS = 0x4000;

      public const int RGN_AND = 1;
      public const int RGN_COPY = 5;
      public const int RGN_OR = 2;
      public const int RGN_XOR = 3;
      public const int RGN_DIFF = 4;

      public const int DCX_WINDOW = 0x1;
      public const int DCX_CACHE = 0x2;
      public const int DCX_CLIPCHILDREN = 0x8;
      public const int DCX_CLIPSIBLINGS = 0x10;
      public const int DCX_LOCKWINDOWUPDATE = 0x400;



      public static readonly IntPtr HWND_TOP;
      public static readonly IntPtr HWND_BOTTOM;
      public static readonly IntPtr HWND_TOPMOST;
      public static readonly IntPtr HWND_NOTOPMOST;

      /// <summary>
      /// Constants for GetWindowLong
      /// </summary>
      public const int GWL_WNDPROC = -4;
      public const int GWL_HINSTANCE = -6;
      public const int GWL_HWNDPARENT = -8;
      public const int GWL_STYLE = -16;
      public const int GWL_EXSTYLE = -20;
      public const int GWL_USERDATA = -21;
      public const int GWL_ID = -12;
      public const int DT_TOP = 0x0000;
      public const int DT_LEFT = 0x0000;
      public const int DT_CENTER = 0x0001;
      public const int DT_RIGHT = 0x0002;
      public const int DT_VCENTER = 0x0004;
      public const int DT_BOTTOM = 0x0008;
      public const int DT_WORDBREAK = 0x0010;
      public const int DT_SINGLELINE = 0x0020;
      public const int DT_EXPANDTABS = 0x0040;
      public const int DT_TABSTOP = 0x0080;
      public const int DT_NOCLIP = 0x0100;
      public const int DT_EXTERNALLEADING = 0x0200;
      public const int DT_CALCRECT = 0x0400;
      public const int DT_NOPREFIX = 0x0800;
      public const int DT_INTERNAL = 0x1000;
      public const int DT_EDITCONTROL = 0x2000;
      public const int DT_END_ELLIPSIS = 0x00008000;


      /* Background Modes */
      public const int TRANSPARENT = 1;
      public const int OPAQUE = 2;
      public const int BKMODE_LAST = 2;
      public const int PS_SOLID = 0;
      public const int PS_DASH = 1;
      public const int PS_DOT = 2;
      public const int PS_DASHDOT = 3;
      public const int PS_DASHDOTDOT = 4;
      public const int PS_NULL = 5;
      public const int PS_INSIDEFRAME = 6;

      public const int TVS_TRACKSELECT = 0x200;

      // Constant for DrawEdge
      public const int BF_RECT = 0x0f;

      // Constant for WM_NOTIFY
      public const int NM_CLICK = 0 - 2;

      //Constants for LoadImage
      public const uint IMAGE_BITMAP = 0;
      public const uint IMAGE_ICON = 1;
      public const uint IMAGE_CURSOR = 2;

      public const uint CF_TEXT = 1;
      public const uint CF_UNICODETEXT = 13;


      public enum RedrawWindowFlags
      {
         RDW_INVALIDATE = 0x1,
         RDW_FRAME = 0x400
      }

      /// <summary>
      /// Types
      /// </summary>
      [StructLayout(LayoutKind.Sequential)]
      public struct POINT
      {
         public int x;
         public int y;
      }

      /// <summary>
      /// Types
      /// </summary>
      [StructLayout(LayoutKind.Sequential)]
      public struct MINMAXINFO
      {
         public POINT ptReserved;
         public POINT ptMaxSize;
         public POINT ptMaxPosition;
         public POINT ptMinTrackSize;
         public POINT ptMaxTrackSize;
      }

      [StructLayout(LayoutKind.Sequential)]
      public struct RECT
      {
         public int left;
         public int top;
         public int right;
         public int bottom;
      }

      /// <summary>
      /// COMBOBOXINFO
      /// </summary>
      [StructLayout(LayoutKind.Sequential)]
      public struct COMBOBOXINFO
      {
         public IntPtr cbSize;
         public RECT rcItem;
         public RECT rcButton;
         public IntPtr stateButton;
         public IntPtr hwndCombo;
         public IntPtr hwndItem;
         public IntPtr hwndList;
      };

      [StructLayout(LayoutKind.Sequential)]
      public struct WINDOWPOS
      {
         public IntPtr hwnd;
         public IntPtr hwndInsertAfter;
         public int x;
         public int y;
         public int cx;
         public int cy;
         public int flags;
      }

      [StructLayout(LayoutKind.Sequential)]
      public struct NMHDR
      {
         public IntPtr hwndFrom;
         public int idFrom;
         public int code;
      }

      [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
      public struct TEXTMETRIC
      {
         public int tmHeight;
         public int tmAscent;
         public int tmDescent;
         public int tmInternalLeading;
         public int tmExternalLeading;
         public int tmAveCharWidth;
         public int tmMaxCharWidth;
         public int tmWeight;
         public int tmOverhang;
         public int tmDigitizedAspectX;
         public int tmDigitizedAspectY;
         public char tmFirstChar;
         public char tmLastChar;
         public char tmDefaultChar;
         public char tmBreakChar;
         public byte tmItalic;
         public byte tmUnderlined;
         public byte tmStruckOut;
         public byte tmPitchAndFamily;
         public byte tmCharSet;
      };

      public struct Message
      {
         public IntPtr HWnd;
         public int Msg;
         public IntPtr LParam;
         public IntPtr WParam;
         public uint time;
         public POINT pt;
      }

      //NCCALCSIZE_PARAMS Structure
      [StructLayout(LayoutKind.Sequential)]
      public struct NCCALCSIZE_PARAMS
      {
         public RECT rgrc0, rgrc1, rgrc2;
         public WINDOWPOS lppos;
      }

      /// <summary>
      /// Static constuctor
      /// </summary>
      static NativeWindowCommon()
      {
         HWND_TOP = (IntPtr)0;
         HWND_BOTTOM = (IntPtr)1;
         HWND_TOPMOST = (IntPtr)(-1);
         HWND_NOTOPMOST = (IntPtr)(-2);
      }

      [Flags()]
      public enum TCHITTESTFLAGS
      {
         TCHT_NOWHERE = 1,
         TCHT_ONITEMICON = 2,
         TCHT_ONITEMLABEL = 4,
         TCHT_ONITEM = TCHT_ONITEMICON | TCHT_ONITEMLABEL
      }

      public const int TCM_HITTEST = 0x130D;

      [StructLayout(LayoutKind.Sequential)]
      public struct TCHITTESTINFO
      {

         public TCHITTESTINFO(Point location)
         {
            pt = location;
            flags = TCHITTESTFLAGS.TCHT_ONITEM;
         }

         public Point pt;
         public TCHITTESTFLAGS flags;
      }

      [DllImport(userdll, CharSet = CharSet.Auto)]
      public static extern int SendMessage(IntPtr hWnd, int msg, int wParam, ref TCHITTESTINFO lParam);


      [StructLayout(LayoutKind.Sequential)]
      public struct CHARFORMAT
      {
         public int cbSize;
         public uint dwMask;
         public uint dwEffects;
         public int yHeight;
         public int yOffset;
         public int crTextColor;
         public byte bCharSet;
         public byte bPitchAndFamily;
         [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
         public char[] szFaceName;
      }

      // CHARFORMAT masks 
      public const UInt32 CFM_BOLD = 0x00000001;
      public const UInt32 CFM_ITALIC = 0x00000002;
      public const UInt32 CFM_UNDERLINE = 0x00000004;
      public const UInt32 CFM_STRIKE = 0x00000008;
      public const UInt32 CFM_FACE = 0x20000000;
      public const UInt32 CFM_SIZE = 0x80000000;

      // CHARFORMAT effects 
      public const int CFE_BOLD = 0x0001;
      public const int CFE_ITALIC = 0x0002;
      public const int CFE_UNDERLINE = 0x0004;
      public const int CFE_STRIKEOUT = 0x0008;

      public const int SCF_ALL = 0x0004;

      public const int EM_SETCHARFORMAT = 0x0444;

      /// <summary>
      /// Helpers
      /// </summary>
      protected static bool IsSysCharSetAnsi()
      {
         return Marshal.SystemDefaultCharSize == 1;
      }

      /// <summary>
      /// Operations
      /// </summary>
      ///     
      [DllImport(userdll, CharSet = CharSet.Auto)]
      public static extern IntPtr FindWindowW(string lpClassName, string lpWindowName);

      [DllImport(userdll, CharSet = CharSet.Auto)]
      public static extern bool SetForegroundWindow(IntPtr hWnd);

      [DllImport(userdll)]
      public static extern IntPtr GetForegroundWindow();

      /// <summary>
      /// C# representation of the IMalloc interface.
      /// </summary>
      [InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("00000002-0000-0000-C000-000000000046")]
      public interface IMalloc
      {
         [PreserveSig]
         IntPtr Alloc([In] int cb);
         [PreserveSig]
         IntPtr Realloc([In] IntPtr pv, [In] int cb);
         [PreserveSig]
         void Free([In] IntPtr pv);
         [PreserveSig]
         int GetSize([In] IntPtr pv);
         [PreserveSig]
         int DidAlloc(IntPtr pv);
         [PreserveSig]
         void HeapMinimize();
      }

      [DllImport(shelldll)]
      public static extern int SHGetMalloc(out IMalloc ppMalloc);

      /// <summary>
      /// Constants for receiving messages in BrowseCallBackProc.
      /// </summary>
      public const int BFFM_INITIALIZED = 1;
      public const int BFFM_SELCHANGED = 2;

      /// <summary>
      /// Styles affecting the appearance and behaviour of the browser dialog. This is a subset of the styles 
      /// available as we're not exposing the full list of options in this simple wrapper.
      /// See http://msdn.microsoft.com/en-us/library/windows/desktop/bb773205%28v=vs.85%29.aspx for the complete
      /// list.
      /// </summary>
      public enum BffStyles
      {  
         NewDialogStyle = 0x0040,    // BIF_NEWDIALOGSTYLE
         NoNewFolderButton = 0x0200, // BIF_NONEWFOLDERBUTTON
      }

      /// <summary>
      /// Constants for sending messages to a Tree-View Control.
      /// </summary>
      public const int TV_FIRST = 0x1100;
      public const int TVM_GETNEXTITEM = (TV_FIRST + 10);
      public const int TVM_ENSUREVISIBLE = (TV_FIRST + 20);
      public const int TVGN_ROOT = 0x0;
      public const int TVGN_CHILD = 0x4;
      public const int TVGN_CARET = 0x9;

      /// <summary>
      /// Struct to pass parameters to the SHBrowseForFolder function.
      /// </summary>
      [StructLayout(LayoutKind.Sequential, Pack = 8)]
      public struct BROWSEINFO
      {
         public IntPtr hwndOwner;
         public IntPtr pidlRoot;
         public IntPtr pszDisplayName;
         [MarshalAs(UnmanagedType.LPTStr)]
         public string lpszTitle;
         public int ulFlags;
         [MarshalAs(UnmanagedType.FunctionPtr)]
         public BrowseCallbackProc lpfn;
         public IntPtr lParam;
         public int iImage;
      }

      /// <summary>
      /// Delegate type used in BROWSEINFO.lpfn field.
      /// </summary>
      /// <param name="hwnd"></param>
      /// <param name="msg"></param>
      /// <param name="lParam"></param>
      /// <param name="lpData"></param>
      /// <returns></returns>
      public delegate int BrowseCallbackProc(IntPtr hwnd, int msg, IntPtr lParam, IntPtr lpData);

      [DllImport(shelldll)]
      public static extern int SHGetSpecialFolderLocation(IntPtr hwndOwner, int nFolder, ref IntPtr ppidl);

      [DllImport(shelldll)]
      public static extern bool SHGetPathFromIDList(IntPtr pidl, StringBuilder Path);

      [DllImport(shelldll, CharSet = CharSet.Auto)]
      public static extern IntPtr SHBrowseForFolder(ref BROWSEINFO bi);

      [DllImport(userdll, SetLastError = true)]
      public static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className, string windowTitle);

      [DllImport(userdll, CharSet = CharSet.Auto)]
      public static extern IntPtr SendMessage(IntPtr hwnd, int msg, int wParam, string lParam);
      
      [DllImport(userdll, CharSet = CharSet.Auto)]
      public static extern int SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

      [DllImport(userdll, CharSet = CharSet.Auto)]
      public static extern int SendMessage(IntPtr hWnd, int msg, bool wParam, int lParam);

      [DllImport(userdll, CharSet = CharSet.Auto)]
      public static extern bool PostMessage(IntPtr hWnd, int msg, int wParam, int lParam);

      [DllImport(userdll, CharSet = CharSet.Auto)]
      public static extern bool PeekMessage(out Message lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);

      [DllImport(userdll, CharSet = CharSet.Auto)]
      public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
         int X, int Y, int cx, int cy, int uFlags);

      [DllImport(userdll, CharSet = CharSet.Auto)]
      public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

      [DllImport(userdll, CharSet = CharSet.Auto)]
      public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

      [DllImport("uxtheme.dll", CharSet = CharSet.Auto)]
      public static extern int SetWindowTheme(IntPtr hWnd, String pszSubAppName, String pszSubIdList);

      [DllImport(gdidll, CharSet = CharSet.Auto)]
      public static extern bool SetLayout(IntPtr hDc, int dwLayout);

      [DllImport(gdidll, CharSet = CharSet.Auto)]
      public static extern int GetLayout(IntPtr hDc);

      [DllImport(userdll)]
      public static extern int InflateRect(ref RECT lpRect, int x, int y);

      [DllImport(userdll, SetLastError = true, CharSet = CharSet.Auto)]
      public static extern IntPtr LoadImage(IntPtr hinst, string lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

      [DllImport(gdidll, CharSet = CharSet.Auto)]
      public static extern bool DeleteObject(IntPtr hObject);

      [DllImport(userdll, SetLastError = true)]
      [return: MarshalAs(UnmanagedType.Bool)]
      public static extern bool DestroyIcon(IntPtr hIcon);

      [DllImport(gdidll)]
      public static extern int CombineRgn(IntPtr hDestRgn, IntPtr hSrcRgn1, IntPtr hSrcRgn2, int nCombineMode);

      [DllImport(userdll)]
      public static extern int ShowWindow(int hwnd, int cmdShow);

      [DllImport(userdll, CharSet = CharSet.Auto)]
      public static extern IntPtr GetParent(IntPtr hWnd);

      [DllImport(userdll)]
      public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

      [DllImport(userdll)]
      public static extern bool EnableWindow(IntPtr hWnd, bool bEnable);

      [DllImport(userdll)]
      public static extern bool UpdateWindow(IntPtr hWnd);

      public const uint MAPVK_VSC_TO_VK = 0x01;
      public const uint MAPVK_VK_TO_CHAR = 0x02;
      public const long FIRST_UNICODE_HEB_CHAR = 0x000005d0; // 1488 first char in heb 
      public const long LAST_UNICODE_HEB_CHAR = 0x000005ea; // 1514 last char in heb 

      [DllImport(userdll, CharSet = CharSet.Auto)]
      public static extern uint MapVirtualKey(uint uCode, uint uMapType);

      [DllImport(kerneldll)]
      public static extern int MulDiv(int nNumber, int nNumerator, int nDenominator);

      // Arg for SetWindowsHookEx()
      public delegate int WindowsHookProc(int code, IntPtr wParam, IntPtr lParam);

      [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
      public static extern int SetWindowsHookEx(int idHook, WindowsHookProc lpfn, IntPtr hInstance, int threadId);

      [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
      public static extern bool UnhookWindowsHookEx(int idHook);

      [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
      public static extern int CallNextHookEx(int idHook, int nCode, IntPtr wParam, IntPtr lParam);

      [DllImport("Kernel32.dll", CharSet = CharSet.Auto)]
      public static extern int GetCurrentThreadId();

      [DllImport("user32.dll", EntryPoint = "MoveWindow")]
      public static extern bool MoveWindow(IntPtr hWnd, int x, int y, int w, int h, bool repaint);

      [DllImport("user32.dll", EntryPoint = "OpenClipboard", SetLastError = true)]
      [return: MarshalAs(UnmanagedType.Bool)]
      public static extern bool OpenClipboard([In] IntPtr hWndNewOwner);

      [DllImport("user32.dll", EntryPoint = "CloseClipboard", SetLastError = true)]
      [return: MarshalAs(UnmanagedType.Bool)]
      public static extern bool CloseClipboard();


      [DllImport("user32.dll", EntryPoint = "GetClipboardData", SetLastError = true)]
      public static extern IntPtr GetClipboardData(uint uFormat);

      [DllImport("kernel32.dll")]
      public static extern UIntPtr GlobalSize(IntPtr hMem);


      [DllImport("kernel32.dll")]
      public static extern IntPtr GlobalLock(IntPtr hMem);

      [DllImport("kernel32.dll")]
      [return: MarshalAs(UnmanagedType.Bool)]
      public static extern bool GlobalUnlock(IntPtr hMem);

      [DllImport("user32.dll")]
      public static extern bool ReleaseCapture();

      [DllImport("user32")]
      [return: MarshalAs(UnmanagedType.Bool)]
      public static extern bool EnumChildWindows(IntPtr window, EnumWindowProc callback, IntPtr i);


      /// <summary>
      /// Delegate for the EnumChildWindows method
      /// </summary>
      /// <param name="hWnd">Window handle</param>
      /// <param name="parameter">Caller-defined variable; we use it for a pointer to our list</param>
      /// <returns>True to continue enumerating, false to bail.</returns>
      public delegate bool EnumWindowProc(IntPtr hWnd, IntPtr parameter);

      public enum ShowWindowOptions
      {
         FORCEMINIMIZE = 11,
         HIDE = 0,
         MAXIMIZE = 3,
         MINIMIZE = 6,
         RESTORE = 9,
         SHOW = 5,
         SHOWDEFAULT = 10,
         SHOWMAXIMIZED = 3,
         SHOWMINIMIZED = 2,
         SHOWMINNOACTIVE = 7,
         SHOWNA = 8,
         SHOWNOACTIVATE = 4,
         SHOWNORMAL = 1
      }

      public enum FontWeight : int
      {
         FW_DONTCARE = 0,
         FW_THIN = 100,
         FW_EXTRALIGHT = 200,
         FW_LIGHT = 300,
         FW_NORMAL = 400,
         FW_MEDIUM = 500,
         FW_SEMIBOLD = 600,
         FW_BOLD = 700,
         FW_EXTRABOLD = 800,
         FW_HEAVY = 900,
      }

      [DllImport(gdidll)]
      public static extern IntPtr CreateRectRgn(int X1, int Y1, int X2, int Y2);

      [DllImport(gdidll)]
      public static extern int SelectClipRgn(IntPtr hdc, IntPtr hrgn);

      [DllImport(gdidll)]
      public static extern IntPtr CreateRectRgnIndirect(ref RECT lpRect);

      [DllImport(userdll)]
      public static extern IntPtr GetDCEx(IntPtr hwnd, IntPtr hrgnclip, int fdwOptions);

      [DllImport(userdll)]
      public static extern IntPtr GetWindowDC(IntPtr hwnd);

      [DllImport(userdll, EntryPoint = "ReleaseDC")]
      public static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDc);

      [DllImport(userdll, CharSet = CharSet.Auto)]
      public static extern int DrawText(IntPtr hDC, string lpString, int nCount, ref RECT Rect, int wFormat);

      [DllImport(userdll, CharSet = CharSet.Auto)]
      public static extern IntPtr GetFocus();

      [DllImport("user32.dll", EntryPoint = "RealGetWindowClassW", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
      public static extern uint RealGetWindowClass(IntPtr hWnd, StringBuilder ClassName, uint ClassNameMax);


      [StructLayout(LayoutKind.Sequential)]
      public struct DRAWTEXTPARAMS
      {
         public uint cbSize;
         public int iTabLength;
         public int iLeftMargin;
         public int iRightMargin;
         public uint uiLengthDrawn;
      }

      [DllImport(gdidll, CharSet = CharSet.Auto)]
      public static extern int EnumFontFamiliesEx(IntPtr hdc, [In] IntPtr pLogfont, EnumFontExDelegate lpEnumFontFamExProc, IntPtr lParam, uint dwFlags);

      public delegate int EnumFontExDelegate(ref ENUMLOGFONTEX lpelfe, ref NEWTEXTMETRICEX lpntme, int FontType, int lParam);

      [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
      public struct NEWTEXTMETRIC
      {
         public int tmHeight;
         public int tmAscent;
         public int tmDescent;
         public int tmInternalLeading;
         public int tmExternalLeading;
         public int tmAveCharWidth;
         public int tmMaxCharWidth;
         public int tmWeight;
         public int tmOverhang;
         public int tmDigitizedAspectX;
         public int tmDigitizedAspectY;
         public char tmFirstChar;
         public char tmLastChar;
         public char tmDefaultChar;
         public char tmBreakChar;
         public byte tmItalic;
         public byte tmUnderlined;
         public byte tmStruckOut;
         public byte tmPitchAndFamily;
         public byte tmCharSet;
         int ntmFlags;
         int ntmSizeEM;
         int ntmCellHeight;
         int ntmAvgWidth;
      }

      [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
      public struct FONTSIGNATURE
      {
         [MarshalAs(UnmanagedType.ByValArray)]
         int[] fsUsb;
         [MarshalAs(UnmanagedType.ByValArray)]
         int[] fsCsb;
      }

      [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
      public struct NEWTEXTMETRICEX
      {
         NEWTEXTMETRIC ntmTm;
         FONTSIGNATURE ntmFontSig;
      }

      [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
      public struct ENUMLOGFONTEX
      {
         public LOGFONT elfLogFont;
         [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
         public string elfFullName;
         [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
         public string elfStyle;
         [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
         public string elfScript;
      }

      public enum FontPitchAndFamily
      {
         DEFAULT_PITCH = 0,
         FIXED_PITCH = 1,
         VARIABLE_PITCH = 2,
         FF_DONTCARE = (0 << 4),
         FF_ROMAN = (1 << 4),
         FF_SWISS = (2 << 4),
         FF_MODERN = (3 << 4),
         FF_SCRIPT = (4 << 4),
         FF_DECORATIVE = (5 << 4),
      }

      [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
      public class LOGFONT
      {
         public const int LF_FACESIZE = 32;
         public int lfHeight;
         public int lfWidth;
         public int lfEscapement;
         public int lfOrientation;
         public int lfWeight;
         public byte lfItalic;
         public byte lfUnderline;
         public byte lfStrikeOut;
         public byte lfCharSet;
         public byte lfOutPrecision;
         public byte lfClipPrecision;
         public byte lfQuality;
         public byte lfPitchAndFamily;
         [MarshalAs(UnmanagedType.ByValTStr, SizeConst = LF_FACESIZE)]
         public string lfFaceName;
      }

      [DllImport(gdidll, CharSet = CharSet.Auto)]
      public static extern IntPtr CreateFontIndirect([In, MarshalAs(UnmanagedType.LPStruct)] LOGFONT lplf);

      [DllImport(userdll)]
      public static extern int DrawTextEx(IntPtr hdc, string lpchText, int cchText,
         ref RECT lprc, uint dwDTFormat, IntPtr ptr);   //ref DRAWTEXTPARAMS lpDTParams);

      public const int ETO_CLIPPED = 0x0004;
      public const int ETO_RTLREADING = 0x0080;

      [DllImport(gdidll, CharSet = CharSet.Unicode)]
      public static extern bool ExtTextOut(IntPtr hdc, int X, int Y, uint fuOptions, ref RECT lprc, string lpString, uint cbCount, int[] lpDx);

      [DllImport(gdidll)]
      public static extern int SetBkMode(IntPtr hDC, int nBkMode);
      //[DllImport(gdidll)]
      //public static extern int SetTextColor(IntPtr hDC, int crColor);
      [DllImport(gdidll)]
      public static extern int Polygon(IntPtr hDC, POINT[] lpPoints, int nCount);

      [DllImport(gdidll)]
      public static extern bool LineTo(IntPtr hdc, int nXEnd, int nYEnd);

      [DllImport(gdidll)]
      public static extern bool MoveToEx(IntPtr hdc, int X, int Y, IntPtr lpPoint);
      [DllImport(gdidll)]
      public static extern IntPtr CreateSolidBrush(int argbColor);
      [DllImport(gdidll)]
      public static extern UInt32 SetTextColor(IntPtr hDC, int uiColor);
      [DllImport(gdidll)]
      public static extern UInt32 SetBackColor(IntPtr hDC, int uiColor);
      [DllImport(gdidll)]
      public static extern IntPtr CreatePen(int fnPenStyle, int nWidth, int crColor);
      [DllImport(userdll)]
      public static extern int FillRect(IntPtr hDC, ref NativeWindowCommon.RECT lprc, IntPtr hbr);


      [DllImport(gdidll)]
      public static extern int SelectObject(IntPtr hDC, IntPtr hObject);

      /// <summary>
      /// Summary description for Class1.
      /// </summary>
      [StructLayout(LayoutKind.Sequential)]
      public struct SIZE
      {
         public int cx;
         public int cy;
      }

      [DllImport(gdidll, CharSet = CharSet.Unicode)]
      public static extern bool GetTextExtentPoint32(IntPtr hdc, string str, int len, out SIZE size);

      [DllImport(gdidll, CharSet = CharSet.Unicode)]
      public static extern bool GetTextMetrics(IntPtr hdc, out TEXTMETRIC lptm);

      [DllImport(userdll)]
      [return: MarshalAs(UnmanagedType.Bool)]
      public static extern bool RedrawWindow(IntPtr hWnd, IntPtr lprcUpdate, IntPtr hrgnUpdate, RedrawWindowFlags flags);

      [DllImport(userdll)]
      public static extern bool GetCaretPos(out Point lpPoint);

      [DllImport("User32.Dll")]
      public static extern long SetCursorPos(int x, int y);

      [DllImport(userdll)]
      public extern static bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

      [DllImport("Kernel32.dll")]
      public extern static int GetLastError();

      [DllImport(userdll)]
      public extern static int CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hwnd, uint msg, uint wParam, int lParam);

#if PocketPC
      [DllImport(userdll)]
      public static extern bool DrawEdge(IntPtr hdc, ref RECT rc, uint edge, uint grfFlags);

      [DllImport(userdll)]
      public static extern bool DrawFocusRect(IntPtr hdc, ref RECT rc);



      public delegate int EnumWindowsDelegate(IntPtr hwnd, int lParam);

      [DllImport(userdll)]
      public static extern bool EnumWindows([MarshalAs(UnmanagedType.FunctionPtr)]EnumWindowsDelegate lpEnumFunc, int lParam);

      [DllImport(userdll)]
      public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

      [DllImport(userdll)]
      public static extern int SystemParametersInfo(uint uiAction, uint uiParam, StringBuilder pvParam, uint fWiniIni);
      public const uint SPI_GETPLATFORMTYPE = 257;
#endif

      public static int MakeLong(int LoWord, int HiWord)
      {
         return (HiWord << 16) | (LoWord & 0xffff);
      }

      static IntPtr MakeLParam(int LoWord, int HiWord)
      {
         return (IntPtr)((HiWord << 16) | (LoWord & 0xffff));
      }

      static public int HiWord(int number)
      {
         if ((number & 0x80000000) == 0x80000000)
            return (number >> 16);
         else
            return (number >> 16) & 0xffff;
      }

      static public int LoWord(int number)
      {
         return number & 0xffff;
      }

      [DllImport(userdll, CharSet = CharSet.Auto)]
      static extern int TrackMouseEvent(ref TRACKMOUSEEVENT lpEventTrack);

      [StructLayout(LayoutKind.Sequential)]
      public struct TRACKMOUSEEVENT
      {
         public UInt32 cbSize;
         public UInt32 dwFlags;
         public IntPtr hwndTrack;
         public UInt32 dwHoverTime;
      }

      [DllImport("uxtheme.dll", CharSet = CharSet.Auto)]
      public static extern bool IsThemeActive();

      // JPN: IME support
#if !PocketPC
      const String imm32dll = "imm32.dll";
#else 
      const String imm32dll = "coredll.dll";
#endif
      [DllImport(imm32dll)]
      public static extern int ImmGetContext(IntPtr hWnd);
      [DllImport(imm32dll)]
      public static extern bool ImmReleaseContext(IntPtr hWnd, int hIMC);
      [DllImport(imm32dll, CharSet = CharSet.Unicode)]
      public static extern int ImmGetCompositionString(int hIMC, uint dwIndex, System.Text.StringBuilder lpBuf, uint dwBufLen);
      [DllImport(imm32dll)]
      public static extern bool ImmGetOpenStatus(int hIMC);
      [DllImport(imm32dll)]
      public static extern bool ImmSetOpenStatus(int hIMC, bool fOpen);
      [DllImport(imm32dll)]
      public static extern bool ImmGetConversionStatus(int hIMC, out uint lpfdwConversion, out uint lpfdwSentence);
      [DllImport(imm32dll)]
      public static extern bool ImmSetConversionStatus(int hIMC, uint fdwConversion, uint fdwSentence);

      public const int WM_IME_STARTCOMPOSITION = 0x010D;
      public const int WM_IME_ENDCOMPOSITION = 0x010E;
      public const int WM_IME_COMPOSITION = 0x010F;
      public const int WM_IME_NOTIFY = 0x0282;
      public const int WM_REFLECT_WM_COMMAND = 0x2111;
      public const int GCS_COMPSTR = 0x0008;
      public const int GCS_RESULTREADSTR = 0x0200;
      public const int GCS_RESULTSTR = 0x0800;

#if !PocketPC
        [DllImport("kernel32.dll", EntryPoint = "OutputDebugStringW", CharSet = CharSet.Unicode)]
      public static extern int OutputDebugString(string msg);
#endif

      [DllImport(userdll)]
      public extern static bool LockWindowUpdate(IntPtr hWnd);

      [DllImport("User32.dll")]
      public static extern IntPtr LoadCursorFromFile(String str);

      [StructLayout(LayoutKind.Sequential)]
      public class BITMAPINFO
      {
         public Int32 biSize;
         public Int32 biWidth;
         public Int32 biHeight;
         public Int16 biPlanes;
         public Int16 biBitCount;
         public Int32 biCompression;
         public Int32 biSizeImage;
         public Int32 biXPelsPerMeter;
         public Int32 biYPelsPerMeter;
         public Int32 biClrUsed;
         public Int32 biClrImportant;
         public Int32 colors;
      };

      [DllImport(gdidll)]
      public static extern IntPtr CreateDIBSection(IntPtr hdc, [In, MarshalAs(UnmanagedType.LPStruct)]BITMAPINFO pbmi, uint iUsage, out IntPtr ppvBits, IntPtr hSection, uint dwOffset);
      [DllImport(kerneldll)]
      public static extern bool RtlMoveMemory(IntPtr dest, IntPtr source, int dwcount);
      [DllImport("comctl32.dll")]
      public static extern bool ImageList_Add(IntPtr hImageList, IntPtr hBitmap, IntPtr hMask);

      [DllImport(userdll)]
      public static extern bool ValidateRect(IntPtr hWnd, IntPtr lpRect);

      [DllImport(userdll, CharSet = CharSet.Unicode)]
      public static extern int GetWindowText(IntPtr hWnd, StringBuilder strText, int maxCount);

      [DllImport(userdll, CharSet = CharSet.Unicode)]
      public static extern int GetWindowTextLength(IntPtr hWnd);

      [DllImport(userdll)]
      public static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

      public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

      [DllImport(userdll)]
      public static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

      public const int VK_ESCAPE = 0x1B;
      public const int VK_LEFT = 0x25;
      public const int VK_F4 = 0x73;
      public const int VK_RIGHT = 0x27;  
      public const int VK_INSERT = 0x2D;

      #region SendInput stuff
      public const int INPUT_KEYBOARD = 1;
      public const int WH_KEYBOARD = 2;
      public const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
      public const uint KEYEVENTF_KEYUP = 0x0002;
      public const uint KEYEVENTF_UNICODE = 0x0004;
      public const uint KEYEVENTF_SCANCODE = 0x0008;

      public struct INPUT
      {
         public int type;
         public InputUnion u;
      }

      [StructLayout(LayoutKind.Explicit)]
      public struct InputUnion
      {
         [FieldOffset(0)]
         public MOUSEINPUT mi;
         [FieldOffset(0)]
         public KEYBDINPUT ki;
         [FieldOffset(0)]
         public HARDWAREINPUT hi;
      }

      [StructLayout(LayoutKind.Sequential)]
      public struct MOUSEINPUT
      {
         public int dx;
         public int dy;
         public uint mouseData;
         public uint dwFlags;
         public uint time;
         public IntPtr dwExtraInfo;
      }

      [StructLayout(LayoutKind.Sequential)]
      public struct KEYBDINPUT
      {
         /*Virtual Key code.  Must be from 1-254.  If the dwFlags member specifies KEYEVENTF_UNICODE, wVk must be 0.*/
         public ushort wVk;
         /*A hardware scan code for the key. If dwFlags specifies KEYEVENTF_UNICODE, wScan specifies a Unicode character which is to be sent to the foreground application.*/
         public ushort wScan;
         /*Specifies various aspects of a keystroke.  See the KEYEVENTF_ constants for more information.*/
         public uint dwFlags;
         /*The time stamp for the event, in milliseconds. If this parameter is zero, the system will provide its own time stamp.*/
         public uint time;
         /*An additional value associated with the keystroke. Use the GetMessageExtraInfo function to obtain this information.*/
         public IntPtr dwExtraInfo;
      }

      [StructLayout(LayoutKind.Sequential)]
      public struct HARDWAREINPUT
      {
         public uint uMsg;
         public ushort wParamL;
         public ushort wParamH;
      }

      [DllImport("user32.dll")]
      public static extern IntPtr GetMessageExtraInfo();

      [DllImport("user32.dll", SetLastError = true)]
      public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);
      #endregion SendInput stuff

      public const int EM_SETMARGINS  = 0x00D3;
      public const int EC_LEFTMARGIN = 0x0001;
      public const int EC_RIGHTMARGIN = 0x0002;

      [DllImport(userdll, SetLastError = true)]
      public static extern bool SetProcessDPIAware();

      [System.Runtime.InteropServices.DllImport("SHCore.dll", SetLastError = true)]
      public static extern bool SetProcessDpiAwareness(PROCESS_DPI_AWARENESS awareness);
      public enum PROCESS_DPI_AWARENESS
      {
         Process_DPI_Unaware = 0,
         Process_System_DPI_Aware = 1,
         Process_Per_Monitor_DPI_Aware = 2
      }

      [DllImport(userdll)]
      public static extern bool ShowCaret(IntPtr hWnd);

      [DllImport(userdll)]
      public static extern bool HideCaret(IntPtr hWnd);

   } // NativeWindowCommon

   //==================== CUSTOM DRAW ==========================================

   public sealed class NativeCustomDraw : NativeWindowCommon
   {
      public const int NM_CUSTOMDRAW = 0 - 12;

      // custom draw return flags
      // values under 0x00010000 are reserved for global custom draw values.
      // above that are for specific controls
      public const int CDRF_DODEFAULT = 0x00000000;
      public const int CDRF_NEWFONT = 0x00000002;
      public const int CDRF_SKIPDEFAULT = 0x00000004;


      public const int CDRF_NOTIFYPOSTPAINT = 0x00000010;
      public const int CDRF_NOTIFYITEMDRAW = 0x00000020;
      public const int CDRF_NOTIFYSUBITEMDRAW = 0x00000020; // flags are the same, we can distinguish by context
      public const int CDRF_NOTIFYPOSTERASE = 0x00000040;

      // drawstage flags
      // values under 0x00010000 are reserved for global custom draw values.
      // above that are for specific controls
      public const int CDDS_PREPAINT = 0x00000001;
      public const int CDDS_POSTPAINT = 0x00000002;
      public const int CDDS_PREERASE = 0x00000003;
      public const int CDDS_POSTERASE = 0x00000004;
      // the 0x000010000 bit means it's individual item specific
      public const int CDDS_ITEM = 0x00010000;
      public const int CDDS_ITEMPREPAINT = (CDDS_ITEM | CDDS_PREPAINT);
      public const int CDDS_ITEMPOSTPAINT = (CDDS_ITEM | CDDS_POSTPAINT);
      public const int CDDS_ITEMPOSTERASE = (CDDS_ITEM | CDDS_POSTERASE);

      [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
      public struct NMCUSTOMDRAW
      {
         public NMHDR hdr;
         public int dwDrawStage;
         public IntPtr hdc;
         public RECT rc;
         public /*DWORD_PTR*/ IntPtr dwItemSpec;  // this is control specific, but it's how to specify an item.  valid only with CDDS_ITEM bit set
         public int uItemState;
         public int lItemlParam;
      }
   }
   public sealed class NativeScroll : NativeWindowCommon
   {
      public const int SB_LINEUP = 0;
      public const int SB_LINELEFT = 0;
      public const int SB_LINEDOWN = 1;
      public const int SB_LINERIGHT = 1;
      public const int SB_PAGEUP = 2;
      public const int SB_PAGELEFT = 2;
      public const int SB_PAGEDOWN = 3;
      public const int SB_PAGERIGHT = 3;
      public const int SB_THUMBPOSITION = 4;
      public const int SB_THUMBTRACK = 5;
      public const int SB_TOP = 6;
      public const int SB_LEFT = 6;
      public const int SB_BOTTOM = 7;
      public const int SB_RIGHT = 7;
      public const int SB_ENDSCROLL = 8;
      public const int SB_HORZ = 0;
      public const int SB_VERT = 1;
      public const int SB_CTL = 2;
      public const int SB_BOTH = 3;

      public const int SIF_RANGE = 0x0001;
      public const int SIF_PAGE = 0x0002;
      public const int SIF_POS = 0x0004;
      public const int SIF_DISABLENOSCROLL = 0x0008;
      public const int SIF_TRACKPOS = 0x0010;
      public const int SIF_ALL = (SIF_RANGE | SIF_PAGE | SIF_POS | SIF_TRACKPOS);


      [DllImport(userdll)]
      static public extern bool EnableScrollBar(System.IntPtr hWnd, uint wSBflags, uint wArrows);

      [DllImport(userdll)]
      static public extern bool GetScrollInfo(System.IntPtr hwnd, int fnBar, ref SCROLLINFO lpsi);

      [DllImport(userdll)]
      static public extern int SetScrollInfo(IntPtr hwnd, int fnBar, ref SCROLLINFO
         lpsi, bool fRedraw);


      [DllImport(userdll)]
      static public extern int SetScrollRange(System.IntPtr hWnd, int nBar, int nMinPos, int nMaxPos, bool bRedraw);

      [DllImport(userdll)]
      static public extern int SetScrollPos(System.IntPtr hWnd, int nBar, int nPos, bool bRedraw);

      [DllImport(userdll)]
      static public extern int GetScrollPos(System.IntPtr hWnd, int nBar);

#if !PocketPC
      [DllImport(userdll)]
      static public extern bool ShowScrollBar(IntPtr hWnd, int wBar, bool bShow);
#else
      static public bool ShowScrollBar(IntPtr hWnd, int wBar, bool bShow)
      {
          int dwStyle = GetWindowLong(hWnd, GWL_STYLE);
          int dwStyleOrig = dwStyle;
          if (bShow)
               dwStyle |= ((wBar == NativeScroll.SB_HORZ) ? WS_HSCROLL : WS_VSCROLL);
          else
              dwStyle &= ~((wBar == NativeScroll.SB_HORZ) ? WS_HSCROLL : WS_VSCROLL);

         if(dwStyle != dwStyleOrig)
            return (SetWindowLong(hWnd, GWL_STYLE, dwStyle) != 0);
         return true;
      }
#endif

      [DllImport(userdll)]
      public static extern int ScrollWindowEx(IntPtr hWnd, int dx, int dy, IntPtr scrollRect, IntPtr clipRect, IntPtr hrgn,
         ref RECT updateRect, uint flags);

      [DllImport(userdll)]
      public static extern int ScrollWindowEx(IntPtr hWnd, int dx, int dy, ref RECT scrollRect, IntPtr clipRect, IntPtr hrgn,
         IntPtr updateRect, uint flags);

      [StructLayout(LayoutKind.Sequential)]
      public struct SCROLLINFO
      {
         public int cbSize;
         public int fMask;
         public int nMin;
         public int nMax;
         public int nPage;
         public int nPos;
         public int nTrackPos;
      }

      public const int SW_SCROLLCHILDREN = 0x0001;
      public const int SW_INVALIDATE = 0x0002;
      public const int SW_ERASE = 0x0004;
      public const int SW_SMOOTHSCROLL = 0x0010;

   }


   /// <summary>
   /// NativeHeader class
   /// </summary>
   public sealed class NativeHeader : NativeWindowCommon
   {
      /// <summary>
      /// Constants: Window class name
      /// </summary>
      public const string WC_HEADER = "SysHeader32";

      /// <summary>
      /// Constants: Control styles
      /// </summary>
      public const int HDS_HORZ = 0x00000000;
      public const int HDS_BUTTONS = 0x00000002;
      public const int HDS_HOTTRACK = 0x00000004;
      public const int HDS_HIDDEN = 0x00000008;
      public const int HDS_DRAGDROP = 0x00000040;
      public const int HDS_FULLDRAG = 0x00000080;
      public const int HDS_FILTERBAR = 0x00000100;
      public const int HDS_FLAT = 0x00000200;

      /// <summary>
      /// Constants: Control specific messages
      /// </summary>
      public const int HDM_FIRST = 0x00001200;
      public const int HDM_GETITEMCOUNT = HDM_FIRST + 0;
      public static readonly int HDM_INSERTITEM;
      public const int HDM_DELETEITEM = HDM_FIRST + 2;
      public static readonly int HDM_GETITEM;
      public static readonly int HDM_SETITEM;
      public const int HDM_LAYOUT = HDM_FIRST + 5;
      public const int HDM_HITTEST = HDM_FIRST + 6;
      public const int HDM_GETITEMRECT = HDM_FIRST + 7;
      public const int HDM_SETIMAGELIST = HDM_FIRST + 8;
      public const int HDM_GETIMAGELIST = HDM_FIRST + 9;
      public const int HDM_ORDERTOINDEX = HDM_FIRST + 15;
      public const int HDM_CREATEDRAGIMAGE = HDM_FIRST + 16;
      public const int HDM_GETORDERARRAY = HDM_FIRST + 17;
      public const int HDM_SETORDERARRAY = HDM_FIRST + 18;
      public const int HDM_SETHOTDIVIDER = HDM_FIRST + 19;
      public const int HDM_SETBITMAPMARGIN = HDM_FIRST + 20;
      public const int HDM_GETBITMAPMARGIN = HDM_FIRST + 21;
      public const int HDM_SETUNICODEFORMAT = CCM_SETUNICODEFORMAT;
      public const int HDM_GETUNICODEFORMAT = CCM_GETUNICODEFORMAT;
      public const int HDM_SETFILTERCHANGETIMEOUT = HDM_FIRST + 22;
      public const int HDM_EDITFILTER = HDM_FIRST + 23;
      public const int HDM_CLEARFILTER = HDM_FIRST + 24;

      /// <summary>
      /// Constants: Control specific notifications
      /// </summary>
      public const int HDN_FIRST = 0 - 300;
      public const int HDN_LAST = 0 - 399;
      public const int NM_FIRST = 0;

      public static readonly int HDN_ITEMCHANGING;
      public static readonly int HDN_ITEMCHANGED;
      public static readonly int HDN_ITEMCLICK;
      public static readonly int HDN_ITEMDBLCLICK;
      public static readonly int HDN_DIVIDERDBLCLICK;
      public static readonly int HDN_BEGINTRACK;
      public static readonly int HDN_ENDTRACK;
      public static readonly int HDN_TRACK;
      public static readonly int HDN_GETDISPINFO;

      public const int HDN_BEGINDRAG = HDN_FIRST - 10;
      public const int HDN_ENDDRAG = HDN_FIRST - 11;
      public const int HDN_FILTERCHANGE = HDN_FIRST - 12;
      public const int HDN_FILTERBTNCLICK = HDN_FIRST - 13;


      /// <summary>
      /// Constants: HDITEM mask
      /// </summary>
      public const int HDI_WIDTH = 0x00000001;
      public const int HDI_HEIGHT = HDI_WIDTH;
      public const int HDI_TEXT = 0x00000002;
      public const int HDI_FORMAT = 0x00000004;
      public const int HDI_LPARAM = 0x00000008;
      public const int HDI_BITMAP = 0x00000010;
      public const int HDI_IMAGE = 0x00000020;
      public const int HDI_DI_SETITEM = 0x00000040;
      public const int HDI_ORDER = 0x00000080;
      public const int HDI_FILTER = 0x00000100;

      /// <summary>
      /// Constants: HDITEM fmt
      /// </summary>
      public const int HDF_LEFT = 0x00000000;
      public const int HDF_RIGHT = 0x00000001;
      public const int HDF_CENTER = 0x00000002;
      public const int HDF_JUSTIFYMASK = 0x00000003;
      public const int HDF_RTLREADING = 0x00000004;
      public const int HDF_OWNERDRAW = 0x00008000;
      public const int HDF_STRING = 0x00004000;
      public const int HDF_BITMAP = 0x00002000;
      public const int HDF_BITMAP_ON_RIGHT = 0x00001000;
      public const int HDF_IMAGE = 0x00000800;
      public const int HDF_SORTUP = 0x00000400;
      public const int HDF_SORTDOWN = 0x00000200;

      public const int HHT_NOWHERE = 0x00000001;
      public const int HHT_ONHEADER = 0x00000002;
      public const int HHT_ONDIVIDER = 0x00000004;
      public const int HHT_ONDIVOPEN = 0x00000008;
      public const int HHT_ONFILTER = 0x00000010;
      public const int HHT_ONFILTERBUTTON = 0x00000020;
      public const int HHT_ABOVE = 0x00000100;
      public const int HHT_BELOW = 0x00000200;
      public const int HHT_TORIGHT = 0x00000400;
      public const int HHT_TOLEFT = 0x00000800;

      /// <summary>
      /// Types
      /// </summary>
      [StructLayout(LayoutKind.Sequential)]
      public struct HDHITTESTINFO
      {
         public POINT pt;
         public int flags;
         public int iItem;
      }

      [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
      public struct HDITEM
      {
         public int mask;
         public int cxy;
         [MarshalAs(UnmanagedType.LPTStr)]
         public string lpszText;
         public IntPtr hbm;
         public int cchTextMax;
         public int fmt;
         public int lParam;
         public int iImage;
         public int iOrder;
         public int type;
         public IntPtr pvFilter;
      }

      [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
      public struct HDITEM2
      {
         public int mask;
         public int cxy;
         public IntPtr lpszText;
         public IntPtr hbm;
         public int cchTextMax;
         public int fmt;
         public int lParam;
         public int iImage;
         public int iOrder;
         public int type;
         public IntPtr pvFilter;
      }

      [StructLayout(LayoutKind.Sequential)]
      public struct HDLAYOUT
      {
         public IntPtr prc;   // RECT*
         public IntPtr pwpos; // WINDOWPOS*
      }

      [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
      public struct HDTEXTFILTER
      {
         [MarshalAs(UnmanagedType.LPTStr)]
         public string lpszText;
         public int cchTextMax;
      }

      [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
      public struct NMHDDISPINFO
      {
         public NMHDR hdr;
         public int iItem;
         public int mask;
         [MarshalAs(UnmanagedType.LPTStr)]
         public string lpszText;
         public int cchTextMax;
         public int iImage;
         public int lParam;
      }

      [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
      public struct NMHDFILTERBTNCLICK
      {
         public NMHDR hdr;
         public int iItem;
         public RECT rc;
      }

      [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
      public struct NMHEADER
      {
         public NMHDR hdr;
         public int iItem;
         public int iButton;
         public IntPtr pitem;
      }


      /// <summary>
      /// Static constructor
      /// </summary>
      static NativeHeader()
      {
         if (IsSysCharSetAnsi())
         {
            HDM_INSERTITEM = HDM_FIRST + 1;
            HDM_GETITEM = HDM_FIRST + 3;
            HDM_SETITEM = HDM_FIRST + 4;

            HDN_ITEMCHANGING = HDN_FIRST - 0;
            HDN_ITEMCHANGED = HDN_FIRST - 1;
            HDN_ITEMCLICK = HDN_FIRST - 2;
            HDN_ITEMDBLCLICK = HDN_FIRST - 3;
            HDN_DIVIDERDBLCLICK = HDN_FIRST - 5;
            HDN_BEGINTRACK = HDN_FIRST - 6;
            HDN_ENDTRACK = HDN_FIRST - 7;
            HDN_TRACK = HDN_FIRST - 8;
            HDN_GETDISPINFO = HDN_FIRST - 9;
         }
         else
         {
            HDM_INSERTITEM = HDM_FIRST + 10;
            HDM_GETITEM = HDM_FIRST + 11;
            HDM_SETITEM = HDM_FIRST + 12;

            HDN_ITEMCHANGING = HDN_FIRST - 20;
            HDN_ITEMCHANGED = HDN_FIRST - 21;
            HDN_ITEMCLICK = HDN_FIRST - 22;
            HDN_ITEMDBLCLICK = HDN_FIRST - 23;
            HDN_DIVIDERDBLCLICK = HDN_FIRST - 25;
            HDN_BEGINTRACK = HDN_FIRST - 26;
            HDN_ENDTRACK = HDN_FIRST - 27;
            HDN_TRACK = HDN_FIRST - 28;
            HDN_GETDISPINFO = HDN_FIRST - 29;
         }
      }

      /// <summary>
      /// Helpers
      /// </summary>
      [DllImport(userdll, CharSet = CharSet.Auto)]
      private static extern int SendMessage(IntPtr hWnd, int msg, int wParam,
                                   ref HDITEM hdi);

      [DllImport(userdll, CharSet = CharSet.Auto)]
      private static extern int SendMessage(IntPtr hWnd, int msg, int wParam,
                                   ref RECT rc);

      [DllImport(userdll, CharSet = CharSet.Auto)]
      public static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam,
                               IntPtr lParam);

      [DllImport(userdll, CharSet = CharSet.Auto)]
      private static extern int SendMessage(IntPtr hWnd, int msg, int wParam,
                                   ref HDLAYOUT lParam);

      [DllImport(userdll, CharSet = CharSet.Auto)]
      private static extern int SendMessage(IntPtr hWnd, int msg, int wParam,
                                   ref HDHITTESTINFO lParam);

      [DllImport(userdll, CharSet = CharSet.Auto)]
      private static extern int SendMessage(IntPtr hWnd, int msg, int cItems,
                                   int[] aOrders);


      /// <summary>
      /// Operations
      /// </summary>
      public static int GetItemCount(IntPtr hwnd)
      {
         Debug.Assert(hwnd != IntPtr.Zero);

         return SendMessage(hwnd, HDM_GETITEMCOUNT, 0, 0);
      }

      public static int InsertItem(IntPtr hWnd, int index, ref HDITEM hdi)
      {
         Debug.Assert(hWnd != IntPtr.Zero);

         return SendMessage(hWnd, HDM_INSERTITEM, index, ref hdi);
      }

      public static bool DeleteItem(IntPtr hWnd, int index)
      {
         Debug.Assert(hWnd != IntPtr.Zero);

         return SendMessage(hWnd, HDM_DELETEITEM, index, 0) != 0;
      }

      public static bool GetItem(IntPtr hWnd, int index, ref HDITEM hdi)
      {
         Debug.Assert(hWnd != IntPtr.Zero);

         return SendMessage(hWnd, HDM_GETITEM, index, ref hdi) != 0;
      }

      public static bool SetItem(IntPtr hWnd, int index, ref HDITEM hdi)
      {
         Debug.Assert(hWnd != IntPtr.Zero);

         return SendMessage(hWnd, HDM_SETITEM, index, ref hdi) != 0;
      }

      public static bool GetItemRect(IntPtr hWnd, int index, out RECT rect)
      {
         Debug.Assert(hWnd != IntPtr.Zero);

         rect = new RECT();

         return SendMessage(hWnd, HDM_GETITEMRECT, index, ref rect) != 0;
      }

      public static IntPtr GetImageList(IntPtr hWnd)
      {
         Debug.Assert(hWnd != IntPtr.Zero);

         return SendMessage(hWnd, HDM_GETIMAGELIST, 0, IntPtr.Zero);
      }

      public static IntPtr SetImageList(IntPtr hWnd, IntPtr himl)
      {
         Debug.Assert(hWnd != IntPtr.Zero);

         return SendMessage(hWnd, HDM_SETIMAGELIST, 0, himl);
      }

      public static IntPtr CreateDragImage(IntPtr hWnd, int index)
      {
         Debug.Assert(hWnd != IntPtr.Zero);

         return SendMessage(hWnd, HDM_CREATEDRAGIMAGE, index, IntPtr.Zero);
      }

      public static bool Layout(IntPtr hWnd, ref HDLAYOUT layout)
      {
         Debug.Assert(hWnd != IntPtr.Zero);

         return SendMessage(hWnd, HDM_LAYOUT, 0, ref layout) != 0;
      }

      public static int HitTest(IntPtr hWnd, ref HDHITTESTINFO hdhti)
      {
         Debug.Assert(hWnd != IntPtr.Zero);

         return SendMessage(hWnd, HDM_HITTEST, 0, ref hdhti);
      }

      public static int GetBitmapMargin(IntPtr hWnd)
      {
         Debug.Assert(hWnd != IntPtr.Zero);

         return SendMessage(hWnd, HDM_GETBITMAPMARGIN, 0, 0);
      }

      public static int SetBitmapMargin(IntPtr hWnd, int iWidth)
      {
         Debug.Assert(hWnd != IntPtr.Zero && iWidth >= 0);

         return SendMessage(hWnd, HDM_SETBITMAPMARGIN, iWidth, 0);
      }

      public static int SetHotDivider(IntPtr hWnd, bool flag, int dwInputValue)
      {
         Debug.Assert(hWnd != IntPtr.Zero);

         return SendMessage(hWnd, HDM_SETHOTDIVIDER, flag, dwInputValue);
      }

      public static int OrderToIndex(IntPtr hWnd, int iOrder)
      {
         Debug.Assert(hWnd != IntPtr.Zero);

         return SendMessage(hWnd, HDM_ORDERTOINDEX, iOrder, 0);
      }

      public static bool GetOrderArray(IntPtr hWnd, out int[] aOrders)
      {
         Debug.Assert(hWnd != IntPtr.Zero);

         int cItems = GetItemCount(hWnd);
         aOrders = new int[cItems];

         return SendMessage(hWnd, HDM_GETORDERARRAY, cItems, aOrders) != 0;
      }

      public static bool SetOrderArray(IntPtr hWnd, int[] aOrders)
      {
         Debug.Assert(hWnd != IntPtr.Zero);

         return SendMessage(hWnd, HDM_SETORDERARRAY, aOrders.Length, aOrders) != 0;
      }

      public static bool GetUnicodeFormat(IntPtr hWnd)
      {
         Debug.Assert(hWnd != IntPtr.Zero);

         // ???

         return false;
      }

      public static bool SetUnicodeFormat(IntPtr hWnd, bool fUnicode)
      {
         Debug.Assert(hWnd != IntPtr.Zero);

         // ???

         return false;
      }

      public static int ClearAllFilters(IntPtr hWnd)
      {
         Debug.Assert(hWnd != IntPtr.Zero);

         // ???

         return 0;
      }

      public static int ClearFilter(IntPtr hWnd, int index)
      {
         Debug.Assert(hWnd != IntPtr.Zero);

         // ???

         return 0;
      }

      public static int EditFilter(IntPtr hWnd, int i, bool fDiscardChanges)
      {
         Debug.Assert(hWnd != IntPtr.Zero);

         // ???

         return 0;
      }

      public static int SetFilterChangeTimeout(IntPtr hWnd, int i)
      {
         Debug.Assert(hWnd != IntPtr.Zero);

         // ???

         return 0;
      }

   } // NativeHeader class

   /// <summary>
   /// enum for win32 errors' code
   /// </summary>
   public enum Win32ErrorCode : int
   {
      ERROR_FILE_NOT_FOUND = 2,
      ERROR_ACCESS_DENIED = 5
   };

   public partial class user32
   {
#if !PocketPC
      const String userdll = "user32.dll";
      const String kerneldll = "Kernel32.dll";
#else
      const String userdll = "coredll.dll";
      const String kerneldll = "coredll.dll";
#endif
      // DesiredAccess Values
      public const uint QUERY = 0;
      public const uint READ = 0x80000000;
      public const uint WRITE = 0x40000000;

      // FlagsAndAttributes Values
      public const uint ARCHIVE = 0x00000020;
      public const uint HIDDEN = 0x00000002;
      public const uint NORMAL = 0x00000080;
      public const uint READONLY = 0x00000001;
      public const uint SYSTEM = 0x00000004;
      public const uint TEMP = 0x00000100;

#if !PocketPC
      [DllImport(userdll)]
      public static extern int GetKeyboardState(byte[] pbKeyState);
#else
      [DllImport(userdll)]
      public static extern short GetKeyState(int key);

      public static int GetKeyboardState(byte[] pbKeyState)
      {
         for(int i=0; i<256; i++)
         {
            short s = user32.GetKeyState(i);
            pbKeyState[i] =  (byte)((s & (ushort)0xff00 >> 8) | (s & (byte)0xff));
         }
         return 0;
      }
#endif

      [DllImport(userdll, SetLastError = true)]
      public static extern int ToAscii(
         uint uVirtKey, // virtual-key code 
         uint uScanCode, // scan code 
         byte[] lpKeyState, // key-state array 
         System.Text.StringBuilder lpChar, // buffer for translated key 
         uint flags // active-menu flag 
      );

      [DllImport(userdll, SetLastError = true)]
      [return: MarshalAs(UnmanagedType.Bool)]
      public static extern bool SetFileTime(
        IntPtr hFile,                // File Handle
        ref long lpCreationTime,     // Creation time
        ref long lpLastAccessTime,   // Last access time
        ref long lpLastWriteTime);   // Last write time

      [DllImport(userdll, SetLastError = true)]
      [return: MarshalAs(UnmanagedType.Bool)]
      public static extern bool CloseHandle(
          IntPtr hHandle             // Handle to be closed
        );

      // pipes functions and defines
      [DllImport(kerneldll, SetLastError = true)]
      public static extern SafeFileHandle CreateFile(
         string lpFileName,               // File name
         uint dwDesiredAccess,            // Access Can be 0 (Query), 0x80000000 (Read) or 0x40000000 (Write)
         FileShare dwShareMode,           // Share more. Can be 0x00000001 (Read), 0x00000002 (Write)
         IntPtr lpSecurityAttributes,     // Ignored. Set to NULL
         FileMode dwCreationDispostion,   // Action to take Can be 1 (Create new) , 2  (Create always)),3 
                                          // (Open Existing) ,4  (Open alwats) or 5 (Truncate existing)
         uint dwFlagsAndAttributes,       // File attr and flags Can be 0x00000020  (archive), 
                                          // 0x00000002 (Hidden), 0x00000080 (Normal), 0x00000001 
                                          // (Read Only), 0x00000004 (System) or 0x00000100 (Temp)
         IntPtr hTemplateFile);           // Ingnored.



      // The SendARP function sends an Address Resolution Protocol (ARP) request 
      // to obtain the physical address that corresponds to the specified destination IPv4 address.
      [DllImport("iphlpapi.dll")]
      public extern static int SendARP(int DestIP, int SrcIP, byte[] pMacAddr, ref uint PhyAddrLen);

      [DllImport(kerneldll, SetLastError = true)]
      public static extern IntPtr LoadLibrary(string lpFileName);


#if !PocketPC

      [DllImport("Kernel32.dll", SetLastError = true)]
      public extern static SafeFileHandle CreateNamedPipe(string name, UInt32 openMode, UInt32 pipeMode,
          UInt32 maxInstances, UInt32 outBufferSize, UInt32 inBufferSize, UInt32 defaultTimeOut,
          IntPtr securityAttributes);

      [DllImport("Kernel32.dll", SetLastError = true)]
      public extern static Int32 ConnectNamedPipe(SafeFileHandle namedPipe, IntPtr overlapped);

      public const UInt32 GENERIC_READ = 0x80000000;
      public const UInt32 GENERIC_WRITE = 0x40000000;
      public const int OPEN_EXISTING = 3;
      public const int PIPE_ACCESS_INBOUND = 0x00000001;
      public const int PIPE_TYPE_BYTE = 0x00000000;
      public const int PIPE_READMODE_BYTE = 0x00000000;
      public const int PIPE_WAIT = 0x00000000;
      public const int FILE_FLAG_OVERLAPPED = 0x40000000;


#endif

   }

   public enum ENNOTIFY
   {
      EN_SETFOCUS = 0x0100,
      EN_KILLFOCUS = 0x0200,
      EN_CHANGE = 0x0300,
      EN_UPDATE = 0x0400,
      EN_ERRSPACE = 0x0500,
      EN_MAXTEXT = 0x0501,
      EN_HSCROLL = 0x0601,
      EN_VSCROLL = 0x0602
   };
}
