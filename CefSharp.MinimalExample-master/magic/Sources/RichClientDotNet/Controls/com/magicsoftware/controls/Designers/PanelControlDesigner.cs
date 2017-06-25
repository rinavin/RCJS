﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms.Design;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace com.magicsoftware.controls.designers
{
   /// <summary>
   /// original panel control designer
   /// </summary>
   public class PanelControlDesigner : ScrollableControlDesigner
   {
      public PanelControlDesigner()
      {
         base.AutoResizeHandles = true;
      }

      protected virtual void DrawBorder(Graphics graphics)
      {
         Panel component = (Panel)base.Component;
         if ((component != null) && component.Visible)
         {
            Pen borderPen = this.BorderPen;
            Rectangle clientRectangle = this.Control.ClientRectangle;
            clientRectangle.Width--;
            clientRectangle.Height--;
            graphics.DrawRectangle(borderPen, clientRectangle);
            borderPen.Dispose();
         }
      }

      protected override void OnPaintAdornments(PaintEventArgs pe)
      {
         Panel component = (Panel)base.Component;
         if (component.BorderStyle == BorderStyle.None)
         {
            this.DrawBorder(pe.Graphics);
         }
         base.OnPaintAdornments(pe);
      }

      protected Pen BorderPen
      {
         get
         {
            return new Pen((this.Control.BackColor.GetBrightness() < 0.5) ? ControlPaint.Light(this.Control.BackColor) : ControlPaint.Dark(this.Control.BackColor)) { DashStyle = DashStyle.Dash };
         }
      }
   }
}
