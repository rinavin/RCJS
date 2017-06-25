using System;
using System.Collections.Generic;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.util;
using com.magicsoftware.unipaas.util;
using util.com.magicsoftware.util;
using com.magicsoftware.util.Xml;

namespace com.magicsoftware.richclient.data
{
   /// <summary>
   ///   data for <dvheader ...>...</dvheader>
   /// </summary>
   internal class FieldsTable : unipaas.management.data.FieldsTable
   {
      private int  _rmIdx;
      private int  _rmSize = -1;
      private bool _serverRcmpDone;

      /// <summary>
      ///   a different version of fill data in order to be used in table cache
      /// </summary>
      public void fillData()
      {
         XmlParser parser = ClientManager.Instance.RuntimeCtx.Parser;
         while (initFields(parser.getNextTag()))
         {
         }
      }

      /// <summary></summary>
      /// <param name="foundTagName"></param>
      /// <returns></returns>
      private bool initFields(String foundTagName)
      {
         if (foundTagName == null)
            return false;

         if (foundTagName.Equals(XMLConstants.MG_TAG_FLDH))
         {
            FieldDef field = initField();
            _fields.Add(field);
         }
         else
         {
            //ClientManager.Instance.WriteErrorToLog("in FieldsTable.initFields() only field tag are allowed to be parsed at this instance");
            return false;
         }

         return true;
      }

      /// <summary>
      /// create a field and fill it by data
      /// </summary>
      /// <param name = "dataview"> Dataview </param>
      /// <returns> the created filled field </returns>
      protected override FieldDef initField(DataViewBase dataview)
      {
         var field = new Field((DataView)dataview, _fields.Count);
         field.fillData();
         return field;
      }

      /// <summary>
      /// create a field and fill it by data
      /// </summary>
      /// <returns> the created filled field </returns>
      protected FieldDef initField()
      {
         var field = new FieldDef(_fields.Count);
         field.fillData();
         return field;
      }

      /// <summary>
      ///   return the number of fields in the Record Main
      /// </summary>
      internal int getRMSize()
      {
         if (_rmSize == -1)
            return _fields.Count;
         return _rmSize;
      }

      /// <summary>
      ///   the index of the first field in the record main
      /// </summary>
      internal int getRMIdx()
      {
         return _rmIdx;
      }

      /// <summary>
      ///   set Record Main size
      /// </summary>
      /// <param name = "rms">the new record main size</param>
      /// <param name = "idx">the index of the first field in the record main</param>
      internal void setRMPos(int rms, int idx)
      {
         if (rms < 0 && _rmSize != -1)
            Logger.Instance.WriteExceptionToLog("in FieldsTable.setRMSize(): illegal record main size: " + rms);
         else if (_rmSize >= 0)
            Logger.Instance.WriteExceptionToLog("in FieldsTable.setRMSize(): record main size already set !");
         else
         {
            _rmSize = rms;
            _rmIdx = idx;
         }
      }

      /// <summary>
      ///   get the size of a field by its id
      /// </summary>
      /// <param name = "id">the id of the requested field</param>
      /// <returns> the size of the field</returns>
      protected internal int getSizeOfField(int id)
      {
         var fld = getField(id);
         if (fld != null)
            return (fld.getSize());

         Logger.Instance.WriteExceptionToLog("FieldTable.GetSizeOfField() field id: " + id);
         return 0;
      }

      /// <summary>
      ///   get the type of field by its index
      /// </summary>
      /// <param name = "id">the id of the field</param>
      /// <returns> type of the requested field</returns>
      protected internal StorageAttribute getType(int id)
      {
         return getField(id).getType();
      }

      /// <summary>
      ///   invalidate some or all fields in the table
      /// </summary>
      /// <param name = "forceInvalidate">if true then force the invalidation of all the fields</param>
      /// <param name="clearFlags"></param>
      internal void invalidate(bool forceInvalidate, bool clearFlags)
      {
         foreach (Field field in _fields)
            field.invalidate(forceInvalidate, clearFlags);
      }

      /// <summary>
      ///   take the value of the fields from the current record
      /// </summary>
      internal void takeValsFromRec()
      {
         foreach (Field field in _fields)
            field.takeValFromRec();
      }

      /// <summary>
      ///   return TRUE if we went to the server during a field's compute
      /// </summary>
      internal bool serverRcmpDone()
      {
         return _serverRcmpDone;
      }

      /// <summary>
      ///   set the value of the "we went to the server for rcmp" indication
      /// </summary>
      /// <param name = "val">new value</param>
      internal void setServerRcmp(bool val)
      {
         _serverRcmpDone = val;
      }

      /// <summary>
      ///   return a vector of all fields in the table that belongs to a given link
      /// </summary>
      /// <param name = "lnkId">the id of the link</param>
      internal List<Field> getLinkFields(int lnkId)
      {
         var linkedFields = new List<Field>();

         ///check virtual
         foreach (Field field in _fields)
            if (field.getDataviewHeaderId() == lnkId)
               linkedFields.Add(field);

         return linkedFields;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      internal void resetRecomp()
      {
         foreach (Field field in _fields)
            field.setRecompute(null);
      }
   }
}
