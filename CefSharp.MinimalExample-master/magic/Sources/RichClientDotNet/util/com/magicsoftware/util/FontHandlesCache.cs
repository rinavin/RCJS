using System;
using System.Drawing;
using com.magicsoftware.win32;
using System.Runtime.InteropServices;
using static com.magicsoftware.win32.NativeWindowCommon;

namespace com.magicsoftware.util
{
   /// <summary>
   /// cache for win32 font's handles, need because performance of the Font.ToHfont() is poor
   /// </summary>
   public class FontHandlesCache : ResourcesCache<LogFontKey, FontHandleContainer>
   {

      private static FontHandlesCache _instance;

      /// <summary>
      /// 
      /// </summary>
      private FontHandlesCache()
      {
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      public static FontHandlesCache GetInstance()
      {
         if (_instance == null)
         {
            // synchronize on the class object
            lock (typeof(FontHandlesCache))
            {
               if (_instance == null)
                  _instance = new FontHandlesCache();
            }
         }
         return _instance;
      }
      protected override FontHandleContainer CreateInstance(LogFontKey key)
      {
         return new FontHandleContainer(key.Logfont);
      }

      public FontHandleContainer Get(NativeWindowCommon.LOGFONT logfont)
      {
         LogFontKey logFontKey = new LogFontKey(logfont);
         return base.Get(logFontKey);
      }

      public FontHandleContainer Get(Font key)
      {
         LogFontKey logFontKey = new LogFontKey(key);
         return base.Get(logFontKey);
      }

   }

   /// <summary>
   /// key for the cache
   /// </summary>
   public class LogFontKey
   {
      NativeWindowCommon.LOGFONT logfont;
      public NativeWindowCommon.LOGFONT Logfont
      {
         get { return logfont; }
      }
      public LogFontKey(Font key)
      {
         logfont = new NativeWindowCommon.LOGFONT();
         key.ToLogFont(logfont);
      }

      public LogFontKey(NativeWindowCommon.LOGFONT logfont)
      {
         this.logfont = logfont;         
      }
      public override int GetHashCode()
      {
         var hashBuilder = new HashCodeBuilder();
         hashBuilder.Append(logfont.lfWidth);
         hashBuilder.Append(logfont.lfEscapement);
         hashBuilder.Append(logfont.lfOrientation);
         hashBuilder.Append(logfont.lfWeight);
         hashBuilder.Append(logfont.lfItalic);
         hashBuilder.Append(logfont.lfUnderline);
         hashBuilder.Append(logfont.lfStrikeOut);
         hashBuilder.Append(logfont.lfCharSet);
         hashBuilder.Append(logfont.lfClipPrecision);
         hashBuilder.Append(logfont.lfClipPrecision);
         hashBuilder.Append(logfont.lfQuality);
         hashBuilder.Append(logfont.lfPitchAndFamily);
         hashBuilder.Append(logfont.lfFaceName);
         hashBuilder.Append(logfont.lfHeight);
         return hashBuilder.HashCode;
      }

      public override bool Equals(object obj)
      {
         LogFontKey key = obj as LogFontKey;
         if (key != null)
         {
            if (key.logfont.lfFaceName.Equals(logfont.lfFaceName) &&
               key.logfont.lfWidth == logfont.lfWidth &&
               key.logfont.lfEscapement == logfont.lfEscapement &&
               key.logfont.lfOrientation == logfont.lfOrientation &&
               key.logfont.lfWeight == logfont.lfWeight &&
               key.logfont.lfItalic == logfont.lfItalic &&
               key.logfont.lfUnderline == logfont.lfUnderline &&
               key.logfont.lfStrikeOut == logfont.lfStrikeOut &&
               key.logfont.lfCharSet == logfont.lfCharSet &&
               key.logfont.lfClipPrecision == logfont.lfClipPrecision &&
               key.logfont.lfQuality == logfont.lfQuality &&
               key.logfont.lfPitchAndFamily == logfont.lfPitchAndFamily &&
               key.logfont.lfHeight == logfont.lfHeight)
               return true;
         }
         return false;

      }
   }

   


   /// <summary>
   /// wrapper class for font handle
   /// </summary>
   public class FontHandleContainer : MarshalByRefObject, IDisposable
   {
      #region fields

      IntPtr nativeFontHandle;

      #endregion
      #region properties
      public IntPtr FontHandle
      {
         get { return nativeFontHandle; }
      }
      #endregion

      #region ctors
      public FontHandleContainer(LOGFONT key)
      {
         nativeFontHandle = NativeWindowCommon.CreateFontIndirect(key);
      }
      #endregion

      #region methods
      public void Dispose()
      {
         this.Dispose(true);
         GC.SuppressFinalize(this);
      }
      [DllImport("gdi32.dll", CharSet = CharSet.Auto)]
      public static extern bool DeleteObject(IntPtr hObject);

       protected virtual void Dispose(bool disposing)
        {
           if (this.nativeFontHandle != IntPtr.Zero)
            {
                try
                {
                   //We can not reference MgNative dll here, it causes crash when dropping MgControls on VS designer .
                   //So we call it directly.
                   DeleteObject(nativeFontHandle);
                }
                catch (Exception)
                {
                    
                }
                finally
                {
                   this.nativeFontHandle = IntPtr.Zero;
                }
            }
        }

       ~FontHandleContainer()
        {
            this.Dispose(false);
        }
      #endregion
   }


}
