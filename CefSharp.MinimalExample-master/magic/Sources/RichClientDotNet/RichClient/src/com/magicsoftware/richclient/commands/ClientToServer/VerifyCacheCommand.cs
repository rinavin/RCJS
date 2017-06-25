using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.util;

namespace com.magicsoftware.richclient.commands.ClientToServer
{
   /// <summary>
   /// 
   /// </summary>
   class VerifyCacheCommand : ClientOriginatedCommand
   {
      internal Dictionary<string, string> CollectedOfflineRequiredMetadata { private get; set; }
      internal bool IsAccessingServerUsingHTTPS { private get; set; } // true if web requests from the client are made using the HTTPS protocol.

      protected override string CommandTypeAttribute
      {
         get { return ConstInterface.MG_ATTR_VAL_VERIFY_CACHE; }
      }

      /// <summary>
      /// </summary>
      /// <param name="hasChildElements"></param>
      /// <returns></returns>
      protected override string SerializeCommandData(ref bool hasChildElements)
      {
         CommandSerializationHelper helper = new CommandSerializationHelper();

         helper.SerializeAccessedCacheFiles(CollectedOfflineRequiredMetadata, ref hasChildElements, IsAccessingServerUsingHTTPS);

         return helper.GetString();
      }
   }
}
