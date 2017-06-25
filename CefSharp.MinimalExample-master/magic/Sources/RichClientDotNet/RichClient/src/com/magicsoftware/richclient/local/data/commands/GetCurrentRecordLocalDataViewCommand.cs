using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.local.data.gateways;
using com.magicsoftware.richclient.util;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.richclient.local.data.view;
using com.magicsoftware.richclient.commands.ClientToServer;

namespace com.magicsoftware.richclient.local.data.commands
{
   /// <summary>
   /// Refresh record
   /// </summary>
   class GetCurrentRecordLocalDataViewCommand : LocalDataViewCommandBase
   {    
      /// <summary>
      /// CTOR
      /// </summary>
      /// <param name="command"></param>
      public GetCurrentRecordLocalDataViewCommand(EventCommand command)
         : base(command)
      {
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      internal override ReturnResultBase Execute()
      {
         IRecord record = DataviewSynchronizer.GetCurrentRecord();

         GatewayResult result = TaskViews.GetPrimaryView().FetchCurrent(record);

         if (result.Success)
            TaskViews.RecordComputer.Compute(record, false, false, false);

         return result;
      }
   }
}

