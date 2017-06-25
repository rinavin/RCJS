using System;
using System.Diagnostics;
using com.magicsoftware.richclient.remote;
using com.magicsoftware.richclient.local;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.unipaas.management.tasks;
using com.magicsoftware.richclient.rt;
using com.magicsoftware.util;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.http;
using com.magicsoftware.httpclient;
using com.magicsoftware.richclient.communications;
using com.magicsoftware.richclient.sources;
using util.com.magicsoftware.util;

namespace com.magicsoftware.richclient
{
   /// <summary>
   /// manage the commands processor used, for non-task uses
   /// </summary>
   internal class CommandsProcessorManager
   {
      /// <summary>Represents the status of the session, in regard to the server.</summary>
      internal enum SessionStatusEnum
      {
         Uninitialized, // no attempt was yet done to the server.
         Remote,        // ==> the session was established with the server, and a context was created.
         Local          // ==> there is no context in the server.
      }

      private static SessionStatusEnum _sessionStatus;
      internal static SessionStatusEnum SessionStatus
      { 
         get
         {
            return _sessionStatus;
         }

         set 
         {
            _sessionStatus = value;
         }
      } 

      static CommandsProcessorManager()
      {
         SessionStatus = SessionStatusEnum.Uninitialized;
      }

      /// <summary>
      /// get the commands processor according to the session status.
      /// </summary>
      /// <returns></returns>
      internal static CommandsProcessorBase GetCommandsProcessor()
      {
         if (SessionStatus == SessionStatusEnum.Local)
         {
            ClientManager.Instance.SetService(typeof(TaskServiceBase), new LocalTaskService());
            return (CommandsProcessorBase)LocalCommandsProcessor.GetInstance();
         }

         return (CommandsProcessorBase)RemoteCommandsProcessor.GetInstance();
      }

      /// <summary>
      /// get the URL content according to the active commands processor 
      /// </summary>
      /// <param name="requestedURL"></param>
      /// <param name="decryptResponse"></param>
      /// <returns></returns>
      internal static byte[] GetContent (string requestedURL, bool decryptResponse)
      {
         return GetCommandsProcessor().GetContent(requestedURL, decryptResponse);
      }

      /// <summary>
      /// get the CommandsProcessor to use for executing the defined task
      /// </summary>
      /// <param name="taskDefinitionId"></param>
      /// <returns></returns>
      internal static CommandsProcessorBase GetCommandsProcessor (TaskDefinitionId taskDefinitionId)
      {
         if (ClientManager.Instance.LocalManager.ApplicationDefinitions.TaskDefinitionIdsManager.IsOfflineTask(taskDefinitionId))
            return LocalCommandsProcessor.GetInstance();
         else
            return RemoteCommandsProcessor.GetInstance();
      }

      /// <summary>
      /// sets the commands processor that should serve an operation - depending on the called task and not on the calling task
      /// </summary>
      /// <param name="operation"></param>
      /// <param name="task"></param>
      internal static CommandsProcessorBase GetCommandsProcessor(Operation operation)
      {
         Debug.Assert(operation != null);

         CommandsProcessorBase commandsProcessor;

         // if executing a program or subtask call operation, we need the commands processor to match the called task
         if (operation.IsLocalCall)
            commandsProcessor = CommandsProcessorManager.GetCommandsProcessor(operation.CalledTaskDefinitionId);
         else
            //for all server side operations other than call main program must use remote commands processor
            commandsProcessor = GetCommandsProcessor(operation.Task);
         return commandsProcessor;
      }

      /// <summary>
      /// get command processor for server side operation
      /// </summary>
      /// <param name="task"></param>
      /// <returns></returns>
      internal static CommandsProcessorBase GetCommandsProcessor(Task task)
      {
         // for server operation main program must use Remote command processor, otherwise - default command processor must be used
         CommandsProcessorBase commandsProcessor = task.isMainProg() ? RemoteCommandsProcessor.GetInstance() : task.CommandsProcessor;
         return commandsProcessor;
      }


