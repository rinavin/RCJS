using System;
using System.Collections.Generic;
using com.magicsoftware.richclient.data;
using com.magicsoftware.richclient.util;
using com.magicsoftware.util;
using com.magicsoftware.unipaas.util;
using com.magicsoftware.unipaas.management.data;
using RCFieldsTable = com.magicsoftware.richclient.data.FieldsTable;
using util.com.magicsoftware.util;
using com.magicsoftware.util.Xml;

namespace com.magicsoftware.richclient.rt
{
   /// <summary>
   ///   resident table on client
   /// </summary>
   internal class TableCache
   {
      private readonly List<Key> _keys;
      private readonly List<Record> _records;
      private char _currSortDir;
      private int _currSortKey;
      internal RCFieldsTable FldsTab { private set; get; }
      private bool _isLoaded;
      private String _tableIdent; //a table identifier this id if share between all instances of a resident table used to delete old instnces of a reloaded table;
      private String _tableUId; //the table unique id used to load the table - it is also the id of the data-island containing the table xml

      /// <summary>
      ///   constructs an empty table cache without loading the table
      /// </summary>
      protected internal TableCache(String tableId)
      {
         _tableUId = tableId;
         _keys = new List<Key>();
         _records = new List<Record>();
         _isLoaded = false;
      }

      /// <summary>
      ///   set the table shared identifier will be called directly before the constructor and before parsing
      /// </summary>
      protected internal void SetTableIdent(String ident)
      {
         if (_tableIdent == null)
            _tableIdent = ident;
         else if (!_tableIdent.Equals(ident))
            Logger.Instance.WriteExceptionToLog(
               "in TableCache.setTableIdent() already set and table identifier  does not match");
      }

      /// <summary>
      ///   parses the xml data of the table cache as part of the load process
      /// </summary>
      protected internal void FillData()
      {
         XmlParser parser = ClientManager.Instance.RuntimeCtx.Parser;
         FillAttributes(parser);
         while (InitInnerObjects(parser, parser.getNextTag()))
         {
         }
      }

      /// <summary>
      ///   parses the attributes of the cachedTable tag
      /// </summary>
      private void FillAttributes(XmlParser parser)
      {
         int endContext = parser.getXMLdata().IndexOf(XMLConstants.TAG_CLOSE, parser.getCurrIndex());
         if (endContext != -1 && endContext < parser.getXMLdata().Length)
         {
            // last position of its tag
            string tag = parser.getXMLsubstring(endContext);
            parser.add2CurrIndex(tag.IndexOf(ConstInterface.MG_TAG_CACHED_TABLE) +
                                                                ConstInterface.MG_TAG_CACHED_TABLE.Length);

            List<String> tokensVector = XmlParser.getTokens(parser.getXMLsubstring(endContext), XMLConstants.XML_ATTR_DELIM);

            //parse the cachedTable attributes
            for (int j = 0; j < tokensVector.Count; j += 2)
            {
               string attribute = (tokensVector[j]);
               string valueStr = (tokensVector[j + 1]);

               switch (attribute)
               {
                  case XMLConstants.MG_ATTR_ID:
                     //the id of the table is recived at the constructor and does not need to be parsed
                     //however issue a warnning if the parsed value is different than the current value
                     if (_tableUId.IndexOf(valueStr) == -1)
                     {
                        _tableUId = valueStr;
                        Logger.Instance.WriteExceptionToLog("in TableCache.fillAttributes() table unique id does not match");
                     }
                     break;

                  case XMLConstants.MG_ATTR_NAME:
                     break;

                  case ConstInterface.MG_ATTR_IDENT:
                     if (!_tableIdent.Equals(valueStr))
                     {
                        _tableIdent = valueStr;
                        Logger.Instance.WriteExceptionToLog(
                           "in TableCache.fillAttributes() table identifier id does not match");
                     }
                     break;

                  default:
                     Logger.Instance.WriteExceptionToLog(string.Format("Unrecognized attribute: '{0}'", attribute));
                     break;
               }
            }
            parser.setCurrIndex(++endContext); // to delete ">" too
            return;
         }
         Logger.Instance.WriteExceptionToLog("in TableCache.fillAttributes() out of string bounds");
      }

