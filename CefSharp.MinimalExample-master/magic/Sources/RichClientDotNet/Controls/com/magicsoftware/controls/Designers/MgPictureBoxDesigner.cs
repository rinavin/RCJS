using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms.Design;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace com.magicsoftware.controls.designers
{
   class MgPictureBoxDesigner : ControlDesigner
   {
      public MgPictureBoxDesigner()
      {
         base.AutoResizeHandles = true;
      }

      private void DrawBorder(Graphics graphics)
      {
         Color color;
         System.Windows.Forms.Control control = this.Control;
         Rectangle clientRectangle = control.ClientRectangle;
         if (control.BackColor.GetBrightness() < 0.5)
         {
            color = ControlPaint.Light(control.BackColor);
         }
         else
         {
            color = ControlPaint.Dark(control.BackColor);
         }
         Pen pen = new Pen(color)
         {
            DashStyle = DashStyle.Dash
         };
         clientRectangle.Width--;
         clientRectangle.Height--;
         graphics.DrawRectangle(pen, clientRectangle);
         pen.Dispose();
      }

      protected override void OnPaintAdornments(PaintEventArgs pe)
      {
         MgPictureBox mgPictureBox = base.Component as MgPictureBox;

         //If the control does not have border and it does not have the image as well, draw a dummy dashed border.
         if (!mgPictureBox.HasBorder && mgPictureBox.Image == null)
         {
            this.DrawBorder(pe.Graphics);
         }
         
         base.OnPaintAdornments(pe);
      }
   }
}
