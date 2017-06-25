using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using com.magicsoftware.richclient.util;
using com.magicsoftware.util;
using com.magicsoftware.richclient.cache;
using com.magicsoftware.richclient.remote;
using com.magicsoftware.unipaas.util;
using com.magicsoftware.httpclient;

#if PocketPC
using com.magicsoftware.unipaas.gui.low;
using OSEnvironment = com.magicsoftware.richclient.mobile.util.OSEnvironment;
using util.com.magicsoftware.util;
#else
using System.Deployment.Application;
using util.com.magicsoftware.util;
#endif

namespace com.magicsoftware.richclient.http
{
   /// <summary>
   /// this class is responsible for retrieving remote content, either by:
   ///   (i) accessing a web server (over HTTP, using class HttpClient), or ..
   ///   (ii) retrieving the required content (if up-to-date) from the local cache.
   /// </summary>
   internal class HttpManager
   {
      private static HttpManager _instance; //singleton

      // communication-level timeout (i.e. the access to the web server, NOT the entire request/response round-trip), in ms.
      internal uint _httpCommunicationTimeoutMS = DEFAULT_HTTP_COMMUNICATION_TIMEOUT;
      internal uint HttpCommunicationTimeoutMS
      {
         get { return _httpCommunicationTimeoutMS; }
         set { _httpCommunicationTimeoutMS = value; }
      } // ms
      private const uint DEFAULT_HTTP_COMMUNICATION_TIMEOUT = 5 * 1000;
      internal const uint DEFAULT_OFFLINE_HTTP_COMMUNICATION_TIMEOUT = 2 * 1000;

      private readonly HttpClient _httpClient;

#if !PocketPC
      internal string ActivationURIWithoutCookies { get; set; }
      private string _activationURICookies;

      // animates an icon on the status bar of the topmost task (if a status bar exists, otherwise does nothing) during access to the network.
      private readonly RemoteCommandsProcessor.ExternalAccessAnimator _externalAccessAnimator = new RemoteCommandsProcessor.ExternalAccessAnimator();
#else
      private readonly com.magicsoftware.richclient.mobile.util.ExternalAccessAnimator _externalAccessAnimator = new com.magicsoftware.richclient.mobile.util.ExternalAccessAnimator();
#endif

      private RecentNetworkActivities _recentNetworkActivities;
      internal RecentNetworkActivities RecentNetworkActivities
      {
         get
         {
            if (_recentNetworkActivities == null)
               _recentNetworkActivities = new RecentNetworkActivities();
            return _recentNetworkActivities;
         }
      }

      /// <summary> create/return the single instance of the class.</summary>
      internal static HttpManager GetInstance()
      {
         if (_instance == null)
         {
            lock (typeof(HttpManager))
            {
               if (_instance == null)
                  _instance = new HttpManager();
            }
         }
         return _instance;
      }

      /// <summary></summary>
      private HttpManager()
      {
         _httpClient = new HttpClient();

#if !PocketPC
         ProcessActivationURI();
         if (!String.IsNullOrEmpty(_activationURICookies))
            SetOutgoingCookies(_activationURICookies); //pass cookies query through HttpManager to HttpClient
#endif

#if PocketPC
         if (GUIMain.getInstance().MainForm != null)
         {
            StringBuilder sb = new StringBuilder();
            sb.Append(ClientManager.Instance.getMessageString(MsgInterface.BROWSER_OPT_INFO_SERVER_STR) + ": ");
            sb.Append(ClientManager.Instance.getProtocol());
            sb.Append("://");
            sb.Append(ClientManager.Instance.getServer());
            GUIMain.getInstance().MainForm.updateProgressBar(sb.ToString());
            sb = new StringBuilder();
            sb.Append(ClientManager.Instance.getMessageString(MsgInterface.TASKRULE_STR_APPLICATION) + ": \"");
            sb.Append(ClientManager.Instance.getAppName());
            sb.Append("\" (\"");
            sb.Append(ClientManager.Instance.getPrgName());
            sb.Append("\")");
            GUIMain.getInstance().MainForm.updateProgressBar(sb.ToString());
         }
#endif
         RegisterBasicDelegates();
      }

