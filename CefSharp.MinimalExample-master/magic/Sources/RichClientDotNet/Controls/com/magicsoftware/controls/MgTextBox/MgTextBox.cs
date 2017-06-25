using System;
using System.Windows.Forms;
using com.magicsoftware.win32;
using System.Runtime.InteropServices;
using System.Text;
using System.Globalization;
using com.magicsoftware.util;
using System.Diagnostics;
using System.Drawing;
using Controls.com.magicsoftware.support;
using com.magicsoftware.controls.utils;
using Controls.com.magicsoftware.controls.PropertyInterfaces;
#if PocketPC
using com.magicsoftware.mobilestubs;
using ContentAlignment = OpenNETCF.Drawing.ContentAlignment2;
#endif

namespace com.magicsoftware.controls
{
   public delegate void ImeEvent (Object sender, ImeEventArgs imeEventArgs);

#if !PocketPC
   [ToolboxBitmap(typeof(TextBox))]
#endif
   public class MgTextBox : TextBox, IMultilineProperty, IRightToLeftProperty, ITextProperty, IBorderStyleProperty, IContentAlignmentProperty, IOwnerDrawnControl, IBorderTypeProperty
   {
      private StringBuilder ImeReadStrBuilder; // JPN: ZIMERead function

      private Boolean isKorean()
      {
         return (CultureInfo.CurrentCulture.LCID == 1042);
      }
      private Boolean isJapanese()
      {
         return (CultureInfo.CurrentCulture.LCID == 1041);
      }

#if !PocketPC
      // cut/copy/paste events invoked from Default(System) context menu.
      public delegate void CutDelegate(Object sender, EventArgs e);

      public event CutDelegate CutEvent;

      public delegate void CopyDelegate(Object sender, EventArgs e);

      public event CopyDelegate CopyEvent;

      public delegate void PasteDelegate(Object sender, EventArgs e);

      public event PasteDelegate PasteEvent;

      public delegate void ClearDelegate(Object sender, EventArgs e);

      public event ClearDelegate ClearEvent;

      public delegate void UndoDelegate(Object sender, EventArgs e);

      public event UndoDelegate UndoEvent;
#endif

      #region IContentAlignmentProperty Members

      private ContentAlignment textAlign;
      public new ContentAlignment TextAlign
      {
         get { return textAlign; }
         set
         {
            if (textAlign != value)
            {
               textAlign = value;
               UpdateTextAlign();
               Invalidate();
            }
         }
      }

      #endregion

      /// <summary> Adjust the horizontal alignment based on RightToLeft property.
      /// When RightToLeft=Yes, the framework reverses the alignment set. So, we need 
      /// to revert it again.
      /// </summary>
      private void UpdateTextAlign()
      {
         ControlUtils.AlignmentInfo alignmentInfo = ControlUtils.GetAlignmentInfo(TextAlign);
         HorizontalAlignment horiAlignment = ControlUtils.HorAlign2HorAlign(alignmentInfo.HorAlign);
         if (RightToLeft == RightToLeft.Yes && horiAlignment != HorizontalAlignment.Center)
            horiAlignment = (horiAlignment == HorizontalAlignment.Right ? HorizontalAlignment.Left : HorizontalAlignment.Right);

         if (horiAlignment != base.TextAlign)
            base.TextAlign = horiAlignment;
      }

