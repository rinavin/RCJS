using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.util;
using com.magicsoftware.util;
using Task = com.magicsoftware.richclient.tasks.Task;
using Constants = com.magicsoftware.util.Constants;
using com.magicsoftware.unipaas.management.data;
using util.com.magicsoftware.util;

namespace com.magicsoftware.richclient.data
{
   /// <summary>
   ///   an object of this class holds a collection of records
   /// </summary>
   internal class RecordsTable : IRecordsTable
   {
      // CONSTANTS
      internal const int REC_NOT_FOUND = -1;

      private readonly bool _useLinkedList;
      private Hashtable _hashTab;
      private int _initialCurrRecId = Int32.MinValue; // index of curr rec, as accepted at XML translation time
      private int _lastFetchedRecIdx = Int32.MinValue; // index of last fetched record
      private List<Record> _records;
      private Record _serverCurrRec; // the current rec as the server has it.

      /// <summary>
      ///   CTOR
      /// </summary>
      /// <param name = "withLinkedList">instructs us to maintain this table as a linked list with pointers 
      ///   from one item to another
      /// </param>
      protected internal RecordsTable(bool withLinkedList)
      {
         _records = new List<Record>();
         _hashTab = new Hashtable(100, 0.7F);
         _useLinkedList = withLinkedList;
         InsertedRecordsCount = Int32.MinValue;
      }

      /// <summary>
      ///   parse input string and fill inner data - returns the refresh type for the task
      /// </summary>
      /// <param name = "dataview">a reference to the dataview
      /// </param>
      protected internal char fillData(DataView dataview, char insertAt)
      {
         Record foundRec, prevRec, nextRec;
         String foundTagName;
         char taskRefreshType = Constants.TASK_REFRESH_NONE;
         bool isCurrRec;
         bool modified, computed;
         DataModificationTypes recMode;
         ObjectReferencesCollection firstDcRefs;
         Record LastRecord = null;
         bool ServerSentRecs = false;

         InsertedRecordsCount = 0;
         _initialCurrRecId = Int32.MinValue;

         foundTagName = ClientManager.Instance.RuntimeCtx.Parser.getNextTag();
         while (foundTagName != null && foundTagName.Equals(ConstInterface.MG_TAG_REC))
         {
            Record record = new Record(dataview);
            isCurrRec = record.fillData();
            if (isCurrRec && !dataview.HasMainTable)
            {
               using (firstDcRefs = record.getDcRefs())
               {
                  for (int i = 0; i < getSize(); i++)
                     getRecByIdx(i).setDcRefs(firstDcRefs);
               }
            }
            if (isCurrRec)
            {
               if (dataview.getTask().getMode() != Constants.TASK_MODE_CREATE)
                  record.setOldRec();
               _initialCurrRecId = record.getId();

               // save the current record as sent by the server.
               _serverCurrRec = record.replicate();

               if (taskRefreshType == Constants.TASK_REFRESH_NONE)
                  taskRefreshType = Constants.TASK_REFRESH_CURR_REC;
               else
                  taskRefreshType = Constants.TASK_REFRESH_FORM;
            }
            else
            {
               record.setOldRec();
               taskRefreshType = Constants.TASK_REFRESH_TABLE;
            }
            foundRec = getRecord(record.getId());
            if (foundRec != null)
            {
               bool updated = foundRec.Updated;
               modified = foundRec.Modified;
               recMode = foundRec.getMode();
               computed = foundRec.isComputed();
               prevRec = foundRec.getPrevRec();
               nextRec = foundRec.getNextRec();
               foundRec.setSameAs(record, false);
               foundRec.setPrevRec(prevRec);
               foundRec.setNextRec(nextRec);
               foundRec.setComputed(computed);
               if (modified)
                  foundRec.setModified();
               if (updated)
                  foundRec.setUpdated();
            }
            else
            {
               InsertRecord(insertAt, record);

               //If record being added at the top (0th position), decrement the recordsbeforeCurrentView.
               if (dataview.getTask().isTableWithAbsolutesScrollbar() && insertAt == 'B')
               {
                  //Empty dataview initial value of TotalRecordsCount is zero. And when first record being inserted, 
                  //through Task.insertRecordTable(). Else, the TotalRecordsCount gets incremented through MgForm.Addrec()
                  if (dataview.TotalRecordsCount == 0)
                     dataview.TotalRecordsCount += 1;

                  //Having locate operation with RecordsBeforeCurrentView > 0 and if moving in reverse direction, the updated
                  //counts are not retrieved again. Hence, decrement RecordsBeforeCurrentView as record gets added in dataview.
                  if (dataview.RecordsBeforeCurrentView > 0)
                     dataview.RecordsBeforeCurrentView -= 1;
               }

               // make sure that a new record created by the server is in 'insert' mode
               // and mark it as a 'computed' record
               if (isCurrRec && ((Task)dataview.getTask()).getMode() == Constants.TASK_MODE_CREATE)
                  record.setMode(DataModificationTypes.Insert);

               record.setComputed(true);
            }
            foundTagName = ClientManager.Instance.RuntimeCtx.Parser.getNextTag();
            if (!isCurrRec)
               LastRecord = record;

            ServerSentRecs = true;
         }
         // Currently RecordsTable include lonely record
         // We duplicate it record due to full chunk 
         // This mechanism is for No_main_table case only 
         for (int i = 0; !dataview.HasMainTable && LastRecord != null && i < dataview.getChunkSize() - 1; i++)
         {
            Record RecordN = new Record(dataview);
            RecordN.setSameAs(LastRecord, false, LastRecord.getId() + i + 1);
            RecordN.setOldRec();
            addRec(RecordN);
         }

         if (insertAt == 'B')
            _lastFetchedRecIdx += InsertedRecordsCount;

         // if server sent us a curr rec, then we already have it.
         // if no curr rec was sent, but other data was sent: we no longer know
         //    what curr rec the server has -> discard our curr rec
         // if no data was sent, the server curr rec is what we sent it last time.
         if (_initialCurrRecId == Int32.MinValue)
            if (ServerSentRecs)
               _serverCurrRec = null;
            else if (dataview.getCurrRec() != null)
               _serverCurrRec = ((Record)dataview.getCurrRec()).replicate();
            else
               _serverCurrRec = null;
         else
         {
         }

         return taskRefreshType;
      }

