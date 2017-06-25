using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using com.magicsoftware.richclient.util;
using com.magicsoftware.util;
using com.magicsoftware.unipaas.util;
using com.magicsoftware.richclient.cache;
using com.magicsoftware.httpclient;

namespace com.magicsoftware.richclient.cache
{
   /// <summary>
   /// </summary>
   internal class PersistentOnlyCacheManager : ICacheManager, IEncryptor
   {
      private static PersistentOnlyCacheManager _instance;

      /// <summary>
      /// Create empty (not initialized) instance of PersistentOnlyCacheManager. 
      /// This instance should be used for accessing the HTTP/cache manager functionality in case the execution properties are not yet known 
      /// (and therefore cannot be used to initialize the cache manager) - currently the only such special case is accessing the publish.html file(ClickOnce), 
      /// yet this CreateInstance and empty CTOR are not and should not be specific to ClickOnce in any way.
      /// </summary>
      /// <returns>PersistentOnlyCacheManager instance</returns>
      internal static PersistentOnlyCacheManager CreateInstance()
      {
         lock (typeof(PersistentOnlyCacheManager))
         {
            if (_instance == null)
               _instance = new PersistentOnlyCacheManager();
         }
         return _instance;
      }

      /// <summary>Create and Initialize PersistentOnlyCacheManager instance with parameters.</summary>
      /// <param name="disableEncryption">if true, cached files will not be encrypted/decrypted to/from persistent storage.</param>
      /// <param name="encryptionKey">encryption key with which cache files will be encrypted/decrypted.</param>
      /// <returns>PersistentOnlyCacheManager instance</returns>
      internal static PersistentOnlyCacheManager CreateInstance(bool disableEncryption, byte[] encryptionKey)
      {
         lock (typeof(PersistentOnlyCacheManager))
         {
            if (_instance == null)
               _instance = new PersistentOnlyCacheManager(disableEncryption, encryptionKey);
         }
         return _instance;
      }

      /// <summary>
      /// </summary>
      internal static void DeleteInstance()
      {
         _instance = null;
      }

      /// <summary>
      /// Get singleton class instance already created by CreateInstance() method 
      /// </summary>
      /// <returns></returns>
      internal static PersistentOnlyCacheManager GetInstance()
      {
         Debug.Assert(_instance != null);
         return _instance;
      }

      /// <summary>
      /// Empty CTROR that creates non initialized PersistentOnlyCacheManager. 
      /// </summary>
      private PersistentOnlyCacheManager()
      {
         _cacheFolder = CacheUtils.GetCacheFolderName();
         if (_cacheFolder != null)
         {
            Debug.Assert(_cacheFolder != null);

            if (!string.IsNullOrEmpty(_cacheFolder) &&
                !_cacheFolder.EndsWith(Path.DirectorySeparatorChar.ToString()))
               _cacheFolder += Path.DirectorySeparatorChar.ToString();

            //QCR # 927351:Getting the full path.This is will expand the folder names represented as '~<folder name>' to there full name.This will help
            //in determining the length of the whole path more accurately.
            //Checking if the path contains ~.
            if (_cacheFolder.IndexOf("~") > 0)
               _cacheFolder = Path.GetFullPath(_cacheFolder);
         }
      }

      /// <summary>
      /// Parametrized CTOR that creates PersistentOnlyCacheManager and initializes it 
      /// </summary>
      /// <param name="disableEncryption">if true, cached files will not be encrypted/decrypted to/from persistent storage.</param>
      /// <param name="encryptionKey">encryption key with which cache files will be encrypted/decrypted.</param>
      private PersistentOnlyCacheManager(bool disableEncryption, byte[] encryptionKey) : this()
      {
         InitializeEncryptor(disableEncryption, encryptionKey);
      }

      #region ICacheManager Members

      private String _cacheFolder; //absolute path of the cache folder

      /// <summary>
      /// Save 'content' into the cache folder, identified by 'url', and set the file's time to 'remote Time'.
      /// </summary>
      /// <param name="url">URL to a cached file, excluding the server-side time stamp, e.g. /MG1VAI3T_MP_0$$$_$_$_N02400$$$$G8FM01_.xml.</param>
      /// <param name="content">content to save for future access to the url.</param>
      /// <param name="remoteTime">modification time of the file on the server-side (for story#138618: remote time will be null).</param>
      public void PutFile(String url, byte[] content, String remoteTime)
      {
         lock (this)
         {
            try
            {
               String localFilename = URLToLocalFileName(url);
               bool success = HandleFiles.writeToFile(HandleFiles.getFile(localFilename), content, false, true, ClientManager.Instance.GetWriteClientCacheMaxRetryTime());
               if (!success)
                  throw (new CacheManagerException(string.Format("Failed to save the file {0} in cache directory. Check log for details", localFilename)));
               if (remoteTime != null)
                  HandleFiles.setFileTime(localFilename, remoteTime);
            }
            catch (Exception e)
            {
               Misc.WriteStackTrace(e, Console.Error);
               throw (e);
            }
         }
      }

