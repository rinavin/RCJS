using System;
using System.Collections.Generic;
using System.Text;

namespace com.magicsoftware.util.Xml
{
   static class XmlParserStringUtils
   {
      /// <summary>
      /// Puts an XmlParserCursor information into a string, so that the cursor's position can be
      /// viewed in debug watch list.
      /// </summary>
      /// <param name="original">The original string, to which the cursors apply.</param>
      /// <param name="cursors">A set of cursors applied on the list.</param>
      /// <returns></returns>
      public static string AddCursorsInfo(string original, params XmlParserCursor[] cursors)
      {
#if !PocketPC
         List<XmlParserCursor> cursorSet = new List<XmlParserCursor>(cursors);
         cursorSet.Sort(new CursorComparer());
         StringBuilder result = new StringBuilder();
         XmlParserCursor lastCursor = new XmlParserCursor();
         foreach (var cursor in cursorSet)
         {
            if (cursor.IsValid)
            {
               result.Append(original.Substring(lastCursor.StartPosition, cursor.StartPosition - lastCursor.StartPosition));
               result.AppendFormat("|-{{{0}}}-|", cursor);
               lastCursor = cursor;
            }
         }
         result.Append(original.Substring(lastCursor.StartPosition, original.Length - lastCursor.StartPosition));
         return result.ToString();
#else
         return original;
#endif
      }

      /// <summary>unscape from:
      /// {"&amp;",\\, \q, \o, \l, \g, \e, \\r, \\n}, to:
      /// {"&",     \,  ",  ',  <,  >,  =,  \r,  \n}
      /// <param name="str">String to be converted</param>
      /// <returns>unescaped string</returns>
      public static String Unescape(String str)
      {
         StringBuilder unescapedString = new StringBuilder(str.Length);

         for (int i = 0; i < str.Length; i++)
         {
            if (str[i] != '\\')
            {
               unescapedString.Append(str[i]);
               continue;
            }

            switch (str[++i])
            {
               case 'q':
                  unescapedString.Append('\"');
                  break;
               case 'o':
                  unescapedString.Append('\'');
                  break;
               case 'l':
                  unescapedString.Append('<');
                  break;
               case 'g':
                  unescapedString.Append('>');
                  break;
               case 'e':
                  unescapedString.Append('=');
                  break;
               case 'r':
                  unescapedString.Append('\r');
                  break;
               case 'n':
                  unescapedString.Append('\n');
                  break;
               default:
                  unescapedString.Append(str[i]);
                  break;
            }
         }

         return (unescapedString.ToString());
      }

   }

   /// <summary>
   /// Utility to compare cursors in the sorted list used in 'AddCursorsInfo'.
   /// </summary>
   class CursorComparer : IComparer<XmlParserCursor>
   {
      #region IComparer<XmlParserCursor> Members

      public int Compare(XmlParserCursor x, XmlParserCursor y)
      {
         int result = x.StartPosition.CompareTo(y.StartPosition);
         if (result != 0)
            return result;

         return x.EndPosition.CompareTo(y.EndPosition);
      }

      #endregion
   }

}