      /// <summary>
      /// insert Record into records Table
      /// </summary>
      /// <param name="insertAt"></param>
      /// <param name="record"></param>
      internal void InsertRecord(char insertAt, Record record)
      {
         if (insertAt == 'B')
         {
            InsertedRecordsCount++;
            insertRecord(record, 0);            
         }
         else
            addRec(record);
      }

      /// <summary>
      ///   build XML string for the records
      /// </summary>
      /// <param name = "fieldsTab">the fields table for the records table
      /// </param>
      /// <param name = "message">to build, add parts
      ///   <param name = "skipCurrRec">Skip current record
      ///     <param name = "currRecId">Current record ID
      ///     </param>
      protected internal void buildXML(FieldsTable fieldsTab, StringBuilder message, bool skipCurrRec, int currRecId)
      {
         Record rec, prevRec, orgPrevRec;
         int recIdx = REC_NOT_FOUND;

         for (int i = 0; i < _records.Count; i++)
         {
            // In case the previous record is not known to the server (OP_INSERT'ed) and is to be
            // dumped only after this record (it's idx is greater then 'i'), then temporary change 
            // my prev record so the server won't get confused.
            rec = _records[i];

            if (!rec.SendToServer || (skipCurrRec && rec.getId() == currRecId))
               continue;

            prevRec = orgPrevRec = rec.getPrevRec();
            // QCR #505277: The previous record may be unknown to the server but not in
            // the modified list yet. Therefore, if the previous record was not found
            // in that list then do another iteration of the loop.
            while (prevRec != null && (prevRec.getMode() == DataModificationTypes.Insert || !prevRec.SendToServer) &&
                   rec.getMode() == DataModificationTypes.Insert &&
                   ((recIdx = getRecIdx(prevRec.getId())) > i || recIdx == REC_NOT_FOUND || !prevRec.SendToServer))
            {
               prevRec = prevRec.getPrevRec();
               rec.setPrevRec(prevRec);
            }
            rec.buildXML(message, false);

            if (prevRec != orgPrevRec)
               rec.setPrevRec(orgPrevRec);
         }
      }

      /// <summary>
      ///   return a record by its id or null if the record was not found
      /// </summary>
      /// <param name = "id">the id of the record
      /// </param>
      /// <returns> Record the requested record
      /// </returns>
      protected internal Record getRecord(int id)
      {
         Record rec;
         Int32 hashKey = id;

         rec = (Record)_hashTab[hashKey];
         if (rec != null)
            _lastFetchedRecIdx = _records.IndexOf(rec);
         return rec;
      }

      /// <summary>
      ///   return a record index by its id - if no record was found (REC_NOT_FOUND) is returned
      /// </summary>
      /// <param name = "id">is the id of the record
      /// </param>
      /// <returns> the requested record index
      /// </returns>
      internal int getRecIdx(int id)
      {
         int i = REC_NOT_FOUND;
         Record rec;

         rec = getRecord(id);
         if (rec != null)
            i = _records.IndexOf(rec);
         return i;
      }

      /// <summary>
      ///   get a record by its index in the table
      /// </summary>
      /// <param name = "int">is the record index in the table
      /// </param>
      /// <returns> Record is the requested record
      /// </returns>
      protected internal Record getRecByIdx(int idx)
      {
         Record rec;

         if (idx < 0 || idx >= _records.Count)
         {
            Logger.Instance.WriteDevToLog("in RecordsTable.getRecByIdx() index out of bounds: " + idx);
            return null;
         }

         rec = _records[idx];
         _lastFetchedRecIdx = idx;
         return rec;
      }

