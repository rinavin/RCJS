using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.util;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.richclient.util;
using util.com.magicsoftware.util;
using com.magicsoftware.util.Xml;
using com.magicsoftware.gatewaytypes.data;

namespace com.magicsoftware.richclient.data
{
   /// <summary>
   /// This class is specially designed for DataViewToDataSource Opeartion. As in case of Server To Client communication
   /// data is sent from server in the format which gateway requires. This data can be of any type. Record class always save data in the
   /// string format. So to save the data in any format this class is introduced.
   /// </summary>
   internal class RecordForDataViewToDataSource : Record
   {
      private Object[] _fieldsData;
      private List<DBField> destinationColumnList;

      /// <summary>
      ///   get a field value
      /// </summary>
      /// <param name = "fldIdx">the field index</param>
      /// <returns> String the field value</returns>
      new public object GetFieldValue(int fldIdx)
      {
         object val = null;

         if (fldIdx >= 0 && fldIdx < _fieldsData.Length)
            val = _fieldsData[fldIdx];
         return val;
      }

      internal RecordForDataViewToDataSource(object dvOrTableCache, List<DBField> destinationColumnList)
         : base(dvOrTableCache)
      {
         _fieldsData = new Object[getFieldsTab().getSize()];
         this.destinationColumnList = destinationColumnList;
      }

      /// <summary>
      /// Fill the fields data.
      /// </summary>
      /// <param name="fldValInBytes"></param>
      /// <param name="recFlags"></param>
      /// <param name="isCurrRec"></param>
      protected override void fillFieldsData(byte[] fldValInBytes, String recFlags)
      {
         Object val = null;
         String tmp = null;
         int parsedLen = 0;
         int i, j, from, size;
         StorageAttribute currType;
         bool useHex;
         FieldDef fld = null;
         bool valueNotPassed;

         from = getFromFldIdx(false);
         size = getSizeFld(false);
         int destinationFieldIndex = 0;
         for (i = from, j = 0; j < size; i++, j++)
         {
            fld = getFieldsTab().getField(i);
            currType = fld.getType();

            useHex = (ClientManager.Instance.getEnvironment().GetDebugLevel() > 1 ||
                      currType == StorageAttribute.ALPHA ||
                      currType == StorageAttribute.UNICODE ||
                      StorageAttributeCheck.isTypeLogical(currType));

            // Qcr #940443 : Old fashion flags are still being used by resident table. So check them 1st coz we  might get exception otherwise.
            // The flags for resident are very simple, only 2 options. '.' = false, '/' = true.
            // We keep using it for resident, since in the server side we do not know if we are creating the xml for richclient or for BC at the creation stage.
            // Since we couldn't create different flags for rich, we will use the old flags here.
            if ((byte)(recFlags[j]) == '.')
               _flags[i] = 0;
            else if ((byte)(recFlags[j]) == '/')
               _flags[i] = 1;
            else // New flags style. For the view.
            {
               // Each flag will appear in the xml in 2 chars representing hex value ("42" for 0x42).
               tmp = recFlags.Substring(j * 2, 2);
               _flags[i] = Convert.ToByte(tmp, 16);
            }

            // save the ind that the value was not passed from the server.
            valueNotPassed = (FLAG_VALUE_NOT_PASSED == (byte)(_flags[i] & FLAG_VALUE_NOT_PASSED));
            _flags[i] = (byte)(_flags[i] & ~FLAG_VALUE_NOT_PASSED);

            _flagsHistory[i] = _flags[i];

            if (valueNotPassed)
            {
               if (FLAG_NULL == (byte)(_flags[i] & FLAG_NULL))
               {
                  // null ind is on, just put any value in the field.
                  val = fld.getDefaultValue();
               }
               else
               {
                  // copy the existing value from the existing curr rec.
                  val = ((Record)_dataview.getCurrRec()).GetFieldValue(i);
               }
            }
            else
            {
               DBField dbField = destinationColumnList[destinationFieldIndex++];
               val = RecordUtils.deSerializeItemVal(fldValInBytes, (StorageAttribute)dbField.Attr, dbField.Length, dbField.Storage, ref parsedLen);
            }

            _fieldsData[i] = val;
         }
         setInitialFldVals(false, false);
      }
   }
}
