using System;
using System.Text;
using System.Collections.Generic;
using util.com.magicsoftware.util;

namespace com.magicsoftware.util.Xml
{
   /// <summary> a helper class for the parsing of the XML</summary>
   public class XmlParser
   {
      static readonly char[] endOfNameChar = new char[] { ' ', '>' };

      private int _currIndex = 0;
      private String _xmLdata = "";
      private readonly MgArrayList _history = new MgArrayList(); // In order to allow recursive parsing we save prev data

      /// <summary>
      /// 
      /// </summary>
      /// <param name="data"></param>
      public XmlParser(string data)
      {
         setXMLdata(data);
         setCurrIndex(0);
      }

      public XmlParser()
         : this(String.Empty)
      {
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
              str = str.Trim();

         string[] strTok = str.Split(delimiter.ToCharArray());

         for (i = 0; i < strTok.Length; i++)
         {
            // Split in C# creates a last empty string token if the source string ends with
            // the delimiter or if the string is empty (as opposed to Java that will ignore it)
            // therefore we have to break this loop if such case occurs.
            if (isMagicXML && (i == strTok.Length - 1 && strTok.Length % 2 == 1))
               break;

            token = strTok[i];
            if (isMagicXML)
            {
               // the 1st token in the pair comes with "=", remove it.
               if (i % 2 == 0)
               {
                  token = token.Trim();
                  if (token.EndsWith("="))
                     token = token.Substring(0, (token.Length - 1));
               }
               // 2nd token in the pair can be an empty string, in that case set it to " ".
               else if (token.Equals(""))
                  token = " ";
            }
            if (token == null)
               throw new ApplicationException("in ClientManager.Instance.XMLParser.getTokens() null token value");
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

      /// <summary>get next tag name from current index in XML string</summary>
      /// <returns> next tag name </returns>
      public String getNextTag()
      {
         int endOfTag, tmpIndx;
         
         char tmpChar;

         if (_xmLdata.Length - _currIndex <= 1)
            return null; //end of XML string

         for (tmpIndx = _currIndex + 1; tmpIndx < _xmLdata.Length; tmpIndx++)
         {
            tmpChar = _xmLdata[tmpIndx];
            // a letter starts an element and ends with " ". "/" starts an element closing and ends with '>'.
            if (Char.IsLetter(tmpChar) || tmpChar == '/')
            {
               endOfTag = _xmLdata.IndexOfAny(endOfNameChar, tmpIndx, _xmLdata.Length - tmpIndx);

               if (endOfTag == -1)
                  return null;
               else
               {
                  return _xmLdata.Substring(tmpIndx, endOfTag - tmpIndx);
               }
            }
         }
         return null;
      }

      /// <summary>Substring of XMLstring</summary>
      /// <returns> substring of XML string -from currIndex to endContext </returns>
      public String getXMLsubstring(int endContext)
      {
         return _xmLdata.Substring(_currIndex, (endContext) - (_currIndex));
      }

      /// <summary>get current element value</summary>
      /// <returns> element's value </returns>
      public String GetCurrentElementValue()
      {
         String value;

         setCurrIndex2EndOfTag();
         int endContext = getXMLdata().IndexOf(XMLConstants.TAG_OPEN, getCurrIndex());
         // read value of xml element
         value = getXMLsubstring(endContext);
         setCurrIndex2EndOfTag();
         return value;
      }

      /// <summary>set current index (on parsing time) to the end of current tag</summary>
      public void setCurrIndex2EndOfTag()
      {
         _currIndex = _xmLdata.IndexOf(">", _currIndex) + 1;
      }

      /// <summary>get int from string at parsing time</summary>
      public static int getInt(String valueStr)
      {
         //TODO: Kaushal. I don't know if we need to call DeleteStringsFromEnds() here.
         //We will see if we will have problem.
         //return Int32.Parse(DeleteStringsFromEnds(valueStr, "\"").Trim());
         return Int32.Parse(valueStr.Trim());
      }

      /// <summary>get boolean from string at parsing time</summary>
      public static bool getBoolean(String valueStr)
      {
         return (valueStr[0] == '1');
      }

      /// <summary>get/set functions 4 XMLstring & currIndex, for parser</summary>
      public int getCurrIndex()
      {
         return _currIndex;
      }

      public String getXMLdata()
      {
         return _xmLdata;
      }

      public void add2CurrIndex(int add)
      {
         _currIndex += add;
      }

      public void setCurrIndex(int index)
      {
         _currIndex = index;
      }

      public void setXMLdata(String data)
      {
         if (data != null)
            _xmLdata = data.Trim();
         else
         {
            _xmLdata = null;
            setCurrIndex(0);
         }
      }

      /// <summary>
      /// prepare the parser to read from the newXmlString
      /// </summary>
      /// <param name="newXmlString"></param>
      public void PrepareFormReadString(String newXmlString)
      {
         setXMLdata(newXmlString);
         setCurrIndex(0);

      }

      /// <summary> push the current parsing information into the history stack</summary>
      public void push()
      {
         _history.Add(_currIndex);
         _history.Add(_xmLdata);
      }

      /// <summary> restore the previous parsing information from the history stack</summary>
      public void pop()
      {
         int size = _history.Count;

         _xmLdata = ((String)_history[size - 1]);
         _currIndex = ((Int32)_history[size - 2]);

         _history.SetSize(size - 2);
      }

      /// <summary>gets a table cache xml and set the xmlparser data and index accordingly</summary>
      public void loadTableCacheData(String data)
      {
         setXMLdata(data);
         setCurrIndex(0);
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
         // Get the current tag according to the value of _currIndex.
         string currentTag = getNextTag();
         int currentTagIndex = _xmLdata.IndexOf(XMLConstants.TAG_OPEN + currentTag, getCurrIndex());

         // Find the end of the element's block in the XML.
         // find next open tag
         int nextOpenTagIndex = _xmLdata.IndexOf(XMLConstants.TAG_OPEN, currentTagIndex + 1);
         if (nextOpenTagIndex == -1)
            nextOpenTagIndex = _xmLdata.Length;
         
         // find a close tag BEFORE the next open tag
         int elementEndIndex = _xmLdata.IndexOf(XMLConstants.TAG_TERM, getCurrIndex(), nextOpenTagIndex - getCurrIndex());
         
         if (elementEndIndex == -1)
            // close tag was not found in range - we have inner elements, look for the full close tag
            elementEndIndex = _xmLdata.IndexOf('/' + currentTag, getCurrIndex()) + currentTag.Length + XMLConstants.TAG_TERM.Length;
         else
            elementEndIndex += XMLConstants.TAG_TERM.Length;

         // Copy the element data so it can be returned.
         string elementBlock = getXMLsubstring(elementEndIndex);

         // Move the parser to the end of the element block.
         setCurrIndex(elementEndIndex);

         return elementBlock;
      }

      public string ReadContentOfCurrentElement()
      {
         // Get the current tag according to the value of _currIndex.
         string currentTag = getNextTag();

         // Find the end of the element's block in the XML.
         int elementEndIndex = _xmLdata.IndexOf("</" + currentTag + ">", getCurrIndex());
         if (elementEndIndex == -1)
            // Can't find the end of the current element - either XML is faulty or the element is empty.
            return string.Empty;

         // Move to the end of the opening tag
         setCurrIndex2EndOfTag();

         // Copy the content of the element (from the end of the opening tag to the beginning of the closing tag).
         string elementBlock = getXMLsubstring(elementEndIndex);

         // Move the parser to the end of the element block.
         setCurrIndex(elementEndIndex);
         setCurrIndex2EndOfTag();

         return elementBlock;
      }

      /// <summary>
      /// Generates a string that visualizes the XML parser state (e.g. for debug watch list.)<br/>
      /// The method will show the XML data, trimming it to 20 characters before the 
      /// current position (_currIndex) and up to 50 characters after the current position.
      /// The current position itself will be marked with a marker that looks like:  
      /// |-{current index}-| <br/>
      /// The marker will be placed immediately before _xmlData[_currIndex].
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
      /// The marker will be placed immediately before _xmlData[_currIndex].
      /// </summary>
      /// <param name="headCharCount">Number of characters to show before the current position marker.</param>
      /// <param name="tailCharCount">Number of characters to show after the current position marker.</param>
      /// <returns></returns>
      public string ToString(int headCharCount, int tailCharCount)
      {
         int markerPosition = Math.Min(_currIndex, _xmLdata.Length);
         int segmentStartIndex = Math.Max(0, markerPosition - headCharCount);
         int segmentEndIndex = Math.Min(_xmLdata.Length, markerPosition + tailCharCount);

         int headLength = markerPosition - segmentStartIndex;
         int tailLength = segmentEndIndex - markerPosition;

         StringBuilder segment = new StringBuilder();
         if (segmentStartIndex > 0)
            segment.Append("...");

         if (headLength > 0)
            segment.Append(_xmLdata, segmentStartIndex, headLength);

         segment.Append("|-{").Append(_currIndex.ToString()).Append("}-|");

         if (tailLength > 0)
            segment.Append(_xmLdata, _currIndex, tailLength);

         if (segmentEndIndex < _xmLdata.Length)
            segment.Append("...");

         return segment.ToString();
      }

      /// <summary>
      /// remove the specified elements from the XML buffer
      /// </summary>
      /// <param name="xmlBuffer">The XML buffer to change</param>
      /// <param name="elementTagsToRemove">Array of strings (=element tags) to remove from the buffer</param>
      /// <returns></returns>
      public static String RemoveXmlElements(String xmlBuffer, params string[] elementTagsToRemove)
      {
         XmlParser parser = new XmlParser();

         // Go over all tags to be removed
         foreach (String tagToRemove in elementTagsToRemove)
         {
            StringBuilder returnedXml = new StringBuilder();
            parser.setXMLdata(xmlBuffer);
            parser.setCurrIndex(0);
            int startIndex = 0;
            int startTagIndex;

            // loop while the requested tag exists in the xml buffer
            while ((startTagIndex = xmlBuffer.IndexOf("<" + tagToRemove, startIndex)) != -1)
            {
               // read the string up to the tag
               returnedXml.Append(xmlBuffer.Substring(startIndex, startTagIndex - startIndex));

               // skip the element using the parser to find the end of the tag 
               parser.setCurrIndex(startTagIndex);
               parser.ReadToEndOfCurrentElement();

               // continue from after the tag
               startIndex = parser.getCurrIndex();
            }

            // add what remained after the last removed tag
            returnedXml.Append(xmlBuffer.Substring(parser.getCurrIndex()));
            //prepare the buffer for the next tag to remove
            xmlBuffer = returnedXml.ToString();
         }
         return xmlBuffer;
      }

      /// <summary> Gets the elements from the XML buffer. </summary>
      /// <param name="xmlBuffer"></param>
      /// <param name="elementsToExtract"></param>
      /// <returns></returns>
      public static Dictionary<String, String> GetElements(String xmlBuffer, params string[] elementsToExtract)
      {
         Dictionary<String, String> retList = new Dictionary<String, String>();
         XmlParser parser = new XmlParser();
         int startIndex = 0;

         parser.setXMLdata(xmlBuffer);

         foreach (String elementToExtract in elementsToExtract)
         {
            parser.setCurrIndex(0);

            int startTagIndex = xmlBuffer.IndexOf("<" + elementToExtract, startIndex);

            if (startTagIndex != -1)
            {
               // Move the parser to the current element and read it
               parser.setCurrIndex(startTagIndex);
               retList.Add(elementToExtract, parser.ReadToEndOfCurrentElement());
            }
         }

         return retList;
      }
   }
}
