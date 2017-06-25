using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Specialized;
using com.magicsoftware.httpclient.utils;
using com.magicsoftware.httpclient.utils.compression;
using com.magicsoftware.unipaas;
using com.magicsoftware.unipaas.gui.low;
using com.magicsoftware.unipaas.util;
using com.magicsoftware.util;
using util.com.magicsoftware.util;

#if PocketPC
using OSEnvironment = com.magicsoftware.richclient.mobile.util.OSEnvironment;
#endif

namespace com.magicsoftware.httpclient
{
   /// <summary>
   /// this class is responsible for:
   ///    (i) deciding which method to use (GET/POST).
   ///   (ii) handling communication failures.
   ///  (iii) adding HTTP headers/cookies to requests.
   ///   (iv) compressing requests' data.
   ///    (v) handling the access to the web server.
   ///   (vi) handling authentication to proxy servers and web servers, and caching the credentials provided by the end-user for subsequent requests.
   ///  (vii) retrieving HTTP headers from responses.
   /// (viii) decompressing responses' data.
   /// </summary>
   public class HttpClient
   {
      #region REQUEST_METHOD enum

      /// <summary>Enumeration for method to be used while executing a HTTP request. This enum is
      ///   passed to executeHTTPRequest (...) which uses the enum value as index to access
      /// actual method string from the the array declared in the method.</summary>
      public enum RequestMethod
      {
         UNDEFINED = 0,
         GET,
         POST,
         HEAD,
         PUT,
         DELETE
      };

      #endregion

      private static string _compressionLevel;

      private readonly List<ISpecialAuthenticationHandler> _specialAuthHandlersList;
      private int _HTTPMaxURLLength = 2048;  //TODO: add a new execution property

      /// <summary>
      /// Gets or sets a handler for communications failure. This property may be
      /// set to null, in which case the HttpClient will automatically fail after
      /// the first reconnection attempt.
      /// </summary>
      public ICommunicationsFailureHandler CommunicationsFailureHandler { get; set; }

      private NameValueCollection _outgoingHeaders = new NameValueCollection();

      private WebHeaderCollection _lastResponseHeaders = null;

      /// <summary>
      /// this exception indicates to upper layers that:
      ///   1. the HttpClient was requested by an intermediate server to authenticate,
      ///   2. the end user canceled the authentication.
      /// </summary>
      public class CancelledAuthenticationException : Exception { }

      /// <summary>
      /// 
      /// </summary>
      public HttpClient( )
      {
         _specialAuthHandlersList = new List<ISpecialAuthenticationHandler>();
         CommunicationsFailureHandler = null;
         CreateSpecialResponseHandlers();
      }

      /// <summary>
      /// Creates special response handlers based on the contents of the execution.properties XML.
      /// </summary>
      private void CreateSpecialResponseHandlers()
      {
         string rsaCookies = HttpClientEvents.GetExecutionProperty(HttpClientConsts.RSA_COOKIES);
         if (!String.IsNullOrEmpty(rsaCookies) && !String.IsNullOrEmpty(rsaCookies.Trim()))
            _specialAuthHandlersList.Add(new RSAAuthenticationHandler(rsaCookies));
      }

#if !PocketPC
      /// <summary>
      /// Looks for an appropriate authentication handler in the list of registered handlers.
      /// If found, calls it to handle the HTTP response in order to authenticate the user.
      /// </summary>
      /// <param name="response"></param>
      /// <param name="urlString"></param>
      /// <param name="contentFromServer">the HTML or XML payload in the response, already decompressed if necessary</param>
      /// <returns>true only if special authentication was required and permission was granted.</returns>
      private bool AttemptSpecialAuthHandling(HttpWebResponse response, string urlString, byte[] contentFromServer)
      {
         if (_specialAuthHandlersList.Count > 0)
         {
            Logger.Instance.WriteServerToLog("HttpClient.AttemptSpecialAuthHandling() started");

            var formRect = new MgRectangle(0, 0, 800, 640);
            var authHandlerBrowserWindow = new AuthenticationBrowserWindow(formRect);

            foreach (ISpecialAuthenticationHandler handler in _specialAuthHandlersList)
            {
               if (handler.ShouldHandle(response, contentFromServer))
               {
                  Logger.Instance.WriteServerToLog("HttpClient.AttemptSpecialAuthHandling(): The handler " + handler
                                                 + " will handle the authentication");
                  authHandlerBrowserWindow.SpecialAuthenticationHandler = handler;

                  //the browser control with the aid of the handler should receive
                  //the authentication for the subsequent requests to use
                  authHandlerBrowserWindow.ShowContentFromURL(urlString, true);

                  if (handler.WasPermissionGranted)
                  {
                     Logger.Instance.WriteServerToLog(
                        "HttpClient.AttemptSpecialAuthHandling(): The permission was granted. Setting outgoing cookies: "
                        + handler.PermissionInfo);
                     SetOutgoingCookies(handler.PermissionInfo as string);
                     handler.ResetAuthenticationStatus(); //to take care of the case when authentication fails again in the same session, e.g. due to a timeout
                     return true;
                  }
                  throw new CancelledAuthenticationException();
               }
               if (handler.WasPermissionInfoUpdated)
               {
                  Logger.Instance.WriteServerToLog(
                     "HttpClient.AttemptSpecialAuthHandling(): The authentication cookie was apparently switched. Setting outgoing cookies: "
                     + handler.PermissionInfo);
                  SetOutgoingCookies(handler.PermissionInfo as string);
                  handler.ResetAuthenticationStatus();
               }
            }

            Logger.Instance.WriteServerToLog("HttpClient.AttemptSpecialAuthHandling() finished. Returning false.");
         }

         return false;
      }
#endif

      //   This is cache of Webserver credentials and ProxyServers instance. The key is
      //   protocol://server string. This cache is required because 
      //   a) There can be more than one webserver involved for example Images are accessed from
      //   different webserver. This webserver may have different credentials. 
      //   b) Different alias may have different credentials. For example credentials are given 
      //   only for MagicScripts and not for MgxpaRIACache.
      //   c) Different proxy servers are specified for different hosts/Urls
      //   d) These proxy server may have different credentials
      //   So this cache holds the protocol://server vs UrlAccessDetails. Where UrlAccessDetails contains
      //   Web Server credentials and instance of ProxyServers. While ProxyServers holds a proxy (WebProxy)
      //   along with credentials.
      private readonly Dictionary<string, UrlAccessDetails> _urlAccessDetailsTable = new Dictionary<string, UrlAccessDetails>();

      private ArrayList _outgoingCookies;
#if !PocketPC
      static private Boolean isUsingDefaultCredentials = true;
#endif

      //Sets a list of cookies and their values, to be sent on every HTTP request.
      //sCookies: name1=value1&name2=value2 ...
      //Saves the result into _outgoingCookies in order to be later passed as headers on HTTP request execution.
      public void SetOutgoingCookies(string cookiesString)
      {
         Debug.Assert(cookiesString != null);

         const string CS_COOKIES = "cookies=";
         int cookiesStartIndex = (cookiesString.IndexOf(CS_COOKIES) > -1
                                      ? cookiesString.IndexOf(CS_COOKIES) + CS_COOKIES.Length
                                      : 0);
         Debug.Assert(cookiesStartIndex > -1);

         string cookiesQuery = cookiesString.Substring(cookiesStartIndex);

         char[] externalCookieDelimiter = {','};
         char[] internalCookieDelimiter = {'='};

         string[] parsedCookies = cookiesQuery.Split(externalCookieDelimiter);

         if (_outgoingCookies == null)
            _outgoingCookies = new ArrayList(parsedCookies.Length);

         foreach (string parsedCookie in parsedCookies)
         {
            if (!String.IsNullOrEmpty(parsedCookie.Trim()))
            {
               string[] cookiePair = parsedCookie.Split(internalCookieDelimiter);
               Debug.Assert(cookiePair.Length == 2);
               var cookie = new Cookie(cookiePair[0], cookiePair[1]);
               SetCookie(cookie);
            }
         }
      }

      /// <summary>
      /// Returns an index of a cookie from the _outgoingCookies collection by name
      /// </summary>
      /// <param name="cookieName"></param>
      /// <returns>index or -1 if not found</returns>
      private int IndexOfCookie(string cookieName)
      {
         for (int i = 0; i < _outgoingCookies.Count; ++i)
         {
            if (((Cookie)_outgoingCookies[i]).Name == cookieName)
               return i;
         }
         return -1;
      }

      /// <summary>
      /// Adds a new cookie to the list or replaces the existing one
      /// </summary>
      /// <param name="cookie"></param>
      private void SetCookie(Cookie cookie)
      {
         int cookieIndex;
         if ((cookieIndex = IndexOfCookie(cookie.Name)) > -1)
            _outgoingCookies.RemoveAt((cookieIndex));

         _outgoingCookies.Add(cookie);
      }

