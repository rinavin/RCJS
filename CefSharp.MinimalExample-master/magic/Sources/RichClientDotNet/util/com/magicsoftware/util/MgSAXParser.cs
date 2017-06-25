using System;
using System.Xml;
using System.IO;
using System.Collections.Specialized;
using System.Text;

namespace com.magicsoftware.util
{
   public class MgSAXParser
   {
      MgSAXHandler handler;

      /// <summary>
      /// 
      /// </summary>
      /// <param name="handler"></param>
      public MgSAXParser(MgSAXHandler handler)
      {
         this.handler = handler;
      }

      /// <summary>
      /// Emulates the behavior of a SAX parser, it realizes the callback events of the parser.
      /// Assumes that the attribute values are not encoded
      /// </summary>
      public void parse(byte[] xmlContent)
      {
         parse(xmlContent, false);
      }

      /// <summary>
      /// Emulates the behavior of a SAX parser, it realizes the callback events of the parser.
      /// </summary>
      /// <param name="xmlContent"></param>
      /// <param name="decodeAttributeValues"> indicates that xml attribute values are encode and should be decoded while parsing </param>
      public void parse(byte[] xmlContent, bool decodeAttributeValues)
      {
         try
         {
            XmlTextReader reader = new XmlTextReader(new MemoryStream(xmlContent));
            reader.WhitespaceHandling = WhitespaceHandling.None;

            String elementValue = "";
            NameValueCollection attributes = new NameValueCollection();

            while (reader.Read())
            {
               switch (reader.NodeType)
               {
                  case XmlNodeType.Element:
                     bool Empty = reader.IsEmptyElement;
                     String elementName = reader.Name;

                     if (reader.HasAttributes)
                     {
                        for (int i = 0; i < reader.AttributeCount; i++)
                        {
                           reader.MoveToAttribute(i);
                           String escVal = decodeAttributeValues ? XmlConvert.DecodeName(reader.Value) : reader.Value;
                           attributes.Add(reader.Name, escVal);
                        }
                     }

                     handler.startElement(elementName, attributes);
                     attributes.Clear();

                     if (Empty)
                        handler.endElement(elementName, "");
                     break;

                  case XmlNodeType.EndElement:
                     handler.endElement(reader.Name, elementValue);
                     elementValue = "";
                     break;

                  case XmlNodeType.Text:
                     elementValue = reader.Value.Trim();
                     break;

                  case XmlNodeType.CDATA:
                     elementValue = reader.Value.Trim();
                     break;

                  default:
                     break;
               }
            }
         }
         catch (XmlException ex)
         {
            throw ex;
         }
      }

      /// <summary>
      /// Parses an encoded XML string. This method converts the xml content to
      /// parse-able byte[] using UTF8 encoding and then invokes the parse method.
      /// </summary>
      /// <param name="xmlContent">The encoded XML content.</param>
      public void Parse(string xmlContent)
      {
         byte[] xmlContentBytes = Encoding.UTF8.GetBytes(xmlContent);
         parse(xmlContentBytes);
      }

      public static void Parse(string xmlContent, MgSAXHandler handler)
      {
         MgSAXParser mgSAXParser = new MgSAXParser(handler);
         mgSAXParser.Parse(xmlContent);
      }
   }
}
