using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.gatewaytypes.data;
using com.magicsoftware.util;
using com.magicsoftware.richclient.local.data.cursor;
using com.magicsoftware.gatewaytypes;

namespace com.magicsoftware.richclient.local.data.gateways.commands
{
   /// <summary>
   /// Factory for gateway commands.
   /// </summary>
   internal class GatewayCommandsFactory
   {
      /// <summary>
      /// Create File Open command.
      /// </summary>
      /// <param name="fileName"></param>
      /// <param name="dataSourceDefinition"></param>
      /// <param name="access"></param>
      /// <returns></returns>
      public static GatewayCommandFileOpen CreateFileOpenCommand(string fileName, DataSourceDefinition dataSourceDefinition, Access access, LocalManager localManager)
      {
         GatewayCommandFileOpen fileOpenCommand = new GatewayCommandFileOpen();
         fileOpenCommand.FileName = fileName;
         fileOpenCommand.DataSourceDefinition = dataSourceDefinition;
         fileOpenCommand.Access = access;
         fileOpenCommand.LocalManager = localManager;

         return fileOpenCommand;
      }

      /// <summary>
      /// Create File close command.
      /// </summary>
      /// <param name="fileName"></param>
      /// <param name="dataSourceDefinition"></param>
      /// <param name="access"></param>
      /// <param name="localManager"></param>
      /// <returns></returns>
      public static GatewayCommandFileClose CreateFileCloseCommand(DataSourceDefinition dataSourceDefinition, LocalManager localManager)
      {
         GatewayCommandFileClose fileCloseCommand = new GatewayCommandFileClose();
         fileCloseCommand.DataSourceDefinition = dataSourceDefinition;
         fileCloseCommand.LocalManager = localManager;

         return fileCloseCommand;
      }

      /// <summary>
      /// Create Curosr Prepare command.
      /// </summary>
      /// <param name="runtimeCursor"></param>
      /// <param name="localManager"></param>
      /// <returns></returns>
      public static GatewayCommandPrepare CreateCursorPrepareCommand(RuntimeCursor runtimeCursor, LocalManager localManager)
      {
         GatewayCommandPrepare cursorPrepareCommand = new GatewayCommandPrepare();
         cursorPrepareCommand.RuntimeCursor = runtimeCursor;
         cursorPrepareCommand.LocalManager = localManager;
         
         return cursorPrepareCommand;
      }

      /// <summary>
      /// Create Cursor Release command.
      /// </summary>
      /// <param name="runtimeCursor"></param>
      /// <param name="localManager"></param>
      public static GatewayCommandRelease CreateCursorReleaseCommand(RuntimeCursor runtimeCursor, LocalManager localManager)
      {
         GatewayCommandRelease cursorReleaseCommand = new GatewayCommandRelease();
         cursorReleaseCommand.RuntimeCursor = runtimeCursor;
         cursorReleaseCommand.LocalManager = localManager;

         return cursorReleaseCommand;
      }

      /// <summary>
      /// Create cursor Open sommand.
      /// </summary>
      /// <param name="runtimeCursor"></param>
      /// <param name="localManager"></param>
      /// <returns></returns>
      public static GatewayCommandCursorOpen CreateCursorOpenCommand(RuntimeCursor runtimeCursor, LocalManager localManager, bool ValueInGatewayFormat)
      {
         GatewayCommandCursorOpen cursorOpenCommand = CreateCursorOpenCommand(runtimeCursor, localManager);
         cursorOpenCommand.ValueInGatewayFormat = ValueInGatewayFormat;

         return cursorOpenCommand;
      }

      /// <summary>
      /// Create cursor Open sommand.
      /// </summary>
      /// <param name="runtimeCursor"></param>
      /// <param name="localManager"></param>
      /// <returns></returns>
      public static GatewayCommandCursorOpen CreateCursorOpenCommand(RuntimeCursor runtimeCursor, LocalManager localManager)
      {
         GatewayCommandCursorOpen cursorOpenCommand = new GatewayCommandCursorOpen();
         cursorOpenCommand.RuntimeCursor = runtimeCursor;
         cursorOpenCommand.LocalManager = localManager;

         return cursorOpenCommand;
      }