      /// <summary>
      /// Returns the request method (POST or GET) based on its contents and length.
      /// </summary>
      /// <param name="requestContent">content to be sent to server (relevant only for POST method - is null for other methods). may be byte[] or string.</param>
      /// <param name="requestContentType"></param>
      /// <param name="requestURL"></param>
      /// <returns></returns>
      private RequestMethod DecideOnRequestMethod(object requestContent, String requestContentType, ref string requestURL)
      {
         RequestMethod method = RequestMethod.UNDEFINED;

         // the method optimizes the request method (favoring GET over POST whenever possible) for performance reasons (one roundtrip to the web server).
         if (requestContentType != null)
            // the requestContentType should be sent only for non-textual content, which can't be added to the URL of a GET request
            method = RequestMethod.POST;
         else
         {
            if (requestContent == null)
               // requestContent (== content to be sent to server) is allowed only in POST requests. In case no content is required, opt for GET (for the aforementioned performance reason).
               method = RequestMethod.GET;
            else 
            {
               Debug.Assert(requestContent is string);
               string textaulRequestContent = (requestContent as string);
               if (requestURL.Length + 1 + textaulRequestContent.Length <= _HTTPMaxURLLength) // +1 is for '?'
               {
                  // append the (already guaranteed to be textual and within allowed side) request content to the URL, and switch to using a GET request.
                  requestURL = requestURL + "?" + textaulRequestContent;
                  method = RequestMethod.GET;
               }
               else
                  method = RequestMethod.POST;
            }
         }
         Debug.Assert(method != RequestMethod.UNDEFINED);

         return method;
      }

      /// <summary>Gets contents of a URL, using either GET or POST methods. 
      /// The method is POST if 'requestContent' isn't null.
      /// The method executes the HTTP request, reads from the response stream, and return the content. 
      /// </summary>
      /// <param name="requestURL">URL to be accessed.</param>
      /// <param name="requestContent">content to be sent to server (relevant only for POST method - is null for other methods). may be byte[] or string.</param>
      /// <param name="requestContentType">type of the content to be sent to server, e.g application/octet-stream (for POST method only).</param>
      /// <param name="allowRetry">whether or not retry after connection failure.</param>
      /// <returns>response (from the server).</returns>
      public byte[] GetContent(String requestURL, object requestContent, String requestContentType, bool allowRetry)
      {
         byte[] contentFromServer;
         ulong sizeUploadedAfterCompression = 0, //in bytes
               sizeDownloadedBeforeDecompression = 0; //in bytes
         bool isCompressed = false;
         Compressor lzmaCompressor = null;

         RequestMethod method = DecideOnRequestMethod(requestContent, requestContentType, ref requestURL);

         try
         {
            // WebRequeseter / RC will use compression level value to decide whether to compress / decompress input data.
            string compressionLevel = GetCompressionLevel();

            // QCR#:931933:Changed the datatype of the variable 'compressionMessageSizeThreshold' from short to double,
            // so that larger as well as fractional value can be given.
            // requestContent.Length should be 3072 or greater (i.e 3kb) requestContent.Length >= 3072
            double compressionMessageSizeThreshold = 3072;
            if (compressionLevel.IndexOf('>') >= 0)
            {
               // "level>threshold" --> "level", "threshold"
               string[] compressionLevelPair = compressionLevel.Split('>');
               compressionLevel = compressionLevelPair[0];
               compressionMessageSizeThreshold = double.Parse(compressionLevelPair[1]);
            }
            if (String.Compare(compressionLevel, "NONE", true) != 0 
                && requestContent != null 
                && GetRequestContentLength(requestContent) >= compressionMessageSizeThreshold)
            {
               lzmaCompressor = LZMACompressorsFactory.Instance.getAvailableCompressor();
               requestContent = lzmaCompressor.compress(ToByteArray(requestContent), compressionLevel);
               isCompressed = true;
            }

            if (requestContent != null)
               sizeUploadedAfterCompression = (ulong)GetRequestContentLength(requestContent);

            bool isNewlyAuthenticated = false;
            do
            {
               //Execute the http request
               HttpWebResponse response = ExecuteHttpRequest(requestURL, requestContent, requestContentType, method, isCompressed,
                                                             allowRetry, out contentFromServer);
               if (response != null)
               {
                  _lastResponseHeaders = response.Headers;

                  if (response.Headers.Count > 0)
                  {
                     Logger.Instance.WriteServerToLog("Incoming Headers : " + HeadersToString(response.Headers, true));

                     // set the next session counter (which will be expected by the server in the next request).
                     String nextSessionCounterString = response.Headers.Get("MgxpaNextSessionCounter");
                     if (nextSessionCounterString != null) // only an xpa server returns this header; access to a non-xpa URL doesn't return the header, and shouldn't control the session counter.
                        HttpClientEvents.CheckAndSetSessionCounter(long.Parse(nextSessionCounterString));

                     // the web requester is expected to send on the 1st response a header informing the client whether the web requester supports compression.
                     if (HttpClientEvents.IsFirstRequest())
                     {
                        String webRequesterSupportsCompression = response.Headers.Get("MgxpaSupportsCompression");
                        if (webRequesterSupportsCompression != null)
                        {
                           Debug.Assert(webRequesterSupportsCompression.Equals("Y", StringComparison.CurrentCultureIgnoreCase));
                           SetCompressionLevel();
                        }
                     }
                  }

                  if (contentFromServer != null)
                  {
                     sizeDownloadedBeforeDecompression = (ulong)contentFromServer.Length;

                     // decompress
                     if (contentFromServer.Length > 0)
                     {
                        // decompress
                        if (String.Compare(response.Headers.Get("MgxpaCompressedMessage"), "Y", true) == 0)
                        {
                           if (lzmaCompressor == null)
                              lzmaCompressor = LZMACompressorsFactory.Instance.getAvailableCompressor();
                           contentFromServer = lzmaCompressor.decompress(contentFromServer);
                        }
#if !PocketPC
                        // Checks all the authentication handlers to see if one of them can handle the response.
                        // If false is returned, this means either that no such handler was found (either because
                        // the response was NOT an authentication error or it WAS one, but it
                        // is not supported or some handler was found, but the authentication process has failed
                        // or canceled. In terms of the flow, there is no distinction between all these cases.
                        // If true is returned, it means that the HTTP response was an authentication error response
                        // from the server, a suitable handler was found and the authentication was performed successfully.
                        // In such case, executeHTTPRequest() is attempted again with the same request URL.
                        if (AttemptSpecialAuthHandling(response, requestURL, contentFromServer))
                        {
                           //sets the value to false if we're here for the second time after the authentication,
                           //so we don't continue for the third time (see the "while(isNewlyAuthenticated)")
                           isNewlyAuthenticated = !isNewlyAuthenticated;
                           Logger.Instance.WriteServerMessagesToLog("HttpClient.GetContent(): AttemptSpecialHandling() succeeded, isNewlyAuthenticated = " + isNewlyAuthenticated);
                        }
                        // this can happen if the problem is not authentication-related or there was an error 
                        // during an authentication process or it was canceled
                        else
                        {
                           isNewlyAuthenticated = false;
                           Logger.Instance.WriteServerMessagesToLog("HttpClient.GetContent(): AttemptSpecialHandling(): No handler found or handler failed, isNewlyAuthenticated = false");
                        }
#endif
                     }
                  }

                  // updated the uploaded and downloaded data size on the session statistics form
                  HttpClientEvents.UpdateUpDownDataSizes(sizeUploadedAfterCompression, sizeDownloadedBeforeDecompression);
                  HttpClientEvents.ProcessInternalHttpHeaders(response);

                  response.Close();
               }
            }
            while (isNewlyAuthenticated);
         }
         catch (Exception ex)
         {
            Logger.Instance.WriteWarningToLog(ex);
            throw;
         }
         finally
         {
            if (lzmaCompressor != null)
               lzmaCompressor.release();
         }

         return (contentFromServer);
      }

      /// <summary>
      /// </summary>
      /// <param name="requestContent">content to be sent to server (relevant only for POST method - is null for other methods). may be byte[] or string.</param>
      /// <returns>the length of 'requestContent'.</returns>
      public static int GetRequestContentLength(object requestContent)
      {
         int length = 0;

         if (requestContent is byte[])
            length = ((byte[])requestContent).Length;
         else
         {
            Debug.Assert(requestContent is string);
            length = ((string)requestContent).Length;
         }

         return length;
      }

      ///<summary>
      ///  !!.
      ///</summary>
      ///<returns>!!.</returns>
      public String GetLastResponseHeaders()
      {
         return (_lastResponseHeaders != null
                     ? HeadersToString(_lastResponseHeaders, false).ToString()
                     : String.Empty);
      }

      /// <summary>
      /// </summary>
      /// <param name="requestContent">content - either byte[] or string.</param>
      /// <returns>byte[] content (converted if the given content wasn't byte[]).</returns>
      private byte[] ToByteArray(object requestContent)
      {
         return (requestContent is byte[]
                   ? (byte[])requestContent
                   : System.Text.Encoding.UTF8.GetBytes((string)requestContent));
      }

