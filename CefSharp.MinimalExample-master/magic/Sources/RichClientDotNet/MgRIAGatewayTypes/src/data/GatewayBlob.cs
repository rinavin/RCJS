using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.util;

namespace com.magicsoftware.gatewaytypes.data
{
   public class GatewayBlob
   {
      public int BlobSize;
      public object Blob;
      public BlobContent blobContent;

      public GatewayBlob() {}
      public GatewayBlob(object blob, BlobContent blobContent)
      {
         this.Blob = blob;
         this.blobContent = blobContent;
         switch (blobContent)
         {
            case BlobContent.Binary:
               this.BlobSize = ((byte[])blob).Length;
               break;
            case BlobContent.Ansi:
               this.BlobSize = ((string)blob).Length;
               break;
            case BlobContent.Unicode:
               this.BlobSize = ((string)blob).Length * 2;
               break;
         }
      }
   }
}
