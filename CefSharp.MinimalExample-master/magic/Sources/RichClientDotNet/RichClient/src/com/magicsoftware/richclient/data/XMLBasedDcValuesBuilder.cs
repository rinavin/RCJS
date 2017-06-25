using System;
using System.Collections.Generic;
using com.magicsoftware.richclient.util;
using com.magicsoftware.util;
using com.magicsoftware.unipaas.util;
using com.magicsoftware.unipaas.management.data;
using System.Text;
using com.magicsoftware.unipaas;
using util.com.magicsoftware.util;
using com.magicsoftware.util.Xml;

namespace com.magicsoftware.richclient.data
{
   /// <summary>
   ///   a class that holds a data controls displayed and linked values each instance is a unique set of values
   ///   identified by a unique id
   /// </summary>
   internal class XMLBasedDcValuesBuilder : DcValuesBuilderBase
   {
      DcValues dcv;
      XmlParser parser;

      public string SerializedDCVals 
      { 
         set
         {
            parser.setXMLdata(value);
            parser.setCurrIndex(0);
         }
      }

      public XMLBasedDcValuesBuilder()
      {
         parser = new XmlParser();
      }

      public override DcValues Build()
      {
         dcv = null;

         int endContext = parser.getXMLdata().IndexOf(XMLConstants.TAG_TERM, parser.getCurrIndex());

         if (endContext != -1 && endContext < parser.getXMLdata().Length)
         {
            // last position of its tag
            String tag = parser.getXMLsubstring(endContext);
            parser.add2CurrIndex(tag.IndexOf(ConstInterface.MG_TAG_DC_VALS) + ConstInterface.MG_TAG_DC_VALS.Length);

            List<String> tokensVector = XmlParser.getTokens(parser.getXMLsubstring(endContext), XMLConstants.XML_ATTR_DELIM);
            dcv = (DcValues)CreateDcValues(false);
            InitDCValues(dcv, tokensVector);
         }
         return dcv;
      }

      /// <summary>
      /// Initialize the DcValues according to the values set in the tokens vector.
      /// </summary>
      /// <param name="tokensVector">A set of attribute value pairs built by the XmlParser.</param>
      private void InitDCValues(DcValues dcv, List<string> tokensVector)
      {
         StorageAttribute type = StorageAttribute.NONE;
         string[] displayValues = null;
         string serializedLinkValues = null;
         
         for (int j = 0; j < tokensVector.Count; j += 2)
         {
            String attribute = (tokensVector[j]);
            String valueStr = (tokensVector[j + 1]);

            switch (attribute)
            {
               case XMLConstants.MG_ATTR_ID:
                  SetId(dcv, XmlParser.getInt(valueStr));
                  break;

               case XMLConstants.MG_ATTR_TYPE:
                  type = (StorageAttribute)valueStr[0];
                  SetType(dcv, type);
                  break;

               case ConstInterface.MG_ATTR_DISP:
                  valueStr = XmlParser.unescape(valueStr);
                  displayValues = ParseValues(valueStr, StorageAttribute.UNICODE);
                  break;

               case ConstInterface.MG_ATTR_LINKED:
                  // optional tag
                  valueStr = XmlParser.unescape(valueStr);
                  serializedLinkValues = valueStr;
                  break;

               case ConstInterface.MG_ATTR_NULL_FLAGS:
                  SetNullFlags(dcv, valueStr);
                  break;

               default:
                  Logger.Instance.WriteExceptionToLog("in DcValues.initElements() unknown attribute: " + attribute);
                  break;
            }
         }
         SetDisplayValues(dcv, displayValues);
         if (serializedLinkValues == null)
            SetLinkValues(dcv, displayValues);
         else
            SetLinkValues(dcv, ParseValues(serializedLinkValues, type));
      }

      /// <summary>
      /// This overload of the ParseValues method simplifies the use of the original method by
      /// determining the value of the 'useHex' parameter based on the ClientManager's log level and
      /// the type of the serialized values.
      /// </summary>
      /// <param name = "valueStr">The serialized values as received from the XML</param>
      /// <param name = "dataType">The storage type of the servialized values.</param>
      private String[] ParseValues(String valueStr, StorageAttribute dataType)
      {
         bool useHex = false;

         if (ClientManager.Instance.getEnvironment().GetDebugLevel() > 1 || dataType == StorageAttribute.ALPHA ||
             dataType == StorageAttribute.UNICODE || dataType == StorageAttribute.BOOLEAN)
            useHex = true;

         return ParseValues(valueStr, dataType, useHex);
      }


   }
}
