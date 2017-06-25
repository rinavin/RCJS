using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.richclient.local.data;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.local.data.gateways;
using com.magicsoftware.richclient.commands;
using com.magicsoftware.richclient.commands.ClientToServer;

namespace com.magicsoftware.richclient.remote
{
   /// <summary>
   /// remote dataview manager:
   /// will pass requests to the server
   /// when reply is received will perform fillData to the dataview.xml
   /// Note: in next phase will implement more complicated tasks
   /// </summary>
   class RemoteDataviewManager : DataviewManagerBase
   {
      private RemoteDataViewCommandFactory _remoteDataViewCommandFactory;

      /// <summary>
      /// CTOR
      /// </summary>
      /// <param name="task"></param>
      internal RemoteDataviewManager(Task task) 
         : base (task)
      {
         _remoteDataViewCommandFactory = new RemoteDataViewCommandFactory();
      }

      /// <summary>
      /// execute the command by pass requests to the server 
      /// </summary>
      /// <param name="command"></param>
      internal override ReturnResult Execute(IClientCommand command)
      {
         base.Execute(command);

         RemoteDataViewCommandBase remoteDataViewCommandBase = _remoteDataViewCommandFactory.CreateDataViewCommand((ClientOriginatedCommand)command);
         ReturnResultBase gatewayResult = remoteDataViewCommandBase.Execute();

         return new ReturnResult(gatewayResult);
      }      
   }
}