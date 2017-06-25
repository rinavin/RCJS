using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.util;
using com.magicsoftware.richclient.local.application.datasources;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.gatewaytypes.data;

namespace com.magicsoftware.richclient.local.data.gateways.storage
{
   /// <summary>
   /// This class converts time integer values from gateway to runtime (and vice versa) 
   /// </summary>
   internal class StorageTimeInteger : StorageBase, IConvertMgValueToGateway
   {
      /// <summary>
      /// CTOR
      /// </summary>
      internal StorageTimeInteger()
      {
         FieldStorage = FldStorage.TimeInteger;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="dbField"></param>
      /// <param name="gatewayValue"></param>
      /// <returns></returns>
      internal override object ConvertGatewayToRuntimeField(DBField dbField, object gatewayValue)
      {
         String timeString = DisplayConvertor.Instance.time_2_a(null, TimeLength, (int)gatewayValue, TimeFormat, 0, false);
         return DisplayConvertor.Instance.toTime(timeString, new PIC(TimeFormat, StorageAttribute.TIME, 0));
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
         String timeString = DisplayConvertor.Instance.fromTime(((NUM_TYPE)runtimeValue).toXMLrecord(), new PIC(TimeFormat, StorageAttribute.TIME, 0), false);
         return DisplayConvertor.Instance.a_2_time(timeString, new PIC(TimeFormat, StorageAttribute.TIME, 0), false);
      }
   }
}