      /// <summary>
      /// Start session (Remote/Local)
      /// </summary>
      /// <returns></returns>
      internal static bool StartSession()
      {
         bool succeeded = false;
         bool startInConnectedModeToSyncMetadata = false;
         bool skipConnectionToServer = false;

         using (Logger.Instance.AccumulateMessages())
         {
            // Check if client is required to start in disconnected mode even when network is available
            // Skip connection if,
            //   1. ConnectOnStartup = N in execution properties
            //   2. LastOffline does not contain "UnsyncronizedMetadata=Y" [which means connect server to sync metadata].
            if (LocalCommandsProcessor.GetInstance().CanStartWithoutNetwork && !ClientManager.Instance.GetConnectOnStartup())
            {
               LocalCommandsProcessor.GetInstance().VerifyLastOfflineInitialResponseFile();
               if (LocalCommandsProcessor.GetInstance().CanStartWithoutNetwork)
               {
                  if (LocalCommandsProcessor.GetInstance().IsUnsyncronizedMetadata())
                     startInConnectedModeToSyncMetadata = true;
                  else
                     skipConnectionToServer = true;
               }
            }

            // Create a communications failure handler and pass it to the Http connection layer.
            HttpManager.GetInstance().SetCommunicationsFailureHandler(
                  LocalCommandsProcessor.GetInstance().CanStartWithoutNetwork
                           ? (ICommunicationsFailureHandler)(new SilentCommunicationsFailureHandler())
                           : (ICommunicationsFailureHandler)(new InteractiveCommunicationsFailureHandler()));

            if (skipConnectionToServer)
               succeeded = StartSessionLocally();
            else
            {
               try
               {
                  ApplicationSourcesManager.GetInstance().OfflineRequiredMetadataCollection.Enabled = true;

                  // access the Server: if accessed - continue remotely, otherwise - locally.
                  succeeded = RemoteCommandsProcessor.GetInstance().StartSession();

                  // Notify the Connection State Manager that a successful connection was established.
                  // The first time this happens, the connection state manager will move from 'Unknown' to 'Connected'.
                  ClientManager.Instance.GetService<IConnectionStateManager>().ConnectionEstablished();
               }
               catch (Exception ex)
               {
                  if (ex is InvalidSourcesException) throw;

                  // If client started in connected mode (even when ConnectOnStartup=N in execution properties) because,
                  // UnsyncronisedMetadata was occurred in last execution, and if any sever error occurred during initialization then,
                  // ServerLastAccessStatus() should return 1 (UnsynchroizedMetadata). This way client will avoid trying any server operation.
                  if (startInConnectedModeToSyncMetadata)
                     RemoteCommandsProcessor.GetInstance().ServerLastAccessStatus = ServerAccessStatus.UnsynchroizedMetadata;

                  Logger.Instance.WriteServerToLog("Failed connecting to the server: " + ex.Message);
                  if (LocalCommandsProcessor.GetInstance().CanStartWithoutNetwork)
                  {
                     if (Logger.Instance.ShouldLog())
                        Logger.Instance.FlushAccumulatedMessages();
                     else
                        Logger.Instance.DiscardAccumulatedMessages();
                     Logger.Instance.StopMessageAccumulation();
                     Logger.Instance.WriteServerToLog("Attempting to start locally (Offline mode)");
                     succeeded = LocalCommandsProcessor.GetInstance().StartSession();
                  }

                  if (!succeeded)
                  {
                     Logger.Instance.WriteServerToLog("Cannot start locally. Terminating client.");
                     throw;
                  }
               }
               finally
               {
                  if (Logger.Instance.IsAccumulatingMessages())
                     Logger.Instance.FlushAccumulatedMessages();

                  // all offline required cache files are retrieved when the server is started, in the initial response after the two handshake requests.
                  // therefore, no cache files should be collected once the session is started [i.e. StartSession() was executed].
                  ApplicationSourcesManager.GetInstance().OfflineRequiredMetadataCollection.Enabled = false;
               }
            }
         }

         // if a startupProgram should be started locally then load it locally.
         if (GetCommandsProcessor().ShouldLoadOfflineStartupProgram())
            LocalCommandsProcessor.GetInstance().LoadStartupProgram();

         // From this point onward, all comm failures should show a message.
         HttpManager.GetInstance().SetCommunicationsFailureHandler(new InteractiveCommunicationsFailureHandler());

         return succeeded;
      }

      /// <summary>
      /// Starts session locally without trying to connect to server.
      /// </summary>
      private static bool StartSessionLocally()
      {
         bool succeeded = false;

         // Enable offline required metadata collection which is needed to verify client cache,
         // during connecting to server (switching to connected mode for first server operation)
         ApplicationSourcesManager.GetInstance().OfflineRequiredMetadataCollection.Enabled = true;

         // switch session status from "Uninitialized" to "Local"
         ClientManager.Instance.GetService<IConnectionStateManager>().ConnectionDropped();

         Logger.Instance.WriteServerToLog("Attempting to start locally (Offline mode)");
         succeeded = LocalCommandsProcessor.GetInstance().StartSession();
         if (succeeded)
         {
            // Set server last access status
            RemoteCommandsProcessor.GetInstance().ServerLastAccessStatus = ServerAccessStatus.SkippedConnectionToServer;
         }
         else
            Logger.Instance.WriteServerToLog("Cannot start locally. Terminating client.");

         if (Logger.Instance.IsAccumulatingMessages())
            Logger.Instance.FlushAccumulatedMessages();

         // No cache files should be collected once the session is started.
         ApplicationSourcesManager.GetInstance().OfflineRequiredMetadataCollection.Enabled = false;

         return succeeded;
      } 
   }
}
