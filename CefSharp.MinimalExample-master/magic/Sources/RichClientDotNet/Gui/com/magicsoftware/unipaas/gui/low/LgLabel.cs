using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using com.magicsoftware.unipaas.gui;
using com.magicsoftware.controls;
using Controls.com.magicsoftware.support;

#if PocketPC
using ContentAlignment = OpenNETCF.Drawing.ContentAlignment2;
#endif

namespace com.magicsoftware.unipaas.gui.low
{
   /// <summary>
   /// static implementation for label control
   /// </summary>
   internal class LgLabel : LogicalControl, IFontOrientation
   {
      internal LgLabel(GuiMgControl guiMgControl, Control containerControl)
         : base(guiMgControl, containerControl)
      {
      }

      /// <summary>
      /// Is label color transparent when placed on header
      /// </summary>
      public bool IsTransparentWhenOnHeader { get; set; }

      /// <summary>
      /// sets label properties
      /// </summary>
      /// <param name="control"></param>
      internal override void setSpecificControlProperties(Control control)
      {
         ControlUtils.SetBorder(control, ShowBorder);
         GuiUtils.setText(control, Text);
         ControlUtils.SetMultiLine(control, MultiLine);
         ControlUtils.SetContentAlignment(control, ContentAlignment);
         // Defect# 130255: MgLabel is used on Table Header, here style was not set hence problem.
         // This will also affect automation. i.e. Label on line area on table when AllowTesting=Y
         ControlUtils.SetStyle3D(control, Style);
         ControlUtils.SetTransparentOnHeader(control, IsTransparentWhenOnHeader);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="control"></param>
      internal override void setSpecificControlPropertiesForFormDesigner(Control control)
      {
         ControlUtils.SetStyle3D(control, Style);
      }

      public int FontOrientation { get; set; }

      /// <summary>
      ///   print control's text
      /// </summary>
      /// <param name = "gc"></param>
      /// <param name = "rect"></param>
      /// <param name = "color"></param>
      /// <param name = "str"></param>
      internal override void printText(Graphics g, Rectangle rect, Color color, String str)
      {
         FontDescription font = new FontDescription(Font);
         ControlRenderer.PrintText(g, rect, color, font, str, MultiLine, ContentAlignment, Enabled, WordWrap, true, false,
                                   RightToLeft, FontOrientation);
      }
    }
}
