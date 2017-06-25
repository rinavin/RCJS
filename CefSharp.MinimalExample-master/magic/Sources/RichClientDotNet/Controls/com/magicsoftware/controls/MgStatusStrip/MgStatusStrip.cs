using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace com.magicsoftware.controls
{
   /// <summary>
   /// 
   /// 
   /// </summary>
   public class MgStatusStrip : StatusStrip
   {
      /// <summary>
      /// this a workaround for defect 122976 - it is a known proplem for Graphics.Clear on remote desktop
      /// But we are not able to detect what causes this exception we are catching it for now
      /// </summary>
      /// <param name="e"></param>
      protected override void OnPaintBackground(PaintEventArgs e)
      {
         try
         {
            base.OnPaintBackground(e);
         }
         catch (ExternalException ex)
         {
            System.Console.WriteLine("Got Exception " + ex);
         }
      }
   }
}
