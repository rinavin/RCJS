using com.magicsoftware.controls;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace Controls.com.magicsoftware.controls.Renderer
{
   /// <summary>
   /// 
   /// </summary>
   class WindowsStyleRowDividerRenderer : IRowDividerRenderer
   {
      public void Render(Graphics graphics, Pen dividerPen, int borderSize, int factor, int left, int top, int right)
      {
         graphics.DrawLine(dividerPen, left, top, right, top);
      }
   }
}
