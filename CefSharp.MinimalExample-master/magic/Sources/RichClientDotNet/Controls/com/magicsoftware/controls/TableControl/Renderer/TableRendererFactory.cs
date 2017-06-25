using com.magicsoftware.controls;
using com.magicsoftware.util;

namespace Controls.com.magicsoftware.controls.Renderer
{
   public static class TableRendererFactory
   {
      public static TableStyleRendererBase GetRenderer(TableControl tableControl)
      {
         switch (tableControl.ControlStyle)
         {
            case ControlStyle.TwoD:
               return new TwoDTableStyleRenderer(tableControl);

            case ControlStyle.Windows3d:
               return new WindowsThreeDStyleRenderer(tableControl);

            case ControlStyle.ThreeD:
               return new ThreeDRaisedStyleRenderer(tableControl);

            default:
               return new WindowsTableStyleRenderer(tableControl);
         }
      }
   }
}