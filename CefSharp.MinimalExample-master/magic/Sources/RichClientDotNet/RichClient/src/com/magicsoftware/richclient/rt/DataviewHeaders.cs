using System;
using System.Collections;
using System.Text;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.richclient.util;
using com.magicsoftware.unipaas.util;
using com.magicsoftware.util;
using System.Collections.Specialized;
using System.Collections.Generic;
using util.com.magicsoftware.util;
using com.magicsoftware.util.Xml;

namespace com.magicsoftware.richclient.rt
{
   /// <summary>
   ///   handles all links in the task
   /// </summary>
   internal class DataviewHeaders
   {
      private readonly Hashtable _dataviewHeaders;
      private readonly Task _task;

      //initialize the links table of a given task
      internal DataviewHeaders(Task task)
      {
         _task = task;
         _dataviewHeaders = new Hashtable();
      }

      /// <summary>
      ///   this method will parse the xml data relate to the links table and will allocate and initilaize all related data
      /// </summary>
      protected internal void fillData()
      {
         XmlParser parser = ClientManager.Instance.RuntimeCtx.Parser;
         while (initInnerObjects(parser, parser.getNextTag()))
         {
         }
      }

      /// <summary>
      ///   allocates and initialize inner object according to the found xml data
      /// </summary>
      private bool initInnerObjects(XmlParser parser, String foundTagName)
      {
         if (foundTagName == null)
            return false;

         if (foundTagName.Equals(ConstInterface.MG_TAG_LINKS))
         {
            int endContext = parser.getXMLdata().IndexOf(ConstInterface.MG_TAG_LINKS_END, parser.getCurrIndex()) + ConstInterface.MG_TAG_LINKS_END.Length + 1;
            string xml = parser.getXMLsubstring(endContext);
            xml = XmlParser.escapeUrl(xml);

            // Activate the sax parser
            new DataviewHeadersSaxHandler(_task, this._dataviewHeaders, Encoding.UTF8.GetBytes(xml));

            // skip to after the links
            parser.setCurrIndex(endContext + XMLConstants.TAG_CLOSE.Length);
         }
         else
         {
            Logger.Instance.WriteExceptionToLog("There is no such tag in LinksTable.initInnerObjects(): " + foundTagName);
         }
         return false;
      }

      /// <summary>
      ///   gets a links by it id
      /// </summary>
      protected internal DataviewHeaderBase getDataviewHeaderById(int linkId)
      {
         return (DataviewHeaderBase) _dataviewHeaders[linkId];
      }

      /// <summary>
      ///   builds the db pos string of all links in the table
      ///   the format is @lnk1_dbPos@lnk2_dbPos@...
      /// </summary>
      internal String buildDbPosString()
      {
         if (_dataviewHeaders.Count == 0)
            return null;

         var buf = new StringBuilder();
         IEnumerator dataviewHeaders = _dataviewHeaders.Values.GetEnumerator();
         while (dataviewHeaders.MoveNext())
         {
            RemoteDataviewHeader curr = dataviewHeaders.Current as RemoteDataviewHeader;
            if (curr != null)
            {
               String currDbPosVal = curr.getLastFetchedDbPos();
               //((Link)allLinks.Value).getLastFetchedDbPos();
               buf.Append("@");
               if (currDbPosVal != null)
                  //if the last fetch db pos of the link is null it means that we never fetched a record from the link
                  buf.Append(currDbPosVal);
            }
         }
         buf.Append("@");
         return buf.ToString();
      }

      /// <summary>
      ///   returns the number of links in the table (only simple links are knowen to the client
      /// </summary>
      protected internal int getDataiewHeadersCount()
      {
         return _dataviewHeaders.Count;
      }

      /// <summary>
      /// returns true if we have local data on the task
      /// </summary>
      internal bool HasLocalData
      {
         get
         {
            return (Find(l => l is LocalDataviewHeader)) != null;
            
         }
        
      }

      internal bool HasLocalLinks
      {
         get
         {
            return (Find(l => l is LocalDataviewHeader && l.Id >= 0)) != null;

         }

      }

      /// <summary>
      /// returns true if we have remote data on the task
      /// </summary>
      internal bool HasRemoteData
      {
         get
         {
            return (Find(l => l is RemoteDataviewHeader)) != null;
            
         }
      }

      /// <summary>
      /// find links
      /// </summary>
      /// <param name="predicate"> find link according to the predicate</param>
      /// <returns></returns>
      internal DataviewHeaderBase Find(Predicate<DataviewHeaderBase> predicate)
      {
         IEnumerator dataviewHeaders = _dataviewHeaders.Values.GetEnumerator();
         while (dataviewHeaders.MoveNext())
         {
            DataviewHeaderBase dataviewHeader = (DataviewHeaderBase)dataviewHeaders.Current;
            if (predicate(dataviewHeader))
               return dataviewHeader;

         }
         return null;
      }


      /// <summary>
      /// find all links for which predicate is valid
      /// </summary>
      /// <param name="predicate"></param>
      /// <returns></returns>
      internal List<IDataviewHeader> FindAll(Predicate<IDataviewHeader> predicate)
      {
         List<IDataviewHeader> result = new List<IDataviewHeader>();
         IEnumerator allDataviewHeaders = _dataviewHeaders.Values.GetEnumerator();
         while (allDataviewHeaders.MoveNext())
         {
            IDataviewHeader dataviewHeader = (IDataviewHeader)allDataviewHeaders.Current;
            if (predicate(dataviewHeader))
               result.Add(dataviewHeader);

         }
         return result;
      }

   }   
}
