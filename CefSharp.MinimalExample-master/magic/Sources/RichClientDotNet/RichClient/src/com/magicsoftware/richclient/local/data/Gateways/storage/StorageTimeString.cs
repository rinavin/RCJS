using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.util;
using com.magicsoftware.gatewaytypes.data;

namespace com.magicsoftware.richclient.local.data.gateways.storage
{
   /// <summary>
   /// This class converts time string values from gateway to runtime (and vice versa) 
   /// </summary>
   internal class StorageTimeString : StorageBase, IConvertMgValueToGateway
   {
      /// <summary>
      /// CTOR
      /// </summary>
      internal StorageTimeString()
      {
         FieldStorage = FldStorage.TimeString;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="dbField"></param>
      /// <param name="gatewayValue"></param>
      /// <returns></returns>
      internal override object ConvertGatewayToRuntimeField(DBField dbField, object gatewayValue)
      {
         return DisplayConvertor.Instance.toTime((string)gatewayValue, new PIC(TimeFormat, StorageAttribute.TIME, 0));
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
         return DisplayConvertor.Instance.fromTime(((NUM_TYPE)runtimeValue).toXMLrecord(), new PIC(TimeFormat, StorageAttribute.TIME, 0), false);
      }
   }
}
