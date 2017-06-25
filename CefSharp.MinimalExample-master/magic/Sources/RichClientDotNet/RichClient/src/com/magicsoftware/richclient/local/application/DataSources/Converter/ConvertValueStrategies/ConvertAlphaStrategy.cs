using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.gatewaytypes.data;
using com.magicsoftware.gatewaytypes;
using com.magicsoftware.util;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.unipaas.management.gui;

namespace com.magicsoftware.richclient.local.application.datasources.converter.convertValueStrategies
{
   /// <summary>
   /// Strategy for Alpha value conversion.
   /// </summary>
   internal class ConvertAlphaStrategy : IConvertValueStrategy
   {
      public void Convert(DBField sourceField, DBField destinationField, FieldValue sourceValue, FieldValue destinationValue)
      {
         switch ((StorageAttribute)destinationField.Attr)
         {
            case StorageAttribute.ALPHA:
            case StorageAttribute.UNICODE:
               destinationValue.Value = sourceValue.Value;
               break;
            case StorageAttribute.NUMERIC:
               destinationValue.Value = DisplayConvertor.Instance.toNum(sourceValue.Value.ToString(), new PIC(destinationField.Picture, StorageAttribute.NUMERIC, 0), 0);
               break;
            case StorageAttribute.DATE:
               destinationValue.Value = DisplayConvertor.Instance.toDate((string)sourceValue.Value, new PIC(destinationField.Picture, StorageAttribute.DATE, 0), 0);
               break;
            case StorageAttribute.TIME:
               destinationValue.Value = DisplayConvertor.Instance.toTime((string)sourceValue.Value, new PIC(destinationField.Picture, StorageAttribute.TIME, 0));
               break;
         }
      }
   }
}
