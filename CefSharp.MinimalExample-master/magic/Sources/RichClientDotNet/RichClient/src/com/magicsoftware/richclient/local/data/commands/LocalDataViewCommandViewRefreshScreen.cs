
using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.local.data.gateways;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.commands;

namespace com.magicsoftware.richclient.local.data.commands
{
   /// <summary>
   /// Refresh screen
   /// </summary>
   class LocalDataViewCommandViewRefreshScreen : LocalDataViewCommandBase
   {
      public LocalDataViewCommandViewRefreshScreen(IClientCommand command)
         : base(command)
      {

      }

      /// <summary>
      /// NOT IMLEMENTED !!!
      /// </summary>
      /// <returns></returns>
      internal override ReturnResultBase Execute()
      {
         GatewayResult result = new GatewayResult();
         //TODO: Implement
         return result;
      }
   }
}

