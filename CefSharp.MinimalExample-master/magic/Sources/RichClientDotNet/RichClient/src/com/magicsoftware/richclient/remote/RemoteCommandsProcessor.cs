using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using com.magicsoftware.richclient.cache;
using com.magicsoftware.richclient.communications;
using com.magicsoftware.richclient.rt;
using com.magicsoftware.richclient.http; 
using com.magicsoftware.httpclient;
using com.magicsoftware.richclient.local;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.richclient.util;
using com.magicsoftware.unipaas;
using com.magicsoftware.unipaas.gui.low;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.unipaas.util;
using com.magicsoftware.util;
using Console = System.Console;
using com.magicsoftware.richclient.events;
using util.com.magicsoftware.util;
using com.magicsoftware.util.Xml;
using com.magicsoftware.richclient.commands;
using com.magicsoftware.richclient.commands.ClientToServer;
using com.magicsoftware.richclient.local.application.datasources.converter;
using com.magicsoftware.richclient.sources;

#if PocketPC
using OSEnvironment = com.magicsoftware.richclient.mobile.util.OSEnvironment;
#endif

namespace com.magicsoftware.richclient.remote
{
   /// <summary> this class is responsible for the communication with the server and handling the server messages</summary>
   internal class RemoteCommandsProcessor : CommandsProcessorBase
   {
      internal const int RC_NO_CONTEXT_ID = -1;

      private static RemoteCommandsProcessor _instance; //singleton

      /// <summary> Represents the status of a request </summary>
      private enum RequestStatus
      {
         Handled, //the request was successfully handled.
         Retry,   //the request should be re-sent.
         Abort    //the request could not be handled and should be aborted.
      }

      /// <summary> Server Communication constants</summary>
      private const String EXCEPTION = "EXCEPTION";

      //*****************************************************************************************************
      //*****************************************************************************************************
      //******** When changing RIA_COMMUNICATION_PROTOCOL_VERSION, please change on mobile platforms as well:
      //******** Android: RemoteCommandsProcessor.java
      //******** iOS: RemoteCommandsProcessor.h
      //******** W10M: RemoteCommandsProcessor.cs
      private const String RIA_COMMUNICATION_PROTOCOL_VERSION = "14000";
      //*****************************************************************************************************
      //*****************************************************************************************************

      private MgStatusBar _statusBar; // the first task that is not a main program

      private ClientLogAccumulator _logAccumulator;

      private long _lastRequestTime; // the time of the last request to the server

      // the last modification time of the startup program's (the program activated by the client, unless the MDI container is opened instead). 
      // is sent from the server ONLY when the xpa server is under the studio (AKA agent), otherwise the member remains null.
      private String _startupProgramModificationTime;

      internal String ServerUrl { get; set; }

      internal ServerAccessStatus ServerLastAccessStatus { get; set; }   //Gets or Sets the status of the last server access

      // After the expression is evaluated, we obtain local file path. But to fetch this resource from server, 
      // cache path is needed. If cache path isn't found in cachedFilesMap, then query server for cachedFile. 
      // Later again if the expression is evaluated to the same path, no need to query server for cache path.
      // e.g. for server-file path = "C:\Blue_hills.jpg" 
      // cache path = "/MagicScripts/mgrqispi.dll?CTX=10430335172368&CACHE=agent_3572_C__Blue_hills.jpg|06/10/2009%2016:36:16"
      internal Dictionary<string, string> ServerFilesToClientFiles { get; private set; } // map between server-file path and cachedFileUrl

      // This is a part of 'retrieving multiple files from server' mechanism. It contains filenames sent by server on querying folder/wildcards,
      // which is further used by client to get the contents of multiple files.
      internal ServerFileToClientExecutionHelper ServerFileToClientHelper { get; set; }

      /// <summary> create/return the single instance of the class.</summary>
      internal static RemoteCommandsProcessor GetInstance()
      {
         if (_instance == null)
         {
            lock (typeof(RemoteCommandsProcessor))
            {
               if (_instance == null)
                  _instance = new RemoteCommandsProcessor();
            }
         }
         return _instance;
      }

      /// <summary>CTOR</summary>
      private RemoteCommandsProcessor()
      {
         // get the Url of the Server; the returned url includes 'prot://host/alias/requester' (up to the '?'
         ServerUrl = null;
         Uri url = ClientManager.Instance.getServerURL();
         int iPort = url.Port;
         String httpReq = ClientManager.Instance.getHttpReq();
         String sPort = (iPort == -1)
                           ? ""
                           : ":" + iPort; // inline port

         if (url.Host.Length > 0)
         {
            if (httpReq != null && httpReq.StartsWith("http", StringComparison.CurrentCultureIgnoreCase))
               ServerUrl = httpReq;
            else
            {
               ServerUrl = url.Scheme + "://" + url.Host + sPort;
               if (!ServerUrl.EndsWith("/") && httpReq != null && !httpReq.StartsWith("/"))
                  ServerUrl += "/";
               ServerUrl += httpReq;
            }
         }

         ServerFilesToClientFiles = new Dictionary<string, string>();

         HttpManager.GetInstance(); //TODO: Initialize the http manager (Check whether still required)

         RegisterDelegates();
      }

      /// <summary>
      /// Checks that session counter is consecutive during the session and sets new value.
      /// The only exception is in case the client sent to the server ConstInterface.SESSION_COUNTER_CLOSE_CTX_INDICATION,
      ///    the server is expected to return ConstInterface.SESSION_COUNTER_CLOSE_CTX_INDICATION in response (RequestsServerBase::ProcessRequest), to acknowledge the request to close the context.
      /// </summary>
      /// <param name="newSessionCounter">!!.</param>
      internal void CheckAndSetSessionCounter(long newSessionCounter)
      {
         if (newSessionCounter == ConstInterface.SESSION_COUNTER_CLOSE_CTX_INDICATION)
         {
            /// In case the client sent to the server ConstInterface.SESSION_COUNTER_CLOSE_CTX_INDICATION,
            ///    the server is expected to return ConstInterface.SESSION_COUNTER_CLOSE_CTX_INDICATION in response (RequestsServerBase::ProcessRequest), to acknowledge the request to close the context.
            Debug.Assert(GetSessionCounter() == ConstInterface.SESSION_COUNTER_CLOSE_CTX_INDICATION);
         }
         else
         {
            Debug.Assert(newSessionCounter == GetSessionCounter() + 1);
            SetSessionCounter(newSessionCounter);
            Logger.Instance.WriteServerToLog(String.Format("Session Counter --> {0}", _sessionCounter));
         }
      }

      /// <summary>
      /// </summary>
      /// <param name="newSessionCounter"></param>
      private void SetSessionCounter(long newSessionCounter)
      {
         _sessionCounter = newSessionCounter;
      }

      /// <summary>
      /// Zero session counter when reinitializing server communication details 
      /// </summary>
      /// <returns></returns>
      internal void ClearSessionCounter()
      {
         _sessionCounter = 0;
         Logger.Instance.WriteServerToLog(String.Format("Session Counter --> {0}", _sessionCounter));
      }

      /// <summary>
      ///   Get the Url to cached file on server
      /// </summary>
      /// <param name = "path">path of file on server</param>
      /// <param name = "currTask">current task</param>
      /// <param name = "refreshClientCopy">Even if client copy is present, query server for URL of cached file to get the 
      ///   modified remotetime as part of URL. After accessing URL having modified remotetime, the client copy will get refreshed 
      ///   with that on server.</param>
      /// <returns>If server-file path is valid then Url to cached file is returned else blank value</returns>
      /// e.g for server-file path = "C:\Blue_hills.jpg" 
      /// cache-url returned = "/MagicScripts/mgrqispi.dll?CTX=10430335172368&CACHE=agent_3572_C__Blue_hills.jpg|06/10/2009%2016:36:16"
      internal String ServerFileToUrl(String path, Task currTask, Boolean refreshClientCopy)
      {
         path = ClientManager.Instance.getEnvParamsTable().translate(path);
         String serverCachedFileUrl = path;

 
         if (!String.IsNullOrEmpty(path) && !Misc.isWebURL(path, ClientManager.Instance.getEnvironment().ForwardSlashUsage) &&
             !HttpManager.IsRelativeRequestURL(path))

         {
            // If already cached client copy is to be updated with the modified copy on server, then remove its entry from 
            // cachedFilesMap to allow querying server again for URL of cachedfile.
            if (refreshClientCopy && ServerFilesToClientFiles.ContainsKey(path))
               ServerFilesToClientFiles.Remove(path);

            if (!ServerFilesToClientFiles.ContainsKey(path))
            {
               // query server for Url of cached file
               IClientCommand cmd = CommandFactory.CreateQueryCachedFileCommand(path);
               currTask.getMGData().CmdsToServer.Add(cmd);

               // execute client to server commands
               Execute(SendingInstruction.ONLY_COMMANDS);
            }

            // get the Url of cached file
            serverCachedFileUrl = (ServerFilesToClientFiles.ContainsKey(path)
                                     ? ServerFilesToClientFiles[path]
                                     : "");
         }

         return serverCachedFileUrl;
      }

      /// <summary>copy to the local (Client-side) file system a given file name in the Server-side and return its name.</summary>
      /// <param name="serverFilename">a file name in the Server's file system.</param>
      /// <param name="task">task</param>
      /// <param name="refreshClientCopy">Even if client copy is present, query server for URL of cached file to get the 
      ///   modified remotetime as part of URL. After accessing URL having modified remotetime, the client copy will get refreshed 
      ///   with that on server.</param>
      /// <returns>file name in the local file system.</returns>
      internal override String GetLocalFileName(String serverFilename, Task task, bool refreshClientCopy)
      {
         String localFileName = "";

         // create a request-url for retrieving the file through the cache
         String serverCachedFileUrl = ServerFileToUrl(serverFilename, task, refreshClientCopy);
         if (!String.IsNullOrEmpty(serverCachedFileUrl))
         {
            try
            {
               // execute request to get the file from the server to local cache without loading it into the memory
               DownloadContent(serverCachedFileUrl);

               // get the file name in the local file system
               localFileName = CacheUtils.URLToLocalFileName(serverCachedFileUrl);
            }
            catch (Exception ex)
            {
               Logger.Instance.WriteExceptionToLog(ex);
            }
         }

         return localFileName;
      }

      /// <summary></summary>
      /// <returns>true if web requests from the client are made using the HTTPS protocol.</returns>
      private bool IsAccessingServerUsingHTTPS()
      {
         return (ServerUrl.StartsWith("https", StringComparison.CurrentCultureIgnoreCase));
      }

