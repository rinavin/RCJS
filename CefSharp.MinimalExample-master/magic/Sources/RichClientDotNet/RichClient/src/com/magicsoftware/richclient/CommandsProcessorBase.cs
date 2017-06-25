using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using com.magicsoftware.richclient.exp;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.richclient.util;
using com.magicsoftware.unipaas.util;
using com.magicsoftware.util;
using com.magicsoftware.richclient.rt;
using com.magicsoftware.unipaas.management.tasks;
using util.com.magicsoftware.util;
using com.magicsoftware.util.Xml;

#if PocketPC
using OSEnvironment = com.magicsoftware.richclient.mobile.util.OSEnvironment;
using ExternalAccessAnimator = com.magicsoftware.richclient.mobile.util.ExternalAccessAnimator;
#endif

namespace com.magicsoftware.richclient
{
   /// <summary> this class is responsible for the communication with the server and handling the server messages</summary>
   internal abstract class CommandsProcessorBase
   {
      protected long _sessionCounter; // the session counter that should be used on the next access to the server.

      static protected TaskDefinitionId startupProgram;

      // The stage in the session this execution belongs to
      internal enum SessionStage
      {
         HANDSHAKE = 1, // handshake calls
         INITIAL   = 2, // call to the initial program
         NORMAL    = 3  // subsequent calls after the initial call
      }

      // instruction what to send during execution
      internal enum SendingInstruction
      {
         NO_TASKS_OR_COMMANDS, // don't send tasks or commands.
         ONLY_COMMANDS,        // send only commands.
         TASKS_AND_COMMANDS    // send tasks and commands.
      }
 
      /// <summary>
      /// Retrieve a file from the server to the client's default folder.
      /// In case the file wasn't modified since last retrieved, it will not be downloaded from the server 
      ///   and will not be copied to the client's default folder.
      /// </summary>
      /// <param name="serverFileName">file name, as known in the server's file system.</param>
      /// <param name="currTask">the current executing task.</param>
      /// <returns>true if the file was found on the client's default folder.</returns>
      internal bool CopyToDefaultFolder(string serverFileName, Task currTask)
      {
         Debug.Assert(currTask != null);

         string clientFileName = (serverFileName.Substring(serverFileName.LastIndexOf('\\') + 1));
         string cachedFileName = GetLocalFileName(serverFileName, currTask, false);

         if (!String.IsNullOrEmpty(cachedFileName))
         {
            // if the file was modified since last retrieved, copy it from the client's cache to the client's default folder
            if (HandleFiles.getFileTime(cachedFileName) != HandleFiles.getFileTime(clientFileName))
               HandleFiles.copy(cachedFileName, clientFileName, true, false);
         }

         return HandleFiles.isExists(clientFileName);
      }

      /// <summary>copy to the local (Client-side) file system a given file name in the Server-side and return its name.</summary>
      /// <param name="serverFilename">a file name in the Server's file system.</param>
      /// <param name="task">task</param>
      /// <param name="refreshClientCopy">Even if client copy is present, query server for URL of cached file to get the 
      ///   modified remotetime as part of URL. After accessing URL having modified remotetime, the client copy will get refreshed 
      ///   with that on server.</param>
      /// <returns>file name in the local file system.</returns>
      internal abstract String GetLocalFileName(String serverFilename, Task task, bool refreshClientCopy);

      /// <summary>
      /// </summary>
      internal long GetSessionCounter()
      {
         return _sessionCounter;
      }

      /// <summary>
      /// start the session - e.g. exchange handshake requests with the Server, if connected.
      /// </summary>
      /// <returns>true only if the Client should continue to start the actual requested program.</returns>
      internal abstract bool StartSession();

      /// <summary>build the xml request; send it to the runtime-engine</summary>
      /// <param name="sendingInstruction">instruction what to send during execution - NO_TASKS_OR_COMMANDS / ONLY_COMMANDS / TASKS_AND_COMMANDS.</param>
      internal void Execute(SendingInstruction sendingInstruction)
      {
         Execute(sendingInstruction, SessionStage.NORMAL, null);
      }

