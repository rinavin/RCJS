using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using com.magicsoftware.controls.designers;
using com.magicsoftware.controls.utils;
using com.magicsoftware.util;
using com.magicsoftware.win32;

#if PocketPC
using RightToLeft = com.magicsoftware.mobilestubs.RightToLeft;
using ImageLayout = com.magicsoftware.mobilestubs.ImageLayout;
#endif

namespace com.magicsoftware.controls
{
#if !PocketPC
   [Designer(typeof(MgPanelDesigner))]
   [ToolboxBitmap(typeof(ResourceFinder), "Controls.Resources.Subform16x16.bmp")]
   public class MgPanel : Panel, IRightToLeftProperty, IGradientColorProperty, IBorderStyleProperty, ICanParent, IMgContainer
#else
   public class MgPanel : Panel, IRightToLeftProperty, IGradientColorProperty, IBorderStyleProperty, ICanParent
#endif
   {
#if !PocketPC
      public event NCMouseEventHandler NCMouseDown;

      /// <summary>
      /// event is raised  when processDialogKey request is received 
      /// 
      /// </summary>
      public event EventHandler DialogKeyReceived;
#endif

      #region IGradientColorProperty Members

      public GradientColor GradientColor { get; set; }
      public GradientStyle GradientStyle { get; set; }

      #endregion

      /// <summary>
      /// mg panel class - used to support double buffering on panel
      /// </summary>
      public MgPanel()
         : base()
      {
#if !PocketPC
         DoubleBuffered = true;
#else
         dummy = new Control();
         dummy.Size = new Size(0, 0);
         dummy.TabStop = false;
         Controls.Add(dummy);
#endif
         GradientStyle = GradientStyle.None;
      }

      protected override CreateParams CreateParams
      {
         get
         {
            CreateParams createParams =  base.CreateParams;
            //for defect 129854
            //use this special to prevent flickering during swipe on touch screens
            if (Utils.SpecialSwipeFlickeringRemoval)
               createParams.ExStyle |= NativeWindowCommon.WS_EX_COMPOSITED;
            return createParams;

         }
      }

#if !PocketPC
      /// <summary> override default .Net behavior of reseting the scroll
      /// position on gaining focus.
      /// </summary>
      /// <param name="activeControl"></param>
      /// <returns></returns>
      protected override Point ScrollToControl(Control activeControl)
      {
         // Returning the current location prevents the panel from  
         // scrolling to the active control when the panel loses and regains focus  
         return this.DisplayRectangle.Location;
      }

      /// <summary>
      /// override default .NET behavior
      /// pressing this keys causes focusing on the next or other control ,
      /// and want magic logic to be executed instead
      /// </summary>
      /// <param name="keyData"></param>
      /// <returns></returns>
      protected override bool ProcessDialogKey(Keys keyData)
      {
         OnDialogKeyReceived();
         if (!this.Focused && (keyData & (Keys.Alt | Keys.Control)) == Keys.None)
         {
            Keys keyCode = (Keys)keyData & Keys.KeyCode;
            switch (keyCode)
            {
               case Keys.Tab:
               case Keys.Left:
               case Keys.Right:
               case Keys.Up:
               case Keys.Down:

                  return true;
            }
         }

         return base.ProcessDialogKey(keyData);

      }

      /// <summary>
      /// on dialog key recieved
      /// </summary>
      protected void OnDialogKeyReceived()
      {
         if (DialogKeyReceived != null)
            DialogKeyReceived(this, new EventArgs());
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="se"></param>
      protected override void OnScroll(ScrollEventArgs se)
      {
         base.OnScroll(se);

         if (BackColor == Color.Transparent || BackColor.A < 255)
            Invalidate();
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="e"></param>
      protected override void OnPaintBackground(PaintEventArgs e)
      {
         if (BackColor == Color.Transparent || BackColor.A < 255)
         {
            if (Parent is ISupportsTransparentChildRendering)
               ((ISupportsTransparentChildRendering)Parent).TransparentChild = this;

            int hScrollHeight = (this.HScroll ? SystemInformation.HorizontalScrollBarHeight : 0);
            int vScrollWidth = (this.VScroll ? SystemInformation.VerticalScrollBarWidth : 0);

            int borderWidth = ((this.Width - this.ClientSize.Width) - vScrollWidth) / 2;

            System.Drawing.Drawing2D.GraphicsContainer g = e.Graphics.BeginContainer();
            Rectangle translateRect = this.Bounds;
            e.Graphics.TranslateTransform(-(Left + borderWidth), -(Top + borderWidth));
            PaintEventArgs pe = new PaintEventArgs(e.Graphics, translateRect);
            this.InvokePaintBackground(Parent, pe);
            this.InvokePaint(Parent, pe);
            e.Graphics.ResetTransform();
            e.Graphics.EndContainer(g);
            pe.Dispose();

            if (Parent is ISupportsTransparentChildRendering)
               ((ISupportsTransparentChildRendering)Parent).TransparentChild = null;
         }

         ControlRenderer.PaintMgPanel(this, e.Graphics);
      }

      public bool SuspendPaint { get; set; }
      protected override void WndProc(ref Message m)
      {
         switch (m.Msg)
         {
            case NativeWindowCommon.WM_PAINT:
               if (m.Msg == NativeWindowCommon.WM_PAINT && SuspendPaint)
               {
                  //Ignore the paint message if SuspendPaint=true.
                  return;
               }
               break;

            case NativeWindowCommon.WM_NCLBUTTONDOWN:
            case NativeWindowCommon.WM_NCRBUTTONDOWN:
               OnNCMouseDown(m);
               break;
         }
         base.WndProc(ref m);
      }

      /// <summary>
      /// raised on non client mouse down
      /// </summary>
      /// <param name="msg"></param>
      protected virtual void OnNCMouseDown(Message m)
      {
         if (NCMouseDown != null)
            NCMouseDown(this, new NCMouseEventArgs(m));
      }

#endif


#if PocketPC
      public ImageLayout BackgroundImageLayout { get; set; }
      private Image image;
      public Image BackGroundImage
      {
         get
         {
            return image;
         }
         set
         {
            image = value;
         }
      }
      public RightToLeft RightToLeft { get; set; }
      // This property is used to avoid the exception is setFont
      public override Font Font { get; set; }
      public BorderStyle BorderStyle { get; set; }

      // AutoScrollMinSize not supported on CF - use  a dummy control to force a minimum size on the panel,
      // and to calculate it's display rectangle
      public Control dummy;
      public Rectangle DisplayRectangle
      {
         get
         {
            return new Rectangle(0, 0, dummy.Left, dummy.Top);
         }
      }
      public Size AutoScrollMinSize
      {
         get
         {
               return new Size(dummy.Left, dummy.Top);
         }

         set
         {
               if (dummy.Left != value.Width)
                  dummy.Left = value.Width;
               if (dummy.Top != value.Height)
                  dummy.Top = value.Height;
         }
      }
#endif

      public event CanParentDelegate CanParentEvent;

      public bool CanParent(CanParentArgs allowDragDropArgs)
      {
         if (CanParentEvent != null)
            return CanParentEvent(this, allowDragDropArgs);

         return true;
      }

#region IMgContainer
#if !PocketPC
      public event ComponentDroppedDelegate ComponentDropped;

      public void OnComponentDropped(ComponentDroppedArgs args)
      {
         if (ComponentDropped != null)
            ComponentDropped(this, args);
      }
#endif
#endregion
   }
}