      /// <summary>Prepare and execute the 1st & 2nd handshake requests: 
      /// 1st request:  UTF8TRANS=..&appname=..&prgname=..&arguments=-A<Richclient><Requires EncryptionKey=".."/><RequestingVersionDetails=".."/></Richclient>
      ///     response: instructions to the client - whether to display authentication dialog, the server's version, and more ..
      ///               e.g. <Richclientresponse><ContextID>120308789726371200</ContextID><Environment InputPassword="N" SystemLogin="N" ScrambleMessages="N" MaxInternalLogLevel="Server#" HttpTimeout="10" ClientNetworkRecoveryInterval="0" ForwardSlash="web"/></Richclientresponse>
      /// 2nd request: authentication (optional, depending on the 1st request).
      /// </summary>
      /// <returns>true only if the client should continue to start the actual program (i.e. after handshake).</returns>
      internal override bool StartSession()
      {
         bool authenticationCancelled = false;
         HandshakeResponse firstHandshakeResponse = null;
         AuthenticationDialogHandler dialog = null;
         RequestStatus requestStatus;

         if (Logger.Instance.LogLevel == Logger.LogLevels.Basic)
            HttpManager.GetInstance().AddOutgoingHeader("MgxpaRIABuildNumber", ClientManager.Instance.ClientVersion);

         try
         {
            String lastTypedUserId = null;

            //----------------------------------------------------------------------
            // 1st handshake request: prepare 1st handshake request and scramble it 
            //----------------------------------------------------------------------
            bool isAccessingServerUsingHTTPS = IsAccessingServerUsingHTTPS();

            var handshakeInitialTokens = new StringBuilder();
            handshakeInitialTokens.Append(ConstInterface.UTF8TRANS + 
                                          ConstInterface.REQ_APP_NAME + "=" +
                                          HttpUtility.UrlEncode(ClientManager.Instance.getAppName(), Encoding.UTF8) + "&" +
                                          ConstInterface.REQ_PRG_NAME + "=" +
                                          HttpUtility.UrlEncode(ClientManager.Instance.getPrgName(), Encoding.UTF8));
            if (ClientManager.Instance.getLocalID() != null)
            {
               handshakeInitialTokens.Append("|" + ClientManager.Instance.getLocalID());
               if (ClientManager.Instance.getDebugClient() != null)
                  handshakeInitialTokens.Append("&" + ClientManager.Instance.getDebugClient());
            }
            handshakeInitialTokens.Append("&" + ConstInterface.REQ_ARGS + "=" + ConstInterface.REQ_ARG_ALPHA +
                                          "<Richclient><Requires EncryptionKey=\"" + isAccessingServerUsingHTTPS + "\"/>" +
                                          "<RIAProtocolVersion=\"" + RIA_COMMUNICATION_PROTOCOL_VERSION + "\"/></Richclient>");

            Logger.Instance.WriteDevToLog(String.Format("Handshake request #1 (not scrambled) : {0}",
                                                        ServerUrl + ConstInterface.REQ_ARG_START +
                                                        ConstInterface.RC_INDICATION_INITIAL + handshakeInitialTokens));

            String handshakeInitialUrl = ServerUrl + ConstInterface.REQ_ARG_START +
                                         ConstInterface.RC_INDICATION_INITIAL +
                                         ConstInterface.RC_TOKEN_DATA + 
                                         HttpUtility.UrlEncode(Scrambler.Scramble(handshakeInitialTokens.ToString()), Encoding.UTF8);

            Logger.Instance.WriteServerMessagesToLog("");
            Logger.Instance.WriteServerMessagesToLog(String.Format("Handshake request #1: {0}", handshakeInitialUrl));

            // Initialize temp ctxGroup with CtxGroup from execution.properties
            String ctxGroup = ClientManager.Instance.getCtxGroup();

            while (!authenticationCancelled)
            {
               // execute the 1st request
               String responseStr = DispatchRequest(handshakeInitialUrl, null, SessionStage.HANDSHAKE, out requestStatus);

               // process the 1st response - check if the response is error, unscramble and parse it
               if (string.IsNullOrEmpty(responseStr))
               {
                  throw new ServerError("Client failed to initialize a session." + OSEnvironment.EolSeq +
                                        "Empty response was received from the web server." +
                                        OSEnvironment.EolSeq + OSEnvironment.EolSeq + ServerUrl);
               }

               Logger.Instance.WriteServerMessagesToLog(String.Format("Handshake response #1: {0}", responseStr));
               Logger.Instance.WriteServerMessagesToLog("");
               firstHandshakeResponse = new HandshakeResponse(responseStr);

               ClientManager.Instance.RuntimeCtx.ContextID = Int64.Parse(firstHandshakeResponse.ContextId);

               _startupProgramModificationTime = firstHandshakeResponse.StartupProgramModificationTime;
               if (LocalCommandsProcessor.GetInstance().CanStartWithoutNetwork)
                  // Verify that the last initial response file saved on the client for offline execution is valid:
                  VerifyLastOfflineInitialResponseFile();

               HttpManager.GetInstance().HttpCommunicationTimeoutMS = firstHandshakeResponse.HttpTimeout * 1000;

               // During authentication, if authentication failed then server unloads ctx and creates new ctx for next authentication request. 
               // So every time after authentication fails we receive new ctxid for next authentication request. 
               // Hence take ctxgroup in local variable and update ClientManager only after authentication is done.
               if (String.IsNullOrEmpty(ClientManager.Instance.getCtxGroup()))
                  ctxGroup = firstHandshakeResponse.ContextId;

               // Abort client if any failure occurred during updating the local data in previous executions
               if (ApplicationSourcesManager.GetInstance().SourcesSyncStatus.TablesIncompatibleWithDataSources == true)
                  HandleTablesIncompatibility(firstHandshakeResponse.ContextId, ctxGroup);

               //-----------------------
               // 2nd handshake request 
               //-----------------------
               UsernamePasswordCredentials credentials = null;
               if (ClientManager.Instance.getSkipAuthenticationDialog())
               {
                  // skip authentication (to the runtime-engine) dialog
                  if (ClientManager.Instance.getUsername() != null)
                  {
                     // user the userId & password that were passed, and reset them (they'll be set below, if authenticated by the runtime-engine) 
                     credentials = new UsernamePasswordCredentials(ClientManager.Instance.getUsername(),
                                                                   ClientManager.Instance.getPassword());
                     ClientManager.Instance.setUsername("");
                     ClientManager.Instance.setPassword("");
                  }
               }
               else if (firstHandshakeResponse.InputPassword &&
                        firstHandshakeResponse.SystemLogin != HandshakeResponse.SYSTEM_LOGIN_AD)
               {
                  // authentication (to the runtime-engine) dialog
                  if (lastTypedUserId != null)
                  {
                     String title = ClientManager.Instance.getMessageString(MsgInterface.BRKTAB_STR_ERROR);
                     String error = ClientManager.Instance.getMessageString(MsgInterface.USRINP_STR_BADPASSW);
                     Commands.messageBox(null, title, error, Styles.MSGBOX_ICON_ERROR | Styles.MSGBOX_BUTTON_OK);
                  }

                  String userId = lastTypedUserId;

                  if (dialog == null)
                  {
                     dialog = new AuthenticationDialogHandler(userId, GetLogonDialogExecProps());
                  }
                  dialog.openDialog();

                  credentials = dialog.getCredentials();
                  if (credentials != null)
                     lastTypedUserId = credentials.Username;
                  else
                     authenticationCancelled = true;
               }

               // ConstInterface.SESSION_COUNTER_CLOSE_CTX_INDICATION instructs the server to close the context.
               // (note: unloading a context post handshake should be done using Command.createUnloadCommand).
               if (authenticationCancelled)
                  SetSessionCounter(ConstInterface.SESSION_COUNTER_CLOSE_CTX_INDICATION);

               String handshakeAuthUrl = PrepareAuthenticationUrl(firstHandshakeResponse.ContextId, ctxGroup, GetSessionCounter());

               try
               {
                  if (credentials != null)
                  {
                     String credentialsStr = credentials.Username + ":";
                     if (!string.IsNullOrEmpty(credentials.Password))
                        credentialsStr += (credentials.Password + ":");
                     credentialsStr += firstHandshakeResponse.ContextId;
                     credentialsStr = Scrambler.Scramble(credentialsStr);
                     credentialsStr = Base64.encode(credentialsStr, true, Encoding.UTF8);
                     handshakeAuthUrl += ("&USERNAME=" + HttpUtility.UrlEncode(credentialsStr, Encoding.UTF8));

                     // send 2nd handshake request - with authentication
                     Logger.Instance.WriteServerMessagesToLog(String.Format("Handshake request #2: {0}", handshakeAuthUrl));
                     responseStr = DispatchRequest(handshakeAuthUrl, null, SessionStage.HANDSHAKE, out requestStatus);

                     // verify that the response wasn't replayed
                     if (responseStr.IndexOf(firstHandshakeResponse.ContextId) == -1)
                        throw new ServerError(ClientManager.Instance.getMessageString(MsgInterface.USRINP_STR_BADPASSW));

                     ClientManager.Instance.setUsername(credentials.Username);
                     ClientManager.Instance.setPassword(credentials.Password);
                  }
                  else
                  {
                     // send 2nd handshake request - without authentication               
                     Logger.Instance.WriteServerMessagesToLog(String.Format("Handshake request #2: {0}", handshakeAuthUrl));
                     responseStr = DispatchRequest(handshakeAuthUrl, null, SessionStage.HANDSHAKE, out requestStatus);
                  }
                  Logger.Instance.WriteServerMessagesToLog(String.Format("Handshake response #2: {0}", responseStr));
                  Logger.Instance.WriteServerMessagesToLog("");
                  break;
               }
               catch (ServerError e)
               {
                  if (ClientManager.Instance.getSkipAuthenticationDialog())
                  {
                     switch (e.GetCode())
                     {
                        case ServerError.ERR_ACCESS_DENIED:
                        // External Authenticator authenticated the user, access to the application wasn't authorized.
                        case ServerError.ERR_AUTHENTICATION:
                           // External Authenticator authenticated the user but application authentication failed
                           ClientManager.Instance.setSkipAuthenticationDialog(false);
                           throw;
                     }
                  }

                  // the session will never provide a way to change the credentials, therefore abort
                  if (!firstHandshakeResponse.InputPassword)
                     throw;

                  // Error.INF_NO_RESULT:
                  //    if authentication dialog was canceled, the request was sent only to close the context,
                  //    and the server isn't expected to return a response
                  if (e.GetCode() != ServerError.ERR_AUTHENTICATION &&
                      (!authenticationCancelled || e.GetCode() != ServerError.INF_NO_RESULT))
                     throw;
               }
            }

            // store an indication to skip future authentication (session restart, parallel program)
            ClientManager.Instance.setSkipAuthenticationDialog(true);

            // save context group
            ClientManager.Instance.setCtxGroup(ctxGroup);

            if (dialog != null)
               dialog.closeDialog();

            //------------------------
            // set session properties  
            //------------------------
            ClientManager.Instance.ShouldScrambleAndUnscrambleMessages = firstHandshakeResponse.ScrambleMessages;

            if (Logger.Instance.LogLevel != Logger.LogLevels.Basic)
            {
               // maximal value of the InternalLogLevel that can be set by the client 
               // if set, it controls the maximal value; otherwise, any log level is allowed.
               string maxInternalLogLevel = firstHandshakeResponse.MaxInternalLogLevel;
               if (maxInternalLogLevel != null)
               {
                  Logger.LogLevels maxLogLevel = ClientManager.Instance.parseLogLevel(maxInternalLogLevel);
                  if (maxLogLevel < Logger.Instance.LogLevel)
                  {
                     // textual value (as loaded from the execution properties)
                     ClientManager.Instance.setInternalLogLevel(maxInternalLogLevel);

                     // set the actual log level to the level allowed by the server
                     Logger.Instance.WriteToLog(
                        String.Format("Internal log level was restricted to '{0}' by the Magic xpa server.",
                                      maxInternalLogLevel), false);
                     Logger.Instance.LogLevel = maxLogLevel;
                  }
               }
            }


            // initialize the cache manager
            IEncryptor encryptor = PersistentOnlyCacheManager.CreateInstance(!firstHandshakeResponse.EncryptCache,
                                                                             firstHandshakeResponse.EncryptionKey);

            //----------------------------------------------------------------------
            // 3rd request: Executes the initial program in the server.
            //----------------------------------------------------------------------
            try
            {
               if (!authenticationCancelled)
                  ExecuteInitialRequest();
            }
            catch (DataSourceConversionFailedException e)
            {
               if (HandleDataSourceConversionFailedException(e) == false)
               {
                  // create new exception with error message to be displayed to end user
                  throw new DataSourceConversionFailedException(e.DataSourceName, e.GetUserError());
               }
            }
         }
         catch (Exception e)
         {
            if (e is ServerError || e is InvalidSourcesException || e is DataSourceConversionFailedException)
               throw;

            throw new ServerError(e.Message, e.InnerException);
         }

         return !authenticationCancelled;
      }

      /// <summary>
      /// Handles DataSourceConversionFailedException for offline as well as non-offline applications. For offline applications, 
      /// the client will start in disconnected mode. For non-offline applications, the exception is handled only partly and it 
      /// is callers responsibility to handle it further.
      /// </summary>
      /// <param name="ex"></param>
      /// <returns>True if exception is handled</returns>
      private bool HandleDataSourceConversionFailedException(DataSourceConversionFailedException ex)
      {
         bool handled = false;

         Logger.Instance.WriteExceptionToLog(ex.Message);

         // Data sources conversion is failed. Rollback all modified tables and modified sources, so that tables and sources remain synchronized on client.
         // This way offline application can still continue in disconnected mode.
         ClientManager.Instance.LocalManager.DataSourceConverter.RollBack();
         ApplicationSourcesManager.GetInstance().RollBack();

         // Data source converter is applicable only in Remote session, where new source are get from server. Reset it before switching to offline mode.
         ClientManager.Instance.LocalManager.DataSourceConverter = null;

         if (ClientManager.Instance.HostsOfflineApplication)
         {
            String title = ClientManager.Instance.getMessageString(MsgInterface.BRKTAB_STR_ERROR);
            Commands.messageBox(null, title, ex.GetUserError(), Styles.MSGBOX_ICON_ERROR | Styles.MSGBOX_BUTTON_OK);

            // Before switching to offline mode, reset all definitions in local application. The definitions will be re-build from local sources.
            ClientManager.Instance.LocalManager.ApplicationDefinitions.Init();

            // Since sources are roll backed, report unsynchronized metadata. Disconnect server connection and start application in offline mode.
            HandleServerErrorForOfflineApplication(new ServerError(ex.Message, ServerError.ERR_UNSYNCHRONIZED_METADATA));

            handled = true;
         }

         return handled;
      }