      /// <summary>
      /// Subscribe for events (except Statistics events) defined at HttpClientEvents and 
      /// attach appropriate event handlers
      /// </summary>
      /// <returns></returns>
      private void RegisterBasicDelegates()
      {
         com.magicsoftware.httpclient.HttpClientEvents.GetHttpCommunicationTimeout_Event += GetHttpCommunicationTimeoutMS;
      }

      /// <summary>
      /// Returns the communication-level timeout (i.e. the access to the web server, NOT the entire request/response round-trip), in ms
      /// Refer to 'FirstHTTPRequestTimeout' (execution.properties) and to 'HTTPRequestTimeout' (sent from the server during the handshake).
      /// </summary>
      /// <returns></returns>
      public uint GetHttpCommunicationTimeoutMS()
      {
         return HttpCommunicationTimeoutMS;
      }

      internal string GetCompressionLvl()
      {
         return HttpClient.GetCompressionLevel();
      }

      /// <summary>
      /// Sets the communications failure handler on the Http Client. The client
      /// is not externally accessible (nor should it be), so the HttpManager is
      /// used to access it.
      /// </summary>
      /// <param name="handler">A communications failure handler implementation.</param>
      public void SetCommunicationsFailureHandler(ICommunicationsFailureHandler handler)
      {
         _httpClient.CommunicationsFailureHandler = handler;
      }

      /// <summary> Gets the communication failure handler registered </summary>
      /// <returns></returns>
      public ICommunicationsFailureHandler GetCommunicationsFailureHandler()
      {
         return _httpClient.CommunicationsFailureHandler;
      }

      //Sets a list of cookies and their values, to be sent on every HTTP request.
      //sCookies: name1=value1&name2=value2 ...
      private void SetOutgoingCookies(string sCookies)
      {
         Debug.Assert(sCookies != null);
         _httpClient.SetOutgoingCookies(sCookies);
      }

#if !PocketPC
      //Extract cookies information from ActivationUri query string. Divide it into two distinct strings:
      //one without cookies and the other containing only cookies and saves them
      //as _sActivationURIWithoutCookies and _sActivationURICookies respectively. This method is the only one
      //who sets these variables.
      private void ProcessActivationURI()
      {
         if (ApplicationDeployment.IsNetworkDeployed &&
             ApplicationDeployment.CurrentDeployment.ActivationUri != null &&
             !String.IsNullOrEmpty(ApplicationDeployment.CurrentDeployment.ActivationUri.Query))
         {
            string sQuery = ApplicationDeployment.CurrentDeployment.ActivationUri.Query;
            Debug.Assert(sQuery.StartsWith("?"));
            sQuery = sQuery.Substring(1); //eliminate the '?'

            ActivationURIWithoutCookies = sQuery;
            _activationURICookies = "";

            const string csCookies = "cookies=";
            int nCookiesStart = sQuery.IndexOf(csCookies);

            if (nCookiesStart > -1)
            {
               int nFirstAfterCookiesEnd = sQuery.IndexOf('&', nCookiesStart + csCookies.Length);
               nFirstAfterCookiesEnd = nFirstAfterCookiesEnd > -1
                                          ? nFirstAfterCookiesEnd
                                          : sQuery.Length;

               string sBeforeCookies = "";
               if (nCookiesStart >= 0 && nCookiesStart < sQuery.Length)
                  sBeforeCookies = sQuery.Substring(0, nCookiesStart > 0
                                                          ? nCookiesStart - 1
                                                          : 0);
               string sAfterCookies = "";
               if (nFirstAfterCookiesEnd >= 0 && nFirstAfterCookiesEnd < sQuery.Length)
                  sAfterCookies = sQuery.Substring(nFirstAfterCookiesEnd, sQuery.Length - nFirstAfterCookiesEnd);

               ActivationURIWithoutCookies = sBeforeCookies + sAfterCookies;
               if (ActivationURIWithoutCookies.StartsWith("&"))
                  ActivationURIWithoutCookies = ActivationURIWithoutCookies.Substring(1);
               if (ActivationURIWithoutCookies.EndsWith("&"))
                  ActivationURIWithoutCookies = ActivationURIWithoutCookies.Substring(0, ActivationURIWithoutCookies.Length - 1);

               _activationURICookies = sQuery.Substring(nCookiesStart, nFirstAfterCookiesEnd - nCookiesStart);
               if (_activationURICookies.EndsWith("&"))
                  _activationURICookies = _activationURICookies.Substring(0, _activationURICookies.Length - 1);
            }
         }
      }
#endif

