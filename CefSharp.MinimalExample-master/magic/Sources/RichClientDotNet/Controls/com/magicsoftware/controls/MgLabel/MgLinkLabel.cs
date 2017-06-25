using System;
using System.Drawing;
using System.Windows.Forms;
using com.magicsoftware.util;
using com.magicsoftware.controls.utils;
using Controls.com.magicsoftware.support;
#if PocketPC
using LinkLabel = OpenNETCF.Windows.Forms.LinkLabel2;
using FlatStyle = com.magicsoftware.mobilestubs.FlatStyle;
using RightToLeft = com.magicsoftware.mobilestubs.RightToLeft;
using ContentAlignment = OpenNETCF.Drawing.ContentAlignment2;
using ControlPaint = com.magicsoftware.richclient.mobile.gui.ControlPaint;
#endif

namespace com.magicsoftware.controls
{
#if !PocketPC
      [ToolboxBitmap(typeof(Button))]
#endif
   public class MgLinkLabel : LinkLabel, IContentAlignmentProperty, IRightToLeftProperty, IGradientColorProperty, ITextProperty
   {
      public override Font Font
      {
         get
         {
            return base.Font;
         }
         set
         {
            //Always show the text with underline.
            FontStyle newFontStyle = value.Style;
            if ((newFontStyle & FontStyle.Underline) == 0)
            {
               newFontStyle |= FontStyle.Underline;
               value = new Font(value.Name, value.Size, newFontStyle);
            }

            base.Font = value;
         }
      }

      #region IGradientColorProperty Members

      public GradientColor GradientColor { get; set; }
      public GradientStyle GradientStyle { get; set; }

      #endregion

      public new bool LinkVisited
      {
         get { return base.LinkVisited; }
         set
         {
            base.LinkVisited = value;
            RefreshLinkColor();
         }
      }

      //TODO: Kaushal. Check if OnHovering is needed.
      private bool onHovering_DO_NOT_USE_DIRECTLY;
      public bool OnHovering
      {
         get { return onHovering_DO_NOT_USE_DIRECTLY; }
         set 
         {
            onHovering_DO_NOT_USE_DIRECTLY = value;
            RefreshLinkColor();
         }
      }

      private Color? originalFGColor_DO_NOT_USE_DIRECTLY;
      internal Color? OriginalFGColor
      {
         get { return originalFGColor_DO_NOT_USE_DIRECTLY; }
         set 
         {
            originalFGColor_DO_NOT_USE_DIRECTLY = value;
            RefreshLinkColor();
         }
      }

      private Color? originalBGColor_DO_NOT_USE_DIRECTLY;
      internal Color? OriginalBGColor
      {
         get { return originalBGColor_DO_NOT_USE_DIRECTLY; }
         set
         {
            if (value == Color.Empty)
               value = Control.DefaultBackColor;
            originalBGColor_DO_NOT_USE_DIRECTLY = value;
            RefreshLinkColor();
         }
      }

      public Color? HoveringFGColor { get; set; }
      public Color? HoveringBGColor { get; set; }

      public Color? VisitedFGColor { get; set; }
      public Color? VisitedBGColor { get; set; }

#if PocketPC
      public FlatStyle FlatStyle { get; set; }
      public RightToLeft RightToLeft { get; set; }
      public ContentAlignment TextAlign { get; set; }
      public bool UseMnemonic { get; set; }
#endif
      public MgLinkLabel()
      {
         OriginalFGColor = base.LinkColor;
         OriginalBGColor = base.BackColor;

         GradientStyle = GradientStyle.None;
      }

      /// <summary></summary>
      /// <param name="HoveringFGColor"></param>
      /// <param name="HoveringBGColor"></param>
      public void SetHoveringColor(Color? hoveringFGColor, Color? hoveringBGColor)
      {
         if (hoveringFGColor != null)
            HoveringFGColor = hoveringFGColor;

         if (hoveringBGColor != null)
            HoveringBGColor = hoveringBGColor;
      }

