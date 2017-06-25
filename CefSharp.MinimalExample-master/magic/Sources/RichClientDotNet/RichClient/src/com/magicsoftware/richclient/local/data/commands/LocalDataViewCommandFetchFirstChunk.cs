using com.magicsoftware.util;
using com.magicsoftware.richclient.local.data.gateways;
using com.magicsoftware.gatewaytypes;
using com.magicsoftware.richclient.local.data.view;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.commands;
using System;

namespace com.magicsoftware.richclient.local.data.commands
{
   class LocalDataViewCommandFetchFirstChunk : LocalDataViewCommandFetchTopChunk
   {
      /// <summary>
      /// true if preload view task property is true
      /// </summary>
      bool IsPreloadView { get; set; }
      IClientCommand Command { get; set; }


      public LocalDataViewCommandFetchFirstChunk(IClientCommand command)
         : base(command)
      {
         UseFirstRecord = true;
         Command = command;
      }
      /// <summary>
      /// 
      /// </summary>
      protected override DbPos StartPosition
      {
         get
         {
            if (startPosition == null)
            {
               startPosition = base.StartPosition;
            }

            return startPosition;
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      internal override ReturnResultBase Execute()
      {
         ReturnResultBase result = new GatewayResult();
         if (Task.getMode() == Constants.TASK_MODE_CREATE)
         {
            // QCR's #430059 & #310574. Execute GotoCreateMode command.
            result = SetViewForCreateMode();
         }
         else if (Task.HasFields)
         {
            if (startPosition == null)
               CalculateStartPositionFromLocateExpression();

            IsPreloadView = Task.IsPreloadView;
            result = base.Execute();
         }

         DataviewSynchronizer.ResetFirstDv();
         return result;
      }

      /// <summary>
      /// initialize the start position by creating and executing a fetch locate expression command
      /// </summary>
      private void CalculateStartPositionFromLocateExpression()
      {
         LocalDataViewCommandLocate localDataViewCommandLocate = new LocalDataViewCommandLocate(Command);
         localDataViewCommandLocate.DataviewManager = DataviewManager;
         localDataViewCommandLocate.LocalManager = LocalManager;

         LocalDataviewManager.LocalErrorHandlerManager.HandleResult(localDataViewCommandLocate.Execute());
         
         startPosition = localDataViewCommandLocate.ResultStartPosition;

         //User locates should be used only while calculating startposition after view refresh. So after that reset this flag.
         DataviewSynchronizer.DataviewManager.TaskViews.UseUserLocates = false;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="recordNum"></param>
      /// <returns></returns>
      protected override bool StopFetches(int recordNum)
      {
         return IsPreloadView ? AllRecordsFetched(recordNum) : base.StopFetches(recordNum);
      }

      /// <summary>
      /// all records are fetched
      /// </summary>
      /// <param name="currentRecordNum"></param>
      /// <returns></returns>
      bool AllRecordsFetched(int currentRecordNum)
      {
         return IsEndOfView;
      }

      /// <summary>
      /// update top index on the form
      /// </summary>
      /// <returns></returns>
      protected override ReturnResultBase UpdateAfterFetch()
      {
         
         ReturnResultBase result =  base.UpdateAfterFetch();
         if (result.Success)
            DataviewSynchronizer.UpdateFormTopIndex();
         return result;
      }

   }


}