      /// <summary>This function executes the HTTP request and make the response object. It can execute
      ///   GET, POST or HEAD request. In case of POST request the variables to server will contain the
      ///   variables to be send to the server. 
      ///   This function also handles 
      ///   1) WebServer, Proxy Authentication : In case UrlAccessDetails contains blank credentials for
      ///   web/proxy server or if it contains wrong credentials the request will fail, in such case the
      ///   function will show authentication dialog and accept credentials from user and then it will try 
      ///   to execute the request with the new credentials
      ///   2) Proxy Recovery : In case connection to proxy server is failed then the function will check if
      ///   there are more than one proxy are available if yes then
      ///   a) It will try with another proxy and this will continue until it can make connection 
      ///   or HttpTimeout is elapsed.
      ///   If there is only one proxy then
      ///   b) It will try to connect the same proxy again after waiting for HttpTimeout/10 sec. it will
      ///   continue doing it until it can make connection or HttpTimeout is elapsed.
      /// </summary>
      /// <param name="urlString">URL to be accessed.</param>
      /// <param name="requestContent">content to be sent to server (relevant only for POST method - is null for other methods). may be byte[] or string.</param>
      /// <param name="requestContentType">type of the content to be sent to server, e.g application/octet-stream (for POST method only).</param>
      /// <param name="httpMethod">enum REQUEST_METHOD to specify the method that will be used to execute the request.</param>
      /// <param name="isCompressed"></param>
      /// <param name="allowRetry">whether or not retry after connection failure.</param>
      /// <param name="contentFromServer">content received from the response. [OUT]</param>
      /// <returns></returns>
      private HttpWebResponse ExecuteHttpRequest(String urlString, object requestContent, String requestContentType,
                                                 RequestMethod httpMethod, bool isCompressed,
                                                 bool allowRetry, out byte[] contentFromServer)
      {
         var url = new Uri(urlString);

         contentFromServer = null;

         AuthenticationDialogHandler dialog = null;
         HttpWebRequest request = null;
         HttpWebResponse response;
         
         uint httpCommunicationTimeoutMS = HttpClientEvents.GetHttpCommunicationTimeout(); // communication-level timeout (i.e. the access to the web server, NOT the entire request/response round-trip).

         string clientID = HttpClientEvents.GetGlobalUniqueSessionID();
         string compressionLevel = GetCompressionLevel();

         long startTime = Misc.getSystemMilliseconds();
         ushort executionAttempts = 0; // for logging purpose only.

         // Retrying:
         //    Is controlled by: 
         //       (I)   The parameter 'allowRetry', 
         //       (II)  The method variable 'httpCommunicationTimeoutMS' (above),
         //       (III) The class member 'CommunicationsFailureHandler' (above).
         //    Handles: 
         //       (I)   Web server and proxy server authentications, 
         //       (II)  Network and servers failures.
         while (true)
         {
            executionAttempts++;
            try
            {
               try
               {
                  if (GetUseHighestSecurityProtocol())
                  {
                     //Following TLS handshake protocol, we must start negotiation from the highest supported TLS protocol version. If that version isn't supported by other side, 
                     // negotiation falls back to lower versions of TLS and then to SSL. 
                     //.NET v4.5 platform highest TLS protocol version is TLS1.2
                     ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;//TODO: when the project will be built at .NET v4.5 platform, replace "3072" by SecurityProtocolType.Tls12
                  }

                  request = (HttpWebRequest)WebRequest.Create(url);
                  request.Timeout = -1;                                         // timeout encompassing the entire request/response round-trip. -1 == unlimited.
                  request.ReadWriteTimeout = (Int32)httpCommunicationTimeoutMS; // communication-level timeout (i.e. the access to the web server, NOT the entire request/response round-trip).
                  Logger.Instance.WriteServerToLog(string.Format("HttpWebRequest.ReadWriteTimeout set to {0} ms", request.ReadWriteTimeout));

                  AddHeadersAndCookies(clientID, compressionLevel, isCompressed, request.Headers);

                  // Set the request method (GET/POST/HEAD/..).
                  request.Method = httpMethod.ToString();

                  if (Logger.Instance.LogLevel == Logger.LogLevels.Basic)
                     Logger.Instance.WriteBasicToLog (Logger.MessageDirection.MessageLeaving,
                                                      HttpClientEvents.GetRuntimeCtxID(),
                                                      HttpClientEvents.GetSessionCounter(),
                                                      clientID,
                                                      HttpClientEvents.ShouldDisplayGenericError() ? "-" : request.Address.Host,
                                                      0,
                                                      HttpStatusCode.Unused,
                                                      request.Headers,
                                                      request.Method == "GET" ? request.RequestUri.Query.Length - 1 : request.ContentLength);
                  long timeBeforeRequest = Misc.getSystemMilliseconds();

#if !PocketPC
                  // Set Proxy Server details (if specified). 
                  SetProxyServer(request, urlString);
#endif
                  // Set Web Server credentials (if specified). 
                  SetWebServerCredentials(request, urlString);

                  // logging:
                  Logger.Instance.WriteServerToLog(String.Format("Accessing (method: '{0}'): '{1}'", request.Method, urlString));

                  if (request.Headers.Count > 0)
                     Logger.Instance.WriteServerToLog("Outgoing Headers : " + HeadersToString(request.Headers, false));

                  //=============================================================================================================
                  // send the request:
                  //===================
                  if (httpMethod == RequestMethod.POST)
                  {
                     if (requestContentType != null)
                        request.ContentType = requestContentType;

                     // Sets/Removes HTTP header "Expect:100Continue" More about this header can be found here:
                     // http://msdn.microsoft.com/en-us/library/system.net.servicepoint.expect100continue%28VS.71%29.aspx
#if PocketPC
                     if (request.ServicePoint != null)
#endif
                     request.ServicePoint.Expect100Continue = GetHTTPExpect100Continue();

                     WriteContentToRequest(requestContent, request);
                  }

                  //===================
                  // get the response:
                  //===================
                  response = (HttpWebResponse)request.GetResponse();
                  if (httpMethod != RequestMethod.HEAD)
                  {
                     // Read the contents.
                     Stream stream = response.GetResponseStream();
#if !PocketPC
                     stream = new BufferedStream(stream);
#endif
                     contentFromServer = ReadFully(stream);
                  }
                  //=============================================================================================================

                  long responseTime = Misc.getSystemMilliseconds() - timeBeforeRequest;
                  if (Logger.Instance.LogLevel == Logger.LogLevels.Basic)
                     Logger.Instance.WriteBasicToLog(Logger.MessageDirection.MessageEntering,
                                                     HttpClientEvents.GetRuntimeCtxID(),
                                                     HttpClientEvents.GetSessionCounter(), 
                                                     clientID,
                                                     HttpClientEvents.ShouldDisplayGenericError() ? "-" : request.Address.Host,
                                                     responseTime,
                                                     response.StatusCode, 
                                                     response.Headers,
                                                     response.ContentLength);
               }
               catch (SocketException ex)
               {
                  if (Logger.Instance.LogLevel == Logger.LogLevels.Basic)
                      Logger.Instance.WriteBasicErrorToLog(HttpClientEvents.GetRuntimeCtxID(),
                                                           HttpClientEvents.GetSessionCounter(),
                                                           clientID,
                                                           HttpClientEvents.ShouldDisplayGenericError() ? "-" : request.Address.Host, ex);
                  else
                     Logger.Instance.WriteWarningToLog(ex);
                  throw new WebException("SocketException --> WebException: ", ex, WebExceptionStatus.ConnectFailure, null);
               }
               catch (IOException ex)
               {
                  if (Logger.Instance.LogLevel == Logger.LogLevels.Basic)
                      Logger.Instance.WriteBasicErrorToLog(HttpClientEvents.GetRuntimeCtxID(),
                                                           HttpClientEvents.GetSessionCounter(),
                                                           clientID,
                                                           HttpClientEvents.ShouldDisplayGenericError() ? "-" : request.Address.Host, ex);
                  else
                     Logger.Instance.WriteWarningToLog(ex);
                  throw new WebException("IOException --> WebException: ", ex, WebExceptionStatus.ConnectFailure, null);
               }
               break;
            }
            catch (WebException ex)
            {
               if (Logger.Instance.LogLevel == Logger.LogLevels.Basic)
                   Logger.Instance.WriteBasicErrorToLog(HttpClientEvents.GetRuntimeCtxID(),
                                                        HttpClientEvents.GetSessionCounter(),
                                                        clientID,
                                                        HttpClientEvents.ShouldDisplayGenericError() ? "-" : request.Address.Host, ex);
               else
                  Logger.Instance.WriteWarningToLog(ex);

               if (IsAuthenticationRequired(ex))
               {
                  // Retry if proxy authentication is NTLM (it should not happen, but in reality it does).
                  string proxyAuth = null;
                  if (ex.Status == WebExceptionStatus.ProtocolError &&
                      ((HttpWebResponse)ex.Response).StatusCode == HttpStatusCode.ProxyAuthenticationRequired)
                     proxyAuth = ex.Response.Headers["Proxy-Authenticate"];

                  if (proxyAuth != null && proxyAuth.StartsWith("NTLM"))
                     continue;
#if !PocketPC
                  isUsingDefaultCredentials = false;
#endif
                  // WebServer/Proxy Server Authentication handling.
                  // if web server or proxy requires authentication.HTTP status 401 or 407.
                  // In such case:
                  //    a) Show Authentication dialog and accept credentials
                  //    b) Save the credentials for next request.
                  Logger.Instance.WriteServerToLog("Authentication required");
                  var errResponse = (HttpWebResponse)ex.Response;
                  if (IsUserRequiredToInputCredentials(request, errResponse, urlString))
                  {
                     dialog = ShowAuthenticationDialog(dialog, request, errResponse);
                     UsernamePasswordCredentials credentials = dialog.getCredentials();
                     if (credentials != null)
                     {
                        SaveCredentials(request, errResponse, credentials);
                        continue;
                     }
                  }
                  else
                     continue;
               }
               else if (ProxyAccessFailed(ex, request))
               {
                  //Proxy Recovery :
                  Logger.Instance.WriteServerToLog("Proxy Recovery");
                  Logger.Instance.WriteServerToLog("Failed to access Proxy Server " +
                                                 ((WebProxy)request.Proxy).Address);

                  //Get the proxyServer object that was passed to this function.
                  ProxyServers proxyServers = GetProxyServers(request.RequestUri.ToString());

                  // Continue choosing proxies until HttpTimeout is elapsed.
                  long currTime = Misc.getSystemMilliseconds();
                  if (currTime - startTime <= httpCommunicationTimeoutMS)
                  {
                     if (proxyServers.Count() > 1)
                     {
                        // Set the current proxy as not responsive. Next call to SetProxyServer will select next responsive proxy.
                        proxyServers.SetResponsive((WebProxy) request.Proxy, false);
                        Logger.Instance.WriteServerToLog("Select Next proxy server");
                     }
                     else
                     {
                        // b) If there is only 1 proxy wait for httptimeout/10 sec and try again 
                        // to connect through same proxy, continue doing it until HttpTimeout is
                        // elapsed (httpTimeoutElapsed <= totalHttpTimeout).
                        Logger.Instance.WriteServerToLog(string.Format("Waiting for {0}ms. Total timeout : {1} ms. Time elasped {2} ms.",
                                                                httpCommunicationTimeoutMS/10, httpCommunicationTimeoutMS, currTime - startTime));
                        Thread.Sleep((int) httpCommunicationTimeoutMS/10);
                        Logger.Instance.WriteServerToLog(string.Format("Try the proxy Server {0} again",
                                                                ((WebProxy)request.Proxy).Address));
                     }
                     continue;
                  }
               }
               else if (ex.Status == WebExceptionStatus.ReceiveFailure &&
                        request.Address.Equals(((WebProxy)request.Proxy).Address))
               {
                  Logger.Instance.WriteServerToLog("Proxy credentials couldn't be reused");

                  // in case the request failed due to wrong credentials that were attempted to 
                  // be reused from CredentialsCache (lastUsedCredentials), retry without credentials
                  if (((WebProxy)request.Proxy).Credentials != null)
                  {
                     ((WebProxy)request.Proxy).Credentials = null;
                     continue;
                  }
               }
               else // allow to retry a (temporarily) failed request or web server.
               {
                  // status 404 (Not Found) or 403 (Forbidden) aren't retried, since they can't be recovered.
                  if (ex.Response != null &&
                      (((HttpWebResponse)ex.Response).StatusCode == HttpStatusCode.NotFound ||
                       ((HttpWebResponse)ex.Response).StatusCode == HttpStatusCode.Forbidden))
                  {
                     Logger.Instance.WriteServerToLog(string.Format("ex.Response.StatusCode: {0}",
                                                                    ((HttpWebResponse)ex.Response).StatusCode));
                     throw;
                  }

                  // delay the total http timeout / 10.
                  var currentDelayMS = httpCommunicationTimeoutMS / 10; //ms
                  long httpElapsedTimeMS = Misc.getSystemMilliseconds() - startTime + currentDelayMS;
                  if (httpElapsedTimeMS <= httpCommunicationTimeoutMS)
                  {
                     Thread.Sleep((int)currentDelayMS);
                     Logger.Instance.WriteWarningToLog(string.Format("Retrying {0} : elapsed time {1:N0}ms out of {2:N0}ms",
                                                                     urlString, httpElapsedTimeMS, httpCommunicationTimeoutMS));
                     continue;
                  }
                  Logger.Instance.WriteWarningToLog(string.Format("{0} : http timeout {1:N0}ms expired",
                                                                  urlString, httpCommunicationTimeoutMS));
                  if (allowRetry && CommunicationsFailureHandler != null)
                  {
                     CommunicationsFailureHandler.CommunicationFailed(urlString, ex);
                     if (CommunicationsFailureHandler.ShouldRetryLastRequest)
                     {
                        Logger.Instance.WriteServerToLog(string.Format("Retrying {0}, confirmed by user ...", urlString));
                        startTime = Misc.getSystemMilliseconds();
                        continue;
                     }
                  }
               } // ProtocolError

               Logger.Instance.WriteWarningToLog("Re-throwing ...");
               Logger.Instance.WriteWarningToLog(ex);
               throw ex;
            } //catch (WebException ex)
            catch (Exception ex)
            {
               Logger.Instance.WriteWarningToLog("Re-throwing ...");
               Logger.Instance.WriteWarningToLog(ex);
               throw;
            }
         } //while (true)

         if (dialog != null)
            dialog.closeDialog();

         if (executionAttempts > 1)
            Logger.Instance.WriteServerToLog(String.Format("Succeeded after {0} attempts ...", executionAttempts));
         return response;
      }