      /// <summary></summary>
      /// <param name="VisitedFGColor"></param>
      /// <param name="VisitedBGColor"></param>
      public void SetVisitedColor(Color? visitedFGColor, Color? visitedBGColor)
      {
         if (visitedFGColor != null)
            VisitedFGColor = visitedFGColor;

         if (visitedBGColor != null)
            VisitedBGColor = visitedBGColor;

         if (VisitedFGColor != null && VisitedBGColor != null)
            RefreshLinkColor();

      }

      /// <summary></summary>
      /// <param name="tmpColor"></param>
      private void SetLinkColor(Color? NewFGColor, Color? NewBGColor)
      {
         if (NewFGColor != null)
         {
            LinkColor = (Color)NewFGColor;
            ActiveLinkColor = (Color)NewFGColor;
         }
         if (NewBGColor != null)
            BackColor = (Color)NewBGColor;
      }

      /// <summary></summary>
      /// <param name="NewColor"></param>
      public void RefreshLinkColor()
      {
         if (OnHovering)
         {
            if (HoveringFGColor != null && HoveringBGColor != null)
               SetLinkColor(HoveringFGColor, HoveringBGColor);
         }
         else
         {
            if (LinkVisited && (VisitedFGColor != null && VisitedBGColor != null))
               SetLinkColor(VisitedFGColor, VisitedBGColor);
            else
               SetLinkColor(OriginalFGColor, OriginalBGColor);
         }
      }

      protected override void OnPaint(PaintEventArgs e)
      {
         //Simulate Transparency
         if (BackColor == Color.Transparent)
         {
#if !PocketPC
            if (Parent is ISupportsTransparentChildRendering)
               ((ISupportsTransparentChildRendering)Parent).TransparentChild = this;

            System.Drawing.Drawing2D.GraphicsContainer g = e.Graphics.BeginContainer();
            Rectangle translateRect = this.Bounds;
            e.Graphics.TranslateTransform(-Left, -Top);
            PaintEventArgs pe = new PaintEventArgs(e.Graphics, translateRect);
            this.InvokePaintBackground(Parent, pe);
            this.InvokePaint(Parent, pe);
            //fixed bug#:796041, the forgound not need to display
            //this.InvokePaint(Parent, pe);
            e.Graphics.ResetTransform();
            e.Graphics.EndContainer(g);
            pe.Dispose();

            if (Parent is ISupportsTransparentChildRendering)
               ((ISupportsTransparentChildRendering)Parent).TransparentChild = null;
#endif
         }

         ControlRenderer.FillRectAccordingToGradientStyle(e.Graphics, ClientRectangle, BackColor, LinkColor, 0,
                                                          false, GradientColor, GradientStyle);

         ContentAlignment newTextAlign = ControlUtils.GetOrgContentAligment(RightToLeft, TextAlign);

         FontDescription font = new FontDescription(Font);
         ControlRenderer.PrintText(e.Graphics, ClientRectangle, LinkColor, font, Text, false, newTextAlign,
                                   Enabled, true, !UseMnemonic, false, (RightToLeft == RightToLeft.Yes));

         //fixed bug #:765815 : when parking on hypertext button,focus need to seen on text on the button (not on  entire button)
         if (Focused)
         {
            Size textExt = Utils.GetTextExt(Font, Text, this);
            // get the display the focus on the text of the control
            Rectangle textRect = ControlUtils.GetFocusRect(this, ClientRectangle, newTextAlign, textExt);
            //ass offset, it will look as in online
            textRect.Inflate(2, 2);
            textRect.Width -= 2;
            textRect.Height -= 1;

            textRect.X = Math.Max(1, textRect.X);
            textRect.Y = Math.Max(1, textRect.Y);
            textRect.Width = Math.Min(textRect.Width, ClientRectangle.Width - textRect.X);
            textRect.Height = Math.Min(textRect.Height, ClientRectangle.Height - textRect.Y);

            ControlPaint.DrawFocusRectangle(e.Graphics, textRect);
         }
      }
   
#if !PocketPC
      protected override bool ProcessMnemonic(char inputChar)
      {
         if (CanSelect && IsMnemonic(inputChar, Text))
         {
            this.OnLinkClicked(new LinkLabelLinkClickedEventArgs(new LinkLabel.Link()));
            return true;
         }
         return false;
      }
#endif
   }
}
