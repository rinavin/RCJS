using com.magicsoftware.util;
using com.magicsoftware.gatewaytypes.data;

namespace com.magicsoftware.richclient.local.data.gateways.storage
{
   /// <summary>
   /// This class converts logical values from gateway to runtime (and vice versa) 
   /// </summary>
   internal class StorageBooleanInteger : StorageBase, IConvertMgValueToGateway
   {
      /// <summary>
      /// CTOR
      /// </summary>
      internal StorageBooleanInteger()
      {
         FieldStorage = FldStorage.BooleanInteger;
      }

      internal override object ConvertGatewayToRuntimeField(DBField dbField, object gatewayValue)
      {
         return ((short)gatewayValue).ToString();
      }

      internal override object ConvertRuntimeFieldToGateway(DBField dbField, string runtimeValue)
      {
          short trueValue = 1;
          short falseValue = 0;
          return runtimeValue == "1" ? trueValue : falseValue;
      }

      public object ConvertMgValueToGateway(DBField dbField, object runtimeValue)
      {
         return runtimeValue;
      }

   }
}
