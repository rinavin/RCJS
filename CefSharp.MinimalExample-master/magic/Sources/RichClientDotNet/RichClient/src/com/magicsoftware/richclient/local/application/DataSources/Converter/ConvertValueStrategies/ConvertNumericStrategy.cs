using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.gatewaytypes.data;
using com.magicsoftware.gatewaytypes;
using com.magicsoftware.util;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.unipaas.management.data;

namespace com.magicsoftware.richclient.local.application.datasources.converter.convertValueStrategies
{
   /// <summary>
   /// Strategy for Numeric value conversion
   /// </summary>
   internal class ConvertNumericStrategy : IConvertValueStrategy
   {
      public void Convert(DBField sourceField, DBField destinationField, FieldValue sourceValue, FieldValue destinationValue)
      {
         switch ((StorageAttribute)destinationField.Attr)
         {
            case StorageAttribute.ALPHA:
            case StorageAttribute.UNICODE:
               var pic = new PIC(sourceField.Picture, StorageAttribute.NUMERIC, 0);
               destinationValue.Value = DisplayConvertor.Instance.mg2disp(sourceValue.Value.ToString(), null, pic, false, 0, true, false);
               break;
            
            case StorageAttribute.BOOLEAN:
               NUM_TYPE num = new NUM_TYPE(sourceValue.Value.ToString());
               destinationValue.Value = num.NUM_2_LONG() > 0 ? "1" : "0";
               break;

            case StorageAttribute.NUMERIC:
            case StorageAttribute.DATE:
            case StorageAttribute.TIME:
               destinationValue.Value = sourceValue.Value;
               break;
         }
      }
   }
}
