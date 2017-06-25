using System;

namespace com.magicsoftware.controls
{
   /// <summary>
   /// This interface is responsible for providing image information.
   /// </summary>
   public interface IImageLoader
   {
      /// <summary>
      /// Handle an Exception
      /// </summary>
      /// <param name="urlString"></param>
      /// <param name="e"></param>
      void HandleException(string urlString, Exception e);

      /// <summary>
      /// Get Resource Object
      /// </summary>
      /// <param name="resourceName"></param>
      Object GetResourceObject(string resourceName);
   }
}
