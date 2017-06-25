using System;
using System.Drawing;
using System.Windows.Forms;
using Controls.com.magicsoftware.support;
using com.magicsoftware.util;
using com.magicsoftware.win32;
using com.magicsoftware.support;
#if PocketPC
using Button = OpenNETCF.Windows.Forms.Button2;
using FlatButtonAppearance = com.magicsoftware.mobilestubs.FlatButtonAppearance;
using FlatStyle = com.magicsoftware.mobilestubs.FlatStyle;
using ImageLayout = com.magicsoftware.mobilestubs.ImageLayout;
using RightToLeft = com.magicsoftware.mobilestubs.RightToLeft;
using ContentAlignment = OpenNETCF.Drawing.ContentAlignment2;
#endif

namespace com.magicsoftware.controls
{
   public abstract class MgButtonBase : Button, IDisplayInfo, IMultilineProperty, IContentAlignmentProperty,
                                        IRightToLeftProperty, IGradientColorProperty, ITextProperty
   {
      #region IMultilineRenderer Members

      public bool Multiline { get; set; }

      #endregion

      #region IGradientColorProperty Members

      public GradientColor GradientColor { get; set; }
      public GradientStyle GradientStyle { get; set; }

      #endregion

      #region IDisplayInfo Members

      public string TextToDisplay { get; protected set; }

      #endregion

      public bool IsBasePaint { get; set; }
#if PocketPC
      public Boolean UseVisualStyleBackColor { get; set; }
      public FlatStyle FlatStyle { get; set; }
      public FlatButtonAppearance FlatAppearance { get; set; }
      public ImageLayout BackgroundImageLayout { get; set; }
      public RightToLeft RightToLeft { get; set; }
#endif

      public MgButtonBase()
         : base()
      {
         //TODO : fix double click for button
         //this is needed to support double click on button
         //this.SetStyle(ControlStyles.StandardClick, true);
         //this.SetStyle(ControlStyles.StandardDoubleClick, true);
#if PocketPC
         FlatAppearance = new FlatButtonAppearance();
         // For header/table test - in RC, it is set for each and every button
         IsBasePaint = true;
#endif
         GradientStyle = GradientStyle.None;
         SetDefaultColor();
         TextAlign = ContentAlignment.MiddleCenter;
      }

      /// <summary> Sets the default color of the Button. </summary>
      public void SetDefaultColor()
      {
         BackColor = SystemColors.Control;
         // fixed bug #:242632, for Button control the default ForeColor property will be SystemColors.WindowText
         ForeColor = SystemColors.WindowText;
         UseVisualStyleBackColor = true;
      }
#if !PocketPC
      protected override bool ProcessMnemonic(char inputChar)
      {
         if (CanSelect && IsMnemonic(inputChar, Text) && ((Control.ModifierKeys & Keys.Alt) == Keys.Alt))
         {
            this.OnClick(new System.EventArgs());
            return true;
         }
         return false;
      }
#endif

#if PocketPC
      public void CallKeyDown(KeyEventArgs e)
      {
         OnKeyDown(e);
      }
#endif
   }

#if !PocketPC
   [ToolboxBitmap(typeof(Button))]
#endif
   public class MgPushButton : MgButtonBase
   {
      public override Color BackColor
      {
         get
         {
            return base.BackColor;
         }
         set
         {
            base.BackColor = value;
            UseVisualStyleBackColor = (BackColor == SystemColors.Control);
            FlatStyle = FlatStyle.Standard;
         }
      }

      public MgPushButton()
         : base()
      {
         FlatStyle = FlatStyle.Standard;
      }

      /// <summary></summary>
      /// <param name="e"></param>
      protected override void OnPaint(PaintEventArgs e)
      {
         //base paint should always be called -- even if IsBasePaint is false 
         //because the borders should always be painted by the framework.
         String orgText = Text;
         TextToDisplay = Text;
         base.Text = String.Empty;
         base.OnPaint(e);
         base.Text = orgText;
         //Since we are modifying the Text, we need to Validate the Rect. Otherwise, the paint goes in recursion.
         NativeWindowCommon.ValidateRect(this.Handle, IntPtr.Zero);

         //fixedbug #:735118: when the button is UseVisualStyleBackColor=true(no color is set) and it is normal button
         //                   let window to paint the backgound of the control.
         // QCR #997321: if the button has gradient style then we should paint the background.
         bool paintBackground = !UseVisualStyleBackColor || (GradientStyle != GradientStyle.None);

         ButtonRenderer.DrawButton(this, e, paintBackground, true);
      }

      protected override void OnKeyDown(KeyEventArgs kevent)
      {
         base.OnKeyDown(kevent);
      }
   }

#if !PocketPC
   [ToolboxBitmap(typeof(Button))]
#endif
   public class MgImageButton : MgButtonBase
   {
      public int PBImagesNumber { get; set; }
      public new MgImageList ImageList { get; set; }

      public MgImageButton()
         : base()
      {
         FlatStyle = FlatStyle.Flat;
         FlatAppearance.BorderSize = 0;
         BackgroundImageLayout = ImageLayout.Stretch;
      }

      /// <summary> Whether this is an Image button supporting 6 images </summary>
      /// <returns></returns>
      public bool Supports6Images()
      {
         return (PBImagesNumber == 6);
      }

      /// <summary></summary>
      /// <param name="e"></param>
      protected override void OnPaint(PaintEventArgs e)
      {
         if (IsBasePaint)
            base.OnPaint(e);
         else
         {
#if !PocketPC
            //Simulate Transparency
            if (BackColor == Color.Transparent || BackColor.A < 255)
            {
               if (Parent is ISupportsTransparentChildRendering)
                  ((ISupportsTransparentChildRendering)Parent).TransparentChild = this;

               System.Drawing.Drawing2D.GraphicsContainer g = e.Graphics.BeginContainer();
               Rectangle translateRect = this.Bounds;
               e.Graphics.TranslateTransform(-Left, -Top);
               PaintEventArgs pe = new PaintEventArgs(e.Graphics, translateRect);
               this.InvokePaintBackground(Parent, pe);
               this.InvokePaint(Parent, pe);
               e.Graphics.ResetTransform();
               e.Graphics.EndContainer(g);
               pe.Dispose();

               if (Parent is ISupportsTransparentChildRendering)
                  ((ISupportsTransparentChildRendering)Parent).TransparentChild = null;
            }
#endif
            
            TextToDisplay = Text;
            ButtonRenderer.DrawButton(this, e, true, true);
         }
      }
   }
}