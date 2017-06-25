using com.magicsoftware.controls;
using com.magicsoftware.util;
using com.magicsoftware.win32;
using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

#if PocketPC
using ContextMenuStrip = com.magicsoftware.mobilestubs.ContextMenuStrip;
using ContentAlignment = OpenNETCF.Drawing.ContentAlignment2;
using ContextMenu = com.magicsoftware.controls.MgMenu.MgContextMenu;
#endif

namespace com.magicsoftware.unipaas.gui.low
{
   internal class LgText : LogicalControl
   {
      internal Color? FocusFGColor { get; set; }
      internal Color? FocusBGColor { get; set; }

      internal int MgFocusColorIndex { get; set; }

      internal LgText(GuiMgControl guiMgControl, Control containerControl)
         : base(guiMgControl, containerControl)
      {
         ShowBorder = true;
         ImeMode = -1;
         WordWrap = HorizontalScrollBar == MultilineHorizontalScrollBar.WordWrap;
         ContentAlignment = ContentAlignment.TopLeft;
      }

      /// <summary>
      /// Checks if hint is enabled
      /// </summary>
      public bool IsHintEnabled
      {
         get
         {
            return (Text == "" && HintText != null && HintText.Trim() != "" && Modifable);
         }
      }

      /// <summary>
      /// Hint text
      /// </summary>
      private string hintText;
      public string HintText
      {
         get
         {
            return hintText;
         }
         set
         {
            bool changed = hintText != value;
            hintText = value;
            Refresh(changed);
         }
      }
      static Color hintDefaultColor = SystemColors.GrayText;

      /// <summary>
      /// Hint foreground color
      /// </summary>
      private Color hintForeColor = Color.Gray;
      public Color HintForeColor
      {
         get
         {
            return hintForeColor;
         }
         set
         {
            bool changed = hintForeColor != value || hintForeColor != hintDefaultColor;
            if (value == Color.Empty)
               hintForeColor = hintDefaultColor;
            else
               hintForeColor = value;

            Refresh(changed);
         }
      }

#if !PocketPC
      internal override Font Font
      {
         get
         {
            if (base.Font == null)
               // QCR #999081: the font is used for a Logical Text control before even being initialized.
               // We need a better fix that avoids calling get_Font() for logical controls that are not initialized properly.
               return SystemFonts.DefaultFont;
            else
               return base.Font;
         }
         set { base.Font = value; }
      }
#endif

      /// <summary>
      ///   on table the background of disabled controls isn't gray
      /// </summary>
      internal bool EnabledBackGround
      {
         get { return Enabled || _coordinator is TableCoordinator; }
      }

      internal override void paintBackground(Graphics g, Rectangle rect, Color bgColor, Color fgColor, bool keepColor)
      {
         ControlStyle style = MgTextBox.ShouldDrawFlatTextBox ? ControlStyle.TwoD : ControlStyle.Windows;

         style = (ShowBorder ? style : ControlStyle.NoBorder);
         Rectangle drawRect = rect;
#if PocketPC
   // On Mobile, the offset is not set in the Graphics. Therefore we need to change
   // the offset for all static controls
         
            offsetStaticControlRect(ref drawRect);

#endif
         ControlRenderer.PaintBackgroundAndBorder(g, drawRect, bgColor, FgColor, style, true, EnabledBackGround || bgColor == Color.Transparent);
      }


