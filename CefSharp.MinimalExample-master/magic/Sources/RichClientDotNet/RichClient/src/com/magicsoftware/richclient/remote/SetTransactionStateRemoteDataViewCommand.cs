
using com.magicsoftware.richclient.remote;
using com.magicsoftware.richclient.rt;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.commands;
using com.magicsoftware.richclient.commands.ClientToServer;

namespace com.magicsoftware.richclient.remote
{
   /// <summary>
   /// set state transaction
   /// </summary>
   internal class SetTransactionStateRemoteDataViewCommand : RemoteDataViewCommandBase
   {
      SetTransactionStateDataviewCommand dataviewCommand { get { return (SetTransactionStateDataviewCommand)Command; } }

      public SetTransactionStateRemoteDataViewCommand(SetTransactionStateDataviewCommand command)
         : base(command)
      { }


      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      internal override ReturnResultBase Execute()
      {
         if (Task != null)
         {
            Transaction transaction = Task.DataviewManager.RemoteDataviewManager.Transaction;
            if (transaction != null)
               transaction.Opened = dataviewCommand.TransactionIsOpen;
         }

         return new ReturnResult(); ;
      }
   }
}
