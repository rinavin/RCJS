using System.Windows.Forms;
using com.magicsoftware.unipaas.gui;
using com.magicsoftware.controls;

namespace com.magicsoftware.unipaas.gui.low
{
   internal class LgRich : LogicalControl
   {
      internal LgRich(GuiMgControl guiMgControl, Control containerControl) :
         base(guiMgControl, containerControl)
      {
          ShowBorder = true;
      }

      internal override void setSpecificControlProperties(Control control)
      {
         GuiUtils.setText(control, Text);
         ControlUtils.SetStyle3D(control, Style);
#if !PocketPC //tmp
         GuiUtils.setReadOnly((RichTextBox)control, !Modifable);
#endif
      }
   }
   
}
