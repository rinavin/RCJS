using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using com.magicsoftware.util;

namespace com.magicsoftware.unipaas.gui.low
{
   /// <summary> Cache images according to their file URL. It limits the cache size by either the data size or the number of
   /// images.
   /// 
   /// </summary>
   /// <author>  ehudm</author>
   public class ImagesCache : ResourcesCache<String, Image>
   {
      // instance variables

      // constants
      internal const long MAX_CACHE_SIZE_KB = Int32.MaxValue;
      internal const int MAX_CACHE_SIZE_IMAGES = Int32.MaxValue;
      private static ImagesCache _instance;
      private readonly LeastRecentlyUsed _lru;
      private readonly long _totalCacheSizeBytes;

      /// <summary> CTOR</summary>
      private ImagesCache()
      {
         _lru = new LeastRecentlyUsed();
         _totalCacheSizeBytes = 0;
      }

      /// <returns> the single instance of the images cache object </returns>
      public static ImagesCache GetInstance()
      {
         if (_instance == null)
         {
            // synchronize on the class object
            lock (typeof (ImagesCache))
            {
               if (_instance == null)
                  _instance = new ImagesCache();
            }
         }
         return _instance;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="urlStr"></param>
      /// <param name="image"></param>
      public override void Put(String urlStr, Image image)
      {
         lock (this)
         {
            base.Put(urlStr, image);

            if (image != null)
            {
               //totalCacheSizeBytes += image.getSizeInBytes();
               ApplyCacheLimitations();
            }
         }
      }

      /// <summary> Returns the image according to the URL of the image file. If the image is not in the cache then it first
      /// create it and add it to the cache.</summary>
      /// <param name="urlStr">the URL of the image file</param>
      /// <returns> the requested Image</returns>
      public override Image Get(String urlStr)
      {
         lock (this)
         {
            var image = base.Get(urlStr);
            _lru.Used(urlStr);

            return image;
         }
      }

      /// <summary>
      /// </summary>
      /// <param name="urlStr"></param>
      /// <returns></returns>
      protected override Image CreateInstance(String urlStr)
      {
         return null;
      }

      /// <summary> If the cache limitations are violated then remove the recently used images until the cache limitations
      /// are satisfied
      /// </summary>
      private void ApplyCacheLimitations()
      {
         while (Count() > MAX_CACHE_SIZE_IMAGES || _totalCacheSizeBytes > MAX_CACHE_SIZE_KB*1024)
         {
            // get the url of the image that was least recently used         
            String lruImageUrl = _lru.Remove();
            if (lruImageUrl == null)
            {
               // the cache limitations are violated but there are no images to remove from the cache
               Debug.Assert(false);
            }

            // remove the least recently used image from the cache
            Remove(lruImageUrl);
         }
      }

      #region Nested type: LeastRecentlyUsed

      /// <summary> implement a mechanism of Least Recently Used
      /// </summary>
      private class LeastRecentlyUsed
      {
         private readonly List<String> _list = new List<String>();

         /// <summary> Declares that an object was recently used</summary>
         /// <param name="obj">the object that was recently used</param>
         internal void Used(String obj)
         {
            lock (this)
            {
               // remove the object from the list and add it again as the last object
               _list.Remove(obj);
               _list.Add(obj);
            }
         }

         /// <summary> Returns the least recently used object and removes it from the list of used objects</summary>
         /// <returns> Object</returns>
         internal String Remove()
         {
            lock (this)
            {
               String obj = null;
               if (_list.Count > 0)
               {
                  obj = _list[0];
                  _list.RemoveAt(0);
               }
               return obj;
            }
         }
      }

      #endregion
   }
}