      /// <summary>
      /// get content from the cache; if the cached content's loading time differs from 'remoteTime', return null.
      /// </summary>
      /// <param name="url">URL to a cached file, excluding the server-side time stamp, e.g. /MG1VAI3T_MP_0$$$_$_$_N02400$$$$G8FM01_.xml.</param>
      /// <param name="remoteTime"></param>
      public byte[] GetFile(String url, String remoteTime)
      {
         lock (this)
         {
            byte[] content = null;
            String localFilename = URLToLocalFileName(url);

            String localTime = HandleFiles.getFileTime(localFilename);

            if (HandleFiles.equals(localTime, remoteTime))
               content = HandleFiles.readToByteArray(localFilename, "");

            return content;
         }
      }

      /// <summary>
      /// get content from the cache
      /// </summary>
      /// <param name="url">URL to a cached file, excluding the server-side time stamp, e.g. /MG1VAI3T_MP_0$$$_$_$_N02400$$$$G8FM01_.xml.</param>
      public byte[] GetFile(String url)
      {
         lock (this)
         {
            byte[] content = null;

            String localFilename = URLToLocalFileName(url);
            if (HandleFiles.isExists(localFilename))
               content = HandleFiles.readToByteArray(localFilename, "");

            return content;
         }
      }

      ///<summary>
      /// check that an up-to-date content exists in the cache folder for 'url'.
      ///</summary>
      /// <param name="url">URL to a cached file, excluding the server-side time stamp, e.g. /MG1VAI3T_MP_0$$$_$_$_N02400$$$$G8FM01_.xml.</param>
      /// <param name="remoteTime">modification time of the file on the server side.</param>
      ///<returns>true if content exists for 'url' and matches 'remoteTime'.</returns>
      public bool CheckFile(string url, string remoteTime)
      {
         bool upToDateContentFound = false;

         lock (this)
         {
            String localFilename = URLToLocalFileName(url);
            String localTime = HandleFiles.getFileTime(localFilename);

            upToDateContentFound = (HandleFiles.equals(localTime, remoteTime));
         }

         return upToDateContentFound;
      }

      ///<summary>
      /// check that content exists in the cache folder for 'url'.
      ///</summary>
      /// <param name="url">URL to a cached file, excluding the server-side time stamp, e.g. /MG1VAI3T_MP_0$$$_$_$_N02400$$$$G8FM01_.xml.</param>
      ///<returns>true if content exists for 'url'.</returns>
      public bool IsExists(string url)
      {
         bool contentFound = false;

         lock (this)
         {
            String localFilename = URLToLocalFileName(url);
            contentFound = HandleFiles.isExists(localFilename);
         }
         return contentFound;
      }

      #endregion

      /// <summary>
      /// </summary>
      /// <param name="completeCachedFileRetrievalURL">complete cached file retrieval request (including the server-side time stamp), 
      /// e.g. http://[server]/[requester]?CTX=&CACHE=My Application_DbhDataIds.xml|31/07/2013%2020:15:15</param>
      /// <returns>true if the requested cached file ('completeCachedFileRetrievalURL') exists in the local cache folder.</returns>
      internal bool IsCompleteCacheRequestURLExistsLocally(string completeCachedFileRetrievalURL)
      {
         string cachedFileServerFileName;
         string cachedFileLocalFileName;
         CompleteCacheRequestURLToFileNames(completeCachedFileRetrievalURL, out cachedFileServerFileName, out cachedFileLocalFileName);
         return HandleFiles.isExists(cachedFileLocalFileName);
      }

