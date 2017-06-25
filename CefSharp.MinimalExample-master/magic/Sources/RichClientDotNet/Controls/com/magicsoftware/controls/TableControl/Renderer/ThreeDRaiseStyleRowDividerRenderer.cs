using com.magicsoftware.controls;
using System.Drawing;

namespace Controls.com.magicsoftware.controls.Renderer
{
   /// <summary>
   ///  Class for rendering rows for 3-d raised style 
   /// </summary>
   internal class ThreeDRaiseStyleRowDividerRenderer : IRowDividerRenderer
   {
      public void Render(Graphics graphics, Pen dividerPen, int borderSize, int factor, int left, int top, int right)
      {
         graphics.DrawLine(ThreeDRaisedStyleRenderer.HighLightPen, left, top, right, top);

         int bottom = top - TableControl.Factor;
         graphics.DrawLine(ThreeDRaisedStyleRenderer.ShadowPen, left, bottom, right, bottom);
      }
   }
}