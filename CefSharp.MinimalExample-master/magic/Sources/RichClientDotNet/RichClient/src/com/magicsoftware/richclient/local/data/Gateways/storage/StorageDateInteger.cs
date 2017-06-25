using System;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.util;
using com.magicsoftware.gatewaytypes.data;

namespace com.magicsoftware.richclient.local.data.gateways.storage
{
   /// <summary>
   /// This class converts date integer values from gateway to runtime (and vice versa) 
   /// </summary>
   internal class StorageDateInteger : StorageBase, IConvertMgValueToGateway
   {
      /// <summary>
      /// CTOR
      /// </summary>
      internal StorageDateInteger()
      {
         FieldStorage = FldStorage.DateInteger;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="dbField"></param>
      /// <param name="gatewayValue"></param>
      /// <returns></returns>
      internal override object ConvertGatewayToRuntimeField(DBField dbField, object gatewayValue)
      {
         String dateString = DisplayConvertor.Instance.to_a(null, DateLongLength, (int)gatewayValue, DateLongFormat, 0);
         return DisplayConvertor.Instance.toDate(dateString,
                                                      new PIC(DateLongFormat, StorageAttribute.DATE, 0), 0);
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

      public object ConvertMgValueToGateway(DBField dbField, object runtimeValue)
      {
         string dateString = DisplayConvertor.Instance.fromDate(((NUM_TYPE)runtimeValue).toXMLrecord(), new PIC(DateLongFormat, StorageAttribute.DATE, 0), 0, false);
         return DisplayConvertor.Instance.a_2_date(dateString, DateLongFormat, 0);
      }
    
   }
}