      /// <summary>
      /// Get sources from server and returns the contents
      /// </summary>    
      ///<param name="requestedURL">!!.</param>
      ///<param name="requestContent">!!.</param>
      ///<param name="requestContentType">!!.</param>
      ///<param name="decryptResponse">!!.</param>
      ///<param name="allowRetry">!!.</param>
      ///<param name="isError">!!.</param>
      ///<returns>!!.</returns>
      internal byte[] GetContent(String requestedURL, byte[] requestContent, String requestContentType, bool decryptResponse, bool allowRetry, out bool isError)
      {
         var cachingStrategy = new CachingStrategy() { CanWriteToCache = true, CachedContentShouldBeReturned = true };
         return (GetContent(requestedURL, requestContent, requestContentType, decryptResponse, allowRetry, cachingStrategy, out isError));
      }

      /// <summary> interact with the cache manager; execute HTTP request if needed; if 'decryptResponse' is true, fresh
      /// responses from the server will be decrypted (if needed) using the 'encryptionKey' passed to 'setProperties'; </summary>
      /// <param name="requestedURL">URL to be accessed. If the URL contains a time stamp on the server-side (|timestamp), the time stamp is seperated from the URL and checked against the local/client-side cache.</param>
      /// <param name="requestContent">content to be sent to server (relevant only for POST method - is null for other methods). may be byte[] or string.</param>
      /// <param name="requestContentType">type of the content to be sent to server, e.g application/octet-stream (for POST method only).</param>
      /// <param name="decryptResponse">if true, fresh responses from the server will be decrypted using the 'encryptionKey' passed to 'HttpManager.SetProperties'.</param>
      /// <param name="allowRetry">whether or not retry after connection failure.</param>
      /// <param name="cachingStrategy">Enabels or disables cache read/write operations.</param>
      /// <param name="isError">set to true if server returns error processing the request.</param>
      /// <returns>response (from the server).</returns>
      internal byte[] GetContent(String requestedURL, object requestContent, String requestContentType, bool decryptResponse, bool allowRetry,
                                 CachingStrategy cachingStrategy, out bool isError)
      {
         String urlCachedByRequest = null; // a cached file retrieval request without the timestamp, e.g. http://[server]/[requester]?CTX=&CACHE=MG1VAI3T_MP_0$$$_$_$_N02400$$$$G8FM01_.xml
         byte[] response = null;
         long startTime = Misc.getSystemMilliseconds();
         TimeSpan requestStartTime = DateTime.Now.TimeOfDay; // for RecentNetworkActivities only
         bool isCacheRequest = false;

         try
         {
            Logger.Instance.WriteServerToLog(
               "*************************************************************************************************");

            cachingStrategy.FoundInCache = false;
            isError = false;

            String remoteTime = null;

            // GET request
            if (requestContent == null)
            {
               //--------------------------------------------------------------------------------------------
               // topic #16 (MAGIC version 1.8 for WIN) RC - Cache security enhancements:
               //   the server reports the time stamp as part as the URL, for example:
               //      http://../mgrqispi.dll?CTX=155842418385&CACHE=KeyboardMapping.xml%7C19/03/2009%2009:45:33
               //   cache files are accessed via a request, in this example:
               //      http://myserver/MagicScripts/mgrqispi.dll?CTX=155842418385&CACHE=KeyboardMapping.xml
               //                                                               ^ indexOfCacheToken
               //--------------------------------------------------------------------------------------------
               // extract the remote time from the request 
               int indexOfCacheToken = requestedURL.IndexOf(ConstInterface.RC_TOKEN_CACHED_FILE);
               if (indexOfCacheToken != -1)
               {
                  isCacheRequest = true;
                  string decodedUrl = HttpUtility.UrlDecode(requestedURL, Encoding.UTF8);
                  requestedURL = CacheUtils.URLToFileName(decodedUrl);

                  string cacheFilePath = String.Empty;
                  CacheUtils.GetCacheFileDetailsFromUrl(decodedUrl, ref cacheFilePath, ref remoteTime, ref decryptResponse);

                  byte[] bufferToSend = Encoding.UTF8.GetBytes(cacheFilePath);
                  IEncryptor encryptor = PersistentOnlyCacheManager.GetInstance();
                  if (!encryptor.EncryptionDisabled)
                  {
                     byte[] encryptedURL = encryptor.Encrypt(Encoding.UTF8.GetBytes(cacheFilePath));
                     byte[] base64EncodedEncryptedURL = Base64.encode(encryptedURL);
                     bufferToSend = base64EncodedEncryptedURL;
                  }
                  // prepare URL again by encoding only CACHE value
                  urlCachedByRequest = decodedUrl.Substring(0, indexOfCacheToken) +
                                       ConstInterface.RC_INDICATION +
                                       ConstInterface.RC_TOKEN_CACHED_FILE +
                                       HttpUtility.UrlEncode(bufferToSend);
               }
               else if (requestedURL.IndexOf('?') == -1)
                  // i.e. the url refers to a file on the web server, rather than a requester + query string
                  remoteTime = _httpClient.GetRemoteTime(requestedURL);
            }

            Logger.Instance.WriteServerToLog(requestedURL);

            byte[] errorResponse = null;
            // Story#138618: remoteTime will not exist in the cache url.
            // (for backward compatibility remoteTime exist in url if  SpecialTimeStampRIACache is ON)
            if (remoteTime == null && !isCacheRequest)
            {
               // resources for which no remote time could be retrieved (either by the web server or by the runtime-engine, in a cache request url) can't be and aren't cached,
               // since without a time stamp the client can't decide if the client-side copy is out-dated.
               //-----------------------------

               LogAccessToServer("", requestContent);
               response = GetContentByHTTPRequest(requestedURL, requestContent, requestContentType, allowRetry);
               Debug.Assert(response != null);
               errorResponse = CheckAndGetErrorResponse(response);
            }
            else
            {
               // cachable resources ..
               //----------------------

               ICacheManager cacheManager = PersistentOnlyCacheManager.GetInstance();
               if (cachingStrategy.CachedContentShouldBeReturned)
               {
                  response = (remoteTime == null
                                ? cacheManager.GetFile(requestedURL)
                                : cacheManager.GetFile(requestedURL, remoteTime));
                  cachingStrategy.FoundInCache = (response != null);
               }
               else
               {
                  // here 'requestedURL' is excluding the server-side time stamp, e.g. /MG1VAI3T_MP_0$$$_$_$_N02400$$$$G8FM01_.xml.
                  Debug.Assert(requestedURL.IndexOf('|') == -1);
                  cachingStrategy.FoundInCache = (cachingStrategy.AllowOutdatedContent || remoteTime == null
                                                      ? cacheManager.IsExists(requestedURL)
                                                      : cacheManager.CheckFile(requestedURL, remoteTime));
               }

               if (cachingStrategy.FoundInCache)
                  Logger.Instance.WriteServerToLog("Found in cache.");
               else
               {
                  LogAccessToServer(String.Format("Not in cache{0}!", cachingStrategy.AllowOutdatedContent ? "" : " or outdated"), null);

                  // get fresh content & put in the cache
                  response = GetContentByHTTPRequest((urlCachedByRequest ?? requestedURL), null, requestContentType, allowRetry);
                  if (response != null)
                  {
                     errorResponse = CheckAndGetErrorResponse(response);

                     // do not save the file, if there is an error.
                     if (errorResponse == null && cachingStrategy.CanWriteToCache)
                        cacheManager.PutFile(requestedURL, response, remoteTime);
                  }
               }
            }

            //Error messages are never encrypted. So, do not decrypt them.
            if (errorResponse != null)
            {
               response = errorResponse;
               isError = true;
            }
            else if (decryptResponse && response != null)
            {
               IEncryptor encryptor = PersistentOnlyCacheManager.GetInstance();
               response = encryptor.Decrypt(response);
            }

#if PocketPC
            GUIMain.getInstance().MainForm.updateProgressBar();
#endif

            return response;
         }
         catch (Exception ex)
         {
            throw new Exception(ex.Message + OSEnvironment.EolSeq +
                                (new Uri((urlCachedByRequest ?? requestedURL))).GetLeftPart(UriPartial.Path), ex);
         }
         finally
         {
            //collect statistics
            var elapsedTime = (uint)(Misc.getSystemMilliseconds() - startTime);

            // !foundInCache ==> the request was executed by the HTTP client ==> should be recorded
            if (!cachingStrategy.FoundInCache)
               Statistics.RecordRequest(elapsedTime,
                                        (ulong)(requestContent != null
                                                    ? HttpClient.GetRequestContentLength(requestContent)
                                                    : 0),
                                        (ulong)(response != null
                                                    ? response.Length
                                                    : 0));
            Logger.Instance.WriteServerToLog(
               string.Format(
                  "Completed {0}: {1:N0} ms, accumulated: {2:N0} ms (server: {3:N0}), {4}{5}{6}**************************************************************************************************",
                  (cachingStrategy.FoundInCache
                      ? ""
                      : string.Format("#{0:N0}", Statistics.GetRequestsCnt())
                  ),
                  elapsedTime,
                  Statistics.GetAccumulatedExternalTime(), Statistics.GetAccumulatedServerTime(),
                  (cachingStrategy.FoundInCache
                      ? ""
                      : (response != null
                            ? string.Format("{0:N0} bytes downloaded", response.Length)
                            : "Null response!")
                  ),
                  OSEnvironment.EolSeq, OSEnvironment.TabSeq)
               );
            if (!cachingStrategy.FoundInCache && ClientManager.Instance.getDisplayStatisticInfo())
            {
               // Add the networking activity.
               var downloadSizeKB = (ulong)(response != null
                                                ? response.Length
                                                : 0);
               RecentNetworkActivities.Append(Statistics.GetRequestsCnt(), requestStartTime, elapsedTime, downloadSizeKB, requestedURL);
            }
         }
      }

