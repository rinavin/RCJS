using com.magicsoftware.richclient.commands;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.local.data.gateways;

namespace com.magicsoftware.richclient.local.data.commands
{
   /// <summary>
   /// DataView Command for Loacte Next operation.
   /// </summary>
   class LocalDataviewCommandLocateNext : LocalDataViewCommandFetchFirstChunk
   {
      IClientCommand Command { get; set; }

      /// <summary>
      /// Constructor
      /// </summary>
      /// <param name="command"></param>
      public LocalDataviewCommandLocateNext(IClientCommand command)
         : base(command)
      {
         UseFirstRecord = false;
      }

      /// <summary>
      /// Execute
      /// </summary>
      /// <returns></returns>
      internal override ReturnResultBase Execute()
      {
         ReturnResultBase result = new GatewayResult();

         calculateNextStartPosition();

         //If Next Start position found then fetch the records from that start posotion.
         if (startPosition != null)
         {
            result = base.Execute();
         }

         return result;
      }

      /// <summary>
      /// Calculate start position for Locate Next operation.
      /// </summary>
      private void  calculateNextStartPosition()
      {
         LocateNextLocalDataCommand locateNextCommand = new LocateNextLocalDataCommand(Command);
         locateNextCommand.DataviewManager = DataviewManager;
         locateNextCommand.LocalManager = LocalManager;
         ReturnResultBase result = locateNextCommand.Execute();
         if (PositionCache.Count > 0)
         {
            LocalDataviewManager.LocalErrorHandlerManager.HandleResult(result);
         }

         startPosition = locateNextCommand.ResultStartPosition;
      }
   }
}
