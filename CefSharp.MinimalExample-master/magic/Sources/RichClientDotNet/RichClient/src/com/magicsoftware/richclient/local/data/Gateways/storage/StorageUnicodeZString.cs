using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.local.application.datasources;
using com.magicsoftware.util;

namespace com.magicsoftware.richclient.local.data.gateways.storage
{
   /// <summary>
   /// This class converts UnicodeZString values from gateway to runtime (and vice versa) 
   /// </summary>
   internal class StorageUnicodeZString : StorageBase, IConvertMgValueToGateway
   {
      /// <summary>
      /// CTOR
      /// </summary>
      internal StorageUnicodeZString()
      {
         FieldStorage = FldStorage.UnicodeZString;
      }

      public object ConvertMgValueToGateway(gatewaytypes.data.DBField dbField, object runtimeMgValue)
      {
         return runtimeMgValue;
      }

      internal override object ConvertGatewayToRuntimeField(gatewaytypes.data.DBField dbField, object gatewayValue)
      {
         return PadValue(gatewayValue, dbField);
      }

   }
}
