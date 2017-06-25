using com.magicsoftware.util;
using System;
using System.Drawing;
using static com.magicsoftware.win32.NativeWindowCommon;

namespace Controls.com.magicsoftware.support
{
   /// <summary>
   /// Base class for different font types (System.Drawing.Font and LogFont)
   /// </summary>
   public class FontDescription
   {
      FontHandleContainer fontHandleContainer; //Saved in order that the GC will not release the handle.

      /// <summary>
      /// Logical font represents font
      /// </summary>
      public LOGFONT LogFont { get; set; } 

      /// <summary>
      /// Gets Hfont ptr from font
      /// </summary>
      /// <param name="font"></param>
      /// <returns></returns>
      public IntPtr FontHandle
      {
         get { return fontHandleContainer.FontHandle; }
      }

      public FontDescription(Font font)
      {
         LogFont = new LOGFONT();
         font.ToLogFont(LogFont);
         fontHandleContainer = FontHandlesCache.GetInstance().Get(font);
      }

      public FontDescription(LOGFONT logFont)
      {
         this.LogFont = logFont;
         fontHandleContainer = FontHandlesCache.GetInstance().Get(logFont);
      }
   }
}
