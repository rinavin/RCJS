using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.util;
using com.magicsoftware.richclient.remote;

namespace com.magicsoftware.richclient.rt
{
   /// <summary>
   ///   handles the loading and managing of all cache tables in the context
   /// </summary>
   internal class TableCacheManager
   {
      private readonly Hashtable _tables;

      internal TableCacheManager()
      {
         _tables = new Hashtable();
      }

      /// <summary>
      ///   return the tableCache object  according to its unique id or null if does not exist in the manager
      /// </summary>
      protected internal TableCache GetTableById(String tableUId)
      {
         return (TableCache) _tables[tableUId];
      }

      /// <summary>
      ///   checks if a given table exists in the manager
      /// </summary>
      protected internal bool TableExists(String tableUId)
      {
         bool res = true;

         if (GetTableById(tableUId) == null)
            res = false;

         return res;
      }

      /// <summary>
      ///   inserts a table into the manager is not already exists
      ///   returns true is insert succeeded false otherwise
      /// </summary>
      protected internal bool InsertTable(TableCache table)
      {
         bool res = false;

         if (!TableExists(table.GetTableUniqueId()))
         {
            _tables[table.GetTableUniqueId()] = table;
            res = true;
         }

         return res;
      }

      /// <summary>
      ///   loads a new table start the table parsing process 
      ///   delete any old instances of the same table (even if they other unique ids
      /// </summary>
      protected internal void LoadTable(String tableUId)
      {
         CommandsProcessorBase server = RemoteCommandsProcessor.GetInstance();

         // the table must exist in the hashTable at this stage else it is an error
         if (TableExists(tableUId))
         {
            //get the table
            var current = (TableCache) _tables[tableUId];
            try
            {
               byte[] residentTableContent = server.GetContent(tableUId, true);
               try
               {
                  String residentTableContentStr = Encoding.UTF8.GetString(residentTableContent, 0,
                                                                           residentTableContent.Length);
                  ClientManager.Instance.RuntimeCtx.Parser.loadTableCacheData(residentTableContentStr);
               }
               catch (Exception ex)
               {
                  throw new ApplicationException(
                     "Unable to parse resident table " + tableUId + ", due to unsupported encoding.", ex);
               }
            }
            catch (Exception ex)
            {
               Misc.WriteStackTrace(ex, Console.Error);
            }

            //start parsing
            current.FillData();

            //delete old tables
            var deletedUids = new List<String>();
            IEnumerator enm = _tables.Values.GetEnumerator();
            while (enm.MoveNext())
            {
               var tbl = (TableCache) enm.Current;
               String currIdent = tbl.GetTableCommonIdentifier();
               if (currIdent.Equals(current.GetTableCommonIdentifier()) && (tbl.GetTableUniqueId() != tableUId))
                  deletedUids.Add(tbl.GetTableUniqueId());
            }
            //delete
            for (int i = 0; i < deletedUids.Count; i++)
               _tables.Remove(deletedUids[i]);
         }
      }
   }
}