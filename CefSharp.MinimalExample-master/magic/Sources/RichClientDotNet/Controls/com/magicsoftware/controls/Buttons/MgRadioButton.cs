using System;
using System.Windows.Forms;
using Controls.com.magicsoftware.support;
using System.Drawing;
#if !PocketPC
using ContentAlignment = System.Drawing.ContentAlignment;
#else
using System.Runtime.InteropServices;
using com.magicsoftware.win32;
using ContentAlignment = OpenNETCF.Drawing.ContentAlignment2;
using FlatStyle = com.magicsoftware.mobilestubs.FlatStyle;
using Appearance = com.magicsoftware.mobilestubs.Appearance;
#endif

namespace com.magicsoftware.controls
{
   public class MgRadioButton : RadioButton, IDisplayInfo, IContentAlignmentProperty, ITextProperty, IImageProperty, ISetSpecificControlPropertiesForFormDesigner
   {
      private const int OFFSET_TEXT = 6;

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

      private string orgText { get; set; } = String.Empty;
      #region ITextProperty Members

      public override string Text
      {
         get
         {
            return base.Text;
         }
         set
         {
            orgText = value;

            //For Radio button the '&' is part from the text and not an accelerator.
            value = orgText.Replace("&", "&&");

            TextToDisplay = value;
            if (IsBasePaint)
               base.Text = (Image != null ? String.Empty : value);
            else
               Invalidate();
         }
      }

      #endregion

      #region IDisplayInfo Members

      public string TextToDisplay { get; set; }

      #endregion

      public bool IsBasePaint { get; set; }

#if PocketPC
      public bool AutoCheck { get; set; }
      private ContentAlignment TextAlign_DO_NOT_USE_DIRECTLY { get; set; }
      public ContentAlignment CheckAlign { get; set; }
      public FlatStyle FlatStyle { get; set; }
      public Appearance Appearance { get; set; }
      public Image Image { get; set; }
      public void SetBounds(int x, int y, int width, int height)
      {
         this.Bounds = new Rectangle(x, y, width, height);
      }
#else
      public new Image Image
      {
         get { return base.Image; }
         set
         {
            base.Image = value;
            Text = orgText;
         }
      }

      /// <summary>
      /// Set Background color
      /// </summary>
      public override Color BackColor
      {
         get
         {
            return (Appearance == Appearance.Normal && base.BackColor.A < 255 ?  Color.Transparent : base.BackColor);
         }
         set
         {
            base.BackColor = value;
         }
      }


#endif

      public MgRadioButton()
         : base()
      {
#if !PocketPC
         //this is needed to support double click on button
         this.SetStyle(ControlStyles.StandardClick, true);
         this.SetStyle(ControlStyles.StandardDoubleClick, true);
#endif
#if PocketPC
         TextAlign = ContentAlignment.MiddleLeft;
         subclassRadioButton();
#endif
      }

      /// <summary></summary>
      /// <param name="e"></param>
      protected override void OnPaint(PaintEventArgs pevent)
      {
#if PocketPC
         base.OnPaint(pevent);
#else
         //We need to call base.OnPaint even if isBasePaint=false because the border and the radio
         //should be drawn by the Framework.
         base.OnPaint(pevent);

         if (!IsBasePaint && Image == null)
         {
            if (Appearance == Appearance.Normal)
               CheckBoxAndRadioButtonRenderer.DrawTextAndFocusRect(this, pevent, TextToDisplay, this.ClientRectangle, OFFSET_TEXT);
            else
               ButtonRenderer.DrawButton(this, pevent, true, true);
         }
#endif
      }

#if PocketPC
      // A delegate for our wndproc
      public delegate int MobileWndProc(IntPtr hwnd, uint msg, uint wParam, int lParam);
      // Original wndproc
      private IntPtr OrigWndProc;
      // object's delegate
      MobileWndProc proc;

      /// <summary> subclass the combobox - replace it's window proce
      /// </summary>
      void subclassRadioButton()
      {
         proc = new MobileWndProc(WindowProc);
         OrigWndProc = (IntPtr)NativeWindowCommon.SetWindowLong(Handle, NativeWindowCommon.GWL_WNDPROC,
                         Marshal.GetFunctionPointerForDelegate(proc).ToInt32());
      }

      // Our wndproc
      private int WindowProc(IntPtr hwnd, uint msg, uint wParam, int lParam)
      {
         MouseEventArgs mouseEvents;
         int ret = 0;
         KeyEventArgs keyEvents; 

         switch (msg)
         {
            case NativeWindowCommon.WM_LBUTTONDOWN:
               {
                  ret = NativeWindowCommon.CallWindowProc(OrigWndProc, hwnd, msg, wParam, lParam);
                  
                  mouseEvents = new MouseEventArgs(MouseButtons.Left, 0,
                     NativeWindowCommon.LoWord(lParam), NativeWindowCommon.HiWord(lParam), 0);

                  this.OnMouseDown(mouseEvents);
               }
               break;

            case NativeWindowCommon.WM_LBUTTONUP:
               {
                  ret = NativeWindowCommon.CallWindowProc(OrigWndProc, hwnd, msg, wParam, lParam);

                  mouseEvents = new MouseEventArgs(MouseButtons.Left, 0,
                     NativeWindowCommon.LoWord(lParam), NativeWindowCommon.HiWord(lParam), 0);

                  this.OnMouseUp(mouseEvents);
               }
               break;

            case NativeWindowCommon.WM_KEYDOWN:
               // Get the modifiers
               Keys modifiers = 0;
               if (user32.GetKeyState((int)Keys.ShiftKey) < 0) 
                  modifiers |= Keys.Shift;
               if (user32.GetKeyState((int)Keys.ControlKey) < 0) 
                  modifiers |= Keys.Control;
               if (user32.GetKeyState((int)Keys.Menu) < 0) 
                  modifiers |= Keys.Alt; 

               keyEvents = new KeyEventArgs((Keys)wParam | modifiers);
               this.OnKeyDown(keyEvents);
               break;

            case NativeWindowCommon.WM_KEYUP:
               keyEvents = new KeyEventArgs((Keys)wParam);
               this.OnKeyUp(keyEvents);
               break;
                        
            case NativeWindowCommon.WM_CHAR:
               break;
            
            case NativeWindowCommon.BM_CLICK:
               break;

            default:
               ret = NativeWindowCommon.CallWindowProc(OrigWndProc, hwnd, msg, wParam, lParam);
               break;
         }

         return ret;
      }
#endif   
   
      public void setSpecificControlPropertiesForFormDesigner(Control fromControl)
      {
         // for inner radio that is user control we create it as component and he take the all properties asis
         // we need to set the property enabled to true so it will be seen as enable 
         Enabled = true;
      }
   }
}
