using com.magicsoftware.util;
using com.magicsoftware.gatewaytypes.data;

namespace com.magicsoftware.richclient.local.data.gateways.storage
{
   /// <summary>
   /// Base class for Storage converter
   /// </summary>
   internal class StorageBase
   {
      internal FldStorage FieldStorage;

      protected const string DateShortFormat = "YYMMDD";
      protected const string DateLongFormat = "YYYYMMDD";
      protected const int DateLongLength = 8;
      protected const string TimeFormat = "HHMMSS";
      protected const int TimeLength = 6;

      internal virtual object ConvertRuntimeFieldToGateway(DBField dbField, string runtimeValue)
      {
         return runtimeValue;
      }
     
      
      internal virtual object ConvertGatewayToRuntimeField(DBField dbField, object gatewayValue)
      {
         return gatewayValue;
      }

      protected string PadValue(object fieldValue, DBField field)
      {
         string paddedValue = null;
         if (fieldValue != null)
         {
            var value = fieldValue.ToString();
            paddedValue = value.PadRight(field.Length);
         }
         return paddedValue;
      }

   }
}