      /// <summary>
      /// helper method - gets content using HttpClient, starting and stopping an animator during the call.
      /// </summary>
      /// <param name="requestedURL">URL to be accessed.</param>
      /// <param name="requestContent">content to be sent to server (relevant only for POST method - is null for other methods). may be byte[] or string.</param>
      /// <param name="requestContentType">type of the content to be sent to server, e.g application/octet-stream (for POST method only).</param>
      /// <param name="allowRetry">whether or not retry after connection failure.</param>
      /// <returns>response</returns>
      private byte[] GetContentByHTTPRequest(String requestedURL, object requestContent, String requestContentType, bool allowRetry)
      {
         byte[] response = null;

         _externalAccessAnimator.Start();
         try
         {
            response = _httpClient.GetContent(requestedURL, requestContent, requestContentType, allowRetry);
         }
         catch (Exception)
         {
            throw;
         }
         finally
         {
            _externalAccessAnimator.Stop();
         }

         return response;
      }

      /// <summary>Check if an HTTP response is an error response.
      /// (TODO: rather than manipulating the response, HTTP headers would've been a better way to exchange additional details between partners ...)</summary>
      /// <param name="httpResponse">response returned to an HTTP request.</param>
      /// <returns>if the response contains the error indicator - the error indicator is truncated and the remaining is returned.
      /// otherwise - null (indicating that the 'http Response' didn't contain an error).</returns>
      private byte[] CheckAndGetErrorResponse(byte[] httpResponse)
      {
         byte[] errorRepsonseIndicator = Encoding.UTF8.GetBytes(ConstInterface.V24_RIA_ERROR_PREFIX);
         byte[] errorResponse = null;

         // find 'errorRepsonseIndicator' in 'httpResponse', starting from the start index.
         // note: it's better to convert the shorter 'errorRepsonseIndicator' to byte array and search it manually in the 'httpResponse' 
         //       rather than converting the longer 'httpResponse' to string, every time the method is called.
         int i;
         for (i = 0; i < errorRepsonseIndicator.Length; i++)
            if (httpResponse[i] != errorRepsonseIndicator[i])
               break;
         if (i == errorRepsonseIndicator.Length)
         {
            // 'errorRepsonseIndicator' was found in 'httpResponse'
            errorResponse = new byte[httpResponse.Length - errorRepsonseIndicator.Length];
            Array.Copy(httpResponse, errorRepsonseIndicator.Length, errorResponse, 0, httpResponse.Length - errorRepsonseIndicator.Length);
         }

         return errorResponse;
      }

