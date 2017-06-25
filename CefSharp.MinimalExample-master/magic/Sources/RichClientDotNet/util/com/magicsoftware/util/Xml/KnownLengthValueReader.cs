using System;
using System.Collections.Generic;
using System.Text;

namespace com.magicsoftware.util.Xml
{
   public class KnownLengthValueReader : IAttributeValueReader
   {
      public int ValueLength { get; set; }

      public bool GetValueExtents(string tagString, int valueStartPosition, ref XmlParserCursor cursor)
      {
         if (ValueLength == 0)
            return false;

         // The values are assumed to be quoted, so start position is valueStartPosition + 1.
         cursor.StartPosition = valueStartPosition + 1;
         cursor.Span = ValueLength;
         ValueLength = 0;
         return true;
      }
   }

}
