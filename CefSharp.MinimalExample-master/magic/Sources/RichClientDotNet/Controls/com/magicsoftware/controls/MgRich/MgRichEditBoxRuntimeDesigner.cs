using System.ComponentModel;
#if !PocketPC
using com.magicsoftware.controls.designers;
using System.Windows.Forms;
using System.Drawing;
#endif

namespace com.magicsoftware.controls
{
#if !PocketPC
   [Designer(typeof(MgTextBoxRuntimeDesignerDesigner)), Docking(DockingBehavior.Never)]
   [ToolboxBitmap(typeof(RichTextBox))]
#endif
   public class MgRichEditBoxRuntimeDesigner : MgRichTextBox
   {
      public MgRichEditBoxRuntimeDesigner()
         : base()
      {
      }
   }      
}