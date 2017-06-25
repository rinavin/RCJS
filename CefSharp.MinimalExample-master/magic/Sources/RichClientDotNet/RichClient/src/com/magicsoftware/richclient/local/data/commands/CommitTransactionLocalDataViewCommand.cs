using com.magicsoftware.gatewaytypes;
using com.magicsoftware.richclient.local.data.gateways;
using com.magicsoftware.richclient.local.data.gateways.commands;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.commands;
using com.magicsoftware.richclient.commands.ClientToServer;

namespace com.magicsoftware.richclient.local.data.commands
{
   /// <summary>
   /// prepare command
   /// </summary>
   internal class CommitTransactionLocalDataViewCommand : CloseTransactionLocalDataViewCommandBase
   {
      public CommitTransactionLocalDataViewCommand(TransactionCommand command)
         : base(command)
      {
         TransactionModes = TransactionModes.Commit;
      }

    
   }
}