      #region IRightToLeftProperty Members

#if !PocketPC
      public override RightToLeft RightToLeft
      {
         get
         {
            return base.RightToLeft;
         }
         set
         {
            if (base.RightToLeft != value)
            {
               base.RightToLeft = value;
               UpdateTextAlign();
            }
         }
      }
#else
      public RightToLeft RightToLeft { get; set; }
#endif

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
            if (!base.Text.Equals(value))
               base.Text = value;
         }
      }

      #endregion

      #region IFontProperty
      public FontDescription FontDescription { get; set; }
      #endregion IFontProperty

      /// <summary>
      /// Returns true if the TextBox should draw flat appearance(2D style).
      /// </summary>
      public static bool ShouldDrawFlatTextBox
      {
         get
         {
            return Utils.SpecialFlatEditOnClassicTheme && !Application.RenderWithVisualStyles;
         }
      }
   

      private ControlStyle controlStyle = ControlStyle.NoBorder;
      /// <summary>
      /// Returns the controlStyle based on Borderstyle of TextBox.
      /// </summary>
      public ControlStyle ControlStyle
      {
         get
         {
            switch (BorderStyle)
            {
               case BorderStyle.FixedSingle:
                  controlStyle = ControlStyle.TwoD;
                  break;
               case BorderStyle.Fixed3D:
                  controlStyle = ControlStyle.Windows;
                  break;
               default:
                  controlStyle = ControlStyle.NoBorder;
                  break;
            }
            return controlStyle;
         }
      }

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

      /// <summary>
      /// TextBox strategy that handles if the textbox has hint or not.
      /// The strategy changed when ControlUtils.SetHint is evaluated.
      /// </summary>
      public TextBoxHintStrategyBase TextBoxStrategy { get; set; }

      private ControlStyle style = ControlStyle.NoBorder;

      /// <summary>
      /// Control style (2D / 3D Raised/ 3D Sunken)
      /// </summary>
      public ControlStyle Style
      {
         get
         {
            return style;
         }
         set
         {
            if (style != value)
            {
               style = value;
               if (style == ControlStyle.Windows3d)
                  BorderStyle = ShouldDrawFlatTextBox ? BorderStyle.FixedSingle : BorderStyle.Fixed3D;
               else
                  BorderStyle = BorderStyle.None;
            }
         }
      }

#if !PocketPC
      public event ImeEvent ImeEvent;
#endif

      public MgTextBox()
         : base()
      {
#if !PocketPC
         base.RightToLeft = RightToLeft.No;
#endif
         AutoSize = false;
         if (isJapanese())
         {
            ImeReadStrBuilder = new StringBuilder();
            ImeReadStrBuilder.Capacity = 128;
         }
         else
            ImeReadStrBuilder = null;

         TextAlign = ContentAlignment.TopLeft;
         TextBoxStrategy = new TextBoxNoHintStrategy(this);
         //We need to set the default border style here because in case of ShowBorder = true, we don't set the BorderStyle again.
         //So framework set its default borderstyle to Fixed3D.
         BorderStyle = ShouldDrawFlatTextBox ? BorderStyle.FixedSingle : BorderStyle.Fixed3D;

         FontDescription = new FontDescription(Font);
         this.SetStyle(ControlStyles.SupportsTransparentBackColor, true);

         this.FontChanged += MgTextBox_FontChanged;
      }

      private void MgTextBox_FontChanged(object sender, EventArgs e)
      {
         FontDescription = new FontDescription(Font);
      }

      /// <summary>
      /// Set strategy to Hint strategy
      /// </summary>
      /// <param name="hintText"></param>
      public void EnableHintStrategy(string hintText)
      {
         if (!(TextBoxStrategy is TextBoxWithHintStrategy)) //It not enabled
         {
            TextBoxStrategy.UnregisterEvents();
            Color oldHintcolor = TextBoxStrategy.HintFgColor;
            TextBoxStrategy = new TextBoxWithHintStrategy(this);
            TextBoxStrategy.HintText = hintText;
            TextBoxStrategy.HintFgColor = oldHintcolor;
         }
      }

      /// <summary>
      /// Set strategy to regular text box strategy
      /// </summary>
      /// <param name="hintText"></param>
      public void DisableHintStrategy()
      {
         if (!(TextBoxStrategy is TextBoxNoHintStrategy)) //If not disabled
         {
            TextBoxStrategy.UnregisterEvents();
            Color oldHintcolor = TextBoxStrategy.HintFgColor;
            TextBoxStrategy = new TextBoxNoHintStrategy(this);
            Debug.Assert(TextBoxStrategy.HintText == null);
            TextBoxStrategy.HintFgColor = oldHintcolor;
         }
      }

#if !PocketPC
      protected override void OnGotFocus(EventArgs e)
      {
         base.OnGotFocus(e);

         // Defect 116683- Reproduced on classic theme the when edit control(having vertical alignment Center or Bottom) is focused a dotted line appears (i.e text is painted twice)
         // Reason : When edit control is Focused, we don't get WM_PAINT message, but framework paints it from somewhere. 
         // In this case, control is not invalidated and hence previously painted text is also seen.
         // So invalidate the control after it gains focus.
         if (!Application.RenderWithVisualStyles)
            Invalidate();
      }
