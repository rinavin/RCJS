using System.Collections.Generic;
using com.magicsoftware.util;
using com.magicsoftware.gatewaytypes.data;

namespace com.magicsoftware.richclient.local.data.gateways.storage
{
   /// <summary>
   /// This class converts values from gateway to runtime (and vice versa) values according to the storage
   /// </summary>
   public class StorageConverter
   {
      // Dictionary of converting functions per storage
      private Dictionary<FldStorage, StorageBase> convertersDictionary = 
                                                   new Dictionary<FldStorage, StorageBase>();

      /// <summary>
      /// Converts the gateway value to the runtime value
      /// </summary>
      /// <param name="fldStorage"></param>
      /// <param name="gatewayValue"></param>
      /// <returns>converted value</returns>
      internal object ConvertGatewayToRuntimeField(DBField dbField, object gatewayValue)
      {
         return convertersDictionary[dbField.Storage].ConvertGatewayToRuntimeField(dbField, gatewayValue);
      }

      /// <summary>
      /// Converts the runtime value to the gateway value
      /// </summary>
      /// <param name="fldStorage"></param>
      /// <param name="runtimeValue"></param>
      /// <returns>converted value</returns>
      internal object ConvertRuntimeFieldToGateway(DBField dbField, string runtimeValue)
      {
         return runtimeValue != null ? convertersDictionary[dbField.Storage].ConvertRuntimeFieldToGateway(dbField, runtimeValue) : null;
      }

      /// <summary>
      /// Converts the runtime value to the gateway value
      /// </summary>
      /// <param name="fldStorage"></param>
      /// <param name="runtimeValue"></param>
      /// <returns>converted value</returns>
      internal object ConvertMgValueToGateway(DBField dbField, object runtimeValue)
      {
         if (runtimeValue != null)
         {
            if (runtimeValue is string)
               runtimeValue = ((string)runtimeValue).TrimEnd();
            runtimeValue = ((IConvertMgValueToGateway)convertersDictionary[dbField.Storage]).ConvertMgValueToGateway(dbField, runtimeValue);
         }
         return runtimeValue;
      }

      /// <summary>
      /// Adds a new row to converter dictionary
      /// </summary>
      /// <param name="storageTypeConverter"></param>
      internal void Add(StorageBase storageTypeConverter)
      {
         convertersDictionary.Add(storageTypeConverter.FieldStorage, storageTypeConverter);
      }

   }
}