      /// <summary>log access to the server</summary>
      /// <param name="msg"></param>
      /// <param name="requestContent">content to be sent to server (relevant only for POST method - is null for other methods). may be byte[] or string.</param>
      private static void LogAccessToServer(String msg, object requestContent)
      {
         if (Logger.Instance.ShouldLogServerRelatedMessages())
         {
            if (!String.IsNullOrEmpty(msg))
               msg += "; accessing server ...";
            if (requestContent == null)
            {
               if (!String.IsNullOrEmpty(msg))
                  Logger.Instance.WriteServerToLog(msg);
            }
            else
            {
               if (!String.IsNullOrEmpty(msg))
                  msg += " ";
               msg += ("uploading " + HttpClient.GetRequestContentLength(requestContent) + " bytes");
               Logger.Instance.WriteServerToLog(msg);
            }
         }
      }

      /// <summary>
      /// returns true iff the url is a relative request to the runtime-engine.
      /// </summary>
      /// <param name="urlString"></param>
      /// <returns>true iff the url starts with: /requester?ctx=, e.g. /mgrequester19?ctx=</returns>
      internal static bool IsRelativeRequestURL(string urlString)
      {
         return urlString.StartsWith(("/" + ClientManager.Instance.getHttpReq() + "?" + ConstInterface.RC_TOKEN_CTX_ID), StringComparison.CurrentCultureIgnoreCase);
      }

