using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.richclient.commands.ClientToServer;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.local.data.view.fields;
using com.magicsoftware.gatewaytypes;
using com.magicsoftware.util;
using com.magicsoftware.unipaas.management.gui;

namespace com.magicsoftware.richclient.local.data.commands
{
   /// <summary>
   /// Local DataView command for add user locate.
   /// </summary>
   class AddUserLocateLocalDataCommand : AddUserRangeLocalDataCommand
   {
      protected override bool IsLocate
      {
         get
         {
            return true;
         }
      }
      /// <summary>
      /// CTOR
      /// </summary>
      /// <param name="command"></param>
      public AddUserLocateLocalDataCommand(AddUserLocateDataViewCommand command)
         : base(command)
      {
         fieldIndex = (int)command.Range.veeIdx - 1;
         userRange = command.Range;
      }

      /// <summary>
      /// This will add the user locates so that view refresh will use this locates value to perform locate.
      /// </summary>
      /// <returns></returns>
      internal override ReturnResultBase Execute()
      {
         IFieldView fieldView = TaskViews.Fields[fieldIndex];

         if (fieldView.IsVirtual || fieldView.IsLink)
         {
            LocalDataviewManager.UserLocates.Add(userRange);
         }
         else
         {
            LocalDataviewManager.UserGatewayLocates.Add(UserRangeToDataRange(userRange, fieldView));
         }

         return new ReturnResult();
      }

   }
}
