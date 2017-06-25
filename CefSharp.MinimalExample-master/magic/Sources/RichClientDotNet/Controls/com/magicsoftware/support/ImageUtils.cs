using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using com.magicsoftware.util;
using System.Diagnostics;
using com.magicsoftware.win32;
using com.magicsoftware.controls;

namespace com.magicsoftware.support
{
   /// <summary>
   ///   ImageUtils is used for implementing Image functions not supported on .Net Compact Framework
   /// </summary>
   public class ImageUtils
   {
      public static IImageLoader ImageLoader { get; set; }

      private const int DOTNET_MAX_IMAGE_LIST_HEIGHT = 256;
      private const int DOTNET_MAX_IMAGE_LIST_WIDTH = 256;

      /// <summary>
      /// Creates a new Image out of the source image based on the style and size.
      /// </summary>
      /// <param name="srcImage">the source Image from which a new Image will be created.</param>
      /// <param name="imageStyle">the style of the created image</param>
      /// <param name="size">the size of the image control (Form/PictureBox)</param>
      /// <param name="getBottomUpImage">Decide whether the image should be drawn from bottom to top for copied image style.
      /// This value is true for guiOutput type image control</param>
      /// <returns>the new image created.</returns>
      public static Image GetImageByStyle(Image srcImage, CtrlImageStyle imageStyle, Size size, bool getBottomUpImage)
      {
         Image retImage = null;

         // if the width or the height is 0 don't create the image
         if (srcImage != null && size.Width > 0 && size.Height > 0)
         {
            Size newSize = ImageSize(srcImage, imageStyle, size, getBottomUpImage);

            switch (imageStyle)
            {
               case CtrlImageStyle.ScaleFit:
               case CtrlImageStyle.ScaleFill:
                  if (newSize.Width != 0 && newSize.Height != 0)
                     retImage = GetScaledImage(srcImage, newSize.Width, newSize.Height);
                  break;
               case CtrlImageStyle.Distorted:
                  retImage = GetScaledImage(srcImage, newSize.Width, newSize.Height);
                  break;
               case CtrlImageStyle.Copied:
                  retImage = GetCopiedImage(srcImage, newSize.Width, newSize.Height, getBottomUpImage);
                  break;

               case CtrlImageStyle.Tiled:
                  retImage = GetTiledImage(srcImage, newSize);
                  break;

               default:
                  Debug.Assert(false);
                  break;
            }
         }

         return retImage;
      }

      /// <summary>
      ///   Return a scaled image according to the desired width and height
      /// </summary>
      /// <param name = "image"></param>
      /// <param name = "width"></param>
      /// <param name = "height"></param>
      /// <returns></returns>
      public static Image GetScaledImage(Image image, int width, int height)
      {
         return GetScaledImage(image, width, height, new Rectangle(0, 0, image.Width, image.Height));
      }

      /// <summary>
      /// Returns the copied image according to the desired width and height.
      /// </summary>
      /// <param name="image"></param>
      /// <param name="width"></param>
      /// <param name="height"></param>
      /// <param name="getBottomUpImage"></param>
      /// <returns></returns>
      public static Image GetCopiedImage(Image image, int width, int height, bool getBottomUpImage)
      {
         Image retImage = null;

         if (getBottomUpImage)
         {
            retImage = new Bitmap(width, height);
            using (Graphics gr = Graphics.FromImage(retImage))
            {
               var rc = new Rectangle(0, 0, width, height);
               //if image control width or height is less than the image width or height then show the image from bottom to top.
               gr.DrawImage(image, rc, rc.X, Math.Max(0, image.Height - height), width, height, GraphicsUnit.Pixel);
            }
         }
         else
            retImage = (Image)image.Clone();

         return retImage;
      }