      /// <summary>
      /// Handles incompatibility of local table structures with data sources. Sends a requests to the server 
      /// to terminate the session and then throws InvalidSourcesException
      /// </summary>
      /// <param name="contextId"></param>
      /// <param name="ctxGroup"></param>
      private void HandleTablesIncompatibility(String contextId, String ctxGroup)
      {
         // Structures for one or more tables in local database doesn't match with their sources. So cannot continue.
         // Ask server to terminate context

         string errorFormat = "Local database table structures incompatible with sources. Close context {0}";
         SetSessionCounter(ConstInterface.SESSION_COUNTER_CLOSE_CTX_INDICATION);
         String urlForClosingContext = PrepareUrlForClosingContext(contextId, ctxGroup, GetSessionCounter());
         Logger.Instance.WriteServerMessagesToLog(String.Format(errorFormat, "request : " + urlForClosingContext));

         try
         {
            RequestStatus requestStatus;
            String responseStr = DispatchRequest(urlForClosingContext, null, SessionStage.HANDSHAKE, out requestStatus);
            Logger.Instance.WriteServerMessagesToLog(String.Format(errorFormat, "response : " + responseStr));
         }
         catch (ServerError)
         {
            // Don't handle server error if tables structures are incompatible with sources. Because in this case client should be terminated 
            // without switching to disconnected mode. The method should always throw InvalidSourcesException.
         }

         // Reset context id on client because server has deleted the context.
         ClientManager.Instance.RuntimeCtx.ContextID = RC_NO_CONTEXT_ID;

         String errorMessage = ClientManager.Instance.getMessageString(MsgInterface.RC_ERROR_INCOMPATIBLE_DATASOURCES);
         throw new InvalidSourcesException(errorMessage, null);
      }

      /// <summary>
      /// prepares request url for closing context
      /// </summary>
      /// <param name="contextId"></param>
      /// <param name="ctxGroup"></param>
      /// <param name="sessionCount"></param>
      /// <returns></returns>
      private String PrepareUrlForClosingContext(String contextId, String ctxGroup, long sessionCount)
      {
         String url = ServerUrl + ConstInterface.REQ_ARG_START +
                                  ConstInterface.RC_INDICATION + ConstInterface.UTF8TRANS +
                                  ConstInterface.RC_TOKEN_CTX_ID + contextId +
                                  ConstInterface.REQ_ARG_SEPARATOR +
                                  ConstInterface.RC_TOKEN_SESSION_COUNT + sessionCount +
                                  ConstInterface.REQ_ARG_SEPARATOR +
                                  ConstInterface.RC_TOKEN_CTX_GROUP + ctxGroup;
         return url;
      }


      /// <summary>
      /// prepares handshake request url for authentication
      /// </summary>
      /// <param name="contextId"></param>
      /// <param name="ctxGroup"></param>
      /// <param name="sessionCount"></param>
      /// <returns></returns>
      private String PrepareAuthenticationUrl(String contextId, String ctxGroup, long sessionCount)
      {
         String handshakeAuthUrl = ServerUrl + ConstInterface.REQ_ARG_START +
                                               ConstInterface.RC_INDICATION + ConstInterface.UTF8TRANS + 
                                               ConstInterface.RC_TOKEN_CTX_ID + contextId +
                                               ConstInterface.REQ_ARG_SEPARATOR +
                                               ConstInterface.RC_TOKEN_SESSION_COUNT + sessionCount +
                                               ConstInterface.REQ_ARG_SEPARATOR +
                                               ConstInterface.RC_TOKEN_CTX_GROUP + ctxGroup +
                                               ConstInterface.REQ_ARG_SEPARATOR +
                                               ConstInterface.RC_AUTHENTICATION_REQUEST;
         return handshakeAuthUrl;
      }

      /// <summary> 
      /// Executes the initial program in the server:
      /// </summary>
      private void ExecuteInitialRequest()
      {
         // send the INITIAL request to the server. 
         Execute(CommandsProcessorBase.SendingInstruction.TASKS_AND_COMMANDS, CommandsProcessorBase.SessionStage.INITIAL, null);
      }

      /// <summary>
      /// Gets all the values of the logon dialog executions properties from the ClientManager
      /// </summary>
      /// <param name="logonDialogExecProps"></param>
      private static Dictionary<string, string> GetLogonDialogExecProps()
      {
         Dictionary<string, string> logonDialogExecProps = new Dictionary<string, string>();

         // LogonWindowTitle
         String windowTitle = ClientManager.Instance.getLogonWindowTitle();
         if (String.IsNullOrEmpty(windowTitle))
            windowTitle = GuiConstants.LOGON_CAPTION + ClientManager.Instance.getAppName();
         logonDialogExecProps.Add(GuiConstants.STR_LOGON_WIN_TITLE, windowTitle);

         // LogonGroupTitle
         String grpTitle = ClientManager.Instance.getLogonGroupTitle();
         if (!String.IsNullOrEmpty(grpTitle))
            grpTitle = (grpTitle.Length > GuiConstants.GROUP_BOX_TITLE_MAX_SIZE
                        ? grpTitle.Substring(0, GuiConstants.GROUP_BOX_TITLE_MAX_SIZE)
                        : grpTitle);
         else
            grpTitle = ConstInterface.APPL_LOGON_SUB_CAPTION;
         logonDialogExecProps.Add(GuiConstants.STR_LOGON_GROUP_TITLE, grpTitle);

         // LogonMessageCaption
         String logonMsgCaption = ClientManager.Instance.getLogonMsgCaption();
         if (!String.IsNullOrEmpty(logonMsgCaption))
         {
            logonMsgCaption = logonMsgCaption.Length > GuiConstants.MSG_CAPTION_MAX_SIZE
                                          ? logonMsgCaption.Substring(0, GuiConstants.MSG_CAPTION_MAX_SIZE)
                                          : logonMsgCaption;

            logonDialogExecProps.Add(GuiConstants.STR_LOGON_MSG_CAPTION, logonMsgCaption);
         }

         // LogonWindowIconURL
         Uri windowIconUrl = ClientManager.Instance.getLogonWindowIconURL();
         if (windowIconUrl != null && !String.IsNullOrEmpty(windowIconUrl.ToString()))
            logonDialogExecProps.Add(GuiConstants.STR_LOGO_WIN_ICON_URL, windowIconUrl.ToString());

         // LogonImageURL
         Uri windowImageUrl = ClientManager.Instance.getLogonWindowImageURL();
         if (windowImageUrl != null && !String.IsNullOrEmpty(windowImageUrl.ToString()))
            logonDialogExecProps.Add(GuiConstants.STR_LOGON_IMAGE_URL, windowImageUrl.ToString());

         // LogonUserIDCaption
         String userIdCaption = ClientManager.Instance.getLogonUserIdCaption();
         if (!String.IsNullOrEmpty(userIdCaption))
         {
            userIdCaption = (userIdCaption.Length > GuiConstants.LABEL_CAPTION_MAX_SIZE
                             ? userIdCaption.Substring(0, GuiConstants.LABEL_CAPTION_MAX_SIZE)
                             : userIdCaption);
            logonDialogExecProps.Add(GuiConstants.STR_LOGON_USER_ID_CAPTION, userIdCaption);
         }

         // LogonPasswordCaption
         String passwordCaption = ClientManager.Instance.getLogonPasswordCaption();
         if (!String.IsNullOrEmpty(passwordCaption))
         {
            passwordCaption = passwordCaption.Length > GuiConstants.LABEL_CAPTION_MAX_SIZE ?
                              passwordCaption.Substring(0, GuiConstants.LABEL_CAPTION_MAX_SIZE) : passwordCaption;
            logonDialogExecProps.Add(GuiConstants.STR_LOGON_PASS_CAPTION, passwordCaption);
         }

         // LogonOKCaption
         String okBtnCaption = ClientManager.Instance.getLogonOKCaption();
         if (!String.IsNullOrEmpty(okBtnCaption))
            logonDialogExecProps.Add(GuiConstants.STR_LOGON_OK_CAPTION, okBtnCaption);

         // LogonCancelCaption
         String cancelCaption = ClientManager.Instance.getLogonCancelCaption();
         if (!String.IsNullOrEmpty(cancelCaption))
            logonDialogExecProps.Add(GuiConstants.STR_LOGON_CANCEL_CAPTION, cancelCaption);

         return logonDialogExecProps;
      }

      /// <summary>
      ///  execute HTTP request in order to detect settings
      /// </summary>
      /// <param name="serverUrl"></param>
      /// <param name="allowRetry">whether or not retry after connection failure.</param>
      /// <returns></returns>
      internal static bool EchoWebRequester(string serverUrl, bool allowRetry)
      {
         bool requestSucceed = true;
         try
         {
            String tempString = DateTime.Now.ToString();
            bool isError;
            byte[] echoedBytes = HttpManager.GetInstance().GetContent(serverUrl + "?echo=" + tempString, null, null, false, allowRetry, out isError);
            String echoedString = HttpUtility.UrlDecode(Encoding.UTF8.GetString(echoedBytes, 0, echoedBytes.Length), Encoding.UTF8);
            if (tempString != echoedString)
               Logger.Instance.WriteWarningToLog(String.Format("RemoteCommandsProcessor.EchoWebRequester: {0} <> {1}", echoedString, tempString));
         }
         catch (Exception)
         {
            requestSucceed = false;
         }
         return requestSucceed;
      }

      /// <summary> When we reconnect to the server once it is disconnected, server will create a new context and
      /// therefore MP variables are with their intial (empty) value and not with their real values.
      /// So, we must always send Main program's field's latest  values to the server.
      /// </summary>
      /// <returns></returns>
      private String BuildXMLForMainProgramDataViewSwitchingFromLocal()
      {
         StringBuilder message = new StringBuilder();

         if (ClientManager.Instance.RuntimeCtx.ContextID == RemoteCommandsProcessor.RC_NO_CONTEXT_ID)
         {
            MGData firstMgData = MGDataCollection.Instance.getMGData(0);
            Task mainPrg = firstMgData.getMainProg(0);

            while (mainPrg != null)
            {
               mainPrg.buildXML(message);
               mainPrg = firstMgData.getNextMainProg(mainPrg.getCtlIdx());
            }

            if (ClientManager.Instance.ShouldScrambleAndUnscrambleMessages)
            {
               string scrambledChanges = Scrambler.Scramble(message.ToString());
               message = new StringBuilder(scrambledChanges);
            }

            message.Insert(0, XMLConstants.MG_TAG_OPEN);
            message.Append(XMLConstants.MG_TAG_XML_END_TAGGED);
         }

         return message.ToString();
         
      }

      /// <summary> On trying to connect/reconnect to the engine, add special command.
      /// 1. On trying to connect: Add VerifyCache command to check the integrity of the sources.
      /// 2. On trying to reconnect: Add AbortNonOfflineTasks to close all the previously opened 
      //     non-offline tasks.
      /// </summary>
      /// <returns></returns>
      private String PrepareCommandForSwitchingFromLocal()
      {
         Debug.Assert(CommandsProcessorManager.SessionStatus == CommandsProcessorManager.SessionStatusEnum.Local);

         //The two commands are mutually exclusive. If the client is re-connecting to an existing context, there are 
         //no chances that the sources were modified on the server (to modify the sources, the context needs to be 
         //terminated and engine needs to be restarted). On the other hand, when the client, which started in 
         //disconnected mode, tries to connect to the server, a new context will be created and so, there wont 
         //be any unwanted non-offline tasks on the engine.

         ClientOriginatedCommand command = null;
         if (ClientManager.Instance.RuntimeCtx.ContextID != RemoteCommandsProcessor.RC_NO_CONTEXT_ID)
         {
            // On trying to re-connect to an existing context, tell the runtime engine to close all the previously opened 
            // non-offline tasks because, we had closed them when the connection was lost.
            command = CommandFactory.CreateAbortNonOfflineTasksCommand();
         }
         else
         {
            // Spec section 6.1.1:	Calling from Offline to non-offline program.
            // "During the first access to the server from status local/disconnected, 
            //    a check will be made to see if the metadata [e.g. Main Program, offline programs, data sources, 
            //    environment files (e.g. colors file )] on the client is equal to the one on the server."
            var collectedOfflineRequiredMetadata = ApplicationSourcesManager.GetInstance().OfflineRequiredMetadataCollection.CollectedMetadata;
            Debug.Assert(collectedOfflineRequiredMetadata != null && collectedOfflineRequiredMetadata.Count > 0);
            bool isAccessingServerUsingHTTPS = IsAccessingServerUsingHTTPS();
            command = CommandFactory.CreateVerifyCacheCommand(collectedOfflineRequiredMetadata, isAccessingServerUsingHTTPS);
         }

         var commandStr = new StringBuilder(command.Serialize());

         if (ClientManager.Instance.ShouldScrambleAndUnscrambleMessages)
         {
            string scrambledChanges = Scrambler.Scramble(commandStr.ToString());
            commandStr = new StringBuilder(scrambledChanges);
         }
         commandStr.Insert(0, XMLConstants.MG_TAG_OPEN);
         commandStr.Append(XMLConstants.MG_TAG_XML_END_TAGGED);

         return commandStr.ToString();
      }

