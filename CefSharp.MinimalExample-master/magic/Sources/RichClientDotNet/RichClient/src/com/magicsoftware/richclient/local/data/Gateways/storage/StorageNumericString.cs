using System;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.util;
using com.magicsoftware.gatewaytypes.data;
using com.magicsoftware.unipaas.management.gui;
using System.Globalization;

namespace com.magicsoftware.richclient.local.data.gateways.storage
{
   /// <summary>
   /// This class converts numeric string values from gateway to runtime (and vice versa) 
   /// </summary>
   internal class StorageNumericString : StorageBase, IConvertMgValueToGateway
   {
      /// <summary>
      /// CTOR
      /// </summary>
      internal StorageNumericString()
      {
         FieldStorage = FldStorage.NumericString;
      }

      internal override object ConvertGatewayToRuntimeField(DBField dbField, object gatewayValue)
      {
         NUM_TYPE runtimeValue = new NUM_TYPE();
         String numberString = gatewayValue.ToString();
         if (dbField.Dec > 0)
         {
            string systemNumberDecimalSeperator = CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator;
            char decimalSeparator = ClientManager.Instance.getEnvironment().GetDecimal();

            // temporarily exclude this code from Windows mobile. When handling offline and client side storage on Windows mobile
            // please handle this too
#if !PocketPC
            if (!decimalSeparator.Equals(systemNumberDecimalSeperator) && numberString.Contains(systemNumberDecimalSeperator))
               numberString = numberString.Replace(systemNumberDecimalSeperator, decimalSeparator.ToString());
#endif
         }
         runtimeValue.num_4_a_std((string)numberString);
         return runtimeValue.toXMLrecord();
      }

      internal override object ConvertRuntimeFieldToGateway(DBField dbField, string gatewayValue)
      {
         NUM_TYPE A = new NUM_TYPE(gatewayValue);
         //return ((NUM_TYPE)gatewayValue).to_double().ToString();
         return ConvertMgValueToGateway(dbField, A);
      }


      public object ConvertMgValueToGateway(DBField dbField, object gatewayValue)
      {
         string val = ((NUM_TYPE)gatewayValue).to_a(new PIC(dbField.Picture, (StorageAttribute)dbField.Attr, 0));
         if (dbField.Dec > 0)
         {
            string systemNumberDecimalSeperator = CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator;
            char magicDecimalSeparator = ClientManager.Instance.getEnvironment().GetDecimal();

            // temporarily exclude this code from Windows mobile. When handling offline and client side storage on Windows mobile
            // please handle this too
#if !PocketPC
            if (!magicDecimalSeparator.Equals(systemNumberDecimalSeperator) && val.Contains(magicDecimalSeparator.ToString()))
               val = val.Replace(magicDecimalSeparator.ToString(), systemNumberDecimalSeperator);
#endif
         }
         return val;
      }
   }
}
