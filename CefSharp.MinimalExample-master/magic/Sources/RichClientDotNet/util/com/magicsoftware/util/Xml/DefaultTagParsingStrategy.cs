using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace com.magicsoftware.util.Xml
{
   class DefaultTagParsingStrategy : IXmlTagParsingStrategy
   {
      public static readonly IXmlTagParsingStrategy Instance = new DefaultTagParsingStrategy();

      /// <summary>
      /// Finds the end of the tag pointed to by the cursor, and updates the cursor's
      /// end position to point to the end of the tag.
      /// </summary>
      /// <param name="cursor">A cursor initialized so its start position is at the beginning of a tag.</param>
      /// <returns>Method returns true if the tag's end was found, false otherwise.</returns>
      public bool FindEndOfTag(string xml, XmlParserCursor cursor)
      {
         Debug.Assert(xml[cursor.StartPosition] == '<', "The cursor is not positioned on the beginning of tag.");
         int endPosition = xml.IndexOf('>', cursor.StartPosition);
         if (endPosition < 0)
            return false;
         cursor.EndPosition = endPosition + 1;
         return true;
      }
   }
}
