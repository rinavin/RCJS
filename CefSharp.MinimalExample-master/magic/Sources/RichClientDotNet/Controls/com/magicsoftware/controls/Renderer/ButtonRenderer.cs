using System;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
using com.magicsoftware.controls.utils;
using com.magicsoftware.util;
using Controls.com.magicsoftware.support;
#if PocketPC
using RightToLeft = com.magicsoftware.mobilestubs.RightToLeft;
using ContentAlignment = OpenNETCF.Drawing.ContentAlignment2;
using ControlPaint = com.magicsoftware.richclient.mobile.gui.ControlPaint;
#endif

namespace com.magicsoftware.controls
{
   /// <summary> This class provides methods to draw the background and text of buttons --- PushButton and 
   /// CheckBox & RadioButton with appearance=Button</summary>
   public class ButtonRenderer
   {
      /// <summary>paint the push button control</summary>
      /// <param name="mgButton"></param>
      /// <param name="e"></param>
      public static void DrawButton(Control control, PaintEventArgs e, bool paintBackground, bool drawText)
      {
         if (paintBackground)
            PaintBackground(control, e);
         if (drawText)
            DrawText(control, e);
      }

      /// <summary>paint the backround button</summary>
      /// <param name="mgButton"></param>
      /// <param name="e"></param>
      private static void PaintBackground(Control buttonBase, PaintEventArgs e)
      {
         Rectangle displayRect = buttonBase.ClientRectangle;

         // when button is image , the display rect is on all the button, else the display rect is on the tect rect
         if (!ControlUtils.IsImageButton(buttonBase))
            displayRect = GetTextRect(buttonBase);

         ControlRenderer.PaintBackgoundColorAndImage(buttonBase, e.Graphics, false, displayRect);

#if PocketPC
         // Draw the border for the button-appearance checkbox
         if (buttonBase is MgCheckBox)
         {
            Rectangle rect = buttonBase.ClientRectangle;
            rect.Inflate(-1, -1);
            Color fgColor = buttonBase.ForeColor;
            ControlStyle borderStyle = (((MgCheckBox)buttonBase).CheckState == CheckState.Unchecked) ? ControlStyle.ThreeD : ControlStyle.TwoD;
            BorderRenderer.PaintBorder(e.Graphics, rect, fgColor, borderStyle, false);
         }
#endif
      }

      /// <summary>paint the push button control
      /// Can be called from :
      /// 1. push control with property 'ButtonStyle' = button
      /// 2. CheckBox control with property appearance = button
      /// 3. Radio control with property appearance = button
      /// </summary>
      /// <param name="mgButton"></param>
      /// <param name="e"></param>
      private static void DrawText(Control control, PaintEventArgs e)
      {
         Debug.Assert(control.Visible);
         bool restoreClip = false;
         Region r = e.Graphics.Clip;
         bool isImage = false;
         ContentAlignment textAlign = ContentAlignment.MiddleLeft;
         RightToLeft rightToLeft = RightToLeft.No;

         String Text = ((IDisplayInfo)control).TextToDisplay;
         if (Text == null)
            return;

         Rectangle displayRect = control.ClientRectangle;

#if PocketPC
         if (control is MgButtonBase)
         {
            isImage = ((MgButtonBase)control).BackgroundImage != null;
            textAlign = ((MgButtonBase)control).TextAlign;
            rightToLeft = ((MgButtonBase)control).RightToLeft;
         }
         else if (control is MgCheckBox)
         {
            isImage = ((MgCheckBox)control).Image != null;
            textAlign = ((MgCheckBox)control).TextAlign;
            rightToLeft = ((MgCheckBox)control).RightToLeft;
         }
         else
            Debug.Assert(false);
#else
         if (control is ButtonBase)
         {
            if (control is IImageProperty)
               isImage = ((IImageProperty)control).Image != null;
            else
               isImage = ((ButtonBase)control).BackgroundImage != null;

            textAlign = ((ButtonBase)control).TextAlign;
            rightToLeft = ((ButtonBase)control).RightToLeft;
         }
         else
            Debug.Assert(false);
#endif

         // The runtime engine sends the alignment in reverse order if rightToLeft=Yes.
         // This is because the .net framework need it to be in the reverse order when rightToLeft=Yes.
         // So, when we paint the control, we should convert it back to the original alignment.
         textAlign = ControlUtils.GetOrgContentAligment(rightToLeft, textAlign);

         if (!isImage)
         {
            //2. get the display rect
            displayRect = GetTextRect(control);
            if (control is MgPushButton)
            {
               restoreClip = true;
               using (Region TextRegion = new Region(displayRect))
               {
                  e.Graphics.Clip = TextRegion;
               }
            }
         }

         bool isMultiLine = ControlUtils.GetIsMultiLine(control);
         bool RTL = rightToLeft == RightToLeft.Yes ? true : false;
         //4. display the text of the control

         FontDescription font = new FontDescription(control.Font);
         ControlRenderer.PrintText(e.Graphics, displayRect, control.ForeColor, font, Text, isMultiLine,
                                   textAlign, control.Enabled, false, false, false, RTL, true);
         if (restoreClip)
            e.Graphics.Clip = r;

         //focus rect should not be drawn for image button.
         if (!ControlUtils.IsImageButton(control))
         {
            if (control.Focused)
            {
               displayRect.Inflate(-1, -1);
               ControlPaint.DrawFocusRectangle(e.Graphics, displayRect);
            }
         }
      }

      /// <summary>get the display rect for MgButton</summary>
      /// <param name="mgButton"></param>
      private static Rectangle GetTextRect(Control controlAsPush)
      {
         Rectangle displayRect = controlAsPush.ClientRectangle;
         int borderWidth = Utils.IsXPStylesActive() ? 3 : 2;

         if (ControlUtils.IsImageButton(controlAsPush))
            borderWidth = 0;

         displayRect.Inflate(-borderWidth, -borderWidth);

         return displayRect;
      }
   }
}
