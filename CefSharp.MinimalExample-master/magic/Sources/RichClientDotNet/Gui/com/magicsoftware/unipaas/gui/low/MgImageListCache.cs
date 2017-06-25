using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.util;
using com.magicsoftware.support;
using System.Drawing;
using System.Diagnostics;

namespace com.magicsoftware.unipaas.gui.low
{
   /// <summary> Cache images list according to ImageListCacheKey (i.e. file name and no. of parts).
   /// 
   /// </summary>
   /// <author>  Kaushal
   /// </author>
   public class MgImageListCache : ResourcesCache<MgImageListCacheKey, MgImageList>
   {
      // static members
      private static MgImageListCache _instance;

      /// <summary>
      /// </summary>
      private MgImageListCache()
      {
      }

      /// <returns> the single instance of the images cache object
      /// </returns>
      public static MgImageListCache GetInstance()
      {
         if (_instance == null)
         {
            // synchronize on the class object
            lock (typeof(ImageListCache))
            {
               if (_instance == null)
                  _instance = new MgImageListCache();
            }
         }
         return _instance;
      }

      /// <summary>
      /// </summary>
      /// <param name="imageListCacheKey"></param>
      /// <returns></returns>
      protected override MgImageList CreateInstance(MgImageListCacheKey imageListCacheKey)
      {
         // create the image and cache it         
         MgImageList imageList = LoadImageList(imageListCacheKey);

         return imageList;
      }

      /// <summary> Get the image data from a URL of an image file
      /// 
      /// </summary>
      /// <param name="imageListCacheKey">the URL of the image file</param>
      /// <returns> ImageData object or null if loading the image fails</returns>
      private MgImageList LoadImageList(MgImageListCacheKey imageListCacheKey)
      {
         MgImageList imageList = null;

         if (!String.IsNullOrEmpty(imageListCacheKey.imageListFileName))
         {
            Image image = ImageLoader.GetImage(imageListCacheKey.imageListFileName);

            imageList = MgImageList.LoadImageList(image, imageListCacheKey.numberOfImages);
         }

         return (imageList);
      }
   }
}
