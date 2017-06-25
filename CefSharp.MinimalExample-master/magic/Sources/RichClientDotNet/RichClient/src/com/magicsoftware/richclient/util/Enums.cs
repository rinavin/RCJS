using System;
namespace com.magicsoftware.richclient.util
{
   internal enum EventSubType
   {
      // InternalInterface.MG_ACT_CANCEL in runtime event
      Normal = 0,

      CancelWithNoRollback = 1,
      CancelIsQuit = 2,

      //InternalInterface.MG_ACT_RT_REFRESH_VIEW in runtime event
      RtRefreshViewUseCurrentRow = 3,
      
      //InternalInterface.MG_ACT_EXIT in runtime event
      ExitDueToError = 4


   }      


   /// <summary> An application will have 4 different execution stages.
   /// </summary>
   internal enum ApplicationExecutionStage
   {
      //This covers the execution till the program starts to execute i.e. handshake, initial request and downloading metadata.
      Initializing,
      //An application enters this stage just before starting to execute the program. 
      Executing,
      //An application enters this stage when trying to send unload CTX command to the server. 
      Terminating,
      //An application will be in this stage when it trying to send logs to the server.
      HandlingLogs
   }

   /// <summary> Indicates the server access status </summary>
   internal enum ServerAccessStatus
   {
      Success = 0,               //0 - Success
      UnsynchroizedMetadata,     //1 - Metadata files are not synchronized
      InaccessibleWebServer,     //2 - The WebServer is unavailable (either on startup or because the user pressed ‘No’ in the retry dialog)
      RequestNotServedByServer,  //3 - The server could not serve the request (upon server errors, such as license error, etc.) 
      UnavailableContext,        //4 - The context is no longer available
      InvalidDeploymentMode,     //5 - The studio cannot serve the request (when the studio is opened in deployment mode = Online and the application was started in offline)
      SkippedConnectionToServer, //6 - The client has not connected with server due to execution property ConnectOnStartup = N. 
   }
}
