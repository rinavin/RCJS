using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.gatewaytypes.data;
using com.magicsoftware.gatewaytypes;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.util;

namespace com.magicsoftware.richclient.local.application.datasources.converter.convertValueStrategies
{
   /// <summary>
   /// Strategy for Blob value conversion
   /// </summary>
   internal class ConvertBlobStrategy : IConvertValueStrategy
   {
      public void Convert(DBField sourceField, DBField destinationField, FieldValue sourceValue, FieldValue destinationValue)
      {
         switch(destinationField.Storage)
         {
            case FldStorage.AnsiBlob:
               destinationValue.Value = BlobType.createFromString(string.Empty, BlobType.CONTENT_TYPE_ANSI);
               break;
            case FldStorage.UnicodeBlob:
               destinationValue.Value = BlobType.createFromString(string.Empty, BlobType.CONTENT_TYPE_UNICODE);
               break;
            case FldStorage.Blob:
               destinationValue.Value = BlobType.createFromString(string.Empty, BlobType.CONTENT_TYPE_BINARY);
               break;
         }
         destinationValue.Value = BlobType.copyBlob(destinationValue.Value.ToString(), sourceValue.Value.ToString());
      }
   }
}
