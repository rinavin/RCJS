using System;
using System.Collections.Generic;
using com.magicsoftware.richclient.util;
using com.magicsoftware.unipaas.util;
using com.magicsoftware.util;
using util.com.magicsoftware.util;
using com.magicsoftware.util.Xml;

namespace com.magicsoftware.richclient.rt
{
   internal class CompMainPrgTable
   {
      private String[] _ctlIdxTab; // table of the ctl_idx of the components

      /// <summary>
      ///   To part tag <compmainprg value = "string">
      ///                 and save only string
      /// </summary>
      internal void fillData()
      {
         XmlParser parser = ClientManager.Instance.RuntimeCtx.Parser;
         int endContext = parser.getXMLdata().IndexOf(XMLConstants.TAG_TERM, parser.getCurrIndex());
         if (endContext != -1 && endContext < parser.getXMLdata().Length)
         {
            // find last position of its tag
            String tag = parser.getXMLsubstring(endContext);
            parser.add2CurrIndex(tag.IndexOf(ConstInterface.MG_TAG_COMPMAINPRG) +
                                                                ConstInterface.MG_TAG_COMPMAINPRG.Length);

            List<string> tokensVector = XmlParser.getTokens(parser.getXMLsubstring(endContext),
                                                            XMLConstants.XML_ATTR_DELIM);
            Logger.Instance.WriteDevToLog("in CompMainPrg.FillData: " + Misc.CollectionToString(tokensVector));

            initElements(tokensVector);
            parser.setCurrIndex(endContext + XMLConstants.TAG_TERM.Length); // to delete "/>" too
            return;
         }
         Logger.Instance.WriteExceptionToLog("in CompMainPrg.FillData() out of string bounds");
      }

      /// <summary>
      ///   Fill inner elements of the class from tokensVector
      /// </summary>
      /// <param name = "tokensVector">attribute/value/...attribute/value/ vector</param>
      private void initElements(List<String> tokensVector)
      {
         for (int j = 0; j < tokensVector.Count; j += 2)
         {
            string attribute = (tokensVector[j]);
            string valueStr = (tokensVector[j + 1]);

            if (attribute.Equals(XMLConstants.MG_ATTR_VALUE))
            {
               valueStr = StrUtil.DeleteStringsFromEnds(valueStr, "\"");
               initCtlIdxTab(valueStr);
            }
            else
               Logger.Instance.WriteExceptionToLog(
                  string.Format("There is no such index in CompMainPrg. Add case to CompMainPrg.initElements for {0}",
                                attribute));
         }
         Logger.Instance.WriteDevToLog("End CompMainPrg ");
      }

      /// <summary>
      ///   init ctlIdxTab vector by variables from a comma-separated input string
      /// </summary>
      /// <param name = "val">comma-separated string </param>
      private void initCtlIdxTab(String val)
      {
         if (string.IsNullOrEmpty(val))
            return;
         if (val.IndexOf(",") != -1)
            _ctlIdxTab = StrUtil.tokenize(val, ",");
         else
         {
            _ctlIdxTab = new String[1];
            _ctlIdxTab[0] = val;
         }
      }

      /// <summary>
      ///   get CtlIdx by index in List
      /// </summary>
      /// <param name = "idx">of requested ctl idx</param>
      internal int getCtlIdx(int idx)
      {
         if (idx >= 0 && idx < getSize())
         {
            string strIdx = _ctlIdxTab[idx];
            return Int32.Parse(strIdx);
         }
         return -1;
      }

      /// <summary>
      ///   return the index in order of search of the ctl idx
      /// </summary>
      internal int getIndexOf(int ctlIdx)
      {
         int i;

         for (i = 0; i < getSize(); i++)
         {
            if (ctlIdx == getCtlIdx(i))
               return i;
         }
         return -1;
      }

      /// <summary>
      ///   get size of ctlidx list
      /// </summary>
      protected internal int getSize()
      {
         if (_ctlIdxTab == null)
            return 0;
         return _ctlIdxTab.GetLength(0);
      }
   }
}
