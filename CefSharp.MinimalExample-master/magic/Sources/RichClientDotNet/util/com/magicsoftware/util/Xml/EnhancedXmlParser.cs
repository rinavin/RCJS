using System;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Collections.Specialized;

namespace com.magicsoftware.util.Xml
{
   /// <summary> a helper class for the parsing of the XML</summary>
   public class EnhancedXmlParser
   {
      XmlTagsParser _currentState = null;
      XmlTagsParser CurrentState
      {
         get
         {
            Debug.Assert(_currentState == null || _currentState.IsActive, "Current state is not marked as active. Anyone modified _currentState var directly?");
            return _currentState;
         }
         set
         {
            if (_currentState != null)
               _currentState.IsActive = false;
            _currentState = value;
            if (_currentState != null)
               _currentState.IsActive = true;
         }
      }

      /// <summary>
      /// Parser state stack, to allow recursive parsing (instead of creating new parser instances, God knows why...)
      /// </summary>
      private readonly Stack<XmlTagsParser> _history = new Stack<XmlTagsParser>();

      private bool suppressNextMove = false;

      /// <summary>
      /// 
      /// </summary>
      /// <param name="data"></param>
      public EnhancedXmlParser(string data)
      {
         SetXMLData(data);
      }

      public EnhancedXmlParser()
         : this(String.Empty)
      {
      }

      public bool IsOnElementClosingTag
      {
         get
         {
            if (CurrentState != null)
               return CurrentTag.IsElementClosingTag;
            else
               return false;
         }
      }

      private XmlParserTagInfo CurrentTag
      {
         get
         {
            if (CurrentState == null)
               return null;

            return CurrentState.Current;
         }
      }

      private bool IsOnEndOfElementBoundary
      {
         get
         {
            if (CurrentTag == null)
               return false;

            return (CurrentTag.IsBoundaryElement && (CurrentTag.IsElementClosingTag || CurrentTag.IsEmptyElement));
         }
      }



      public void SetXMLData(String data)
      {
         CurrentState = new XmlTagsParser(data ?? String.Empty);
         suppressNextMove = false;
      }

      public bool MoveToNextTag()
      {
         return MoveToNextTag(DefaultTagParsingStrategy.Instance);
      }

      public bool MoveToNextTag(IXmlTagParsingStrategy tagParsingStrategy)
      {
         if (IsOnEndOfElementBoundary)
            return false;

         if (suppressNextMove)
         {
            suppressNextMove = false;
            return true;
         }

         return CurrentState.MoveToNextTag(tagParsingStrategy);
      }

      public IDisposable ElementBoundary()
      {
         return new ElementBoundaryControl(CurrentState);
      }

      public bool MoveToEndOfCurrentElement()
      {
         using (ElementBoundary())
         {
            while (MoveToNextTag()) ;
         }
         return CurrentState.Cursor.IsValid;
      }

      /// <summary>
      /// Moves the parser's cursor to the next element whose tag name is 'elementTagName'.
      /// The parser will not be moved if there's no such tag name following the current cursor position.
      /// </summary>
      /// <param name="elementTagName"></param>
      /// <returns></returns>
      public bool MoveToElement(string elementTagName)
      {
         if (CurrentState.FindTag(elementTagName, CurrentState.Cursor.StartPosition))
         {
            while (MoveToNextTag())
            {
               if (CurrentTag.Name == elementTagName)
                  return true;
            }
         }
         return false;
      }

      public string CurrentTagName { get { return CurrentTag.Name; } }

      public int CurrentDepth { get { return CurrentState.Depth; } }

      public string[] CurrentPathEntries
      {
         get
         {
            return CurrentState.PathEntries;
         }
      }

      public string CurrentPath
      {
         get
         {
            return CurrentState.Path;
         }
      }

      public int CurrentPosition { get { return CurrentState.Cursor.StartPosition; } }

      /// <summary> push the current parsing information into the history stack</summary>
      public IDisposable Push()
      {
         var oldState = CurrentState;
         PushCurrentState();
         CurrentState = oldState.Clone();
         return new ParserContextSwitch(this);
      }

