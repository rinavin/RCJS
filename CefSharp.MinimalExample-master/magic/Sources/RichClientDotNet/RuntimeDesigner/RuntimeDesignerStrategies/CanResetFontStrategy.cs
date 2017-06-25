using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using com.magicsoftware.win32;

namespace RuntimeDesigner.RuntimeDesignerStrategies
{
   /// <summary>
   /// strategy for the runtime designer property descriptor CanReset method, for the font property on LinkLabel
   /// </summary>
   class CanResetFontStrategy : ICanResetStrategy
   {
      /// <summary>
      /// 
      /// </summary>
      /// <param name="defaultValue"></param>
      /// <param name="value"></param>
      /// <returns></returns>
      public bool CanResetData(object defaultValue, object value)
      {
         return !FontEqualNoUnderline((Font)defaultValue, (Font)value);
      }

      /// <summary>
      /// "Equals" for fonts, ignoring the underline difference
      /// </summary>
      /// <param name="font1"></param>
      /// <param name="font2"></param>
      /// <returns></returns>
      bool FontEqualNoUnderline(Font font1, Font font2)
      {
         if (!font1.FontFamily.Equals(font2.FontFamily) ||
            font1.GdiVerticalFont != font2.GdiVerticalFont ||
            font1.GdiCharSet != font2.GdiCharSet ||
            font1.Size != font2.Size ||
            font1.Unit != font2.Unit)
            return false;

         return (font1.Style | FontStyle.Underline) == (font2.Style | FontStyle.Underline);
      }
   }
}