#endif

      /// <summary> return composition string, which is got from IME
      /// and stored in StringBuilder (JPN: ZIMERead function)
      /// </summary>
      public String GetCompositionString()
      {
         if (ImeReadStrBuilder == null)
            return null;
         return ImeReadStrBuilder.ToString();
      }

      /// <summary> clear composition string (JPN: ZIMERead function)
      /// </summary>
      public void ClearCompositionString()
      {
         if (ImeReadStrBuilder == null)
            return;
         ImeReadStrBuilder.Length = 0;
      }

#if !PocketPC
      public bool IsTransparent { get; set; }

      /// <summary> Paints the control. </summary>
      private void PaintNotFocusedControl()
      {
         Debug.Assert(!Focused);

         using (Graphics gr = Graphics.FromHwnd(this.Handle))
         {
            //Simulate Transparency
            if (IsTransparent || BackColor.A < 255)
            {
               if (Parent is ISupportsTransparentChildRendering)
                  ((ISupportsTransparentChildRendering)Parent).TransparentChild = this;

               System.Drawing.Drawing2D.GraphicsContainer g = gr.BeginContainer();
               Rectangle translateRect = this.Bounds;
               int borderWidth = (this.Width - this.ClientSize.Width) / 2;
               gr.TranslateTransform(-(Left + borderWidth), -(Top + borderWidth));
               PaintEventArgs pe = new PaintEventArgs(gr, translateRect);
               this.InvokePaintBackground(Parent, pe);
               this.InvokePaint(Parent, pe);
               gr.ResetTransform();
               gr.EndContainer(g);
               pe.Dispose();

               if (Parent is ISupportsTransparentChildRendering)
                  ((ISupportsTransparentChildRendering)Parent).TransparentChild = null;
            }

            Rectangle rect = this.ClientRectangle;

            //In case of Fixed Single border style, we get same client rectangle & bounds. So we need to reduce them to draw the 2D border.
            if (BorderStyle == BorderStyle.FixedSingle)
               rect.Inflate(-2, -2);

            ControlRenderer.PaintBackGround(gr, rect, (IsTransparent ? Color.Transparent : this.BackColor), Enabled || IsTransparent);

            PaintForeground(gr, rect, ForeColor);
         }
      }

      /// <summary> Renders the text on the Control. </summary>
      /// <param name="g"></param>
      /// <param name="rect"></param>
      /// <param name="fgColor"></param>
      protected virtual void PaintForeground(Graphics g, Rectangle rect, Color fgColor)
      {
         Rectangle drawRect = rect;

         String str = GetNotFocusedText();
         if (UseSystemPasswordChar && str != null && !HasHintWithoutText())
         {
            StringBuilder sb = new StringBuilder();
            sb.Append(PasswordChar, str.Length);
            str = sb.ToString();
         }
         else if (Multiline)
         {
            // find the character near the top left corner of the control.
            int charIdx = GetCharIndexFromPosition(new Point(ClientRectangle.Left, ClientRectangle.Top + 2));
            str = str.Substring(charIdx);
         }

         ControlRenderer.PrintText(g, drawRect, GetNotFocusedColor(), FontDescription, str, Multiline, TextAlign, Enabled, WordWrap, true, false,
                                  RightToLeft == RightToLeft.Yes);
      }

      /// <summary>
      /// Gets text of not focused control (happens in AllowTesting=Y ans in the designer form). In this case, the hint is disabled but the text should be the hint
      /// </summary>
      /// <returns></returns>
      private string GetNotFocusedText()
      {
         return HasHintWithoutText() && !ReadOnly ? TextBoxStrategy.HintText : Text;
      }

      /// <summary>
      /// Gets color of not focused control (happens in AllowTesting=Y ans in the designer form). In this case, the hint is disabled but the color should be the hint color
      /// </summary>
      /// <returns></returns>
      private Color GetNotFocusedColor()
      {
         return HasHintWithoutText() && !ReadOnly ? TextBoxStrategy.HintFgColor : ForeColor;
      }

      /// <summary>
      /// Checks if hint is set and text is empty
      /// </summary>
      /// <returns></returns>
      private bool HasHintWithoutText()
      {
         return TextBoxStrategy.IsHintTextHasValue && string.IsNullOrEmpty(Text);
      }

      /// <summary>
      /// Gets the foreground color that the text should be painted on.
      /// </summary>
      /// <returns></returns>
      private Color GetColorToPaint()
      {
         return TextBoxStrategy.IsHintEnabled ? TextBoxStrategy.HintFgColor : ForeColor;
      }

      /// <summary>
      /// Gets the text that shhould be shown.
      /// </summary>
      /// <returns></returns>
      private string GetTextToPaint()
      {
         return TextBoxStrategy.IsHintEnabled ? TextBoxStrategy.HintText : Text;
      }

      protected override void WndProc(ref Message m)
      {
         if (m.Msg == NativeWindowCommon.WM_LBUTTONDOWN)
         {
            SaveSelectionStart = SelectionStart;
            SaveSelectionLength = SelectionLength;
         }
         else if (m.Msg == NativeWindowCommon.WM_PAINT)
         {
            if (!Focused)
            {
               //Allow framework to draw the borders, we will draw text & other things. 
               //So called base.WndProc & removed ValidateRect() from PaintNotFocusedControl() method.
               base.WndProc(ref m);
               PaintNotFocusedControl();
               return;
            }
         }

         if (isJapanese()) // JPN: ZIMERead function
         {
            if (m.Msg == NativeWindowCommon.WM_IME_COMPOSITION)
            {
               if (((int)m.LParam & NativeWindowCommon.GCS_RESULTREADSTR) > 0)
               {
                  int hIMC = NativeWindowCommon.ImmGetContext(this.Handle);

                  try
                  {
                     int size = NativeWindowCommon.ImmGetCompositionString(hIMC, NativeWindowCommon.GCS_RESULTREADSTR, null, 0);

                     StringBuilder buffer = new StringBuilder(size);
                     NativeWindowCommon.ImmGetCompositionString(hIMC, NativeWindowCommon.GCS_RESULTREADSTR, buffer, (uint)size);
                     string str = buffer.ToString().Substring(0, size / 2);
                     ImeReadStrBuilder.Append(str);
                  }
                  finally
                  {
                     NativeWindowCommon.ImmReleaseContext(this.Handle, hIMC);
                  }
               }
            }
         }

         // handling of cut/copy/paste event raised from default(system) conext menu invoked on text box
         switch (m.Msg)
         {
            case NativeWindowCommon.WM_CUT:
               if (CutEvent != null)
               {
                  CutEvent(this, new EventArgs());
                  return;
               }
               else
                  break;                  
            case NativeWindowCommon.WM_COPY:
               if (CopyEvent != null)
               {
                  CopyEvent(this, new EventArgs());
                  return;
               }
               else
                  break;
            case NativeWindowCommon.WM_PASTE:
               if (PasteEvent != null)
               {
                  PasteEvent(this, new EventArgs());
                  return;
               }
               else
                  break;
            case NativeWindowCommon.WM_CLEAR:
               if (ClearEvent != null)
               {
                  ClearEvent(this, new EventArgs());
                  return;
               }
               else
                  break;
            case NativeWindowCommon.WM_UNDO:
               if (UndoEvent != null)
               {
                  UndoEvent(this, new EventArgs());
                  return;
               }
               else
                  break;
         }

         if (isKorean() && ! Multiline)
         {
            if (CheckClickSequence(m))
               return;

            ImeParam im = KoreanImeMessageParam (m);
            if (im != null)
            {
               
               // (Korean) Some IME messages must be postponed until processing in 
               // TextMaskEditor.action() is finished.  So it is routed to RT Event loop.               
               ImeEventArgs iea = new ImeEventArgs(im);
               ImeEvent(this, iea);
               if (iea.Handled)
                  return;
            }
            else
            {
               // RT Event handler send back those IME messagesas MG_IME_XX
               // to avoid confusion, so it is translated to WM_IME_XX
               KoreanMessageTranslation(ref m);
            }
         }
         base.WndProc(ref m);
      }

      /// <summary>
      /// Defect 135689: When typing in Korean language the last letter is doubled after mouse click
      /// It happens only on Win10, and not on Win7.
      /// </summary>
      private bool shouldIgnoreNextImeChar = false;

      protected Boolean CheckClickSequence(Message m)
      {
         Boolean rc = false;

         // In Win10, WM_IME_CHAR just after WM_IME_ENDCOMPOSITION should be ignored.
         // In Win7, it must be processed.
         // To distinguish, WM_REFLECT_WM_COMMAND/EN_CHANGE is checked.
         switch (m.Msg)
         {
            case NativeWindowCommon.WM_IME_STARTCOMPOSITION:
               shouldIgnoreNextImeChar = false;
               break;

            case NativeWindowCommon.WM_IME_ENDCOMPOSITION:
               shouldIgnoreNextImeChar = true;
               break;

            case NativeWindowCommon.WM_REFLECT_WM_COMMAND:
               if ((ENNOTIFY)NativeWindowCommon.HiWord((int)m.WParam) == ENNOTIFY.EN_CHANGE)
                  shouldIgnoreNextImeChar = false;
               break;

            case NativeWindowCommon.WM_IME_CHAR:
               rc = shouldIgnoreNextImeChar;
               shouldIgnoreNextImeChar = false;
               break;
         }
         return rc;
      }

      protected ImeParam KoreanImeMessageParam(Message m)
        {
            ImeParam im = null;
            switch (m.Msg)
            {
                case NativeWindowCommon.WM_IME_STARTCOMPOSITION:
                    if (MaxLength > 0)
                        MaxLength = MaxLength + 1;
                    im = new ImeParam(m.HWnd, m.Msg, m.WParam, m.LParam);
                    break;

                case NativeWindowCommon.WM_IME_COMPOSITION:
                    // send to work thread unless this is the result str (might follow the ENDCOMPOSITION)
                    if (((int)m.LParam & NativeWindowCommon.GCS_RESULTSTR) == 0)
                       im = new ImeParam(m.HWnd, m.Msg, m.WParam, m.LParam);
                    break;

                case NativeWindowCommon.WM_IME_CHAR:
                    // WM_IME_CHAR is handled as KeyPressEvent
                    break;

            }

         return im;
      }

      protected void KoreanMessageTranslation(ref Message m)
      {
         switch (m.Msg)
         {
            case NativeWindowCommon.MG_IME_STARTCOMPOSITION:
               m.Msg = NativeWindowCommon.WM_IME_STARTCOMPOSITION;
               break;

            case NativeWindowCommon.MG_IME_ENDCOMPOSITION:
               if (MaxLength > 1)
                  MaxLength -= 1;
               m.Msg = NativeWindowCommon.WM_IME_ENDCOMPOSITION;
               break;

            case NativeWindowCommon.MG_IME_COMPOSITION:
               m.Msg = NativeWindowCommon.WM_IME_COMPOSITION;
               break;

            case NativeWindowCommon.MG_KEYDOWN:
               m.Msg = NativeWindowCommon.WM_KEYDOWN;
               break;

            case NativeWindowCommon.MG_CHAR:
               m.Msg = NativeWindowCommon.WM_CHAR;
               break;
         }
        }

