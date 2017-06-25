using System;

namespace com.magicsoftware.richclient.cache
{
   /// <summary>
   /// responsibilities:
   /// (1) access to persistent storage
   /// (2) encryption/decryption
   /// </summary>   
   internal interface ICacheManager
   {
      /// <summary>
      /// Save 'content' into the cache folder, identified by 'url', and set the file's time to 'remote Time'.
      /// </summary>
      /// <param name="url">URL to a cached file, excluding the server-side time stamp, e.g. /MG1VAI3T_MP_0$$$_$_$_N02400$$$$G8FM01_.xml.</param>
      /// <param name="content">content to save for future access to the url.</param>
      /// <param name="remoteTime">modification time of the file on the server-side.</param>
      void PutFile(String url, byte[] content, String remoteTime);

      /// <summary>
      /// get content (always decrypted) from the cache; if the cached content's loading time differs from 'remoteTime', remove the file from the cache folder & return null.
      /// </summary>
      /// <param name="url">URL to a cached file, excluding the server-side time stamp, e.g. /MG1VAI3T_MP_0$$$_$_$_N02400$$$$G8FM01_.xml.</param>
      /// <param name="remoteTime">modification time of the file on the server side.</param>
      /// <returns> content from the cache </returns>
      byte[] GetFile(String url, String remoteTime);

      /// <summary>
      /// get content from the cache.
      /// </summary>
      /// <param name="url">URL to a cached file, excluding the server-side time stamp, e.g. /MG1VAI3T_MP_0$$$_$_$_N02400$$$$G8FM01_.xml.</param>
      /// <returns>content from the cache</returns>
      byte[] GetFile(String url);

      ///<summary>
      /// check that an up-to-date content exists in the cache folder for 'url'.
      ///</summary>
      /// <param name="url">URL to a cached file, excluding the server-side time stamp, e.g. /MG1VAI3T_MP_0$$$_$_$_N02400$$$$G8FM01_.xml.</param>
      /// <param name="remoteTime">modification time of the file on the server side.</param>
      ///<returns>true if content exists for 'url' and matches 'remoteTime'.</returns>
      bool CheckFile(string url, string remoteTime);

      ///<summary>
      /// check that content exists in the cache folder for 'url'.
      ///</summary>
      /// <param name="url">URL to a cached file, excluding the server-side time stamp, e.g. /MG1VAI3T_MP_0$$$_$_$_N02400$$$$G8FM01_.xml.</param>
      ///<returns>true if content exists for 'url'.</returns>
      bool IsExists(string url);
   }
}