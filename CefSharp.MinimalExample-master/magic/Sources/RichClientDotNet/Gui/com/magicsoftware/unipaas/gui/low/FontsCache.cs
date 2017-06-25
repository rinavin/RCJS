using System.Drawing;
using com.magicsoftware.unipaas.env;
using com.magicsoftware.util;

namespace com.magicsoftware.unipaas.gui.low
{
   /// <summary> 
   /// Cache Gui Fonts. Singleton class
   /// </summary>
   internal class FontsCache : ResourcesCache<MgFont, Font>
   {
      private static FontsCache _instance;

      /// <summary> private CTOR</summary>
      private FontsCache()
      {
      }

      /// <returns> the single instance of the fonts cache object</returns>
      internal static FontsCache GetInstance()
      {
         if (_instance == null)
         {
            // synchronize on the class object
            lock (typeof (FontsCache))
            {
               if (_instance == null)
                  _instance = new FontsCache();
            }
         }
         return _instance;
      }

      /// <summary>
      /// Translate magic font style to Gui font style
      /// </summary>
      /// <param name="mgFont"></param>
      /// <returns></returns>
      internal static FontStyle GetFontStyle(MgFont mgFont)
      {
         FontStyle guiFontStyle = FontStyle.Regular;

         if (mgFont.Italic)
            guiFontStyle |= FontStyle.Italic;
         if (mgFont.Strikethrough)
            guiFontStyle |= FontStyle.Strikeout;
         if (mgFont.Underline)
            guiFontStyle |= FontStyle.Underline;
         if (mgFont.Bold)
            guiFontStyle |= FontStyle.Bold;

         return guiFontStyle;
      }

      /// <summary>
      /// </summary>
      /// <param name="mgFont"></param>
      /// <returns></returns>
      protected override Font CreateInstance(MgFont mgFont)
      {
         return (new Font(mgFont.TypeFace, mgFont.Height, GetFontStyle(mgFont)));
      }
   }
}