using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.remote;
using com.magicsoftware.richclient.local;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.cache;
using com.magicsoftware.httpclient; //ref's HttpUtility.cs
using com.magicsoftware.util;
using com.magicsoftware.unipaas.util;
using util.com.magicsoftware.util;
using com.magicsoftware.richclient.http;

namespace com.magicsoftware.richclient.sources
{
   /// <summary>
   /// This class provides functionality for managing sources for maintaining sources integrity. 
   /// While reading sources remotely, all sources in the application should be read in entirety. 
   /// If there is a failure while reading sources, then all new sources will be discarded.
   /// </summary>
   internal class ApplicationSourcesManager
   {
      // This list contains urls for modified source files
      private List<String> _commitList = new List<String>();

      // Sources integrity is needed in initial stage of application execution, when application sources are get at once.
      // After committing the sources, sources integrity will be disabled
      private bool _enableSourcesIntegrity = true;

      // Indicates status of sources synchronization which is mainly used in next execution.
      internal SourcesSyncStatus SourcesSyncStatus {get; private set;}

      // offline-required metadata (should be sent to the server whenever the client tries to [re/]establish a connected session).
      internal OfflineRequiredMetadataCollection OfflineRequiredMetadataCollection { get; private set; }

      // singleton
      private static ApplicationSourcesManager _instance;

      /// <summary>Returns the single instance of the ApplicationSourcesManager class  </summary>
      internal static ApplicationSourcesManager GetInstance()
      {
         if (_instance == null)
         {
            if (_instance == null)
               _instance = new ApplicationSourcesManager();
         }

         return _instance;
      }

      /// <summary>
      /// CTOR
      /// </summary>
      private ApplicationSourcesManager()
      {
         SourcesSyncStatus = new SourcesSyncStatus();
         OfflineRequiredMetadataCollection = new OfflineRequiredMetadataCollection();
      }

      /// <summary>
      /// Reads the sources from server and writes its content to temporary sources.
      /// For Local session reads sources from cache.
      /// </summary>
      /// <param name="completeCachedFileRetrievalURL"></param>
      /// <param name="cachedContentShouldBeReturned">if false, the method will return null as the retrieved content (avoiding retrieval of the requested URL from the local/client cache folder, to save resources / improve performance).</param>
      /// <returns></returns>
      internal byte[] ReadSource(String completeCachedFileRetrievalURL, bool cachedContentShouldBeReturned)
      {
         return ReadSource(completeCachedFileRetrievalURL, cachedContentShouldBeReturned, true);
      }

      /// <summary>
      /// Reads the sources from server and writes its content to temporary sources.
      /// For Local session reads sources from cache.
      /// </summary>
      /// <param name="completeCachedFileRetrievalURL">complete cached file retrieval request (including the server-side time stamp), 
      /// e.g. http://[server]/[requester]?CTX=&CACHE=MG1VAI3T_MP_0$$$_$_$_N02400$$$$G8FM01_.xml|31/07/2013%2020:15:15</param>
      /// <param name="cachedContentShouldBeReturned">if false, the method will return null as the retrieved content (avoiding retrieval of the requested URL from the local/client cache folder, to save resources / improve performance).</param>
      /// <param name="allowOutdatedContent">if true, check only file existence at client cache</param>
      /// <returns>content</returns>
      internal byte[] ReadSource(String completeCachedFileRetrievalURL, bool cachedContentShouldBeReturned, bool allowOutdatedContent)
      {
         Logger.Instance.WriteSupportToLog("ReadSource():>>>>> ", true);

         byte[] content = null;

         if (_enableSourcesIntegrity && CommandsProcessorManager.SessionStatus == CommandsProcessorManager.SessionStatusEnum.Remote)
         {
            var cachingStrategy = new HttpManager.CachingStrategy() { CanWriteToCache = false, CachedContentShouldBeReturned = cachedContentShouldBeReturned, AllowOutdatedContent = allowOutdatedContent};
            content = RemoteCommandsProcessor.GetInstance().GetContent(completeCachedFileRetrievalURL, null, null, false, cachingStrategy);

            // if sources were modified (i.e. not retrieved from the cache), save the content to a temporary file:
            if (!cachingStrategy.FoundInCache)
            {
               WriteToTemporarySourceFile(completeCachedFileRetrievalURL, content);
               _commitList.Add(completeCachedFileRetrievalURL);
            }
            else
               // record/register a cached file path + its local time:
               OfflineRequiredMetadataCollection.Collect(completeCachedFileRetrievalURL);

            if (cachedContentShouldBeReturned)
               content = PersistentOnlyCacheManager.GetInstance().Decrypt(content);
         }
         else
         {
            if (cachedContentShouldBeReturned)
               content = CommandsProcessorManager.GetContent(completeCachedFileRetrievalURL, true);
            else
            {
               if (!PersistentOnlyCacheManager.GetInstance().IsCompleteCacheRequestURLExistsLocally(completeCachedFileRetrievalURL))
                  throw new LocalSourceNotFoundException("Some of the required files are missing. Please restart the application when connected to the network."); //TODO: use a message from mgconst - here as well as in LocalCommandsProcessor.GetContent 
            }

            // record/register a cached file path + its local time:
            OfflineRequiredMetadataCollection.Collect(completeCachedFileRetrievalURL);
         }

         Logger.Instance.WriteSupportToLog("ReadSource():<<<< ", true);
         return content;
      }