      /// <summary>
      /// Create cursor Close command.
      /// </summary>
      /// <param name="runtimeCursor"></param>
      /// <param name="localManager"></param>
      /// <returns></returns>
      public static GatewayCommandCursorClose CreateCursorCloseCommand(RuntimeCursor runtimeCursor, LocalManager localManager)
      {
         GatewayCommandCursorClose cursorCloseCommand = new GatewayCommandCursorClose();
         cursorCloseCommand.RuntimeCursor = runtimeCursor;
         cursorCloseCommand.LocalManager = localManager;

         return cursorCloseCommand;
      }

      /// <summary>
      /// Create Cursor Delete command.
      /// </summary>
      /// <param name="fileName"></param>
      /// <param name="dataSourceDefinition"></param>
      /// <param name="localManager"></param>
      /// <returns></returns>
      public static GatewayCommandFileDelete CreateFileDeleteCommand(string fileName, DataSourceDefinition dataSourceDefinition, LocalManager localManager)
      {
         GatewayCommandFileDelete fileDeleteCommand = new GatewayCommandFileDelete();
         fileDeleteCommand.DataSourceDefinition = dataSourceDefinition;
         fileDeleteCommand.LocalManager = localManager;
         fileDeleteCommand.FileName = fileName;

         return fileDeleteCommand;
      }

      /// <summary>
      /// Create File Exist command.
      /// </summary>
      /// <param name="fileName"></param>
      /// <param name="dataSourceDefinition"></param>
      /// <param name="localManager"></param>
      /// <returns></returns>
      public static GatewayCommandFileExist CreateFileExistCommand(string fileName, DataSourceDefinition dataSourceDefinition, LocalManager localManager)
      {
         GatewayCommandFileExist fileExistCommand = new GatewayCommandFileExist();
         fileExistCommand.DataSourceDefinition = dataSourceDefinition;
         fileExistCommand.LocalManager = localManager;
         fileExistCommand.FileName = fileName;
         return fileExistCommand;
      }


      /// <summary>
      /// Create Cursor Fetch command.
      /// </summary>
      /// <param name="runtimeCursor"></param>
      /// <param name="localManager"></param>
      /// <returns></returns>
      public static GatewayCommandFetch CreateCursorFetchCommand(RuntimeCursor runtimeCursor, LocalManager localManager)
      {
         GatewayCommandFetch cursorFetchCommand = new GatewayCommandFetch();
         cursorFetchCommand.RuntimeCursor = runtimeCursor;
         cursorFetchCommand.LocalManager = localManager;

         return cursorFetchCommand;
      }

      /// <summary>
      /// Create Cursor Insert Commmand.
      /// </summary>
      /// <param name="runtimeCursor"></param>
      /// <param name="localManager"></param>
      /// <returns></returns>
      public static GatewayCommandCursorInsertRecord CreateCursorInsertCommand(RuntimeCursor runtimeCursor, LocalManager localManager, bool ValueInGatewayFormat)
      {
         GatewayCommandCursorInsertRecord cursorInsertCommand = CreateCursorInsertCommand(runtimeCursor, localManager);
         cursorInsertCommand.ValueInGatewayFormat = ValueInGatewayFormat;

         return cursorInsertCommand;
      }

      /// <summary>
      /// Create Cursor Insert Commmand.
      /// </summary>
      /// <param name="runtimeCursor"></param>
      /// <param name="localManager"></param>
      /// <returns></returns>
      public static GatewayCommandCursorInsertRecord CreateCursorInsertCommand(RuntimeCursor runtimeCursor, LocalManager localManager)
      {
         GatewayCommandCursorInsertRecord cursorInsertCommand = new GatewayCommandCursorInsertRecord();
         cursorInsertCommand.RuntimeCursor = runtimeCursor;
         cursorInsertCommand.LocalManager = localManager;

         return cursorInsertCommand;
      }
      /// <summary>
      /// Create File Rename Command.
      /// </summary>
      /// <param name="dataSourceDefinition"></param>
      /// <param name="sourceFileName"></param>
      /// <param name="destinationFileName"></param>
      /// <param name="localManager"></param>
      /// <returns></returns>
      public static GatewayCommandFileRename CreateFileRenameCommand(DataSourceDefinition sourceDataSourceDefinition, DataSourceDefinition destinationDataSourceDefinition, 
                                                                     LocalManager localManager)
      {
         GatewayCommandFileRename fileRenameCommand = new GatewayCommandFileRename();
         fileRenameCommand.DataSourceDefinition = sourceDataSourceDefinition;
         fileRenameCommand.DestinationDataSourceDefinition = destinationDataSourceDefinition;
         fileRenameCommand.LocalManager = localManager;

         return fileRenameCommand;
      }

