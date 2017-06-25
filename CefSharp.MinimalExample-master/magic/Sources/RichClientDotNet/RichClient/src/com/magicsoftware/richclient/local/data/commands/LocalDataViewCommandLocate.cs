using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.local.data.gateways;
using com.magicsoftware.gatewaytypes;
using com.magicsoftware.richclient.local.data.view;
using com.magicsoftware.richclient.local.data.view.RecordCompute;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.commands;
using com.magicsoftware.richclient.commands.ClientToServer;
using com.magicsoftware.util;
using com.magicsoftware.unipaas.management.gui;

namespace com.magicsoftware.richclient.local.data.commands
{
   /// <summary>
   /// special command to calculate the start position not based on values from the database
   /// </summary>
   class LocalDataViewCommandLocate : LocalDataViewCommandLocateBase
   {

      protected override RecordComputer RecordComputer 
      { 
         get 
         {
            if (TaskViews.UseUserLocates)
            {
               return TaskViews.RecordComputer;
            }
            else
            {
               return TaskViews.LocateExpressionRecordComputer;
            }
         } 
      }

      bool startPosFound = true;
      /// <summary>
      /// 
      /// </summary>
      protected override DbPos StartPosition
      {
         get
         {
            // If there's a "locate" value, it should be used to get a startup position
            if (startPosition == null)
            {
               if (TaskViews.ViewMain != null
                  && (TaskViews.ViewMain.HasLocate || TaskViews.VirtualView.HasLocate || Task.checkIfExistProp(PropInterface.PROP_TYPE_TASK_PROPERTIES_LOCATE)) 
                  && Task.IsInteractive)
               {
                  startPosition = TaskViews.ViewMain.SetCursorOnPosition(Reverse, new DbPos(true), BoudariesFlags.Locate | BoudariesFlags.Range);
               }
               else
               {
                  startPosition = base.StartPosition;
               }

               // If we didn't realy find a startposition, we need to know the locate failed
               if (startPosition.IsZero)
                  startPosFound = false;
            }

            return startPosition;
         }
      }

      /// <summary>
      /// CTOR
      /// </summary>
      /// <param name="command"></param>
      public LocalDataViewCommandLocate(IClientCommand command)
         : base(command)
      {
         RefreshEventCommand refreshEventCommand = command as RefreshEventCommand;
         if (refreshEventCommand != null)
            clientRecId = refreshEventCommand.ClientRecId;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      internal override ReturnResultBase Execute()
      {
         ReturnResult result;
         if (TaskViews.UseTaskLocate && TaskViews.ViewMain != null)
         {
            TaskViews.ViewMain.CurrentCursor = TaskViews.ViewMain.LocateCursor;

            base.Execute();

            if (ResultStartPosition != null && !ResultStartPosition.IsZero)
            {
               startPosFound = true;
            }
            else
            {
               startPosFound = false;
            }

            TaskViews.ViewMain.CurrentCursor = TaskViews.ViewMain.defaultCursor;
         }

         if (startPosFound)
            result = new ReturnResult();
         else
            // locate failed - return error
            result = new ReturnResult(MsgInterface.LOCATE_STR_ERR_EOF);
         
         return result;
      }
   }
}
