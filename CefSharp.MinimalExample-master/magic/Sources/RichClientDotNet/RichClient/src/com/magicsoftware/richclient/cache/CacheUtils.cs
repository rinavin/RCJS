using System;
using com.magicsoftware.util;
using System.IO;
using com.magicsoftware.httpclient;
using System.Text;
using com.magicsoftware.richclient.util;
using System.Diagnostics;
#if PocketPC
using com.magicsoftware.richclient.mobile.util;
#endif

namespace com.magicsoftware.richclient.cache
{
   internal class CacheUtils
   {
      /// <returns>prepare the name of the folder in which cache files will be written</returns>
      internal static String GetRootCacheFolderName()
      {
         String cacheFolder = null;

#if !PocketPC
         // First check for client cache folder from execution properties
         cacheFolder = ClientManager.Instance.GetClientCachePath();
#endif

         // Use Temp folder from system for client cache folder, if failed to get it from execution properties
         if (string.IsNullOrEmpty(cacheFolder))
            cacheFolder = GetEnvTempFolder() + @"\MgxpaRIACache";

         return cacheFolder;
      }

      /// <summary>Prepare the cache folder's name(<root cache folder>\<machinName>\[https]</summary>
      /// <returns>String Name of cache folder.</returns>
      internal static String GetCacheFolderName()
      {
         String cacheFolder = null;

         String serverName = ClientManager.Instance.getServer();
         if (serverName != null)
         {
            cacheFolder = GetRootCacheFolderName();

            //The server name may contain :port 
            // Found during testing of #976985 that if server is started on some other port than
            // default then we will have problem while preparing foldername.
            int indexOfColon = serverName.IndexOf(':');
            if (indexOfColon != -1)
               // If it contains :port, we will ignore it 
               serverName = serverName.Substring(0, indexOfColon);

            cacheFolder += (Path.DirectorySeparatorChar + serverName);

            if (ClientManager.Instance.getProtocol().Equals("https", StringComparison.InvariantCultureIgnoreCase))
               cacheFolder += (Path.DirectorySeparatorChar + "https");
         }

         return cacheFolder;
      }

      /// <summary>convert an HTTP URL to a file name in the cache</summary>
      /// <param name="url"></param>
      internal static String URLToLocalFileName(String url)
      {
         String relativeFileName = URLToFileName(HttpUtility.UrlDecode(url, Encoding.UTF8));
         return PersistentOnlyCacheManager.GetInstance().URLToLocalFileName(relativeFileName);
      }

      /// <summary>convert an HTTP URL to a file name</summary>
      /// <param name="decodedUrl"></param>
      internal static String URLToFileName(String decodedUrl)
      {
         string fileName = null;

         //------------------------------------------------------------------------
         // topic #16 (MAGIC version 1.8 for WIN) RC - Cache security enhancements
         //------------------------------------------------------------------------
         int indexOfCacheToken = decodedUrl.IndexOf(ConstInterface.RC_TOKEN_CACHED_FILE);
         if (indexOfCacheToken == -1)
            fileName = decodedUrl;
         else
         {
            fileName = GetCacheFilePath(decodedUrl);

            // uniPaaS 1.9, topics #17, #34 (automatic resources caching):
            // the server creates a request to retrieve a file from its original location. 
            // the client will cache the file in its own cache, using a qualified name.
            //    for example:  c:\serverfiles\a.jpg --> c__serverfiles_a.jpg
            fileName = ServerFilePathToFileName(fileName);
         }

         Debug.Assert(fileName != null);

         return fileName;
      }

      /// <summary>
      /// get cache file path from the URL
      /// </summary>
      /// <param name="decodedUrl"></param>
      /// <returns>Cache file path</returns>
      private static String GetCacheFilePath(String decodedUrl)
      {
         string fileName = null;

         int indexOfCacheToken = decodedUrl.IndexOf(ConstInterface.RC_TOKEN_CACHED_FILE);
         string[] tokenizeArray = decodedUrl.Split('&');

         foreach (string token in tokenizeArray)
         {
            indexOfCacheToken = token.IndexOf(ConstInterface.RC_TOKEN_CACHED_FILE);
            if (indexOfCacheToken == -1)
               continue;

            // split the url --> url | remote time
            string[] urlAndRemoteTimePair = token.Split('|');
            fileName = urlAndRemoteTimePair[0].Substring(indexOfCacheToken + ConstInterface.RC_TOKEN_CACHED_FILE.Length);
            break;
         }
        
         return fileName;
      }

      /// <summary>
      /// Get cache file details from the url.
      /// </summary>
      /// <param name="decodedUrl"></param>
      /// <param name="cacheFilePath"></param>
      /// <param name="remoteTime"></param>
      /// <param name="isEncrypted"></param>
      internal static void GetCacheFileDetailsFromUrl(string decodedUrl, ref string cacheFilePath, ref string remoteTime, ref bool isEncrypted)
      {
         //get Cache file path from url
         cacheFilePath = GetCacheFilePath(decodedUrl);

         // split the url --> url and remote time
         String[] urlAndRemoteTimePair = decodedUrl.Split('|');

         // Story#138618: remote time may not exits in the Url 
         // (for backward compatibility remoteTime may be exist in the url if SpecialTimeStampRIACache is ON).
         if (urlAndRemoteTimePair.Length == 2)
            remoteTime = urlAndRemoteTimePair[1];

         if (remoteTime != null)
         {
            int indexOfNonEncryptedToken = remoteTime.IndexOf(ConstInterface.RC_TOKEN_NON_ENCRYPTED);
            if (indexOfNonEncryptedToken != -1)
            {
               isEncrypted = false;
               remoteTime = remoteTime.Substring(0, indexOfNonEncryptedToken - 1);
            }
         }
      }
      /// <summary> Converts a server file path into the local cached file path
      /// eg. c:\serverfiles\a.jpg --> <cached folder>\c__serverfiles_a.jpg
      /// </summary>
      /// <param name="serverFileName"></param>
      /// <returns></returns>
      internal static String ServerFileToLocalFileName(String serverFileName)
      {
         String fileName = ServerFilePathToFileName(serverFileName);
         return PersistentOnlyCacheManager.GetInstance().URLToLocalFileName(fileName);
      }

      /// <summary> converts a file path into a file name.
      /// eg. c:\serverfiles\a.jpg --> c__serverfiles_a.jpg
      /// </summary>
      /// <param name="filePath"></param>
      /// <returns></returns>
      private static String ServerFilePathToFileName(String filePath)
      {
         // uniPaaS 1.9, topics #17, #34 (automatic resources caching):
         // the server creates a request to retrieve a file from its original location. 
         // the client will cache the file in its own cache, using a qualified name.
         //    for example:  c:\serverfiles\a.jpg --> c__serverfiles_a.jpg
         //QCR #: 783047.Added "/" at the beginning of the URL.Added Replace("/", "_") to handle the case if URL contains "/".
         return ("/" + filePath.Replace(':', '_').Replace('\\', '_').Replace("/", "_"));
      }

      /// <summary>Get temp folder on the system</summary>
      /// <returns></returns>
      private static String GetEnvTempFolder()
      {
         String tempFolder;
#if !PocketPC
         tempFolder = OSEnvironment.get("TEMP");
#else
         tempFolder = OSEnvironment.getTempFolder();
#endif
         return tempFolder;
      }
   }
}
