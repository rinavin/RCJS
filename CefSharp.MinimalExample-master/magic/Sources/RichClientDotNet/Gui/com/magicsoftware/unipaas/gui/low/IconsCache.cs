using System;
using System.Drawing;
using com.magicsoftware.win32;
using System.IO;
using com.magicsoftware.util;
using com.magicsoftware.support;

namespace com.magicsoftware.unipaas.gui.low
{
   /// <summary>
   ///   Cache icons according to their file URL.
   /// </summary>
   internal class IconsCache : ResourcesCache<String, Icon>
   {
      // static members
      private static IconsCache _instance;

      /// <summary>
      ///   CTOR
      /// </summary>
      private IconsCache()
      {
      }

      /// <returns> the single instance of the icons cache object</returns>
      internal static IconsCache GetInstance()
      {
         if (_instance == null)
         {
            // synchronize on the class object
            lock (typeof (IconsCache))
            {
               if (_instance == null)
                  _instance = new IconsCache();
            }
         }
         return _instance;
      }

      /// <summary>
      /// </summary>
      /// <param name = "urlStr"></param>
      /// <returns></returns>
      protected override Icon CreateInstance(String urlStr)
      {
         // create the icon and cache it
         Icon icon = LoadIcon(urlStr);

         return icon;
      }

      /// <summary>
      ///   Load the icon from a url
      /// </summary>
      /// <param name = "urlString">the URL of the icon file</param>
      /// <returns>If succeeds returns icon object, otherwise null.</returns>
      private Icon LoadIcon(String urlString)
      {
         Icon icon = null;
         Icon tmpIcon = ImageUtils.LoadImageIcon(urlString);

         if (tmpIcon != null)
         {
            icon = (Icon) tmpIcon.Clone();
         }
         // seems this code is no longer needed and seems redundant
         //else
         //{
         //   try
         //   {
         //      icon = new Icon(urlString);
         //   }
         //   catch (DirectoryNotFoundException)
         //   {
         //      Events.WriteWarningToLog(string.Format("File \"{0}\" wasn't found.", urlString));
         //   }
         //   catch (FileNotFoundException)
         //   {
         //      Events.WriteWarningToLog(string.Format("File \"{0}\" wasn't found.", urlString));
         //   }
         //   catch (Exception e)
         //   {
         //      Events.WriteErrorToLog(string.Format("{0}\n{1}", urlString, e.StackTrace));
         //   }
         //}

         return (icon);
      }
   }
}