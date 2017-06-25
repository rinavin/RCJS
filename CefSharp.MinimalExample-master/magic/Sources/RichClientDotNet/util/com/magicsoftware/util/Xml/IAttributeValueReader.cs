using System;
using System.Collections.Generic;
using System.Text;

namespace com.magicsoftware.util.Xml
{
   public interface IAttributeValueReader
   {
      /// <summary>
      /// The implementing method should calculate the start and end position of the attribute value
      /// within 'tagString'.
      /// </summary>
      /// <param name="tagString">The string containing the attribute. All positions are relative to this string.</param>
      /// <param name="valueStartPosition">The position of the beginning of the value, skipping all whitespaces after the '=' symbol.</param>
      /// <param name="cursor"></param>
      /// <returns>The method should return a cursor whose StartPosition is the beginning of the actual value and whose Span is 
      /// the length of the value, excluding quotes, or any character surrounding the actual value.
      /// IF the method fails, it should return an invalidated cursor.</returns>
      bool GetValueExtents(string tagString, int valueStartPosition, ref XmlParserCursor cursor);
   }
}