      public IDisposable Push(string newData)
      {
         PushCurrentState();
         SetXMLData(newData);
         return new ParserContextSwitch(this);
      }

      private void PushCurrentState()
      {
         _history.Push(CurrentState);
         CurrentState = null;
      }

      /// <summary> restore the previous parsing information from the history stack</summary>
      public void Pop()
      {
         Debug.Assert(_history.Count > 0);
         if (_history.Count == 0)
            return;

         CurrentState = _history.Pop();
      }

      /// <summary>
      /// Reads the XML from the element at the current position until the end of
      /// the element, returning the contents as a string. This allows deferring the 
      /// processing of an element until the time is right to do so.<br/>
      /// The returned string contains the element tag itself. For example:<br/>
      /// - Assuming that the current element is 'element1', with 2 'innerElement' elements, the
      /// resulting string will look like this:<br/>
      /// <element1>
      ///   <innerelement/>
      ///   <innerelement/>
      /// </element1>
      /// 
      /// This makes the result valid for processing by this XML parser.
      /// </summary>
      /// <returns></returns>
      public string ReadToEndOfCurrentElement()
      {
         int startPosition = CurrentPosition;
         using (ElementBoundary())
         {
            while (MoveToNextTag()) ;
         }
         int endPosition = CurrentPosition;

         return CurrentState.XmlSubstring(startPosition, endPosition - startPosition);
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
         if (CurrentState != null)
            return CurrentState.ToString(headCharCount, tailCharCount);

         return "<not initialized>";
      }

      #endregion

      #region Static Methods

      /// <summary>
      /// remove the specified elements from the XML buffer
      /// </summary>
      /// <param name="xmlBuffer">The XML buffer to change</param>
      /// <param name="elementTagsToRemove">Array of strings (=element tags) to remove from the buffer</param>
      /// <returns></returns>
      public static String RemoveXmlElements(String xmlBuffer, params string[] elementTagsToRemove)
      {
         EnhancedXmlParser parser = new EnhancedXmlParser();

         // Go over all tags to be removed
         foreach (String tagToRemove in elementTagsToRemove)
         {
            StringBuilder returnedXml = new StringBuilder();
            parser.SetXMLData(xmlBuffer);
            int startIndex = 0;

            // loop while the requested tag exists in the XML buffer
            while (parser.MoveToElement(tagToRemove))
            {
               // read the string up to the tag
               returnedXml.Append(xmlBuffer.Substring(startIndex, parser.CurrentPosition - startIndex));

               // skip the element using the parser to find the end of the tag 
               parser.ReadToEndOfCurrentElement();
               startIndex = parser.CurrentPosition;
            }

            // add what remained after the last removed tag
            returnedXml.Append(xmlBuffer.Substring(startIndex));
            //prepare the buffer for the next tag to remove
            xmlBuffer = returnedXml.ToString();
         }
         return xmlBuffer;
      }

      /// <summary> parse a string according to a set of delimiters and return the result in a vector</summary>
      /// <param name="str">the String which need be parted </param>
      /// <param name="delimiter">the delimiter which part different parts of str </param>
      /// <param name="isMagicXML">is needed tokenizer working on Magic XML, so the "=" sign will be delited in the end of every first token </param>
      /// <returns> tmpVector dynamically array, which consist tokens in every element, every token is String </returns>
      protected internal static List<String> getTokens(String str, String delimiter, bool isMagicXML)
      {
         List<String> tokensVec = new List<String>();
         String token = null;
         int i;

         if (isMagicXML)
            str = convertEmptyAttrs(str.Trim());
         string[] strTok = str.Split(delimiter.ToCharArray());

         for (i = 0; i < strTok.Length; i++)
         {
            // Split in C# creates a last empty string token if the source string ends with
            // the delimiter or if the string is empty (as opposed to Java that will ignore it)
            // therefore we have to break this loop if such case occurs.
            if (isMagicXML && (i == strTok.Length - 1 && strTok.Length % 2 == 1))
               break;

            token = strTok[i];
            if (i % 2 == 0 && isMagicXML)
            {
               token = token.Trim(); // remove leading & trailing spaces from the attribute name
               if (token.EndsWith("="))
                  token = token.Substring(0, (token.Length - 1));
            }
            if (token == null)
               throw new ApplicationException("in ClientManager.Instance.XmlParser.getTokens() null token value");
            tokensVec.Add(token);
         }
         return tokensVec;
      }

