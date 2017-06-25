using com.magicsoftware.controls.designers;
using com.magicsoftware.util;
using Controls.com.magicsoftware.controls.MgShape;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace com.magicsoftware.controls
{
#if !PocketPC
   [Designer(typeof(GroupBoxDesigner))]
   [ToolboxBitmap(typeof(GroupBox))]
    public class MgGroupBox : GroupBox, IRightToLeftProperty, IGradientColorProperty, ITextProperty, ICanParent, IMgContainer, ISupportsTransparentChildRendering
#else
   public class MgGroupBox : GroupBox, IRightToLeftProperty, IGradientColorProperty, ITextProperty, ICanParent
#endif
   {
      static Font NoTextXPThemeFont;
      static Font NoTextClassicThemeFont;

      Font NoTextFont
      {
         get
         {
            return (Application.RenderWithVisualStyles ? NoTextXPThemeFont : NoTextClassicThemeFont);
         }
      }

      #region IGradientColorProperty Members

      public GradientColor GradientColor { get; set; }
      public GradientStyle GradientStyle { get; set; }

      #endregion

      #region ITextProperty Members

      public override string Text
      {
         get
         {
            return base.Text;
         }
         set
         {
            //MgGroupBox does not support accelerators.
            base.Text = value != null ? value.Replace("&", "&&") : value;
            UpdateFont();
         }
      }

      #endregion

      private Font orgFont;
      public override Font Font
      {
         get
         {
            return base.Font;
         }
         set
         {
            if (orgFont != value)
            {
               orgFont = value;
               UpdateFont();
            }
         }
      }

      private bool topBorderMargin;
      public bool TopBorderMargin
      {
         get 
         {
            return topBorderMargin;
         }
         set 
         {
            if (topBorderMargin != value)
            {
               topBorderMargin = value;
               UpdateFont();
            }
         }
      }

      /// <summary>
      /// Border Color
      /// </summary>
      public Color BorderColor { get; set; }

      /// <summary>
      /// Style
      /// </summary>
      public ControlStyle ControlStyle { get; set; }

      /// <summary>
      /// Return true if Top Border Margin should be displayed.
      /// </summary>
      public bool ShouldDisplayTopBorderMargin
      {
         get
         {
            return TopBorderMargin || !String.IsNullOrEmpty(Text);
         }
      }

      /// <summary>
      /// MgGroupBox class - used to support double buffering on GroupBox
      /// </summary>
      public MgGroupBox()
         : base()
      {
         DoubleBuffered = true;
         GradientStyle = GradientStyle.None;
         orgFont = base.Font;
         TopBorderMargin = true;
      }

      static MgGroupBox()
      {
         NoTextXPThemeFont = new Font(DefaultFont.FontFamily, (float)0.5, DefaultFont.Style, DefaultFont.Unit, DefaultFont.GdiCharSet, DefaultFont.GdiVerticalFont);
         NoTextClassicThemeFont = new Font(DefaultFont.FontFamily, (float)1.0, DefaultFont.Style, DefaultFont.Unit, DefaultFont.GdiCharSet, DefaultFont.GdiVerticalFont);
      }

      /// <summary>
      /// Update the Font based on TopBorderMargin and Text.
      /// </summary>
      private void UpdateFont()
      {
         Font newFont = ShouldDisplayTopBorderMargin ? orgFont : NoTextFont;

         if (newFont != base.Font)
            base.Font = newFont;
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

         ControlRenderer.FillRectAccordingToGradientStyle(pevent.Graphics, this.ClientRectangle, BackColor, ForeColor,
                                                          ControlStyle.NoBorder, false, GradientColor, GradientStyle);

         if (TransparentChild != null && DesignMode)
            ControlRenderer.RenderOwnerDrawnControlsOfParent(pevent.Graphics, TransparentChild);
      }

      /// <summary>
      /// paint the group box
      /// </summary>
      /// <param name="e"></param>
      protected override void OnPaint(PaintEventArgs e)
      {
         if (ControlStyle == ControlStyle.TwoD)
         {
            Rectangle bounds = new Rectangle(0, 0, Width, Height);
            Rectangle rectangle = bounds;
            rectangle.Width -= 14;
            TextFormatFlags flags = TextFormatFlags.PreserveGraphicsTranslateTransform | TextFormatFlags.PreserveGraphicsClipping | TextFormatFlags.TextBoxControl | TextFormatFlags.WordBreak;

            if (this.RightToLeft == RightToLeft.Yes)
               flags |= TextFormatFlags.RightToLeft | TextFormatFlags.Right;

            Size size = TextRenderer.MeasureText(e.Graphics, Text, Font, new Size(rectangle.Width, rectangle.Height), flags);
            rectangle.Width = size.Width;
            rectangle.Height = size.Height;

            if ((flags & TextFormatFlags.Right) == TextFormatFlags.Right)
               rectangle.X = ((bounds.Right - rectangle.Width) - 7) + 1;
            else
               rectangle.X += 6;

            TextRenderer.DrawText(e.Graphics, Text, Font, rectangle, ForeColor, flags);

            Color borderColor = BorderColor;
            if (borderColor == Color.Empty)
               borderColor = Color.Black;

            Pen pen = PensCache.GetInstance().Get(borderColor);
            rectangle.Inflate(-1, 0);

            GroupRenderer.DrawBorder(e.Graphics, ClientRectangle, pen, ControlStyle.TwoD, rectangle, Font, ShouldDisplayTopBorderMargin);

            //Fixed defect 142805 & 142774: The logical controls present inside the group control were not painted.
            //Reason : The reason was the when group controls style is 2D, then we don't call base.paint() which internally calls logical control's paint event from Control.OnPaint() method.
            //Solution : Raise Control's OnPaint event so that the logical control's present inside the Group control will be painted.
            RaisePaintEvent(this, e);
         }
         else
            base.OnPaint(e);
      }

      #region ICanParent members
      public event CanParentDelegate CanParentEvent;

      public bool CanParent(CanParentArgs allowDragDropArgs)
      {
         if (CanParentEvent != null)
            return CanParentEvent(this, allowDragDropArgs);
         return true;
      }
      #endregion

#if !PocketPC

      #region IMgContainer
      public event ComponentDroppedDelegate ComponentDropped;

      public virtual void OnComponentDropped(ComponentDroppedArgs args)
      {
         if (ComponentDropped != null)
            ComponentDropped(this, args);
      }
      #endregion

#endif

      #region ISupportsTransparentChildRendering

      public Control TransparentChild { get; set; }

      #endregion
   }



}
