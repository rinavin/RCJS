using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.util;
using com.magicsoftware.unipaas.util;
using util.com.magicsoftware.util;
using com.magicsoftware.util.Xml;

namespace com.magicsoftware.richclient.util
{
   /// <summary> This class manages a dictionary of variables. then name of the variable is the key
   /// </summary>
   internal class PrmMap<TValue>
   {
      protected Dictionary<string, TValue > values;

      /// <summary> get the value
      /// 
      /// </summary>
      /// <param name="s"></param>
      /// <returns></returns>
      internal virtual TValue get(string s)
      {
         if (values.ContainsKey(s))
            return values[s];
         return default(TValue);
      }

      /// <summary> set
      /// </summary>
      virtual internal void set(string s, TValue v)
      {
         values[s] = v;
      }

      /// <summary> remove an entry
      /// </summary>
      virtual internal void remove(string s)
      {
         values.Remove(s);
      }

      /// <summary> CTOR
      /// </summary>
      internal PrmMap()
      {
         values = new Dictionary<string, TValue>();
      }

   }

   /// <summary> return values from the value init functions
   /// </summary>
   public enum ParamParseResult
   {
      OK,        // save the value as is
      TOUPPER,   // convert the name to upper case, so it would be case insensitive
      DELETE,    // remove the variable from the dictionary
      FAILED     // parsing error
   }

   /// <summary> Interface for mirroring a single item</summary>
   internal interface IMirrorXML
   {
      string mirrorToXML();
      ParamParseResult init(String name, XmlParser xmlParser);
   }

   /// <summary> This class manages the mirroring. It holds a list of changed values that need to be 
   /// mirrored, and manages the writing of those values to an XML buffer
   /// </summary>
   internal abstract class MirrorPrmMap<TValue> : PrmMap<TValue> where TValue : IMirrorXML, new()
   {
      // List of changed variables
      protected List<string> changes;

      // Type of variables in the table - use as an id in the XML buffer
      protected string mirroredID;

      /// <summary> CTOR
      /// </summary>
      internal MirrorPrmMap()
      {
         changes = new List<string>();
      }

      /// <summary> set
      /// </summary>
      internal void set(string s, TValue v, Boolean addToChanges)
      {
         if(addToChanges && !changes.Contains(s))
            changes.Add(s);
         base.set(s, v);
      }

      /// <summary> set
      /// </summary>
      override internal void set(string s, TValue v)
      {
         if (!changes.Contains(s))
            changes.Add(s);
         base.set(s, v);
      }

      /// <summary> remove
      /// </summary>
      override internal void remove(string s)
      {
         if(!changes.Contains(s))
            changes.Add(s);
         base.remove(s);
      }

      /// <summary>
      /// write to an XML buff: write the ID and call each changed variable to write itself
      /// </summary>
      internal String mirrorToXML()
      {
         StringBuilder xml = new StringBuilder();

         if (changes.Count > 0)
         {
            //write the id
            xml.Append("<" + mirroredID + ">");

            // loop on all changed variables
            foreach (string change in changes)
            {
               xml.Append("<" + ConstInterface.MG_TAG_PARAM + " " +
                  XMLConstants.MG_ATTR_NAME + "=\"" + XmlParser.escape(change) + "\" ");

               if (values.ContainsKey(change))
                  // call the variable to write it's own data
                  xml.Append(values[change].mirrorToXML());
               else
                  xml.Append("removed=\"Y\"");
               xml.Append(">");
            }
            xml.Append("</" + mirroredID + ">");
         }

         changes.Clear();

         return xml.ToString();
      }

      /// <summary> parse the XML and fill the data
      /// </summary>
      internal void fillData()
      {
         while (mirrorFromXML(ClientManager.Instance.RuntimeCtx.Parser.getNextTag(), ClientManager.Instance.RuntimeCtx.Parser))
         {
         }
      }

      /// <summary> fill from XML
      /// </summary>
      /// <param name="foundTagName"></param>
      /// <param name="xmlParser"></param>
      /// <returns></returns>
      internal Boolean mirrorFromXML(String foundTagName, XmlParser xmlParser)
      {
         if (foundTagName == null)
            return false;

         if (foundTagName.Equals(mirroredID))
         {
            xmlParser.setCurrIndex2EndOfTag();
            fillDataEntry(xmlParser);
            return true;
         }
         else if (foundTagName.Equals("/" + mirroredID))
         {
            // After updating from the server, clear the changes list so they won't be sent again
            changes.Clear();
            xmlParser.setCurrIndex2EndOfTag();
            return false;
         }
         else
         {
            Logger.Instance.WriteExceptionToLog("There is no such tag in MirrorPrmMap.mirrorFromXML(): " + foundTagName);
            return false;
         }
      }

      /// <summary> 
      /// parse and fill the params, until the section ends
      /// </summary>
      internal void fillDataEntry(XmlParser xmlParser)
      {
         String nextTag = xmlParser.getNextTag();

         // Loop while we have a parameter
         while (nextTag.Equals(ConstInterface.MG_TAG_PARAM))
         {
            int nameStart = xmlParser.getXMLdata().IndexOf(XMLConstants.MG_ATTR_NAME + "=\"", xmlParser.getCurrIndex()) +
                            XMLConstants.MG_ATTR_NAME.Length + 2; // skip ="
            xmlParser.setCurrIndex(nameStart);
            int nameEnd = xmlParser.getXMLdata().IndexOf("\"", nameStart);

            // get the variables data
            String name = xmlParser.getXMLsubstring(nameEnd).Trim();
            xmlParser.setCurrIndex(nameEnd);

            // create and init the new value
            TValue newVal = new TValue();

            switch (newVal.init(name, xmlParser))
            {
               case ParamParseResult.OK:
                  values[name] = newVal;
                  break;
               case ParamParseResult.TOUPPER:
                  values[name.ToUpper()] = newVal;
                  break;
               case ParamParseResult.FAILED:
                  break;
               case ParamParseResult.DELETE:
                  values.Remove(name);
                  break;
            }

            // move to next tag
            xmlParser.setCurrIndex2EndOfTag();
            nextTag = xmlParser.getNextTag();
         }
      }
   }
}
