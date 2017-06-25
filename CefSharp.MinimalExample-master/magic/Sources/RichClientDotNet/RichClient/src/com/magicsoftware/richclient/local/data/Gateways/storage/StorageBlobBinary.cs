using System;
using com.magicsoftware.gatewaytypes.data;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.util;
using System.Diagnostics;

namespace com.magicsoftware.richclient.local.data.gateways.storage
{
   internal class StorageBlobBinary : StorageBase
   {
      internal StorageBlobBinary()
      {
         FieldStorage = FldStorage.Blob;
      }

      internal override object ConvertRuntimeFieldToGateway(DBField dbField, string runtimeValue)
      {
         GatewayBlob gatewayValue = new GatewayBlob();
         gatewayValue.Blob = BlobType.getBytes(runtimeValue);
         gatewayValue.blobContent = BlobContent.Binary;
         gatewayValue.BlobSize = BlobType.getBlobSize(runtimeValue);
         return gatewayValue;
      }

      internal override object ConvertGatewayToRuntimeField(DBField dbField, object gatewayValue)
      {
         var gatewayBlob = (GatewayBlob)gatewayValue;
         byte[] blobData = (gatewayBlob.BlobSize > 0 )? ((byte[])gatewayBlob.Blob) : null;
         return BlobType.createFromBytes(blobData, (char)dbField.BlobContent);
      }
   }
}