      /// <summary>build the xml request; send it to the runtime-engine; receive a response</summary>
      /// <param name="sendingInstruction">instruction what to send during execution - NO_TASKS_OR_COMMANDS / ONLY_COMMANDS / TASKS_AND_COMMANDS.</param>
      /// <param name="sessionStage">HANDSHAKE / INITIAL / NORMAL.</param>
      /// <param name="res">result ot be  read after parsing the response from the server.</param>
      internal abstract void Execute(SendingInstruction sendingInstruction, SessionStage sessionStage, IResultValue res);

      /// <summary> Invoke the request URL & return the response.</summary>
      /// <param name="requestedURL">URL to be accessed.</param>
      /// <param name="decryptResponse">if true, fresh responses from the server will be decrypted using the 'encryptionKey' passed to 'HttpManager.SetProperties'.</param>
      /// <returns>response (from the server).</returns>
      internal abstract byte[] GetContent(String requestedURL, bool decryptResponse);

      /// <summary> Upload contents to a file on server </summary>
      /// <param name="serverFileName">filename to be saved on the server.</param>
      /// <param name="fileContent">file content to be sent to server.</param>
      /// <param name="contentType">type of the content to be sent to server, e.g application/octet-stream.</param>
      /// <returns></returns>
      internal abstract byte[] UploadFileToServer(String serverFileName, byte[] fileContent, String contentType);

      /// <summary>send Monitor messaging to the server</summary>
      internal abstract void SendMonitorOnly();

      /// <summary> unscramble servers response</summary>
      /// <returns>unscrambled content</returns>
      protected static String UnScramble(String respBuf)
      {
         int openTagLocation = respBuf.IndexOf(XMLConstants.MG_TAG_OPEN);
         string core;

         if (openTagLocation != -1)
         {
            int start = openTagLocation + XMLConstants.MG_TAG_OPEN.Length;
            string openTag = respBuf.Substring(0, start);

            int finish = respBuf.LastIndexOf(XMLConstants.TAG_OPEN);
            string closeTag = respBuf.Substring(finish);

            core = openTag + Scrambler.UnScramble(respBuf, start, --finish) + closeTag;
         }
         // We got a scrambled error message, there is no open tag 
         else
            core = Scrambler.UnScramble(respBuf, 0, respBuf.Length - 1);

         return core;
      
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      internal virtual ArgumentsList BuildArgList()
      {
         return null;
      }

      /// <summary>
      /// Parse the startup sequence element (<see cref="ConstInterface.MG_TAG_STARTUP_PROGRAM"/>) which is
      /// expected to be the current element of the passed parser.
      /// </summary>
      /// <param name="parser">The parser whose current element is the startup sequence element to be parsed.</param>
      internal void ParseStartupProgram(XmlParser parser)
      {
         // Read the contents of the Startup Sequence element.
         string serializedStartupSequence = parser.ReadToEndOfCurrentElement();

         // Convert the serialized information to byte[] for xml parsing.
         byte[] bytes = Encoding.UTF8.GetBytes(serializedStartupSequence);

         // Use a sax parser to parse each task task definition ID in the startup sequence block.
         TaskDefinitionIdTableSaxHandler handler = new TaskDefinitionIdTableSaxHandler(HandleTaskDefinitionId);
         handler.parse(bytes);
      }

      /// <summary>
      /// Delegate method for the startup sequence sax parser see: <see cref="ParseStartupProgram"/> above.
      /// </summary>
      /// <param name="taskId">The task identifier created by the sax handler.</param>
      /// <param name="taskUrl">Ignored</param>
      /// <param name="defaultTags">Ignored</param>
      void HandleTaskDefinitionId(TaskDefinitionId taskId)
      {
         startupProgram = taskId;
      }

      /// <summary>
      /// Does this command processor allow execution of programs as parallel programs
      /// </summary>
      /// <returns></returns>
      internal virtual bool AllowParallel()
      {
         return !ClientManager.IsMobile;
      }

      /// <summary>Determine whether the startup program should be run localy or not.</summary>
      /// <returns>The method returns <code>true</code> if the statupProgram should be run locally.</returns>
      internal bool ShouldLoadOfflineStartupProgram()
      {
         bool shouldLoadOffline = false;

         if (startupProgram != null && 
            ClientManager.Instance.LocalManager.ApplicationDefinitions.TaskDefinitionIdsManager.IsOfflineTask(startupProgram))
               shouldLoadOffline = true;

         return shouldLoadOffline;
      }
   }
}