      ///<summary>
      ///  Adds headers and cookies into 'requestHeaders', based on the passed parameters and class members ('_outgoingHeaders' and '_outgoingCookies').
      ///</summary>
      ///<param name="clientID">!!.</param>
      ///<param name="compressionLevel">!!.</param>
      ///<param name="isCompressed">!!.</param>
      ///<param name="requestHeaders">Headers collection populated by the method. [REF]</param>
      private void AddHeadersAndCookies(string clientID, string compressionLevel, bool isCompressed, WebHeaderCollection requestHeaders)
      {
         if (clientID != null)
            requestHeaders.Add("MgxpaRIAglobalUniqueSessionID", clientID);

         // inform the web requester about the level of compression
         // to use while decompressing the request and/or compressing the response.
         if (compressionLevel != HttpClientConsts.HTTP_COMPRESSION_LEVEL_NONE)
            requestHeaders.Add("MgxpaCompressionLevel", compressionLevel);

         // inform the web requester that the request is compressed (i.e. should be decompressed).
         if (isCompressed)
            requestHeaders.Add("MgxpaCompressedMessage", "Y");

         for (ushort i = 0; i < _outgoingHeaders.Count; i++)
            requestHeaders.Add(_outgoingHeaders.GetKey(i), _outgoingHeaders.GetValues(i)[0]);

         //if cookies were specified in the ActivationUri query string, (e.g. for authentication reasons) then send them in each request as headers
         string outgoingCookies = "";
         if (_outgoingCookies != null)
         {
            foreach (Cookie ck in _outgoingCookies)
               outgoingCookies += String.Format("{0}={1};", ck.Name, ck.Value);

            char[] charToTrim = { ';' };
            outgoingCookies = outgoingCookies.TrimEnd(charToTrim);
            requestHeaders.Add("Cookie", outgoingCookies);
         }
      }

      /// <summary>Write Mg* prefixed headers to string in format "HEADER1:VALUE1 HEADER2:VALUE2 ..."</summary>
      /// <param name="headers"></param>
      /// <param name="bFilter">if true, list only headers prefixed with "Mg"</param>
      /// <returns></returns>
      private static StringBuilder HeadersToString(WebHeaderCollection headers, bool bFilter)
      {
         string[] headerNames = headers.AllKeys;
         var headersStr = new StringBuilder();

         foreach (String header in headerNames)
         {
            // filter only headers that are prefixed with Mg* (sent from the Middleware and Server):
            if (!bFilter || header.StartsWith("Mg"))
               headersStr.Append(string.Format("{0}:{1} ", header, headers.Get(header)));
         }

         return headersStr;
      }

