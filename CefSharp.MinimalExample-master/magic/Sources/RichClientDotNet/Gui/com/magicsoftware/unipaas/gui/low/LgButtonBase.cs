using System;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using com.magicsoftware.controls;
using com.magicsoftware.support;
#if PocketPC
using ContentAlignment = OpenNETCF.Drawing.ContentAlignment2;
#endif

namespace com.magicsoftware.unipaas.gui.low
{
   internal class LgButton : LogicalControl 
   {
      internal LgButton(GuiMgControl guiMgControl, Control containerControl) :
         base(guiMgControl, containerControl)
      {
         ContentAlignment = ContentAlignment.MiddleCenter;
         //Fixed bug #:242632 set the default color of the button(only when the user don't have color define (color=0))
         BgColor = SystemColors.Control;
         FgColor = SystemColors.WindowText;
      }

      private MgImageList _imageList;
      internal MgImageList ImageList
      {
         get { return _imageList; }
         set
         {
            _imageList = value;
            _coordinator.Refresh(true);
         }
      }

      internal int PBImagesNumber { get; set; }

      /// <summary> </summary>
      internal void setImageList(String imageFileName)
      {
         //Form imageList : not need to Dispose the bacause it is from the ImageListCashe.
         //For image : dispose is need because it is new image according to the size of the control.
         if (GuiMgControl.IsImageButton())
            _imageList = GuiUtils.getImageListItemForButtonControl(imageFileName, PBImagesNumber);
      }

      internal override string getSpecificControlValue()
      {
         return Text;
      }

      internal override void setSpecificControlProperties(Control control)
      {
         ControlUtils.SetContentAlignment(control, ContentAlignment);
         
         if (Text != null)
            GuiUtils.setText(control, Text);

         if (GuiMgControl.IsImageButton())
         {
            MgImageButton mgImageButton = ((MgImageButton)control);
            mgImageButton.PBImagesNumber = PBImagesNumber;
            GuiUtils.setImageList(mgImageButton, _imageList);
         }
      }
   }
}
