using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using com.magicsoftware.win32;

namespace com.magicsoftware.controls
{
   /// <summary>
   /// QCR #781575
   /// 
   /// This class adds on to the functionality provided in System.Windows.Forms.ToolStrip.
   /// From http://blogs.msdn.com/rickbrew/archive/2006/01/09/511003.aspx

   /// </summary>

   public class ToolStripEx : ToolStrip
   {


      /// <summary>

      /// Gets or sets whether the ToolStripEx honors item clicks when its containing form does

      /// not have input focus.

      /// </summary>

      /// <remarks>

      /// Default value is false, which is the same behavior provided by the base ToolStrip class.

      /// </remarks>
      public bool ClickThrough { get; set; }

 
      protected override void WndProc(ref Message m)
      {

         base.WndProc(ref m);


         if (this.ClickThrough &&

             m.Msg == NativeWindowCommon.WM_MOUSEACTIVATE &&

             m.Result == (IntPtr)NativeWindowCommon.MA_ACTIVATEANDEAT)
         {

            m.Result = (IntPtr)NativeWindowCommon.MA_ACTIVATE;

         }

      }

   }



   
}

