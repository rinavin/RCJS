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
   //ToolboxBitmap is used to find the toolbox image icon of RichText Control
   [ToolboxBitmap(typeof(RichTextResourceFinder), "Controls.Resources.RichText16x16.ico")]
#endif
   public class MgRichTextBoxRuntimeDesigner : MgRichTextBox
   {
      public MgRichTextBoxRuntimeDesigner()
         : base()
      {
      }
   }      
}

/// <summary>
/// This internal class is used for ToolboxBitmap which is outside of the namespace and toolbox image is inside Studio.Resources namespace.
/// </summary>
internal class RichTextResourceFinder
{
}
