using com.magicsoftware.richclient.local.data.commands;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.commands;
using com.magicsoftware.richclient.commands.ClientToServer;
using System.Diagnostics;

namespace com.magicsoftware.richclient.remote
{ 
   /// <summary>
   /// remote dataview command
   /// </summary>

   internal class RemoteDataViewCommandBase : DataViewCommandBase
   {
      internal ClientOriginatedCommand Command { get; private set; }
      protected Task Task { get; set;}

      /// <summary>
      /// 
      /// </summary>
      /// <param name="command"></param>
      internal RemoteDataViewCommandBase(ClientOriginatedCommand command)
      {
         this.Command = command;
         if(command is ICommandTaskTag)
            Task = (Task)MGDataCollection.Instance.GetTaskByID(((ICommandTaskTag)command).TaskTag);
      }


      /// <summary>
      /// execute the command by pass requests to the server 
      /// </summary>
      /// <param name="command"></param>
      internal override ReturnResultBase Execute()
      {
         CommandsTable cmdsToServer = Task.getMGData().CmdsToServer;
         cmdsToServer.Add(Command);
         RemoteCommandsProcessor.GetInstance().Execute(CommandsProcessorBase.SendingInstruction.TASKS_AND_COMMANDS);

         return new ReturnResult();
      } 
   }
}