      /// <summary>
      /// </summary>
      internal class CachingStrategy
      {
         // instructions:
         //--------------

         internal bool CachedContentShouldBeReturned { get; set; } // the cache manager is required (by the caller) to retrieve a requested URL from the local/client cache folder and return it to the caller. 
                                                                   // if true: the cache manager sets 'FoundInCache' after retrieving the content (from the local/client cache folder).
                                                                   // otherwise (false): the cache manager sets 'FoundInCache' by checking its existence, without retrieving the content from the server and without loading the content into the response's byte[].
         internal bool CanWriteToCache { get; set; }      // the cache manager is allowed (by the caller) to write content that was retrieved over HTTP to the local/client cache folder.

         internal bool AllowOutdatedContent { get; set; } // (Refines file search activity; required for offline applications - they must determine file existence in the client-side cache, regardless of the time stamps.) 
                                                          // when true: check only file existence at client cache.
                                                          // when false: check file existence and that the content isn't outdated (timestamps).

         // result:
         //--------

         internal bool FoundInCache { get; set; }         // the cache manager informs (the caller) whether a requested URL was retrieved from the local/client cache folder.
      }

      /// <summary>
      /// store an extra header for the request
      /// </summary>
      /// <param name="name"></param>
      /// <param name="value"></param>
      internal void AddOutgoingHeader(string name, string value)
      {
         _httpClient.AddRequestHeader(name, value);
      }

      /// <summary>
      /// clear the extra request header
      /// </summary>
      internal void ClearOutgoingHeaders()
      {
         _httpClient.ClearRequestHeaders();
      }
   }
}
