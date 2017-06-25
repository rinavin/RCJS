using System;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.util;
using com.magicsoftware.gatewaytypes.data;

namespace com.magicsoftware.richclient.local.data.gateways.storage
{
   /// <summary>
   /// This class converts date string values from gateway to runtime (and vice versa) 
   /// </summary>
   internal class StorageDateString : StorageBase, IConvertMgValueToGateway
   {
      /// <summary>
      /// CTOR
      /// </summary>
      internal StorageDateString()
      {
         FieldStorage = FldStorage.DateString;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="dbField"></param>
      /// <param name="gatewayValue"></param>
      /// <returns></returns>
      internal override object ConvertGatewayToRuntimeField(DBField dbField, object gatewayValue)
      {
         String datePic = ((string)gatewayValue).Length == DateLongLength ? DateLongFormat : DateShortFormat;
         return DisplayConvertor.Instance.toDate((string)gatewayValue,
                                                      new PIC(datePic, StorageAttribute.DATE, 0), 0);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="dbField"></param>
      /// <param name="gatewayValue"></param>
      /// <returns></returns>
      internal override object ConvertRuntimeFieldToGateway(DBField dbField, string runtimeValue)
      {
         NUM_TYPE mgNum = new NUM_TYPE(runtimeValue);
         return ConvertMgValueToGateway(dbField, mgNum);  
      }

      public object ConvertMgValueToGateway(DBField dbField, object runtimeMgValue)
      {
         string fullDate = DisplayConvertor.Instance.fromDate(((NUM_TYPE)runtimeMgValue).toXMLrecord(), new PIC(DateLongFormat, StorageAttribute.DATE, 0), 0, false);

         string date = string.Empty;
         if(dbField.Length == 6)
         {
            date = string.Format("{0}", fullDate.Substring(2, 6));
         }
         else if(dbField.Length == 8)
         {
            date = fullDate;
         }

         return date;
      }
   }
}
