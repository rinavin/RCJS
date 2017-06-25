using com.magicsoftware.controls;
using System;
using System.Drawing;

namespace Controls.com.magicsoftware.controls.Renderer
{
   internal class WindowsThreeDColumnDividerRenderer : IColumnDividerRenderer
   {
      /// <summary>
      /// draw column divider
      /// </summary>
      /// <param name="graphics"></param>
      /// <param name="dividerPen"></param>
      /// <param name="borderSize"></param>
      /// <param name="Factor"></param>
      /// <param name="rightToLeftLayout"></param>
      /// <param name="left"></param>
      /// <param name="top"></param>
      /// <param name="bottom"></param>
      public void Render(Graphics graphics, TableControl tableControl, Pen dividerPen, int left, int top, int bottom)
      {
         graphics.DrawLine(dividerPen, left, top, left, bottom);
      }
   }
}