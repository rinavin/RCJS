using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using com.magicsoftware.controls.utils;
using com.magicsoftware.util;
using com.magicsoftware.win32;
using System.Windows.Forms.VisualStyles;
using ContentAlignment = System.Drawing.ContentAlignment;
using Controls.com.magicsoftware.support;

namespace com.magicsoftware.controls
{
   /// <summary>
   /// This class provides methods to draw text and focus rectangle on the CheckBox and
   /// RadioButtons of Normal appearance.
   /// </summary>
   public class CheckBoxAndRadioButtonRenderer
   {
      private const int BOX_WIDTH = 10;
      private const int BOX_HEIGHT = 10;

      /// <summary>
      /// Draw the text and the focus rectangle on a CheckBox/RadioButon control.
      /// </summary>
      /// <param name="control"></param>
      /// <param name="e"></param>
      /// <param name="textToDisplay"></param>
      public static void DrawTextAndFocusRect(ButtonBase control, PaintEventArgs e, String textToDisplay, Rectangle rectangle, int textOffset)
      {
         bool isMultiLine = ControlUtils.GetIsMultiLine(control);
         Rectangle textRect = new Rectangle();
         Size textSize = new Size();
         ContentAlignment NewTextAli = ContentAlignment.MiddleCenter;
         FontDescription font = new FontDescription(control.Font);

         if (!String.IsNullOrEmpty(textToDisplay))
         {
            textRect = rectangle;
            NewTextAli = ControlUtils.GetOrgContentAligment(control.RightToLeft, control.TextAlign);
            textSize = Utils.GetTextExt(control.Font, textToDisplay, control);
            
            ControlUtils.AlignmentInfo AlignmentInfo = ControlUtils.GetAlignmentInfo(control.TextAlign);
            int Offset = (int)((float)(BOX_WIDTH + textOffset) * Utils.GetDpiScaleRatioX(control));

            switch (AlignmentInfo.HorAlign)
            {
               case AlignmentTypeHori.Left:
                  if (control.RightToLeft == RightToLeft.No)
                  {
                     textRect.X += Offset;
                     textRect.Width -= Offset;
                  }
                  if (control.RightToLeft == RightToLeft.Yes)
                     textRect.X -= Offset;
                  break;
               case AlignmentTypeHori.Right:
                  if (isMultiLine && control.RightToLeft == RightToLeft.No)
                  {
                        textRect.X += Offset;
                        textRect.Width -= Offset;
                  }
                  break;
               case AlignmentTypeHori.Center:
                  if (control.RightToLeft == RightToLeft.No)
                  {
                     textRect.X += Offset;
                     textRect.Width -= Offset;
                     // if the text is bigger then the display rect.
                     if (!isMultiLine)
                     {
                        if (textSize.Width > textRect.Width)
                        {
                           textRect.X += (int)(((textRect.Width - textSize.Width) / 2));
                           textRect.Width = Math.Max(textRect.Width, textSize.Width);
                        }
                     }
                  }
                  else if (control.RightToLeft == RightToLeft.Yes)
                  {
                     textRect.X -= Offset;
                     textRect.Width += Offset;
                  }
                  break;
            }

            ControlRenderer.PrintText(e.Graphics, textRect, control.ForeColor, font, textToDisplay, isMultiLine, NewTextAli,
                                      control.Enabled, true, false, true, control.RightToLeft == RightToLeft.Yes, true);
         }

         // display the focus on the text of the control
         if (control.Focused)
         {
            if (!String.IsNullOrEmpty(textToDisplay))
            {
               if (isMultiLine)
                  textSize = GetTextSize(e, textToDisplay, font, ref textRect, isMultiLine);

               // get the display the focus on the text of the control
               textRect = ControlUtils.GetFocusRect(control, textRect, NewTextAli, textSize);

               ControlPaint.DrawFocusRectangle(e.Graphics, textRect);
            }
            else
            {
               //For CheckBox, if the text is not available, focus rect should be drawn on the Glyph.
               if (control is MgCheckBox)
                  DrawFocusRectOnCheckBoxGlyph((MgCheckBox)control, e.Graphics);
            }
         }
      }

