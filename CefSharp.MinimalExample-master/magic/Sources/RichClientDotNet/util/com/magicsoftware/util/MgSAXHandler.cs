using System;
using System.Collections.Specialized;
using System.Collections.Generic;

namespace com.magicsoftware.util
{
   /// <summary> 
   /// This class parses an XML using MgSAXParser.
   /// Those who want to use this class for parsing the XML should 
   /// either implement MgSAXHandlerInterface
   /// or should extend this class.
   /// </summary>
   /// <author>  Kaushal Sanghavi </author>
   public class MgSAXHandler
   {
      private readonly MgSAXHandlerInterface _finalHandler;
      private Stack<NameValueCollection> _attributes = new Stack<NameValueCollection>();

      /// <summary>
      /// 
      /// </summary>
      public MgSAXHandler()
      {
         _finalHandler = null;
      }

      /// <summary> </summary>
      /// <param name="finalHandler"> </param>
      public MgSAXHandler(MgSAXHandlerInterface finalHandler)
      {
         _finalHandler = finalHandler;
      }

      /// <summary>
      /// Emulates the behavior of a SAX parser, it realizes the callback events of the parser.
      /// Assumes that the attribute values are not encoded
      /// </summary>
      public void parse(byte[] xmlContent)
      {
         MgSAXParser mgSAXParser = new MgSAXParser(this);
         mgSAXParser.parse(xmlContent, false);
      }

      /// <summary>
      /// Emulates the behavior of a SAX parser, it realizes the callback events of the parser.
      /// </summary>
      /// <param name="xmlContent"> </param>
      /// <param name="decodeAttributeValues"> indicates that xml attribute values are encode and should be decoded while parsing </param>
      public void parse(byte[] xmlContent, bool decodeAttributeValues)
      {
         MgSAXParser mgSAXParser = new MgSAXParser(this);
         mgSAXParser.parse(xmlContent, decodeAttributeValues);
      }

      public virtual void startElement(String elementName, NameValueCollection attributes)
      {
         _attributes.Push(new NameValueCollection(attributes));
      }

      public virtual void endElement(String elementName, String elementValue)
      {
         if (_finalHandler != null)
            _finalHandler.endElement(elementName, elementValue, _attributes.Pop());
      }
   }
}