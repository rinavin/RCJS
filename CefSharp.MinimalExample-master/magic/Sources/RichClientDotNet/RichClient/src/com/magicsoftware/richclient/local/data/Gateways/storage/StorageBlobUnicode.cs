using System;
using com.magicsoftware.gatewaytypes.data;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.util;
using System.Diagnostics;

namespace com.magicsoftware.richclient.local.data.gateways.storage
{
   internal class StorageBlobUnicode : StorageBase
   {
      internal StorageBlobUnicode()
      {
         FieldStorage = FldStorage.UnicodeBlob;
      }

      internal override object ConvertRuntimeFieldToGateway(DBField dbField, string runtimeValue)
      {
         Debug.Assert(BlobType.getContentType(runtimeValue) == BlobType.CONTENT_TYPE_UNICODE);
         GatewayBlob gatewayValue = new GatewayBlob();
         gatewayValue.Blob = BlobType.getString(runtimeValue);
         gatewayValue.blobContent = BlobContent.Unicode;
         gatewayValue.BlobSize = BlobType.getBlobSize(runtimeValue);
         return gatewayValue;
      }

      internal override object ConvertGatewayToRuntimeField(DBField dbField, object gatewayValue)
      {
         var gatewayBlob = (GatewayBlob)gatewayValue;
         return BlobType.createFromString((string)gatewayBlob.Blob, BlobType.CONTENT_TYPE_UNICODE);
      }
   }
}
