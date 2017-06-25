using com.magicsoftware.util;

namespace com.magicsoftware.controls
{
   /// <summary>
   /// factory for header
   /// </summary>
   internal class HeaderRendererFactory
   {
      internal static HeaderRenderer GetRenderer(TableControl tableControl, Header header)
      {
         switch (tableControl.ControlStyle)
         {
            case ControlStyle.TwoD:
               return new TwoDStyleHeaderRenderer(tableControl, header);

            case ControlStyle.ThreeD:
               return new ThreeDRaisedStyleHeaderRenderer(tableControl, header);

            case ControlStyle.Windows3d:
               return new WindowsThreeDStyleHeaderRenderer(tableControl, header);

            default:
               return new WindowsStyleHeaderRenderer(tableControl, header);
         }
      }
   }
}