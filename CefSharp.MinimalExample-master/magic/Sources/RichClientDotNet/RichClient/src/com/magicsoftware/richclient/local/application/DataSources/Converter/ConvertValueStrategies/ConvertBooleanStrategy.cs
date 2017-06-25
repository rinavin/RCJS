using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.gatewaytypes.data;
using com.magicsoftware.gatewaytypes;
using com.magicsoftware.util;
using com.magicsoftware.unipaas.management.gui;

namespace com.magicsoftware.richclient.local.application.datasources.converter.convertValueStrategies
{
   /// <summary>
   /// Strategy for Boolean value conversion
   /// </summary>
   internal class ConvertBooleanStrategy : IConvertValueStrategy
   {
      public void Convert(DBField sourceField, DBField destinationField, FieldValue sourceValue, FieldValue destinationValue)
      {
         switch ((StorageAttribute)destinationField.Attr)
         {
            case StorageAttribute.ALPHA:
            case StorageAttribute.UNICODE:
            case StorageAttribute.BOOLEAN:
               destinationValue.Value = sourceValue.Value;
               break;
            case StorageAttribute.NUMERIC:
               destinationValue.Value = DisplayConvertor.Instance.toNum(sourceValue.Value.ToString(), null, 0);
               break;
         }
      }
   }
}
