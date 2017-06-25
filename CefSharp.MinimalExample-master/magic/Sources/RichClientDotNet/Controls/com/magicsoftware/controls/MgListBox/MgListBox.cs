using System.Windows.Forms;
using System;
using com.magicsoftware.win32;
#if !PocketPC
using System.ComponentModel;
using com.magicsoftware.controls.designers;
using System.Drawing;
using Controls.com.magicsoftware.controls.PropertyInterfaces;
#else
using System.Runtime.InteropServices;
using NativeWindowCommon = com.magicsoftware.win32.NativeWindowCommon;
#endif

namespace com.magicsoftware.controls
{

#if !PocketPC
   [Designer(typeof(ListControlDesigner))]
   [ToolboxBitmap(typeof(ListBox))]
   public class MgListBox : ListBox, IRightToLeftProperty, IBorderStyleProperty, IChoiceControl, ISetSpecificControlPropertiesForFormDesigner, IItemsCollection
#else
   public class MgListBox : ListBox, IBorderStyleProperty
#endif
   {
      public MgListBox()
         : base()
      {
#if PocketPC
         subclassListbox();
#endif
#if !PocketPC
         base.RightToLeft = RightToLeft.No;
#endif
      }

      public void AddRange(Object[] items)
      {
#if !PocketPC
         if (items != null)
            Items.AddRange(items);
#else
         foreach (Object item in items)
            Items.Add(item);
#endif
      }

#if PocketPC
      private BorderStyle borderStyle;
      public BorderStyle BorderStyle 
      { 
        get
        {
            return borderStyle;
        }
        set 
        { 
           borderStyle = value;
           if (borderStyle == BorderStyle.None)
             hideListBoxBorder(this);
         }
       }

      /// <summary>
      /// Hides the border of Listbox Control
      /// </summary>
      /// <param name="listBox"> listbox control object whose border is to be hidden</param>
      internal static void hideListBoxBorder(ListBox listbox)
      {

         IntPtr handle = listbox.Handle;
         int style = NativeWindowCommon.GetWindowLong(handle, NativeWindowCommon.GWL_STYLE);

         style &= ~NativeWindowCommon.WS_BORDER;
         NativeWindowCommon.SetWindowLong(handle, NativeWindowCommon.GWL_STYLE, style);
         NativeWindowCommon.SetWindowPos(handle, IntPtr.Zero, 0, 0, 0, 0, NativeWindowCommon.SWP_NOMOVE | NativeWindowCommon.SWP_NOSIZE | NativeWindowCommon.SWP_FRAMECHANGED);

      }


      // A delegate for our wndproc
      public delegate int MobileWndProc(IntPtr hwnd, uint msg, uint wParam, int lParam);
      // Original wndproc
      private IntPtr OrigWndProc;
      // object's delegate
      MobileWndProc proc;

      /// <summary> subclass the listbox - replace it's window proce
      /// </summary>
      void subclassListbox()
      {
         proc = new MobileWndProc(WindowProc);
         OrigWndProc = (IntPtr)NativeWindowCommon.SetWindowLong(Handle, NativeWindowCommon.GWL_WNDPROC,
                         Marshal.GetFunctionPointerForDelegate(proc).ToInt32());
      }

      // Our wndproc
      private int WindowProc(IntPtr hwnd, uint msg, uint wParam, int lParam)
      {
         // Call original proc
         int ret = NativeWindowCommon.CallWindowProc(OrigWndProc, hwnd, msg, wParam, lParam);

         // Raise the mouse down event we don't get on Mobile
         if (msg == NativeWindowCommon.WM_LBUTTONDOWN)
         {
            MouseEventArgs mouseEvents = new MouseEventArgs(MouseButtons.Left, 0,
               NativeWindowCommon.LoWord(lParam), NativeWindowCommon.HiWord(lParam), 0);

            this.OnMouseDown(mouseEvents);
         }
         return ret;
      }
#endif


#if !PocketPC
      public void setSpecificControlPropertiesForFormDesigner(Control fromControl)
      {
         ControlUtils.SetSpecificControlPropertiesForFormDesignerForListControl((MgListBox)fromControl, this);      
      }

      public System.Collections.IList ItemsCollection
      {
         get
         {
            return Items;
         }
      }
#endif
   }
}