      /// <summary>
      ///   get the last record fetched index
      /// </summary>
      /// <returns> the index of the last record fetched
      /// </returns>
      protected internal int getLastFetchedIdx()
      {
         return _lastFetchedRecIdx;
      }

      /// <summary>
      ///   remove all the records from the table
      /// </summary>
      protected internal void removeAll()
      {
         _records.Clear();
         _hashTab.Clear();
         _lastFetchedRecIdx = Int32.MinValue;
      }

      /// <summary>
      ///   get the number of records in the table
      /// </summary>
      protected internal int getSize()
      {
         return _records.Count;
      }

      /// <summary>
      ///   returns the number of records inserted in the begining of the table
      /// </summary>
      protected internal int InsertedRecordsCount { get; set; }


      /// <summary>
      ///   returns the current record, as was set by the XML (arriving from the server).
      ///   A Integer.MIN_VALUE indicates that it was not set at all.
      /// </summary>
      protected internal int getInitialCurrRecId()
      {
         return _initialCurrRecId;
      }

      /// <summary>
      ///   add a record to the end of the table - unless it is already in the table
      /// </summary>
      /// <param name = "rec">the record to add
      /// </param>
      protected internal void addRecord(Record rec)
      {
         if (_records.IndexOf(rec) < 0)
            addRec(rec);
      }

      /// <summary>
      ///   adds a record to the records table
      /// </summary>
      private void addRec(Record rec)
      {
         Record last = null;

         // If we maintain a linklist, then update the record before this one.
         if (_useLinkedList && _records.Count > 0)
         {
            last = _records[_records.Count - 1];
            last.setNextRec(rec);
            rec.setPrevRec(last);
         }

         _records.Add(rec);
         _hashTab[rec.getHashKey()] = rec;
      }


      /// <summary>
      ///   insert a record at the specified index
      /// </summary>
      /// <param name = "rec">the record to insert
      /// </param>
      /// <param name = "idx">the index of the new record
      /// </param>
      protected internal void insertRecord(Record rec, int idx)
      {
         Record prev = null;
         Record next = null;

         // If we maintain a linklist then update the records before and after this one
         if (_useLinkedList)
         {
            if (idx > 0)
            {
               prev = _records[idx - 1];
               rec.setPrevRec(prev);
               prev.setNextRec(rec);
            }

            if (idx != _records.Count)
            {
               next = _records[idx];
               rec.setNextRec(next);
               next.setPrevRec(rec);
            }
         }

         _records.Insert(idx, rec);
         _hashTab[rec.getHashKey()] = rec;
      }

      /// <summary>
      ///   remove a record from the table by its index in the table
      /// </summary>
      /// <param name = "recIdx">record index
      /// </param>
      protected internal void removeRecord(int recIdx)
      {
         Record rec, neighborRec;

         if (recIdx >= 0 && recIdx < _records.Count)
         {
            rec = _records[recIdx];

            // If we need to keep a linked list - update neighbor records.
            if (_useLinkedList)
            {
               //Update the record located before the record we are deleting
               if (recIdx > 0)
               {
                  neighborRec = _records[recIdx - 1];
                  neighborRec.setNextRec(rec.getNextRec());
               }

               // Update the record located after the record we are deleting
               if (recIdx + 1 < _records.Count)
               {
                  neighborRec = _records[recIdx + 1];
                  neighborRec.setPrevRec(rec.getPrevRec());
               }
            }

            _hashTab.Remove(rec.getHashKey());
            _records.RemoveAt(recIdx);
         }
         else
            throw new ApplicationException("in RecordsTable.removeRecord(): invalid index: " + recIdx);
      }

      /// <summary>
      ///   remove a record from the table
      /// </summary>
      /// <param name = "rec">a reference to the record to remove
      /// </param>
      protected internal void removeRecord(Record rec)
      {
         int recIdx = _records.IndexOf(rec);

         if (recIdx >= 0)
            removeRecord(recIdx);
      }

      /// <summary>
      ///   clones this Records Tab
      /// </summary>
      protected internal RecordsTable replicate()
      {
         RecordsTable rep = (RecordsTable)MemberwiseClone();
         rep._records = new List<Record>();
         rep._hashTab = new Hashtable(100, 0.7F);

         for (int i = 0; i < _records.Count; i++)
         {
            Record current = _records[i].replicate();
            rep._records.Add(current);
            rep._hashTab[current.getHashKey()] = current;
         }
         return rep;
      }

      protected internal Record getServerCurrRec()
      {
         return _serverCurrRec;
      }

      protected internal void zeroServerCurrRec()
      {
         _serverCurrRec = null;
      }


      #region IRecordsTable
      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      int IRecordsTable.GetSize()
      {
         return this.getSize();
      }

      Record IRecordsTable.GetRecByIdx(int idx)
      {
         return getRecByIdx(idx);
      }

      void IRecordsTable.RemoveAll()
      {
         this.removeAll();
      }

      #endregion IRecordsTable
   }
}