      /// <summary>build the xml request; send it to the runtime-engine; receive a response</summary>
      /// <param name="sendingInstruction">instruction what to send during execution - NO_TASKS_OR_COMMANDS / ONLY_COMMANDS / TASKS_AND_COMMANDS.</param>
      /// <param name="sessionStage">HANDSHAKE / INITIAL / NORMAL.</param>
      /// <param name="exp">expression to be executed after parsing the response from the server.</param>
      internal override void Execute(SendingInstruction sendingInstruction, SessionStage sessionStage, IResultValue res)
      {
         String reqBuf;
         bool isInitialCall = (sessionStage == SessionStage.INITIAL);

         //If the last server access code is UnsynchroizedMetadata, there is no point trying to 
         //connect the server again. So, remove the server command from the queue and abort the request. 
         if (ServerLastAccessStatus == ServerAccessStatus.UnsynchroizedMetadata)
         {
            MGDataCollection.Instance.RemoveServerCommands();
            return;
         }

         if (sendingInstruction == SendingInstruction.NO_TASKS_OR_COMMANDS)
            reqBuf = null;
         else
         {
            reqBuf = ClientManager.Instance.PrepareRequest(sendingInstruction == SendingInstruction.TASKS_AND_COMMANDS);

            if (!isInitialCall)
            {
               var changes = new StringBuilder();
               var buffer = new StringBuilder(reqBuf);

               // send activity monitor logs to the server
               if (_logAccumulator != null && !_logAccumulator.Empty())
                  buffer.Append(_logAccumulator.Read());

               // synchronize client-side SetParams and INIPut data to the server
               changes.Append(ClientManager.Instance.getGlobalParamsTable().mirrorToXML());
               changes.Append(ClientManager.Instance.getEnvParamsTable().mirrorToXML());

               // If there is stuff to synchronize, wrap it, scramble it and add the MGDATA flag
               if (changes.Length > 0)
               {
                  changes.Insert(0, "<" + ConstInterface.MG_TAG_ENV_CHANGES + ">");
                  changes.Append("</" + ConstInterface.MG_TAG_ENV_CHANGES + ">");

                  if (ClientManager.Instance.ShouldScrambleAndUnscrambleMessages)
                  {
                     string scrambledChanges = Scrambler.Scramble(changes.ToString());
                     changes = new StringBuilder(scrambledChanges);
                  }
                  changes.Insert(0, XMLConstants.MG_TAG_OPEN);
                  changes.Append(XMLConstants.MG_TAG_XML_END_TAGGED);

                  buffer.Append(changes);
               }

               reqBuf = buffer.ToString();
            }
         }

         string orgReqBuf = reqBuf;
         string respBuf = null;
         RequestStatus requestStatus;
         //Execute a request, as long as instructed to retry. 
         //A non-offline application terminates the client in case of ServerError.
         //But, an offline application handles the server error differently:
         //In some cases, it closes the non-offline tasks and continues with the offline ones.
         //In some cases, it terminates the client.
         //In some cases, it requests to resend the request and so on.
         //These cases are better explained in HandleServerErrorForOfflineApplication().
         do
         {
            reqBuf = (CommandsProcessorManager.SessionStatus == CommandsProcessorManager.SessionStatusEnum.Local ?
                      PrepareCommandForSwitchingFromLocal() + orgReqBuf + BuildXMLForMainProgramDataViewSwitchingFromLocal() :
                      orgReqBuf);

            try
            {
               respBuf = DispatchRequest(ServerUrl, reqBuf, sessionStage, out requestStatus);
            }
            catch (ServerError)
            {
               throw;
            }
         } while (requestStatus == RequestStatus.Retry);

         if (respBuf == null)
            return;

         FlowMonitorQueue.Instance.enable(false);
         ClientManager.Instance.ProcessResponse(respBuf, MGDataCollection.Instance.currMgdID, new OpeningTaskDetails(), res);

         if (isInitialCall)
         {
            // If it was initial call. we will send all pending logs (Failed earlier execution scenario)
            // They will be sent here, after first request is served. 
            if (ClientManager.Instance.getLogClientSequenceForActivityMonitor() &&
                ClientLogAccumulator.ExistingLogsFound())
            {
               bool success = SendPendingLogsToServer();

               // if we have successfully sent the pending logs delete them
               if (success)
                  DeletePendingLogs();
            }
            HttpManager.GetInstance().RecentNetworkActivities.GetTranslateStrings();

            // In case of SpecialClientDisableCache=Y flag (ini), the sources are not copied to client cache, every thing is sent
            // as part of response.(instead of cache file urls in response).
            // DataSourceConverter is allocated when data source repository is loaded from sources in cache
            // Need to check if datasource conversion is needed (or can be supported) when SpecialClientDisableCache = y?
            if (ClientManager.Instance.LocalManager.DataSourceConverter != null)
            {
               // Once all sources are read and data source conversion is successfully completed during processing the server response,
               // its time to commit the tables and application sources.
               ClientManager.Instance.LocalManager.DataSourceConverter.Commit();
               ApplicationSourcesManager.GetInstance().Commit();

               // Sources integrity is needed in initial stage of application execution, when application sources are get at once.
               // So after commiting the sources, disable sources integrity.
               ApplicationSourcesManager.GetInstance().DisableSourceIntegrity();
            }
         }
         else if (_logAccumulator != null && !_logAccumulator.Empty())
         {
            // Accumulated logs were sent, so reset (delete) them now
            _logAccumulator.Reset();
         }

         // refresh the displays
         if (sendingInstruction == SendingInstruction.TASKS_AND_COMMANDS)
         {
            MGDataCollection mgdTab = MGDataCollection.Instance;
            mgdTab.startTasksIteration();
            Task task;
            while ((task = mgdTab.getNextTask()) != null)
            {
               if (task.DataViewWasRetrieved)
                  task.RefreshDisplay();
            }
         }

         if (isInitialCall)
         {
            ClientManager.Instance.setGlobalParams(null);
         }

         // check and if needed save the initial response into the local/client file system
         CheckAndSaveOfflineStartupInfo(respBuf, isInitialCall);
      }

      /// <summary> Handle server errors for Offline applications. </summary>
      /// The network/server errors are handled differently according to the application execution stage.
      /// 1. Initializing:
      ///   a) The error message is not shown.
      ///   b) The connection is set to dropped.
      ///   c) The server last access status is set.
      ///   d) The UnavailableServer event is put in queue to be handled once the program starts.
      ///   
      /// 2. Executing:
      ///   a) The error message is shown.
      ///   b) All existing non-offline tasks are closed.
      ///   c) The connection is set to dropped.
      ///   d) The server last access status is set. Status is saved in LastOffline when relevant.
      ///   e) The UnavailableServer event is handled immediately.
      ///      Exception:
      ///      If the error is 'Context not found' and there are no non-offline task's running, the error 
      ///      is ignored and a fresh request is sent which will create a new context on the server (engine).
      ///      
      /// 3. Terminating:
      ///   a) The error message is not shown.
      ///   b) The connection is set to dropped.
      ///   c) The server last access status is set.
      ///   d) The UnavailableServer event is handled immediately.
      /// 
      /// 4. HandlingLogs:
      ///   No special handling is required here. If there is an error, just throw it and the caller just ignores it.
      /// <param name="error"></param>
      /// <returns>Returns whether the request should be retried or aborted.</returns>
      private RequestStatus HandleServerErrorForOfflineApplication(ServerError serverError)
      {
         RequestStatus requestStatus = RequestStatus.Abort;

         Debug.Assert(ClientManager.Instance.HostsOfflineApplication);

         //No special handling is required here. If there is an error, just throw it and the caller just ignores it.
         if (ClientManager.Instance.ApplicationExecutionStage == ApplicationExecutionStage.HandlingLogs)
            return requestStatus;

         //1. re-init server communication details.
         if (serverError.GetCode() == ServerError.ERR_CTX_NOT_FOUND)
         {
            //Only if context does not exist on the server. Otherwise, there is a chance
            //that we will reconnect to the server and then, we will use the same details.
            ClientManager.Instance.RuntimeCtx.ContextID = RC_NO_CONTEXT_ID;
            ClearSessionCounter();
         }
         else if (serverError.GetCode() == ServerError.ERR_UNSYNCHRONIZED_METADATA)
         {
            // Save this indication in last offline, so that next time client will start in connected mode to sync metadata
            LocalCommandsProcessor.GetInstance().SaveUnsyncronizedMetadataFlagInLastOfflineFile();
         }

         //2. Set ServerLastAccessStatus
         SetServerLastAccessStatusOnServerError(serverError);

         //3. Set ConnectionDropped
         ClientManager.Instance.GetService<IConnectionStateManager>().ConnectionDropped();

         if (ClientManager.Instance.ApplicationExecutionStage == ApplicationExecutionStage.Executing)
         {
            //If there is no non-offline task in the runtime tree, the request should be resent 
            //to create a new context and it should be executed in this new context.
            //so, set the RequestStatus to Retry so that the caller will resend request.
            //Since the _sessionCount is set to 0, this new request will be sent as 
            //OfflineRCInitialRequest.
            if (serverError.GetCode() == ServerError.ERR_CTX_NOT_FOUND &&
                !MGDataCollection.Instance.ContainsNonOfflineProgram())
            {
               return RequestStatus.Retry;
            }
            else
            {
               //4. Show error message and Close non-offline tasks
               if (HttpManager.GetInstance().GetCommunicationsFailureHandler().ShowCommunicationErrors)
               {
                  String title = ClientManager.Instance.getMessageString(MsgInterface.BRKTAB_STR_ERROR);
                  int style = Styles.MSGBOX_ICON_ERROR | Styles.MSGBOX_BUTTON_OK;
                  if (serverError.Message.StartsWith("<HTML", StringComparison.CurrentCultureIgnoreCase))
                     ClientManager.Instance.processHTMLContent(serverError.Message);
                  else
                     Commands.messageBox(null, title, serverError.GetMessage(), style);
               }

               MGDataCollection.Instance.StopNonOfflineTasks();
            }
         }

         //5. Raise UnavailableServer event.
         RaiseUnavailableServerEvent();

         //throw the exception, 
         //1. if ApplicationStage = Initializing: in this case the caller will start the session in offline.
         //2. if ApplicationStage = Terminating: in this case, the caller will execute the MP TS locally.
         if (ClientManager.Instance.ApplicationExecutionStage != ApplicationExecutionStage.Executing)
            throw serverError;

         return requestStatus;
      }

      /// <summary> Sets the status of the last server access from the ServerError </summary>
      /// <param name="serverError"></param>
      private void SetServerLastAccessStatusOnServerError(ServerError serverError)
      {
         ServerAccessStatus lastAccessStatus;

         switch (serverError.GetCode())
         {
            case ServerError.ERR_UNSYNCHRONIZED_METADATA:
               lastAccessStatus = ServerAccessStatus.UnsynchroizedMetadata;
               break;

            case ServerError.ERR_CTX_NOT_FOUND:
               lastAccessStatus = ServerAccessStatus.UnavailableContext;
               break;

            case ServerError.ERR_CANNOT_EXECUTE_OFFLINE_RC_IN_ONLINE_MODE:
               lastAccessStatus = ServerAccessStatus.InvalidDeploymentMode;
               break;

            default:
               if (serverError.InnerException != null && serverError.InnerException is WebException)
                  lastAccessStatus = ServerAccessStatus.InaccessibleWebServer;
               else
                  lastAccessStatus = ServerAccessStatus.RequestNotServedByServer;
               break;
         }

         ServerLastAccessStatus = lastAccessStatus;
      }

      /// <summary> Raise UnavailableServer event. 
      /// If the ApplicationExecutionStage is Initializing, the event cannot be handled immediately since 
      /// there is no task running to handle it. So, it should be put in queue to be handled once the task starts.
      /// In other cases, the event should be handled immediately.
      /// </summary>
      private void RaiseUnavailableServerEvent()
      {
         //According to section #7.4.1 modified in revisions #64 and #65 of the offline spec, 
         //the unavailable server event will not be raised during handshake/getting metadata.
         if (ClientManager.Instance.ApplicationExecutionStage != ApplicationExecutionStage.Initializing)
         {
            //Raise the 'Unavailable Server' event on the task of the server operation which generated the error.
            //But it needs an adjustment. Server error will occur only for server operations which are allowed 
            //only from MP and non-offline tasks. There is no problem if the task is MP. But, if it is a non-offline 
            //program, it would be closed on the error. So, the event should be raised on MP even in this case. Get
            //the correct MP by traversing the parent tasks.
            Task task = ClientManager.Instance.EventsManager.getLastRtEvent().getTask();

            while (!task.isMainProg())
            {
               task = task.getParent();
            }

            RunTimeEvent rtEvt = new RunTimeEvent(task, false);
            rtEvt.setInternal(InternalInterface.MG_ACT_UNAVAILABLE_SERVER);

            ClientManager.Instance.EventsManager.handleEvent(rtEvt, false);
         }
      }

