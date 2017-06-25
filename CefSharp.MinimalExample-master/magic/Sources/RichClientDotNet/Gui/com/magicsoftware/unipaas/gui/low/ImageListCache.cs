using System;
using System.Drawing;
using System.Windows.Forms;
using com.magicsoftware.support;
using com.magicsoftware.util;

namespace com.magicsoftware.unipaas.gui.low
{
   /// <summary> Cache images list according to their file URL. It limits the cache size by either the data size or the number of
   /// images.
   /// 
   /// </summary>
   /// <author>  Rinat
   /// </author>
   public class ImageListCache : ResourcesCache<String, ImageList>
   {
      // static members
      private static ImageListCache _instance;
      private const int TOOL_WIDTH = 16;

      /// <summary>
      /// </summary>
      private ImageListCache()
      {
      }

      /// <returns> the single instance of the images cache object
      /// </returns>
      public static ImageListCache GetInstance()
      {
         if (_instance == null)
         {
            // synchronize on the class object
            lock (typeof (ImageListCache))
            {
               if (_instance == null)
                  _instance = new ImageListCache();
            }
         }
         return _instance;
      }

      /// <summary>
      /// </summary>
      /// <param name="imageListFileName"></param>
      /// <returns></returns>
      protected override ImageList CreateInstance(String imageListFileName)
      {
         // create the image and cache it         
         ImageList imageList = LoadImageList(imageListFileName);

         return imageList;
      }

      /// <summary> Get the image data from a URL of an image file
      /// 
      /// </summary>
      /// <param name="imageListFileName">the URL of the image file</param>
      /// <returns> ImageData object or null if loading the image fails</returns>
      private ImageList LoadImageList(String imageListFileName)
      {
         ImageList imageList = null;

         if (!String.IsNullOrEmpty(imageListFileName))
         {
            Image image = ImageLoader.GetImage(imageListFileName);

            if (image != null)
               imageList = ImageUtils.LoadImageList(image, TOOL_WIDTH, true);
         }

         return (imageList);
      }
   }
}