using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.local.data.gateways;
using com.magicsoftware.richclient.commands;
using com.magicsoftware.richclient.commands.ClientToServer;

namespace com.magicsoftware.richclient.remote
{

   /// <summary>
   /// remote command : dummy for now
   /// </summary>
   internal class DummyDataViewCommand : RemoteDataViewCommandBase
   {
      internal DummyDataViewCommand(ClientOriginatedCommand command)
         : base(command)
      {
      }


      /// <summary>
      /// !!
      /// </summary>
      /// <param name="command"></param>
      internal override ReturnResultBase Execute()
      {
         return new GatewayResult(); ;
      }
   } 
}