#endif // !PocketPC

#if PocketPC
      public Boolean AutoSize { get; set; }
      private bool usePasswordChar;
      public bool UseSystemPasswordChar
      {
         get
         {
            return usePasswordChar;
         }
         set
         {
            usePasswordChar = value;
            if (usePasswordChar)
               this.PasswordChar = '*';
            else
               this.PasswordChar = '\0';
         }
      }
      public void AppendText(string text)
      {
         Text += text;
      }
      public void Clear()
      {
         Text = "";
      }

      // Special key code to be used by a TextBox with accumulated text. Same definition in ConstInterface
      public const int KEY_ACCUMULATED = 245;

      // A delegate for our wndproc
      public delegate int MobileWndProc(IntPtr hwnd, uint msg, uint wParam, int lParam);
      // Original wndproc
      private IntPtr OrigWndProc;
      // object's delegate
      MobileWndProc proc;

      /// <summary> subclass the textbox - replace it's window proce
      /// </summary>
      void subclassTextbox()
      {
         proc = new MobileWndProc(WindowProc);
         OrigWndProc = (IntPtr)NativeWindowCommon.SetWindowLong(Handle, NativeWindowCommon.GWL_WNDPROC,
                         Marshal.GetFunctionPointerForDelegate(proc).ToInt32());
      }

      // For barcode scanner: The scanner send the data in a series of keydown-keyup-char messages. The usual 
      // treatment of key events in RC mobile is too slow, so the digits appear one by one. As a workaround, we
      // accumulate the characters, don't pass on the keyboard messages, and set the text at the end of the input.
      // NOTE: We count on the scanner sending '\f' in the prefix and '\t' after the end of the string
      Boolean accumulationMode = false;
      StringBuilder accumulatedBuffer = null;

      public String getAccumulatedBuffer()
      {
         return accumulatedBuffer.ToString();
      }

      private int WindowProc(IntPtr hwnd, uint msg, uint wParam, int lParam)
      {
         int ret = 0;

         // keydown of '\f' - barcode prefix. start accumulating key strokes
         if (msg == NativeWindowCommon.WM_KEYDOWN && wParam == '\f' && !(isJapanese() && usePasswordChar))
         {
            accumulationMode = true;
            accumulatedBuffer = new StringBuilder();
         }

         // If we are accumulating key strokes we don't want to pass on the keys messages
         if (accumulationMode && (msg == NativeWindowCommon.WM_KEYDOWN ||
             msg == NativeWindowCommon.WM_CHAR || msg == NativeWindowCommon.WM_KEYUP))
         {
            if (msg == NativeWindowCommon.WM_CHAR)
            {
               // Add the chars to our accumulation string
               if (wParam != '\f')
                  accumulatedBuffer.Append((char)wParam);
            }

            if (msg == NativeWindowCommon.WM_KEYDOWN)
            {
               // tab signs the end of scanner input. set the text to the control and stop accumulating
               if ((char)wParam == '\t')
               {
                  accumulationMode = false;
                  KeyEventArgs e = new KeyEventArgs((Keys)KEY_ACCUMULATED);
                  this.OnKeyDown(e);
               }
            }
         }
         else
         {
            // Call original proc
            // 1. We may get sometime (from Hebrew keyboard on a PC, from a barcode scanner...)a keydown event 
            //    with wParam 0, which translates to nothing and causes a crash
            // 2. The KeyDown event for the enter key is not raised for textbox on Mobile. 
            //    On Windows Mobile 6 standard, the up and down are used by the system to change focus, 
            //    and we don't get the events in the text box, so we skip the native window proc and raise 
            //    the event.
            if (msg != NativeWindowCommon.WM_KEYDOWN || 
               (wParam != (int)Keys.Enter &&
               wParam != (int)Keys.Down &&
               wParam != (int)Keys.Up &&
               wParam != 0))
            {
               if (isJapanese()) // JPN: ZIMERead function
               {
                  if (msg == NativeWindowCommon.WM_IME_COMPOSITION)
                  {
                     if ((lParam & NativeWindowCommon.GCS_RESULTREADSTR) > 0)
                     {
                        int hIMC = NativeWindowCommon.ImmGetContext(this.Handle);

                        try
                        {
                           int size = NativeWindowCommon.ImmGetCompositionString(hIMC, NativeWindowCommon.GCS_RESULTREADSTR, null, 0);
                           StringBuilder buffer = new StringBuilder(size);
                           NativeWindowCommon.ImmGetCompositionString(hIMC, NativeWindowCommon.GCS_RESULTREADSTR, buffer, (uint)size);
                           string str = buffer.ToString().Substring(0, size / 2);
                           ImeReadStrBuilder.Append(str);
                        }
                        finally
                        {
                           NativeWindowCommon.ImmReleaseContext(this.Handle, hIMC);
                        }
                     }
                  }
               }
               ret = NativeWindowCommon.CallWindowProc(OrigWndProc, hwnd, msg, wParam, lParam);
            }
            else if (msg != NativeWindowCommon.WM_KEYDOWN || wParam != 0)
            {
               KeyEventArgs e = new KeyEventArgs((Keys)wParam);
               this.OnKeyDown(e);
            }
            // else WM_KEYDOWN with wParam == 0 - do nothing
         }

         // Raise the events we don't get on Mobile
         if (msg == NativeWindowCommon.WM_LBUTTONDOWN)
         {
            MouseEventArgs mouseEvents = new MouseEventArgs(MouseButtons.Left, 0,
               NativeWindowCommon.LoWord(lParam), NativeWindowCommon.HiWord(lParam), 0);

            this.OnMouseDown(mouseEvents);
         }
         else if (msg == NativeWindowCommon.WM_LBUTTONUP)
         {
            MouseEventArgs mouseEvents = new MouseEventArgs(MouseButtons.Left, 0,
               NativeWindowCommon.LoWord(lParam), NativeWindowCommon.HiWord(lParam), 0);

            this.OnMouseUp(mouseEvents);
         }

         return ret;
      }

      public void CallKeyDown(KeyEventArgs e)
      {
         OnKeyDown(e);
      }

      // Changing border style causes creating a new window. for our subclassing to work,
      // we need to catch such events
      protected override void OnHandleCreated(EventArgs e)
      {
         subclassTextbox();
      }

