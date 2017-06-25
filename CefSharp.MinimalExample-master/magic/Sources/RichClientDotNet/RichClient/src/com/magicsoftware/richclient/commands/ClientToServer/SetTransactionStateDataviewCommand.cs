using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.local.data;

namespace com.magicsoftware.richclient.commands.ClientToServer
{
   /// <summary>
   /// 
   /// </summary>
   class SetTransactionStateDataviewCommand : DataviewCommand
   {
      internal bool TransactionIsOpen { get; set; }

      /// <summary>
      /// CTOR
      /// </summary>
      public SetTransactionStateDataviewCommand()
      {
         CommandType = DataViewCommandType.SetTransactionState;
      }
   }
}
