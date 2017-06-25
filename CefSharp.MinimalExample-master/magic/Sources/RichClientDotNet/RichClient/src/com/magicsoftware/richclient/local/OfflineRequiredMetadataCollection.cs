using System;
using System.Collections.Generic;
using System.Diagnostics;
using com.magicsoftware.richclient.cache;
using com.magicsoftware.unipaas.util;

namespace com.magicsoftware.richclient.local
{
   /// <summary>
   /// This class handles spec section 6.1.1: Calling from Offline to non-offline program.
   /// "During the first access to the server from a session in status offline/local/disconnected to status remote/connected, 
   ///    a check will be made to see if the metadata [e.g. Main Program, offline programs, data sources, 
   ///    environment files (e.g. colors file…)] on the client is equal to the one on the server."
   /// </summary>
   internal class OfflineRequiredMetadataCollection
   {
      internal Boolean Enabled { private get; set; } // offline-required metadata will be collected only when the collection is enabled.
      internal Dictionary<string, string> CollectedMetadata { get; private set; } // offline-required metadata collected while the session status was offline (AKA local).

      /// <summary>
      /// CTOR
      /// </summary>
      internal OfflineRequiredMetadataCollection()
      {
         Reset();
      }

      /// <summary>
      /// record an offline-required metadata + its local time
      /// </summary>
      /// <param name="requestedURL">complete cache file retrieval request to an offline-required metadata, 
      /// e.g. http://[server]/[requester]?CTX=&CACHE=MG1VAI3T_MP_0$$$_$_$_N02400$$$$G8FM01_.xml|31/07/2013%2020:15:15</param>
      internal void Collect(string requestedURL)
      {
         if (Enabled)
         {
            string cachedFileServerFileName;
            string cachedFileLocalFileName;
            PersistentOnlyCacheManager.GetInstance().CompleteCacheRequestURLToFileNames(requestedURL, out cachedFileServerFileName, out cachedFileLocalFileName);
            Debug.Assert(HandleFiles.isExists(cachedFileLocalFileName));
            CollectedMetadata[cachedFileServerFileName] = HandleFiles.getFileTime(cachedFileLocalFileName);
         }
      }

      /// <summary>
      /// Creating a new instance of accessed cached files list
      /// </summary>
      internal void Reset()
      {
         CollectedMetadata = new Dictionary<string, string>();
      }
   }
}
