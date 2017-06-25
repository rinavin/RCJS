using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.util;
using com.magicsoftware.util;
using System.Collections.Specialized;
using com.magicsoftware.richclient.tasks;
using System.Collections;
using com.magicsoftware.util.Xml;

namespace com.magicsoftware.richclient.rt
{
   /// <summary>
   /// sax parser for parsing link collection
   /// </summary>
   class DataviewHeadersSaxHandler : MgSAXHandlerInterface
   {

      Hashtable _dataviewHeaders;
      Task _task;
      DataviewHeaderFactory _dataviewHeadersFactory;

      /// <summary>
      /// CTOR - create and activate the parsing process
      /// </summary>
      /// <param name="dataSourceDefinition"></param>
      /// <param name="xmlData"></param>
      public DataviewHeadersSaxHandler(Task task, Hashtable dataviewHeaders, byte[] xmlData)
      {
         _dataviewHeaders = dataviewHeaders;
         _task = task;
         _dataviewHeadersFactory = new DataviewHeaderFactory();

         MgSAXHandler mgSAXHandler = new MgSAXHandler(this);
         mgSAXHandler.parse(xmlData);
      }

      #region MgSAXHandlerInterface
      /// <summary>
      /// When the parser encounters the end of an element, it calls this method
      /// </summary>
      /// <param name="elementName"></param>
      /// <param name="elementValue"></param>
      /// <param name="attributes"></param>
      public void endElement(string elementName, string elementValue, NameValueCollection attributes)
      {
         String valueStr = attributes[ConstInterface.MG_ATTR_TABLE_INDEX];

         if (valueStr != null)
         {
            int tableIndex = XmlParser.getInt(valueStr);
            DataviewHeaderBase dataviewHeader = _dataviewHeadersFactory.CreateDataviewHeaders(_task, tableIndex);
            attributes.Remove(ConstInterface.MG_ATTR_TABLE_INDEX);
            dataviewHeader.SetAttributes(attributes);
            _dataviewHeaders[dataviewHeader.Id] = dataviewHeader;
         }
      }
      #endregion
   }
}