      internal override void paintForeground(Graphics g, Rectangle rect, Color fgColor)
      {
         Rectangle drawRect = rect;
#if PocketPC
   // On Mobile, the offset is not set in the Graphics. Therefore we need to change
   // the offset for all static controls
            offsetStaticControlRect( ref drawRect);
#endif

         if (ShowBorder)
         {
            Rectangle saveRect = drawRect;
            //Temporary calculation of text position when control has border
            drawRect.Inflate(-2, -2);
            drawRect.X += 1;
            drawRect.Width -= 1;
            drawRect.Y += 1;
            drawRect.Height -= 1;
#if !PocketPC
            //fixed bug #:771694
            if (Font != null && drawRect.Height < Font.Height && saveRect.Height > Font.Height)
            {
               drawRect = saveRect;
               drawRect.Inflate(-2, -2);
            }
#endif
         }

         String str = IsHintEnabled ? HintText : Text;
         if (PasswordEdit && str != null && !IsHintEnabled)
         {
            StringBuilder sb = new StringBuilder();
            sb.Append(GuiUtils.getCharPass(), str.Length);
            str = sb.ToString();
         }

         printText(g, drawRect, IsHintEnabled ? HintForeColor : fgColor, str);
      }

      /// <summary>
      /// refresh the logical control
      /// </summary>
      /// <param name="changed"></param>
      public override void Refresh(bool changed)
      {
         if (changed && getEditorControl() != null)
            _coordinator.RefreshNeeded = true;
         else
            base.Refresh(changed);
      }

      /// <summary>
      ///   set all properties to text control
      /// </summary>
      /// <param name = "control"></param>
      internal override void setSpecificControlProperties(Control control)
      {
         _coordinator.RefreshNeeded = false;
         MgTextBox mgTextBox = (MgTextBox)control;
         setEditControlProperties(mgTextBox);
         ControlUtils.SetHint(mgTextBox, HintText);
         ControlUtils.SetHintColor(mgTextBox, HintForeColor);   
      }

      /// <summary>
      /// 
      /// </summary>
      protected override void SetMargin(Control control)
      {
         base.SetMargin(control);
         // fixed defect #:135398  & 136561
         // same as old defect(fix in edt_edit.cpp)  QCR#793923: Caret for hebrew edit control is not seen fully when there is no text.
         // Exact reason for this problem is not known. However, setting right margin to 1, seems to
         // fix this problem. Other solution discussed was, removing WS_CLIPSIBLINGS style from
         // edit control. But this looked to be risky, as impact on the behavior for existing problems
         // cannot be predicted.

         // fixed 136561: this code must be execute after set right to left property
         if (Manager.Environment.Language == 'H')
            NativeWindowCommon.SendMessage(control.Handle, NativeWindowCommon.EM_SETMARGINS,
                                           NativeWindowCommon.EC_LEFTMARGIN | NativeWindowCommon.EC_RIGHTMARGIN, 
                                           NativeWindowCommon.MakeLong(0, 1));


      }

      /// <summary>
      /// while user defined focus color, set the new color to the control
      /// </summary>
      /// <param name="control"></param>
      /// <param name="bgColor"></param>
      /// <param name="fgColor"></param>
      protected override void SetFocusColor(Control control, Color bgColor, Color fgColor, bool isInFocus)
      {
         TagData tag = control.Tag as TagData;
         if (tag != null)
         {
            tag.FocusBGColor = FocusBGColor;
            tag.FocusFGColor = FocusFGColor;

            if (this.FocusBGColor != null && FocusFGColor != null)
            {
               if (GuiUtils.AccessTest)
               {
                  if (isInFocus)
                     GuiUtils.SetFocusColor(control as TextBox);
                  else
                  {
                     GuiUtils.ResetFocusColor(control as TextBox);
                     SetColors(control, (Color)bgColor, (Color)fgColor);
                  }
               }
               else
               {
                  SetColors(control, (Color)FocusBGColor, (Color)FocusFGColor, true);
               }
            }
         }
      }
      /// <summary>
      ///   set IME mode (JPN: IME support)
      /// </summary>
      /// <param name = "mode">
      /// </param>
      internal void setImeMode(int mode)
      {
         ImeMode = mode;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="refreshNow"></param>
      internal override void RecalculateColors(bool refreshNow)
      {
         if (MgFocusColorIndex != 0)
         {
            FocusBGColor = ColorIndexToColor(MgFocusColorIndex, true);
            FocusFGColor = ColorIndexToColor(MgFocusColorIndex, false);
         }

         base.RecalculateColors(refreshNow);
      }
   }
}