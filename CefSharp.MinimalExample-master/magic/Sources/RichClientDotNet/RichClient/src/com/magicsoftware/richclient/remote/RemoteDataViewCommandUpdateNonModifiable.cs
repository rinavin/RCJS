using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.gatewaytypes;
using com.magicsoftware.richclient.util;
using com.magicsoftware.util;
using com.magicsoftware.richclient.commands;
using com.magicsoftware.richclient.commands.ClientToServer;
using System.Diagnostics;

namespace com.magicsoftware.richclient.remote
{
   class RemoteDataViewCommandUpdateNonModifiable : RemoteDataViewCommandBase
   {
      ExecOperCommand execOperCommand { get { return (ExecOperCommand)Command; } }

      /// <summary>
      /// CTOR
      /// </summary>
      /// <param name="command"></param>
      public RemoteDataViewCommandUpdateNonModifiable(ExecOperCommand command)
         : base(command)
      {

      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      internal override ReturnResultBase Execute()
      {
         execOperCommand.Operation.operServer(execOperCommand.MprgCreator);
         return new ReturnResult(MsgInterface.RT_STR_NON_MODIFIABLE);
      }
   }
}
