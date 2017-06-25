using System;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using com.magicsoftware.win32;
using System.Globalization;
using com.magicsoftware.util;
using System.Runtime.InteropServices;
using System.Windows.Forms.VisualStyles;

namespace com.magicsoftware.controls
{
#if !PocketPC
   [ToolboxBitmap(typeof(RichTextBox))]
#endif
   public class MgRichTextBox : RichTextBox, IRightToLeftProperty, IBorderStyleProperty, ISetSpecificControlPropertiesForFormDesigner
   {
      public StringBuilder ImeReadStrBuilder { get; set; } // JPN: ZIMERead function
      public bool isTransparent { get; set; }
      // Korean IME
      public int KoreanInterimSel { get; set; }

      private ControlStyle controlStyle = ControlStyle.NoBorder;

      /// <summary>
      /// Set the border style to No Border, 2D & 3D-Sunken.
      /// </summary>
      public ControlStyle ControlStyle
      {
         get
         {
            return controlStyle;
         }
         set
         {
            controlStyle = value;
               if (controlStyle == ControlStyle.NoBorder || controlStyle == ControlStyle.ThreeD)
                  this.BorderStyle = BorderStyle.None;
               else if (controlStyle == ControlStyle.TwoD)
                  this.BorderStyle = BorderStyle.FixedSingle;
               else
                  this.BorderStyle = BorderStyle.Fixed3D;

               UpdateControlStyles();
         }
      }

      Control parentInternal = null;

      public override RightToLeft RightToLeft 
      {
         get 
         {
            return base.RightToLeft;
         }

         set 
         {
            base.RightToLeft = value;
            UpdateControlStyles();
         }
      }

      public override Color BackColor
      {
         get
         {
            return base.BackColor;
         }
         set
         {
            base.BackColor = value;
            isTransparent = (base.BackColor == Color.Transparent);
            UpdateControlStyles();
         }
      }

      public MgRichTextBox()
         : base()
      {
         if (CultureInfo.CurrentCulture.LCID == 1041) // Japanese
         {
            ImeReadStrBuilder = new StringBuilder();
            ImeReadStrBuilder.Capacity = 128;
         }
         else
            ImeReadStrBuilder = null;
            
         KoreanInterimSel = -1;

         this.SetStyle(ControlStyles.SupportsTransparentBackColor, true);
      }
      
