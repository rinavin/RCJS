using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.local.data.gateways;
using com.magicsoftware.richclient.rt;
using com.magicsoftware.richclient.data;
using com.magicsoftware.richclient.local.data.view;
using IRecord = com.magicsoftware.unipaas.management.data.IRecord;
using com.magicsoftware.util;
using com.magicsoftware.gatewaytypes;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.commands;
using System.Diagnostics;
using com.magicsoftware.richclient.commands.ClientToServer;

namespace com.magicsoftware.richclient.local.data.commands
{
   /// <summary>
   /// checks if a non-modifiable field can be updated. if not - create the command which will show the error.
   /// </summary>
   class LocalDataViewCommandUpdateNonModifiable : LocalDataViewCommandBase
   {
      Field Field { get; set; }

      /// <summary>
      /// CTOR
      /// </summary>
      /// <param name="command"></param>
      public LocalDataViewCommandUpdateNonModifiable(ExecOperCommand command)
         : base(command)
      {
         Field = command.Operation.GetReturnValueField();
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      internal override ReturnResultBase Execute()
      {
         if (Field.IsVirtual)
            return new ReturnResult();

         // check the conditions which will allow the update
         if (Field.IsLinkField)
         {
            LinkView linkView = TaskViews.LinkViews[Field.getDataviewHeaderId()];
            IRecord record = DataviewSynchronizer.GetCurrentRecord();

            if ((linkView.DataviewHeader.Mode == LnkMode.Write && linkView.GetPosition(record) == null) ||
               linkView.DataviewHeader.Mode == LnkMode.Create)
               return new ReturnResult();
         }

         // update not allowed - return the error
         return new ReturnResult(MsgInterface.RT_STR_NON_MODIFIABLE);
      }
   }
}
