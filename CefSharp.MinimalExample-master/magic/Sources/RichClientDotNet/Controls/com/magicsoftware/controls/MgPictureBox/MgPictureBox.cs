using System;
using System.Windows.Forms;
using Controls.com.magicsoftware.support;
using com.magicsoftware.util;
using System.Drawing;
using System.ComponentModel;
using Controls.com.magicsoftware.controls.PropertyInterfaces;
#if !PocketPC
using com.magicsoftware.controls.designers;
#endif

namespace com.magicsoftware.controls
{
#if !PocketPC
   [Designer(typeof(MgPictureBoxDesigner)), Docking(DockingBehavior.Never)]
   [ToolboxBitmap(typeof(PictureBox))]
#endif
   public class MgPictureBox : PictureBox, IImageProperty  , ISetSpecificControlPropertiesForFormDesigner, IOwnerDrawnControl, IBorderTypeProperty
   {
      private ControlStyle style_DO_NOT_USE_DIRECTLY;
#if !PocketPC
      public ControlStyle Style
#else
      internal ControlStyle Style
#endif
      {
         get { return style_DO_NOT_USE_DIRECTLY; }
         set
         {
            if (style_DO_NOT_USE_DIRECTLY != value)
            {
               style_DO_NOT_USE_DIRECTLY = value;
               Invalidate();
            }
         }
      }

      public virtual bool HasBorder { get { return (Style != ControlStyle.NoBorder); } }

      #region IBorderTypeProperty Members

      private BorderType borderType = BorderType.Thin;

      /// <summary>
      /// Type of border (Thick / Thin / No Border)
      /// </summary>
      public BorderType BorderType
      {
         get { return borderType; }
         set
         {
            if (borderType != value)
            {
               borderType = value;
               Invalidate();
            }
         }
      }

      #endregion

      public MgPictureBox()
         : base()
      {
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="pevent"></param>
      protected override void OnPaintBackground(PaintEventArgs pevent)
      {
         //Simulate Transparency
         if (BackColor == Color.Transparent || BackColor.A < 255)
         {
            if (Parent is ISupportsTransparentChildRendering)
               ((ISupportsTransparentChildRendering)Parent).TransparentChild = this;

            System.Drawing.Drawing2D.GraphicsContainer g = pevent.Graphics.BeginContainer();
            Rectangle translateRect = this.Bounds;
            pevent.Graphics.TranslateTransform(-Left, -Top);
            PaintEventArgs pe = new PaintEventArgs(pevent.Graphics, translateRect);
            this.InvokePaintBackground(Parent, pe);
            this.InvokePaint(Parent, pe);
            pevent.Graphics.ResetTransform();
            pevent.Graphics.EndContainer(g);
            pe.Dispose();

            if (Parent is ISupportsTransparentChildRendering)
               ((ISupportsTransparentChildRendering)Parent).TransparentChild = null;
         }

         DrawBackground(pevent.Graphics, ClientRectangle);
      }

      /// <summary></summary>
      /// <param name="e"></param>
      protected override void OnPaint(PaintEventArgs e)
      {
         DrawForeground(e.Graphics, ClientRectangle);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="fromControl"></param>
      public void setSpecificControlPropertiesForFormDesigner(Control fromControl)
      {         
         Style = ((MgPictureBox)fromControl).Style;
      }

      #region IOwnerDrawControl Members
      
      /// <summary>
      /// Draw the control on the given Graphics at the specified rectangle.
      /// </summary>
      /// <param name="graphics"></param>
      /// <param name="rect"></param>
      public void Draw(Graphics graphics, Rectangle rect)
      {
         DrawBackground(graphics, rect);
         DrawForeground(graphics, rect);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="graphics"></param>
      /// <param name="rect"></param>
      private void DrawBackground(Graphics graphics, Rectangle rect)
      {
         Image imageToDisplay = this.Image;
         ControlRenderer.PaintBackGround(graphics, rect, BackColor, true);

         
         //paint the image 
         if (imageToDisplay != null)
         {
            using (Region imageRegion = new Region(rect))
            {
               Region r = graphics.Clip;
               graphics.Clip = imageRegion;

               Rectangle imageRect = new Rectangle(rect.X, rect.Y, imageToDisplay.Size.Width, imageToDisplay.Height);

               if (HasBorder)
               {
                  imageRect.X++;
                  imageRect.Y++;
               }
               ControlRenderer.DrawImage(graphics, imageRect, imageToDisplay, 0, 0);

               graphics.Clip = r;
            }
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="graphics"></param>
      /// <param name="rect"></param>
      private void DrawForeground(Graphics graphics, Rectangle rect)
      {
         // paint the border
         if (HasBorder)
            BorderRenderer.PaintBorder(graphics, rect, ForeColor, Style, false, BorderType);
      }

      #endregion
   }
}