      /// <summary>
      ///   Return a scaled image according to the desired width and height
      /// </summary>
      /// <param name="image"></param>
      /// <param name="width"></param>
      /// <param name="height"></param>
      /// <param name="srcRect"></param>
      /// <returns></returns>
      public static Image GetScaledImage(Image image, int width, int height, Rectangle srcRect)
      {
         var bmp = new Bitmap(width, height);
         using (Graphics gr = Graphics.FromImage(bmp))
         {
            var im = new ImageAttributes();
#if !PocketPC
            //Fixed bug #:775804,DrawImage is scaled the image so the wrap mode need to be tile
            im.SetWrapMode(WrapMode.TileFlipXY);
            if (width != image.Width || height != image.Height)
               gr.InterpolationMode = InterpolationMode.HighQualityBicubic;
#endif
            var rc = new Rectangle(0, 0, width, height);
            gr.DrawImage(image, rc, srcRect.X, srcRect.Y, srcRect.Width, srcRect.Height, GraphicsUnit.Pixel, im);
         }
         return bmp;
      }

      /// <summary>
      ///   Returns a tiled image
      /// </summary>
      /// <param name = "srcImage">source image</param>
      /// <param name = "newSize">size of new image</param>
      /// <returns>tiled image constructed of source image</returns>
      public static Image GetTiledImage(Image srcImage, Size newSize)
      {
         Image retImage = CreateBlankImage(srcImage, newSize);
         var srcRect = new Rectangle(0, 0, srcImage.Width, srcImage.Height);
         var destRect = new Rectangle(0, 0, srcImage.Width, srcImage.Height);

         using (Graphics graphics = Graphics.FromImage(retImage))
         {
            Color transparentColor = GetTransparentColor(srcImage);
            if (transparentColor != Color.Empty)
               graphics.Clear(transparentColor);

            // Paint the image through the height
            for (int y = 0;
                 y < newSize.Height;
                 y += srcImage.Height)
            {
               destRect.Y = y;

               // Paint the image through the width in each line
               for (int x = 0;
                    x < newSize.Width;
                    x += srcImage.Width)
               {
                  destRect.X = x;
                  graphics.DrawImage(srcImage, destRect, srcRect, GraphicsUnit.Pixel);
               }
            }
         }
         return retImage;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="srcImage"></param>
      /// <param name="imageStyle"></param>
      /// <param name="size"></param>
      /// <param name="getBottomUpImage"></param>
      /// <returns></returns>
      public static Size ImageSize(Image srcImage, CtrlImageStyle imageStyle, Size size, bool getBottomUpImage)
      {
         // if the width or the height is 0 don't create the image
         Size newSize = new Size();
         if (size.Width != 0 && size.Height != 0)
         {
            int dx, dy;

            switch (imageStyle)
            {
               case CtrlImageStyle.Copied:
                  if (getBottomUpImage)
                     newSize = size;
                  else
                     newSize = srcImage.Size;
                  break;
               case CtrlImageStyle.Distorted:
               case CtrlImageStyle.Tiled:
                  newSize = size;
                  break;
               case CtrlImageStyle.ScaleFit:
               case CtrlImageStyle.ScaleFill:
                  float px = ((float)size.Width / (float)srcImage.Width);
                  float py = ((float)size.Height / (float)srcImage.Height);
                  float p = 0;
                  if (imageStyle == CtrlImageStyle.ScaleFit)
                     p = Math.Min(px, py);
                  else
                     p = Math.Max(px, py);
                  dx = (int)System.Math.Round((p * srcImage.Width));
                  dy = (int)System.Math.Round((p * srcImage.Height));
                  if (dx != 0 && dy != 0)
                  {
                     // create the image with the min size only for the fit style
                     if (imageStyle == CtrlImageStyle.ScaleFit)
                     {
                        dy = Math.Min(size.Height, dy);
                        dx = Math.Min(size.Width, dx);
                     }
                     newSize = new Size(dx, dy);
                  }
                  break;
            }
         }
         return newSize;
      }

      /// <summary> Crops the Image according to the required size. </summary>
      /// <param name="srcImage"> the source image. </param>
      /// <param name="newSize"> the size of the new image. </param>
      /// <returns></returns>
      public static Image CropImage(Image srcImage, Size newSize)
      {
         Image retImage = CreateBlankImage(srcImage, newSize);

         using (Graphics graphics = Graphics.FromImage(retImage))
         {
            Color transparentColor = GetTransparentColor(srcImage);
            if (transparentColor != Color.Empty)
               graphics.Clear(transparentColor);

            var rc = new Rectangle(0, 0, srcImage.Width, srcImage.Height);
            graphics.DrawImage(srcImage, 0, 0, rc, GraphicsUnit.Pixel);
         }

         return retImage;
      }

      /// <summary>
      ///   Create a blank image of the specified size and of SrcImage's PixelFormat.
      /// </summary>
      /// <param name = "srcImage"></param>
      /// <param name = "size"></param>
      /// <returns></returns>
      public static Image CreateBlankImage(Image srcImage, Size size)
      {
         Bitmap retImage;

#if !PocketPC
         PixelFormat pixelFormat = (srcImage).PixelFormat;

         // Graphics.FromImage() throws exception if the image is of one of the following PixelFormats:
         // Format1bppIndexed, Format4bppIndexed, Format8bppIndexed, Undefined, DontCare, Format16bppArgb1555 and Format16bppGrayScale 
         // Refer http://msdn.microsoft.com/en-us/library/system.drawing.graphics.fromimage.aspx
         //
         // So, if the srcImage is in one of these formats we canot use,  
         // new Bitmap(size.Width, size.Height, ((Bitmap)srcImage).PixelFormat);
         // 
         // Hence, create a new image from the source image, create graphics from the 
         // the new image and then clear the graphics using transparent color, if any.
         // If it is a non-transparent image, we need not clear the graphics, because, in any case, 
         // we will be putting new images on it.
         if (pixelFormat == PixelFormat.Format1bppIndexed ||
             pixelFormat == PixelFormat.Format4bppIndexed ||
             pixelFormat == PixelFormat.Format8bppIndexed ||
             pixelFormat == PixelFormat.Undefined ||
             pixelFormat == PixelFormat.DontCare ||
             pixelFormat == PixelFormat.Format16bppArgb1555 ||
             pixelFormat == PixelFormat.Format16bppGrayScale)
         {
            retImage = new Bitmap(size.Width, size.Height);
         }
         else
            retImage = new Bitmap(size.Width, size.Height, pixelFormat);

         retImage.SetResolution(srcImage.HorizontalResolution, srcImage.VerticalResolution);
#else
         retImage = new Bitmap(size.Width, size.Height);
#endif

         return retImage;
      }

      /// <summary> Get the transparent color.
      /// If the image is palletized, it returns the transparent color, else return Empty color.
      /// </summary>
      /// <param name="image"></param>
      /// <returns></returns>
      public static Color GetTransparentColor(Image image)
      {
         Color transparentColor = Color.Empty;

#if !PocketPC
         ColorPalette colorPalette = image.Palette;

         // if the color values in the array contain alpha information,
         // then find out the transparentColor.
         // The following flag values are valid: 
         // 0x00000001 --- The color values in the array contain alpha information. 
         // 0x00000002 --- The colors in the array are grayscale values. 
         // 0x00000004 --- The colors in the array are halftone values. 
         if (colorPalette.Flags == 1)
         {
            Color[] colors = colorPalette.Entries;

            foreach (Color color in colors)
            {
               if (color.A == 0)
               {
                  transparentColor = color;
                  break;
               }
            }
         }
#endif

         return transparentColor;
      }

      /// <summary> Get the image data from a URL of an image file
      /// 
      /// </summary>
      /// <param name="imageListCacheKey">the URL of the image file</param>
      /// <returns> ImageData object or null if loading the image fails</returns>
      public static ImageList LoadImageList(Image image, int singleImageWidth, bool supportsTransparency)
      {
         ImageList imageList = null;

         if (image != null)
         {
            Size singleImageSize = new Size(singleImageWidth, image.Height);
            image = GetMatchImageBySingleImageSize(image, ref singleImageSize);

            //In case of no matching image of singleImageSize then image will be null so checking the null condition.
            if (image != null)
            {
               imageList = new ImageList { ImageSize = singleImageSize };
#if !PocketPC
               imageList.ColorDepth = GetColorDepthFromPixelFormat(image.PixelFormat);
               if (supportsTransparency)
                  imageList.TransparentColor = ((Bitmap)image).GetPixel(0, 0);
#endif
               AddStrip(imageList, image);
            }
         }

         return (imageList);
      }

      /// <summary>
      ///   CF implementation of ImageList.Images.AddStrip
      /// </summary>
      /// <param name = "imageList"></param>
      /// <param name = "image"></param>
      /// <returns></returns>
      private static void AddStrip(ImageList imageList, Image image)
      {
#if !PocketPC
         imageList.Images.AddStrip(image);
#else
         int count = image.Width/imageList.ImageSize.Width;
         for (int a = 0;
              a < count;
              a++)
         {
            Bitmap bm = new Bitmap(imageList.ImageSize.Width, imageList.ImageSize.Height);
            Graphics g = Graphics.FromImage(bm);
            Rectangle srcRect = new Rectangle(a*imageList.ImageSize.Width, 0, imageList.ImageSize.Width,
                                              imageList.ImageSize.Height);
            g.DrawImage(image, 0, 0, srcRect, GraphicsUnit.Pixel);
            imageList.Images.Add(bm);
         }
#endif
      }

      /// <summary>
      /// return the match image according to the singleImageSize
      /// </summary>
      /// <param name="srcImage"></param>
      /// <param name="singleImageWidth"></param>
      /// <returns></returns>
      private static Image GetMatchImageBySingleImageSize(Image srcImage, ref Size singleImageSize)
      {
         Image retImage = srcImage;
         int newImageWidth;
         int singleImageWidth = singleImageSize.Width;
         int singleImageHeight = singleImageSize.Height;
         int numberOfParts = (srcImage.Width / singleImageWidth);
         //Fixed defect # 125837 : If imageWidth < imageHeight then new image width will be 0. So return null because no image will be created of given size.
         if (numberOfParts == 0)
            return null;

         //1. First crop the Image so that it can be divided equally according to the single image width.
         if ((srcImage.Width % singleImageWidth) > 0)
         {
            newImageWidth = numberOfParts * singleImageWidth;
            var newSize = new Size(newImageWidth, singleImageHeight);
            retImage = CropImage(srcImage, newSize);
         }

         //2. .NET limitation, the size of the image list is 256*256.
         //So, resize the image accordingly.
         if (singleImageWidth > DOTNET_MAX_IMAGE_LIST_WIDTH || singleImageHeight > DOTNET_MAX_IMAGE_LIST_HEIGHT)
         {
            int newSingleImageWidth = Math.Min(singleImageWidth, DOTNET_MAX_IMAGE_LIST_WIDTH);
            int newImageHeight = Math.Min(singleImageHeight, DOTNET_MAX_IMAGE_LIST_HEIGHT);

            newImageWidth = numberOfParts * newSingleImageWidth;

            retImage = GetScaledImage(retImage, newImageWidth, newImageHeight);

            singleImageSize.Width = newSingleImageWidth;
            singleImageSize.Height = newImageHeight;
         }

         return retImage;
      }

#if !PocketPC
      /// <summary> Get ColorDepth From PixelFormat. </summary>
      /// <param name="pixelFormat"></param>
      /// <returns>If PixelFormat is 32 bit compatible, return Depth32Bit else return Depth24Bit.</returns>
      private static ColorDepth GetColorDepthFromPixelFormat(PixelFormat pixelFormat)
      {
         ColorDepth colorDepth = ColorDepth.Depth24Bit;

         switch (pixelFormat)
         {
            case PixelFormat.Format32bppRgb:
            case PixelFormat.Format32bppPArgb:
            case PixelFormat.Canonical:
            case PixelFormat.Format32bppArgb:
               colorDepth = ColorDepth.Depth32Bit;
               break;
         }

         return colorDepth;
      }
#endif

      /// <summary>
      /// Get the image from an image file
      /// </summary>
      /// <param name="fileName">the image file name</param>
      /// <param name="loadAsIcon"></param>
      /// <returns> Image or null, if loading the image fails</returns>
      public static Image LoadImage(String fileName)
      {
         Image image = null;
         Stream imageStream = null;

         if (!IsValidImageFileName(ref fileName))
            return null;

         if (ShouldLoadImageFromResource(fileName))
            image = LoadImageFromResource(fileName);
         else
         {
            // load the image from the file system (local / network):
            imageStream = GetStreamFromFile(ref fileName);

            if (imageStream != null)
            {
               try
               {
                  image = FromStream(imageStream);
               }
               catch (Exception e)
               {
                  if (fileName.EndsWith("ICO", StringComparison.OrdinalIgnoreCase))
                  {
                     // exception "Value does not fall within the expected range" occurs if the 
                     // position of the imageStream is not set back to beginning.
                     imageStream.Seek(0, SeekOrigin.Begin);
                     image = GetIconImage(imageStream);
                  }
                  else
                  {
                     if (ImageLoader != null)
                        ImageLoader.HandleException(fileName, e);
                  }
               }
            }
         }

         return (image);
      }

      /// <summary>
      /// Attempts to load file as Icon. If not succeeded, loads it as an image
      /// </summary>
      /// <param name="urlString"></param>
      /// <returns></returns>
      public static Icon LoadImageIcon(string urlString)
      {
         Icon tmpIcon = null;

         try
         {
            tmpIcon = LoadIcon(urlString);
         }
         catch (Exception)
         {
            // QCR# 283870: For images not in ico format
            Image image = ImageUtils.LoadImage(urlString);

            if (image != null)
            {
#if !PocketPC
               // get an HICON for the bitmap
               IntPtr hIcon = ((Bitmap)image).GetHicon();

               // create a new icon from the icon handle
               tmpIcon = Icon.FromHandle(hIcon);
#endif
            }
         }

         return tmpIcon;
      }

      // There is problem for icon file loading (411754)
      // 1) While loading icon, the icon was converted into image which resulted in several problems like image with correct  size was not used.
      // Correct image not shown as icon in titleBar and on Taskbar, etc.
      // 2) Getting icon back from this bitmap gave incorrect or lower quality images
      // To solve this: Ensure icon is loaded as icon and not converted to bitmap and then icon file. This takes care of above problems.

      /// <summary>
      /// Loads Icon from file
      /// </summary>
      /// <param name="fileName"></param>
      /// <returns></returns>
      static Icon LoadIcon(String fileName)
      {
         Icon icon = null;
         Stream iconStream = null;

         if (!IsValidImageFileName(ref fileName))
            return null;

         if (ShouldLoadImageFromResource(fileName))
            icon = LoadIconFromResource(fileName);
         else
         {
            // load the image from the file system (local / network):
            iconStream = GetStreamFromFile(ref fileName);

            if (iconStream != null)
            {
               icon = new Icon(iconStream);
            }
         }

         return (icon);
      }

      /// <summary>
      /// Returns whther to load file from resource or not
      /// </summary>
      /// <param name="fileName"></param>
      /// <returns></returns>
      static bool ShouldLoadImageFromResource(string fileName)
      {
         return fileName.StartsWith("@"); // then this is a file from the resources
      }

      /// <summary>
      /// Returns fileName is valid or not
      /// </summary>
      /// <param name="fileName"></param>
      /// <returns></returns>
      static bool IsValidImageFileName(ref string fileName)
      {
         bool isValidImageFileName = true;
         
         fileName = fileName.Trim();

         if (String.IsNullOrEmpty(fileName))
            isValidImageFileName = false;

         return isValidImageFileName;
      }

      /// <summary>
      /// Read file to Stream
      /// </summary>
      /// <param name="fileName"></param>
      /// <returns></returns>
      static Stream GetStreamFromFile(ref string fileName)
      {
         byte[] content = null;
         Stream stream = null;

         try
         {
            TruncateFileProtocol(ref fileName);
            using (FileStream fis = File.OpenRead(fileName))
            {
               content = new byte[fis.Length];
               fis.Read(content, 0, content.Length);
            }
            stream = new MemoryStream(content);
         }
         catch (Exception e)
         {
            if (ImageLoader != null)
               ImageLoader.HandleException(fileName, e);
         }

         return stream;
      }

      /// <summary> Load image from resource. </summary>
      /// <param name="name"></param>
      /// <param name="loadAsIcon"></param>
      /// <returns></returns>
      private static Image LoadImageFromResource(String name)
      {
         Image image = null;
         string resourceName = name.Substring(1);

         int indexOfDot = resourceName.IndexOf('.');
         if (indexOfDot != -1)
         {
            string resourceFile = resourceName.Substring(0, indexOfDot);
            resourceName = resourceName.Substring(indexOfDot + 1);

            //TODO: load the library and the image from managed assembly as well.
            IntPtr hLib = user32.LoadLibrary(resourceFile);
            IntPtr imgPtr = NativeWindowCommon.LoadImage(hLib, resourceName, NativeWindowCommon.IMAGE_BITMAP, 0, 0, 0);

            if (imgPtr != IntPtr.Zero)
            {
               try
               {
                  image = (Image)(Image.FromHbitmap(imgPtr).Clone());
               }
               finally
               {
                  NativeWindowCommon.DeleteObject(imgPtr);
               }
            }
         }
         else
         {
            if (ImageLoader != null)
               image = (Image)ImageLoader.GetResourceObject(resourceName);
         }

         return image;
      }

      /// <summary>
      /// Loads Icon from Resource
      /// </summary>
      /// <param name="name"></param>
      /// <returns></returns>
      private static Icon LoadIconFromResource(String name)
      {
         string resourceName = name.Substring(1);
         Icon icon = null;

         int indexOfDot = resourceName.IndexOf('.');
         if (indexOfDot != -1)
         {
            string resourceFile = resourceName.Substring(0, indexOfDot);
            resourceName = resourceName.Substring(indexOfDot + 1);

            //TODO: load the library and the image from managed assembly as well.
            IntPtr hLib = user32.LoadLibrary(resourceFile);
            IntPtr imgPtr = NativeWindowCommon.LoadImage(hLib, resourceName, NativeWindowCommon.IMAGE_ICON, 0, 0, 0);

            if (imgPtr != IntPtr.Zero)
            {
               try
               {
                  icon = (Icon)(Icon.FromHandle(imgPtr).Clone());
               }
               finally
               {
                  NativeWindowCommon.DestroyIcon(imgPtr);
               }
            }
         }
         else
         {
            if (ImageLoader != null)
               icon = (Icon)ImageLoader.GetResourceObject(resourceName);
         }

         return icon;
      }

      /// <summary>
      /// truncate the file:// protocol (if exists) from the given file name (file:// is allowed by Window's Files Explorer, but not by the .Net Framework ....)
      /// </summary>
      /// <param name="fileName"></param>
      private static void TruncateFileProtocol(ref string fileName)
      {
         if (fileName.StartsWith(Constants.FILE_PROTOCOL))
            fileName = fileName.Substring(Constants.FILE_PROTOCOL.Length);
      }

      /// <summary>
      /// CF implementation of Image.FromStream
      /// </summary>
      /// <param name = "stream"></param>
      /// <returns></returns>
      public static Image FromStream(Stream stream)
      {
#if !PocketPC
         return Image.FromStream(stream);
#else
         return new Bitmap(stream);
#endif
      }

      /// <summary>
      ///   Function create the bitmap Image from imagestream passed to it
      /// </summary>
      /// <param name = "imageStream">input Image stream</param>
      /// <returns>Image created from input stream</returns>
      private static Image GetIconImage(Stream imageStream)
      {
         Bitmap bitmap = null;

         try
         {
            var icon = new Icon(imageStream);
            bitmap = new Bitmap(icon.Width, icon.Height);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
               g.DrawIcon(icon, 0, 0);
            }
         }
         catch (Exception e)
         {
            if (ImageLoader != null)
               ImageLoader.HandleException(string.Empty, e);
         }

         return bitmap;
      }
   }
}
