using System;
using System.Collections;
using System.Windows.Forms;
using com.magicsoftware.util;
using System.Drawing;
using System.Diagnostics;
using System.Collections.Generic;
using BorderType = com.magicsoftware.util.BorderType;
using com.magicsoftware.support;
using com.magicsoftware.win32;
#if !PocketPC
using ContentAlignment = System.Drawing.ContentAlignment;
using System.Windows.Forms.VisualStyles;
using com.magicsoftware.controls.utils;
using System.ComponentModel;
using Controls.com.magicsoftware.controls.PropertyInterfaces;
using System.ComponentModel.Design;
using Controls.com.magicsoftware.controls.MgShape;
#else
using RightToLeft = com.magicsoftware.mobilestubs.RightToLeft;
using ContentAlignment = OpenNETCF.Drawing.ContentAlignment2;
using FlatStyle = com.magicsoftware.mobilestubs.FlatStyle;
using Appearance = com.magicsoftware.mobilestubs.Appearance;
using TabAlignment = com.magicsoftware.mobilestubs.TabAlignment;
using Panel = com.magicsoftware.controls.MgPanel;
#endif

namespace com.magicsoftware.controls
{
   /// <summary>
   /// This is the utility class which provides methods for setting/getting properties of the Magic controls.
   /// </summary>
   public class ControlUtils
   {
      private static ContentAlignmentDict _contentAlignmentTable;
      private static ContentAlignment[,] _horVerTranslation;

      public static GetSubFormControlmDelegate GetSubFormControl;
      public static IsSubFormControlDelegate IsSubFormControl;
      static ControlUtils()
      {
         InitContentAligmentInfo();
      }

#if !PocketPC
      /// <summary>
      /// get control for currently focused window
      /// </summary>
      /// <returns></returns>
      public static Control GetFocusedControl()
      {
         Control focusControl = null;
         IntPtr focusHandle = NativeWindowCommon.GetFocus();
         if (focusHandle != IntPtr.Zero)
            // returns null if handle is not to a .NET control
            focusControl = Control.FromHandle(focusHandle);
         return focusControl;
      }

#endif

      /// <summary>
      /// Sets transpanercy property of MgLabel
      /// </summary>
      /// <param name="control"></param>
      /// <param name="isTransparentOnHeader"></param>
      public static void SetTransparentOnHeader(Control control, bool isTransparentOnHeader)
      {
         if (control is MgLabel)
         {
            ((MgLabel)control).IsTransparentWhenOnHeader = isTransparentOnHeader;
         }
      }

      /// <summary> Init the content alignment table 
      /// </summary>
      private static void InitContentAligmentInfo()
      {
         //ContentAlignment =  TopLeft = 1, TopCenter = 2, TopRight = 4, 
         //                    MiddleLeft = 16, MiddleCenter = 32,MiddleRight = 64
         //                    BottomLeft = 256, BottomCenter = 512, BottomRight = 1024        
         _horVerTranslation = new ContentAlignment[3, 3];
         _horVerTranslation[0, 0] = ContentAlignment.TopLeft;
         _horVerTranslation[0, 1] = ContentAlignment.TopCenter;
         _horVerTranslation[0, 2] = ContentAlignment.TopRight;
         _horVerTranslation[1, 0] = ContentAlignment.MiddleLeft;
         _horVerTranslation[1, 1] = ContentAlignment.MiddleCenter;
         _horVerTranslation[1, 2] = ContentAlignment.MiddleRight;
         _horVerTranslation[2, 0] = ContentAlignment.BottomLeft;
         _horVerTranslation[2, 1] = ContentAlignment.BottomCenter;
         _horVerTranslation[2, 2] = ContentAlignment.BottomRight;

         _contentAlignmentTable = new ContentAlignmentDict();

         //Ver ->Button
         _contentAlignmentTable[ContentAlignment.BottomCenter] = new AlignmentInfo(AlignmentTypeVert.Bottom, AlignmentTypeHori.Center);
         _contentAlignmentTable[ContentAlignment.BottomLeft] = new AlignmentInfo(AlignmentTypeVert.Bottom, AlignmentTypeHori.Left);
         _contentAlignmentTable[ContentAlignment.BottomRight] = new AlignmentInfo(AlignmentTypeVert.Bottom, AlignmentTypeHori.Right);
         //Ver ->Center
         _contentAlignmentTable[ContentAlignment.MiddleCenter] = new AlignmentInfo(AlignmentTypeVert.Center, AlignmentTypeHori.Center);
         _contentAlignmentTable[ContentAlignment.MiddleLeft] = new AlignmentInfo(AlignmentTypeVert.Center, AlignmentTypeHori.Left);
         _contentAlignmentTable[ContentAlignment.MiddleRight] = new AlignmentInfo(AlignmentTypeVert.Center, AlignmentTypeHori.Right);
         //Ver ->Top
         _contentAlignmentTable[ContentAlignment.TopCenter] = new AlignmentInfo(AlignmentTypeVert.Top, AlignmentTypeHori.Center);
         _contentAlignmentTable[ContentAlignment.TopLeft] = new AlignmentInfo(AlignmentTypeVert.Top, AlignmentTypeHori.Left);
         _contentAlignmentTable[ContentAlignment.TopRight] = new AlignmentInfo(AlignmentTypeVert.Top, AlignmentTypeHori.Right);
      }

