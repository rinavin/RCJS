using System.Windows.Forms.Design;
using com.magicsoftware.util;
using System.ComponentModel;

namespace Controls.com.magicsoftware.controls.MgShape
{
   public class ShapeDesigner : ControlDesigner
   {

      public override void InitializeNewComponent(System.Collections.IDictionary defaultValues)
      {
         base.InitializeNewComponent(defaultValues);
         // Frameworks set Text (like Ellipse1, Rect1..) we don't want it .
         // So reset the text .
         PropertyDescriptor descriptor = TypeDescriptor.GetProperties(base.Component)["Text"];
         if (((descriptor != null) && (descriptor.PropertyType == typeof(string))))
         {
            descriptor.ResetValue(Component);
         }
      }
   }
}