      /// <summary>
      ///   allocates and initialize inner object according to the found xml data
      /// </summary>
      private bool InitInnerObjects(XmlParser parser, String foundTagName)
      {
         if (foundTagName == null)
            return false;

         switch (foundTagName)
         {
            case XMLConstants.MG_TAG_FLDH:
               FldsTab = new RCFieldsTable();
               FldsTab.fillData();
               break;

            case ConstInterface.MG_ATTR_KEY:
               {
                  var current = new Key(this);
                  current.FillData();
                  _keys.Add(current);
                  break;
               }

            case ConstInterface.MG_TAG_RECORDS:
               parser.setCurrIndex(
                  parser.getXMLdata().IndexOf(XMLConstants.TAG_CLOSE, parser.getCurrIndex()) + 1);
                  //end of outer tad and its ">"
               break;

            case ConstInterface.MG_TAG_RECORDS_END:
               parser.setCurrIndex2EndOfTag();
               break;

            case ConstInterface.MG_TAG_CACHED_TABLE_END:
               parser.setCurrIndex2EndOfTag();
               return false;

            case ConstInterface.MG_TAG_REC:
               {
                  var current = new Record(this);
                  current.fillData();
                  _records.Add(current);
                  break;
               }
         }
         return true;
      }

      /// <summary>
      ///   returns the unique id of the current cached table
      /// </summary>
      protected internal String GetTableUniqueId()
      {
         return _tableUId;
      }

      /// <summary>
      ///   return the table common identifier
      /// </summary>
      protected internal String GetTableCommonIdentifier()
      {
         return _tableIdent;
      }

      /// <summary>
      ///   returns the key by whic we are currently sorting (or currently sorted) the table
      /// </summary>
      internal int GetCurrSortKey()
      {
         return _currSortKey;
      }

      /// <summary>
      ///   returns the key by id
      /// </summary>
      internal Key GetKeyById(int keyId)
      {
         for (int i = 0; i < _keys.Count; i++)
         {
            if (_keys[i].GetKeyId() == keyId)
               return _keys[i];
         }
         return null;
      }

      /// <summary>
      ///   loads a table from the dataIsland according to is unique id
      /// </summary>
      private void Load()
      {
         if (!_isLoaded)
         {
            ClientManager.Instance.getTableCacheManager().LoadTable(_tableUId);
            _isLoaded = true;
         }
      }

      /// <summary>
      ///   sort the current table
      /// </summary>
      /// <param name="sortKeyId">the id of the key to sort by </param>
      /// <param name="sortDir"></param>
      protected internal void SortTable(int sortKeyId, char sortDir)
      {
         if (!_isLoaded)
            Load();

         //if the table is already sorted the way we want it
         if (sortKeyId != _currSortKey || sortDir != _currSortDir)
         {
            _currSortDir = sortDir;
            _currSortKey = sortKeyId;

            var recordsArr = new Record[_records.Count];
            _records.CopyTo(recordsArr);
            HeapSort.sort(recordsArr);

            if (_currSortDir == 'D')
               ReverseVector(recordsArr);

            for (int i = 0; i < _records.Count; i++)
               _records[i] = recordsArr[i];
         }
      }

      /// <summary>
      ///   executes the the link operation we assume the table is already sorted by the correct key and direction
      ///   the method returns the requested record according the locates
      /// </summary>
      protected internal Record Fetch(List<Boundary> loc)
      {
         //if there are not any locate expression then either we return the ffirst record that accepts
         //al the range condition or we return the first record in the table according to the current sort
         bool checkLoc = (loc.Count != 0);

         if (_isLoaded)
            //should be loaded at sort time the time must be already sorted at this stage
         {
            if (!checkLoc)
               //if there are no locates return the first record
               return (_records.Count != 0 ? _records[0] : null);

            //find the first record that agrees with the locates ant return it
            for (int i = 0; i < _records.Count; i++)
            {
               Record currRec = _records[i];
               //check if this record agrees with all the locate conditions
               if (validateRec(currRec, loc))
                  return currRec;
            }
         }

         //if we got here then either the table was not loaded and sorted or we did not find any record
         //that agrees with the locate expressions - in either case return null
         return null;
      }

      /// <summary>
      ///   validate a record data against a series of locate expressions
      /// </summary>
      private static bool validateRec(Record currRec, List<Boundary> rangeCond)
      {
         bool res = true;

         //go over all the conditions
         for (int i = 0; i < rangeCond.Count; i++)
         {
            Boundary currCnd = rangeCond[i];
            //if there is at least one filed that does not agree all the record dies not agree
            if (!currCnd.checkRange(currRec.GetFieldValue(currCnd.getCacheTableFldId()), currRec.IsNull(currCnd.getCacheTableFldId())))
            {
               res = false;
               break;
            }
         }

         return res;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="array"></param>
      protected internal void ReverseVector(Object[] array)
      {
         int right = array.Length - 1;
         for (int left = 0; left < (array.Length - 1)/2; left++, right--)
         {
            object tmp = array[left];
            array[left] = array[right];
            array[right] = tmp;
         }
      }
   }
}
