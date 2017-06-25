using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.commands;
using com.magicsoftware.richclient.commands.ClientToServer;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.local.data.gateways.commands;
using util.com.magicsoftware.util;
using com.magicsoftware.util;
using com.magicsoftware.richclient.local.data.gateways;
using com.magicsoftware.gatewaytypes;

namespace com.magicsoftware.richclient.local.data.commands
{
   /// <summary>
   /// LocalClientDbDisconnectCommand
   /// </summary>
   class LocalClientDbDisconnectCommand : LocalDataViewCommandBase
   {
      private string dataBaseName;
      private ClientDbDisconnectCommand clientDbDisconnectCommand;
      /// <summary>
      /// Contructor
      /// </summary>
      /// <param name="command"></param>
      public LocalClientDbDisconnectCommand(ClientDbDisconnectCommand command)
         : base(command)
      {
         dataBaseName = ((ClientDbDisconnectCommand)command).DataBaseName;
         clientDbDisconnectCommand = command;
      }

      /// <summary>
      /// Execute DbDisConnect gateway command.
      /// </summary>
      /// <returns></returns>
      internal override ReturnResultBase Execute()
      {
         GatewayResult result = new GatewayResult();
         GatewayCommandDbDisconnect dbDisconnectCommand = GatewayCommandsFactory.CreateGatewayCommandDbDisconnect(LocalDataviewManager.LocalManager, dataBaseName);
         result = dbDisconnectCommand.Execute();

         return result;
      }

   }
}