      /// <summary>
      /// 
      /// </summary>
      /// <param name="disposing"></param>
      protected override void Dispose(bool disposing)
      {
         base.Dispose(disposing);

         if (disposing && parentInternal != null)
            parentInternal.Paint -= Parent_Paint;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="e"></param>
      protected override void OnParentChanged(EventArgs e)
      {
         base.OnParentChanged(e);

         if (parentInternal != null)
            parentInternal.Paint -= Parent_Paint;

         parentInternal = Parent;

         if (parentInternal != null)
            parentInternal.Paint += Parent_Paint;
      }

      protected override void OnResize(EventArgs eventargs)
      {
         InvalidateNC();
         base.OnResize(eventargs);
      }

      /// <summary>
      /// Invalidates the non client area of the rich text box control.
      /// </summary>
      private void InvalidateNC()
      {
         NativeWindowCommon.SetWindowPos(this.Handle, IntPtr.Zero, 0, 0, 0, 0,
                              NativeWindowCommon.SWP_NOMOVE |
                              NativeWindowCommon.SWP_NOSIZE |
                              NativeWindowCommon.SWP_NOZORDER |
                              NativeWindowCommon.SWP_NOACTIVATE |
                              NativeWindowCommon.SWP_DRAWFRAME);
      }

   /// <summary>
   /// Update the control parameter's styles.
   /// </summary>
      private void UpdateControlStyles()
      {
         int fStyle = NativeHeader.GetWindowLong(Handle, NativeHeader.GWL_EXSTYLE);
         
         if (BorderStyle == BorderStyle.FixedSingle)
         {
            // remove the Fixed3D border style
            fStyle &= ~NativeWindowCommon.WS_EX_CLIENTEDGE;
            InvalidateNC();
         }
         
         if (isTransparent)
            fStyle |= NativeHeader.WS_EX_TRANSPARENT;
         else
            fStyle &= ~NativeHeader.WS_EX_TRANSPARENT;

         NativeWindowCommon.SetWindowLong(Handle, NativeHeader.GWL_EXSTYLE, fStyle);

         fStyle = NativeHeader.GetWindowLong(Handle, NativeHeader.GWL_STYLE);
         if (base.RightToLeft == RightToLeft.Yes)
         {
            fStyle |= NativeHeader.WS_EX_LTRREADING;
            fStyle |= NativeHeader.WS_EX_RIGHT;
            fStyle |= NativeHeader.WS_EX_LEFTSCROLLBAR;
         }
         else if (base.RightToLeft == RightToLeft.No)
         {
            fStyle |= NativeHeader.WS_EX_RTLREADING;
            fStyle |= NativeHeader.WS_EX_LEFT;
            fStyle |= NativeHeader.WS_EX_RIGHTSCROLLBAR;
         }

         NativeWindowCommon.SetWindowLong(Handle, NativeHeader.GWL_STYLE, fStyle);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      void Parent_Paint(object sender, PaintEventArgs e)
      {
         if (isTransparent)
            this.Invalidate();
      }

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

      protected override void WndProc(ref Message m)
      {
         if (m.Msg == NativeWindowCommon.WM_IME_COMPOSITION)
         {
            switch (CultureInfo.CurrentCulture.LCID)
            {
               case 1041: // Japanese (JPN: ZIMERead function)
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
                  break;

               case 1042: // Korean
                  if (KoreanInterimSel < 0)
                  {
                     KoreanInterimSel = NativeWindowCommon.SendMessage(this.Handle, NativeWindowCommon.EM_GETSEL, 0, 0);
                  }
                  break;
            }
         }
         else if (m.Msg == NativeWindowCommon.WM_IME_ENDCOMPOSITION)
         {
            if (CultureInfo.CurrentCulture.LCID == 1042)
               KoreanInterimSel = -1;
         }
         else if (m.Msg == NativeWindowCommon.WM_PAINT)
         {
            if (isTransparent)
               NativeWindowCommon.SendMessage(this.Handle, NativeWindowCommon.WM_NCPAINT, 0, 0);

            if (ControlStyle == ControlStyle.ThreeD)
            {  
               base.WndProc(ref m);
               using (Graphics g = Graphics.FromHwnd(this.Handle))
               {
                  BorderRenderer.PaintBorder(g, ClientRectangle, ForeColor, ControlStyle.ThreeD, false);
                  return;
               }
            }
         }
         else if (m.Msg == NativeWindowCommon.WM_NCCALCSIZE)
         {
            if (BorderStyle == BorderStyle.FixedSingle)
               RecalcNonClientArea(ref m);
         }
         else if (m.Msg == NativeWindowCommon.WM_NCPAINT)
         {
            if (BorderStyle == BorderStyle.FixedSingle)
               WMNCPaint(ref m);
         }
         base.WndProc(ref m);
      }

      /// <summary>
      /// Paint the border
      /// </summary>
      /// <param name="m"></param>
      private void WMNCPaint(ref Message m)
      {
         IntPtr hDC = NativeWindowCommon.GetWindowDC(m.HWnd);
         if (hDC != IntPtr.Zero)
         {
            IntPtr hrgnWindow = IntPtr.Zero;
            IntPtr hrgnClient = IntPtr.Zero;
            try
            {
               Rectangle rect = new Rectangle(1, 1, Width - 2, Height - 2);
               hrgnWindow = NativeWindowCommon.CreateRectRgn(0, 0, Width, Height);
               hrgnClient = NativeWindowCommon.CreateRectRgn(rect.Left, rect.Top, rect.Right, rect.Bottom);

               //Creates the union of two combined regions except for any overlapping areas.
               NativeWindowCommon.CombineRgn(hrgnWindow, hrgnWindow, hrgnClient, NativeWindowCommon.RGN_XOR);
               NativeWindowCommon.SelectClipRgn(hDC, hrgnWindow);

               using (Graphics g = Graphics.FromHdc(hDC))
               {
                  g.Clear(VisualStyleInformation.TextControlBorder);
               }
               m.Result = IntPtr.Zero;
            }
            finally
            {
               NativeWindowCommon.ReleaseDC(m.HWnd, hDC);
               if (hrgnWindow != IntPtr.Zero)
               {
                  NativeWindowCommon.DeleteObject(hrgnWindow);
               }
               if (hrgnClient != IntPtr.Zero)
               {
                  NativeWindowCommon.DeleteObject(hrgnClient);
               }
            }
         }
      }

      /// <summary>
      /// Calculates the size of the window frame and client area of the RichTextBox
      /// </summary>
      /// <param name="lParam"></param>
      private void RecalcNonClientArea(ref Message m)
      {
         //Check WPARAM
         if (m.WParam != IntPtr.Zero)
         {
            //If wParam is TRUE, lParam points to an NCCALCSIZE_PARAMS structure that contains information an application can use to calculate the new size and position of the client rectangle.
            var nccsp = (NativeWindowCommon.NCCALCSIZE_PARAMS)Marshal.PtrToStructure(m.LParam, typeof(NativeWindowCommon.NCCALCSIZE_PARAMS));

            //We're adjusting the size of the client area here. Right now, the client area is the whole area of RichTextBox.
            //Adding to the Top, Bottom, Left, and Right will size the client area
            nccsp.rgrc0.top += 1;       //1-pixel top border
            nccsp.rgrc0.bottom -= 1;    //1-pixel bottom (resize) border
            nccsp.rgrc0.left += 1;      //1-pixel left (resize) border
            nccsp.rgrc0.right -= 1;     //1-pixel right (resize) border

            //Set the structure back into memory
            Marshal.StructureToPtr(nccsp, m.LParam, true);
         }
         else
         {
            //If wParam is FALSE, lParam points to a RECT structure. On entry, the structure contains the proposed window rectangle for the window.
            //On exit, the structure should contain the screen coordinates of the corresponding window client area.
            var clnRect = (NativeWindowCommon.RECT)Marshal.PtrToStructure(m.LParam, typeof(NativeWindowCommon.RECT));

            //Like before, we're adjusting the rectangle...
            //Adding to the Top, Bottom, Left, and Right will size the client area.
            clnRect.top += 1;       //1-pixel top border
            clnRect.bottom -= 1;    //1-pixel bottom (resize) border
            clnRect.left += 1;      //1-pixel left (resize) border
            clnRect.right -= 1;     //1-pixel right (resize) border

            //Set the structure back into memory
            Marshal.StructureToPtr(clnRect, m.LParam, true);
         }

         //Return Zero
         m.Result = IntPtr.Zero;

      }

      public void setSpecificControlPropertiesForFormDesigner(Control fromControl)
      {
         ControlUtils.SetTextFotRichTextBox(this, ((RichTextBox)fromControl).Rtf);
      }
   }
}