      /// <summary>
      /// commit modified sources to cache.
      /// </summary>
      internal void Commit()
      {
         Logger.Instance.WriteSupportToLog("Commit():>>>> ", true);

         bool success = true;

         // It is about to start committing the sources. So save the status. If process get terminated during commit,
         // in next execution, client will get to know the status of sources synchronization and take action accordingly.
         SourcesSyncStatus.InvalidSources = true;
         SourcesSyncStatus.SaveToFile();

         // Renames the temporary sources to original names.
         foreach (String url in _commitList)
         {
            String tempFileName = BuildTemporarySourceFileName(GetFileNameFromURL(url));
            String tempSourceFileFullName = PersistentOnlyCacheManager.GetInstance().URLToLocalFileName(tempFileName);
            String originalSourceFileFullName = PersistentOnlyCacheManager.GetInstance().URLToLocalFileName(GetFileNameFromURL(url));
            String remoteTime = GetRemoteTime(url);

            // check if source with remotetime exist
            if (IsFileExistWithRequestedTime(tempSourceFileFullName, remoteTime))
            {
               Logger.Instance.WriteSupportToLog(String.Format("commit(): renaming {0}", tempFileName), true);

               success = HandleFiles.renameFile(tempSourceFileFullName, originalSourceFileFullName);
               if (!success)
                  break;

               if (remoteTime != null)
                  HandleFiles.setFileTime(originalSourceFileFullName, remoteTime);

               // record/register a cached file path + its local time:
               OfflineRequiredMetadataCollection.Collect(url);
            }
         }

         if (!success)
         {
            String errorMessage;

            // sources commit failed
            // If tables were converted and committed, there structure won't match the sources
            if (SourcesSyncStatus.TablesIncompatibleWithDataSources == true)
               errorMessage = ClientManager.Instance.getMessageString(MsgInterface.RC_ERROR_INCOMPATIBLE_DATASOURCES);
            else
               errorMessage = ClientManager.Instance.getMessageString(MsgInterface.RC_ERROR_INVALID_SOURCES);

            throw new InvalidSourcesException(errorMessage, null);
         }
         
         // Commit is done successfully, clear the status.
         SourcesSyncStatus.Clear();

         Logger.Instance.WriteSupportToLog("Commit():<<<< ", true);
      }

      /// <summary>
      /// Removes the changes to be committed.
      /// </summary>
      internal void RollBack()
      {
         _commitList.Clear();
      }

      /// <summary>
      /// disables the source integrity.
      /// </summary>
      internal void DisableSourceIntegrity()
      {
         _enableSourcesIntegrity = false;
      }

      /// <summary>prepare temporary source file name</summary>
      private String BuildTemporarySourceFileName(String sourceUrl)
      {
         int extentionLocation = sourceUrl.IndexOf('.');
         String tempSourceFileName = sourceUrl.Insert(extentionLocation, "_tmp");

         return tempSourceFileName;
      }

      /// <summary>
      /// get file name from URL.
      ///</summary>
      /// <param name="url"></param>
      /// <returns></returns>
      private String GetFileNameFromURL(String url)
      {
         // split the url --> url and remote time
         String[] urlAndRemoteTimePair = HttpUtility.UrlDecode(url, Encoding.UTF8).Split('|');
         String urlCachedByRequest = urlAndRemoteTimePair[0];
         String fileUrl = CacheUtils.URLToFileName(urlCachedByRequest);

         return fileUrl;
      }

      /// <summary>
      /// get remote time from URL.
      /// </summary>
      /// <param name="url"></param>
      /// <returns></returns>
      private String GetRemoteTime(String url)
      {
         String remoteTime = null;

         // split the url --> url and remote time
         String[] urlAndRemoteTimePair = HttpUtility.UrlDecode(url, Encoding.UTF8).Split('|');
         if (urlAndRemoteTimePair.Length == 2) // Story#138618: for backward compatibility remoteTime may be exist in the url.
            remoteTime = urlAndRemoteTimePair[1];

         return remoteTime;
      }

      /// <summary>
      /// checks file exist with specified remote time.
      /// when remote time is null, then check only file is exist or not
      /// otherwise check if source with remotetime exist
      /// </summary>
      /// <param name="fileFullName">Full name of the file</param>
      /// <param name="remoteTime">last modification time of the file (for story#138618: remote time will be null)</param>
      /// <returns></returns>
      private bool IsFileExistWithRequestedTime(String fileFullName, String remoteTime)
      {
         bool isFileExist = HandleFiles.isExists(fileFullName);

         try
         {
            if (isFileExist && remoteTime != null)
            {
               String localTime = HandleFiles.getFileTime(fileFullName);
               if (!HandleFiles.equals(localTime, remoteTime))
                  isFileExist = false;
            }
         }
         catch (Exception exception)
         {
            Logger.Instance.WriteExceptionToLog(exception.Message);
            throw;
         }

         return isFileExist;
      }

      /// <summary>
      /// writes content to temporary file.
      /// </summary>
      /// <param name="url"></param>
      /// <param name="content"></param>
      private void WriteToTemporarySourceFile(String url, byte[] content)
      {
         // extract file url & remote time
         String tempSourceFileUrl = BuildTemporarySourceFileName(GetFileNameFromURL(url));
         String remoteTime = GetRemoteTime(url);

         PersistentOnlyCacheManager.GetInstance().PutFile(tempSourceFileUrl, content, remoteTime);
      }
   }
}
