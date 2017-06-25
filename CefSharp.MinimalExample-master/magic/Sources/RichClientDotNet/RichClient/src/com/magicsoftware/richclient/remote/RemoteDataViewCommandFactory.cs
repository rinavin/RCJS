using com.magicsoftware.richclient.local.data;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.commands.ClientToServer;

namespace com.magicsoftware.richclient.remote
{
   /// <summary>
   /// 
   /// </summary>
   internal class RemoteDataViewCommandFactory
   {
      /// <summary>
      /// a factory method that creates an data view command from the send command
      /// </summary>
      /// <param name="command"></param>
      /// <returns></returns>
      internal RemoteDataViewCommandBase CreateDataViewCommand(ClientOriginatedCommand command)
      {
         RemoteDataViewCommandBase remoteDataViewCommandBase = new DummyDataViewCommand(command);
         ExecOperCommand execOperCommand = command as ExecOperCommand;
         if (execOperCommand != null && execOperCommand.Operation != null && execOperCommand.Operation.getType() == ConstInterface.MG_OPER_UPDATE)
            remoteDataViewCommandBase = new RemoteDataViewCommandUpdateNonModifiable(execOperCommand);
         else if (!(command is DataviewCommand)) //this is local dataview commands
            remoteDataViewCommandBase = new RemoteDataViewCommandBase(command);
         else //if (command.Type == ConstInterface.CMD_TYPE_DATAVIEW)
         {
            DataviewCommand dataviewCommand = command as DataviewCommand;
            switch (dataviewCommand.CommandType)
            {                
                case DataViewCommandType.SetTransactionState:
                  remoteDataViewCommandBase = new SetTransactionStateRemoteDataViewCommand((SetTransactionStateDataviewCommand)dataviewCommand);
                    break;

               case DataViewCommandType.AddUserRange:
                    remoteDataViewCommandBase = new AddUserRangeRemoteDataViewCommand((AddUserRangeDataviewCommand)command);
                    break;

               case DataViewCommandType.ResetUserRange:
                    remoteDataViewCommandBase = new ResetUserRangeRemoteDataviewCommand(command);
                    break;

               case DataViewCommandType.AddUserLocate:
                    remoteDataViewCommandBase = new AddUserLocateRemoteDataViewCommand((AddUserLocateDataViewCommand)command);
                    break;

               case DataViewCommandType.ResetUserLocate:
                    remoteDataViewCommandBase = new ResetUserLocateRemoteDataviewCommand(command);
                    break;

               case DataViewCommandType.AddUserSort:
                    remoteDataViewCommandBase = new AddUserSortRemoteDataViewCommand((AddUserSortDataViewCommand)command);
                    break;
               
               case DataViewCommandType.ResetUserSort:
                    remoteDataViewCommandBase = new ResetUserSortRemoteDataviewCommand(command);
                    break;
               case DataViewCommandType.DataViewToDataSource:
                    remoteDataViewCommandBase = new RemoteDataViewToDataSourceCommand((DataViewOutputCommand)command);
                    break;
               case DataViewCommandType.ControlItemsRefresh:
                    remoteDataViewCommandBase = new RemoteControlItemsRefreshCommand((ControlItemsRefreshCommand)command);
                    break;

            }
        }

         return remoteDataViewCommandBase;
      }
   }
}
