using System;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.util;
using com.magicsoftware.gatewaytypes.data;
using com.magicsoftware.unipaas.management.data;
using System.Globalization;

namespace com.magicsoftware.richclient.local.data.gateways.storage
{
   /// <summary>
   /// This class converts numeric float values from gateway to runtime (and vice versa) 
   /// </summary>
   internal class StorageNumericFloat : StorageBase, IConvertMgValueToGateway
   {
      /// <summary>
      /// CTOR
      /// </summary>
      internal StorageNumericFloat()
      {
         FieldStorage = FldStorage.NumericFloat;
      }

      internal override object ConvertGatewayToRuntimeField(DBField dbField, object gatewayValue)
      {
         String numberString = gatewayValue.ToString();

         string systemNumberDecimalSeperator = CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator;
         char decimalSeparator = ClientManager.Instance.getEnvironment().GetDecimal();

         // temporarily exclude this code from Windows mobile. When handling offline and client side storage on Windows mobile
         // please handle this too
#if !PocketPC
         if (!decimalSeparator.Equals(systemNumberDecimalSeperator) && numberString.Contains(systemNumberDecimalSeperator))
            numberString = numberString.Replace(systemNumberDecimalSeperator, decimalSeparator.ToString());
#endif

         return DisplayConvertor.Instance.toNum(numberString, new PIC(dbField.Picture, StorageAttribute.NUMERIC, 0), 0);
      }

      internal override object ConvertRuntimeFieldToGateway(DBField dbField, string runtimeValue)
      {
         NUM_TYPE mgNum = new NUM_TYPE(runtimeValue);
         return ConvertMgValueToGateway(dbField, mgNum);
      }

      public object ConvertMgValueToGateway(DBField dbField, object runtimeValue)
      {
         return ((NUM_TYPE)(runtimeValue)).to_double();
      }

   }
}
