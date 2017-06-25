using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.util;
using com.magicsoftware.gatewaytypes.data;

namespace com.magicsoftware.richclient.local.data.gateways.storage
{
   /// <summary>
   /// This class converts numeric signed values from gateway to runtime (and vice versa) 
   /// </summary>
   internal class StorageNumericSigned : StorageBase, IConvertMgValueToGateway
   {
      /// <summary>
      /// CTOR
      /// </summary>
      internal StorageNumericSigned()
      {
         FieldStorage = FldStorage.NumericSigned;
      }

      internal override object ConvertGatewayToRuntimeField(DBField dbField, object gatewayValue)
      {
         NUM_TYPE value = new NUM_TYPE();

         if (dbField.Length <= 2)
         {
            value.NUM_4_LONG((short)gatewayValue);
         }
         else
         {
            value.NUM_4_LONG((int)gatewayValue);
         }

         return value.toXMLrecord();
      }

      internal override object ConvertRuntimeFieldToGateway(DBField dbField, string runtimeValue)
      {
         NUM_TYPE mgNum = new NUM_TYPE(runtimeValue);
         return ConvertMgValueToGateway(dbField, mgNum);
      }

      public object ConvertMgValueToGateway(DBField dbField, object runtimeValue)
      {
         int value = ((NUM_TYPE)(runtimeValue)).NUM_2_ULONG();

         if (dbField.Length <= 2)
         {
            if (value > short.MaxValue)
               return short.MaxValue;
            else
               return (short)value;
         }

         return value;
      }

   }
}