      /// <summary>
      /// return the size of the text
      /// </summary>
      /// <param name="e"></param>
      /// <param name="Text"></param>
      /// <param name="buttonBase"></param>
      /// <param name="textRect"></param>
      /// <param name="NewTextAli"></param>
      /// <param name="isMultiLine"></param>
      /// <returns></returns>
      private static Size GetTextSize(PaintEventArgs e, String Text, FontDescription font, ref Rectangle textRect, bool isMultiLine)
      {
         Size textExt = new Size();

         // Rinat: I used ContentAlignment.MiddleRight for the calculation because if we use ContentAlignment.MiddleCenter
         // then GuiUtils.CalcTextRect() returns wrong results. The problem is that the height returned by GuiUtils.CalcTextRect()
         // will be of a single line always.
         int flags = (int)Utils.GetTextFlags(ContentAlignment.MiddleRight, /*wordWrap*/true, isMultiLine,
            /*AddNoPrefixFlag*/false,  /*AddNoClipping*/true,
            /*rightToLeft*/false);

         NativeWindowCommon.RECT retCalcTextRect;
         Utils.CalcTextRect(e.Graphics, textRect, font, Text, flags, out retCalcTextRect);

         textExt.Width = retCalcTextRect.right - retCalcTextRect.left;
         textExt.Height = retCalcTextRect.bottom - retCalcTextRect.top;

         return textExt;
      }

      /// <summary> Draws the focus on the check box glyph. </summary>
      /// <param name="mgCheckBox"></param>
      /// <param name="graphics"></param>
      public static void DrawFocusRectOnCheckBoxGlyph(MgCheckBox mgCheckBox, Graphics graphics)
      {
         Rectangle focusRectangle = new Rectangle();
         Rectangle clientRectangle = mgCheckBox.ClientRectangle;
         int boxWidth = CheckBoxRenderer.GetGlyphSize(graphics, CheckBoxState.UncheckedNormal).Width;
         int boxHeight = CheckBoxRenderer.GetGlyphSize(graphics, CheckBoxState.UncheckedNormal).Height;
         int left = mgCheckBox.Padding.Left;
         int top = 0;

         //CheckBoxRenderer.GetGlyphSize() always returns the size for 3D style.But, the 2D box is smaller.So, the focus rectangle was too big.
         //There is no way where we can query the size for 2D.Framework has hardcoded the size of the Box as { 11, 11} in CheckBoxFlatAdapter.Layout().
         //So, we also need to hard-code this value. Note that, if framework changes these hard-coded values, we will also need to change them.
         if (mgCheckBox.FlatStyle == FlatStyle.Flat)
            boxWidth = boxHeight = (int)(11f * Utils.GetDpiScaleRatioY(mgCheckBox));

         // calculate the Left based on RightToLeft property.
         // Depends on RightToLeft and NOT Horizontal alignment.
         if (mgCheckBox.RightToLeft == RightToLeft.Yes)
            left = clientRectangle.Width - boxWidth - 1;

         // calculate the Top based on Vertical alignment. 
         ControlUtils.AlignmentInfo AlignmentInfo = ControlUtils.GetAlignmentInfo(mgCheckBox.TextAlign);

         switch (AlignmentInfo.VerAlign)
         {
            case AlignmentTypeVert.Top:
               top = 2 + mgCheckBox.Padding.Top;
               break;

            case AlignmentTypeVert.Center:
               top = (clientRectangle.Height - boxHeight)/2;
               break;

            case AlignmentTypeVert.Bottom:
               top = clientRectangle.Height - boxHeight - 1 - mgCheckBox.Padding.Bottom;
               break;
         }

         focusRectangle.X = left;
         focusRectangle.Y = top;
         focusRectangle.Width = boxWidth;
         focusRectangle.Height = boxHeight;
         focusRectangle.Inflate(1, 1);

         //Ensure that the rectangle is within the control.
         focusRectangle.X = Math.Max(focusRectangle.X, 0);
         focusRectangle.Y = Math.Max(focusRectangle.Y, 0);
         focusRectangle.Width = Math.Min(focusRectangle.Width, clientRectangle.Right - focusRectangle.X);
         focusRectangle.Height = Math.Min(focusRectangle.Height, clientRectangle.Bottom - focusRectangle.Y);

         ControlPaint.DrawFocusRectangle(graphics, focusRectangle);
      }
   }
}