      /// <summary>
      /// verify that the last initial response file saved on the client for offline execution is valid (otherwise remove it).
      /// the last initial response file becomes invalid in the following cases:
      ///   (1) the executed program's public name (ClientManager.Instance.getPrgName()) doesn't match the one saved in the last initial response file.
      ///   (2) the executed program's modification time doesn't match the last initial response file's modification time .
      /// </summary>
      private void VerifyLastOfflineInitialResponseFile()
      {
         Debug.Assert(LocalCommandsProcessor.GetInstance().CanStartWithoutNetwork);

         LocalCommandsProcessor.GetInstance().InitializeEncryptor();
         string lastOfflineInitialResponsePrgName = LocalCommandsProcessor.GetInstance().GetPublicNameFromLastOfflineInitialResponse();

         // TODO: Also check if all code related to last offline execution properties can be shifted to ClientManager
         // or a new class whose object is maintained in ClientManager. 
         string lastOfflineExecutionFileName = LocalCommandsProcessor.GetInstance().GetLastOfflineExecutionPropertiesFileName();

         /// the last initial response file becomes invalid in the following cases:
         ///   (1) the executed program's public name (ClientManager.Instance.getPrgName()) doesn't match the one saved in the last initial response file.
         ///   (2) the executed program's modification time doesn't match the last initial response file's modification time .
         if (!ClientManager.Instance.getPrgName().Equals(lastOfflineInitialResponsePrgName) ||
             _startupProgramModificationTime != null && !_startupProgramModificationTime.Equals(HandleFiles.getFileTime(lastOfflineExecutionFileName)))
            HandleFiles.deleteFile(lastOfflineExecutionFileName);
      }

      /// <summary>
      /// check and if needed save the initial response into the local/client file system.
      /// </summary>
      /// <param name="responseString"></param>
      private void CheckAndSaveOfflineStartupInfo(String responseString, bool isInitialCall)
      {
         String initialResponseString;

         // The execution.properties should be saved only if the first task is Offline. 
         // The first task can either be the first non-MainProgram task or in absence of such a task, it can be the MainProgram with MDI frame. 
         if (MGDataCollection.Instance.StartupMgData.ShouldSaveOfflineStartupInfo)
         {
            //Save execution properties including encryption details.
            IEncryptor encryptor = PersistentOnlyCacheManager.GetInstance();
            if (isInitialCall)
            {
               //if this is the initial request, save the response from server in LastOffline file, so that it can be used 
               //by the client to start an offline session.
               initialResponseString = responseString;
               MgProperties offlineExecutionProps = new MgProperties();

               offlineExecutionProps[ConstInterface.LAST_OFFLINE_FILE_VERSION] = System.Convert.ToString(ConstInterface.LastOfflineFileVersion);

               //put DISABLE_ENCRYPTION tag only if encryption is disabled. By default, it should be false.
               //If encryption is on, set the encryption key -- only if it is different than the default key.
               if (encryptor.EncryptionDisabled)
                  offlineExecutionProps[ConstInterface.DISABLE_ENCRYPTION] = "Y";
               else if (encryptor.HasNonDefaultEncryptionKey())
               {
                  Debug.Assert(encryptor.EncryptionKey != null);

                  // TODO: Scrambling the encryption key in the execution.properties_LastOffline file is not secured enough!
                  String scrambledKey = Scrambler.Scramble(Encoding.Default.GetString(encryptor.EncryptionKey, 0, encryptor.EncryptionKey.Length));
                  offlineExecutionProps[ConstInterface.ENCRYPTION_KEY] = scrambledKey;
               }

               offlineExecutionProps[ConstInterface.SECURE_MESSAGES] = ClientManager.Instance.ShouldScrambleAndUnscrambleMessages ? "Y" : "N";

               // save the initial response as a property in a new execution.properties_lastOffline file:
               initialResponseString = XmlParser.RemoveXmlElements(initialResponseString, ConstInterface.MG_TAG_DATAVIEW, ConstInterface.MG_TAG_EVENTS_QUEUE);
               byte[] initialResponseBytes = null;
               string cleartextInitialResponseString = initialResponseString;
               if (encryptor.EncryptionDisabled)
                  initialResponseString = XMLConstants.CDATA_START + "\n" + initialResponseString + "\n" + XMLConstants.CDATA_END;
               else
               {
                  initialResponseBytes = Encoding.UTF8.GetBytes(initialResponseString);
                  initialResponseBytes = encryptor.Encrypt(initialResponseBytes);
                  initialResponseBytes = Base64.encode(initialResponseBytes);
                  initialResponseString = Encoding.ASCII.GetString(initialResponseBytes, 0, initialResponseBytes.Length);
               }

               offlineExecutionProps[ConstInterface.INITIAL_RESPONSE] = initialResponseString;

               // save the modified execution properties 
               string lastOfflineExecutionFileName = LocalCommandsProcessor.GetInstance().GetLastOfflineExecutionPropertiesFileName();
               offlineExecutionProps.WriteToXMLFile(lastOfflineExecutionFileName);

               // set its time stamp according to the last modification time of the startup program.
               // the method 'VerifyLastOfflineInitialResponseFile' relies on this fact to delete the file in any subsequent session in case the program was modified in the server.
               if (_startupProgramModificationTime != null)
                  HandleFiles.setFileTime(lastOfflineExecutionFileName, _startupProgramModificationTime);
            }
            else
            {
               //If during the session, user credential changed, server will resend these info.
               //In such a case, replace the old user details with the new one in the existing LastOffline, so that the next offline session
               //will use these new credentials.
               Dictionary<String, String> newUserData;
               Dictionary<String, String> oldUserData;
               String[] elementsToReplace = { ConstInterface.MG_TAG_USER_RIGHTS, ConstInterface.MG_TAG_USER_DETAILS };

               newUserData = XmlParser.GetElements(responseString, elementsToReplace);

               //If server has sent new User credentials, update the LastOffline file accordingly.
               if (newUserData.Count > 0)
               {
                  initialResponseString = LocalCommandsProcessor.GetInstance().GetLastOfflineInitialResponse();
                  oldUserData = XmlParser.GetElements(initialResponseString, elementsToReplace);

                  //Replace userRights element in initialResponse, if needed
                  if (newUserData.ContainsKey(ConstInterface.MG_TAG_USER_RIGHTS))
                     initialResponseString = initialResponseString.Replace(oldUserData[ConstInterface.MG_TAG_USER_RIGHTS], newUserData[ConstInterface.MG_TAG_USER_RIGHTS]);

                  //Replace userDetails element in initialResponse, if needed
                  if (newUserData.ContainsKey(ConstInterface.MG_TAG_USER_DETAILS))
                     initialResponseString = initialResponseString.Replace(oldUserData[ConstInterface.MG_TAG_USER_DETAILS], newUserData[ConstInterface.MG_TAG_USER_DETAILS]);

                  byte[] initialResponseBytes = null;
                  if (encryptor.EncryptionDisabled)
                     initialResponseString = XMLConstants.CDATA_START + "\n" + initialResponseString + "\n" + XMLConstants.CDATA_END;
                  else
                  {
                     initialResponseBytes = Encoding.UTF8.GetBytes(initialResponseString);
                     initialResponseBytes = encryptor.Encrypt(initialResponseBytes);
                     initialResponseBytes = Base64.encode(initialResponseBytes);
                     initialResponseString = Encoding.ASCII.GetString(initialResponseBytes, 0, initialResponseBytes.Length);
                  }

                  LocalCommandsProcessor.GetInstance().SaveInitialResponseInLastOfflineFile(initialResponseString);
               }
            }
         }
      }

      /// <summary>send 'reqBuf' to 'url'; receive a response.
      /// For Normal/InitialCall requests, the url points to the web-requester.
      /// The other parameters and data goes as the encoded body.
      /// This means that it is a POST request.
      /// On the other hand, HandShake request is a GET request and so the parameters are
      /// sent in the URL itself. In this case, encodedBody has to be null.
      /// </summary>
      /// <param name="url">URL to be accessed.</param>
      /// <param name="reqBuf">data to be sent to the url</param>
      /// <param name="sessionStage">HANDSHAKE / INITIAL / NORMAL.</param>
      /// <returns>response</returns>
      private String DispatchRequest(String url, String reqBuf, SessionStage sessionStage, out RequestStatus requestStatus)
      {
         String response = null;
         String encodedBody = null;
         String urlSuffix = null;

         if (url == null)
         {
            Logger.Instance.WriteExceptionToLog("in sendMsgToSrvr() unknown server");
            requestStatus = RequestStatus.Abort;
            return response;
         }

         if (sessionStage != SessionStage.HANDSHAKE)
         {
            urlSuffix = BuildUrlSuffix(reqBuf != null, sessionStage == SessionStage.INITIAL);
            String reqBufEncoded = HttpUtility.UrlEncode(reqBuf, Encoding.UTF8);
            encodedBody = urlSuffix + reqBufEncoded;
         }

         try
         {
            if (Logger.Instance.ShouldLogExtendedServerRelatedMessages())
               Logger.Instance.WriteServerMessagesToLog(String.Format("MESSAGE TO SERVER:\n URL: {0}\n BODY: {1}", url, encodedBody));
           
            response = Execute(url, encodedBody);

            /* remove the unwanted data before the MGDATA tag */
            if (!response.StartsWith("<HTML", StringComparison.CurrentCultureIgnoreCase))
            {
               int startIdx = response.IndexOf("<xml id=\"MGDATA\">");
               if (startIdx > 0)
                  response = response.Substring(startIdx);
            }

            Logger.Instance.WriteServerMessagesToLog("MESSAGE FROM SERVER: " + response);

            // handshake requests are always scrambled (the scrambling can be disabled starting from the 3rd request).
            if (sessionStage == SessionStage.HANDSHAKE ||
                response.Length > 0 &&
                (ClientManager.Instance.ShouldScrambleAndUnscrambleMessages && response != "<xml id=\"MGDATA\">\n</xml>"))
            {
               if (sessionStage == SessionStage.HANDSHAKE)
                  // handshake requests are always scrambled.
                  Debug.Assert(response.Length > 0 && response[0] != '<');
               response = UnScramble(response);
            }

            Logger.Instance.WriteDevToLog("MESSAGE FROM SERVER: (size = " + response.Length + ")" +
                                                 OSEnvironment.EolSeq + response);

            _lastRequestTime = Misc.getSystemMilliseconds();

            //If we are here, it means that the server (i.e. the engine) was successfully connected.
            ClientManager.Instance.GetService<IConnectionStateManager>().ConnectionEstablished();
            requestStatus = RequestStatus.Handled;
            ServerLastAccessStatus = ServerAccessStatus.Success;
         }
         catch (ServerError serverError)
         {
            requestStatus = RequestStatus.Abort;
            if (ClientManager.Instance.HostsOfflineApplication)
               requestStatus = HandleServerErrorForOfflineApplication(serverError);
            else
               throw;
         }

         return response;
      }

      /// <summary> Checks the response string, throws an error if the response string contains
      /// an xml/html error</summary>
      /// <param name="response"></param>
      private void HandleErrorResponse(byte[] response)
      {
         String responseStr = null;

         try
         {
            //error responses are always scrambled by the web server.
            responseStr = Encoding.UTF8.GetString(response, 0, response.Length);
            Logger.Instance.WriteServerMessagesToLog("MESSAGE FROM SERVER: " + responseStr);
            responseStr = UnScramble(responseStr);
            Logger.Instance.WriteServerMessagesToLog("MESSAGE FROM SERVER: " + responseStr);
         }
         catch (Exception)
         {
         }

         if (responseStr.StartsWith("<xmlerr"))
         {
            var e = new ErrorMessageXml(this, responseStr);
            throw new ServerError(e.GetMessage(), e.GetCode());
         }
         else if (responseStr.StartsWith("<HTML", StringComparison.CurrentCultureIgnoreCase))
            throw new ServerError(responseStr);
         else if (responseStr.Equals(EXCEPTION))
            throw new ServerError(ClientManager.Instance.getMessageString(MsgInterface.STR_ERR_INACCESSIBLE_URL) +
                                  OSEnvironment.EolSeq + OSEnvironment.EolSeq +
                                  ServerUrl + OSEnvironment.EolSeq);
      }

      /// <summary>send 'encodedBody' to 'url' and receive a response, without trying to recover from the slow/disruptive network.
      /// This means that this function will issue a request and wait for the response.
      /// </summary>
      /// <param name="url">URL to be accessed.</param>
      /// <param name="encodedBody">In case of POST, content to be sent to server. For other methods, null.</param>
      /// <returns>the response from the server</returns>
      private String Execute(String url, String encodedBody)
      {
         String response = null;

         byte[] responseBytes = GetContent(url, encodedBody, null, false);
         if (responseBytes != null)
            response = Encoding.UTF8.GetString(responseBytes, 0, responseBytes.Length);

         return response;
      }


      /// <summary>
      /// By given file URL (server side), checks the file existence in the local cache without loading it's content.
      /// If the file not found or outdated, reads it from the server and writes to the local cache.
      /// </summary>
      /// <param name="requestedURL">URL to be accessed</param>
      /// <returns></returns>
      internal void DownloadContent(String requestedURL)
      {
         // configure CachingStrategy to allow writing of a new file to local cache, and deny reading of the cached file from the cache into the memory
         var cachingStrategy = new HttpManager.CachingStrategy() { CanWriteToCache = true, CachedContentShouldBeReturned = false, AllowOutdatedContent = false };
         GetContent(requestedURL, null, null, false, cachingStrategy);
      }