    /// <summary>
    /// create open transaction command
    /// </summary>
    /// <param name="localManager"></param>
    /// <returns></returns>
      public static GatewayCommandOpenTransaction CreateGatewayCommandOpenTransaction( LocalManager localManager)
      {
          GatewayCommandOpenTransaction transactionCommand = new GatewayCommandOpenTransaction();
          transactionCommand.LocalManager = localManager;
          return transactionCommand;
      }

      /// <summary>
      /// create close transaction command
      /// </summary>
      /// <param name="localManager"></param>
      /// <returns></returns>
      public static GatewayCommandCloseTransaction CreateGatewayCommandCloseTransaction(LocalManager localManager)
      {
         GatewayCommandCloseTransaction transactionCommand = new GatewayCommandCloseTransaction();
         transactionCommand.LocalManager = localManager;
         return transactionCommand;
      }

      /// <summary>
      /// Create Gateway DbDisconnect command.
      /// </summary>
      /// <param name="localManager"></param>
      /// <param name="databaseName"></param>
      /// <returns></returns>
      public static GatewayCommandDbDisconnect CreateGatewayCommandDbDisconnect(LocalManager localManager, string databaseName)
      {
         GatewayCommandDbDisconnect dbDisconnectCommand = new GatewayCommandDbDisconnect();
         dbDisconnectCommand.LocalManager = localManager;

         DataSourceDefinition datasourceDefinition = new DataSourceDefinition();
         datasourceDefinition.DBaseName = databaseName.ToUpper();
         dbDisconnectCommand.DataSourceDefinition = datasourceDefinition;
         return dbDisconnectCommand;
      }

      /// <summary>
      /// Create Cursor update command.
      /// </summary>
      /// <param name="runtimeCursor"></param>
      /// <param name="localManager"></param>
      /// <returns></returns>
      public static GatewayCommandCursorUpdateRecord CreateGatewayCommandCursorUpdateRecord(RuntimeCursor runtimeCursor, LocalManager localManager, bool ValueInGatewayFormat)
      {
         GatewayCommandCursorUpdateRecord cursorUpdateCommand = CreateGatewayCommandCursorUpdateRecord(runtimeCursor, localManager);
         cursorUpdateCommand.ValueInGatewayFormat = ValueInGatewayFormat;

         return cursorUpdateCommand;
      }

      /// <summary>
      /// Create Cursor update command.
      /// </summary>
      /// <param name="runtimeCursor"></param>
      /// <param name="localManager"></param>
      /// <returns></returns>
      public static GatewayCommandCursorUpdateRecord CreateGatewayCommandCursorUpdateRecord(RuntimeCursor runtimeCursor, LocalManager localManager)
      {
         GatewayCommandCursorUpdateRecord cursorUpdateCommand = new GatewayCommandCursorUpdateRecord();
         cursorUpdateCommand.RuntimeCursor = runtimeCursor;

         // execute the command
         cursorUpdateCommand.LocalManager = localManager;

         return cursorUpdateCommand;
      }

      /// <summary>
      /// Create Gateway SQL execute command.
      /// </summary>
      /// <param name="localManager"></param>
      /// <param name="databaseName"></param>
      /// <returns></returns>
      public static GatewayCommandSQLExecute CreateGatewayCommandSQLExecute(LocalManager localManager, string databaseName, 
         string sqlStatement, StorageAttribute[] storageAtrributes, DBField[] dbFields)
      {
         GatewayCommandSQLExecute gatewayCommandSQLExecute = new GatewayCommandSQLExecute();
         gatewayCommandSQLExecute.LocalManager = localManager;

         DataSourceDefinition datasourceDefinition = new DataSourceDefinition();
         datasourceDefinition.DBaseName = databaseName.ToUpper();
         gatewayCommandSQLExecute.DataSourceDefinition = datasourceDefinition;
         gatewayCommandSQLExecute.sqlStatement = sqlStatement;
         gatewayCommandSQLExecute.storageAttributes = storageAtrributes;
         gatewayCommandSQLExecute.dbFields = dbFields;
         return gatewayCommandSQLExecute;
      }

   }
}