#else  // !Pocket PC

        public int SaveSelectionStart { get; set; }
      public int SaveSelectionLength { get; set; }
      public int MouseDownLocation { get; set; }

      /// <summary>
      /// Reset the selection and caret position.
      /// </summary>
      public void ResetSavedProperties ()
      {
         // Reset the selection and caret position.
         SelectionStart = MouseDownLocation;
         SelectionLength = 0;

         // Reset properties added for Drag & Drop.
         MouseDownLocation = 0;
         SaveSelectionLength = 0;
         SaveSelectionStart = 0;
      }

      /// <summary>
      /// Sets/removes user paint style from the control
      /// </summary>
      /// <param name="userPaint"></param>
      public void SetStyle(bool userPaint)
      {
         SetStyle(ControlStyles.UserPaint, userPaint);
      }

      protected override void OnPaint(PaintEventArgs e)
      {
         if (TextBoxStrategy is TextBoxWithHintStrategy)
         {
            //General comment - next time that we will have probelm with some of the flags, use logical control PaintText method instead of this.
            StringFormat stringFormat = new StringFormat();
            if (RightToLeft == RightToLeft.Yes)
               stringFormat = new StringFormat(StringFormatFlags.DirectionRightToLeft);
            else
               stringFormat = StringFormat.GenericDefault;

            stringFormat.Alignment = ControlUtils.ContentAlignmentToStringAlignment(this);

            if (!Multiline)
               stringFormat.FormatFlags |= StringFormatFlags.NoWrap;

            using (SolidBrush drawBrush = new SolidBrush(GetColorToPaint()))
            {
               e.Graphics.DrawString(GetTextToPaint(), Font, drawBrush, e.ClipRectangle, stringFormat);
            }
         }
         base.OnPaint(e);
      }

#endif // PocketPC

      #region IOwnerDrawnControl Members
      
      /// <summary>
      /// Draw the control on the given Graphics at the specified rectangle.
      /// </summary>
      /// <param name="graphics"></param>
      /// <param name="rect"></param>
      public void Draw(Graphics graphics, Rectangle rect)
      {
         ControlStyle style = (BorderStyle == BorderStyle.Fixed3D ? ControlStyle.Windows : ControlStyle.NoBorder);

         ControlRenderer.PaintBackgroundAndBorder(graphics, rect, (IsTransparent ? Color.Transparent : this.BackColor), ForeColor, style, true, Enabled || IsTransparent);

         rect.Inflate((ClientRectangle.Width - Bounds.Width) / 2, (ClientRectangle.Height - Bounds.Height) / 2);
         PaintForeground(graphics, rect, ForeColor);
      }

      #endregion
   }

   public class ImeEventArgs : EventArgs
   {
      public ImeParam im;
      public Boolean Handled;

      public ImeEventArgs(ImeParam i)
      {
         im = i;
         Handled = false;
      }
   }
}