      /// <summary> parse a string according to a set of delimiters and return the result in a vector 
      /// for Magics XML (first=value1, second=value2 ... gives out [first][value1][second][value2] and so on)
      /// </summary>
      /// <param name="str">the String which need be parted
      /// </param>
      /// <param name="delimiter">the delimiter which part different parts of str
      /// </param>
      /// <returns> tmpVector dynamically array, which consist tokens in every element,
      /// every token is String
      /// </returns>
      public static List<String> getTokens(String str, String delimiter)
      {
         return getTokens(str, delimiter, true);
      }

      /// <summary> convert attribute="" to attribute=" "</summary>
      private static String convertEmptyAttrs(String str)
      {
         int index = 0;
         String empty = "\"\"";

         while ((index = str.IndexOf(empty, index)) != -1)
            str = str.Substring(0, (index + 1)) + " " + str.Substring(index + 1);
         return str;
      }

      /// <summary>unscape from:
      /// {"&amp;",\\, \q, \o, \l, \g, \e, \\r, \\n}, to:
      /// {"&",     \,  ",  ',  <,  >,  =,  \r,  \n}
      /// <param name="str">String to be converted</param>
      /// <returns>unescaped string</returns>
      public static String unescape(String str)
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

      /// <summary>escape from:
      /// {\,  ",  ',  <,   >,  =,  \r,  \n}, to:
      /// {\\, \q, \0, \l,  \g, \e, \\r, \\n}
      /// <param name="str">String to be converted</param>
      /// <returns>escaped string</returns>
      public static String escape(String str)
      {
         StringBuilder escapedString = new StringBuilder(str.Length * 2);

         for (int i = 0; i < str.Length; i++)
         {
            switch (str[i])
            {
               case '\\':
                  escapedString.Append("\\\\");
                  break;
               case '"':
                  escapedString.Append("\\q");
                  break;
               case '\'':
                  escapedString.Append("\\o");
                  break;
               case '<':
                  escapedString.Append("\\l");
                  break;
               case '>':
                  escapedString.Append("\\g");
                  break;
               case '=':
                  escapedString.Append("\\e");
                  break;
               case '\r':
                  escapedString.Append("\r");
                  break;
               case '\n':
                  escapedString.Append("\n");
                  break;
               default:
                  escapedString.Append(str[i]);
                  break;
            }
         }

         return (escapedString.ToString());
      }

      /// <summary>
      /// here we only need to take care of "&" so that Sax parser will be able to handle url
      /// </summary>
      /// <param name="str"></param>
      /// <returns></returns>
      public static String escapeUrl(String str)
      {
         return str.Replace("&", "&amp;");
      }

      #endregion

      public IEnumerator<XmlAttribute> GetCurrentElementAttributesEnumerator(params IAttributeValueReader[] prependedValueReaders)
      {
         int tagLength = CurrentState.Cursor.Span - 1;
         if (CurrentTag.IsEmptyElement)
            tagLength--;
         string elementTag = CurrentState.XmlSubstring(CurrentPosition, tagLength);
         return new AttributesParser(elementTag, prependedValueReaders);
      }

      public IEnumerable<XmlAttribute> GetCurrentElementAttributes(params IAttributeValueReader[] prependedValueReaders)
      {
         var attrsEnum = GetCurrentElementAttributesEnumerator(prependedValueReaders);
         while (attrsEnum.MoveNext())
            yield return attrsEnum.Current;
      }

      class ParserContextSwitch : IDisposable
      {
         private EnhancedXmlParser parser;

         public ParserContextSwitch(EnhancedXmlParser parser)
         {
            this.parser = parser;
         }

         public void Dispose()
         {
            parser.Pop();
         }
      }


      class ElementBoundaryControl : IDisposable
      {
         XmlTagsParser parserState;