      /// <summary>
      /// enable/disable XP themes
      /// </summary>
      /// <param name="enableXPThemes">'true' to enable XPThemes.</param>
      public static void EnableXPThemes(bool enableXPThemes)
      {
#if !PocketPC
         if (enableXPThemes && Utils.IsXPStylesActive())
            Application.EnableVisualStyles();
         Application.VisualStyleState = (enableXPThemes
                                            ? VisualStyleState.ClientAndNonClientAreasEnabled
                                            : VisualStyleState.NonClientAreaEnabled);
#endif
      }

      /// <summary> Sets the Multiline prop of a control 
      /// </summary>
      public static void SetMultiLine(Control control, bool isMultiLine)
      {
         IMultilineProperty multiLineRenderer = control as IMultilineProperty;

         if (multiLineRenderer != null)
            multiLineRenderer.Multiline = isMultiLine;
      }

      /// <summary>
      /// return isMultiLine of the control, if it is child of MgRadioPanel then take the isMultiLine from the parent
      /// </summary>
      /// <param name="control"></param>
      /// <returns></returns>
      public static bool GetIsMultiLine(Control control)
      {
         bool isMultiLine = false;

         // MgRadioButton is on MgRadioPanel and multiline info is saved on MgRadioPanel.
         if (control is MgRadioButton)
            control = control.Parent;

         IMultilineProperty multiLineRenderer = control as IMultilineProperty;
         if (multiLineRenderer != null)
            isMultiLine = multiLineRenderer.Multiline;

         return isMultiLine;
      }

      /// <summary> Sets vertical alignment of the control. </summary>
      /// <param name="control"></param>
      /// <param name="verticalAlignment"></param>
      public static void SetVerticalAlignment(Control control, AlignmentTypeVert verticalAlignment)
      {
         if (control is MgLinkLabel || control is MgButtonBase || control is Label || control is MgRadioPanel || control is MgCheckBox || control is MgTextBox || control is MgShape)
         {
            ContentAlignment contentAlignment = GetContentAligmentForSetVerAligment(control, verticalAlignment);
            SetContentAlignment(control, contentAlignment);
         }
      }

      /// <summary> Sets horizontal alignment of the control. </summary>
      /// <param name="control"></param>
      /// <param name="verticalAlignment"></param>
      public static void SetHorizontalAlignment(Control control, AlignmentTypeHori horizontalAlignment)
      {
         if (control is MgLinkLabel || control is MgButtonBase || control is Label || control is MgRadioPanel || control is MgCheckBox || control is MgTextBox || control is MgShape)
         {
            ContentAlignment contentAlignment = GetContentAligmentForSetHorAligment(control, horizontalAlignment);
            SetContentAlignment(control, contentAlignment);
         }
      }

      /// <summary>
      /// Sets hint text to MgTextBox (edit control)
      /// </summary>
      /// <param name="control"></param>
      /// <param name="hint"></param>
      public static void SetHint(MgTextBox hintTextBox, string hint)
      {
         //Set hint text and change strategy
         bool setHint = hint != null && hint.Trim() != "";
         if (setHint)
            hintTextBox.EnableHintStrategy(hint);
         else
         {//remove strategy when hint text is removed
            hintTextBox.DisableHintStrategy();
            hintTextBox.Refresh();
         }
      }
      