      /// <summary>Checks if the exception is ProxyResolutionFailure. If yes then we are vary much sure that we have to recover proxy.
      ///  If this is not the case then we will look into the inner exception details and look for the IP address in the exception
      ///  details.If this IP address matches with the IP of the proxy, then we have to recover for the proxy.
      ///</summary>
      ///<param name = "ex"></param>
      ///<param name = "request"></param>
      ///<returns></returns>
      private static bool ProxyAccessFailed(WebException ex, WebRequest request)
      {
         bool proxyAccessFailed = false;

         // if exception is ProxyNameResolutionFailure then we must recover proxy.
         if (ex.Status == WebExceptionStatus.ProxyNameResolutionFailure)
            proxyAccessFailed = true;
         else if (ex.Status == WebExceptionStatus.ConnectFailure)
         {
            // if exception is ConnectFailure then check if the ip address of proxy is present in
            // inner exception message, if its there it is the proxy that has failed.
            if ((request.Proxy) != null && (((WebProxy)request.Proxy).Address != null))
               proxyAccessFailed = IsIPInException(ex, ((WebProxy)request.Proxy).Address.Host);
         }

         if (proxyAccessFailed)
            Logger.Instance.WriteWarningToLog("Proxy Server Access Failed");
         return proxyAccessFailed;
      }

      /// <summary>Checks whether the IP is in the innner exception detail or not.This is required to determine on which machine the problem has arised.
      /// The IP of the machine which is not behaving properly is returned as a part of the inner exception. Hence we check for inner exception.
      /// If IP is of proxy, then recovery should be on proxy or if IP is of web server, then recovery should be on web server.
      /// </summary>
      /// <param name = "ex"></param>
      /// <param name="hostAddr"></param>
      /// <returns></returns>
      private static bool IsIPInException(WebException ex, string hostAddr)
      {
         bool ipInExecption = false;

         //Getting the inner exception value.
         if (ex.InnerException != null)
         {
            Logger.Instance.WriteWarningToLog(ex.InnerException);
            string strException = ex.InnerException.Message;
            /*---------------------------------------------------------------------------*/
            /* When we pass IP address to GetHostEntry () first the hostname is resolved */
            /* from IP address and then using the hostname all possible IP addresses are */
            /* retrieved. In some cases (for example proxy is 130.9.3.16) IPHostEntry.   */
            /* AddressList contains invalid address (127.0.0.2)                          */
            /* Ref : http://msdn.microsoft.com/en-us/library/ms143998%28v=VS.100%29.aspx */
            /* so first check if hostAddr is valid IP address, if yes it can be directly */
            /* used. In case we get FormatException (ie. hostAddr is not IP address) then*/
            /* resolve it with GetHostEntry                                              */
            /*---------------------------------------------------------------------------*/
            try
            {
               ipInExecption = (strException.IndexOf(hostAddr) > 0);
            }
            catch (FormatException e)
            {
               Logger.Instance.WriteDevToLog(e.Message);
               // Not an IP address try to resolve with GetHostEntry
               IPHostEntry hostEntry = Dns.GetHostEntry(hostAddr);
               // Search in all ip address in the AddressList
               foreach (IPAddress address in hostEntry.AddressList)
               {
                  if (strException.IndexOf(address.ToString()) > 0)
                  {
                     ipInExecption = true;
                     break;
                  }
               }
            }
         }
         return ipInExecption;
      }

      /// <summary>Checks if the WebException is for authentication required.</summary>
      /// <param name = "e"></param>
      /// <returns></returns>
      private static bool IsAuthenticationRequired(WebException e)
      {
         return (e.Status == WebExceptionStatus.ProtocolError &&
                 (((HttpWebResponse)e.Response).StatusCode == HttpStatusCode.Unauthorized ||
                  ((HttpWebResponse)e.Response).StatusCode == HttpStatusCode.ProxyAuthenticationRequired));
      }

      /// <summary>Save the credentials (both proxy and webserver). 
      /// This function along with SetWebServerCredentials() and SetProxyServer () 
      ///   does the job of storing and using correct credentials. i.e. When new credentials are accepted 
      ///   this function saves the credentials in a cache (UrlAccessDetailsTable). 
      ///   While when it comes to execute a request, SetWebServerCredentials() and SetProxyServer() 
      ///   function queries this cache and uses the results to set in HttpWebRequest method that is used to execute Http request.
      /// 
      /// Our Authentication dialog does not distinguish between Webserver/Proxy Server authentication
      ///   i.e. if there is HTTP:401 or HTTP:407, we just show a authentication dialog and accept the
      ///   credentials, these credentials are passed to this function. This function decides whether to
      /// save them as Webserver credentials or Proxy Server credentials depending on Http Status in the error.</summary>
      /// <param name = "request"></param>
      /// <param name = "errorResponse">HttpWebResponse that contains error.</param>
      /// <param name = "credentials">UsernamePasswordCredentials Web/Proxy Server credentials that 
      ///   are accepted from user.</param>
      private void SaveCredentials(WebRequest request, HttpWebResponse errorResponse,
                                   UsernamePasswordCredentials credentials)
      {
         string username, domain;

         //split the username (domain\user) in to domain and user.
         SplitUserName(credentials.Username, out username, out domain);
         //Prepare network credentials from UsernamePasswordCredentials.
         var networkCredentials = new NetworkCredential(username, credentials.Password, domain);

         switch (errorResponse.StatusCode)
         {
            case HttpStatusCode.Unauthorized: // HTTP : 401
               SaveWebServerCredentials(request.RequestUri.ToString(), networkCredentials);
               break;
            case HttpStatusCode.ProxyAuthenticationRequired: //HTTP : 407
               SaveProxyServerCredentials(request.RequestUri.ToString(), (WebProxy)request.Proxy, networkCredentials);
               break;
         }
      }

      /// <summary>
      /// This function checks that the proxy against which the previous request was authenticated supports
      /// the Integrated authentication protocol, and if so, sets DefaultCredentials to the WebProxy's 
      /// Credentials property that will be later used for additional authentication attempt; also, false
      /// is returned since there is no need for a user to enter their credentials manually. Otherwise, true
      /// is returned.
      /// </summary>
      /// <param name="request"></param>
      /// <param name="errorResponse"></param>
      /// <param name="finalEndPointUrl"></param>
      /// <returns>whether additional input of credentials is still required</returns>
      private bool IsUserRequiredToInputCredentials(WebRequest request, 
                                                    HttpWebResponse errorResponse, 
                                                    string finalEndPointUrl)
      {
         bool credentialsUserInputRequired = true;
         var proxy = (WebProxy) request.Proxy;
         string proxyAuthenticate = (errorResponse.Headers).Get("Proxy-Authenticate");
         IsIntegratedAuthSupportedByProxy(request.RequestUri.ToString(), proxy, proxyAuthenticate);

         ProxyServers proxyServers = GetProxyServers(finalEndPointUrl);
         bool isIntegratedAuthSupported = proxyServers.SupportsIntegratedAuthentication((WebProxy)request.Proxy);

         if (isIntegratedAuthSupported)
         {
            proxy.Credentials = CredentialCache.DefaultCredentials;
            credentialsUserInputRequired = false;
         }

         return credentialsUserInputRequired;
      }

      /// <summary>
      /// Parses the value of Proxy-Authenticate header and determines if integrated authentication
      /// is supported by the specified web proxy, then saves this info to the proxy cache.
      /// </summary>
      /// <param name="finalEndPointUrl"></param>
      /// <param name="proxy"></param>
      /// <param name="proxyAuthenticate">if not null, the proxy required authentication.</param>
      /// <returns></returns>
      private void IsIntegratedAuthSupportedByProxy(string finalEndPointUrl,
                                                     WebProxy proxy,
                                                     string proxyAuthenticate)
      {
#if !PocketPC
         string srvAliasKey = MakeKeyFromUrl(finalEndPointUrl);
         UrlAccessDetails details;

         if (_urlAccessDetailsTable.TryGetValue(srvAliasKey, out details))
         {
            ProxyServers proxyServers = details.GetProxyServers();
            bool isIntegratedAuthSupported = false;

            if (proxyAuthenticate != null && proxyAuthenticate.ToLower().Contains("negotiate"))
               isIntegratedAuthSupported = true;

            proxyServers.SupportsIntegratedAuthentication(proxy, isIntegratedAuthSupported);
            Logger.Instance.WriteServerToLog("INFO : Got proxy for " + finalEndPointUrl + " and set its supported authentication protocols");
         }
         else
            Debug.Assert(false);
#endif
      }

      /// <summary>Sets the Credentials required to access the URL pointed by finalEndPointURL. 
      /// This function queries the cache for credentials for a URL. 
      /// This function sets this credentials in HttpWebRequest which is used to access the URL.
      ///   This function works along with SaveCredentials, which is responsible for caching the credentials.
      /// </summary>
      /// <param name = "request"></param>
      /// <param name = "finalEndPointUrl">string URL for which credentials needs to be set</param>
      private void SetWebServerCredentials(WebRequest request, string finalEndPointUrl)
      {
         NetworkCredential webserverCreds = GetWebServerCredentials(finalEndPointUrl);
         request.Credentials = (webserverCreds ?? CredentialCache.DefaultCredentials);

         //send an HTTP Authorization header with requests after authentication has taken place
         request.PreAuthenticate = true;
      }