      /// <summary>
      /// return (using the two 'out' parameters) the server-side and client-side file names for a given request to retrieve a cached file.
      /// </summary>
      /// <param name="completeCachedFileRetrievalURL">complete cached file retrieval request (including the server-side time stamp), 
      /// e.g. http://[server]/[requester]?CTX=&CACHE=MG1VAI3T_MP_0$$$_$_$_N02400$$$$G8FM01_.xml|31/07/2013%2020:15:15</param>
      /// <param name="cachedFileServerFileName">the cached file name on the server, e.g. MG1VAI3T_MP_0$$$_$_$_N02400$$$$G8FM01_.xml</param>
      /// <param name="cachedFileLocalFileName">the cached file name in the client, e.g. C:\Users\[user]\AppData\Local\Temp\MgxpaRIACache\[server]\MG1VAI3T_MP_0$$$_$_$_N02400$$$$G8FM01_.xml</param>
      internal void CompleteCacheRequestURLToFileNames(string completeCachedFileRetrievalURL,
                                                       out string cachedFileServerFileName,
                                                       out string cachedFileLocalFileName)
      {
         // discard the time stamp, leaving only 'cachedFileRetrievalURL', e.g. 
         string[] urlAndRemoteTimePair = HttpUtility.UrlDecode(completeCachedFileRetrievalURL, Encoding.UTF8).Split('|');
         String cachedFileRetrievalURL = urlAndRemoteTimePair[0]; // the cached file retrieval request without the timestamp, e.g. http://[server]/[requester]?CTX=&CACHE=MG1VAI3T_MP_0$$$_$_$_N02400$$$$G8FM01_.xml

         // get 'cachedFileServerFileName' (i.e. server-side, see the example in the method's comment)  from 'cachedFileRetrievalURL', e.g. 
         const string precedingString = "&" + ConstInterface.RC_TOKEN_CACHED_FILE;
         int pathStartIndex = cachedFileRetrievalURL.IndexOf(precedingString) + precedingString.Length;
         cachedFileServerFileName = cachedFileRetrievalURL.Substring(pathStartIndex);
         Debug.Assert(cachedFileServerFileName != null);

         // get 'cachedFileLocalFileName' (i.e. client-side, see the example in the method's comment) from 'cachedFileRetrievalURL' 
         cachedFileLocalFileName = PersistentOnlyCacheManager.GetInstance().URLToLocalFileName(CacheUtils.URLToFileName(cachedFileRetrievalURL));
      }

      /// <summary>get the (local) cache file name from the URL.</summary>
      /// <param name="url">URL to a cached file, excluding the server-side time stamp, e.g. /MG1VAI3T_MP_0$$$_$_$_N02400$$$$G8FM01_.xml.</param>
      /// <returns>the name of the (local) file in the persistent cache.</returns>
      internal String URLToLocalFileName(String url)
      {
         string localFileName = HttpUtility.UrlDecodeSpaces(url.Substring(url.LastIndexOf("/") + 1, url.Length - (url.LastIndexOf("/") + 1)));
         string absoluteFileName = _cacheFolder + localFileName;
         if (absoluteFileName.Length > 260)
            absoluteFileName = _cacheFolder + localFileName.GetHashCode().ToString();
         return absoluteFileName;
      }

      #region IEncryptor Members

      public bool EncryptionDisabled { set; get; } //if true, cached files will not be encrypted/decrypted to/from persistent storage
      public byte[] EncryptionKey { get; private set; }

      private static readonly byte[] _DEFAULT_IV = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

      private static readonly byte[] _DEFAULT_KEY = new[]
                                                      {
                                                         (byte) (0x23), (byte) (0x45), (byte) (0x44), (byte) (0x43),
                                                         (byte) (0x76), (byte) (0x66), (byte) (0x72), (byte) (0x34),
                                                         (byte) (0x25), (byte) (0x54), (byte) (0x47), (byte) (0x42),
                                                         (byte) (0x6E), (byte) (0x68), (byte) (0x79), (byte) (0x36)
                                                      };
      private ICryptoTransform _decryptor;
      private ICryptoTransform _encryptor;

      /// <param name="disableEncryption">if true, cached files will not be encrypted/decrypted to/from persistent storage.</param>
      /// <param name="encryptionKey">encryption key with which cache files will be encrypted/decrypted.</param>
      public void InitializeEncryptor(bool disableEncryption, byte[] encryptionKey)
      {
         EncryptionDisabled = disableEncryption;

         if (disableEncryption)
            Debug.Assert(encryptionKey == null); // there's no point in passing an encryption key when the encryption is disabled.
         else
            SetEncryptionKey(encryptionKey != null
                                 ? encryptionKey
                                 : (byte[])(_DEFAULT_KEY).Clone());
      }

