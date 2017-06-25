using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

using com.magicsoftware.win32;
using com.magicsoftware.mobilestubs;

namespace com.magicsoftware.controls.utils
{
   public delegate void NCMouseEventHandler(object sender, NCMouseEventArgs e);

   /// <summary>
   /// event args for non client mouse down events
   /// </summary>
   public class NCMouseEventArgs : MouseEventArgs
   {
      public int HitTest {private set; get;}
      public NCMouseEventArgs(Message m) :
         base(m.Msg == NativeWindowCommon.WM_NCLBUTTONDOWN ? MouseButtons.Left : MouseButtons.Right,1,0,0,1) 
      {
         this.HitTest = m.WParam.ToInt32();
      }
   }
}
