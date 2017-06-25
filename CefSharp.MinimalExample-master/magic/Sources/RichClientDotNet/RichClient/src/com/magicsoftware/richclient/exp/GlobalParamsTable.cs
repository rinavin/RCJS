using System;
using com.magicsoftware.richclient.data;
using com.magicsoftware.richclient.util;
using com.magicsoftware.unipaas.util;
using com.magicsoftware.util;
using util.com.magicsoftware.util;
using com.magicsoftware.util.Xml;

namespace com.magicsoftware.richclient.exp
{
   // mirroring a single global parameter
   internal class MirrorExpVal : ExpressionEvaluator.ExpVal, IMirrorXML
   {
      /// <summary>
      ///   CTOR
      /// </summary>
      public MirrorExpVal()
         : base()
      {
      }

      /// <summary>
      ///   CTOR
      /// </summary>
      internal MirrorExpVal(ExpressionEvaluator.ExpVal val)
      {
         Copy(val);
      }

      #region IMirrorXML Members

      public string mirrorToXML()
      {
         bool toBase64 = (ClientManager.Instance.getEnvironment().GetDebugLevel() <= 1);

         StorageAttribute cellAttr = (Attr == StorageAttribute.BLOB_VECTOR
                                         ? VectorField.getCellsType()
                                         : StorageAttribute.SKIP);

         //Story 131961 : Do not serialize dotnet params
          string value = "";
          if (Attr != StorageAttribute.DOTNET)
              value = Record.itemValToXML(ToMgVal(), Attr, cellAttr, toBase64);

         return ConstInterface.MG_ATTR_PAR_ATTRS + "=\"" + Attr + "\" " + ConstInterface.MG_ATTR_LEN + "=" +
                value.Length + " " + ConstInterface.MG_ATTR_ENV_VALUE + "=\"" + value + "\"";
      }

      /// <summary>
      ///   initialize the object from the XML buffer
      /// </summary>
      /// <param name = "xmlParser"></param>
      public ParamParseResult init(String name, XmlParser xmlParser)
      {
         int valueOffset, attrOffset, lenOffset, valueLen, lenOffsetEnd, count;
         StorageAttribute currType;
         Boolean isNull;
         String paramAttr, paramValue, correctValue = null;

         // Look for parameter attributes. Xml parser's current index is set to end of Name. 
         // So look for attribute (i.e. "attr") from current index to the end of tag (i.e. "/>")
         count = xmlParser.getXMLdata().IndexOf(XMLConstants.TAG_TERM, xmlParser.getCurrIndex()) -
                 xmlParser.getCurrIndex();
         attrOffset = xmlParser.getXMLdata().IndexOf(ConstInterface.MG_ATTR_PAR_ATTRS, xmlParser.getCurrIndex(), count);

         if (attrOffset != -1)
         {
            // add tag length and get the attribute
            attrOffset += ConstInterface.MG_ATTR_PAR_ATTRS.Length + 2;
            paramAttr = xmlParser.getXMLdata().Substring(attrOffset, 1);

            // length of value
            lenOffset = xmlParser.getXMLdata().IndexOf(ConstInterface.MG_ATTR_LEN, attrOffset);
            lenOffset += ConstInterface.MG_ATTR_LEN.Length + 1;
            lenOffsetEnd = xmlParser.getXMLdata().IndexOf(" ", lenOffset);
            valueLen = XmlParser.getInt(xmlParser.getXMLdata().Substring(lenOffset, lenOffsetEnd - lenOffset));

            // param value
            valueOffset = xmlParser.getXMLdata().IndexOf(ConstInterface.MG_ATTR_ENV_VALUE, lenOffset);
            valueOffset += ConstInterface.MG_ATTR_ENV_VALUE.Length + 2;
            paramValue = xmlParser.getXMLdata().Substring(valueOffset, valueLen);

            // get data for this object
            currType = (StorageAttribute)paramAttr[0];
            correctValue = Record.getString(paramValue, currType);
            isNull = false;

            // skip to after this param
            xmlParser.setCurrIndex(valueOffset + valueLen);
         }
         else if (xmlParser.getXMLdata().IndexOf(ConstInterface.MG_ATTR_ENV_REMOVED, xmlParser.getCurrIndex()) != -1)
         {
            return ParamParseResult.DELETE;
         }
         else
         {
            currType = StorageAttribute.NONE;
            isNull = true;
         }

         base.Init(currType, isNull, correctValue);

         return ParamParseResult.TOUPPER;
      }

      #endregion
   }

   /// <summary>
   ///   manager for global parameters
   /// </summary>
   internal class GlobalParams : MirrorPrmMap<MirrorExpVal>
   {
      /// <summary>
      ///   CTOR
      /// </summary>
      internal GlobalParams()
      {
         mirroredID = ConstInterface.MG_TAG_GLOBALPARAMSCHANGES;
      }

      /// <summary>
      ///   create a new MirrorExpVal and add it to the map
      /// </summary>
      /// <param name = "s"></param>
      /// <param name = "v"></param>
      internal void set(string s, ExpressionEvaluator.ExpVal v)
      {
         if (v.IsNull || v.isEmptyString())
            base.remove(s.Trim().ToUpper());
         else
            base.set(s.Trim().ToUpper(), new MirrorExpVal(v));
      }

      internal override MirrorExpVal get(string s)
      {
         try
         {
            return base.get(s.Trim().ToUpper());
         }
         catch
         {
            return null;
         }
      }
   }
}