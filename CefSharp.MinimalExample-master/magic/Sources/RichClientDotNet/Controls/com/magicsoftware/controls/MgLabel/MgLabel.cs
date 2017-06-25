using System.Drawing;
using System.Windows.Forms;
using com.magicsoftware.util;
using Controls.com.magicsoftware.support;
using Controls.com.magicsoftware.controls.PropertyInterfaces;
#if PocketPC
using ContentAlignment = OpenNETCF.Drawing.ContentAlignment2;
using FlatStyle = com.magicsoftware.mobilestubs.FlatStyle;
using RightToLeft = com.magicsoftware.mobilestubs.RightToLeft;
#endif

namespace com.magicsoftware.controls
{
#if !PocketPC
   [ToolboxBitmap(typeof(Label))]
#endif
   public class MgLabel : Label, IMultilineProperty, IRightToLeftProperty, IContentAlignmentProperty, IGradientColorProperty, ITextProperty, IFontOrientation , IOwnerDrawnControl, IFontProperty, IFontDescriptionProperty, IBorderTypeProperty
   {
   
      #region IMultilineRenderer Members

      public bool Multiline
      {
         get { return multiline_DO_NOT_USE_DIRECTLY; }
         set
         {
            if (multiline_DO_NOT_USE_DIRECTLY != value)
            {
               multiline_DO_NOT_USE_DIRECTLY = value;
               Invalidate();
            }
         }
      }

      #endregion

      #region IFontDescriptionProperty

      public FontDescription FontDescription { get; set; }

      #endregion IFontDescriptionProperty

      #region IFontProperty
      public override Font Font
      {
         get
         {
            return base.Font;
         }

         set
         {
            base.Font = value;
            FontDescription = new FontDescription(value);
         }
      }
      #endregion IFontProperty

      #region IGradientColorProperty Members

      public GradientColor GradientColor { get; set; }
      public GradientStyle GradientStyle { get; set; }

      #endregion

      public ControlStyle Style
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

#if PocketPC
      public Boolean AutoSize { get; set; }
      public FlatStyle FlatStyle { get; set; }
      public RightToLeft RightToLeft { get; set; }
      public bool UseMnemonic { get; set; }
      public new ContentAlignment TextAlign { get; set; }
#endif

      private bool multiline_DO_NOT_USE_DIRECTLY = false;
      private ControlStyle style_DO_NOT_USE_DIRECTLY;

      /// <summary>
      /// QCR# 429881 fix : For the size and cluient rectangle to be same , we must not set BorderStyle of the Label control.
      ///  It should always be none as we paint the Border.
      /// </summary>
#if !PocketPC
      public override BorderStyle BorderStyle
      {
         get
         {
            return BorderStyle.None;
         }
         set
         {
           // Debug.Assert(false, "Must not set BorderStyle instead Set Style ");
         }
      }

      #region IBorderTypeProperty Members

      /// <summary>
      /// Type of border (Thick / Thin / No border)
      /// </summary>
      private BorderType borderType = BorderType.Thin;
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

#endif

      /// <summary>
      /// Is label color transparent when placed on header
      /// </summary>
      public bool IsTransparentWhenOnHeader { get; set; }

      public MgLabel()
      {
         GradientStyle = GradientStyle.None;
         UseMnemonic = false;
         FontDescription = new FontDescription(Font);
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

      protected override void OnPaint(PaintEventArgs e)
      {
         DrawForeground(e.Graphics, ClientRectangle);
      }

      #region IOwnerDrawnControl Members

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
         Color bgColorToUse = (Parent is TableControl) ? ((TableControl)Parent).GetBGColorToUse(this) : BackColor;
         ControlRenderer.FillRectAccordingToGradientStyle(graphics, rect, bgColorToUse, ForeColor, Style,
                                                          false, GradientColor, GradientStyle, BorderType);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="graphics"></param>
      /// <param name="rect"></param>
      protected virtual void DrawForeground(Graphics graphics, Rectangle rect)
      {
         ControlRenderer.PrintText(graphics, rect, ForeColor, FontDescription, Text, Multiline, TextAlign,
                                   Enabled, true, !UseMnemonic, false, (RightToLeft == RightToLeft.Yes), FontOrientation);
      }

      #endregion

      public int FontOrientation { get; set; }
   }
}