         public ElementBoundaryControl(XmlTagsParser parserState)
         {
            this.parserState = parserState;
            if (parserState.Current == XmlParserTagInfo.NullTag)
               throw new InvalidOperationException("You must call MoveToNextTag at least once before creating an element boundary.");
            parserState.Current.IsBoundaryElement = true;
         }

         #region IDisposable Members

         public void Dispose()
         {
            Debug.Assert(parserState.IsActive, "Parser state owning current element boundary is not active. Missing a Pop() somewhere? XML data was replaced?");
            if (parserState.IsActive)
               parserState.LeaveCurrentTag();
         }

         #endregion
      }

      #region backwards compatibilty

      public string getNextTag()
      {
         return CurrentTagName;
      }

      public void add2CurrIndex(int n)
      {

      }

      public int getCurrIndex()
      {
         return 0;
      }

      public void setCurrIndex(int i)
      {
         MoveToNextTag();
      }

      public static bool getBoolean(string s)
      {
         return false;
      }

      public static int getInt(string s)
      {
         return 0;
      }

      public string getXMLdata()
      {
         return "";
      }

      public string getXMLsubstring(int n)
      { return ""; }

      public void setCurrIndex2EndOfTag()
      {
         MoveToNextTag();
      }

      public void setXMLdata(String data)
      {
         CurrentState = new XmlTagsParser(data ?? String.Empty);
         MoveToNextTag();
      }

      public List<string> AttributesToStringList()
      {
         List<string> attributesList = new List<string>();
         foreach (var attr in GetCurrentElementAttributes())
         {
            attributesList.Add(attr.Name);
            attributesList.Add(attr.Value);
         }
         return attributesList;
      }

      public bool MoveToCurrentTagOfOldParser(XmlParser oldParser, IXmlTagParsingStrategy parsingStrategy)
      {
         Debug.Assert(CurrentPosition <= oldParser.getCurrIndex());

         RefreshDataFromOldParser(oldParser);


         var currentTag = oldParser.getNextTag();
         while (CurrentPosition < oldParser.getCurrIndex())
         {
            if (!MoveToNextTag(parsingStrategy))
               return false;
         }

         if ((CurrentTagName == currentTag) || (CurrentTagName == currentTag.Substring(1)))
         {
            suppressNextMove = true;
            return true;
         }

         return false;
      }

      public void MoveOldParserToCurrentTag(XmlParser oldParser)
      {
         oldParser.setCurrIndex(CurrentPosition);
      }

      public IDisposable SwitchToOldParser(XmlParser oldParser)
      {
         return SwitchToOldParser(oldParser, DefaultTagParsingStrategy.Instance);
      }

      public IDisposable SwitchToOldParser(XmlParser oldParser, IXmlTagParsingStrategy parsingStrategy)
      {
         oldParser.setXMLdata(CurrentState.XmlData);
         return new OldParserSynchronizer(this, parsingStrategy, oldParser);
      }

      public void RefreshDataFromOldParser(XmlParser oldParser)
      {
         XmlParserTagInfo currentTag = CurrentState.Current.Clone();
         CurrentState.RefreshDataFromOldParser(oldParser);
         MoveToNextTag();
         CurrentState.Current.IsBoundaryElement = currentTag.IsBoundaryElement;
      }

      #endregion

   }

   class OldParserSynchronizer : IDisposable
   {
      private EnhancedXmlParser enhancedXmlParser;
      private XmlParser oldParser;
      private IXmlTagParsingStrategy parsingStrategy;

      public OldParserSynchronizer(EnhancedXmlParser enhancedXmlParser, IXmlTagParsingStrategy parsingStrategy, XmlParser oldParser)
      {
         this.enhancedXmlParser = enhancedXmlParser;
         this.oldParser = oldParser;
         this.parsingStrategy = parsingStrategy;
         enhancedXmlParser.MoveOldParserToCurrentTag(oldParser);
      }

      public void Dispose()
      {
         if (!enhancedXmlParser.MoveToCurrentTagOfOldParser(oldParser, parsingStrategy))
            throw new InvalidOperationException("Failed synchronizing new parser with old parser");
      }
   }
}