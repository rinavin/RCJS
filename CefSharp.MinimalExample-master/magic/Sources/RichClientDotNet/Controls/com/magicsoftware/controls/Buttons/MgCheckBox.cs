using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using com.magicsoftware.util;
using Controls.com.magicsoftware.support;
using System.Drawing.Drawing2D;
using Controls.com.magicsoftware.controls.PropertyInterfaces;
#if PocketPC
using CheckBox = OpenNETCF.Windows.Forms.CheckBox2;
using ContentAlignment = OpenNETCF.Drawing.ContentAlignment2;
using FlatStyle = com.magicsoftware.mobilestubs.FlatStyle;
using RightToLeft = com.magicsoftware.mobilestubs.RightToLeft;
using Appearance = com.magicsoftware.mobilestubs.Appearance;
using com.magicsoftware.win32;
using System.Runtime.InteropServices;
#endif

namespace com.magicsoftware.controls
{
#if !PocketPC
   [ToolboxBitmap(typeof(CheckBox))]
#endif
   public class MgCheckBox : CheckBox, IDisplayInfo, IMultilineProperty, IContentAlignmentProperty,
                             IRightToLeftProperty, IGradientColorProperty, ITextProperty, IImageProperty, IBorderTypeProperty
   {
      #region IMultilineRenderer Members

      public bool Multiline { get; set; }

      #endregion

      #region IContentAlignmentRenderer Members

#if !PocketPC
      public override ContentAlignment TextAlign
      {
         get
         {
            return base.TextAlign;
         }
         set
         {
            base.TextAlign = value;

            ControlUtils.SetCheckAlign(this);
            ControlUtils.SetImageAlign(this);
         }
      }
#else
      public ContentAlignment TextAlign
      {
         get
         {
            return TextAlign_DO_NOT_USE_DIRECTLY;
         }
         set
         {
            TextAlign_DO_NOT_USE_DIRECTLY = value;

            ControlUtils.SetCheckAlign(this);
            ControlUtils.SetImageAlign(this);
         }
      }
#endif

      #endregion

      #region IGradientColorProperty Members

      public GradientColor GradientColor { get; set; }
      public GradientStyle GradientStyle { get; set; }

      #endregion

      #region IDisplayInfo Members

      public string TextToDisplay { get; protected set; }

      #endregion

      #region ITextProperty Members

#if PocketPC
      public new string Text
      {
         get 
         {
            return TextToDisplay;
         }
         set 
         { 
            TextToDisplay = value;
            base.Text = value;
         }
      }
#endif

      #endregion

#if PocketPC
      public FlatStyle FlatStyle { get; set; }
      public RightToLeft RightToLeft { get; set; }
      private ContentAlignment TextAlign_DO_NOT_USE_DIRECTLY { get; set; }
      public new ContentAlignment CheckAlign { get; set; }
      public Appearance Appearance { get; set; }
      public Image Image { get; set; }
      public bool ThreeState { get; set; }
#else
      #region IBorderTypeProperty Members

      private int borderWidth;
      private BorderType borderType;
      public BorderType BorderType
      {
         get { return borderType; }
         set
         {
            if (borderType != value)
            {
               borderType = value;
              
               switch (borderType)
               {
                  case BorderType.Thin:
                     borderWidth = 1;
                     break;
                  case BorderType.Thick:
                     borderWidth = 2;
                     break;
                  default:
                     borderWidth = 0;
                     break;
               }

               UpdatePadding();

               Invalidate();
            }
         }
      }

      #endregion

      private int textOffset
      {
         get
         {
            int offset = 6;

            if (FlatStyle == FlatStyle.Standard)
               offset = 5;
            else if (FlatStyle == FlatStyle.Flat)
               offset = 4;

            return offset;
         }
      }

      public new ContentAlignment CheckAlign
      {
         get
         {
            return base.CheckAlign;
         }
         set
         {
            if (value != base.CheckAlign)
            {
               base.CheckAlign = value;

               UpdatePadding();
            }
         }
      }

      public new FlatStyle FlatStyle
      {
         get
         {
            return base.FlatStyle;
         }
         set
         {
            if (value != base.FlatStyle)
            {
               base.FlatStyle = value;

               UpdatePadding();
            }
         }
      }

      protected override Padding DefaultPadding
      {
         get
         {
            int leftPadding = 0;

            if (FlatStyle == FlatStyle.Standard)
               leftPadding = 1;
            else if (FlatStyle == FlatStyle.Flat)
               leftPadding = 2;

            return (new Padding(base.DefaultPadding.Left + leftPadding, base.DefaultPadding.Top, base.DefaultPadding.Right, base.DefaultPadding.Bottom));
         }
      }

      int checkTopPadding = 0;
#endif

      public MgCheckBox()
         : base()
      {
         //this is needed to support double click on button

         //ControlStyles.StandardClick on checkbox causes wring checkbox behaviour - cab bot be used
         //TODO - find solution for double click on checkbox

         //this.SetStyle(ControlStyles.StandardClick, true);
         //this.SetStyle(ControlStyles.StandardDoubleClick, true);
#if PocketPC
         TextAlign = ContentAlignment.MiddleLeft;
         Text = "";
         ((Control)this).Text = "";
#endif
         GradientStyle = GradientStyle.None;
         UpdatePadding();
      }

      /// <summary>
      /// Update the padding of the control based on its FlatStyle.
      /// </summary>
      private void UpdatePadding()
      {
         Padding = DefaultPadding;

         checkTopPadding = 0;
         ControlUtils.AlignmentInfo AlignmentInfo = ControlUtils.GetAlignmentInfo(CheckAlign);
         if (FlatStyle == FlatStyle.Standard && AlignmentInfo.VerAlign == AlignmentTypeVert.Center)
         {
            checkTopPadding = 1;
         }

         Padding = new Padding(Padding.Left + borderWidth, Padding.Top + borderWidth + checkTopPadding, Padding.Right + borderWidth, Padding.Bottom + borderWidth);
      }

      /// <summary></summary>
      /// <param name="e"></param>
      protected override void OnPaint(PaintEventArgs e)
      {
         //For Desktop:
         //Always call base.OnPaint() to let the Framework render the border and the box.
         //Image is always to be rendered by the Framework.
         //If Image is not available, then only...
         //1. Appearance=Normal, just draw the text.
         //2. Appearance=Button, paint the background and text.

         //Since we do not want the Framework to paint the Text, set it to "" (blank) before calling base.OnPaint() and 
         //reset it later.
         String orgText = Text;
         TextToDisplay = Text;
         base.Text = String.Empty;

         if (BackColor == Color.Transparent && Parent is ISupportsTransparentChildRendering)
            ((ISupportsTransparentChildRendering)Parent).TransparentChild = this;

         base.OnPaint(e);

         if (BackColor == Color.Transparent && Parent is ISupportsTransparentChildRendering)
            ((ISupportsTransparentChildRendering)Parent).TransparentChild = null;

         base.Text = orgText;
         //Since we are modifying the Text, we need to Validate the Rect. Otherwise, the paint will go in recursion.
         win32.NativeWindowCommon.ValidateRect(this.Handle, IntPtr.Zero);

         if (Image == null)
         {
            if (Appearance == Appearance.Normal)
            {
               Rectangle textRect = new Rectangle();
               textRect = ClientRectangle;
               //Padding also includes the padding set for the BOX. But it is not needed while rendering the text.
               //So, ignore it.
               textRect.Location = new Point(textRect.Left + Padding.Left, textRect.Top + Padding.Top - checkTopPadding);
               textRect.Size = new Size(textRect.Width - (Padding.Left + Padding.Right), textRect.Height - (Padding.Top + Padding.Bottom - checkTopPadding));

               CheckBoxAndRadioButtonRenderer.DrawTextAndFocusRect(this, e, TextToDisplay, textRect, textOffset);
            }
            else
               ButtonRenderer.DrawButton(this, e, true, true);
         }
         else
         {
            if (Focused && Appearance == Appearance.Normal)
               CheckBoxAndRadioButtonRenderer.DrawFocusRectOnCheckBoxGlyph(this, e.Graphics);
         }

         // paint the border
         if (BorderType != BorderType.NoBorder)
            BorderRenderer.PaintBorder(e.Graphics, ClientRectangle, ForeColor, ControlStyle.TwoD, false, BorderType);
      }

#if PocketPC
      // OpenNetCF Checkbox2 does not use the indeterminate state. 
      // After it is changed in the base function, check the state and change if needed
      protected override void OnMouseDown(MouseEventArgs e)
      {
         // Save original state
         CheckState beforeBase = CheckState;
         // call base function
         base.OnMouseDown(e);

         // Set the state as it should be
         if (ThreeState && (beforeBase != CheckState))
         {
            switch (beforeBase)
            {
               case CheckState.Checked:
                  CheckState = CheckState.Indeterminate;
                  break;
               case CheckState.Indeterminate:
                  CheckState = CheckState.Unchecked;
                  break;
               case CheckState.Unchecked:
                  CheckState = CheckState.Checked;
                  break;
            }
         }
      }

      protected override void OnKeyPress(KeyPressEventArgs e)
      {
         // Save original state
         CheckState beforeBase = CheckState;
         // call base function
         base.OnKeyPress(e);

         // Set the state as it should be
         if(beforeBase != CheckState)
         {
            if (ThreeState)
            {
               switch (beforeBase)
               {
                  case CheckState.Checked:
                     CheckState = CheckState.Indeterminate;
                     break;
                  case CheckState.Indeterminate:
                     CheckState = CheckState.Unchecked;
                     break;
                  case CheckState.Unchecked:
                     CheckState = CheckState.Checked;
                     break;
               }
            }
            OnCheckStateChanged(null);
         }
      }

      public void CallKeyDown(KeyEventArgs e)
      {
         OnKeyDown(e);
      }
#endif
   }
}