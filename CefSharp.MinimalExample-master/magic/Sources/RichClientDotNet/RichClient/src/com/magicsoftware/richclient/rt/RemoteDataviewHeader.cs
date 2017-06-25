using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.unipaas.util;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.data;
using com.magicsoftware.richclient.tasks;

namespace com.magicsoftware.richclient.rt
{
   /// <summary>
   /// link to Remote table
   /// </summary>
   internal class RemoteDataviewHeader : DataviewHeaderBase
   {
      private TableCache _table;
      private String _lastFetchRecPos; //the db pos of the last fetched record from the table cache

      public RemoteDataviewHeader(Task task): base(task)      { }
      protected override void setAttribute(string attribute, string valueStr)
      {
         switch (attribute)
         {

            case ConstInterface.MG_ATTR_CACHED_TABLE:
               if (ClientManager.Instance.getTableCacheManager().TableExists(valueStr))
                  _table = ClientManager.Instance.getTableCacheManager().GetTableById(valueStr);
               else
               {
                  _table = new TableCache(valueStr);
                  ClientManager.Instance.getTableCacheManager().InsertTable(_table);
               }
               break;
            case ConstInterface.MG_ATTR_IDENT:
               _table.SetTableIdent(valueStr);
               break;
           

            default:
               base.setAttribute(attribute, valueStr);
               break;

         }
         
      }

      /// <summary>
      ///   will return the linked record according to the link properties and the current record
      ///   this method will change the current record fields values
      /// </summary>
      internal override bool getLinkedRecord(Record curRec)
      {
         var lnkFlds = ((FieldsTable)Task.DataView.GetFieldsTab()).getLinkFields(_id);
         bool ret = false;
         if (_cond.getVal())
         {
            //build locate and range vectors
            if (Loc == null)
            {
               Loc = new List<Boundary>();
               for (int i = 0; i < lnkFlds.Count; i++)
               {
                  Boundary currLoc = lnkFlds[i].Locate;
                  if (currLoc != null)
                  {
                     Loc.Add(currLoc);
                     currLoc.compute(false);
                  }
               }
            }
            else
            {
               //compute the range and locate expressions anew
               for (int i = 0; i < Loc.Count; i++)
                  Loc[i].compute(false);
            }

            //sort the table (if the table was not loaded yet it will also load it
            _table.SortTable(_keyIdx, _dir);
            //perform the link
            Record res = _table.Fetch(Loc);

            if (res == null)
               //link faild
               initRec(curRec, lnkFlds, false);
            else
            {
               ret = true;
               copyLinkFldToRec(curRec, lnkFlds, res, true);
            }
         }
         //calculate init expressions if there are any or default values
         else
            initRec(curRec, lnkFlds, false); //every scenario where the linked did not 
         //returned any record fro example here when 
         //the link condition is false then the link is 
         //considered to has failed and should return 
         //false as return value

         return ret;
      }

      /// <summary>
      ///   if the link succeeded copies the values return from the linked table record 
      ///   to the current data view record
      /// </summary>
      private void copyLinkFldToRec(Record curRec, List<Field> linkFlds, Record tableRec, bool ret)
      {
         //if we got here it means that we have successfully fetch a record from the link
         //set the lastFetchRecPos to be the pos of the fetched record
         _lastFetchRecPos = tableRec.getDbPosVal();

         for (int i = 0; i < linkFlds.Count; i++)
         {
            Field curFld = linkFlds[i];

            curRec.setFieldValue(curFld.getId(), tableRec.GetFieldValue(curFld.CacheTableFldIdx), false);

            //clear the invalid flag in case it was set
            curRec.clearFlag(curFld.getId(), Record.FLAG_INVALID);

            curFld.invalidate(true, false);
         }

         //set the ret value
         SetReturnValue(curRec, ret, true);
      }

      
      /// <summary>
      ///   in case the link fails set the value to default or to the value of the init expression
      /// </summary>
      private void initRec(Record currRec, List<Field> linkFlds, bool ret)
      {
         //if we are here it means that we tried to fetch a record but faild to find one therefor
         //the lastFetchRecPos should be marked accordingly
         _lastFetchRecPos = "#";

         InitLinkFields(currRec);

         //set the ret value
         var retFld = ReturnField;
         if (retFld != null)
         {
            currRec.setFieldValue(retFld.getId(), (ret
                                                      ? "1"
                                                      : "0"), false);
            retFld.invalidate(true, false);
         }
      }


      /// <summary>
      ///   returns the last fetch record db pos value
      /// </summary>
      internal String getLastFetchedDbPos()
      {
         return _lastFetchRecPos;
      }
   }
}