      /// <summary> sets a symmetric encryption key (for subsequent decryptions/encryptions);</summary>
      /// <param name="encryptionKey"></param>
      private void SetEncryptionKey(byte[] encryptionKey)
      {
         // encryption key
         //Overwriting the default key.
         if (EncryptionKey == null || (Equals(EncryptionKey, _DEFAULT_KEY)))
         {
            EncryptionKey = encryptionKey;
            AesCryptoServiceProvider encryptionProvider = (AesCryptoServiceProvider)Aes.Create();
            encryptionProvider.Mode = CipherMode.ECB;
            encryptionProvider.Padding = PaddingMode.Zeros;
            _decryptor = encryptionProvider.CreateDecryptor(EncryptionKey, _DEFAULT_IV);
            _encryptor = encryptionProvider.CreateEncryptor(EncryptionKey, _DEFAULT_IV);

         }
         else if (!Equals(encryptionKey, EncryptionKey))
         {
            throw (new CacheManagerException("Encryption key cannot be set more than once"));
         }
      }

      /// <summary> checks if encryption key is different than the default key. </summary>
      /// <returns></returns>
      public bool HasNonDefaultEncryptionKey()
      {
         return (EncryptionKey == null || (!Equals(EncryptionKey, _DEFAULT_KEY)));
      }

      /// <summary> encrypt the content using 'encryptionKey_';</summary>
      public byte[] Encrypt(byte[] decryptedContent)
      {
         if (decryptedContent == null)
            throw (new CacheManagerException("Null content"));

         byte[] encryptedContent;

         if (EncryptionDisabled)
            encryptedContent = decryptedContent;
         else
         {
            if (EncryptionKey == null)
               throw (new CacheManagerException("Encryption key must be set"));

            var ms = new MemoryStream();
            var cs = new CryptoStream(ms, _encryptor, CryptoStreamMode.Write);
            // The following block is relevant only in case of strings, otherwise all it does
            // is add one extra block to the buffer and fills it with zeros.
            // We need this block for strings whose length is a multiple of the block size,
            // because the encryptor will not pad them with zeros, and there will be no zero
            // at the end of the string, so the decrypted string at the unmanaged side will be
            // illegal because of that.
            if (decryptedContent.Length % _encryptor.InputBlockSize == 0)
            {
               var paddedArray = new byte[decryptedContent.Length + _encryptor.InputBlockSize];
               Array.Copy(decryptedContent, paddedArray, decryptedContent.Length);

               for (int i = decryptedContent.Length; i < paddedArray.Length; i++)
                  paddedArray[i] = 0;

               decryptedContent = paddedArray;
            }

            cs.Write(decryptedContent, 0, decryptedContent.Length);
            cs.Close();

            encryptedContent = ms.ToArray();
         }
         return encryptedContent;
      }

      /// <summary> decrypt the content using 'encryptionKey_';</summary>
      public byte[] Decrypt(byte[] encryptedContent)
      {
         if (encryptedContent == null)
            throw (new CacheManagerException("Null content"));

         byte[] decryptedContent;

         if (EncryptionDisabled)
            decryptedContent = encryptedContent;
         else
         {
            if (EncryptionKey == null)
               throw (new CacheManagerException("Encryption key must be set"));

            var ms = new MemoryStream();
            var cs = new CryptoStream(ms, _decryptor, CryptoStreamMode.Write);
            cs.Write(encryptedContent, 0, encryptedContent.Length);
            cs.Close();

            decryptedContent = ms.ToArray();

            /*---------------------------------------------------------------------------*/
            /* We use Cipher mode ECB for encryption/decryption. ECB is block cipher so  */
            /* on the server side we pad plain text with NULLs to make the final block   */
            /* complete. After decryption we need to remove those trailing NULLS. The fix*/
            /* is done after investigating other alternatives (other ciphers modes etc). */
            /* All the data we encrypt on server is text and it comes over HTTP hence it */
            /* is safe to assume that data will not contain nulls                        */
            /*---------------------------------------------------------------------------*/
            //get last index.
            int index = decryptedContent.Length - 1;
            while (decryptedContent[index] == 0)
               index--;
            //now decryptedContent[index] is last non zero byte
            var unpaddedContent = new byte[index + 1];
            Array.Copy(decryptedContent, unpaddedContent, index + 1);
            ms.Close();
            decryptedContent = unpaddedContent;
         }
         return decryptedContent;
      }

      #endregion

      /// comparing two byte arrays
      private static bool Equals(byte[] array1, byte[] array2)
      {
         bool ret = (array1.Length == array2.Length);
         if (ret)
         {
            int len1 = array1.Length;
            for (int i = 0; i < len1; i++)
               if (array1[i] != array2[i])
               {
                  ret = false;
                  break;
               }
         }
         return (ret);
      }
   }
}