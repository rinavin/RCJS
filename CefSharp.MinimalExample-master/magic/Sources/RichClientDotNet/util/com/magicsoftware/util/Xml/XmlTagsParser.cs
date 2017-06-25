using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace com.magicsoftware.util.Xml
{
   /// <summary>
   /// Implements an xml reader that reads through the xml tags. The parser can only move from
   /// one tag to another, keeping track of the tags it went through as a path. This allows saving
   /// it as a state and recalling that state at a later stage.
   /// <para/>
   /// This class is implemented as an XML tag enumerator (although not bound as such by interface).
   /// To use the class, the using component should invoke the instance's 'MoveNext' method to move
   /// from one tag to another and use its 'Current' property to get the current XML tag information.
   /// <para/>
   /// The state manages a cursor (current tag indicator) and the xml path. This way, when the xml
   /// parser restores the state to be its current state, the xml is properly set up to continue from
   /// where it left off.
   /// </summary>
   class XmlTagsParser
   {
      XmlParserCursor _cursor = new XmlParserCursor();
      private XmlParserPath _path = new XmlParserPath();
      private string _xml;
      private bool isDisposed = false;

      internal string XmlData { get { return _xml; } }

      public bool IsActive { get; set; }

      /// <summary>
      /// Gets the current cursor of the tag parser.
      /// </summary>
      public ReadOnlyCursor Cursor { get; private set; }

      /// <summary>
      /// Gets the information of the tag that is currently pointed to by the cursor.
      /// </summary>
      public XmlParserTagInfo Current { get; private set; }

      /// <summary>
      /// Gets the depth of the current element in the path. The root element's depth is 0.
      /// </summary>
      public int Depth { get { Validate(); return _path.Depth; } }

      /// <summary>
      /// Gets a string describing the xml path, as returned by XmlParserPath.
      /// </summary>
      public string Path { get { Validate(); return _path.ToString(); } }

      /// <summary>
      /// Gets the entries of the xml path, as returned by XmlParserPath.
      /// </summary>
      public string[] PathEntries { get { Validate(); return _path.ToStringArray(); } }

      /// <summary>
      /// Instantiates a new XmlTagsParser for the given xml string.
      /// </summary>
      /// <param name="xml">A string with the xml to be parsed.</param>
      public XmlTagsParser(string xml)
      {
         _xml = xml;
         Cursor = new ReadOnlyCursor(_cursor);
         IsActive = false;
         Current = XmlParserTagInfo.NullTag;
      }

      /// <summary>
      /// Replicates the tag parser to create a new tag parser with exactly the same state.
      /// Changing either the original tag parser or the clone will not affect the other.
      /// </summary>
      /// <returns>Returns a new XmlTagsParser, initialized to the same data as the 
      /// original parser and placed on the same position as the original parser.</returns>
      public XmlTagsParser Clone()
      {
         Validate();

         XmlTagsParser clone = new XmlTagsParser(_xml);
         clone._cursor = _cursor.Clone();
         clone.Cursor = new ReadOnlyCursor(clone._cursor);
         clone._path = _path.Clone();
         clone.Current = Current.Clone();
         return clone;
      }

      /// <summary>
      /// Moves the parser to the next tag. If the operation succeeded - which means that the parser's
      /// cursor was moved to a valid tag, the method returns true. Otherwise, it returns false, in which
      /// case the parser is invalidated and cannot be used until Reset() is called.
      /// </summary>
      /// <returns>The method returns true if the tags parser was actually moved to another tag and is in valid state. 
      /// Otherwise, the method returns false.</returns>
      public bool MoveToNextTag(IXmlTagParsingStrategy parsingStrategy)
      {
         Validate();

         int nextTagStartPosition;

         if (!FindNextTag(Cursor.EndPosition, out nextTagStartPosition))
            // No tag start.
            return Invalidate(); 

         _cursor.StartPosition = nextTagStartPosition;

         return UpdateState(parsingStrategy);
      }

      /// <summary>
      /// Resets the parser to the beginning of the xml string, so that the next
      /// invocation of "MoveToNextTag()" will position the parser on the first tag.
      /// </summary>
      public void Reset()
      {
         Validate(false);
         _cursor.Reset();
         Current = XmlParserTagInfo.NullTag;
         _path.Clear();
      }

      /// <summary>
      /// Updates the current state after a successful move.
      /// </summary>
      /// <returns>Return whether the parser is still in valid state (true) or not (false).</returns>
      bool UpdateState(IXmlTagParsingStrategy parsingStrategy)
      {
         // Find the end of the current tag.
         if (!parsingStrategy.FindEndOfTag(_xml, _cursor))
            // Tag is not closed.
            return Invalidate();

         // Fit cursor to exact tag boundaries.
         while (_cursor.EndPosition > _cursor.StartPosition && _xml[_cursor.EndPosition - 1] != '>')
            _cursor.EndPosition--;

         if (_cursor.EndPosition <= _cursor.StartPosition)
            return Invalidate();

         XmlParserTagInfo currentTag = Current;
         if (currentTag != XmlParserTagInfo.NullTag && !(currentTag.IsEmptyElement || currentTag.IsElementClosingTag))
         {
            _path.Append(currentTag);
         }


         bool isElementClosingTag = false;
         bool isEmptyElement = false;

         // Determine whether the current tag is an element closing tag.
         if (IsClosingTag(Cursor.StartPosition))
         {
            isElementClosingTag = true;
         }
         else
         {
            // Determine whether the current element is empty (no children).
            if (_xml[Cursor.EndPosition - 2] == '/')
               isEmptyElement = true;
         }

         string currentTagName;
         if (!GetTagName(Cursor.StartPosition, out currentTagName))
            return Invalidate(); 

         currentTag = new XmlParserTagInfo(_cursor, currentTagName);
         currentTag.IsEmptyElement = isEmptyElement;
         currentTag.IsElementClosingTag = isElementClosingTag;
         Current = currentTag;

         if (isElementClosingTag)
         {
            XmlParserTagInfo tag = _path.RemoveLastEntry();
            Debug.Assert(tag.Name == currentTagName, "Popped tag name '" + tag.Name + "' is not as expected: '" + currentTagName + "'");
            Current.IsBoundaryElement = tag.IsBoundaryElement;
         }

         return true;
      }

      /// <summary>
      /// Finds the first tag after the given position. If the method
      /// succeeds it returns true and the value of 'nextTagPosition' is properly set.
      /// Otherwise the method returns false, and nextTagPosition is set to -1.
      /// </summary>
      /// <param name="startFrom">The position within _xml string to start searching for the next tag.</param>
      /// <param name="nextTagPosition">A ref variable to hold the next tag's position.</param>
      /// <returns>Returns true if the beginning of a tag was found after 'startFrom' position and the value of nextTagPosition is valid.
      /// Otherwise, returns false.</returns>
      bool FindNextTag(int startFrom, out int nextTagPosition)
      {
         nextTagPosition = -1;
         if (startFrom >= _xml.Length)
            return false;

         nextTagPosition = _xml.IndexOf('<', startFrom);
         return nextTagPosition >= 0;
      }

      /// <summary>
      /// Extracts the name of tag at 'tagStartPosition', considering whether it is an
      /// element opening tag or an element closing tag.
      /// </summary>
      /// <param name="tagStartPosition">The position within _xml where the tag starts - i.e. where the less than character is (&lt;).</param>
      /// <param name="tagName">Out variable to hold the name of the tag.</param>
      /// <returns>If the method succeeds, it returns true and sets 'tagName' value to be the name of the tag. Otherwise it returns false
      /// and tagName is set to null.</returns>
      bool GetTagName(int tagStartPosition, out string tagName)
      {
         Debug.Assert(_xml[tagStartPosition] == '<', "No tag starts at position " + tagStartPosition);
         tagName = null;
         int nameStartPosition = tagStartPosition + 1;
         if (IsClosingTag(tagStartPosition))
            nameStartPosition++;
         int nameEndPosition = _xml.IndexOfAny(XmlParserConstants.EndOfTagNameChars, nameStartPosition);
         if (nameEndPosition == -1)
            return false;

         tagName = _xml.Substring(nameStartPosition, nameEndPosition - nameStartPosition);
         return true;
      }

      /// <summary>
      /// Moves the cursor to the end of the current tag, without moving to the next
      /// tag. The next invocation of 'MoveToNextTag()' will move the parser to next 
      /// tag.
      /// </summary>
      internal void LeaveCurrentTag()
      {
         _cursor.CloseGapForward();
         Current = XmlParserTagInfo.NullTag;
      }

      /// <summary>
      /// Finds the first tag whose name is tagName, starting at fromPosition, within _xml.
      /// This method has no effect on the parser's state. It only returns whether the tag
      /// name exists down the road, so that a decision can be made whether to move the parser
      /// towards it, or not.
      /// </summary>
      /// <param name="tagName">The name of the tag to find.</param>
      /// <param name="fromPosition">The position within _xml to start the search.</param>
      /// <returns>The method returns whether the tag was found or not.</returns>
      internal bool FindTag(string tagName, int fromPosition)
      {
         string searchString = "<" + tagName;
         int tagPosition = _xml.IndexOf(searchString, fromPosition);
         if (tagPosition == -1)
            return false;

         string foundTagName;
         if (GetTagName(tagPosition, out foundTagName))
         {
            if (foundTagName == tagName)
               return true;
            else
               return FindTag(tagName, tagPosition + 1);
         }
         else
            return false;
      }

      /// <summary>
      /// Returns whether the tag starting at position tagStart is a 'closing tag', which
      /// means it starts with '&lt;/'.
      /// </summary>
      /// <param name="tagStart">The tag start position within _xml.</param>
      /// <returns>The method returns true if the tag is a closing tag and false otherwise.</returns>
      bool IsClosingTag(int tagStart)
      {
         Debug.Assert(_xml[tagStart] == '<', "tagStart is not set to the beginning of a tag.");
         return _xml[tagStart + 1] == '/';
      }

      /// <summary>
      /// Extracts a substring from _xml.
      /// </summary>
      /// <param name="startPosition"></param>
      /// <param name="length"></param>
      /// <returns></returns>
      public string XmlSubstring(int startPosition, int length)
      {
         Validate(); 
         return _xml.Substring(startPosition, length);
      }

      void Validate()
      {
         Validate(true);
      }

      /// <summary>
      /// Ensures the parser is in valid state. If not, will throw an exception.
      /// </summary>
      /// <param name="validateCursor">Denotes whether the cursor's state should be validated as well.</param>
      void Validate(bool validateCursor)
      {
         if (isDisposed)
            throw new InvalidOperationException("The XmlTagEnumerator was already disposed and cannot be used any further.");

         if (_xml == null)
            throw new InvalidOperationException("The XmlTagEnumerator does not have XML data to work with.");

         if (validateCursor && !_cursor.IsValid)
            throw new InvalidOperationException("The cursor was invalidated. You probably need to call Reset()");
      }

      /// <summary>
      /// Invalidates the parser so that it cannot be used until reset. The method
      /// returns a constant 'false' value, so that it can be used directly on 'return'
      /// statement. For example: return Invalidate().
      /// </summary>
      /// <returns>Always returns false.</returns>
      bool Invalidate()
      {
         _cursor.Invalidate();
         _path.Clear();
         return false;
      }

      #region ToString Methods

      /// <summary>
      /// Generates a string that visualizes the XML parser state (e.g. for debug watch list.)<br/>
      /// The method will show the XML data, trimming it to 20 characters before the 
      /// current position (_currIndex) and up to 50 characters after the current position.
      /// The current position itself will be marked with a marker that looks like:  
      /// |-{current index}-| <br/>
      /// The marker will be placed immediately before currentState.Data[_currIndex].
      /// </summary>
      /// <returns></returns>
      public override string ToString()
      {
         Validate(); 
         return ToString(20, 50);
      }

      /// <summary>
      /// Generates a string that visualizes the XML parser state (e.g. for debug watch list.)<br/>
      /// The method will show the XML data, trimming it to headCharCount characters before the 
      /// current position (_currIndex) and up to tailCharCount characters after the current position.
      /// The current position itself will be marked with a marker that looks like:  
      /// |-{current index}-| <br/>
      /// The marker will be placed immediately before currentState.Data[_currIndex].
      /// </summary>
      /// <param name="headCharCount">Number of characters to show before the current position marker.</param>
      /// <param name="tailCharCount">Number of characters to show after the current position marker.</param>
      /// <returns></returns>
      public string ToString(int headCharCount, int tailCharCount)
      {
         Validate(); 
         int markerPosition = Math.Min(Cursor.StartPosition, _xml.Length);
         int segmentStartIndex = Math.Max(0, markerPosition - headCharCount);
         int segmentEndIndex = Math.Min(_xml.Length, markerPosition + tailCharCount);

         int headLength = markerPosition - segmentStartIndex;
         int tailLength = segmentEndIndex - markerPosition;

         StringBuilder segment = new StringBuilder();
         if (segmentStartIndex > 0)
            segment.Append("...");

         if (headLength > 0)
            segment.Append(_xml, segmentStartIndex, headLength);

         segment.Append("|-{").Append(Cursor.StartPosition.ToString()).Append("}-|");

         if (tailLength > 0)
            segment.Append(_xml, Cursor.StartPosition, tailLength);

         if (segmentEndIndex < _xml.Length)
            segment.Append("...");

         return segment.ToString();
      }

      #endregion


      #region IDisposable Members

      public void Dispose()
      {
         Validate(); 
         _xml = null;
         _cursor.Invalidate();
         Current = null;
      }

      #endregion

      #region Transition Methods

      public void RefreshDataFromOldParser(XmlParser oldParser)
      {
         _xml = oldParser.getXMLdata();
         // force re-reading of the next tag.
         _cursor.EndPosition = _cursor.StartPosition;
         Current = XmlParserTagInfo.NullTag;
      }

      #endregion

   }
}