      /// <summary>
      ///   Make key protocol://server from full URL
      /// </summary>
      /// <returns></returns>
      private static string MakeKeyFromUrl(string finalEndPointUrl)
      {
         Debug.Assert(finalEndPointUrl != null);
         var uri = new Uri(finalEndPointUrl);
         return uri.GetLeftPart(UriPartial.Authority).ToUpper();
      }

      /// <summary>
      ///   Saves /Updates web server credentials for a protocol://server key in cache. This function makes a key from
      ///   URL and then looks for the key. The key must be there in the cache. (Otherwise its error condition)
      ///   then the web server credentials will be updated with the credentials passed to the function.
      /// </summary>
      /// <param name = "finalEndPointUrl">string from which key will be generated</param>
      /// <param name = "networkCredentials">NetworkCedential credentials that needs to be saved.</param>
      private void SaveWebServerCredentials(string finalEndPointUrl, NetworkCredential networkCredentials)
      {
         string srvAliasKey = MakeKeyFromUrl(finalEndPointUrl);
         UrlAccessDetails details;

         if (_urlAccessDetailsTable.TryGetValue(srvAliasKey, out details))
         {
            //Update the web server credentials in the cache.
            details.SetWebServerCredentials(networkCredentials);
         }
         else
         {
            Debug.Assert(false);
         }
      }

      /// <summary>
      ///   This function saves proxy credentials in the cache. This function makes the key from URL, then looks this key
      ///   in the cache.  The key must be there in the cache. (Otherwise its error condition). Then actual credentials are
      ///   passed to the ProxyServers object who will save the credentials in another cache maintained by ProxyServers.
      /// </summary>
      /// <param name = "finalEndPointUrl">URL from which key will be generated</param>
      /// <param name = "proxy">Webproxy whose credentials are to be set.</param>
      /// <param name = "networkCredentials">NetworkCrdential credentials that need to be saved</param>
      private void SaveProxyServerCredentials(string finalEndPointUrl, WebProxy proxy,
                                              NetworkCredential networkCredentials)
      {
         string srvAliasKey = MakeKeyFromUrl(finalEndPointUrl);
         UrlAccessDetails details;
         if (_urlAccessDetailsTable.TryGetValue(srvAliasKey, out details))
         {
            ProxyServers proxyServers = details.GetProxyServers();
            proxyServers.SetCredentials(proxy, networkCredentials);
            Logger.Instance.WriteServerToLog(string.Format("Got proxy for {0} and set its credentials", finalEndPointUrl));
         }
         else
         {
            Debug.Assert(false);
         }
      }

      /// <summary>
      ///   This function gets ProxyServers object for protocol://server specified in this URL.It looks for the key in the
      ///   cache , if found that will be returned, otherwise a new blank record will be inserted in the cache and 
      ///   ProxyServers from the new record will be returned.
      /// </summary>
      /// <param name = "finalEndPointUrl">URL from which key will be generated</param>
      /// <returns></returns>
      private ProxyServers GetProxyServers(string finalEndPointUrl)
      {
         UrlAccessDetails details = GetUrlAccessDetails(finalEndPointUrl);
         return details.GetProxyServers();
      }

      /// <summary>
      ///   This function gets Webserver credentials for protocol://server specified in this URL.It looks for the key in the
      ///   cache , if found that will be returned, otherwise a new blank record will be inserted in the cache and 
      ///   Webserver credentials from the new record will be returned.
      /// </summary>
      /// <param name = "finalEndPointUrl">URL from which key will be generated</param>
      /// <returns></returns>
      private NetworkCredential GetWebServerCredentials(string finalEndPointUrl)
      {
         UrlAccessDetails details = GetUrlAccessDetails(finalEndPointUrl);
         return details.GetWebServerCredentials();
      }

      /// <summary>Set the proxy server required to access the URL pointed by finalEndPoint URL. 
      /// This function queries the cache for "ProxyServers" object for the URL. 
      /// Then ProxyServers instance is used to get actual WebProxy that is to be set in the
      ///   HttpWebRequest object which is used to access the URL
      ///   This function works with SaveCredentials to achieve authentication.
      /// </summary>
      /// <param name = "request"></param>
      /// <param name = "finalEndPointUrl">string URL for which proxy needs to be set</param>
      private void SetProxyServer(WebRequest request, string finalEndPointUrl)
      {
         //Query for proxy server that needs to be used to access this URL
         ProxyServers proxyServers = GetProxyServers(finalEndPointUrl);

         if (proxyServers != null)
         {
            //ProxyServers will select a responsive proxy for this access.
            WebProxy proxy = proxyServers.Select();
#if !PocketPC
            if (isUsingDefaultCredentials)
               proxy.UseDefaultCredentials = true;
#endif
            request.Proxy = proxy;
         }
      }

      /// <summary>Splits the domain\user in to domain and user.</summary>
      /// <param name = "pair">String containing domain user pair in form if domain\user</param>
      /// <param name = "username">Out paramter will contain user name</param>
      /// <param name = "domain">out parameter will contain domain</param>
      private static void SplitUserName(String pair, out String username, out String domain)
      {
         String[] domainUsernamePair = pair.Split(new[] { '\\' });

         if (domainUsernamePair.Length == 1)
         {
            domain = null;
            username = domainUsernamePair[0];
         }
         else
         {
            domain = domainUsernamePair[0];
            username = domainUsernamePair[1];
         }
      }

      /// <summary>retrieve the time of the given URL from the web-server.</summary>
      /// <param name = "url"></param>
      /// <returns></returns>
      public String GetRemoteTime(String url)
      {
         String modifiedTime;
         try
         {
            byte[] contentFromServer;
            HttpWebResponse response = ExecuteHttpRequest(url, null, null, RequestMethod.HEAD, false,
                                                          true, out contentFromServer);
            Debug.Assert(contentFromServer == null);
            modifiedTime = DateTimeUtils.ToString(response.LastModified, XMLConstants.CACHED_DATE_TIME_FORMAT);
            response.Close();
         }
         catch (Exception ex)
         {
            Logger.Instance.WriteExceptionToLog(url + " : " + ex.StackTrace);
            throw;
         }
         return modifiedTime;
      }

      /// <summary>Shows Authentication dialog and returns Credentials.</summary>
      /// <param name = "dialog"></param>
      /// <param name = "request"></param>
      /// <param name = "errorResponse"></param>
      /// <returns></returns>
      private static AuthenticationDialogHandler ShowAuthenticationDialog(AuthenticationDialogHandler dialog,
                                                                          WebRequest request,
                                                                          HttpWebResponse errorResponse)
      {
         String caption, authServerUrl;
         String error;

         // Caption of authentication dialog will be decided upon whether it is proxy authentication or
         // Webserver authentication. Server URL on dialog will be Web/Proxy Server URL
         if (errorResponse.StatusCode == HttpStatusCode.ProxyAuthenticationRequired)
         {
            caption = HttpClientConsts.PROXY_AUTH_CAPTION;
            error = HttpClientEvents.GetMessageString(MsgInterface.USRINP_STR_BADPASSW_PROXYSERVER);

            // Server URL = Proxy address.
            authServerUrl = ((WebProxy)request.Proxy).Address.ToString();
         }
         else
         {
            caption = HttpClientConsts.WEB_AUTH_CAPTION;
            error = HttpClientEvents.GetMessageString(MsgInterface.USRINP_STR_BADPASSW_WEBSERVER);

            //Server URL = <protocol://host/alias>
            String[] urlParts = errorResponse.ResponseUri.GetLeftPart(UriPartial.Path).Split(new[] {'/'});
            authServerUrl = urlParts[0] + "//" + urlParts[2] + '/' + urlParts[3];
         }

         // If dialog is not created yet, then create a new
         if (dialog == null)
            dialog = new AuthenticationDialogHandler(null, caption, authServerUrl);
         else
            // If dialog is already created and still we have come here, means the earlier credentials were wrong, so show error
            Commands.messageBox(null, caption, error, Styles.MSGBOX_ICON_ERROR | Styles.MSGBOX_BUTTON_OK);

         //Open dialog and accept credentials.
         dialog.openDialog();
         return dialog;
      }

      /// <summary>
      ///   Gets UrlAccess details from the cache. The cache contains the WebServer credentials and ProxyServers instance
      ///   for a protocol://server. This function takes the URL, extract protocol://server from the URL and checks if record is present for
      ///   this protocol://server, if found it return UrlAccessDetails Object. If not present, it inserts blank object for this
      ///   protocol://server key and return it.
      /// </summary>
      /// <param name = "finalEndPointUrl">The URL for which details are required.</param>
      /// <returns></returns>
      private UrlAccessDetails GetUrlAccessDetails(string finalEndPointUrl)
      {
         //Extract protocol://server key from URL.
         string srvAliasKey = MakeKeyFromUrl(finalEndPointUrl);
         UrlAccessDetails details = null;

         if (srvAliasKey != null)
         {
            // TryGet value will fill out parameter details if key is found, and function will return true
            // otherwise function will return false.
            if (!_urlAccessDetailsTable.TryGetValue(srvAliasKey, out details))
            {
               // If not found in map create new element
               details = new UrlAccessDetails(finalEndPointUrl, _outgoingHeaders);
               _urlAccessDetailsTable[srvAliasKey] = details;
               Logger.Instance.WriteDevToLog("" + srvAliasKey +
                                                         " not found in map, creating new record");
            }
            else
            {
               Logger.Instance.WriteDevToLog("" + srvAliasKey + " found in map");
            }
         }
         return details;
      }

