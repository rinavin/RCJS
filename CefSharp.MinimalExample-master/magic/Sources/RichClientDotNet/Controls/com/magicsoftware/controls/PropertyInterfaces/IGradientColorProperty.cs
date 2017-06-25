using com.magicsoftware.util;

namespace com.magicsoftware.controls
{
   //TODO: Kaushal. When the rendering code will be moved from MgUtils to MgControls,
   //the scope of this intrerface should be changed to internal.
   public interface IGradientColorProperty
   {
      GradientColor GradientColor { get; set; }
      GradientStyle GradientStyle { get; set; }
   }
}
