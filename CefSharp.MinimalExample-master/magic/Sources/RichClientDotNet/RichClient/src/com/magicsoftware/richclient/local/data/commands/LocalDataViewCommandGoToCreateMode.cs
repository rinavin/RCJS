using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.util;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.local.data.gateways;
using com.magicsoftware.richclient.commands;
using com.magicsoftware.richclient.commands.ClientToServer;

namespace com.magicsoftware.richclient.local.data.commands
{
   class LocalDataViewCommandGoToCreateMode : LocalDataViewCommandBase
   {
      public LocalDataViewCommandGoToCreateMode(EventCommand command)
         : base(command)
      {

      }
      internal override ReturnResultBase Execute()
      {
         ReturnResultBase result = new ReturnResult();
         // QCR #301538. Only when GotoCreateMode execute Invalidate.
         DataviewSynchronizer.Invalidate();
         result = SetViewForCreateMode();
         return result;
      }
   }
}
