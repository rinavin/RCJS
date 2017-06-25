using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace com.magicsoftware.util.Xml
{
   /// <summary>
   /// Xml tag attributes parser. The parser is implemented as IEnumerable of XmlAttribute:
   /// Once you obtain an attributes parser, you need to use a while loop on the parser's 
   /// 'MoveNext()' method to get the attributes, as described in the example.
   /// </summary>
   /// <example>
   /// var attrsParser = get attributes parser...
   /// while (attrsParser.MoveNext())
   /// {
   ///    attrName = attrsParser.Current.Key;
   ///    attrValue = attrsParser.Current.Value;
   /// }
   /// </example>
   public class AttributesParser : IEnumerator<XmlAttribute>
   {
      Dictionary<string, string> attrs = new Dictionary<string, string>();

      string tagString;
      XmlParserCursor nameCursor;
      XmlParserCursor valueCursor;
      XmlAttribute currentAttribute;
      IAttributeValueReader valueReader;

      public AttributesParser(string tagString, IAttributeValueReader[] prependedValueReaders)
      {
         this.tagString = tagString;
         nameCursor = new XmlParserCursor();
         valueCursor = new XmlParserCursor();
         valueReader = new CacadedValueReaders(prependedValueReaders);
         Reset();
      }

      #region IEnumerator Members

      object System.Collections.IEnumerator.Current
      {
         get { return Current; }
      }

      public XmlAttribute Current
      {
         get
         {
            if (currentAttribute == null)
            {
               if (!nameCursor.IsValid || !valueCursor.IsValid)
                  currentAttribute = new XmlAttribute(null, null);
               else
                  currentAttribute = new XmlAttribute(GetAttributeName(), GetAttributeValue());
            }
            return currentAttribute;
         }
      }

      public bool MoveNext()
      {
         if (!(nameCursor.IsValid && valueCursor.IsValid))
            return false;

         currentAttribute = null;

         int currentIndex = valueCursor.EndPosition;
         if (tagString[currentIndex] == '"')
            currentIndex++;
         currentIndex = SkipWhitespace(currentIndex);
         if (currentIndex >= tagString.Length)
            return Invalidate();

         nameCursor.StartPosition = currentIndex;

         // CurrentIndex is at the beginning of an attribute name --> Find the '=' symbol
         // to know where the name ends.
         currentIndex = tagString.IndexOf('=', currentIndex);
         if (currentIndex < 0)
            return Invalidate();

         nameCursor.EndPosition = currentIndex;

         // Move to the position after the '=' symbol.
         currentIndex = SkipWhitespace(currentIndex + 1);

         if (!valueReader.GetValueExtents(tagString, currentIndex, ref valueCursor))
            return Invalidate();

         return true;
      }

      public void Reset()
      {
         currentAttribute = null;

         nameCursor.Reset();
         valueCursor.Reset();

         // Skip any whitespace that may be before the actual beginning of the tag.
         int currentIndex = SkipWhitespace(0);

         // Skip the tag name to the first attribute.
         currentIndex = tagString.IndexOfAny(XmlParserConstants.WhitespaceChars, currentIndex);

         // May the element does not have attributes. Just skip to the end.
         if (currentIndex == -1)
            currentIndex = tagString.Length;

         currentIndex = SkipWhitespace(currentIndex);

         nameCursor.StartPosition = currentIndex;
         valueCursor.StartPosition = currentIndex;

         // At this point, if there are no attributes the cursors are positioned 
         // at the end of the tag string --> Invalidate the enumerator.
         if (nameCursor.StartPosition == tagString.Length)
            Invalidate();
      }

      #endregion

      private bool Invalidate()
      {
         nameCursor.Invalidate();
         valueCursor.Invalidate();
         return false;
      }

      private string GetAttributeName()
      {
         return tagString.Substring(nameCursor.StartPosition, nameCursor.Span);
      }

      private string GetAttributeValue()
      {
         return tagString.Substring(valueCursor.StartPosition, valueCursor.Span);
      }


      /// <summary>
      /// Finds the first non-whitespace character, within 'tagString', starting at 'startPosition'.
      /// </summary>
      /// <param name="startPosition">The position from which to start looking for non-whitespace characters.</param>
      int SkipWhitespace(int startPosition)
      {
         int currentIndex = startPosition;
         while (currentIndex < tagString.Length && Char.IsWhiteSpace(tagString, currentIndex))
            currentIndex++;
         return currentIndex;
      }

      #region IDisposable Members

      public void Dispose()
      {
         throw new NotImplementedException();
      }

      #endregion

      public override string ToString()
      {
         return XmlParserStringUtils.AddCursorsInfo(tagString, nameCursor, valueCursor);
      }
   }

   /// <summary>
   /// Implements the IAttributeParsingStrategy to parse an unquoted value.
   /// </summary>
   class UnquotedValueReader : IAttributeValueReader
   {
      public bool GetValueExtents(string tagString, int valueStartPosition, ref XmlParserCursor cursor)
      {
         cursor = null;

         int valueEndPos = tagString.IndexOfAny(XmlParserConstants.WhitespaceChars, valueStartPosition + 1);
         if (valueEndPos == -1)
            return false;

         cursor = new XmlParserCursor();
         cursor.StartPosition = valueStartPosition;
         cursor.EndPosition = valueEndPos;
         return true;
      }
   }


   /// <summary>
   /// Implements the IAttributeParsingStrategy to parse a quoted value.
   /// </summary>
   class QuotedValueReader : IAttributeValueReader
   {
      public bool GetValueExtents(string tagString, int valueStartPosition, ref XmlParserCursor cursor)
      {
         if (tagString[valueStartPosition] != '"')
            cursor.Invalidate();
         else
         {
            // Skip the opening quote.
            cursor.StartPosition = valueStartPosition + 1;

            // The attribute value starts with '"'. The returned string will be unquoted.
            int valueEndPos = FindExpectedChar(tagString, '"', cursor.StartPosition + 1);

            while (valueEndPos != -1)
            {
               bool isEscaped = false;
               int testChar = valueEndPos - 1;
               while (testChar >= 0 && tagString[testChar] == '\\')
               {
                  isEscaped = !isEscaped;
                  testChar--;
               }
               // If the quote is escaped with '\\', keep on searching for an non-escaped quote.
               if (isEscaped)
                  valueEndPos = FindExpectedChar(tagString, '"', valueEndPos + 1);
               else
                  break;
            }
            if (valueEndPos == -1)
               cursor.Invalidate();
            else
               cursor.EndPosition = valueEndPos;
         }
         
         return cursor.IsValid;
      }

      /// <summary>
      /// Attempts to find character 'ch' from the position specified by startFromIndex. If the
      /// character is not found the method will raise an assertion error in debug.
      /// </summary>
      /// <param name="ch"></param>
      /// <param name="startFromIndex"></param>
      /// <returns></returns>
      int FindExpectedChar(string tagString, char ch, int startFromIndex)
      {
         int charPosition = tagString.IndexOf(ch, startFromIndex);
         if (charPosition == -1)
         {
            string elementTagWithMarker = tagString;
            elementTagWithMarker = elementTagWithMarker.Insert(startFromIndex, "|->");
            Debug.Assert(false, "Expected character not found",
            String.Format("The character '{0}' was not found in '{1}' when searching from position {2}", ch, elementTagWithMarker, startFromIndex));
         }
         return charPosition;
      }
   }

   class CacadedValueReaders : IAttributeValueReader
   {
      List<IAttributeValueReader> cascadedReaders = new List<IAttributeValueReader>();

      public CacadedValueReaders(IAttributeValueReader[] prependedReaders)
      {
         cascadedReaders.AddRange(prependedReaders);
         cascadedReaders.Add(new QuotedValueReader());
         cascadedReaders.Add(new UnquotedValueReader());
      }

      public bool GetValueExtents(string tagString, int valueStartPosition, ref XmlParserCursor cursor)
      {
         foreach (var reader in cascadedReaders)
         {
            if (reader.GetValueExtents(tagString, valueStartPosition, ref cursor))
               return true;
         }

         return false;
      }
   }

}
