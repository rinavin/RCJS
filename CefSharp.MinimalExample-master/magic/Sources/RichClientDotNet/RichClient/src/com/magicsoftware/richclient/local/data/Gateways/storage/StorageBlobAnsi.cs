using System;
using com.magicsoftware.util;
using com.magicsoftware.gatewaytypes.data;
using com.magicsoftware.unipaas.management.data;
using System.Diagnostics;

namespace com.magicsoftware.richclient.local.data.gateways.storage
{
   internal class StorageBlobAnsi : StorageBase
   {
      internal StorageBlobAnsi()
      {
         FieldStorage = FldStorage.AnsiBlob;
      }

      internal override object ConvertRuntimeFieldToGateway(DBField dbField, string runtimeValue)
      {
         Debug.Assert(BlobType.getContentType(runtimeValue) == BlobType.CONTENT_TYPE_ANSI);
         GatewayBlob gatewayValue = new GatewayBlob();
         gatewayValue.Blob = BlobType.getString(runtimeValue);
         gatewayValue.blobContent = BlobContent.Ansi;
         gatewayValue.BlobSize = BlobType.getBlobSize(runtimeValue);
         return gatewayValue;
      }

      internal override object ConvertGatewayToRuntimeField(DBField dbField, object gatewayValue)
      {
         var gatewayBlob = (GatewayBlob)gatewayValue;
         return BlobType.createFromString((string)gatewayBlob.Blob, BlobType.CONTENT_TYPE_ANSI);
      }
   }
}