      /// <summary> Invoke the request URL & return the response.</summary>
      /// <param name="requestedURL">URL to be accessed.</param>
      /// <param name="decryptResponse">if true, fresh responses from the server will be decrypted using the 'encryptionKey' passed to 'HttpManager.SetProperties'.</param>
      /// <returns>response (from the server).</returns>
      internal override byte[] GetContent(String requestedURL, bool decryptResponse)
      {
         return GetContent(requestedURL, null, null, decryptResponse);
      }

      /// <summary>
      /// Get sources from server and returns the contents
      /// </summary>      
      internal byte[] GetContent(String requestedURL, object requestContent, String requestContentType, bool decryptResponse)
      {
         var cachingStrategy = new HttpManager.CachingStrategy() { CanWriteToCache = true, CachedContentShouldBeReturned = true};
         return GetContent(requestedURL, requestContent, requestContentType, decryptResponse, cachingStrategy);
      }

      /// <summary> Pass 'requestContent' to the URL & return the response.</summary>
      /// <param name="requestedURL">URL to be accessed.</param>
      /// <param name="requestContent">content to be sent to server (relevant only for POST method - is null for other methods). may be byte[] or string.</param>
      /// <param name="requestContentType">type of the content to be sent to server, e.g application/octet-stream (for POST method only).</param>
      /// <param name="decryptResponse">if true, fresh responses from the server will be decrypted using the 'encryptionKey' passed to 'HttpManager.SetProperties'.</param>
      /// <param name="cachingStrategy">!!</param>
      /// <returns>response (from the server).</returns>
      internal byte[] GetContent(String requestedURL, object requestContent, String requestContentType, bool decryptResponse, HttpManager.CachingStrategy cachingStrategy)
      {
         byte[] response;

         try
         {
            // if relative, prefix with the 'protocol://server/' from which the rich-client was activated
            if (requestedURL.StartsWith("/"))
               requestedURL = ClientManager.Instance.getProtocol() + "://" + ClientManager.Instance.getServer() + requestedURL;
            else if (requestedURL.StartsWith("?"))
               requestedURL = ClientManager.Instance.getServerURL() + requestedURL;
            requestedURL = ValidateURL(requestedURL, GetSessionCounter());

            bool isError;
            response = HttpManager.GetInstance().GetContent(requestedURL, requestContent, requestContentType, decryptResponse, true, cachingStrategy, out isError);
            if (isError)
               HandleErrorResponse(response);
         }
         catch (Exception ex)
         {
            // don't write server error to log when the log level is basic
            if (!(ex is ServerError) || Logger.Instance.LogLevel != Logger.LogLevels.Basic)
               Logger.Instance.WriteExceptionToLog(ex, string.Format("requested URL = \"{0}\"", requestedURL));
            
            // the exception should be thrown and the callers should handle it.
            if (!(ex is ServerError))
               ex = new ServerError(ex.Message, ex.InnerException ?? ex);

            throw ex;
         }
         finally
         {
            UpdateRecentNetworkActivitiesTooltip();
         }

         return (response);
      }

      /// <summary> Upload contents to a file on server </summary>
      /// <param name="serverFileName">filename to be saved on the server.</param>
      /// <param name="fileContent">file content to be sent to server.</param>
      /// <param name="contentType">type of the content to be sent to server, e.g application/octet-stream.</param>
      /// <returns></returns>
      internal override byte[] UploadFileToServer(String serverFileName, byte[] fileContent, String contentType)
      {
         byte[] response = null;

         try
         {
            String encodedName = HttpUtility.UrlEncode(serverFileName, Encoding.UTF8);

            // build POST request
            // http://server/MagicScripts/MGRQISPI.dll?CTX=...&TARGETFILE=c:\serverfiles\myfile.txt
            String queryStr = ConstInterface.REQ_ARG_START + ConstInterface.RC_INDICATION + 
                              ConstInterface.RC_TOKEN_CTX_ID + ClientManager.Instance.RuntimeCtx.ContextID + ConstInterface.REQ_ARG_SEPARATOR +
                              ConstInterface.RC_TOKEN_SESSION_COUNT + GetSessionCounter() + ConstInterface.REQ_ARG_SEPARATOR +
                              ConstInterface.RC_TOKEN_TARGET_FILE + encodedName;
            String url = ServerUrl + queryStr;

            // execute http POST request for uploading file content
            response = GetContent(url, fileContent, contentType, false);
         }
         catch (Exception ex)
         {
            Logger.Instance.WriteExceptionToLog(ex);
         }

         return response;
      }

      /// <summary> When LogClientSequenceForActivityMonitor = Y we will be keeping the logs in file and send the file with next 
      /// server access. If client is abnormally terminated then there can be 1 or more files containing logs since 
      /// last server access.Now these logs will be sent to server, when next time RC program is executed,.</summary>
      /// <returns> If sending was successful</returns>
      private bool SendPendingLogsToServer()
      {
         bool success = true;
         var buffer = new StringBuilder(1000); // can not use buffer.setLenght(0) since it causes a memory leak      
         FlowMonitorQueue flowMonitor = FlowMonitorQueue.Instance;

         //Get iterator to names of log files.
         IEnumerator logIterator = ClientLogAccumulator.ExistingLogIterator();

         //while there are more files
         while (logIterator.MoveNext())
         {
            //Take next file name
            var logName = (String)logIterator.Current;
            //Prepare the header line (Client log file : <Context : XXXXXX>) and add it to 
            // flow monitor build monitor message
            String headerLine = ClientLogAccumulator.BuildLogHeaderLine(logName);
            flowMonitor.addFlowVerifyInfo(headerLine);
            StringBuilder logCtxBuf = BuildMonitorMessage();

            // Read file contents append to header line, no need to add the file contents to Flow monitor
            // since file contents are already in XML 
            String fileContents = HandleFiles.readToString(logName);
            logCtxBuf.Append(fileContents);
            buffer.Append(logCtxBuf);
         }

         string respBuf = null;

         ApplicationExecutionStage applicationStage = ClientManager.Instance.ApplicationExecutionStage;

         try
         {
            ClientManager.Instance.ApplicationExecutionStage = ApplicationExecutionStage.HandlingLogs;
            RequestStatus requestStatus;
            respBuf = DispatchRequest(ServerUrl, buffer.ToString(), SessionStage.NORMAL, out requestStatus);
         }
         catch (Exception)
         {
            //do nothing. if sending log fails, there is no harm.
         }
         finally
         {
            ClientManager.Instance.ApplicationExecutionStage = applicationStage;
         }

         if (respBuf == null)
         {
            success = false;
         }
         return success;
      }

      /// <summary> Delete pending logs files.</summary>
      private static void DeletePendingLogs()
      {
         //Iterator to file names
         IEnumerator logIterator = ClientLogAccumulator.ExistingLogIterator();

         // Loop through all files
         while (logIterator.MoveNext())
         {
            var logName = (String)logIterator.Current;
            HandleFiles.deleteFile(logName);
         }
      }

      /// <summary> get the suffix of the URL to be sent to the server. This includes the CTX, SESSION and DATA parameters.</summary>
      /// <param name="hasContent">if true, the HTTP DATA parameter contains something.</param>
      /// <param name="isInitialCall">if true then the generated suffix has no specific context data</param>
      /// <returns> the URL suffix (i.e. "CTX=...&SESSION=...&DATA="</returns>
      private String BuildUrlSuffix(bool hasContent, bool isInitialCall)
      {
         String prefix = ConstInterface.RC_INDICATION + ConstInterface.RC_TOKEN_CTX_ID + ClientManager.Instance.RuntimeCtx.ContextID;

         if (isInitialCall)
         {
            String prgArgs = ClientManager.Instance.getPrgArgs();
            if (prgArgs != null)
            {
               prefix += (ConstInterface.REQ_ARG_SEPARATOR + ConstInterface.REQ_ARGS + "=");

               // insert -A (if missing) before each argument; for example: xxx,yyy --> -Axxx,-Ayyy
               // splits the args based on ',' ignores '\,'
               String[] st = Regex.Split(prgArgs, "(?<!\\\\),", RegexOptions.IgnorePatternWhitespace);
               prgArgs = "";

               for (int i = 0; i < st.Length; i++)
               {
                  String prgArg = st[i];

                  if (!prgArg.StartsWith(ConstInterface.REQ_ARG_ALPHA) &&
                      !prgArg.StartsWith(ConstInterface.REQ_ARG_UNICODE) &&
                      !prgArg.StartsWith(ConstInterface.REQ_ARG_NUMERIC) &&
                      !prgArg.StartsWith(ConstInterface.REQ_ARG_DOUBLE) &&
                      !prgArg.StartsWith(ConstInterface.REQ_ARG_LOGICAL) &&
                      !prgArg.StartsWith(ConstInterface.REQ_ARG_NULL))
                  {
                     prgArgs += ConstInterface.REQ_ARG_ALPHA;
                  }
                  prgArgs += HttpUtility.UrlEncode(prgArg, Encoding.UTF8);

                  // if there is at least one more token, append ","
                  if (i + 1 < st.Length)
                  {
                     prgArgs += ",";
                  }
               }

               prefix += prgArgs;
            } // prgArgs != null

            String envVars = ClientManager.Instance.getEnvVars();
            if (!String.IsNullOrEmpty(envVars))
            {
               // environment variables: ENV1,ENV2,.. --> &ENV1=val1&ENV2=val2...
               String[] envVarsVec = envVars.Split(",".ToCharArray());
               for (int i = 0; i < envVarsVec.Length; i++)
                  prefix += ("&" + envVarsVec[i] + "=" + OSEnvironment.get(envVarsVec[i]));
            }

#if !PocketPC
            // protocol://server/requester/project/project.application?N1=v1&N2=v2... : &N1=v1&N2=v2 --> URL
            HttpManager httpMgrInstance = HttpManager.GetInstance(); // Initialize the http manager

            Debug.Assert(httpMgrInstance != null);

            if (!String.IsNullOrEmpty(httpMgrInstance.ActivationURIWithoutCookies))
            {
               prefix += ("&" + httpMgrInstance.ActivationURIWithoutCookies);
            }
#endif

            String globalParams = ClientManager.Instance.getGlobalParams();
            if (globalParams != null)
            {
               prefix += ("&" + ConstInterface.MG_TAG_GLOBALPARAMS + "=" + globalParams.Replace("+", "%2B"));
            }
         } // isInitialCall
         else if (GetSessionCounter() == 0) // first request from offline, after starting without network access.
         {
            prefix += (ConstInterface.REQ_ARG_SEPARATOR + ConstInterface.OFFLINE_RC_INITIAL_REQUEST);
            prefix += (ConstInterface.REQ_ARG_SEPARATOR + ConstInterface.REQ_APP_NAME + "=" + HttpUtility.UrlEncode(ClientManager.Instance.getAppName(), Encoding.UTF8));
         }

         if (hasContent)
            prefix += ("&" + ConstInterface.RC_TOKEN_SESSION_COUNT + GetSessionCounter() +
                       ConstInterface.REQ_ARG_SEPARATOR + ConstInterface.RC_TOKEN_DATA);

         return prefix;
      }

      /// <summary>send Monitor messaging to the server</summary>
      internal override void SendMonitorOnly()
      {
         MGDataCollection mgdTab = MGDataCollection.Instance;
         FlowMonitorQueue flowMonitor = FlowMonitorQueue.Instance;

         if (mgdTab == null || mgdTab.getMGData(0) == null || mgdTab.getMGData(0).IsAborting)
            return;

#if PocketPC
         // If context is hibernating, don't try to send the flow monitor info
         if (_isHibernated)
         return;
#endif
         if (!flowMonitor.isEmpty())
         {
            // build out message
            StringBuilder buffer = BuildMonitorMessage();

            // If client sequence is to be accumulated until next server access.
            bool shouldAccumulateClientLog = ClientManager.Instance.getLogClientSequenceForActivityMonitor();
            if (shouldAccumulateClientLog)
            {
               if (_logAccumulator == null)
                  _logAccumulator = new ClientLogAccumulator(ClientManager.Instance.RuntimeCtx.ContextID);

               // If we're unable to open the file, deactivate logging accumulation, 
               //    and let the message be sent to the server as done without accumulated logging
               if (_logAccumulator.IsFailed())
                  shouldAccumulateClientLog = false;
            }

            if (shouldAccumulateClientLog)
               // Write message to file.
               _logAccumulator.Write(buffer.ToString());
            else
            {
               ApplicationExecutionStage applicationStage = ClientManager.Instance.ApplicationExecutionStage;

               try
               {
                  ClientManager.Instance.ApplicationExecutionStage = ApplicationExecutionStage.HandlingLogs;
                  RequestStatus requestStatus;
                  DispatchRequest(ServerUrl, buffer.ToString(), SessionStage.NORMAL, out requestStatus);
               }
               catch (Exception)
               {
                  //do nothing. if sending log fails, there is no harm.
               }
               finally
               {
                  ClientManager.Instance.ApplicationExecutionStage = applicationStage;
               }
            }
         }
      }

