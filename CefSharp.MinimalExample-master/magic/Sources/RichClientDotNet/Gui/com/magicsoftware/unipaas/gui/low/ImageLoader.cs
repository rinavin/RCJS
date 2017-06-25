using System;
using System.Drawing;
using System.IO;
using com.magicsoftware.controls;
using com.magicsoftware.support;

namespace com.magicsoftware.unipaas.gui.low
{
   /// <summary>
   ///   The class is used to load the images.
   /// </summary>
   internal class ImageLoader : IImageLoader
   {
      #region IImageLoader Members

      /// <summary>
      /// Get Resource Object
      /// </summary>
      /// <param name="resourceName"></param>
      public Object GetResourceObject(string resourceName)
      {
         return Events.GetResourceObject(resourceName);
      }

      /// <summary>
      /// Handle an Exception
      /// </summary>
      /// <param name="urlString"></param>
      /// <param name="e"></param>
      public void HandleException(string urlString, Exception e)
      {
         if (e is DirectoryNotFoundException || e is FileNotFoundException)
            Events.WriteWarningToLog(string.Format("File \"{0}\" wasn't found.", urlString));
         else
            Events.WriteExceptionToLog(string.Format("\"{0}\":\n{1}", urlString, e.StackTrace));
      }

      #endregion

      /// <summary>
      /// 
      /// </summary>
      /// <param name="fileName"></param>
      /// <param name="loadAsIcon"></param>
      /// <returns></returns>
      internal static Image GetImage(String fileName)
      {
         Image image = ImagesCache.GetInstance().Get(fileName);

         if (image == null)
         {
            image = ImageUtils.LoadImage(fileName);

            if (image != null)
            {
               bool putInCache = true;
#if !PocketPC
               putInCache = !ImageAnimator.CanAnimate(image);
#endif
               if (putInCache)
                  ImagesCache.GetInstance().Put(fileName, image);
            }
         }

         return image;
      }
   }
}