using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.local.data.gateways;
using com.magicsoftware.richclient.commands.ClientToServer;
using com.magicsoftware.gatewaytypes;

namespace com.magicsoftware.richclient.local.data.commands
{
   class LocalDataViewCommandExecuteLocalUpdates : LocalDataViewCommandBase
   {
      public LocalDataViewCommandExecuteLocalUpdates(DataviewCommand command)
         : base(command)
      {

      }
      internal override ReturnResultBase Execute()
      {
         GatewayResult result = new GatewayResult();
         IRecord modifiedRecord = DataviewSynchronizer.GetModifiedRecord();
         DataviewSynchronizer.PrepareForModification();

         if (modifiedRecord != null)
         {
            result = TaskViews.ApplyModifications(modifiedRecord);
            DataviewSynchronizer.UpdateDataviewAfterModification(result.Success);
         }
         return result;
        
      }
   }
}
