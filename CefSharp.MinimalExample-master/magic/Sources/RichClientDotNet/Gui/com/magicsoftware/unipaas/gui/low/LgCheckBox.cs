using System;
using System.Drawing;
using System.Windows.Forms;
using com.magicsoftware.controls;
using com.magicsoftware.util;
#if PocketPC
using Appearance = com.magicsoftware.mobilestubs.Appearance;
#endif

namespace com.magicsoftware.unipaas.gui.low
{
   internal class LgCheckBox : LogicalControl
   {
      internal LgCheckBox(GuiMgControl guiMgControl, Control containerControl) :
         base(guiMgControl, containerControl)
      {
      }

      internal Image OrgImage { get; private set; }

      private Appearance _appearance;
      internal Appearance Appearance
      {
         get { return _appearance; }
         set
         {
            _appearance = value;
            _coordinator.Refresh(true);
         }
      }

      private bool _threeStates;
      internal bool ThreeStates
      {
         get { return _threeStates; }
         set
         {
            _threeStates = value;
            _coordinator.Refresh(true);
         }
      }

      private CheckState _checkState;
      internal CheckState CheckState
      {
         get { return _checkState; }
         set
         {
            _checkState = value;
            _coordinator.Refresh(true);
         }
      }

      /// <summary> set checked
      /// 
      /// </summary>
      /// <param name="isChecked">
      /// </param>
      internal void setChecked(bool isChecked)
      {
         _coordinator.Refresh(true);
      }

      internal void SetImage(Image image)
      {
         OrgImage = image;

         Refresh(true);
      }

      internal override string getSpecificControlValue()
      {
         return (GuiUtils.getCheckBoxValue(CheckState));
      }

      internal override void setSpecificControlProperties(Control control)
      {
         ControlUtils.SetContentAlignment(control, ContentAlignment);

         if (Text != null)
            GuiUtils.setText(control, Text);

         ControlUtils.SetCheckboxMainStyle((MgCheckBox)control, _appearance);
         GuiUtils.SetThreeStates((MgCheckBox)control, _threeStates);
         GuiUtils.setCheckBoxCheckState((MgCheckBox)control, _checkState);
         ControlUtils.SetStyle3D(control, Style);
         ((MgCheckBox)control).BorderType = ControlBorderType;
         GuiUtils.setImageInfoOnTagData(control, OrgImage, CtrlImageStyle.Copied);
         GuiUtils.setBackgroundImage(control);
         ((MgCheckBox)control).Multiline = MultiLine;
         //TODO padding
      }
   }
}
