using com.magicsoftware.win32;
using System.Windows.Forms;

namespace com.magicsoftware.controls
{
   /// <summary>
   /// Class for button used in expander
   /// </summary>
   public class ExpanderButton : Button
   {
      private bool isExpanded = true;

      /// <summary>
      /// denotes state of expander
      /// </summary>
      public bool IsExpanded
      {
         get
         {
            return isExpanded;
         }
         set
         {
            if (isExpanded != value)
            {
               isExpanded = value;
               Invalidate();
            }
         }
      }

      protected override void OnPaint(PaintEventArgs paintEventArgs)
      {
         base.OnPaint(paintEventArgs);

         // Draw expander
         ExpanderRenderer.RenderExpander(paintEventArgs.Graphics, this.Bounds, IsExpanded, BackColor, ForeColor);
      }

      protected override void WndProc(ref Message message)
      {
         if (message.Msg == NativeWindowCommon.WM_LBUTTONDBLCLK)
         {
            return; // don't do anything on double-click;
         }
         base.WndProc(ref message);
      }

   }
}
