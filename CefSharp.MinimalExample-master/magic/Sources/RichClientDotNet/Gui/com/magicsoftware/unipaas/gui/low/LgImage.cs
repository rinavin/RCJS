using System;
using System.Drawing;
using System.Windows.Forms;
using com.magicsoftware.util;
using com.magicsoftware.controls;
using com.magicsoftware.support;

namespace com.magicsoftware.unipaas.gui.low
{
   /// <summary>
   /// static implementation for image control
   /// </summary>
   internal class LgImage : LogicalControl
   {
      private Size _prevSize; // used for images

      //   original image
      internal Image OrgImage { get; private set; }
      //   style of image
      internal CtrlImageStyle ImageStyle { get; private set; }
      internal Image DisplayImage { get; private set; }

      /// <summary>
      /// Animate the display image
      /// </summary>
      internal LgImage(GuiMgControl guiMgControl, Control containerControl)
         : base(guiMgControl, containerControl)
      {
      }

      internal void SetImage(Image image, CtrlImageStyle imageStyle)
      {
         OrgImage = image;
         ImageStyle = imageStyle;
         
         SetBackgroundImage();
         
         Refresh(true);
      }

      /// <summary>
      /// 
      /// </summary>
      internal void SetBackgroundImage()
      {
         Rectangle displayRect = getRectangle();
#if !PocketPC
         if (DisplayImage != null)
         {
            if (DisplayImage.Tag != null)
            {
               ImageAnimator.StopAnimate(DisplayImage, OnFrameChanged);
               DisplayImage.Dispose();
            }
         }
#endif
         DisplayImage = ImageUtils.GetImageByStyle(OrgImage, ImageStyle, displayRect.Size, false);
      }

#if !PocketPC
      internal void AnimateImage()
      {
         if (DisplayImage.Tag == null)
         {
            // Begin the animation only once.
            DisplayImage.Tag = true;

            // Begin the animation.
            ImageAnimator.Animate(DisplayImage, OnFrameChanged);
         }
      }

      /// <summary>
      ///   handle the animation FrameChanged event
      /// </summary>
      /// <param name = "sender"></param>
      /// <param name = "e"></param>
      internal void OnFrameChanged(object sender, EventArgs e)
      {
         //Force a call to the Paint event handler.

         Invalidate(false);
      }
#endif

      internal override void paintBackground(Graphics g, Rectangle rect, Color bgColor, Color fgColor, bool keepColor)
      {
         Rectangle rectImage = this.getRectangle();
         ControlRenderer.PaintBackGround(g, rectImage, bgColor, true);

        // no background
      }

      internal override void paintForeground(Graphics g, Rectangle rect, Color fgColor)
      {
#if PocketPC
           offsetStaticControlRect( ref rect);
           
#endif
         drawImage(g, rect);
      }
 
      /// <summary>
      /// set image control properties
      /// </summary>
      /// <param name="control"></param>
      internal override void setSpecificControlProperties(Control control)
      {
         GuiUtils.setImageInfoOnTagData(control, OrgImage, ImageStyle);
         GuiUtils.setBackgroundImage(control);
      }

      private void setBackgroundImage(Rectangle displayRect)
      {
         if (_prevSize != displayRect.Size &&
             (ImageStyle == CtrlImageStyle.Tiled || DisplayImage == null))
         {
            SetBackgroundImage();
            _prevSize = displayRect.Size;
         }
      }

      /// <summary>
      /// draw image
      /// </summary>
      /// <param name="gr"></param>
      /// <param name="displayRect"></param>
      internal void drawImage(Graphics gr, Rectangle displayRect)
      {
         bool hasBorder = Style == ControlStyle.TwoD || Style == ControlStyle.ThreeDSunken;

         setBackgroundImage(displayRect);
         if (DisplayImage != null)
         {
#if !PocketPC

            if (ImageAnimator.CanAnimate(DisplayImage))
               AnimateImage();
#endif
            using (Region imageRegion = new Region(displayRect))
            {
               Region r = gr.Clip;
               gr.Clip = imageRegion;
               Size size = ImageUtils.ImageSize(DisplayImage, ImageStyle, displayRect.Size, false);
               Rectangle rect = new Rectangle(displayRect.X, displayRect.Y,
                      size.Width, size.Height);
               if (hasBorder)
               {
                  rect.X++;
                  rect.Y++;
               }
#if !PocketPC //tmp
               gr.DrawImage(DisplayImage, rect);
#else
               // This is probably what the original call is doing - need to verify
               gr.DrawImage(DisplayImage, displayRect.X, displayRect.Y);
#endif
               gr.Clip = r;

            }
         }
         
         BorderRenderer.PaintBorder(gr, displayRect, FgColor, Style, false);
      }

      internal override void Dispose()
      {
         base.Dispose();
         if (DisplayImage != null)
         {
#if !PocketPC
            if (DisplayImage.Tag != null)
               ImageAnimator.StopAnimate(DisplayImage, this.OnFrameChanged);
#endif
            DisplayImage.Dispose();
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="control"></param>
      internal override void setSpecificControlPropertiesForFormDesigner(Control control)
      {
         ControlUtils.SetStyle3D(control, Style);
      }
   }
}

