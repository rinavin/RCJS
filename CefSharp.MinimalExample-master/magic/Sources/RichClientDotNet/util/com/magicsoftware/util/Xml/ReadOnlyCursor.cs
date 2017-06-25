using System;
using System.Collections.Generic;
using System.Text;

namespace com.magicsoftware.util.Xml
{
   /// <summary>
   /// A wrapper over the XmlParserCursor, to expose an internal cursor with
   /// an unmodifiable interface.
   /// </summary>
   class ReadOnlyCursor
   {
      XmlParserCursor cursor;

      public ReadOnlyCursor(XmlParserCursor cursor)
      {
         this.cursor = cursor;
      }

      public int StartPosition { get { return cursor.StartPosition; } }

      public int EndPosition { get { return cursor.EndPosition; } }

      public int Span { get { return cursor.Span; } }

      public bool IsValid { get { return cursor.IsValid; } }
   }
}