      /// <summary>
      /// Sets hint text color to MgTextBox (edit control)
      /// </summary>
      /// <param name="hintTextBox"></param>
      /// <param name="hintColor"></param>
      public static void SetHintColor(MgTextBox hintTextBox, Color hintFgColor)
      {
         if (hintTextBox != null)
            hintTextBox.TextBoxStrategy.HintFgColor = hintFgColor;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="control"></param>
      /// <param name="VerAligment"></param>
      /// <returns></returns>
      internal static ContentAlignment GetContentAligmentForSetVerAligment(Control control, AlignmentTypeVert VerAligment)
      {
         //get the current content alignment of the control
         ContentAlignment CurrContentAlignment = GetContentAlignment(control);

         return GetContentAligmentForSetVerAligment(CurrContentAlignment, VerAligment);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="contentAlignment"></param>
      /// <param name="VerAligment"></param>
      /// <returns></returns>
      public static ContentAlignment GetContentAligmentForSetVerAligment(ContentAlignment contentAlignment, AlignmentTypeVert VerAligment)
      {
         ContentAlignment RetContentAlignment;

         //get the Current Content alignment info from the ContentAlignmentTable
         AlignmentInfo aligmentInfo = _contentAlignmentTable[contentAlignment];
         //get the current Horizontal alignment
         AlignmentTypeHori CurrHorAligment = aligmentInfo.HorAlign;

         //get the new Contant alignment from the HorVerConversion;
         RetContentAlignment = _horVerTranslation[(int)(VerAligment - 1), (int)(CurrHorAligment - 1)];

         return RetContentAlignment;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="control"></param>
      /// <param name="HorAligment"></param>
      /// <returns></returns>
      internal static ContentAlignment GetContentAligmentForSetHorAligment(Control control, AlignmentTypeHori HorAligment)
      {
         //get the current content alignment of the control
         ContentAlignment CurrContentAlignment = GetContentAlignment(control);

         return GetContentAligmentForSetHorAligment(CurrContentAlignment, HorAligment);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="contentAlignment"></param>
      /// <param name="HorAligment"></param>
      /// <returns></returns>
      public static ContentAlignment GetContentAligmentForSetHorAligment(ContentAlignment contentAlignment, AlignmentTypeHori HorAligment)
      {
         ContentAlignment RetContentAlignment;

         //get the Current Content alignment info from the ContentAlignmentTable
         AlignmentInfo aligmentInfo = _contentAlignmentTable[contentAlignment];
         //get the current Vertical alignment
         AlignmentTypeVert CurrVerAligment = aligmentInfo.VerAlign;

         //get the new Contant alignment from the HorVerConversion;
         RetContentAlignment = _horVerTranslation[(int)(CurrVerAligment - 1), (int)(HorAligment - 1)];

         return RetContentAlignment;
      }

      /// <summary>Sets the text alignment of control</summary>
      /// <param name="label"></param>
      /// <param name="contentAlignment"></param>
      public static void SetContentAlignment(Control control, ContentAlignment contentAlignment)
      {
         if (control is IContentAlignmentProperty)
            ((IContentAlignmentProperty)control).TextAlign = contentAlignment;
         else
            Debug.Assert(false);
      }

      /// <summary>Sets the text alignment of control</summary>
      /// <param name="control"></param>
      /// <returns></returns>
      private static ContentAlignment GetContentAlignment(Control control)
      {
         ContentAlignment retContentAlignment = ContentAlignment.MiddleCenter;

         if (control is IContentAlignmentProperty)
            retContentAlignment = ((IContentAlignmentProperty)control).TextAlign;
         else
            Debug.Assert(false);

         return retContentAlignment;
      }

      /// <summary>Set the CheckAlign according to the TextAlignment of the control</summary>
      /// <param name="CurrContentAlignment"></param>
      /// <param name="HorAligment"></param>
      /// <returns></returns>
      internal static void SetCheckAlign(Control control)
      {
         ContentAlignment checkContentAlignment = ContentAlignment.MiddleLeft;
         ContentAlignment TextAlign = ContentAlignment.MiddleLeft;

         if (control is MgCheckBox)
            TextAlign = ((MgCheckBox)control).TextAlign;
         else if (control is MgRadioButton)
            TextAlign = ((MgRadioButton)control).TextAlign;
         else
            Debug.Assert(false);

         switch (TextAlign)
         {
            case ContentAlignment.BottomCenter:
            case ContentAlignment.BottomLeft:
            case ContentAlignment.BottomRight:
               checkContentAlignment = ContentAlignment.BottomLeft;
               break;
            case ContentAlignment.MiddleCenter:
            case ContentAlignment.MiddleLeft:
            case ContentAlignment.MiddleRight:
               checkContentAlignment = ContentAlignment.MiddleLeft;
               break;
            case ContentAlignment.TopCenter:
            case ContentAlignment.TopLeft:
            case ContentAlignment.TopRight:
               checkContentAlignment = ContentAlignment.TopLeft;
               break;
            default:
               break;
         }

         if (control is MgCheckBox)
            ((MgCheckBox)control).CheckAlign = checkContentAlignment;
         else if (control is MgRadioButton)
            ((MgRadioButton)control).CheckAlign = checkContentAlignment;
         else
            Debug.Assert(false);
      }

      /// <summary>Set the ImageAlign according to the TextAlignment of the control</summary>
      /// <param name="CurrContentAlignment"></param>
      /// <param name="HorAligment"></param>
      /// <returns></returns>
      internal static void SetImageAlign(Control control)
      {
#if !PocketPC //temp
         if (control is ButtonBase)
            ((ButtonBase)control).ImageAlign = ((ButtonBase)control).TextAlign;
         else
            Debug.Assert(false);
#endif
      }

      /// <summary>return the display rect of the display size according to the contentAlignment</summary>
      /// <param name="ClientRect"></param>
      /// <param name="DisplaySize"></param>
      /// <param name="contentAlignment"></param>
      /// <returns></returns>
      public static Rectangle GetFocusRect(Control control, Rectangle displayRect,
                                           ContentAlignment contentAlignment, Size textSize)
      {
         AlignmentInfo aligmentInfo = GetAlignmentInfo(contentAlignment);
         //get the current vertical\horizontal aligment
         AlignmentTypeVert alignmentTypeVert = aligmentInfo.VerAlign;
         AlignmentTypeHori alignmentTypeHori = aligmentInfo.HorAlign;

         Rectangle focusRect = new Rectangle(displayRect.X, displayRect.Y, textSize.Width, textSize.Height);

         switch (alignmentTypeVert)
         {
            case AlignmentTypeVert.Top:
               break;
            case AlignmentTypeVert.Bottom:
               focusRect.Y = displayRect.Bottom - textSize.Height;
               break;
            case AlignmentTypeVert.Center:
               focusRect.Y += (displayRect.Height - textSize.Height) / 2;
               break;
         }

         switch (alignmentTypeHori)
         {
            case AlignmentTypeHori.Left:
               break;
            case AlignmentTypeHori.Right:
               focusRect.X = displayRect.Right - textSize.Width;
               break;
            case AlignmentTypeHori.Center:
               focusRect.X += (displayRect.Width - textSize.Width) / 2;
               break;
         }
         //check the rect focus that is not out the control
         focusRect.X = Math.Max(1, focusRect.X);
         focusRect.Width = Math.Min(focusRect.Width, control.ClientRectangle.Width - focusRect.X);

         return focusRect;
      }

      /// <summary> return the AlignmentInfo according to send contentAlignment
      /// </summary>
      /// <param name="contentAlignment"></param>
      /// <returns></returns>
      public static AlignmentInfo GetAlignmentInfo(ContentAlignment contentAlignment)
      {
         return _contentAlignmentTable[contentAlignment];
      }

      /// <summary> return alignment flags for horizontal and vertical alignment
      /// </summary>
      /// <param name="HorAligment"></param>
      /// <param name="VerAligment"></param>
      /// <returns></returns>
      private static ContentAlignment GetContentAligmentForHorAligmentAndVerAligment(AlignmentTypeHori HorAligment, AlignmentTypeVert VerAligment)
      {
         ContentAlignment RetContentAlignment = _horVerTranslation[(int)(VerAligment - 1), (int)(HorAligment - 1)];
         return RetContentAlignment;
      }

      /// <summary>
      /// in WebInfo.cpp we send the HorAligement property revers, because the control displays the text according to the RTL Property
      /// the printText() displays the text according to the send aligment so we need to reverse the org horisontal aligment
      /// </summary>
      /// <param name="rightToLeft"></param>
      /// <param name="AlignmentInfo"></param>
      /// <param name="TextAlign"></param>
      /// <returns></returns>
      public static ContentAlignment GetOrgContentAligment(RightToLeft rightToLeft, ContentAlignment TextAlign)
      {
         AlignmentInfo alignmentInfo = GetAlignmentInfo(TextAlign);
         ContentAlignment NewTextAli = TextAlign;
         if (rightToLeft == RightToLeft.Yes && alignmentInfo.HorAlign != AlignmentTypeHori.Center)
         {
            AlignmentTypeHori newTypeHor = (alignmentInfo.HorAlign == AlignmentTypeHori.Right ? AlignmentTypeHori.Left : AlignmentTypeHori.Right);
            NewTextAli = GetContentAligmentForHorAligmentAndVerAligment(newTypeHor, alignmentInfo.VerAlign);
         }

         return NewTextAli;
      }

      /// <summary>
      /// Gets the System.Windows.Forms.HorizontalAlignment corresponding to AlignmentTypeHori
      /// </summary>
      /// <param name="alignmentTypeHori"></param>
      /// <returns></returns>
      public static HorizontalAlignment HorAlign2HorAlign(AlignmentTypeHori alignmentTypeHori)
      {
         HorizontalAlignment horiAlignment = HorizontalAlignment.Left;

         if (alignmentTypeHori == AlignmentTypeHori.Left)
            horiAlignment = HorizontalAlignment.Left;
         else if (alignmentTypeHori == AlignmentTypeHori.Center)
            horiAlignment = HorizontalAlignment.Center;
         else if (alignmentTypeHori == AlignmentTypeHori.Right)
            horiAlignment = HorizontalAlignment.Right;

         return horiAlignment;
      }


      /// <summary>
      /// Converts TextBox ContentAlignment to String Alignment
      /// </summary>
      /// <returns></returns>
      public static StringAlignment ContentAlignmentToStringAlignment(MgTextBox control)
      {
         AlignmentInfo alignmentInfo = GetAlignmentInfo(control.TextAlign);
         switch (alignmentInfo.HorAlign)
         {
            case AlignmentTypeHori.Center:
               return StringAlignment.Center;

            case AlignmentTypeHori.Left:
               if (control.RightToLeft == RightToLeft.Yes)
                  return StringAlignment.Far;
               else
                  return StringAlignment.Near;

            case AlignmentTypeHori.Right:
               if (control.RightToLeft == RightToLeft.Yes)
                  return StringAlignment.Near;
               else
                  return StringAlignment.Far;

            default:
               return StringAlignment.Near;
         }
      }

      ///<summary>
      /// Set RightToLeft prop of a control
      ///</summary>
      ///<param name="control"></param>
      ///<param name="rightToLeftval"></param>
      ///<returns></returns>
      public static bool SetRightToLeft(Control control, bool rightToLeftval)
      {
         bool succeeded = false;
         RightToLeft rightToLeft = (rightToLeftval ? RightToLeft.Yes : RightToLeft.No);

         if (control is IRightToLeftProperty)
         {
            ((IRightToLeftProperty)control).RightToLeft = rightToLeft;
            succeeded = true;
         }

         return succeeded;
      }

      /// <summary>Converts MgColor to an System color</summary>
      /// <param name="mgColor"></param>
      /// <param name="checkTransparent">if true, returns null for transparent colors, if false - return real bgcolor</param>
      /// <param name="useAlpha">if true, use alpha(opacity) value of the color</param>
      /// <returns></returns>
      public static Color MgColor2Color(MgColor mgColor, bool checkTransparent, bool useAlpha)
      {
         Color color = Color.Empty;
         if (mgColor.IsTransparent && checkTransparent)
            color = Color.Transparent;
         else if (mgColor.IsSystemColor)
         {
            switch (mgColor.SystemColor)
            {
               case MagicSystemColor.ScrollBar:
                  color = SystemColors.ScrollBar;
                  break;
               case MagicSystemColor.Background:
                  color = SystemColors.Desktop;
                  break;
               case MagicSystemColor.ActiveCaption:
                  color = SystemColors.ActiveCaption;
                  break;
               case MagicSystemColor.InactiveCaption:
                  color = SystemColors.InactiveCaption;
                  break;
               case MagicSystemColor.Menu:
                  color = SystemColors.Menu;
                  break;
               case MagicSystemColor.Window:
                  color = SystemColors.Window;
                  break;
               case MagicSystemColor.WindowFrame:
                  color = SystemColors.WindowFrame;
                  break;
               case MagicSystemColor.MenuText:
                  color = SystemColors.MenuText;
                  break;
               case MagicSystemColor.WindowText:
                  color = SystemColors.WindowText;
                  break;
               case MagicSystemColor.CaptionText:
                  color = SystemColors.ActiveCaptionText;
                  break;
               case MagicSystemColor.ActiveBorder:
                  color = SystemColors.ActiveBorder;
                  break;
               case MagicSystemColor.InActiveBorder:
                  color = SystemColors.InactiveBorder;
                  break;
               case MagicSystemColor.AppWorkSpace:
                  color = SystemColors.AppWorkspace;
                  break;
               case MagicSystemColor.Highlight:
                  color = SystemColors.Highlight;
                  break;
               case MagicSystemColor.HighlightText:
                  color = SystemColors.HighlightText;
                  break;
               case MagicSystemColor.BtnFace:
                  color = SystemColors.Control;             //SystemColors.ButtonFace -> in CF we don't have it, use its equivalent.
                  break;
               case MagicSystemColor.BtnShadow:
                  color = SystemColors.ControlDark;         //SystemColors.ButtonShadow
                  break;
               case MagicSystemColor.BtnHighlight:
                  color = SystemColors.ControlLightLight;   //SystemColors.ButtonHighlight
                  break;
               case MagicSystemColor.GrayText:
                  color = SystemColors.GrayText;
                  break;
               case MagicSystemColor.BtnText:
                  color = SystemColors.ControlText;
                  break;
               case MagicSystemColor.InActiveCaptionText:
                  color = SystemColors.InactiveCaptionText;
                  break;
               case MagicSystemColor.ThreeDDarkShadow:
                  color = SystemColors.ControlDarkDark;
                  break;
               case MagicSystemColor.ThreeDLight:
                  color = SystemColors.ControlLight;
                  break;
               case MagicSystemColor.InfoText:
                  color = SystemColors.InfoText;
                  break;
               case MagicSystemColor.Info:
                  color = SystemColors.Info;
                  break;
               default:
                  color = Color.Black;
                  break;
            }
         }
#if !PocketPC
         else if (useAlpha)
            color = Color.FromArgb(mgColor.Alpha, mgColor.Red, mgColor.Green, mgColor.Blue);
#endif
         else
            color = Color.FromArgb(mgColor.Red, mgColor.Green, mgColor.Blue);

         return color;
      }

      /// <summary></summary>
      /// <param name="control"></param>
      /// <param name="ForegroundColor"></param>
      public static void SetFGColor(Control control, Color foregroundColor)
      {
         control.ForeColor = foregroundColor;
         if (control is MgLinkLabel)
            ((MgLinkLabel)control).OriginalFGColor = foregroundColor;
      }

      /// <summary></summary>
      /// <param name="control"></param>
      /// <param name="backgroundColor"></param>
      public static void SetBGColor(Control control, Color backgroundColor)
      {
         // QCR #781950: catch a possible exception when the control doesn't support transparency
         try
         {
            if (control is MgLinkLabel)
               ((MgLinkLabel)control).OriginalBGColor = backgroundColor;
            else if (control is MgTabControl)
               ((MgTabControl)control).PageColor = backgroundColor;
            else if (control is MgShape)
               ((MgShape)control).FillColor = backgroundColor;
            else
               control.BackColor = backgroundColor;
         }
         catch (System.ArgumentException)
         {
         }
      }

      /// <summary></summary>
      /// <param name="control"></param>
      /// <param name="Font"></param>
      public static void SetFont(Control control, Font font)
      {
         if (font != null)
         {
#if PocketPC
            // This code is needed to prevent the exception when setting Control.Font in a panel
            if (!(control is Panel))
#endif
            control.Font = font;
         }
      }

      /// <summary></summary>
      /// <param name="control"></param>
      /// <param name="gradientColor"></param>
      public static void SetGradientColor(Control control, GradientColor gradientColor)
      {
         if (IsMdiClient(control))
            control = control.Parent;

         if (control is IGradientColorProperty)
         {
            IGradientColorProperty gradientColorProperty = (IGradientColorProperty)control;
            if ((gradientColorProperty.GradientColor != gradientColor))
            {
               gradientColorProperty.GradientColor = gradientColor;

               control.Refresh();
            }
         }
      }

      /// <summary></summary>
      /// <param name="control"></param>
      public static GradientColor GetGradientColor(Control control)
      {
         GradientColor gradientColor = new GradientColor();

         if (IsMdiClient(control))
            control = control.Parent;

         if (control is IGradientColorProperty)
            gradientColor = ((IGradientColorProperty)control).GradientColor;

         return gradientColor;
      }

      /// <summary></summary>
      /// <param name="control"></param>
      /// <param name="gradientStyle"></param>
      public static void SetGradientStyle(Control control, GradientStyle gradientStyle)
      {
         if (IsMdiClient(control))
            control = control.Parent;

         if (control is IGradientColorProperty)
         {
            IGradientColorProperty gradientColorProperty = (IGradientColorProperty)control;
            if (gradientColorProperty.GradientStyle != gradientStyle)
            {
               gradientColorProperty.GradientStyle = gradientStyle;

               control.Refresh();
            }
         }
      }

      /// <summary></summary>
      /// <param name="control"></param>
      public static GradientStyle GetGradientStyle(Control control)
      {
         GradientStyle gradientStyle = GradientStyle.None;

         if (IsMdiClient(control))
            control = control.Parent;

         if (control is IGradientColorProperty)
            gradientStyle = ((IGradientColorProperty)control).GradientStyle;

         return gradientStyle;
      }

      /// <summary></summary>
      /// <param name="control"></param>
      /// <param name="style"></param>
      public static void SetStyle3D(Control control, ControlStyle style)
      {
         FlatStyle flatStyle = (style == ControlStyle.TwoD ? FlatStyle.Flat : FlatStyle.Standard);

         if (control is MgCheckBox)
            ((MgCheckBox)control).FlatStyle = flatStyle;
         else if (control is MgRadioPanel)
            ((MgRadioPanel)control).FlatStyle = flatStyle;
         else if (control is MgComboBox)
            ((MgComboBox)control).SetDrawMode(style == ControlStyle.TwoD ? DrawMode.OwnerDrawFixed : DrawMode.Normal);
         else if (control is MgLabel)
            ((MgLabel)control).Style = style;
         else if (control is MgPictureBox)
            ((MgPictureBox)control).Style = style;
         else if (control is MgTextBox)
            ((MgTextBox)control).Style = style;
         else if (control is MgShape)
            ((MgShape)control).ControlStyle = style;
         else if (control is MgRichTextBox)
            ((MgRichTextBox)control).ControlStyle = style;
         else if (control is TableControl)
            ((TableControl)control).ControlStyle = style;
         else if (control is MgGroupBox)
            ((MgGroupBox)control).ControlStyle = style;
         else
            Debug.Assert(false);
      }

      /// <summary></summary>
      /// <param name="control"></param>
      /// <param name="style"></param>
      public static void SetBorderStyle(Panel panel, BorderType borderType)
      {
         BorderStyle borderStyle = BorderStyle.FixedSingle;

         switch (borderType)
         {
            case BorderType.NoBorder:
               borderStyle = BorderStyle.None;
               break;
            case BorderType.Thick:
               borderStyle = BorderStyle.Fixed3D;
               break;
            case BorderType.Thin:
               borderStyle = BorderStyle.FixedSingle;
               break;
         }

         panel.BorderStyle = borderStyle;
                 
      }

      /// <summary>
      /// Set the Border Type of the control.
      /// </summary>
      /// <param name="control"></param>
      /// <param name="borderType"></param>
      public static void SetBorderType(Control control, BorderType borderType)
      {
         if (control is IBorderTypeProperty)
            ((IBorderTypeProperty)control).BorderType = borderType;
      }

#if !PocketPC
      public static void SetTextFotRichTextBox(RichTextBox richEditCtrl, String text)
      {
         if (text != null && text.StartsWith("{\\rtf"))//Check if text is rtf text
            richEditCtrl.Rtf = text;
         else
         {
            // re-set the original Rtf so that the new text will use the default Font.
            richEditCtrl.Rtf = "";
            richEditCtrl.Text = text;
         }
      }
#endif

      /// <summary> Set Text </summary>
      /// <param name="control"></param>
      /// <param name="text"></param>
      public static void SetText(Control control, String text)
      {
         if (control is ITextProperty)
            ((ITextProperty)control).Text = text;
         else
            Debug.Assert(false);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="ctrl"></param>
      /// <param name="image"></param>
      public static void SetImage(Control control, Image image)
      {
         if (control is IImageProperty)
         {
            IImageProperty imageProperty = control as IImageProperty;
            imageProperty.Image = image;
         }
#if !PocketPC
         else
         {
            control.BackgroundImage = image;
         }

#else
         if (control is Panel)
         {
            Panel panel = (Panel)control;
            panel.BackGroundImage = image;
         }
#endif
      }
      /// <summary>SetBorder</summary>
      /// <param name="control"></param>
      /// <param name="showBorder"></param>
      public static void SetBorder(Control control, bool showBorder)
      {
#if !PocketPC
         BorderStyle borderStyle = (control is MgTextBox && MgTextBox.ShouldDrawFlatTextBox) ? BorderStyle.FixedSingle : BorderStyle.Fixed3D;

         borderStyle = (showBorder ? borderStyle : BorderStyle.None);

#else
         BorderStyle borderStyle = (showBorder ? BorderStyle.FixedSingle : BorderStyle.None);
#endif

         if (control is IBorderStyleProperty)
            ((IBorderStyleProperty)control).BorderStyle = borderStyle;
         else if (!(control is WebBrowser))
         {
            //  Debug.Assert(false);
         }
      }

      /// <summary> 
      /// Sets the Password property
      /// </summary>
      public static void SetPasswordEdit(MgTextBox textBox, bool isPasswordEdit)
      {
         textBox.UseSystemPasswordChar = isPasswordEdit;
      }

      public static bool IsMdiClient(Object obj)
      {
#if !PocketPC
         return (obj is MdiClient);
#else
         return false;
#endif
      }

      /// <summary>
      /// Add items to control's list
      /// </summary>
      /// <param name="mgListBox"></param>
      /// <param name="itemsList"></param>
      public static void SetItemsList(ListControl listControl, String[] itemsList)
      {
         if (listControl is MgComboBox)
         {
            ((MgComboBox)listControl).BeginUpdate();

            ((MgComboBox)listControl).Items.Clear();

            ((MgComboBox)listControl).AddRange(itemsList);

            ((MgComboBox)listControl).EndUpdate();
         }

         else
         {
            ((MgListBox)listControl).BeginUpdate();

            ((MgListBox)listControl).Items.Clear();

            ((MgListBox)listControl).AddRange(itemsList);

            ((MgListBox)listControl).EndUpdate();
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="listControl"></param>
      /// <returns></returns>
      public static ArrayList GetItemsList(ListControl listControl)
      {
         ArrayList values = new ArrayList();

         if (listControl is MgComboBox)
            values.AddRange(((MgComboBox)listControl).Items);
         else
            values.AddRange(((MgListBox)listControl).Items);

         return values;
      }

      /// <summary>return true if control can be transparent</summary>
      /// <param name="control"></param>
      /// <returns></returns>
      public static bool SupportsTransparency(Control control)
      {
         if (control is TextBox || control is MgComboBox || control is ListBox || control is Form || control is MgTabControl)
            return false;
         else
            return true;
      }

      /// <summary>
      /// Sets Item height for 2d combobox
      /// </summary>
      /// <param name="height"></param>
      public static void SetComboBoxItemHeight(Control control, int height)
      {
         MgComboBox c = control as MgComboBox;
         if (c != null && c.DrawMode == DrawMode.OwnerDrawFixed)
            c.SetItemHeight(height);
      }

      /// <summary> Returns true if the control is an Image Button </summary>
      /// <param name="control"></param>
      /// <returns></returns>
      public static bool IsImageButton(Control control)
      {
         return (control is MgImageButton);
      }

      /// <summary>
      /// Set the appearance of CheckBox
      /// </summary>
      public static void SetCheckboxMainStyle(MgCheckBox mgCheckBox, Appearance appearance)
      {
         mgCheckBox.Appearance = appearance;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="tabControl"></param>
      /// <param name="sideType"></param>
      public static void SetTabAlignment(MgTabControl tabControl, SideType sideType)
      {
         TabAlignment tabAlignment;

         switch (sideType)
         {
            case SideType.Top:
               tabAlignment = TabAlignment.Top;
               break;

            case SideType.Bottom:
               tabAlignment = TabAlignment.Bottom;
               break;

            case SideType.Left:
               tabAlignment = TabAlignment.Left;
               break;

            case SideType.Right:
               tabAlignment = TabAlignment.Right;
               break;

            default:
               tabAlignment = TabAlignment.Top;
               break;
         }

         tabControl.Alignment = tabAlignment;
      }

      /// <summary>
      /// Gets the System.Windows.Forms.Appearance from RadioButtonAppearance.
      /// </summary>
      /// <param name="radioButtonAppearance"></param>
      /// <returns></returns>
      public static Appearance RadioButtonAppearance2Appearance(RbAppearance radioButtonAppearance)
      {
         return (radioButtonAppearance == RbAppearance.Button ? Appearance.Button : Appearance.Normal);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="obj"></param>
      /// <returns></returns>
      public delegate bool AllowDesignerActionsDelegate(object obj);
  
      /// <summary>
      /// Convert BorderType to FormBorderStyle.
      /// </summary>
      /// <param name="borderType"></param>
      /// <param name="windowType"></param>
      /// <param name="showTitleBar"></param>
      /// <returns></returns>
      public static FormBorderStyle BorderTypeToFormBorderStyle(BorderType borderType, WindowType windowType, bool showTitleBar)
      {
         FormBorderStyle formBorderStyle = FormBorderStyle.None;

         if (showTitleBar && borderType == BorderType.NoBorder)
            borderType = BorderType.Thin;

         if (windowType == WindowType.FitToMdi)
            borderType = BorderType.Thin;

         switch (borderType)
         {
            case BorderType.NoBorder:
               formBorderStyle = FormBorderStyle.None;
               break;
            case BorderType.Thick:
               if (windowType == WindowType.Tool)
                  formBorderStyle = FormBorderStyle.SizableToolWindow;
               else
                  formBorderStyle = FormBorderStyle.Sizable;
               break;
            case BorderType.Thin:
               if (windowType == WindowType.Tool)
                  formBorderStyle = FormBorderStyle.FixedToolWindow;
               else
                  formBorderStyle = FormBorderStyle.FixedSingle;
               break;
            default:
               Debug.Assert(false);
               break;
         }

         return formBorderStyle;
      }

      /// <summary>
      /// Set TitlePadding on Tab Control
      /// </summary>
      /// <param name="mgTabControl"></param>
      /// <param name="padding"></param>
      public static void SetTitlePadding(MgTabControl mgTabControl, int padding)
      {
         mgTabControl.TitlePadding = padding;
      }

#if !PocketPC
      /// <summary>
      /// 
      /// </summary>
      /// <param name="host"></param>
      /// <param name="selectedItems"></param>
      /// <param name="allowDesignerActions"></param>
      /// <returns></returns>
      public static DesignerVerbCollection GetVerbsForControl(IDesignerHost host, IList selectedItems, AllowDesignerActionsDelegate allowDesignerActions)
      {

         DesignerVerbCollection verbs = new DesignerVerbCollection();


         //if we have a selected item, get its verbs
         if (selectedItems.Count == 1)
         {
            IComponent selectedItem = selectedItems[0] as IComponent;
            if (selectedItem != null && allowDesignerActions(selectedItem))
            {
               IDesigner designer = host.GetDesigner(selectedItem);

               if (designer != null)
               {
                  DesignerVerbCollection orgVerbs = designer.Verbs;
                  const int MaxOptionsToShow = 25;

                  if (orgVerbs.Count > MaxOptionsToShow)
                  {
                     //The designer actions in the property grid are shown on a single WinForms.LinkLabel.
                     //And, WinForms.LinkLabel can show 31 links at max.
                     for (int i = 0; i < MaxOptionsToShow; i++)
                     {
                        verbs.Add(orgVerbs[i]);
                     }
                  }
                  else
                     verbs.AddRange(orgVerbs);
               }
            }
         }
         return verbs;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="treeView"></param>
      /// <param name="expandedNode_s"></param>
      /// <param name="collapsedNode_s"></param>
      /// <param name="leaf_s"></param>
      public static void InitDefualtNodesForTree(TreeView treeView, string expandedNode_s, string collapsedNode_s, string leaf_s,
                                                 bool updateNodesState, ref TreeNode RootNode, ref TreeNode ExpandedNode, 
                                                 ref TreeNode CollapsedNode)
      {
         treeView.BeginUpdate();

         RootNode = treeView.Nodes.Add(expandedNode_s);

         ExpandedNode = RootNode.Nodes.Add(expandedNode_s);
         CollapsedNode = RootNode.Nodes.Add(collapsedNode_s);
         RootNode.Nodes.Add(leaf_s);

         ExpandedNode.Nodes.Add(leaf_s);
         ExpandedNode.Nodes.Add(leaf_s);
         ExpandedNode.Nodes.Add(leaf_s);

         CollapsedNode.Nodes.Add(leaf_s);

         RootNode.EnsureVisible();
         if (updateNodesState)
            UpdateNodesState(RootNode, ExpandedNode);

         treeView.EndUpdate();
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="RootNode"></param>
      /// <param name="ExpandedNode"></param>
      private static void UpdateNodesState(TreeNode RootNode, TreeNode ExpandedNode)
      {         
         RootNode.Expand();
         ExpandedNode.Expand();       
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="fromControl"></param>
      /// <param name="toControl"></param>
      public static void SetSpecificControlPropertiesForFormDesignerForListControl(IItemsCollection fromControl,
                                                                                   IItemsCollection toControl)
      {
         toControl.ItemsCollection.Clear();
         foreach (var item in fromControl.ItemsCollection)
            toControl.ItemsCollection.Add(item);

         if (fromControl is ListControl)
         {
            ListControl fromListControl = fromControl as ListControl;            
            ListControl toListControl = toControl as ListControl;

            if (fromListControl.SelectedIndex > -1)
               toListControl.SelectedIndex = fromListControl.SelectedIndex;

         }
      }

        /// <summary>
      /// save DragComponents in the components list
      /// </summary>
      /// <param name="de"></param>
      public static List<IComponent> GetDraggedComponents(DragEventArgs de)
      {

         //BehaviorDataObject is internal class in .NET
         // use this work around to get dragged objects
         //http://vbcity.com/forums/t/163927.aspx
         List<IComponent> components = new List<IComponent>();
         Type t = de.Data.GetType();
         if (t.Name == "BehaviorDataObject")
         {
            System.Reflection.PropertyInfo pi = t.GetProperty("DragComponents");
            ArrayList comps = pi.GetValue(de.Data, null) as ArrayList;
            if (comps != null && comps.Count > 0)
            {
               foreach (Object item in comps)
               {
                  if (item is IComponent)
                     components.Add((IComponent)item);
               }
            }
         }

         return components;
      }
#endif
      #region Nested type: AlignmentInfo
      /// <summary> for convert Hor\Ver alignment
      /// </summary>
      public struct AlignmentInfo
      {
         //TODO: Kaushal. When the rendering code will be moved from MgUtils to MgControls,
         //the scope of these members should be changed to internal. 
         public AlignmentTypeHori HorAlign; //(LEFT = 1, CENTER = 2,RIGHT = 3)
         public AlignmentTypeVert VerAlign; //(TOP = 1, CENTER = 2,BOTTOM = 3)

         internal AlignmentInfo(AlignmentTypeVert VerAlign, AlignmentTypeHori HorAlign)
         {
            this.HorAlign = HorAlign;
            this.VerAlign = VerAlign;
         }
      }
      #endregion

      #region Nested type: ContentAlignmentDict
      internal class ContentAlignmentDict : Dictionary<ContentAlignment, AlignmentInfo>
      {
      }
      #endregion
   }
}
