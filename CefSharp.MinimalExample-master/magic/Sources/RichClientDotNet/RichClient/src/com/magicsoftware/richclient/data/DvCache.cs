using System;
using System.Collections;
using System.Collections.Generic;
using com.magicsoftware.util;
using com.magicsoftware.richclient.tasks;
using Task = com.magicsoftware.richclient.tasks.Task;

namespace com.magicsoftware.richclient.data
{
   /// <summary>
   ///   this class represents the datastructure of the subforms dataview cache
   /// </summary>
   internal class DvCache
   {
      private readonly Hashtable _cacheTable; // the table that save all dataview in the cache
      private readonly List<long> _deletedList; // a list of all dvPos values (non hashed) that were deleted from the cache
      private readonly Task _task;
      private long _cacheSize;

      /// <summary>
      /// 
      /// </summary>
      /// <param name="tsk"></param>
      internal DvCache(Task tsk)
      {
         _task = tsk;
         _cacheTable = new Hashtable(100, 0.7F);
         _deletedList = new List<long>();
         _cacheSize = 0;
      }

      /// <summary>
      ///   puts a dataview object in the cache
      /// </summary>
      /// <param name = "repOfOriginal">a replicate of the dataview we wish to insert in cache </param>
      internal bool putInCache(DataView repOfOriginal)
      {
         Int64 hashKey = repOfOriginal.getDvPosValue();
         int insertedSize = repOfOriginal.getSize()*((Record)repOfOriginal.getCurrRec()).getRecSize();

         //if the new d.v already exists in cache remove the old entry and enter the new one
         if (_cacheTable[hashKey] != null)
            removeDvFromCache(hashKey, false);

         // time stamps the storded d.v put it in cache and update cache size
         repOfOriginal.setCacheLRU();
         repOfOriginal.taskModeFromCache = _task.getMode();
         repOfOriginal.zeroServerCurrRec();
         _cacheTable[hashKey] = repOfOriginal;

         //check s that there the new inserted data view does not appear in the del list
         //this could happen due to lru management
         _deletedList.Remove(hashKey);

         _cacheSize += insertedSize;
         return true;
      }

      /// <summary>
      ///   removes a dtatview from the cache
      /// </summary>
      /// <param name = "DvPosValue">the dvPos value which uniquely identify the dataview </param>
      internal bool removeDvFromCache(long DvPosValue, bool updateDel)
      {
         Int64 hashKey = DvPosValue;
         //locate the dataview object according to the given key
         DataView rep = (DataView) _cacheTable[hashKey];

         if (rep != null)
         {
            _cacheSize -= rep.getSize()*((Record)rep.getCurrRec()).getRecSize();

            if (updateDel)
               _deletedList.Add(DvPosValue);

            _cacheTable.Remove(hashKey);
            return true;
         }
         return false;
      }

      /// <summary>
      ///   constructs from the deleted list a string representation
      /// </summary>
      protected internal String getDeletedListToXML()
      {
         String list = "";

         for (int i = 0;
              i < _deletedList.Count;
              i++)
         {
            if (i != 0)
               list += ",";

            list += (_deletedList[i]).ToString();
         }

         return list;
      }

      /// <summary>
      ///   clears the deleted list
      ///   usally will be invoked after a server access
      /// </summary>
      protected internal void clearDeletedList()
      {
         _deletedList.RemoveRange(0, _deletedList.Count);
      }

      /// <summary>
      ///   gets a requested dataview
      /// </summary>
      /// <param name = "DvPosValue">the dvPos value which uniquely identify the dataview </param>
      /// <returns> the requested cached DataView object </returns>
      internal DataView getCachedDataView(long DvPosValue)
      {
         Int64 hashKey = DvPosValue;
         //locate the dataview object according to the given key
         DataView cached = ((DataView) _cacheTable[hashKey]);

         if (cached != null)
            cached = cached.replicate();
         return cached;
      }

      /// <summary>
      ///   get the hashed dvPosValue of the lru dataview in the cache
      /// </summary>
      private long getLastUsedDv()
      {
         long currentTimeStamp = Misc.getSystemMilliseconds();
         long lruDiff = -1;
         long lruDvPos = -1;
         DataView current = null;

         //goes over all elemnts in the hash table
         IEnumerator keysList = _cacheTable.Keys.GetEnumerator();
         while (keysList.MoveNext())
         {
            current = (DataView) _cacheTable[keysList.Current];
            long diff = currentTimeStamp - current.getCacheLRU();

            if (diff > lruDiff)
            {
               lruDiff = diff;
               lruDvPos = current.getDvPosValue();
            }
         }

         return lruDvPos;
      }

      /// <summary>
      ///   remove all records from cache
      ///   clear the cache of all sub forms too
      /// </summary>
      internal void clearCache()
      {
         //clears current cache
         long[] dvKeysArray = new long[_cacheTable.Count];
         _cacheTable.Keys.CopyTo(dvKeysArray, 0);

         for (int i = 0;
              i < dvKeysArray.Length;
              i++)
            removeDvFromCache(dvKeysArray[i], true);

         //clears all sub-forms cache
         if (_task.hasSubTasks())
         {
            TasksTable subs = _task.getSubTasks();

            for (int i = 0;
                 i < subs.getSize();
                 i++)
               subs.getTask(i).getTaskCache().clearCache();
         }

         //invalidate the current d.v so it will not enter the cache on return from server
         ((DataView) _task.DataView).setChanged(true);
      }

      protected internal bool isDeleted(long dvPosVal)
      {
         return _deletedList.Contains(dvPosVal);
      }
   }
}