      /// <summary>
      /// Adds one header to the request.
      /// </summary>
      /// <param name="name"></param>
      /// <param name="value"></param>
      public void AddRequestHeader(string name, string value)
      {
         _outgoingHeaders.Add(name.Trim(), value.Trim());
      }

      /// <summary>
      /// Adds headers (0 to N) to the request.
      /// </summary>
      /// <param name="headersAndValuesString">semicolon delimited headers+values, e.g.:  header1: value 1; header2: value2</param>
      public void AddRequestHeaders(string headersAndValuesString)
      {
         String[] headersAndValuesArray = headersAndValuesString.Split(';');
         foreach (var headerAndValueString in headersAndValuesArray)
         {
            String[] headerAndValueArray = headerAndValueString.Split(':');
            if (headerAndValueArray.Length == 2)
               AddRequestHeader(headerAndValueArray[0], headerAndValueArray[1]);
            else
               Logger.Instance.WriteErrorToLog(String.Format("Invalid header {0} should be formatted 'name: value'", headerAndValueString));
         }
      }

      /// <summary>
      /// clear the extra request header
      /// </summary>
      public void ClearRequestHeaders()
      {
         _outgoingHeaders.Clear();
      }

      /// <summary>Return the property which decide whether to set HTTP header "Expect:100Continue"</summary>
      /// <returns>bool</returns>
      private bool GetHTTPExpect100Continue()
      {
         // In case the property is not given in the execution properties, it should default to true
         bool ret = true;
         string val = HttpClientEvents.GetExecutionProperty(HttpClientConsts.HTTP_EXPECT100CONTINUE);
         if (!String.IsNullOrEmpty(val))
            ret = val.Equals("Y", StringComparison.CurrentCultureIgnoreCase);

         return ret;
      }

      /// <summary>Return the property which allows to use TLS v1.2 (implemented at .NET v4.5) as a highest TLS protocol version. 
      /// Otherwise use TLS v1.0 as a highest TLS protocol version</summary>
      /// <returns>bool</returns>
      private bool GetUseHighestSecurityProtocol()
      {
         // In case the property is not given in the execution properties, it should default to false (a highest TLS protocol version is v1.0)
         bool ret = false;
         string val =  HttpClientEvents.GetExecutionProperty(HttpClientConsts.USE_HIGHEST_SECURITY_PROTOCOL);
         if (!String.IsNullOrEmpty(val))
            ret = val.Equals("Y", StringComparison.CurrentCultureIgnoreCase);

         return ret;
      }

      /// <summary>Sets effective the compression level with following algorithm.
      /// 1) If specified in execution properties take from execution properties
      /// 2) If compression level in execution properties is invalid, fallback to NONE
      /// 3) If not specified in execution properties fallback to default 
      /// This function will be called only once i.e. after execution properties are read
      /// after this we should never attempt to set the compression level during the session.</summary>
      /// </summary>
      private static void SetCompressionLevel()
      {
         if (_compressionLevel == null)
         {
            // read the compression level from the execution properties.
            _compressionLevel = HttpClientEvents.GetExecutionProperty(HttpClientConsts.HTTP_COMPRESSION_LEVEL);

            if (_compressionLevel != null)
            {
               String[] ValidCompressionLevels = {
                                                  HttpClientConsts.HTTP_COMPRESSION_LEVEL_NORMAL,
                                                  HttpClientConsts.HTTP_COMPRESSION_LEVEL_MAXIMUM,
                                                  HttpClientConsts.HTTP_COMPRESSION_LEVEL_MINIMUM,
                                                  HttpClientConsts.HTTP_COMPRESSION_LEVEL_NONE
                                                 };

               // verify that the compression level read from the execution properties is valid. If not then set it to none:
               bool compressionLevelIsValid = false;
               foreach (String item in ValidCompressionLevels)
               {
                  if (_compressionLevel.StartsWith(item, StringComparison.CurrentCultureIgnoreCase))
                  {
                     compressionLevelIsValid = true;
                     break;
                  }
               }
               if (!compressionLevelIsValid)
                  _compressionLevel = HttpClientConsts.HTTP_COMPRESSION_LEVEL_NONE;
            }
         }
      }

      /// <summary>
      /// Returns the compression level that is decided after execution properties are read
      /// In case this function is called before execution properties are read For example 
      /// while reading publish html (for execution properties) this function will return NONE
      /// </summary>
      /// <returns></returns>
      public static String GetCompressionLevel()
      {
         return (_compressionLevel != null ? _compressionLevel : HttpClientConsts.HTTP_COMPRESSION_LEVEL_NONE);
      }

      /// <summary>
      /// Write requestContent to request.
      /// </summary>
      /// <param name="requestContent"></param>
      /// <param name="httpRequest"></param>
      private void WriteContentToRequest(object requestContent, HttpWebRequest httpRequest)
      {
         // set the content-length:
         byte[] byteArrayRequestContent = ToByteArray(requestContent);
         httpRequest.ContentLength = byteArrayRequestContent.Length;

         // write the content:
         Stream reqStream = httpRequest.GetRequestStream();
         reqStream.Write(byteArrayRequestContent, 0, byteArrayRequestContent.Length);
         reqStream.Close();
      }

      /// <summary>Reads data from a stream until the end is reached. The data is returned as a byte array. 
      /// An IOException is thrown if any of the underlying IO calls fail.
      /// </summary>
      /// <param name="stream">The stream to read data from</param>
      /// <returns>the content that was read from the stream.</returns>
      private static byte[] ReadFully(Stream stream)
      {
         var buffer = new byte[2048];
         int totalBytesRead = 0;
         int chunk;

         while ((chunk = stream.Read(buffer, totalBytesRead, buffer.Length - totalBytesRead)) > 0)
         {
            totalBytesRead += chunk;

            // If we've reached the end of our buffer, check to see if there's any more data in the stream
            if (totalBytesRead == buffer.Length)
            {
               int nextByte = stream.ReadByte();

               // End of stream? If so, we're done
               if (nextByte == -1)
                  return buffer;
               // Nope. Resize the buffer, put in the byte we've just read, and continue
               Misc.arrayResize(ref buffer, buffer.Length * 2);
               buffer[totalBytesRead] = (byte)nextByte;
               totalBytesRead++; //+nextByte
            }
         }

         // Buffer is now too big. Shrink it.
         Misc.arrayResize(ref buffer, totalBytesRead);
         return buffer;
      }

      #region Facade method for direct HTTP requests (without RIA-specific functionality such as compression, retries, proxies cache, authentication dialogs, etc ...)

      /// <summary>Executes an HTTP verb: GET / POST / HEAD / PUT / DELETE.
      /// The method executes the HTTP request, reads from the response stream and return the content / headers (when applicable to the verb). 
      /// </summary>
      /// <param name="httpVerb">Requested HTTP verb - GET, POST, HEAD, PUT, DELETE.</param>
      /// <param name="requestURL">URL to be accessed.</param>
      /// <param name="requestContent">content to be sent to server (when applicable to the verb). May be byte[] or string.</param>
      /// <param name="httpTimeoutInSeconds">http roundtrip timeout.</param>
      /// <param name="httpProxyAddress">httpproxy address in magic format (i.e. [userName:password@]ProxyMachine:PORT).</param>
      /// <returns>response (from the server).</returns>
      public byte[] ExecuteRequest(String httpVerb, String requestURL, String requestContent, ushort httpTimeoutInSeconds, String httpProxyAddress)
      {
         byte[] contentFromServer = null;
         HttpWebRequest httpRequest = null;
         HttpWebResponse httpResponse = null;

         try
         {
            //Following TLS handshake protocol, we must start negotiation from the highest supported TLS protocol version. If that version isn't supported by other side, 
            // negotiation falls back to lower versions of TLS and then to SSL. 
            //.NET v4.5 platform highest TLS protocol version is TLS1.2
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;//TODO: when the project will be built at .NET v4.5 platform, replace "3072" by SecurityProtocolType.Tls12

            httpRequest = (HttpWebRequest)WebRequest.Create(requestURL);
            RequestMethod httpMethod = GetRequestMethodFromString(httpVerb);
            httpRequest.Method = httpMethod.ToString();
            httpRequest.Timeout = (httpTimeoutInSeconds == 0 ? -1 : (int)httpTimeoutInSeconds * 1000); //timeout that encompasses the entire request/response round-trip. Set to unlimited to accommodate time-consuming requests.
            httpRequest.Proxy = CreateWebProxy(httpProxyAddress);

            AddHeadersToHttpRequest(httpRequest);

            // add the content-length header:
            if (httpMethod == RequestMethod.POST ||
                httpMethod == RequestMethod.DELETE || //TODO: Delete may have body but some servers ignores. See http://stackoverflow.com/questions/299628/is-an-entity-body-allowed-for-an-http-delete-request
                httpMethod == RequestMethod.PUT)
               WriteContentToRequest(requestContent, httpRequest);
            else if (httpMethod == RequestMethod.GET)
               httpRequest.ContentLength = 0;

            // execute the http request and (optionally) get the response.
            httpResponse = (HttpWebResponse)httpRequest.GetResponse();
            if (httpResponse != null)
            {
               _lastResponseHeaders = httpResponse.Headers;
               if (httpMethod != RequestMethod.HEAD)
                  contentFromServer = ReadFully(new BufferedStream(httpResponse.GetResponseStream()));
            }
         }
         catch (Exception ex)
         {
            
         }
         finally
         {
            if (httpResponse != null)
               httpResponse.Close();
         }

         return (contentFromServer);
      }

