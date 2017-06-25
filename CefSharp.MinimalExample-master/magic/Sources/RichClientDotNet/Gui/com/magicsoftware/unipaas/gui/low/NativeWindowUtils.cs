using System;
using System.Windows.Forms;
using com.magicsoftware.win32;
using System.Drawing;
using System.Runtime.InteropServices;
using com.magicsoftware.unipaas.gui;
using com.magicsoftware.util;
using com.magicsoftware.controls;


/// This is the place to hold all our win32 NativeWindow wrappers that we need
/// to handle win32 messeges sent to window

namespace com.magicsoftware.unipaas.gui.low
{  
   
   /// <summary>
   /// This is a wrapper for MDIClient window
   /// We are using it to get scroll messages from it which are not caught by any other way
   /// </summary>
   internal class MDIClientNativeWindow : NativeWindow
   {

      //internal readonly Form mdiFrame;
      internal readonly MdiClient mdiClient;

      internal MDIClientNativeWindow(Form form)
      {
         mdiClient = (MdiClient)form.Controls[0];
         AssignHandle(mdiClient.Handle);
         
      }

      protected override void WndProc(ref Message m)
      {
         switch (m.Msg)
         {
            case NativeWindowCommon.WM_VSCROLL:
            case NativeWindowCommon.WM_HSCROLL:
               //keep all FitToMDI children at location 0,0 even after scroll
               foreach (var item in mdiClient.MdiChildren)
               {
                  TagData tagData = item.Tag as TagData;
                  if (tagData != null && (tagData.WindowType == WindowType.FitToMdi || tagData.IsMDIClientForm))
                  {
                     item.Location = new Point();
                  }
               }
               mdiClient.Invalidate();
               break;

            case NativeWindowCommon.WM_PAINT:
               using (Graphics gr = Graphics.FromHwnd(mdiClient.Handle))
               {
                  //Paint the mdiClient its children, background image, border etc.
                  ControlRenderer.PaintMgPanel(mdiClient, gr);
                  NativeWindowCommon.ValidateRect(mdiClient.Handle, IntPtr.Zero);
               }
               return;

            default:
               break;
         }
         base.WndProc(ref m);
      }
   }

  


}
