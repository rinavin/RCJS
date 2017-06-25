using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.util;

namespace com.magicsoftware.richclient.local.data.gateways.storage
{
   /// <summary>
   /// This class converts AlphaZString values from gateway to runtime (and vice versa) 
   /// </summary>
   internal class StorageAlphaZString : StorageBase, IConvertMgValueToGateway
   {
      /// <summary>
      /// CTOR
      /// </summary>
      internal StorageAlphaZString()
      {
         FieldStorage = FldStorage.AlphaZString;
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