      /// <summary>
      /// Add headers from _outgoingHeaders to HttpRequest
      /// Few headers (content-type & accept) require special handling.
      /// </summary>
      /// <param name="httpRequest"></param>
      /// <returns></returns>
      private void AddHeadersToHttpRequest(HttpWebRequest httpRequest)
      {
         for (ushort i = 0; i < _outgoingHeaders.Count; i++) // Add Headers to HttpWebRequest.
         {
            String headerKey = _outgoingHeaders.GetKey(i);

            if (WebHeaderCollection.IsRestricted(headerKey))
            {
               try
               {
                  if (headerKey.Equals("Accept", StringComparison.CurrentCultureIgnoreCase))
                     httpRequest.Accept = _outgoingHeaders[i];
                  else if (headerKey.Equals("Connection", StringComparison.CurrentCultureIgnoreCase))
                     httpRequest.Connection = _outgoingHeaders[i];
                  else if (headerKey.Equals("Content-Type", StringComparison.CurrentCultureIgnoreCase))
                     httpRequest.ContentType = _outgoingHeaders[i];
                  else if (headerKey.Equals("Content-Length", StringComparison.CurrentCultureIgnoreCase))
                     continue; // setting httpRequest.ContentLength is the sole responsibility of the method 'WriteContentToRequest'.
                  else if (headerKey.Equals("Date", StringComparison.CurrentCultureIgnoreCase))
                     httpRequest.Date = Convert.ToDateTime(_outgoingHeaders[i]);
                  else if (headerKey.Equals("Expect", StringComparison.CurrentCultureIgnoreCase))
                     httpRequest.Expect = _outgoingHeaders[i];
                  else if (headerKey.Equals("Host", StringComparison.CurrentCultureIgnoreCase))
                     httpRequest.Host = _outgoingHeaders[i];
                  else if (headerKey.Equals("If-Modified-Since", StringComparison.CurrentCultureIgnoreCase))
                     httpRequest.IfModifiedSince = Convert.ToDateTime(_outgoingHeaders[i]);
                  else if (headerKey.Equals("Referer", StringComparison.CurrentCultureIgnoreCase))
                     httpRequest.Referer = _outgoingHeaders[i];
                  else if (headerKey.Equals("Transfer-Encoding", StringComparison.CurrentCultureIgnoreCase))
                     httpRequest.TransferEncoding = _outgoingHeaders[i];
                  else if (headerKey.Equals("User-Agent", StringComparison.CurrentCultureIgnoreCase))
                     httpRequest.UserAgent = _outgoingHeaders[i];
                  else if (headerKey.Equals("Range", StringComparison.CurrentCultureIgnoreCase))
                  {
                     // Parse ranges and add it to HttpRequest
                     // valid examples-  Range: bytes =0 - 200,  Range:bytes=50-,  Range:bytes=-490,  Range:bytes=500
                     String headerValue = _outgoingHeaders[i];
                     String[] values = headerValue.Split('=');
                     if (values.Length == 2 && values[0].Trim().Equals("bytes", StringComparison.CurrentCultureIgnoreCase))
                     {
                        if (values[1].StartsWith("-") || values[1].EndsWith("-"))  // bytes=-490, bytes=50-
                        {
                           int intRange;
                           int.TryParse(values[1], out intRange);
                           httpRequest.AddRange(intRange);
                        }
                        else
                        {
                           bool hasHigherRange = false;
                           bool hasLowerRange = false;
                           int lowerRange = 0;
                           int higherRange = 0;

                           String[] rangeValues = values[1].Split('-');
                           if (rangeValues.Length == 2)
                              hasHigherRange = int.TryParse(rangeValues[1], out higherRange);

                           hasLowerRange = int.TryParse(rangeValues[0], out lowerRange);
                           if (hasHigherRange && hasLowerRange)    // bytes=1-2000
                              httpRequest.AddRange(lowerRange, higherRange);
                           else if (hasLowerRange)                 // bytes=50
                              httpRequest.AddRange(lowerRange);
                        }
                     }
                  }
               }
               catch(Exception)
               {                                    
               }
            }
            else
               httpRequest.Headers.Add(headerKey, _outgoingHeaders[i]);
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="methodName"></param>
      /// <returns></returns>
      private RequestMethod GetRequestMethodFromString(string methodName)
      {
         RequestMethod method = RequestMethod.UNDEFINED;

         if (methodName.Equals("GET", StringComparison.CurrentCultureIgnoreCase))
            method = RequestMethod.GET;
         else if (methodName.Equals("POST", StringComparison.CurrentCultureIgnoreCase))
            method = RequestMethod.POST;
         else if (methodName.Equals("HEAD", StringComparison.CurrentCultureIgnoreCase))
            method = RequestMethod.HEAD;
         else if (methodName.Equals("PUT", StringComparison.CurrentCultureIgnoreCase))
            method = RequestMethod.PUT;
         else if (methodName.Equals("DELETE", StringComparison.CurrentCultureIgnoreCase))
            method = RequestMethod.DELETE;

         return method;
      }

      /// <summary>
      /// Parse httpProxyAddress string and create WebProxy.
      /// </summary>
      /// <param name="httpProxyAddress">in magic format : i.e. "username:password@proxy:port" or "proxy:port"</param>
      /// <returns>created WebProxy object</returns>
      private WebProxy CreateWebProxy(String httpProxyAddress)
      {
         WebProxy webProxy = null;
         NetworkCredential networkCredentials = null;

         if (httpProxyAddress != null && httpProxyAddress.Length > 0)
         {
            String httpProxyUri;
            String[] httpProxiesValues = httpProxyAddress.Split('@');
            if (httpProxiesValues.Length == 2)
            {
               String[] userNamePassword = httpProxiesValues[0].Split(':');
               networkCredentials = new NetworkCredential(userNamePassword[0], userNamePassword[1]);
               httpProxyUri = httpProxiesValues[1];
            }
            else
               httpProxyUri = httpProxiesValues[0];

            webProxy = new WebProxy("http://" + httpProxyUri);
            webProxy.Credentials = (networkCredentials != null) ? networkCredentials : null;
         }

         return webProxy;
      }

      #endregion

      #region Nested type: Cookie

      internal struct Cookie
      {
         private readonly string _sName;

         private readonly string _sValue;

         internal Cookie(string sName, string sValue)
         {
            _sName = sName;
            _sValue = sValue;
         }

         internal string Name
         {
            get { return _sName; }
         }

         internal string Value
         {
            get { return _sValue; }
         }
      }

      #endregion

      #region Nested type: UrlAccessDetails

      /// <summary>
      ///   This is element in the cache UrlAccessDetailsTable. It holds a ProxyServer Object and WebServer credentials.
      ///   Every new protocol://server combination will be associated with an object of UrlAccessDetails.
      /// </summary>
      internal class UrlAccessDetails
      {
         private readonly ProxyServers _proxyServers;
         private NetworkCredential _webServerCredentials;

         internal UrlAccessDetails(string url, NameValueCollection outgoingHeaders)
         {
            //Create a new ProxyServers object for every new protocol://server key.
            _proxyServers = new ProxyServers();
            // Initialize the proxy servers. This will ensure that ProxyServers is ready to select
            // proxy when request is to be executed.
            _proxyServers.Initialize(url, outgoingHeaders);
         }

         /// <summary>
         /// </summary>
         /// <param name="webServerCredentials"></param>
         internal void SetWebServerCredentials(NetworkCredential webServerCredentials)
         {
            _webServerCredentials = webServerCredentials;
         }

         /// <summary>
         /// </summary>
         /// <returns></returns>
         internal ProxyServers GetProxyServers()
         {
            return _proxyServers;
         }

         /// <summary>
         /// </summary>
         /// <returns></returns>
         internal NetworkCredential GetWebServerCredentials()
         {
            return _webServerCredentials;
         }
      }

      #endregion
   }
}
