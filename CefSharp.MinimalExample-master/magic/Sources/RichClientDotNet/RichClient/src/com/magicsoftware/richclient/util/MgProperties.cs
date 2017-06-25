using System;
using System.Xml;
using System.Text;
using System.Collections;
using System.Collections.Specialized;
using com.magicsoftware.util;
using com.magicsoftware.unipaas.util;
using util.com.magicsoftware.util;

namespace com.magicsoftware.richclient.util
{
   /// <summary> 
   /// This class reads/writes the execution properties from/to an XML source
   /// </summary>
   /// <author>  Kaushal Sanghavi</author>
   internal class MgProperties : NameValueCollection, MgSAXHandlerInterface
   {
      /// <summary>
      /// CTOR
      /// </summary>
      internal MgProperties() {}

      /// <summary>
      /// CTOR
      /// </summary>
      internal MgProperties(String propertiesFileName)
      {
         byte[] propertiesContentBytes = HandleFiles.readToByteArray(propertiesFileName, "");
         var propertiesContentXML = Encoding.UTF8.GetString(propertiesContentBytes, 0, propertiesContentBytes.Length);
         loadFromXML(propertiesContentXML, Encoding.UTF8);
      }

      /// <summary>
      /// </summary>
      /// <param name="propertiesFileName"></param>
      internal void WriteToXMLFile(String propertiesFileName)
      {
         var propertiesContentXML = new StringBuilder();
         storeToXML(propertiesContentXML);
         HandleFiles.writeToFile(propertiesFileName, Encoding.UTF8.GetBytes(propertiesContentXML.ToString()));
      }

      /// <summary> 
      /// This function reads the execution properties from an XML source
      /// </summary>
      /// <param name="XMLdata"></param>
      internal void loadFromXML(String XMLdata)
      {
         loadFromXML(XMLdata, Encoding.Default);
      }

      /// <summary> 
      /// This function reads the execution properties from an XML source
      /// </summary>
      /// <param name="XMLdata"></param>
      /// <param name="encoding"></param>
      internal void loadFromXML(String XMLdata, Encoding encoding)
      {
         MgSAXHandler mgSAXHandler = new MgSAXHandler(this);
         try
         {
            mgSAXHandler.parse(encoding.GetBytes(XMLdata), true);
         }
         catch (Exception ex)
         {
            Logger.Instance.WriteExceptionToLog(ex);
         }         
      }

      // When the parser encounters the end of an element, it calls this method
      public void endElement(String elementName, String elementValue, NameValueCollection attributes)
      {
         String key = null;
         String value = null;

         if (elementName.Equals(ConstInterface.MG_TAG_PROPERTY))
         {
            key = attributes[ConstInterface.MG_ATTR_KEY];

            if (!String.IsNullOrEmpty(elementValue))
               value = elementValue;
            else
               value = attributes[XMLConstants.MG_ATTR_VALUE];

            this.Set(key, value);
         }
      }

      /// <summary> 
      /// This function writes the execution properties to an XML source
      /// </summary>
      internal void storeToXML(StringBuilder XMLdata)
      {
         XMLdata.Append(XMLConstants.TAG_OPEN + ConstInterface.MG_TAG_PROPERTIES + XMLConstants.TAG_CLOSE);

         ICollection keys = this.Keys;
         IEnumerator keysEnumerator = keys.GetEnumerator();
         while (keysEnumerator.MoveNext())
         {
            String key = (String)keysEnumerator.Current;
            String value = getProperty(key);

            XMLdata.Append("\n" + XMLConstants.TAG_OPEN + ConstInterface.MG_TAG_PROPERTY);
            XMLdata.Append(" key=\"" + key + "\"");

            if (value.StartsWith(XMLConstants.CDATA_START) && value.EndsWith(XMLConstants.CDATA_END))
            {
               XMLdata.Append(XMLConstants.TAG_CLOSE + "\n");
               XMLdata.Append(value + "\n");
               XMLdata.Append(XMLConstants.END_TAG + ConstInterface.MG_TAG_PROPERTY + XMLConstants.TAG_CLOSE);
            }
            else
            {
               value = XmlConvert.EncodeName(value);
               XMLdata.Append(" val=\"" + value + "\"");
               XMLdata.Append(XMLConstants.TAG_TERM);
            }
         }

         XMLdata.Append("\n" + XMLConstants.END_TAG + ConstInterface.MG_TAG_PROPERTIES + XMLConstants.TAG_CLOSE);
      }

      internal String getProperty(String key)
      {
         return this[key];
      }

      /// <summary>
      /// </summary>
      /// <returns></returns>
      internal MgProperties Clone()
      {
         MgProperties clonedProperties = new MgProperties();

         IEnumerator keysEnumerator = this.Keys.GetEnumerator();
         while (keysEnumerator.MoveNext())
         {
            String key = (String)keysEnumerator.Current;
            clonedProperties.Set(key, this[key]);
         }

         return clonedProperties;
      }
   }
}