      /// <summary>build Flow Monitor message</summary>
      private static StringBuilder BuildMonitorMessage()
      {
         var buffer = new StringBuilder(1000);
         FlowMonitorQueue flowMonitor = FlowMonitorQueue.Instance;

         if (!ClientManager.Instance.ShouldScrambleAndUnscrambleMessages)
         {
            buffer.Append(XMLConstants.MG_TAG_OPEN);
            flowMonitor.buildXML(buffer);
         }
         else
         {
            flowMonitor.buildXML(buffer);
            string scrambledOut = Scrambler.Scramble(buffer.ToString());
            buffer = new StringBuilder(1000); // can not use buffer.setLenght(0) since it causes a memory leak
            buffer.Append(XMLConstants.MG_TAG_OPEN + scrambledOut);
         }
         buffer.Append("</" + XMLConstants.MG_TAG_XML + XMLConstants.TAG_CLOSE);

         return buffer;
      }

      /// <summary> This function encodes the host and path in the URL string (e.g. http://host/alias/dir1/dir2/<not encoded>.<not encoded>). 
      /// The whole URL string can not be encoded, because then it will encode the /, :  chars also.
      /// </summary>
      /// <param name="url"></param>
      /// <returns></returns>
      internal static String ValidateURL(String url, long sessionCounter)
      {
         Encoding utf8Encoding = Encoding.UTF8;

         // Create URL object from string
         var u = new Uri(url);

         // Unlike java.net.URL, System.Uri doesn't throw Exception if the protocol is 
         // not one of the expected values i.e. http, https, file. So assert.
         Debug.Assert(u.Scheme.Equals("http") || u.Scheme.Equals("https") || u.Scheme.Equals("file"));

         // Add protocol as is
         var validURL = new StringBuilder(u.Scheme + "://");

         // encode host name
         validURL.Append(HttpUtility.UrlEncode(u.Host, utf8Encoding));

         // If port is specified then append the port
         int port = u.Port;
         if (port != -1)
            validURL.Append(":" + Convert.ToString(port));

         // Get path ie. alias/dir1/dir2/file
         String path = u.AbsolutePath;
         if (!path.StartsWith("/"))
            validURL.Append("/");

         // Tokenize the string and encode each item.
         String[] st = path.Split("/".ToCharArray());

         // recompose the url (note: path is already encoded)
         for (int i = 0; i < st.Length; i++)
            validURL.Append(st[i] + "/");

         String validURLStr = validURL.ToString();
         validURLStr = validURLStr.Substring(0, validURLStr.Length - 1);

         // Remove last /
         if (validURLStr.EndsWith("/"))
            validURLStr = validURLStr.Substring(0, (validURLStr.Length - 1));

         // URLEncoder encode space ( ) with + as per x-www-form-urlencoded format but here
         // we want space to be converted to %20 so replace it
         validURLStr = validURLStr.Replace("\\+", "%20");

         // Add the query string. If it contains the "CTX=&" substring, it means that the server didn't specify the context ID
         // because by now it should be already known to the client, and so here we will insert the actual context ID before
         // the '&' symbol.
         if (u.Query != null)
         {
            string modifiedQuery = u.Query;  //this query string will be modified to contain the actual context ID
            const string CTX_ID_PLACEHOLDER = "CTX=&";
            int ctxIdIdx = u.Query.IndexOf(CTX_ID_PLACEHOLDER);

            // We use (CTX_ID_PLACEHOLDER.Length-1) because we would like to insert the context ID before the last character ('&').
            if (ctxIdIdx > -1)
               modifiedQuery = u.Query.Insert(ctxIdIdx + CTX_ID_PLACEHOLDER.Length - 1,
                                              String.Format("{0}&{1}{2}",
                                              ClientManager.Instance.RuntimeCtx.ContextID.ToString(),
                                              ConstInterface.RC_TOKEN_SESSION_COUNT, sessionCounter));

            validURLStr += modifiedQuery;
         }

         return validURLStr;
      }

      /// <summary> Update the status bar tooltip with the recent network activity</summary>
      internal void UpdateRecentNetworkActivitiesTooltip()
      {
         if (ClientManager.Instance.getDisplayStatisticInfo())
         {
            MgStatusBar statusBar = GetTopmostStatusBar();
            if (statusBar != null)
            {
               String toolTip = HttpManager.GetInstance().RecentNetworkActivities.ToTooltipString();
               statusBar.UpdatePaneToolTip(ConstInterface.SB_IMG_PANE_LAYER, toolTip);
            }
         }
      }

      /// <summary> Returns the first task of the current MGData. If no current MGData or no tasks in it then it returns null.</summary>
      private MgStatusBar GetTopmostStatusBar()
      {
         if (_statusBar == null)
         {
            Task lastFocusedTask = ClientManager.Instance.getLastFocusedTask();
            if (lastFocusedTask != null)
               _statusBar = lastFocusedTask.getTopMostForm().GetStatusBar(false);
         }
         return _statusBar;
      }

      /// <summary>has the time since the last request to the server exceeded [MAGIC_ENV]ContextInactivityTimeout?</summary>
      private bool InactivityTimeoutExpired()
      {
         bool expired = false;
         if (ClientManager.Instance.getEnvironment().getContextInactivityTimeout() > 0)
         {
            long currTimeMilli = Misc.getSystemMilliseconds();
            expired = (currTimeMilli - _lastRequestTime >
                       ClientManager.Instance.getEnvironment().getContextInactivityTimeout() * 100);
         }
         return expired;
      }

#if !PocketPC
      /// <summary>
      /// animates an icon on the status bar of the topmost task (if a status bar exists, otherwise does nothing) during access to the network.
      /// </summary>
      internal class ExternalAccessAnimator
      {
         private long _animatingCount;
         private Thread _animatorThread;

         /// <summary> Start the animation.</summary>
         internal void Start()
         {
            if (Interlocked.Increment(ref _animatingCount) == 1)
            {
               _animatorThread = new Thread(Run);
               _animatorThread.Start();
            }
         }

         /// <summary> Stop the animation</summary>
         internal void Stop()
         {
            Debug.Assert(_animatingCount >= 1);
            if (Interlocked.Decrement(ref _animatingCount) == 0)
            {
               // stop animation
               _animatorThread.Abort();
               try
               {
                  GUIManager guiManager = GUIManager.Instance;
                  MgStatusBar statusBarToAnimateOn = GetInstance().GetTopmostStatusBar();
                  if (statusBarToAnimateOn != null)
                  {
                     statusBarToAnimateOn.UpdatePaneContent(ConstInterface.SB_IMG_PANE_LAYER, String.Empty);
                     Commands.beginInvoke();
                  }
               }
               catch (Exception)
               {
                  //TODO: redesign:
                  // 1. RemoteCommandsProcessor shouldn't be responsible for the server access animation, since it doesn't know whether the server is actually accessed.
                  // 2. retrieving the topmost statusbar in the lower layers of RemoteCommandsProcessor/HttpManager is ugly and unclear. 
               }
            }
         }

         /// <summary></summary>
         private void Run()
         {
            try
            {
               // delay before starting animating in order to prevent animation for short term server access
               Thread.Sleep(1000);

               // the task on which to animate (i.e. display the server access in the status bar)
               MgStatusBar statusBarToAnimateOn = GetInstance().GetTopmostStatusBar();
               if (statusBarToAnimateOn != null)
               {
                  statusBarToAnimateOn.UpdatePaneContent(ConstInterface.SB_IMG_PANE_LAYER, "@RichClientServerAccess");
                  Commands.beginInvoke();
               }
            }
            catch (Exception)
            {
               //TODO: redesign:
               // 1. RemoteCommandsProcessor shouldn't be responsible for the server access animation, since it doesn't know whether the server is actually accessed.
               // 2. retrieving the topmost statusbar in the lower layers of RemoteCommandsProcessor/HttpManager is ugly and unclear. 
            }
         }
      }
#endif

      /// <summary> When LogClientSequenceForActivityMonitor is Y, the messages written to flow 
      /// monitor will not be send immediately, but they will be written to a log file 
      /// and will be sent to server during next server access. So in case if an execution
      /// fails then the pending logs will be sent when next time client starts.
      /// </summary>
      /// <author>  rajendrap
      /// 
      /// </author>
      private class ClientLogAccumulator
      {
         private const String CLIENT_LOG_HDR_MSG = "Client log file";
         private static readonly List<String> _existingLogNames = new List<String>();
         private StreamWriter _clientLogOs; // Stream for writing to file.
         private Int64 _contextId; // Context Id of current CTX, whose logs are being accumulated
         private bool _failed; // If initialization is failed.
         private String _fileName; // file name

         /// <summary> CTOR</summary>
         /// <param name="contextId">active/target context (irelevant for RC)</param>
         internal ClientLogAccumulator(Int64 contextID)
         {
            _contextId = contextID;
            _fileName = BuildFileName();
            OpenForWrite();
         }

         /// <summary> Write buffer to output stream.</summary>
         /// <param name="buffer"></param>
         /// <throws>  IOException </throws>
         internal void Write(String buffer)
         {
            try
            {
               if (!_failed)
               {
                  if (OpenForWrite())
                  {
                     _clientLogOs.Write(buffer);
                     // Close the file, so the contents will be written to file.
                     _clientLogOs.Close();
                     _clientLogOs = null;

                     // ReOpen so the file so it will be remain locked.
                     OpenForWrite();
                  }
               }
               else
               {
                  throw new IOException("ClientLogSequence : Loging client sequence is deactivated");
               }
            }
            catch (IOException e)
            {
               Logger.Instance.WriteDevToLog(e.Message);
               Misc.WriteStackTrace(e, Console.Error);
            }
         }

         /// <summary> Reads contents from the stream</summary>
         /// <returns></returns>
         /// <throws>  IOException </throws>
         internal String Read()
         {
            String buffer = null;
            try
            {
               // if not failed
               if (!_failed)
               {
                  // We have to read the file. so first close the file which is open in write mode
                  _clientLogOs.Close();
                  _clientLogOs = null;

                  buffer = HandleFiles.readToString(_fileName, Encoding.Default);

                  // Now re-open the file it write mode. This will ensure that
                  // the file can not deleted.
                  OpenForWrite();
               }
               else
               {
                  throw new IOException("ClientLogSequence : Loging client sequence is deactivated");
               }
            }
            catch (IOException e)
            {
               Logger.Instance.WriteDevToLog(e.Message);
               Misc.WriteStackTrace(e, Console.Error);
            }
            return buffer;
         }

         /// <summary> When the logs are sent to server, we should be delete files.</summary>
         /// <throws>  IOException </throws>
         internal void Reset()
         {
            try
            {
               if (!_failed)
               {
                  _clientLogOs.Close();
                  _clientLogOs = null;
                  HandleFiles.deleteFile(_fileName);
               }
               else
               {
                  throw new IOException("ClientLogSequence : Loging client sequence is deactivated");
               }
            }
            catch (IOException e)
            {
               Logger.Instance.WriteExceptionToLog(e);
            }
         }

         /// <summary> If there are any logs that are pending (Failed earlier execution scenario)</summary>
         /// <returns>
         /// </returns>
         internal static bool ExistingLogsFound()
         {
            bool bRet = false;
            // Build list of pending file. it updates array list
            BuildexistingLogList();

            // if there is any file
            if (_existingLogNames != null && _existingLogNames.Count > 0)
            {
               bRet = true;
            }

            return bRet;
         }

         /// <summary> Returns iterator to array list</summary>
         /// <returns></returns>
         internal static IEnumerator ExistingLogIterator()
         {
            return _existingLogNames.GetEnumerator();
         }

         /// <summary> Builds the header line (Client Log file : <Context : XXXXXX>)</summary>
         /// <param name="logName"></param>
         /// <returns></returns>
         internal static String BuildLogHeaderLine(String logName)
         {
            var headerLine = new StringBuilder(CLIENT_LOG_HDR_MSG);
            String context =
            logName.Substring(logName.LastIndexOf(Path.DirectorySeparatorChar + "CS") + 3,
                              (logName.LastIndexOf(".log")) -
                              (logName.LastIndexOf(Path.DirectorySeparatorChar + "CS") + 3));
            headerLine.Append(" (Context : ");
            headerLine.Append(context + " ) ");
            return headerLine.ToString();
         }

         /// <summary> Returns, if accumulator is empty</summary>
         /// <returns></returns>
         internal bool Empty()
         {
            return (_failed || !HandleFiles.isExists(_fileName) || HandleFiles.getFileSize(_fileName) == 0);
         }

