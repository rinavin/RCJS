using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Diagnostics;

namespace com.magicsoftware.support
{
   /// <summary>
   /// This class is created to be used in lieu of System.Windows.Forms.ImageList.
   /// The reason for not using System.Windows.Forms.ImageList is that it has a limitation of 256X256 image size.
   /// </summary>
   public class MgImageList
   {
      private List<Image> images = new List<Image>();
      private static Size DefaultImageSize = new Size(0x10, 0x10);
      private Size imageSize;

      public List<Image> Images { get { return images; } }

      public Size ImageSize
      {
         get
         {
            return this.imageSize;
         }
         set
         {
            if (value.IsEmpty)
            {
               throw new ArgumentException("Empty image size", String.Empty);
            }
            if (value.Width <= 0)
            {
               throw new ArgumentOutOfRangeException("Invalid image width", String.Empty);
            }
            if (value.Height <= 0)
            {
               throw new ArgumentOutOfRangeException("Invalid image height", String.Empty);
            }

            if ((this.imageSize.Width != value.Width) || (this.imageSize.Height != value.Height))
            {
               this.imageSize = new Size(value.Width, value.Height);
            }
         }
      }

      public MgImageList()
      {
         this.imageSize = DefaultImageSize;
      }

      public void AddStrip(Image image)
      {
         if (image == null)
         {
            throw new ArgumentNullException("null image");
         }
         if ((image.Width == 0) || ((image.Width % ImageSize.Width) != 0))
         {
            throw new ArgumentException("invalid image: width not compatible with ImageSize", String.Empty);
         }
         if (image.Height != ImageSize.Height)
         {
            throw new ArgumentException("invalid image: height not compatible with ImageSize", String.Empty);
         }
         int nImages = image.Width / ImageSize.Width;

         for (int a = 0; a < nImages; a++)
         {
            Bitmap bm = new Bitmap(ImageSize.Width, ImageSize.Height);
            using (Graphics gr = Graphics.FromImage(bm))
            {
               var im = new ImageAttributes();
#if !PocketPC
               //DrawImage scales the image so the wrap mode need to be tile
               im.SetWrapMode(WrapMode.Tile);
#endif
               var rc = new Rectangle(0, 0, ImageSize.Width, ImageSize.Height);
               gr.DrawImage(image, rc, a * ImageSize.Width, 0, ImageSize.Width, ImageSize.Height, GraphicsUnit.Pixel, im);
            }

            images.Add(bm);
         }
      }

      /// <summary>
      /// Load image list.
      /// </summary>
      /// <param name="image"></param>
      /// <param name="imageListNumber"></param>
      /// <returns></returns>
      public static MgImageList LoadImageList(Image image, int imageListNumber)
      {
         MgImageList imageList = null;

         if (image != null)
         {
            Debug.Assert(imageListNumber > 0);
            int singleImageWidth = (image.Width / imageListNumber);
            int singleImageHeight = image.Height;
            Size singleImageSize = new Size(singleImageWidth, singleImageHeight);

            //Crop the Image so that it can be divided equally according to the single image width.
            if ((image.Width % singleImageWidth) > 0)
            {
               int newImageWidth = imageListNumber * singleImageWidth;
               var newSize = new Size(newImageWidth, singleImageHeight);
               image = ImageUtils.CropImage(image, newSize);
            }

            imageList = new MgImageList { ImageSize = singleImageSize };

            imageList.AddStrip(image);
         }

         return (imageList);
      }
   }
}
