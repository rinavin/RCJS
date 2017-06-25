using System.Windows.Forms;

namespace com.magicsoftware.controls
{
   internal interface ISupportsTransparentChildRendering
   {
      Control TransparentChild { get; set; }
   }
}