         /// <summary> </summary>
         /// <returns></returns>
         internal bool IsFailed()
         {
            return _failed;
         }

         /// <summary> Builds array list of existing file names. from temp folder.</summary>
         private static void BuildexistingLogList()
         {
            if (_existingLogNames != null && _existingLogNames.Count == 0)
            {
               // Take temp folder path
               String strFolder = CacheUtils.GetRootCacheFolderName();
               // get filtered list of files from that folder (CS*.log) 
               _existingLogNames.AddRange(Directory.GetFiles(strFolder, "CS*.LOG"));
            }
         }

         /// <summary> Open the file for writing.</summary>
         private bool OpenForWrite()
         {
            if (_clientLogOs == null)
            {
               try
               {
                  //Open file for writing in append mode.
                  _clientLogOs = new StreamWriter(_fileName, true, Encoding.Default);
                  _failed = false;
               }
               catch (IOException e)
               {
                  // if file creation failed then we will set failed = true
                  // in which case client log sequence will not be active
                  Logger.Instance.WriteDevToLog("ClientLogSequences : " + e.Message);

                  _failed = true;
                  _clientLogOs = null;
                  _contextId = -1;
                  _fileName = null;
               }
            }
            return !_failed;
         }

         /// <summary> </summary>
         /// <returns></returns>
         private String BuildFileName()
         {
            return CacheUtils.GetRootCacheFolderName() + Path.DirectorySeparatorChar + "CS" + _contextId + ".log";
         }
      }
      
      /// <summary>This class implements MgSAXHandlerInterface, which means that it defines all the "callback"
      /// methods that the SAX parser will invoke to notify the application. In this example we override the
      /// methods that we require.</summary>
      private class ErrorMessageXml : MgSAXHandlerInterface
      {
         private readonly RemoteCommandsProcessor _enclosingInstance;

         internal RemoteCommandsProcessor EnclosingInstance
         {
            get { return _enclosingInstance; }
         }

         private String _errorMessage;
         private int _errorCode;
#pragma warning disable 219
         private String _middlewareAddress;
#pragma warning restore 219
         private readonly bool _parsingFailed;

         /// <summary>The main method sets things up for parsing </summary>
         internal ErrorMessageXml(RemoteCommandsProcessor enclosingInstance, string xmlContent)
         {
            _enclosingInstance = enclosingInstance;
            try
            {
               var mgSAXHandler = new MgSAXHandler(this);
               mgSAXHandler.parse(Encoding.UTF8.GetBytes(xmlContent));
            }
            catch (Exception th)
            {
               Logger.Instance.WriteExceptionToLog(th);
               Misc.WriteStackTrace(th, Console.Error);
               _parsingFailed = true;
            }
         }

         // When the parser encounters the end of an element, it calls this method
         public void endElement(String elementName, String elementValue, NameValueCollection attributes)
         {
            switch (elementName)
            {
               case "errmsg":
                  _errorMessage = elementValue;
                  break;
               case "errcode":
                  _errorCode = Int32.Parse(elementValue);
                  break;
               case "server":
                  _middlewareAddress = elementValue;
                  break;
               case "appname": //ignored - the client lists this value from the execution properties
               case "prgname": //ignored - the client lists this value from the execution properties
               case "arguments": //ignored
               case "username":  //ignored
               case "xmlerr": //end of the error message
                  break;
               default:
                  Logger.Instance.WriteExceptionToLog(string.Format("Unknown element: '{0}'", elementName));
                  break;
            }
         }

         // getter 
         internal int GetCode()
         {
            return _errorCode;
         }

         // build & return a formatted error message
         internal String GetMessage()
         {
            var sb = new StringBuilder();
            if (_parsingFailed)
               sb.Append(ClientManager.Instance.getMessageString(MsgInterface.RC_STR_F7_UNEXPECTED_ERR));
            else
            {
               switch (_errorCode)
               {
                  case ServerError.ERR_CTX_NOT_FOUND:
                     if (EnclosingInstance.InactivityTimeoutExpired())
                        sb.Append(string.Format("{0} ({1} {2})",
                                                ClientManager.Instance.getMessageString(
                                                   MsgInterface.STR_ERR_SESSION_CLOSED_INACTIVITY),
                                                ClientManager.Instance.getEnvironment().getContextInactivityTimeout()
                                                / 600, //conversion from 1/10 of seconds to minutes
                                                ClientManager.Instance.getMessageString(MsgInterface.STR_MINUTES)));
                     else
                        sb.Append(string.Format("{0} ({1}).", ClientManager.Instance.getMessageString(MsgInterface.STR_ERR_SESSION_CLOSED), _errorCode));
                     break;

                  case ServerError.ERR_AUTHENTICATION:
                     sb.Append(ClientManager.Instance.getMessageString(MsgInterface.USRINP_STR_BADPASSW));
                     break;

                  case ServerError.ERR_ACCESS_DENIED:
                     sb.Append(ClientManager.Instance.getMessageString(MsgInterface.STR_ERR_AUTHORIZATION_FAILURE));
                     break;

                  default:
                     sb.Append(_errorMessage);
                     break;
               }
               sb.Append(OSEnvironment.EolSeq + OSEnvironment.EolSeq);

               if (!ClientManager.Instance.ShouldDisplayGenericError())
               {
                  sb.Append(ClientManager.Instance.getMessageString(MsgInterface.BROWSER_OPT_INFO_SERVER_STR) + ":\t\t");
                  sb.Append(ClientManager.Instance.getServer());
                  if (!String.IsNullOrEmpty(_middlewareAddress))
                  {
                     sb.Append(" --> ");
                     sb.Append(_middlewareAddress);
                  }
                  sb.Append(OSEnvironment.EolSeq);
               }

               sb.Append(ClientManager.Instance.getMessageString(MsgInterface.TASKRULE_STR_APPLICATION) + ":\t\"");
               sb.Append(ClientManager.Instance.getAppName());
               sb.Append("\" (\"");
               string prgDescription = ClientManager.Instance.getPrgDescription();
               if (string.IsNullOrEmpty(prgDescription))
                  prgDescription = ClientManager.Instance.getPrgName();
               sb.Append(prgDescription);
               sb.Append("\")");
            }

            return sb.ToString();
         }
      }
      // class ErrorMessageXml

      /// <summary>helper class: details from the runtime-engine - environment values, encryption key.
      /// Handshake response: <Richclientresponse> <ContextID>...</ContextID> <Environment InputPassword="Y|N"
      /// SystemLogin="F|N|D|L" /> <EncryptionKey>base64 value</EncryptionKey> </Richclientresponse>
      /// </summary>
      private class HandshakeResponse : MgSAXHandlerInterface
      {
         internal const char SYSTEM_LOGIN_AD = 'D';

         private bool _scrambleMessages = true; // true if messages should be scrambled/unscrambled by the client and server

         private bool _encryptCache     = true; // true if cache files should be encrypted.

         /// <summary>
         /// 
         /// </summary>
         /// <param name="responseXML"></param>
         internal HandshakeResponse(String responseXML)
         {
            //QCR# 712357:Added try catch block, so that the exception thrown by the function 'mgSAXHandler.parse(responseXML)'
            //will be caught here, and will not be propagated further. This will make sure that the object of
            //hand shake response is created.After creation of this object, the html file specified in DefError in mgreq.ini
            //will be shown to the user.
            try
            {
               var mgSAXHandler = new MgSAXHandler(this);
               mgSAXHandler.parse(Encoding.Default.GetBytes(responseXML));
            }
            catch (Exception ex)
            {
               Logger.Instance.WriteExceptionToLog(ex, responseXML);
            }
         }

         internal String ContextId { get; private set; }

         internal bool ScrambleMessages
         {
            get { return _scrambleMessages; }
            private set { _scrambleMessages = value; }
         }

         internal bool EncryptCache
         {
            get { return _encryptCache; }
            private set { _encryptCache = value; }
         }

         internal byte[] EncryptionKey { get; private set; }

         internal bool InputPassword { get; private set; }

         internal uint HttpTimeout { get; private set; }  // seconds

         internal char SystemLogin { get; private set; }

         internal string MaxInternalLogLevel { get; private set; }

         internal String StartupProgramModificationTime { get; private set; } // start up program's modified time. Used to set time of execution.properties_lastOffline

         #region MgSAXHandlerInterface Members

         public void endElement(String elementName, String elementValue, NameValueCollection attributes)
         {
            switch (elementName)
            {
               case "ContextID":
                  ContextId = elementValue;
                  break;

               case "StartProgramModifiedTime":
                  StartupProgramModificationTime = elementValue;
                  break;

               case "Environment":
                  if (attributes["ScrambleMessages"] != null)
                  {
                     Debug.Assert(attributes["ScrambleMessages"] == "N");
                     ScrambleMessages = false;
                  }
                  if (attributes["EncryptCache"] != null)
                  {
                     Debug.Assert(attributes["EncryptCache"] == "N");
                     EncryptCache = false;
                  }
                  if (attributes["MaxInternalLogLevel"] != null)
                     MaxInternalLogLevel = attributes["MaxInternalLogLevel"];
                  if (attributes["MaxInternalLogLevel"] != null)
                     MaxInternalLogLevel = attributes["MaxInternalLogLevel"];
                  if (attributes["InputPassword"].Equals("y", StringComparison.CurrentCultureIgnoreCase))
                     InputPassword = true;
                  if (attributes["SystemLogin"] != null)
                     SystemLogin = attributes["SystemLogin"][0];
                  if (attributes[ConstInterface.MG_TAG_HTTP_COMMUNICATION_TIMEOUT] != null)
                     HttpTimeout = uint.Parse(attributes[ConstInterface.MG_TAG_HTTP_COMMUNICATION_TIMEOUT]);
                  if (attributes["ForwardSlash"] != null)
                     ClientManager.Instance.getEnvironment().ForwardSlashUsage = attributes["ForwardSlash"];
                  break;

               case "EncryptionKey":
                  EncryptionKey = Base64.decodeToByte(elementValue);
                  break;
            }
         }
         #endregion
      }

      /// <summary>
      /// Subscribe for events defined at HttpClientEvents and attach appropriate event handlers
      /// </summary>
      /// <returns></returns>
      internal void RegisterDelegates()
      {
         com.magicsoftware.httpclient.HttpClientEvents.GetSessionCounter_Event += GetSessionCounter;
         com.magicsoftware.httpclient.HttpClientEvents.CheckAndSetSessionCounter_Event += CheckAndSetSessionCounter;
      }

#if PocketPC
      private bool _isHibernated = false; // Is context in hibernation state

      //---------------------------------------------------------------------------
      // Topic #26 (MAGIC version 1.9 for WIN) RC mobile: Application Minimize
      //---------------------------------------------------------------------------

      /// <summary>hibernate the context of the current client:
      /// the method sends EXTERNAL_EVENT::HIBERNATE to the server.
      /// the server will control the context according to the ContextUnloadTimeout keyword.</summary>
      internal void Hibernate()
      {
         _isHibernated = true;
         IClientCommand cmd = CommandFactory.CreateHibernateCommand();
         MGDataCollection.Instance.getMGData(0).CmdsToServer.Add(cmd);
         Execute(CommandsProcessorBase.SendingInstruction.ONLY_COMMANDS);
      }

      /// <summary>Resume a hibernated context. If the context can not be resumed, throw an error that will
      /// tell the Clientmanager to respawn a new procee and exit.
      /// Context resumeability is checked 1st by checking if the unload timeout has passed. if not - ask the
      /// server to resume the context.
      /// </summary>
      internal void Resume()
      {
         bool expired = false;

         // Did the unload timeout elapse?
         if (ClientManager.Instance.getEnvironment().getContextUnloadTimeout() > 0)
         {
            long CurrTimeMilli = Misc.getSystemMilliseconds();
            expired = (CurrTimeMilli - _lastRequestTime >
                       ClientManager.Instance.getEnvironment().getContextUnloadTimeout() * 100);
         }

         // So far we are ok, try to resume the context
         if (!expired)
         {
            // send EXTERNAL_EVENT::RESUME to the server.
            // the server will revive the context (to the state before the UNLOAD)
            IClientCommand cmd = CommandFactory.CreateResumeCommand();
            MGDataCollection.Instance.getMGData(0).CmdsToServer.Add(cmd);
            try
            {
               Execute(SendingInstruction.ONLY_COMMANDS);
               _isHibernated = false;
            }
            catch (ServerError e)
            {
               // If context deleted already, just exit and launch a new process
               if (e.GetCode() == ServerError.ERR_CTX_NOT_FOUND)
                  expired = true;
               else
                  throw;
            }
         }

         // if context expired, throw the error that will cause the spawn of a new process
         if (expired)
            throw new InternalError("ctx resume failed");
      }

      /// <summary> This class is used to mark an error that will cause the quiet spawn of a new process,
      /// used for when a hibernated context expires
      /// </summary>
      internal class InternalError : ServerError
      {
         internal InternalError(String msg)
            : base(msg)
         {
         }
      }
#endif

   }
}
