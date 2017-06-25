using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Text;
using com.magicsoftware.util;
using com.magicsoftware.gatewaytypes;
using com.magicsoftware.gatewaytypes.data;
using SQL3_CODE = System.Int32;
using util.com.magicsoftware.util;
using System.Collections;

namespace MgSqlite.src
{
   /// <summary>
   ///  This is main class of SQLiteGateway. It implements all interface methods of SQLite Gateway.
   /// </summary>
   public class SQLiteGateway : ISQLGateway 
   {
      internal SQLiteLow            SQLiteLow;

      internal Dictionary<DataSourceDefinition, SQL3Dbd> DbdTbl;
      internal Dictionary<string,SQL3Connection> ConnectionTbl;
      internal List<SQL3Cursor>     CursorTbl;
      internal Dictionary<GatewayAdapterCursor, GatewayCursor> GatewayCursorTbl;
      internal List<Sql3Stmt>       StmtTbl;
      internal string               Statement;

      internal long                 SqldaNum;

      public string                  LastErr;
      public bool                    SaveError; // will tell whether to overwrite an existing err in LAST_err.
      public int                    LastErrCode;    
      public long                    ServerErrCode;
      public int                     SQL3NotNull;
      public string                  SQL3PrefixBuf;
      public Int64                   SQL3InsRowid;
      public string                  SQL3FloatStr;
      public object                  TID_buf;
      public bool                    TransToOpen; // Used to execute 'postponed' transaction Begin opern.

      /// </summary>
      ///   constructor that create SqliteInfo object.
      /// </summary>
      public SQLiteGateway()
      {
         DbdTbl = new Dictionary<DataSourceDefinition, SQL3Dbd>();
         ConnectionTbl = new Dictionary<string,SQL3Connection>();
         CursorTbl = new List<SQL3Cursor>();
         GatewayCursorTbl = new Dictionary<GatewayAdapterCursor, GatewayCursor>();
         StmtTbl = new List<Sql3Stmt>();
         SQLiteLow = new SQLiteLow(this);
         SQL3NotNull = 0;
         TransToOpen = false;
#if SQLITE_CIPHER_CPP_GATEWAY
         SQLite3DLLImports.Initialize(new SQLite3DLLImports());
#endif
      }

      /// <summary>
      /// FileOpen() opens a table, creating it if necessary. If Magic is
      /// opening the table for the first time (its DBD->opened count is zero),
      /// then an SQL3_DBD is allocated for the table and DBD->dbdHdl is assigned
      /// with that handle, so Magic can tell the driver the handle of the table
      /// when it operates on it. The function finds the name of the table
      /// from the name that Magic supplies; it then finds out whether a table by
      /// that name already exists (see also sql3_fil_exist()). If the table does
      /// not exist, FilCreate() is called to create it and sql3_add_index()
      /// to add indexes to it. If the table is already open (DBD->opened is one
      /// or more), then there is an SQL3_DBD for the table. If the table is being
      /// opened for reindexing, the indexes are added to assure they can be
      /// created and then dropped, to be recreated on sql3_fil_close(). If the
      /// table is opened in SHARE_NONE mode, locking is exclusive, so no other
      /// user can lock the table or update its contents (but can query it).
      /// Allowed error codes are ERR_BADCREATE, ERR_BADOPEN or ERR_LOCKED. If the
      /// function created a new table, it sets the warning flag to WRM_CREATED.
      /// </summary>
      /// <param name="dbd"></param>
      /// <param name="dbDefinition"></param>
      /// <param name="fileName"></param>
      /// <param name="access"></param>
      /// <param name="share"></param>
      /// <param name="mode"></param>
      /// <param name="referencedDbhVec"></param>
      /// <returns>RET_CODE</returns>
      public GatewayErrorCode FileOpen(DataSourceDefinition dbh, DatabaseDefinition dbDefinition, string fileName, Access access, 
                               DbShare share, DbOpen mode, List<DataSourceDefinition> referencedDbhVec)
      {
         bool              sqlTabExist    = false;
         bool              checkFileExist = true;
         GatewayErrorCode  returnCode     = GatewayErrorCode.Any;
         int               errorCode      = 0;
         int               idx            = 0;
         SQL3Dbd           sql3Dbd;
         SQL3Connection    sql3Connection;
         TableType         tableType      = TableType.Table;
         bool              sqlView        = false;

         Logger.Instance.WriteSupportToLog(string.Format("FileOpen(): >>>>> database = {0}, table = {1}, share = {2}", dbDefinition.Name, fileName, share), true);

         Logger.Instance.WriteDevToLog(string.Format("FileOpen(): DB_OPEN_REINDEX = {0}", (mode == DbOpen.Reindex ? "TRUE" : "FALSE")));

         if (string.IsNullOrEmpty(fileName))
         {
            returnCode = GatewayErrorCode.StringBadName;
            Logger.Instance.WriteSupportToLog(string.Format("FileOpen(): <<<<< returnCode = {0}\n", returnCode), true);

            return returnCode;
         }

         if (!DbdTbl.TryGetValue(dbh, out sql3Dbd))
         {
            // get an sql3Dbd
            idx = Sql3DbdAlloc(dbh);
            DbdTbl.TryGetValue(dbh, out sql3Dbd);

            sql3Dbd.DataSourceDefinition = dbh;

            SQL3CheckIdentityField(sql3Dbd);

            if ((FindPoskey(dbh) != null))
            {
               sql3Dbd.IsUnique = true;
            }
            else
            {
               sql3Dbd.IsUnique = false;
            }

            // Create empty / already used connection
            int connectDbRetCode = ConnectDb(dbDefinition);

            switch (connectDbRetCode)
            {
               case SqliteConstants.NO_DATABASE_NAME_GIVEN:
                  Logger.Instance.WriteSupportToLog("FileOpen(): no database name given\n", true);
                  return GatewayErrorCode.BadOpen;

               case SqliteConstants.UNABLE_TO_OPEN_DATABASE:
                  Logger.Instance.WriteSupportToLog("FileOpen(): Unable to open the database file\n", true);
                  return GatewayErrorCode.BadOpen;

               case SqliteConstants.SQLITE_NOTADB:
                  Logger.Instance.WriteSupportToLog(string.Format("FileOpen(): <<<<< returnCode = {0}\n", connectDbRetCode), true);
                  return GatewayErrorCode.GetUserPassword;
            }

            sql3Connection = ConnectionTbl[dbDefinition.Location];

            Sql3GetFullName(out sql3Dbd.FullName, sql3Dbd.FullName.Length, dbh, dbDefinition, fileName);

            sql3Dbd.DatabaseName = dbDefinition.Location;
            sql3Dbd.TableName = fileName.ToUpper();
            sql3Dbd.mode = mode;
            sql3Dbd.share = share;

            // initialize the number of records that should be read from the server in each fetch
            sql3Dbd.arrayBufferSize = dbh.ArraySize;


            if (Logger.Instance.LogLevel >= Logger.LogLevels.Development)
            {
               if(sql3Dbd.arrayBufferSize != SqliteConstants.NULL_ARRAY_SIZE)
               {
                  Logger.Instance.WriteDevToLog(string.Format("FileOpen(): array size is {0} ", sql3Dbd.arrayBufferSize));
               }
               else
               {
                  Logger.Instance.WriteDevToLog("FileOpen(): array size was not specified");
               }
            }

            if (dbh.CheckMask(DbhMask.CheckExistMask))
               sqlTabExist = false;
            else
               sqlTabExist = true;

            if (sqlTabExist)
               checkFileExist = false;
            else
               checkFileExist = true;


            if (checkFileExist)
            {
               errorCode = SQLiteLow.LibTableType(ConnectionTbl[sql3Dbd.DatabaseName], sql3Dbd.TableName, out tableType);

               if (errorCode == SqliteConstants.SQL3_SQL_NOTFOUND)
               {
                  errorCode = (int)SQLiteErrorCode.Ok;
                  tableType = TableType.Table;
               }
            }

            if (errorCode == (int)SQLiteErrorCode.Ok)
            {
               switch (tableType)
               {
                  case TableType.Table:
                     if (sqlView == true)
                     {
                        Logger.Instance.WriteDevToLog("FileOpen(): table type is UPD VIEW ");
                        if (dbh.RowIdentifier == (char)DBHRowIdentifier.RowId)
                           sql3Dbd.IsView = false;
                        else
                           sql3Dbd.IsView = true;
                     }
                     else
                     {
                        Logger.Instance.WriteDevToLog("FileOpen(): table type is TABLE ");

                        if (dbh.RowIdentifier == (char)DBHRowIdentifier.UniqueKey)
                           sql3Dbd.IsView = true;
                        else
                           sql3Dbd.IsView = false;
                     }
                     break;

                  case TableType.View:
                     if (checkFileExist == false)
                     {
                        Logger.Instance.WriteDevToLog("FileOpen(): table type is UPD VIEW ");
                        if (dbh.RowIdentifier != (char)DBHRowIdentifier.RowId)
                           sql3Dbd.IsView = true;
                        else
                           sql3Dbd.IsView = false;
                     }
                     else
                     {
                        Logger.Instance.WriteDevToLog("FileOpen(): table type is VIEW ");

                        if (!sql3Dbd.IsUnique)
                        {
                           Logger.Instance.WriteDevToLog("FileOpen(): <<<<< NEED A UNIQUE INDEX ON A VIEW ");
                           LastErr = "SQLite Gateway: use a unique virtual key on a view";

                           returnCode = GatewayErrorCode.BadOpen;
                           Logger.Instance.WriteSupportToLog(string.Format("FileOpen(): <<<<< returnCode = {0}\n", returnCode), true);
                           return returnCode;
                        }

                        sql3Dbd.IsView = true;
                     }

                     break;
                  default:
                     Logger.Instance.WriteDevToLog("FileOpen(): table type is UNKNOWN ");
                     Logger.Instance.WriteSupportToLog(string.Format("FileOpen(): <<<<< returnCode = {0}\n", returnCode), true);

                     return GatewayErrorCode.BadOpen;
               }// end switch
            }


            sql3Dbd.posLen = GetPosSize(sql3Dbd);

            if (!string.IsNullOrEmpty(sql3Dbd.DatabaseName))
            {
               if (dbh.CheckMask(DbhMask.CheckExistMask))
                  errorCode = FileExist (sql3Dbd);
               else
                  errorCode = SqliteConstants.SQL3_OK;

               if (errorCode == SqliteConstants.SQL3_OK)
                  returnCode = SqliteConstants.RET_OK;
               else
               {
                  if (errorCode == SqliteConstants.SQL3_SQL_NOTFOUND)
                  {
                     if ( dbh.CheckMask(DbhMask.FileTypeViewMask))
                        returnCode = GatewayErrorCode.FileIsView;
                     else
                     {
                        errorCode = FilCreate (sql3Dbd, referencedDbhVec, dbDefinition);

                        if (dbh.RowIdentifier == (char)DBHRowIdentifier.UniqueKey)
                        {
                           sql3Dbd.IsView = true; //Mark the table created as a view 
                        }

                        if (errorCode == SqliteConstants.SQL3_OK)
                        {
                           returnCode = SqliteConstants.RET_OK;

                           if (mode != DbOpen.Reindex)
                              errorCode = AddIndexes (sql3Dbd, IndexingMode.CREATE_MODE, false);
                           else
                              errorCode = AddIndexes (sql3Dbd, IndexingMode.REINDEX_OPEN_MODE, false);

                           if (errorCode != SqliteConstants.SQL3_OK)
                              returnCode = GatewayErrorCode.BadCreate;
                        }
                        else
                           returnCode = GatewayErrorCode.BadCreate;
                     }
                 }
                 else
                 {
                     returnCode = GatewayErrorCode.BadCreate;
                 }
               }
            }
            else
            {
                returnCode = GatewayErrorCode.BadCreate;
            }
         }
         else
         {
            DbdTbl.TryGetValue(dbh, out sql3Dbd);
            
            sql3Dbd.share = share;
            sql3Dbd.DataSourceDefinition = dbh;
            returnCode = SqliteConstants.RET_OK;
         }
          
         if (access == Access.Read)
            sql3Dbd.access = Access.Read;
         if (access == Access.Read || sql3Dbd.IsLoaclTempTable())
            sql3Dbd.IsPhysicalLock = false;

         if (returnCode == SqliteConstants.RET_OK)
         {
            if (mode >= DbOpen.Reindex)
            {
               if (DropIndexes(sql3Dbd) != SqliteConstants.SQL3_OK)
               {
                  // Ignore drop index message so the user can open the file and delete the
                  // duplicate records (the reason why the index didn't created
                  SaveError = true;
                  LastErrCode = SqliteConstants.SQL3_OK;
                  ServerErrCode = SqliteConstants.SQL3_OK;
                  returnCode = SqliteConstants.RET_OK;
               }
            }
         }

         Logger.Instance.WriteSupportToLog(string.Format("FileOpen(): <<<<< returnCode = {0}\n", returnCode), true);
         
         return returnCode;
      }

      /// <summary>
      ///  sql3_trans() opens, commits or aborts a transaction on all currently open
      ///  connections. OPEN_LOCK starts a fresh transaction (by committing the last
      ///  one) with the write_lock flag turned on: sql3_crsr_fetch() will lock the
      ///  rows that it reads. OPEN_WRITE is insignificant, because Magic guarantees
      ///  that there will be an OPEN_LOCK transaction preceding it. ( not SQL3 - OPEN_READ starts
      ///  a read-only transaction in which no locking or updating is allowed; it
      ///  issues a SET TRANSACTION READ ONLY command, but since that must be the
      ///  first command in the transaction it follows a COMMIT to end the previous
      ///  transaction.) COMMIT initiates a commit on the current transaction and
      ///  ABORT does a rollback of the transaction to the point of the last commit.
      ///  Possible error codes are ERR_TRANS_OPEN, ERR_TRANS_COMMIT and
      ///  ERR_TRANS_ABORT.
      /// </summary>
      /// <param name="transmode"></param>
      /// <param name="db"></param>
      /// <returns>RET_CODE</returns>

#if SQLITE_CIPHER_CPP_GATEWAY
      public GatewayErrorCode Trans(int transmode)
      {
         GatewayErrorCode returnCode = GatewayErrorCode.Any;

         Logger.Instance.WriteSupportToLog(string.Format("Trans(): >>>>> transmode = {0}", transmode), true);

         SQL3Connection connection;
         Sql3Stmt sql3Stmt;
         //Fixed performance problem when manipulating data not in the scope of a transaction.
         //So,Open a transaction for every connection which are present in the connection table.
         Dictionary<string, SQL3Connection>.Enumerator connectionEnumerator = ConnectionTbl.GetEnumerator();
         while (connectionEnumerator.MoveNext())
         {
            connection = connectionEnumerator.Current.Value;
            if (connection.ConnHandleUnmanaged != null)
            {

               switch (transmode)
               {
                  case (int)TransactionModes.OpenWrite:
                     Logger.Instance.WriteSupportToLog("Trans(): OPEN_WRITE", true);
                     TransToOpen = true;
                     break;

                  case (int)TransactionModes.OpenRead:
                     Logger.Instance.WriteSupportToLog("Trans(): OPEN_READ", true);
                     TransToOpen = true;
                     break;

                  case (int)TransactionModes.Commit:
                     if (connection.InTransaction)
                     {
                        Logger.Instance.WriteSupportToLog("Trans(): COMMIT", true);
                        if (SQLiteLow.LibCommitTransaction(connection) != SqliteConstants.SQLITE_OK)
                           returnCode = GatewayErrorCode.TransactionCommit;
                        else
                        {
                           connection.InTransaction = false;
                           TransToOpen = false;
                        }
                     }

                     break;

                  case (int)TransactionModes.Abort:
                     if (connection.InTransaction)
                     {
                        Logger.Instance.WriteSupportToLog("trans(): rollback", true);

                        // TODO : Needs to be checked with BG transaction progs.
                        // Check for all prepared statements.
                        // Here we need to release the statement before aborting the transaction as transaction is opened on 
                        // a connection so prepared statements should get released first.
                        for (int i = 0; i < StmtTbl.Count; i++)
                        {
                           sql3Stmt = StmtTbl[i];

                           if (sql3Stmt.InUse)
                           {
                              SQLiteLow.LibReleaseStmt(sql3Stmt);
                           }
                        }

                        if (SQLiteLow.LibRollbackTransaction(connection) != SqliteConstants.SQLITE_OK)
                           returnCode = GatewayErrorCode.TransactionAbort;
                        else
                           connection.InTransaction = false;
                     }
                     TransToOpen = false;
                     break;

                  default:
                     break;
               } /*switch stmt*/
            }
         }

         Logger.Instance.WriteSupportToLog(string.Format("Trans(): <<<<< returnCode = {0}\n", returnCode), true);
         return (returnCode);
      }
#else
      public GatewayErrorCode Trans (int transmode)
      {
         GatewayErrorCode        returnCode           = GatewayErrorCode.Any;

         Logger.Instance.WriteSupportToLog(string.Format("Trans(): >>>>> transmode = {0}", transmode), true);

         SQL3Connection connection;
         Sql3Stmt sql3Stmt;
         //Fixed performance problem when manipulating data not in the scope of a transaction.
         //So,Open a transaction for every connection which are present in the connection table.
         Dictionary<string, SQL3Connection>.Enumerator connectionEnumerator = ConnectionTbl.GetEnumerator();
         while (connectionEnumerator.MoveNext())
         {
            connection = connectionEnumerator.Current.Value;
            if (connection.connectionHdl != null)
            {

               switch (transmode)
               {
                  case (int)TransactionModes.OpenWrite:
                     Logger.Instance.WriteSupportToLog("Trans(): OPEN_WRITE", true);
                     try
                     {

                        TransToOpen = true;
                     }
                     catch(SQLiteException)
                     {
                        returnCode = GatewayErrorCode.TransactionOpen;
                     }
                     break;

                  case (int)TransactionModes.OpenRead:
                     Logger.Instance.WriteSupportToLog("Trans(): OPEN_READ", true);
                     TransToOpen = true;
                     break;

                  case (int)TransactionModes.Commit:
                     if (connection.Transaction != null)
                     {
                        Logger.Instance.WriteSupportToLog("Trans(): COMMIT", true);
                  
                        try
                        {
                           connection.Transaction.Commit();
                           connection.InTransaction = false;
                           connection.Transaction = null;
                        }
                        catch (SQLiteException )
                        {
                           returnCode = GatewayErrorCode.TransactionCommit;
                        }
                        TransToOpen = false;
                     }
                  
                     break;

                  case (int)TransactionModes.Abort:
                     if (connection.Transaction != null)
                     {
                        Logger.Instance.WriteSupportToLog("Trans(): ROLLBACK", true);
                        ServerErrCode = SqliteConstants.SQL3_OK;

                        // TODO : Needs to be checked with BG transaction progs.
                        // Check for all prepared statements.
                        // Here we need to release the statement before aborting the transaction as transaction is opened on 
                        // a connection so prepared statements should get released first.
                        for (int i = 0; i < StmtTbl.Count; i++)
                        {
                           sql3Stmt = StmtTbl[i];

                           if (sql3Stmt.InUse)
                           {
                              SQLiteLow.LibReleaseStmt(sql3Stmt);
                           }
                        }

                        try
                        {
                           connection.Transaction.Rollback();
                           connection.InTransaction = false;
                           connection.Transaction = null;
                           
                        }
                        catch (SQLiteException)
                        {
                           returnCode = GatewayErrorCode.TransactionAbort; 	
                        }
                        TransToOpen = false;
                     
                     }
                     break;
         
                  default:
                     break;
               } /*switch stmt*/
            }
         }

         Logger.Instance.WriteSupportToLog(string.Format("Trans(): <<<<< returnCode = {0}\n", returnCode), true);
         return (returnCode);
      }
#endif

      /// <summary>
      /// FilExist() checks whether a table already exists. It tries to select
      /// a single rowid from that table without actually collecting the returned
      /// data. If anything is returned, or no more records, then the table exists
      /// and the function returns RET_OK, if the table or view does not exist, the
      /// function returns DB_ERR_NOT_EXIST.
      /// </summary>
      /// <param name="dbh"></param>
      /// <param name="dbDefinition"></param>
      /// <param name="fname"></param>
      /// <returns>RET_CODE</returns>
      public GatewayErrorCode FilExist (DataSourceDefinition dbh, DatabaseDefinition dbDefinition, string fname)
      {
         SQL3_CODE         errcode     = SqliteConstants.SQL3_OK;
         GatewayErrorCode  retcode     = GatewayErrorCode.Any;
         SQL3Connection    sql3Connection  = null;
         string            fullName    = string.Empty;

         Logger.Instance.WriteSupportToLog(string.Format("FilExist(): >>>>> database = {0}, file : {1}", dbDefinition.Name, fname), true);

         if (ConnectDb(dbDefinition) == SqliteConstants.NO_DATABASE_NAME_GIVEN)
         {
            errcode = SqliteConstants.NO_DATABASE_NAME_GIVEN;
            Logger.Instance.WriteSupportToLog("FilExist(): <<<<< no database name given\n", true);
            return GatewayErrorCode.BadOpen;
         }
         

         sql3Connection = ConnectionTbl[dbDefinition.Location];

         if (!Sql3CheckTableName(fname))
         {
           
            LastErr =  "SQLite: Table name too long";

            Logger.Instance.WriteDevToLog(string.Format("FilExist(): <<<<< Table name : {0}\n too long", fname));
            return GatewayErrorCode.BadOpen;
         }
         else if (dbDefinition.Location.Length > SqliteConstants.SQL3_MAX_OBJECTNAME)
         {
            
            LastErr =  "SQLite: database name is too long, maximum is %d characters";

            Logger.Instance.WriteSupportToLog(string.Format("FilExist(): <<<<< SQLite: database name is too long, maximum is %d characters\n", SqliteConstants.SQL3_MAX_OBJECTNAME), true);
            return GatewayErrorCode.BadOpen;
         }

         Sql3GetFullName(out fullName, fullName.Length, dbh, dbDefinition, fname);

         if (BeginTransactionIfNeeded(sql3Connection) == SqliteConstants.SQL3_TRANS_ERROR)
         {
            Logger.Instance.WriteSupportToLog(string.Format("FilExist(): <<<<< retcode = %d\n", GatewayErrorCode.TransactionOpen), true);
            return GatewayErrorCode.TransactionOpen;
         }

         errcode = SQLiteLow.LibFilExist(ConnectionTbl[dbDefinition.Location], fullName);
            
         if (errcode == SqliteConstants.SQL3_OK)
         {
            retcode = SqliteConstants.RET_OK;
            Logger.Instance.WriteDevToLog(string.Format("FilExist(): file : {0} EXISTS", fullName));
         }
         /*-------------------------------------------------------------------------*/
         /* 9/6/97 add checking of Server_err_code of wrong Database name           */
         /*-------------------------------------------------------------------------*/
         else  if (errcode == SqliteConstants.SQL3_SQL_NOTFOUND)
         {
            Logger.Instance.WriteDevToLog(string.Format("FilExist(): file : {0}DOES NOT EXIST", fullName));
            retcode =  GatewayErrorCode.NotExist;
         }
         else
         {
            Logger.Instance.WriteDevToLog(string.Format("FilExist(): file : {0} sqlcode = {1}", fullName, errcode));
            retcode = GatewayErrorCode.BadOpen;
         }

         Logger.Instance.WriteSupportToLog(string.Format("FilExist(): <<<<< retcode = {0}\n", retcode), true);
         return (retcode);

      }

      /// <summary>
      ///  FilDel ()
      /// </summary>
      /// <param name="dbh"></param>
      /// <param name="dbPts"></param>
      /// <param name="fname"></param>
      /// <returns>RET_CODE</returns>
      public GatewayErrorCode FileDelete (DataSourceDefinition dbh, DatabaseDefinition dbDefinition, string fname)
      {
         SQL3_CODE            errcode                 = 0;
         GatewayErrorCode     retcode                 = GatewayErrorCode.Any;
         string               fullName                = string.Empty;
         SQL3Connection       sql3Connection;
         SQL3Dbd              sql3Dbd;

         Logger.Instance.WriteSupportToLog(string.Format("FilDel(): >>>>> database = {0}, file = {1}", dbDefinition.Name, fname), true);

         if (dbh.CheckMask(DbhMask.FileTypeViewMask))
         {
            LastErr = "SQLite: A view can not be drop";

            Logger.Instance.WriteSupportToLog("FilDel(): <<<<< SQLite: A view can not be drop", true);
            
            return GatewayErrorCode.CannotRemove;
         }

         int connectDbRetCode = ConnectDb(dbDefinition);

         if (connectDbRetCode == SqliteConstants.NO_DATABASE_NAME_GIVEN)
         {
            errcode = SqliteConstants.NO_DATABASE_NAME_GIVEN;
            Logger.Instance.WriteSupportToLog("FilDel(): <<<<< no database name given", true);
            return GatewayErrorCode.CannotRemove;
         }

         if (connectDbRetCode == SqliteConstants.SQLITE_NOTADB)
            return GatewayErrorCode.GetUserPassword;

         if (!DbdTbl.TryGetValue(dbh, out sql3Dbd))
         {
            sql3Connection = ConnectionTbl[dbDefinition.Location];

            if (!Sql3CheckTableName(fname))
            {
               LastErr = "SQLite: Table name too long";
               return GatewayErrorCode.CannotRemove;
            }

            else if (dbDefinition.Location.Length > SqliteConstants.SQL3_MAX_OBJECTNAME)
            {

               LastErr = "SQLite: database name is too long, maximum is %d characters";

               Logger.Instance.WriteSupportToLog(string.Format("FilDel(): <<<<< SQLite: database name is too long, maximum is %d characters\n", SqliteConstants.SQL3_MAX_OBJECTNAME), true);

               return GatewayErrorCode.CannotRemove;
            }

            Sql3GetFullName(out fullName, fullName.Length, dbh, dbDefinition, fname);

            errcode = Sql3DropObject(ConnectionTbl[dbDefinition.Location], fullName);

            switch (ServerErrCode)
            {
               case (int)SQLiteErrorCode.Full:
                  retcode = GatewayErrorCode.UpdateFail;
                  LastErr = "SQL3 Gateway: Insufficient Memory Error.";
                  break;
               case SqliteConstants.SQL3_OK:
                  if (errcode == SqliteConstants.S_False)
                  {
                     retcode = GatewayErrorCode.UpdateFail;
                  }
                  else
                  {
                     retcode = SqliteConstants.RET_OK;
                  }
                  break;
               case (int)SQLiteErrorCode.Constraint:
                  retcode = GatewayErrorCode.ConstraintFail;
                  break;
               case (int)SQLiteErrorCode.ReadOnly:
                  retcode = GatewayErrorCode.ReadOnly;
                  break;
               case (int)SQLiteErrorCode.Busy:
               case (int)SQLiteErrorCode.Locked:
                  retcode = GatewayErrorCode.FileLocked;
                  break;
               default:
                  retcode = GatewayErrorCode.UpdateFail;
                  break;
            }
         }
         else
         {
            return GatewayErrorCode.CannotRemove;
         }

         Logger.Instance.WriteSupportToLog(string.Format("FilDel(): <<<<< retcode = {0}\n", retcode), true);
         
         return (retcode);
      }

      /// <summary>
      ///  FilRecCount ()
      /// </summary>
      /// <param name="dbd"></param>
      /// <param name="count"></param>
      /// <returns>RET_CODE</returns>
      public GatewayErrorCode FilRecCount(DataSourceDefinition dbh, out int count)
      {
         count = 0;
         GatewayErrorCode returnCode = GatewayErrorCode.Any;
         return returnCode;
      }

      /// <summary>
      /// FileClose()
      /// </summary>
      /// <param name="dbh"></param>
      /// <returns></returns>
      public GatewayErrorCode FileClose(DataSourceDefinition dbh)
      {
         SQL3Dbd          sql3Dbd;
         GatewayErrorCode retcode   = SqliteConstants.RET_OK;
         SQL3_CODE        errcode   = SqliteConstants.SQL3_OK;

         DbdTbl.TryGetValue(dbh, out sql3Dbd);

         Logger.Instance.WriteSupportToLog(string.Format("FileClose(): >>>>> file : {0}", sql3Dbd.FullName), true);

         // build indexes on close if not done on open (for performance)
         if (sql3Dbd.mode == DbOpen.Reindex)
         {  
            sql3Dbd.DataSourceDefinition = dbh;
            errcode = AddIndexes (sql3Dbd, IndexingMode.REINDEX_CLOSE_MODE, false);
         }

         DbdTbl.Remove(dbh);

         Logger.Instance.WriteSupportToLog(string.Format("FileClose(): <<<<< retcode = {0}\n", retcode), true);

         return (retcode);
      }

      /// <summary>
      ///  CrsrPrepare() prepares SQL statements for fetch, get_curr, key_chk,
      ///  insert, update and delete operations. It also opens and prepares SQL3
      ///  cursors for the four fetch and two get_curr statements and a general
      ///  cursor to carry out all other activities. The function performs the
      ///  minimum of work to prepare the statements and attempts to maximize their
      ///  simplicity. If an SQL statement is given explicitly, the function only
      ///  makes enough provisions to execute (and fetch) that statement. If the
      ///  function fails to open one of the cursors it returns ERR_FATAL.
      /// </summary>
      /// <param name="dbCrsr"></param>
      /// <param name="dbDefinition"></param>
      /// <returns>RET_CODE</returns>
      public GatewayErrorCode CrsrPrepare (GatewayAdapterCursor dbCrsr, DatabaseDefinition dbDefinition)
      {
         GatewayCursor     crsr      = null;
         DBKey             key       = null;
         DBSegment         seg       = null;
         GatewayErrorCode  retCode   = GatewayErrorCode.Any;
         SQL3Dbd           sql3Dbd   = null;
         int               xtra      = 0,
                           keyIdx    = 0,
                           segIdx    = 0;
         SQL3Connection    sql3Connection  = null;

         Logger.Instance.WriteSupportToLog(string.Format("CrsrPrepare(): >>>>> database = {0}, key Name = {1}", dbDefinition.Location, dbCrsr.Definition.Key == null ? "No Key" : dbCrsr.Definition.Key.KeyDBName), true);

         if (DbdTbl.TryGetValue(dbCrsr.Definition.DataSourceDefinition, out sql3Dbd))
         {
            Logger.Instance.WriteSupportToLog(string.Format("CrsrPrepare(): table = {0}", sql3Dbd.TableName), true);
         }

         // prepare start new cursor so it should not be influence by 
         // unsuccesful previo-s operations                           
         crsr = InitGatewayCursor(dbCrsr);

         crsr.NumOfBlobs = dbCrsr.Definition.Blobs;

         Logger.Instance.WriteSupportToLog(string.Format("CrsrPrepare(): number of blobs = {0}", crsr.NumOfBlobs), true);

         if (DbdTbl.TryGetValue(dbCrsr.Definition.DataSourceDefinition, out sql3Dbd))
         {
            Logger.Instance.WriteDevToLog(string.Format("CrsrPrepare(): table = {0}", sql3Dbd.TableName));
            sql3Connection = ConnectionTbl[sql3Dbd.DatabaseName];
         }

         xtra = CountExtraFlds (dbCrsr);
         crsr.Output = new Sql3Sqldata(this);

         crsr.Output.SQL3SqldaAlloc((dbCrsr.Definition.FieldsDefinition.Count) + xtra);

         // allocate the space for dbPos 
         crsr.CurrPos.Alloc(sql3Dbd.posLen);
         crsr.LastPos.Alloc(sql3Dbd.posLen);
         crsr.DbPosBuf = new byte[sql3Dbd.posLen];

         Logger.Instance.WriteDevToLog(string.Format("CrsrPrepare(): crsr.PosKey.Name = {0}", crsr.PosKey == null ? "NoKey" : crsr.PosKey.KeyDBName));

         // build the field list without a prefix
         Statement = string.Empty;
         BuildFieldListStmt (dbCrsr, ref Statement, SqliteConstants.NO_PREFIX);
         crsr.StmtFields = Statement;

         // build the extra field list */
         Statement = string.Empty;
         BuildExtraFieldsStmt(dbCrsr, null);
         crsr.StmtExtraFields = Statement;

         // copy full table name to the crsr
         crsr.StmtAllTables = sql3Dbd.TableName;

         crsr.StmtAllTablesWithOtimizer = sql3Dbd.TableName;

         // if key check will be called, prepare an array of BOOLEANs indicating whether a key check can be done 
         // key check can be done only on unique keys which all the segments are in the magic data view          
         
         /* if (db_crsr->key_chk) */
         if (dbCrsr.Definition.IsFlagSet(CursorProperties.KeyCheck))
         {
            Logger.Instance.WriteDevToLog("CrsrPrepare(): db_crsr has key check");

            crsr.AllChkKeySegsInDataView = new List<bool>(dbCrsr.Definition.DataSourceDefinition.Keys.Count);

            for (keyIdx = 0; keyIdx < dbCrsr.Definition.DataSourceDefinition.Keys.Count; keyIdx++)
            {
               key = dbCrsr.Definition.DataSourceDefinition.Keys[keyIdx];
               if (key.CheckMask(KeyMasks.UniqueKeyModeMask))
               {
                  crsr.AllChkKeySegsInDataView.Add(true);

                  for (segIdx = 0; segIdx < key.Segments.Count; ++segIdx)
                  {
                     seg = key.Segments[segIdx];
                     DBField fld = seg.Field;
                     if (!Sql3IsDbCrsrField(dbCrsr, fld) && sql3Dbd!= null && sql3Dbd.IsView)
                     {
                        crsr.AllChkKeySegsInDataView[keyIdx] = false;
                        break;
                     }
                  }
               }
               else
                  crsr.AllChkKeySegsInDataView.Add(false);
            }
         }

         crsr.Output.SQL3SqldaOutput(dbCrsr);

         Logger.Instance.WriteSupportToLog(string.Format("CrsrPrepare(): <<<<< retcode = {0}\n", retCode), true);

         return retCode;
      }

      /// <summary>
      /// CrsrRelease ()
      /// </summary>
      /// <param name="dbCrsr"></param>
      /// <returns></returns>
      public GatewayErrorCode CrsrRelease (GatewayAdapterCursor dbCrsr)
      {
         GatewayCursor            crsr;
         SQL3Dbd                  sql3Dbd;
         
         GatewayErrorCode         retcode = SqliteConstants.RET_OK;

         Logger.Instance.WriteSupportToLog("CrsrRelease(): >>>>> ", true);

         GatewayCursorTbl.TryGetValue(dbCrsr, out crsr);
         
         DbdTbl.TryGetValue(dbCrsr.Definition.DataSourceDefinition, out sql3Dbd);


         // mark the gateway cursor as not in use.
         crsr.InUse = false;

         // Close the cursor only if the connection is established successfully
         FreeStatements(crsr);

         // allocated in prepare 

         if (crsr.Output != null)
         {
            crsr.Output.SQL3SqldaFree();
         }

         if (crsr.Input != null)
         {
            crsr.Input.SQL3SqldaFree();
         }

         if (crsr.Ranges != null)
         {
            crsr.Ranges.SQL3SqldaFree();
         }

         if(crsr.Key != null)
         {
            crsr.Key.SQL3SqldaFree();
         }
            
         if(crsr.StartPos != null)
         {
            crsr.StartPos.SQL3SqldaFree();
         }
            
         if(crsr.SearchKey != null)
         {
            crsr.SearchKey.SQL3SqldaFree();
         }

         if (crsr.GcurrInput != null)
         {
            crsr.GcurrInput.SQL3SqldaFree();
         }

         if (crsr.GcurrOutput != null)
         {
            crsr.GcurrOutput.SQL3SqldaFree();
         }

         if (crsr.Update != null)
         {
            crsr.Update.SQL3SqldaFree();
         }

         if (crsr.CGcurr != SqliteConstants.NULL_CURSOR)
         {
            Sql3CursorRelease(crsr.CGcurr);
         }

         if (crsr.CGkey != SqliteConstants.NULL_CURSOR)
         {
            Sql3CursorRelease(crsr.CGkey);
         }

         if (crsr.CInsert != SqliteConstants.NULL_CURSOR)
         {
            Sql3CursorRelease(crsr.CInsert);
         }

         if (crsr.CRead != SqliteConstants.NULL_CURSOR)
         {
            Sql3CursorRelease(crsr.CRead);
         }

         if (crsr.CRange != SqliteConstants.NULL_CURSOR)
         {
            Sql3CursorRelease(crsr.CRange);
         }

         // statements 

         Logger.Instance.WriteDevToLog("CrsrRelease(): freeing gateway cursor statements ");

         Logger.Instance.WriteDevToLog(string.Format("CrsrRelease(): keyDbName - {0}", dbCrsr.Definition.Key == null ? "No Key" : dbCrsr.Definition.Key.KeyDBName));

         Logger.Instance.WriteDevToLog(string.Format("CrsrRelease(): freeing startpos for {0} segments", crsr.StmtStartpos.Count));

         crsr.StmtStartpos = null;
            
         crsr.StmtOrderBy = string.Empty;

         crsr.StmtOrderByRev = string.Empty;
            
         crsr.StmtInsert = string.Empty;

         crsr.StmtDelete = string.Empty;

         crsr.StmtUpdate = string.Empty;

         crsr.StmtWhereKey = string.Empty;
            
         crsr.StmtFields = string.Empty;            
            
         crsr.StmtExtraFields = string.Empty;
            
         crsr.StmtKeyFields = string.Empty;

         crsr.StmtAllTables = string.Empty;

         crsr.StmtAllTablesWithOtimizer = string.Empty;

         crsr.StmtAllTablesUpdLock = string.Empty;

         crsr.NullIndicator = null;

         crsr.KeyArray = null;

         GatewayCursorTbl.Remove(dbCrsr);

         Logger.Instance.WriteSupportToLog(string.Format("CrsrRelease(): <<<<< retcode = {0}\n", retcode), true);

         return retcode;
      }

      /// <summary>
      /// free the statements of this crsr
      /// </summary>
      /// <param name="crsr"></param>
      void FreeStatements(GatewayCursor crsr)
      {
         FreeStatement(crsr.SDelete);
         FreeStatement(crsr.SGCurr);
         FreeStatement(crsr.SGCurrlock);
         FreeStatement(crsr.SGKey);
         FreeStatement(crsr.SInsert);
         FreeStatement(crsr.SReadA);
         FreeStatement(crsr.sReadD);
         FreeStatement(crsr.SRngA);
         FreeStatement(crsr.SRngD);
         FreeStatement(crsr.SStrt);
         FreeStatement(crsr.SUpdate);
      }

      /// <summary>
      /// free a statement
      /// </summary>
      /// <param name="i"></param>
      void FreeStatement(int i)
      {
         if (i == SqliteConstants.NULL_STMT)
            return;

         Sql3Stmt sql3Stmt = StmtTbl[i];
         if (sql3Stmt.InUse)
         {
            /* free the statement - add routine to drop the command */
            SQLiteLow.LibReleaseStmt(sql3Stmt);
            sql3Stmt.InUse = false;
            sql3Stmt.IsPrepared = false;
            return;
         }
      }

      /// <summary>
      ///  CrsrOpen() decides which of the four fetch cursors will be used and
      ///  prepares the selected cursor for fetching. If DB_CRSR holds a position
      ///  then the fetch is done using a key and in relation to another row -
      ///  one of the _strt cursors; if dir_reversed is true the direction is
      ///  descending. Bind the ranges to the fetch cursor using the same
      ///  selector table and criteria as when creating the statment (do not bind
      ///  memos as ranges); do not bind to the first place-holder it is reserved
      ///  for rowid. The fetch cursor is executed and is ready to fetch the first
      ///  row with the next call to sql3_crsr_fetch(). If an SQL statement is given,
      ///  just execute the statement (it has no ranges associated with it), and
      ///  get ready for a possible fetch. Allowed errors are ERR_FATAL.
      /// </summary>
      /// <param name="dbCrsr"></param>
      /// <returns>RET_CODE</returns>
      public GatewayErrorCode CrsrOpen (GatewayAdapterCursor dbCrsr)
      {
         GatewayCursor        crsr;
         SQL3Dbd              sql3Dbd; 
         bool                 dirOrig        = true,
                              strtPos        = !dbCrsr.Definition.StartPosition.IsZero,
                              hasRange       = ((dbCrsr.Ranges.Count > 0 || dbCrsr.SqlRng != null) ? true : false);

         bool                 range          = hasRange;
         SQL3_CODE            errorCode      = 0;
         GatewayErrorCode     returnCode     = GatewayErrorCode.Any;
         SQL3Cursor           sql3Cursor     = null;
         DBKey                sortKey        = null;
         DBKey                originalKey    = null;
         SQL3Connection       sql3Connection = null;
         Sql3Stmt             sql3Stmt       = null;


         ServerErrCode = SqliteConstants.SQL3_OK;
         GatewayCursorTbl.TryGetValue(dbCrsr, out crsr);
         Statement = string.Empty;

         DbdTbl.TryGetValue(dbCrsr.Definition.DataSourceDefinition, out sql3Dbd);

         sql3Connection = ConnectionTbl[sql3Dbd.DatabaseName];

         {
            Logger.Instance.WriteSupportToLog(string.Format("CrsrOpen(): >>>>> db = {0}, table = {1}, dir = {2}, dirRev = {3} , rngs = {4}, Range:{5}, Start Pos:{6}", sql3Connection.DbName, sql3Dbd.FullName, dbCrsr.Definition.Direction,
               dbCrsr.Definition.IsFlagSet(CursorProperties.DirReversed), dbCrsr.Ranges.Count, hasRange ? "TRUE" : "FALSE", strtPos ? "TRUE" : "FALSE"), true);
            
            /* determine the direction of the cursor */
            /* dir_orig inited to TRUE */
            /* only change if necessary */
            if (dbCrsr.Definition.Direction == Order.Ascending)
               if (dbCrsr.Definition.IsFlagSet(CursorProperties.DirReversed))
                  dirOrig = false;

            if (dbCrsr.Definition.Direction == Order.Descending)
               if (!dbCrsr.Definition.IsFlagSet(CursorProperties.DirReversed))
                  dirOrig = false;

            if (crsr.StmtOrderBy == null)
            {
               // build the ORDER BY clause for the sort key and copy it to GTWY_cursor
               if (dbCrsr.Definition.Key == null)
                  sortKey = null;
               else
                  sortKey = dbCrsr.Definition.Key;

               BuildOrderByStmt (dbCrsr, ref Statement, sortKey);

               if (!string.IsNullOrEmpty(Statement))
               {
                  if (dbCrsr.Definition.Direction == Order.Ascending)
                  {
                     crsr.StmtOrderBy = Statement;
                     crsr.StmtOrderByRev = SQL3StmtReverseOrder(Statement);
                  }
                  else
                  {
                     crsr.StmtOrderByRev= Statement;
                     crsr.StmtOrderBy = SQL3StmtReverseOrder(Statement);
                  }
               }
            }

            if (range)
            {
               crsr.CRange = Sql3CursorAlloc ("Range", crsr.CRange);
               sql3Cursor = CursorTbl[crsr.CRange];
            }
            else
            {
               crsr.CRead = Sql3CursorAlloc("Read", crsr.CRead);
               sql3Cursor = CursorTbl[crsr.CRead];
            }

         /* The settings for cursor/command & cursor type is done after the cursor is closed & reopened*/

         /*---------------------------------------------------------------------*/
         /* This test was added in order by pass the problem of last_pos being  */
         /* associated with crsr while there are two physical cursors.          */ 
         /*---------------------------------------------------------------------*/
            {
               if (range == sql3Cursor.IsRange)
               {
                  Logger.Instance.WriteDevToLog("CrsrOpen(): ranges are the same");
                  
                  if (!sql3Cursor.DoOpen)
                  {
                     Logger.Instance.WriteDevToLog("CrsrOpen(): no need to open cursor");
                     
                     /* and the direction remains the same */
                     if (dirOrig == crsr.DirOrig)
                     {
                        Logger.Instance.WriteDevToLog("CrsrOpen(): same direction");
                        
                        if (!dbCrsr.Definition.StartPosition.IsZero)
                        {
                           Logger.Instance.WriteDevToLog("CrsrOpen(): start position exist");
                           
                           /* if the saved start position is equal to the current */
                           if (crsr.LastPos.Equals(dbCrsr.Definition.StartPosition))
                           {
                              Logger.Instance.WriteSupportToLog(string.Format("CrsrOpen(): <<<<< reusing cursor {0}\n", sql3Cursor.Name), true);
                              
                              sql3Cursor.DoDummyFetch = true;
                              sql3Cursor.DoDummyFetchBlob = true;
                              return (SqliteConstants.RET_OK);
                           }
                        }
                     }
                  }
               }
            }

            errorCode = BeginTransactionIfNeeded(sql3Connection);

            /* close the cursor if it is still open */
            if (sql3Cursor.StmtIdx != SqliteConstants.NULL_STMT)
            {
               sql3Stmt = StmtTbl[sql3Cursor.StmtIdx];
               if (sql3Stmt.IsOpen)
                  errorCode = SQLiteLow.LibClose (ConnectionTbl[sql3Dbd.DatabaseName], sql3Stmt);
            }

            crsr.Rngs = 0;
            sql3Cursor.IsStartPos = false;

            /* zero the cascading startpos level counter */
            sql3Cursor.StartPosLevel = 0;

            Logger.Instance.WriteDevToLog(string.Format("CrsrOpen(): is_open - {0}", (sql3Stmt != null && sql3Stmt.IsOpen) ? "TRUE" : "FALSE"));

            sql3Cursor.DoOpen = false;
            sql3Cursor.DoDummyFetch = false;
            sql3Cursor.DoDummyFetchBlob = false;

            crsr.DirOrig = dirOrig;
           
            if (errorCode == SqliteConstants.SQL3_OK)
            {
               crsr.FirstFetchAfterOpen = false;
               // use which statement? 
               // NO START POSITION 
               if (!strtPos)
                  if (dbCrsr.Ranges.Count > 0 || dbCrsr.SqlRng != null)
               // RANGE 
                     errorCode = RangeOpen (dbCrsr, dirOrig);
                  else
                     errorCode = NoStartPosOpen (dbCrsr, dirOrig);
               // START POSITION 
               else
               {
                  originalKey = dbCrsr.Definition.Key;
                  if (originalKey == null && crsr.PosKey != null)
                     dbCrsr.Definition.Key = crsr.PosKey;

                  errorCode = StartPosOpen (dbCrsr, false, dirOrig);

                  dbCrsr.Definition.Key = originalKey;
               }
            }
         }


         switch (errorCode)
         {
            case SqliteConstants.SQLITE_OK:
               returnCode = GatewayErrorCode.Any;
               break;
            case SqliteConstants.SQLITE_DONE:
               returnCode = GatewayErrorCode.LostRecord;
               break;
            default:
               if (ServerErrCode > 0)
               {
                  returnCode = GatewayErrorCode.BadOpen;
                  break;
               }
               break;
         }

         Logger.Instance.WriteSupportToLog(string.Format("CrsrOpen(): <<<<< retcode = {0}\n", returnCode), true);

         return (returnCode);
      }

      /// <summary>
      ///  CrsrClose()
      /// </summary>
      /// <param name="dbCrsr"></param>
      /// <returns>RET_CODE</returns>
      public GatewayErrorCode CrsrClose(GatewayAdapterCursor dbCsr)
      {
         GatewayCursor                    crsr;
         bool                             strtPos                = !(dbCsr.Definition.StartPosition.IsZero),
                                          hasRange               = (dbCsr.Ranges.Count > 0 ? true : false),
                                          range                  = (hasRange && !strtPos);
         SQL3Dbd                          sql3Dbd ;
         SQL3Cursor                       sql3Cursor;
         Sql3Stmt                         sql3Stmt;
         SQL3Connection                   sql3Connection;
         GatewayErrorCode                 retcode                 = SqliteConstants.RET_OK;

         Logger.Instance.WriteSupportToLog("CrsrClose(): >>>>>", true);
         
         GatewayCursorTbl.TryGetValue(dbCsr, out crsr);

         if (DbdTbl.TryGetValue(dbCsr.Definition.DataSourceDefinition, out sql3Dbd))
         {
            
            sql3Connection = ConnectionTbl[sql3Dbd.DatabaseName];

            if (dbCsr.Definition.LimitTo == 1 )
            {
               if (range)
               {
                  crsr.CRange = Sql3CursorAlloc("Range", crsr.CRange);
                  sql3Cursor = CursorTbl[crsr.CRange];
               }
               else
               {
                  crsr.CRead = Sql3CursorAlloc("Range", crsr.CRead);
                  sql3Cursor = CursorTbl[crsr.CRead];
               }

               // close the cursor if it is still open 
               if (sql3Cursor.StmtIdx > 0)
               {
                  sql3Stmt = StmtTbl[sql3Cursor.StmtIdx];
                  if(sql3Stmt.IsOpen) 
                  {
                     SQLiteLow.LibClose(ConnectionTbl[sql3Dbd.DatabaseName], sql3Stmt);
                  }
               }
            }
         }

         Logger.Instance.WriteSupportToLog("CrsrClose(): <<<<< retcode = RET_OK\n", true);
         return retcode;
      }

      /// <summary>
      ///  CrsrFetch()
      /// </summary>
      /// <param name="dbCrsr"></param>
      /// <returns>RET_CODE</returns>
      public GatewayErrorCode CrsrFetch (GatewayAdapterCursor dbCrsr)
      {
         GatewayCursor        crsr;
         SQL3_CODE            errorCode      = SqliteConstants.SQL3_OK;
         GatewayErrorCode     returnCode     = GatewayErrorCode.Any;
         SQL3Dbd              sql3Dbd        = null; 
         bool                 dirOrig        = true,
                              hasRange       = ((dbCrsr.Ranges.Count > 0 || dbCrsr.SqlRng != null) ? true : false),
                              startPos       = !dbCrsr.Definition.StartPosition.IsZero;
         SQL3Cursor           sql3Cursor        = null;
         Sql3Stmt             sql3Stmt       = null;
         SQL3Connection       sql3Connection = null;
         DbPos                pos            = new DbPos(true);
         DBKey                originalKey    = null;

         GatewayCursorTbl.TryGetValue(dbCrsr, out crsr);

         Logger.Instance.WriteSupportToLog(string.Format("CrsrFetch(): >>>>> lock = {0}", dbCrsr.Definition.IsFlagSet(CursorProperties.CursorLock) ? "TRUE" : "FALSE"), true);

         if (DbdTbl.TryGetValue(dbCrsr.Definition.DataSourceDefinition, out sql3Dbd))
         {
            sql3Connection = ConnectionTbl[sql3Dbd.DatabaseName];
         }

         ServerErrCode = SqliteConstants.SQL3_OK;
         crsr.DummyFetchWasDone = false;

         {
            crsr.CheckForModifiedRow = false;

            if(hasRange) 
            {
               crsr.CRange = Sql3CursorAlloc ("Range", crsr.CRange);
               sql3Cursor = CursorTbl[crsr.CRange];
            }
            else
            {
               crsr.CRead = Sql3CursorAlloc ("Read", crsr.CRead);
               sql3Cursor = CursorTbl[crsr.CRead];
            }

            sql3Stmt = StmtTbl[sql3Cursor.StmtIdx];

            // if we have reused a cursor
            if (sql3Cursor.DoDummyFetch)
            {
               dbCrsr.Definition.CurrentPosition.Set(crsr.LastPos.Get());

               sql3Cursor.DoDummyFetch = false;
               // indicate that a dummy_fetch was done (use in join) 
               crsr.DummyFetchWasDone = true;

               dbCrsr.Definition.SetFlag(CursorProperties.DummyFetch);

               Logger.Instance.WriteSupportToLog("CrsrFetch(): <<<<< done dummy fetch\n", true);

               return (SqliteConstants.RET_OK);
            }

            if (dbCrsr.Definition.Direction == Order.Ascending)

               if (dbCrsr.Definition.IsFlagSet(CursorProperties.DirReversed))
                  dirOrig = false;

            if (dbCrsr.Definition.Direction == Order.Descending)

               if (!dbCrsr.Definition.IsFlagSet(CursorProperties.DirReversed))
                  dirOrig = false;

            errorCode = BeginTransactionIfNeeded(sql3Connection);

            // if the cursor has been closed by a commit, reopen it 
            if (!sql3Stmt.IsOpen)
            {
               originalKey = dbCrsr.Definition.Key;
               if (originalKey == null && crsr.PosKey != null)
                  dbCrsr.Definition.Key = crsr.PosKey;

               errorCode = StartPosOpen (dbCrsr, true, dirOrig);

               dbCrsr.Definition.Key = originalKey;
               // another cursor may have been allocated so re-set the Pcursor pointer 
               if (hasRange)
                  sql3Cursor = CursorTbl[crsr.CRange];
               else
                  sql3Cursor = CursorTbl[crsr.CRead];
               // after the startpos we need to do an extra fetch for the previos record 
               errorCode = SQLiteLow.LibFetch (sql3Cursor, crsr.Output);
            }

            if (errorCode == SqliteConstants.SQL3_OK)
            {
               errorCode = SQLiteLow.LibFetch (sql3Cursor, crsr.Output);

               {
                  //QCR # 787160 : Here problem is with the sqlite behavior. When a statement is fetching the records from one table, 
                  //and if we use another statement to update the record from the same table, the updated record is re-fetched. This 
                  //depens upon the Order by clause used in the stmt. SQLite suggests not to go for fetching and udpating the records 
                  //from the same table simultaniously, using a single connection. However, here the fix is provided as a work around. 
                  //We check whether we are fetching a duplicate record, in crsr_fetch () and if so, 
                  //we skip that record and will be fetching the next record

                  if (!startPos && errorCode == SqliteConstants.SQL3_OK && dbCrsr.CursorType != CursorType.Join)
                  {
                     // Compare with last pos only after first fetch is done after crsrOpen
                     if (!crsr.FirstFetchAfterOpen)
                        crsr.FirstFetchAfterOpen = true;
                     else
                     {
                        BuildDbPos (pos, dbCrsr, crsr.Output);

                        if(pos.Equals(crsr.LastPos))
                        {
                           errorCode = SQLiteLow.LibFetch (sql3Cursor, crsr.Output);
                        }

                     }
                  }

                  // REOPEN - If esqlc_fetch has returned SQL3_REOPEN , we have to reopen the cursor & stmt
                  if(errorCode == SqliteConstants.SQL3_REOPEN)
                  {
                     dbCrsr.Definition.StartPosition.Set(crsr.LastPos.Get());

                     StartPosOpen (dbCrsr, true, dirOrig);

                     if (hasRange)
                        sql3Cursor = CursorTbl[crsr.CRange];
                     else
                        sql3Cursor = CursorTbl[crsr.CRead];
                     errorCode = SQLiteLow.LibFetch (sql3Cursor, crsr.Output);
                  }

                  if (errorCode == SqliteConstants.SQL3_OK)
                  {
                     if (Logger.Instance.LogLevel >= Logger.LogLevels.Development && Logger.Instance.LogLevel != Logger.LogLevels.Basic)
                     {
                        SQLLogging.SQL3LogSqlda(crsr.Output, "crsr->output - output of fetch");
                     }

                     // move the data from the sqlda to the db_crsr->buf 
                     crsr.Output.SQL3SqldaToBuff (dbCrsr);

                     // update the DB_CRSR null vector from the SQLVAR sqlind
                     crsr.Output.SQL3SqlindToBuff(dbCrsr, true);

                     BuildDbPos(dbCrsr.Definition.CurrentPosition, dbCrsr, crsr.Output);

                     crsr.LastPos.Set(dbCrsr.Definition.CurrentPosition.Get());

                     // close the command after it is done. This is needed as we cant open two 
                     // commands on the same session inside a transaction.
                     if (dbCrsr.Definition.LimitTo == 1) // && !SQL3RangeMemo(db_crsr)
                     {
                        if (errorCode == SqliteConstants.SQL3_OK)
                           errorCode = SQLiteLow.LibClose(ConnectionTbl[sql3Dbd.DatabaseName], sql3Stmt);
                        else
                           SQLiteLow.LibClose(ConnectionTbl[sql3Dbd.DatabaseName], sql3Stmt);
                     }
                  }
                  else
                  {
                     dbCrsr.Definition.CurrentPosition.SetZero();
                  }
               }
            }

            Logger.Instance.WriteDevToLog(string.Format("CrsrFetch(): sql3Cursor.IsStartPos = {0}", sql3Cursor.IsStartPos));

            switch (errorCode)
            {
            case SqliteConstants.SQL3_OK:
               returnCode = GatewayErrorCode.Any;
               break;
            case (int)SQLiteErrorCode.Full:
               returnCode = GatewayErrorCode.UpdateFail;
               LastErr =  "SQL3 Gateway: Insufficient Memory Error.";
               break;
            case (int)SQLiteErrorCode.Busy:
            case (int)SQLiteErrorCode.Locked:
               returnCode = GatewayErrorCode.FileLocked;
               break;
            case (int)SqliteConstants.SQL3_SQL_NOTFOUND:
               sql3Cursor.DoOpen = true;

               Logger.Instance.WriteDevToLog("CrsrFetch(): no more records, do_open = TRUE, results pending = FALSE");

               if (sql3Cursor.IsStartPos)
               {
                  Logger.Instance.WriteDevToLog(string.Format("CrsrFetch(): sql3Cursor.StartPosLevel = {0},  crsr.StrtposCnt = {1}", sql3Cursor.StartPosLevel, crsr.StrtposCnt));

                  sql3Cursor.StartPosLevel++;

                  if (sql3Cursor.StartPosLevel < crsr.StrtposCnt)
                  {
                     originalKey = dbCrsr.Definition.Key;
                     if (originalKey == null && crsr.PosKey != null)
                        dbCrsr.Definition.Key = crsr.PosKey;

                     errorCode = StartPosReopen (dbCrsr, dirOrig);

                     dbCrsr.Definition.Key = originalKey;
                     // If open succeeded 
                     if (errorCode == SqliteConstants.SQL3_OK)
                     {
                        sql3Cursor.DoOpen = false;
                        returnCode = CrsrFetch(dbCrsr);
                     }
                     else if (errorCode == SqliteConstants.SQL3_SQL_NOTFOUND)
                        returnCode = GatewayErrorCode.NoRecord;
                     else
                        returnCode = GatewayErrorCode.Fatal;
                  }
                  else
                     returnCode = GatewayErrorCode.NoRecord;
               }
               else
                  returnCode = GatewayErrorCode.NoRecord;
               break;
            case (int)SQLiteErrorCode.Error:

               SQLiteLow.LibErrorhandler (sql3Connection);
               returnCode = GatewayErrorCode.BadOpen;
               break;
            default:

               returnCode = GatewayErrorCode.BadOpen;
               break;
            }
         }

         Logger.Instance.WriteSupportToLog(string.Format("CrsrFetch(): <<<<< returnCode = {0}\n", returnCode), true);

         return (returnCode);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="db_crsr"></param>
      /// <returns></returns>
      public GatewayErrorCode CrsrGetCurr (GatewayAdapterCursor dbCrsr)
      {
         GatewayCursor           crsr;
         SQL3Dbd                 sql3Dbd; 
         SQL3_CODE               errorCode           = SqliteConstants.SQL3_OK;
         GatewayErrorCode        returnCode           = GatewayErrorCode.Any;
         SQL3Cursor              sql3Cursor; 
         Sql3Stmt                sql3Stmt;
         bool                    doPrepare        = true;
         SQL3Connection          connection = null; 
         int                     crsrHdl;
         int                     cnt               = 0,
                                 keyCnt,
                                 rngsCnt          = 0;
         bool                    releaseCmdOutsideTrans = false;
         string                  prefix           = null;   
         string                  noPrefix         = string.Empty;

         long                    origSqln         = 0;

   
         GatewayCursorTbl.TryGetValue(dbCrsr, out crsr);

         Logger.Instance.WriteSupportToLog("CrsrGetCurr(): >>>>>", true);

         if (crsr.GcurrOutput == null)
            crsr.GcurrOutput = new Sql3Sqldata(this);

         if (crsr.GcurrInput == null)
            crsr.GcurrInput = new Sql3Sqldata(this);

         if (DbdTbl.TryGetValue(dbCrsr.Definition.DataSourceDefinition, out sql3Dbd))
         {
            connection = ConnectionTbl[sql3Dbd.DatabaseName];
         }
     
         Logger.Instance.WriteSupportToLog(string.Format("CrsrGetCurr(): database = {0}, table name = {1}, lock = {2}", connection.DbName, sql3Dbd.FullName,
            dbCrsr.Definition.IsFlagSet(CursorProperties.CursorLock) ? "TRUE" : "FALSE"), true);

         ServerErrCode = SqliteConstants.SQL3_OK;

         crsr.CGcurr = Sql3CursorAlloc("Gcurr", crsr.CGcurr);
         sql3Cursor = CursorTbl[crsr.CGcurr];

         prefix = noPrefix;

         if (dbCrsr.Definition.IsFlagSet(CursorProperties.CursorLock))
         {
            crsr.SGCurrlock = Sql3StmtAlloc ("sGcurrlock", crsr.SGCurrlock, sql3Dbd.DatabaseName);
            sql3Stmt = StmtTbl[crsr.SGCurrlock];
         }
         else
         {
            crsr.SGCurr = Sql3StmtAlloc ("sGcurr", crsr.SGCurr, sql3Dbd.DatabaseName);
            sql3Stmt = StmtTbl[crsr.SGCurr];
         }

         /* save the DB_POS  received from magic */
         crsr.CurrPos = dbCrsr.Definition.CurrentPosition;

         errorCode = BeginTransactionIfNeeded(connection);

         // QCR# 291177. Since binding, we reuse the lock commands. In qcr case, we open 2 lock 
         // commands and dont free it (so as to reuse it), And later on, while opening a trans 
         // in fetch(), it fails (max trans exceeded). It seems, we cant open a trans if 
         // multiple commands are already opened on a session (i.e. commands are opened outside 
         // a trans). So, dont reuse a lock cmd if we are outside a transaction.
         if (!TransToOpen)
            releaseCmdOutsideTrans = true;


         // join ranges should be recomputed for get current, they might have been
         // changed since last crsr_open

         if (dbCrsr.CursorType == CursorType.Join && dbCrsr.Ranges.Count > 0)
         {
            if (crsr.OuterJoin || crsr.JoinStmtBuiltWithInnerJoin)
               crsrHdl = 0;
            else
               crsrHdl = -1;

            rngsCnt = BuildRangesStmt (dbCrsr, true, crsrHdl);

            if (!string.IsNullOrEmpty(crsr.StmtJoinRanges))
            {
               doPrepare = crsr.StmtJoinRanges == Statement ? false : true;
            }
        
            if (doPrepare)
            {
               crsr.StmtJoinRanges = string.Empty;
               if (!string.IsNullOrEmpty(Statement))
                  crsr.StmtJoinRanges = Statement;
            }
         }

         if (sql3Dbd.IsView)
         {
            keyCnt = BuildWhereViewStmt(dbCrsr, dbCrsr.Definition.CurrentPosition, 0);
         }
         else
         {
            keyCnt = 1;
         }

         if (crsr.StmtWhereKey != null)
         {
            doPrepare = (crsr.StmtWhereKey == Statement ? false : true);
         }
         else
         {
            doPrepare = true;
         }
         if (doPrepare)
         {
            crsr.StmtWhereKey = Statement;
         }

         if (! sql3Stmt.IsPrepared|| doPrepare)
         {
            // if prev get_curr failed to close - close cursor now.
            if (sql3Stmt.IsOpen)
            {
               SQLiteLow.LibClose(ConnectionTbl[sql3Dbd.DatabaseName], sql3Stmt);
            }

            if (sql3Cursor.OutputSqlda != null)
               sql3Cursor.OutputSqlda.SQL3SqldaFree();
            if (sql3Cursor.InputSqlda != null)
               sql3Cursor.InputSqlda.SQL3SqldaFree();

            if (sql3Dbd.IsView)
            {
               crsr.GcurrOutput.SQL3SqldaAlloc(dbCrsr.Definition.FieldsDefinition.Count);
               sql3Cursor.OutputSqlda = crsr.GcurrOutput;
               sql3Cursor.OutputSqlda.SQL3SqldaCurrOutput (dbCrsr);

               //SQL3UpdateWhereFromDbpos (db_crsr, db_crsr->curr_pos, (char *)SQL3_stmt, 0);     
               crsr.CheckForModifiedRow = dbCrsr.Definition.IsFlagSet(CursorProperties.CursorLock);

               if (keyCnt + rngsCnt > 0)
               {
                  if(crsr.GcurrInput == null)
                  {
                     crsr.GcurrInput = new Sql3Sqldata(this);
                  }

                  crsr.GcurrInput.SQL3SqldaAlloc(keyCnt + rngsCnt);
               }

               sql3Cursor.InputSqlda = crsr.GcurrInput;
         
               //  Gautam B. (23/05/99) Added  - For Locking Implementation
               // UPDLOCK_OUTER
               // Bugfix 441535 - Physical Lock on MS6_DBD is file dependent, so we should considr Join differently
               /* **************************************************************************** */
               /* when we build the stmt the order is : join ranges + pos + join cond          */
               /* **************************************************************************** */
               if (!string.IsNullOrEmpty(crsr.StmtExtraFields))
               {
                  if (dbCrsr.CursorType == CursorType.Join)
                  {
                     Statement = string.Format("SELECT {0},{1} FROM {2} WHERE", crsr.StmtFields, crsr.StmtExtraFields, crsr.StmtAllTablesUpdLock);
                  }
                  else
                  {
                     Statement = string.Format("SELECT {0},{1} FROM {2} WHERE", crsr.StmtFields, crsr.StmtExtraFields, crsr.StmtAllTables);
                  }
               }
               else
               {
                  Statement = string.Format("SELECT {0}{1} FROM {2} WHERE", crsr.StmtFields, crsr.StmtExtraFields, crsr.StmtAllTables);
               }

               if (dbCrsr.CursorType == CursorType.Join && crsr.StmtJoinRanges != null)
               {
                  Statement += string.Format(" ({0}) AND ({1})", crsr.StmtJoinRanges, crsr.StmtWhereKey);
               }
               else
               {
                  Statement += string.Format(" ({0})", crsr.StmtWhereKey);
               }

               if (dbCrsr.CursorType == CursorType.Join)
               {
                  if (crsr.StmtJoinCond != null)
                  {
                     Statement += string.Format(" AND ({0})", crsr.StmtJoinCond);
                  }
               }
            }
            else
            {
               if (dbCrsr.CursorType == CursorType.Join)
               {
                  crsr.GcurrOutput.SQL3SqldaAlloc(dbCrsr.Definition.FieldsDefinition.Count);
               }
               else
               {
                  crsr.GcurrOutput.SQL3SqldaAlloc(dbCrsr.Definition.FieldsDefinition.Count);
               }

               sql3Cursor.OutputSqlda = crsr.GcurrOutput;

               sql3Cursor.OutputSqlda.SQL3SqldaCurrOutput (dbCrsr);

               crsr.GcurrInput.SQL3SqldaAlloc(rngsCnt + 1);
               sql3Cursor.InputSqlda = crsr.GcurrInput;

               sql3Cursor.InputSqlda.SqlVars[rngsCnt].SQL3SqlvarTid();

               if (dbCrsr.CursorType == CursorType.Join)
               {
                  prefix = SQL3PrefixBuf;
                  prefix = string.Format("{0}.", crsr.DbhPrefix[0]);
               }

               if (!string.IsNullOrEmpty(crsr.StmtExtraFields))
               {
                  if (dbCrsr.CursorType == CursorType.Join)
                  {
                     Statement = string.Format("SELECT {0},{1} FROM {2} WHERE", crsr.StmtFields, crsr.StmtExtraFields, crsr.StmtAllTablesUpdLock);
                  }
                  else
                  {
                     Statement = string.Format("SELECT {0},{1} FROM {2} WHERE", crsr.StmtFields, crsr.StmtExtraFields, crsr.StmtAllTables);
                  }
               }
               else
               {
                  Statement = string.Format("SELECT {0} FROM {1} WHERE", crsr.StmtFields, crsr.StmtAllTables, prefix);
               }

               if (dbCrsr.CursorType == CursorType.Join && crsr.StmtJoinRanges != null)
               {
                  Statement += string.Format(" ({0}) AND {1}rowid = ?", crsr.StmtJoinRanges, prefix);
               }
               else
               {
                  Statement += string.Format(" {0}rowid = ?", prefix);
               }

               if (dbCrsr.CursorType == CursorType.Join)
               {
                  if (crsr.StmtJoinCond != null)
                  {
                     Statement += string.Format(" AND ({0})", crsr.StmtJoinCond);
                  }
               }
            }

            if (! sql3Stmt.IsPrepared || doPrepare)
            {
               sql3Stmt.Buf = Statement;
            }

            sql3Cursor.StmtIdx = sql3Stmt.Idx;
            sql3Cursor.InputSqlda = crsr.GcurrInput;
            cnt = 0;

            if (rngsCnt > 0)
            {
               sql3Cursor.InputSqlda.SQL3SqldaRange(dbCrsr, SqliteConstants.ONLY_LINKS_RNG, false);
            }

            sql3Cursor.InputSqlda.SQL3SqldaFromDbpos(dbCrsr, rngsCnt, dbCrsr.Definition.CurrentPosition, false, false);

            errorCode = SQLiteLow.LibPrepare(ConnectionTbl[sql3Dbd.DatabaseName], sql3Stmt, sql3Cursor);
         }
         else
         {
            if (rngsCnt > 0)
            {
               sql3Cursor.InputSqlda.SQL3SqldaRange(dbCrsr, SqliteConstants.ONLY_LINKS_RNG, true);
            }

            cnt = sql3Cursor.InputSqlda.SQL3SqldaFromDbpos(dbCrsr, rngsCnt, dbCrsr.Definition.CurrentPosition, false, true);

            if (!sql3Stmt.IsOpen)
            {
               errorCode = SQLiteLow.LibPrepare(ConnectionTbl[sql3Dbd.DatabaseName], sql3Stmt, sql3Cursor);
            }

         }

         if (errorCode == SqliteConstants.SQL3_OK)
         {
            if (dbCrsr.Definition.IsFlagSet(CursorProperties.CursorLock))
            {
               sql3Cursor.StmtIdx = crsr.SGCurrlock;
            }
            else
            {
               sql3Cursor.StmtIdx = crsr.SGCurr;
            }

            int noOfUpdatedRecords = 0;

            errorCode = SQLiteLow.LibExecuteWithParams(sql3Cursor.InputSqlda, sql3Stmt, ConnectionTbl[sql3Dbd.DatabaseName], out noOfUpdatedRecords, DatabaseOperations.Where);
            

            if (Logger.Instance.LogLevel >= Logger.LogLevels.Support)
            {
               SQL3StmtBuildWithValues(sql3Stmt.Buf, dbCrsr.Definition.DataSourceDefinition, sql3Cursor.InputSqlda, sql3Stmt,false);
               Logger.Instance.WriteSupportToLog(string.Format("\tSTMT: %s", sql3Stmt.StmtWithValues), true);
            }
         }
         else
         {
            returnCode = GatewayErrorCode.BadSqlCommand;
         }

         if (errorCode == SqliteConstants.SQL3_OK)
         {
            if (!sql3Dbd.IsView)
            {
               origSqln = crsr.Output.Sqln;
               crsr.Output.Sqln = sql3Cursor.OutputSqlda.Sqln;
            }

            errorCode = SQLiteLow.LibFetch(sql3Cursor, crsr.Output);

            if (!sql3Dbd.IsView)
            {
               crsr.Output.Sqln = (int)origSqln;
            }
            
            if (errorCode != SqliteConstants.SQL3_OK)
            {
               SQLiteLow.LibClose(ConnectionTbl[sql3Dbd.DatabaseName], sql3Stmt);
               returnCode = GatewayErrorCode.LostRecord;
            }

            if (errorCode == SqliteConstants.SQL3_OK)
            {
               crsr.Output.SQL3SqldaToBuff(dbCrsr);
               crsr.Output.SQL3SqlindToBuff(dbCrsr, true);
            }

            if (releaseCmdOutsideTrans)
            {
               if (sql3Stmt.IsOpen)
                  errorCode = SQLiteLow.LibClose(ConnectionTbl[sql3Dbd.DatabaseName], sql3Stmt);
            }
         }
         else
         {
            if (sql3Stmt.IsOpen)
            {
               errorCode = SQLiteLow.LibClose(ConnectionTbl[sql3Dbd.DatabaseName], sql3Stmt);
            }
         }

         switch (ServerErrCode)
         {
            case (int)SQLiteErrorCode.Busy:
            case (int)SQLiteErrorCode.Locked:
               returnCode = GatewayErrorCode.FileLocked;
               break;
         }

         if (Logger.Instance.LogLevel >= Logger.LogLevels.Development && Logger.Instance.LogLevel != Logger.LogLevels.Basic)
         {
            if (errorCode == SqliteConstants.SQL3_OK)
            {
               SQLLogging.SQL3LogSqlda(crsr.Output, " output sqlda for get current");
            }
         }

         Logger.Instance.WriteSupportToLog(string.Format("CrsrGetCurr(): <<<<< returnCode = %d\n", returnCode), true);

         return (returnCode);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="db_crsr"></param>
      /// <returns></returns>
      public GatewayErrorCode CrsrKeyChk (GatewayAdapterCursor db_crsr)
      {
         GatewayErrorCode returnCode = GatewayErrorCode.Any;
         return returnCode;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="db_crsr"></param>
      /// <returns></returns>
      public GatewayErrorCode CrsrUpdate (GatewayAdapterCursor dbCrsr)
      {
         GatewayCursor            crsr;
         SQL3Dbd                  sql3Dbd;
         SQL3_CODE                errcode                = SqliteConstants.SQL3_OK;
         GatewayErrorCode         retcode                = SqliteConstants.RET_OK;
         int                      idx                    = 0;
         int                      cnt                    = 0;
         bool                     dbposChanged           = false;
         SQL3Connection           sql3Connection         = null;
         int                      []whNullBuf;
         bool                     doPrepare              = true,
                                  updateBlob             = false;
         int                      dummyLong              = 0;
         Sql3Stmt                 sql3Stmt               = null;
         int                      noOfUpdatedRecords     = 0;

         GatewayCursorTbl.TryGetValue(dbCrsr, out crsr);
         DbdTbl.TryGetValue(dbCrsr.Definition.DataSourceDefinition, out sql3Dbd);

         DBKey poskey = crsr.PosKey;

         Logger.Instance.WriteSupportToLog("CrsrUpdate(): >>>>>", true);

         // if no fields in DB_CRSR return 
         if (dbCrsr.Definition.FieldsDefinition.Count == 0)
         {      
            return SqliteConstants.RET_OK;
         }

         
         sql3Connection = ConnectionTbl[sql3Dbd.DatabaseName];

         Logger.Instance.WriteSupportToLog(string.Format("CrsrUpdate(): database = {0}, table = {1}", sql3Connection.DbName, sql3Dbd.FullName), true);
   
         ServerErrCode = SqliteConstants.SQL3_OK;
   
         if (sql3Dbd.IsView == true)
         {
            if (crsr.KeyArray== null)
            {
               Sql3MakeDbpoSegArray (dbCrsr);
            }
      
            // find out whether the dbpos has changed 
            for (idx = 0; idx < poskey.Segments.Count; idx++)
               // if  the field is in  the magic data view 
               if (crsr.KeyArray[idx] < dbCrsr.Definition.FieldsDefinition.Count)
                  // if the fields had changed 
                  if (dbCrsr.Definition.IsFieldUpdated [crsr.KeyArray[idx]])
                  {
                     dbposChanged = true;
                     break;
                  }
         }

         errcode = BeginTransactionIfNeeded(sql3Connection);

         crsr.SUpdate = Sql3StmtAlloc ("sUpdate", crsr.SUpdate, sql3Dbd.DatabaseName);
         sql3Stmt = StmtTbl[crsr.SUpdate];

         // Null_buf for the Where clause fields (other than pos key fields). 
         whNullBuf = new int[dbCrsr.Definition.FieldsDefinition.Count];
         for (int i = 0; i < dbCrsr.Definition.FieldsDefinition.Count; i++)
            whNullBuf[i] = 1;

         cnt = BuildUpdateStmt (dbCrsr);

         // If any field is to be updated then binding is done and executing with values
         if (cnt > 0)
         {
            if (!string.IsNullOrEmpty(crsr.StmtUpdate) && crsr.StmtUpdate == Statement)
		      {
			      // QCR# 235453: In case of datetime and time as a part of datetime, cnt != crsr->update->sqld are different 
			      //  eventhough stmt are same So we need to prepare crsr->update 
			      if(crsr.Update.Sqld == cnt)
				      doPrepare = false;
			      else
				      doPrepare = true;
		      }
		
            Logger.Instance.WriteSupportToLog(string.Format("CrsrUpdate(): No of fields to bind = {0}", cnt), true);

            if (! sql3Stmt.IsPrepared || doPrepare)
            {
               // allocate the sqlda for update 
               if (crsr.Update != null)
                  crsr.Update.SQL3SqldaFree();
               crsr.Update = new Sql3Sqldata(this);
               crsr.Update.SQL3SqldaAlloc(cnt);

               // update the sqlvar structures 
               InitializeCrsrNullIndicator (crsr, dbCrsr, false, out updateBlob);
               errcode = crsr.Update.SQL3SqldaUpdate(dbCrsr, whNullBuf, false);
               
               if (errcode == SqliteConstants.SQL3_OK)
               {
                  if (doPrepare)
                  {
                     // prepare the UPDATE statement 
                     crsr.StmtUpdate  = Statement;
                     sql3Stmt.Buf   = Statement;
                  }

                  if (Logger.Instance.LogLevel >= Logger.LogLevels.Development && Logger.Instance.LogLevel != Logger.LogLevels.Basic)
                  {
                     SQLLogging.SQL3LogSqlda(crsr.Update, "parameter sqlda for update");
                  }
               }

               errcode = SQLiteLow.LibPrepare(ConnectionTbl[sql3Dbd.DatabaseName], sql3Stmt, null);
            }
            else
            {
               InitializeCrsrNullIndicator (crsr, dbCrsr, false, out updateBlob);
               errcode = crsr.Update.SQL3SqldaUpdate(dbCrsr, whNullBuf, true);
            }

            if (errcode == SqliteConstants.SQL3_OK)
            {
               errcode = SQLiteLow.LibExecuteWithParams(crsr.Update, sql3Stmt, ConnectionTbl[sql3Dbd.DatabaseName], out noOfUpdatedRecords, DatabaseOperations.Update);
            }

            if (Logger.Instance.LogLevel >= Logger.LogLevels.Support)
            {
               SQL3StmtBuildWithValues(sql3Stmt.Buf, dbCrsr.Definition.DataSourceDefinition, crsr.Update, sql3Stmt, false);
               Logger.Instance.WriteSupportToLog(string.Format("\tSTMT: {0}", sql3Stmt.StmtWithValues), true);
            }

#if SQLITE_CIPHER_CPP_GATEWAY
            if (errcode == (int)SQLiteErrorCode.Done && noOfUpdatedRecords == 0)
#else
            if (errcode == (int)SQLiteErrorCode.Ok && noOfUpdatedRecords == 0)
#endif
            {
               errcode = SqliteConstants.NO_RECORDS_UPDATED;
               Logger.Instance.WriteSupportToLog("CrsrUpdate() : Update fail or Record has changed by another user", true);
            }
         }

#if SQLITE_CIPHER_CPP_GATEWAY
         if (errcode == (int)SQLiteErrorCode.Done && dbposChanged)
#else
         if (errcode == (int)SQLiteErrorCode.Ok && dbposChanged)
#endif
         {
            Logger.Instance.WriteDevToLog("CrsrUpdate(): dbpos changed, building new dbpos");
            SQL3DbposBuildFromBuf (dbCrsr.Definition.CurrentPosition, dbCrsr, dummyLong, false);
         }
        
         switch (ServerErrCode)
         {
            case (int)SQLiteErrorCode.Full:
               retcode = GatewayErrorCode.UpdateFail;
               LastErr = "SQL3 Gateway: Insufficient Memory Error.";
               break;

            case SqliteConstants.SQL3_OK:
               if (errcode == SqliteConstants.S_False)
               {
                  retcode = GatewayErrorCode.UpdateFail;
               }
               else if (errcode == SqliteConstants.NO_RECORDS_UPDATED)
               {
                  retcode = GatewayErrorCode.NoRowsAffected;
               }
               else
               {
                  retcode = SqliteConstants.RET_OK;
               }
               break;
            case (int)SQLiteErrorCode.Constraint:
               retcode = Sql3GetDbError();
               break;
            case (int)SQLiteErrorCode.ReadOnly:
               retcode = GatewayErrorCode.ReadOnly;
               break;
            case (int)SQLiteErrorCode.Busy:
            case (int)SQLiteErrorCode.Locked:
               retcode = GatewayErrorCode.FileLocked;
               break;
            default:
               retcode = GatewayErrorCode.UpdateFail;
               break;
         }

         ServerErrCode = SqliteConstants.SQL3_OK;
   
         Logger.Instance.WriteSupportToLog(string.Format("CrsrUpdate(): <<<<< retcode = {0}\n", retcode), true);
   
         return (retcode);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="dbCrsr"></param>
      /// <returns></returns>
      public GatewayErrorCode CrsrDelete (GatewayAdapterCursor dbCrsr)
      {
         GatewayCursor           crsr;
         SQL3Dbd                 sql3Dbd;
         SQL3_CODE               errcode      = SqliteConstants.SQL3_OK;
         GatewayErrorCode        retcode      = SqliteConstants.RET_OK;
         Sql3Stmt                sql3Stmt;
         bool                    doPrepare    = true;
         int                     cnt          = 0;
         int                     numOfDeleted = 0;
         SQL3Connection          sql3Connection;
         int                     []whNullBuf;
         int                     varIdx;

         Logger.Instance.WriteSupportToLog("CrsrDelete() >>>>>", true);
         
         GatewayCursorTbl.TryGetValue(dbCrsr, out crsr);
         DbdTbl.TryGetValue(dbCrsr.Definition.DataSourceDefinition, out sql3Dbd);

         sql3Connection = ConnectionTbl[sql3Dbd.DatabaseName];

         Logger.Instance.WriteSupportToLog(string.Format("CrsrDelete() : database = {0}, table = {1}", sql3Connection.DbName, sql3Dbd.FullName), true);

         errcode = BeginTransactionIfNeeded(sql3Connection);

         crsr.SDelete = Sql3StmtAlloc ("sDelete", crsr.SDelete, sql3Dbd.DatabaseName);
         sql3Stmt = StmtTbl[crsr.SDelete];

         // Null_buf for the Where clause fields (other than pos key fields).
         whNullBuf = new int[dbCrsr.Definition.FieldsDefinition.Count];
         for (int i = 0; i < dbCrsr.Definition.FieldsDefinition.Count; i++)
            whNullBuf[i] = 1;

         // build the DELETE statement 
         cnt = BuildDeleteStmt (dbCrsr);

         if (!string.IsNullOrEmpty(crsr.StmtDelete))
            doPrepare = crsr.StmtDelete == Statement  ? false : true;

         if (! sql3Stmt.IsPrepared || doPrepare)
         {
            if (sql3Stmt.IsPrepared)
            {
               SQLiteLow.LibReleaseStmt (sql3Stmt);
            }

            if (doPrepare)
            {
               crsr.StmtDelete = Statement;
               sql3Stmt.Buf  = Statement;
            }

            // allocate the sqlda to hold the input parameters. 
            if (crsr.SearchKey != null)
            {
               crsr.SearchKey.SQL3SqldaFree();
            }
            crsr.SearchKey = new Sql3Sqldata(this);
            crsr.SearchKey.SQL3SqldaAlloc (cnt);

            // set up the search key parameter sqlda 
            varIdx = 0;

            varIdx = crsr.SearchKey.SQL3SqldaFromDbpos (dbCrsr, varIdx, dbCrsr.Definition.CurrentPosition, false, false);

            if (Logger.Instance.LogLevel >= Logger.LogLevels.Development)
            {
               SQLLogging.SQL3LogSqlda(crsr.SearchKey, "parameter sqlda for delete");
            }

            errcode = SQLiteLow.LibPrepare(ConnectionTbl[sql3Dbd.DatabaseName], sql3Stmt, null);
         }
         else
         {
            varIdx = 0;
            varIdx = crsr.SearchKey.SQL3SqldaFromDbpos (dbCrsr, varIdx, dbCrsr.Definition.CurrentPosition, false, false);
         }

         Logger.Instance.WriteDevToLog(string.Format("CrsrDelete(): sqlvars = {0}", varIdx));

         if (errcode == SqliteConstants.SQLITE_OK)
         {
            errcode = SQLiteLow.LibExecuteWithParams(crsr.SearchKey, sql3Stmt, ConnectionTbl[sql3Dbd.DatabaseName], out numOfDeleted, DatabaseOperations.Delete);

            if (Logger.Instance.LogLevel >= Logger.LogLevels.Support)
            {
               SQL3StmtBuildWithValues(sql3Stmt.Buf, dbCrsr.Definition.DataSourceDefinition, crsr.SearchKey, sql3Stmt, false);
               Logger.Instance.WriteSupportToLog(string.Format("\tSTMT: {0}", sql3Stmt.StmtWithValues), true);
            }
          
#if SQLITE_CIPHER_CPP_GATEWAY
            if (errcode == (int)SQLiteErrorCode.Done && numOfDeleted == 0)
            {
               Logger.Instance.WriteDevToLog(string.Format("CrsrDelete(): deleted 0 records, the record has been changed by another user"));
               return GatewayErrorCode.NoRowsAffected;
            }
#else
            //TODO : Check if we can get num_of_deleted.
            if (errcode == (int)SQLiteErrorCode.Ok && numOfDeleted > 0)
            {
               Logger.Instance.WriteSupportToLog("CrsrDelete() : deleted 0 records, the record has been changed by another user\n", true);
            }
#endif
         }
         else
         {
            // By only copying the pos into TID_buf, What are we achieving..??? (27/10/04)
            TID_buf = dbCrsr.Definition.CurrentPosition.Get();

            if (Logger.Instance.LogLevel >= Logger.LogLevels.Development)
            {

            }
         }

         switch (ServerErrCode)
         {
            case (int)SQLiteErrorCode.Full:
               retcode = GatewayErrorCode.UpdateFail;
               LastErr = "SQL3 Gateway: Insufficient Memory Error.";
               break;

            case SqliteConstants.SQL3_OK:
               if (errcode == SqliteConstants.S_False)
                  retcode = GatewayErrorCode.UpdateFail;
               else
                  retcode = SqliteConstants.RET_OK;
               break;

            case (int)SQLiteErrorCode.Constraint:
               retcode = GatewayErrorCode.ConstraintFail;
               break;

            case (int)SQLiteErrorCode.ReadOnly:
               retcode = GatewayErrorCode.ReadOnly;
               break;

            case (int)SQLiteErrorCode.Busy:
            case (int)SQLiteErrorCode.Locked:
               retcode = GatewayErrorCode.FileLocked;
               break;

            default:
               retcode = GatewayErrorCode.UpdateFail;
               break;
         }

         Logger.Instance.WriteSupportToLog(string.Format("CrsrDelete(): <<<<< retcode = {0}\n", retcode), true);
   
         return (retcode);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="dbCrsr"></param>
      /// <returns></returns>
      public GatewayErrorCode CrsrInsert (GatewayAdapterCursor dbCrsr)
      {
         GatewayCursor     crsr;
         SQL3Dbd           sql3Dbd;
         Sql3Stmt          sql3Stmt       = null;
         SQL3_CODE         errorCode      = SqliteConstants.SQL3_OK;
         GatewayErrorCode  returnCode     = GatewayErrorCode.Any;
         SQL3Connection    sql3Connection = null;
         int               inputFlds;
         bool              insertBlob     = false;
         long              uniqueKey      = 0;
         int               numOfUpdatedRecords = 0;
         long              dbPosBufSize   = 0;

         DbdTbl.TryGetValue(dbCrsr.Definition.DataSourceDefinition, out sql3Dbd);
         GatewayCursorTbl.TryGetValue(dbCrsr, out crsr);

         Logger.Instance.WriteSupportToLog("CrsrInsert(): >>>>>", true);

         sql3Connection = ConnectionTbl[sql3Dbd.DatabaseName];

         Logger.Instance.WriteSupportToLog(string.Format("CrsrInsert(): database = {0}, table name = {1}", sql3Connection.DbName, sql3Dbd.FullName), true);
         
         // if no fields in DbCrsr return
         if (dbCrsr.Definition.FieldsDefinition.Count == 0)
         {
            return GatewayErrorCode.Any;
         }

         // cannot perform INSERT operation if not all dbpos segment in magic data view 
         // Check if all the DBPOS segment f are in the data view - if not, insert will not be allowed 
         if ((!Sql3AllKeySegmentsInView(dbCrsr)))
         {
            Logger.Instance.WriteSupportToLog("CrsrInsert(): Not all position key segments in data view, insert not allowed !!!", true);

            crsr.InsertAllowed = false;
         }
         else
         {
            crsr.InsertAllowed = true;
         }

         if (!crsr.InsertAllowed)
         {
            LastErr = string.Empty;

            LastErr = "SQLite: Insert failed, not all position key segments in magic data view";

            Logger.Instance.WriteSupportToLog("CrsrInsert(): <<<<< Not all position key segments in magic data view, insert failed\n", true);

            return GatewayErrorCode.UpdateFail;
         }

         crsr.SInsert = Sql3StmtAlloc("sInsert", crsr.SInsert, sql3Dbd.DatabaseName);
         sql3Stmt = StmtTbl[crsr.SInsert];

         errorCode = BeginTransactionIfNeeded(sql3Connection);

         if (!sql3Stmt.IsPrepared)
         {
            // build the INSERT statement 
            inputFlds = BuildInsertStmt(dbCrsr);
            crsr.StmtInsert = Statement;

            sql3Stmt.Buf = string.Empty;
            sql3Stmt.Buf = Statement;

            // allocate the sqlda to hold the input parameters. 
            if (crsr.Input != null)
               crsr.Input.SQL3SqldaFree();

            crsr.Input = new Sql3Sqldata(this);
            if (sql3Dbd.IsView)
            {
               crsr.Input.SQL3SqldaAlloc(inputFlds);
            }
            else //One for rowid
            {
               crsr.Input.SQL3SqldaAlloc(inputFlds + 1);
            }

            //This sequence of function is changed , To assign correct crsr.nullBuf[fld_idx] to the sqldaInput.
            InitializeCrsrNullIndicator(crsr, dbCrsr, true, out insertBlob);

            // set up the input parameter sqlda
            crsr.Input.SQL3SqldaInput(dbCrsr, sql3Stmt.IsPrepared);

            errorCode = SQLiteLow.LibPrepare(ConnectionTbl[sql3Dbd.DatabaseName], sql3Stmt, null);
         }
         else
         {
            // We need to reprepare the INSERT stmt, if BLOB exists in the tbl
            if (dbCrsr.Definition.Blobs > 0)
            {
               /* allocate the sqlda to hold the input parameters. */
               //input_flds = crsr->input->sqld;
               //SQL3SqldaFree (&(crsr->input));
               //crsr->input = SQL3SqldaAlloc (input_flds);

               ///* set up the input parameter sqlda*/
               //SQL3SqldaInput (crsr->input, db_crsr);
            }

            /* move data from the db_crsr buffer to varchar structures if necessary */
            InitializeCrsrNullIndicator(crsr, dbCrsr, true, out insertBlob);
            crsr.Input.SQL3SqldaInput(dbCrsr, sql3Stmt.IsPrepared);
         }

         /*chng*/
         if (errorCode == SqliteConstants.SQL3_OK)
         {
            errorCode = Sql3InsertValuesAndExec(dbCrsr, sql3Stmt, crsr.Input, out uniqueKey);
            if (Logger.Instance.LogLevel >= Logger.LogLevels.Development && Logger.Instance.LogLevel != Logger.LogLevels.Basic)
            {
               SQLLogging.SQL3LogSqlda(crsr.Input, "parameter sqlda for Insert");
            }

            if (Logger.Instance.LogLevel >= Logger.LogLevels.Support)
            {
               SQL3StmtBuildWithValues(crsr.StmtInsert, dbCrsr.Definition.DataSourceDefinition, crsr.Input, sql3Stmt, true);
               Logger.Instance.WriteSupportToLog(string.Format("\tSTMT: {0}", sql3Stmt.StmtWithValues), true);
            }
         }

#if SQLITE_CIPHER_CPP_GATEWAY
         if ((errorCode == (int)SQLiteErrorCode.Done || errorCode == (int)GatewayErrorCode.FilterAfterInsert) && ServerErrCode == SqliteConstants.SQL3_OK)
#else
         if ((errorCode == (int)SQLiteErrorCode.Ok || errorCode == (int)GatewayErrorCode.FilterAfterInsert) && ServerErrCode == SqliteConstants.SQL3_OK)
#endif
         {
            //sqlite3_total_changes returns the no of rows changed in case of INSERT operation.      
#if SQLITE_CIPHER_CPP_GATEWAY
            numOfUpdatedRecords = Sql3LibTotalChanges(sql3Connection);
#else
            numOfUpdatedRecords = Sql3LibTotalChanges(sql3Stmt);
#endif

            if (numOfUpdatedRecords > 0 && !sql3Dbd.IsView)
            {
               dbPosBufSize = sql3Dbd.posLen;
               //To retrieve rowid on successful INSERT.
               SQL3InsRowid = SQLiteLow.LibGetLastInsertRowId(ConnectionTbl[sql3Dbd.DatabaseName]);

               crsr.Input.SqlVars[crsr.Input.Sqld - 1].SqlData = SQL3InsRowid;
               int rowIdLen = SqliteConstants.SQL3_ROWID_LEN_EXTERNAL;


               int pos = CopyBytes(BitConverter.GetBytes(rowIdLen), crsr.DbPosBuf, 0);

               int data = int.Parse(crsr.Input.SqlVars[crsr.Input.Sqld - 1].SqlData.ToString());
               //convert RowId in bytes
               CopyBytes(BitConverter.GetBytes(data), crsr.DbPosBuf, pos);

               dbCrsr.Definition.CurrentPosition.Set(crsr.DbPosBuf);

               if (sql3Dbd.identityFld != null)
                  SQL3SetNewIdentity(dbCrsr);
            }
            else
               SQL3DbposBuildFromBuf(dbCrsr.Definition.CurrentPosition, dbCrsr, uniqueKey, true);
         }

         switch (ServerErrCode)
         {
            case (int)SQLiteErrorCode.Full:
               returnCode = GatewayErrorCode.UpdateFail;
               LastErr = string.Empty;
               LastErr = "SQL3 Gateway: Insufficient Memory Error.";
               break;
            case SqliteConstants.SQL3_OK:
               if (errorCode == SqliteConstants.S_False)
                  returnCode = GatewayErrorCode.UpdateFail;
               else
                  returnCode = SqliteConstants.RET_OK;
               break;
            case (int)SQLiteErrorCode.Constraint:
               returnCode = Sql3GetDbError();
               break;
            case (int)SQLiteErrorCode.ReadOnly:
               returnCode = GatewayErrorCode.ReadOnly;
               break;
            case (int)SQLiteErrorCode.Busy:
            case (int) SQLiteErrorCode.Locked:
               returnCode = GatewayErrorCode.FileLocked;
               break;

            default:
               returnCode = GatewayErrorCode.UpdateFail;
               break;
         }

         //LAST_err_code = SQL3_OK; 
         ServerErrCode = SqliteConstants.SQL3_OK;

         if (returnCode == SqliteConstants.RET_OK && errorCode == (int)GatewayErrorCode.FilterAfterInsert)
            returnCode = GatewayErrorCode.FilterAfterInsert;

         Logger.Instance.WriteSupportToLog(string.Format("CrsrInsert(): <<<<< retcode = {0}\n", returnCode), true);

         return (returnCode);

      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="db_crsr"></param>
      /// <returns></returns>
      public GatewayErrorCode CrsrUnlock (GatewayAdapterCursor db_crsr)
      {
         GatewayErrorCode returnCode = GatewayErrorCode.Any;
         return returnCode;
      }
      
      /// <summary>
      /// 
      /// </summary>
      /// <param name="dbDefinition"></param>
      /// <param name="clear"></param>
      /// <param name="DBMS_code"></param>
      /// <param name="buf"></param>
      /// <returns></returns>
      public GatewayErrorCode LastError (DatabaseDefinition dbDefinition,  bool clear, ref int DBMS_code, ref string buf)
      {
         DBMS_code = LastErrCode;

         Logger.Instance.WriteSupportToLog(string.Format("LastError(): >>>>> database = {0}", dbDefinition.Name), true);

         // Bugfix 698310 : Here , we need not connect to database actually.Also we do not need DB_CONNECTION
         //  so we should not call sql3_connect_db. We should just check for NO_DATABASE_NAME_GIVEN
         if(string.IsNullOrEmpty(dbDefinition.Location))
         {
            buf= "SQLite: Connection to database failed - No database name given";
            Logger.Instance.WriteSupportToLog(string.Format("LastError(): <<<<<  {0}\n", buf), true);
            return GatewayErrorCode.Any;
         }

         buf = LastErr;
         if (clear)
         {
            LastErrCode = SqliteConstants.SQL3_OK;
            ServerErrCode = SqliteConstants.SQL3_OK;
            SaveError = true;
            LastErr = string.Empty;
         }

         Logger.Instance.WriteSupportToLog(string.Format("LastError(): <<<<< {0}\n", buf), true);

         return GatewayErrorCode.Any;
      }
      
      /// <summary>
      /// CrsrFetchBlobs
      /// </summary>
      /// <param name="db_crsr"></param>
      /// <returns></returns>
      public GatewayErrorCode CrsrFetchBLOBs(GatewayAdapterCursor dbCrsr)
      {
         GatewayErrorCode   returnCode  = GatewayErrorCode.Any;
         GatewayCursor      crsr;
         SQL3Dbd            sql3Dbd = null;
         bool               hasRange  = (dbCrsr.Ranges.Count > 0 || dbCrsr.SqlRng != null) ? true : false;
         SQL3Cursor         sql3Cursor ;
         SQL3Connection     sql3Connection;


         GatewayCursorTbl.TryGetValue(dbCrsr, out crsr);
         Logger.Instance.WriteSupportToLog("CrsrFetchBLOBs(): >>>>>>", true);


         /* BugFix#408060. For get_curr() getting called out of trans(), we have to Hold & 
            Release the conn in get_curr(). Same is for fetch_BLOBs() which may get called after
            get_curr(). So Hold & Release conn here also, if not Held. */
         if(DbdTbl.TryGetValue(dbCrsr.Definition.DataSourceDefinition, out sql3Dbd)) 
         {
            sql3Connection = ConnectionTbl[sql3Dbd.DatabaseName];
         }

         if (sql3Dbd != null)
         {

            if(hasRange) /*      if (has_range && !strt_pos) */
            {
                crsr.CRange = Sql3CursorAlloc("Range", crsr.CRange);
                sql3Cursor = CursorTbl[crsr.CRange];
            }
            else
            {
               crsr.CRead = Sql3CursorAlloc("Read", crsr.CRead);
               sql3Cursor = CursorTbl[crsr.CRead];
            }


            // if we have reused a cursor 
            if (sql3Cursor.DoDummyFetch)
            {
               sql3Cursor.DoDummyFetchBlob = false;

               Logger.Instance.WriteSupportToLog("CrsrFetchBLOBs(): <<<<< done dummy fetch blob\n", true);

               return  SqliteConstants.RET_OK;
            }  
         }

         Sql3ReadAllBlobs(dbCrsr);

         Logger.Instance.WriteSupportToLog(string.Format("CrsrFetchBLOBs(): <<<<<< returnCode = %d\n", returnCode), true);

         return (returnCode);
      }

      /// <summary>
      /// The function receives a database as a parameter and disconnects the current connection of that database.
      /// Disconnecting is only allowed if there is no file open on the database that you are trying to disconnect from. 
      /// If there is a file open and the user issues a DbDiscnt command, an error message will be displayed.
      /// </summary>
      /// <param name="dbDefinition"></param>
      /// <returns></returns>
      public GatewayErrorCode DbDisconnect (string databaseLocation, out string tableName)
      {
         GatewayErrorCode  returnCode  = GatewayErrorCode.Any;
         int idx = 0;
         SQL3Connection sql3Connection = null;
         bool connectionInUse = false;
         bool connectionExist = false;
         tableName = string.Empty;

         Logger.Instance.WriteDevToLog(string.Format("DbDisconnect(): >>>>> database = {0} SQL3 connection cnt = {1}", databaseLocation, ConnectionTbl.Count));

         if (string.IsNullOrEmpty(databaseLocation))
            idx = SqliteConstants.NO_DATABASE_NAME_GIVEN;
         else
         {
            Dictionary<string, SQL3Connection>.Enumerator connectionEnumerator = ConnectionTbl.GetEnumerator();
            while(connectionEnumerator.MoveNext())
            {
               sql3Connection = connectionEnumerator.Current.Value;

               if (sql3Connection.DbName == databaseLocation)
               {
                  connectionExist = true;
                  Dictionary<DataSourceDefinition, SQL3Dbd>.Enumerator dbdEnumerator = DbdTbl.GetEnumerator();
                  while (dbdEnumerator.MoveNext())
                  {
                     SQL3Dbd sql3dbd = dbdEnumerator.Current.Value;
                     if (sql3dbd.DatabaseName == sql3Connection.DbName)
                     {
                        connectionInUse = true;
                        tableName = sql3dbd.TableName;
                        returnCode = GatewayErrorCode.DatasourceOpen;
                        break;
                     }
                  }

                  if (!connectionInUse)
                  {
                     if (SQLiteLow.LibDisconnect(sql3Connection) == SqliteConstants.SQL3_OK)
                     {
                        ConnectionTbl.Remove(databaseLocation);
                     }
                     else
                     {
                        returnCode = GatewayErrorCode.Unmapped;
                     }

                     break;
                  }
                  
               }

            }

            //If connection is not opened for the given database the return error.
            if (!connectionExist)
            {
               returnCode = GatewayErrorCode.Unmapped;
            }
         }

         Logger.Instance.WriteDevToLog(string.Format("DbDisconnect(): <<<<< connection = {0}", idx));

         return returnCode;      
      }

      /// <summary>
      /// Executes the provided sql statement and returns an object array of the returned values.
      /// Only the first row's results are returned. The size of the returned values array is set
      /// according to the provided storage attbribute array, but initiallized only upon an sql execution
      /// with returned value (F.E - SELECT, COUNT...)
      /// </summary>
      /// <param name="dbDefinition"></param>
      /// <param name="sqlStatement"></param>
      /// <param name="storageAttributes"></param>
      /// <param name="statementReturnedValues"></param>
      /// <returns>Gateway error code</returns>
      public GatewayErrorCode SQLExecute(DatabaseDefinition dbDefinition, string sqlStatement,
         StorageAttribute[] storageAttributes, out object[] statementReturnedValues, ref DBField[] dbFields)
      {
         object[] valueArray = null;
         statementReturnedValues = new object[0];
         SQL3_CODE errcode = 0;
         GatewayErrorCode returnCode = GatewayErrorCode.Any;

         if (ConnectDb(dbDefinition) == SqliteConstants.NO_DATABASE_NAME_GIVEN)
         {
            errcode = SqliteConstants.NO_DATABASE_NAME_GIVEN;
            Logger.Instance.WriteSupportToLog("SQLExecute(): <<<<< no database name given", true);
            return GatewayErrorCode.BadSqlCommand;
         }

         SQL3Connection sql3Connection = ConnectionTbl[dbDefinition.Location];

         BeginTransactionIfNeeded(sql3Connection);
         errcode = SQLiteLow.LibExecuteStatement(sqlStatement, sql3Connection, true, out valueArray);

         if (valueArray.Length > 0)
         {
            statementReturnedValues = new object[storageAttributes.Length];
            for (int i = 0; i < Math.Min(statementReturnedValues.Length, valueArray.Length); i++)
            {
               statementReturnedValues[i] = ConvertFundamentalDatatypeValueToGatewayValue(valueArray[i], storageAttributes[i], dbFields[i]);
               dbFields[i].Storage = computeStorageByTargetFieldAttribute(storageAttributes[i], statementReturnedValues[i], ref dbFields[i]);
            }
         }

         if (errcode != SqliteConstants.SQL3_OK)
            returnCode = GatewayErrorCode.BadSqlCommand;
         return returnCode;
      }

      public GatewayErrorCode CrsrPrepareJoin (DB_JOIN_CRSR dbJoinCrsr, DatabaseDefinition dbDefinition)
      {
         GatewayErrorCode returnCode = GatewayErrorCode.Any;
         return returnCode;
      }

      public GatewayErrorCode CrsrBeginJoin (DB_JOIN_CRSR dbJoinCrsr)
      {
         GatewayErrorCode returnCode = GatewayErrorCode.Any;
         return returnCode;
      }
      public GatewayErrorCode CrsrOpenJoin (DB_JOIN_CRSR dbJoinCrsr)
      {
         GatewayErrorCode returnCode = GatewayErrorCode.Any;
         return returnCode;
      }

      public GatewayErrorCode CrsrCloseJoin (DB_JOIN_CRSR dbJoinCrsr)
      {
         GatewayErrorCode returnCode = GatewayErrorCode.Any;
         return returnCode;
      }

      public GatewayErrorCode CrsrEndJoin (DB_JOIN_CRSR dbJoinCrsr)
      {
         GatewayErrorCode returnCode = GatewayErrorCode.Any;
         return returnCode;
      }

      public GatewayErrorCode CrsrReleaseJoin (DB_JOIN_CRSR dbJoinCrsr)
      {
         GatewayErrorCode returnCode = GatewayErrorCode.Any;
         return returnCode;
      }

      public GatewayErrorCode CrsrFetchJoin (DB_JOIN_CRSR dbJoinCrsr)
      {
         GatewayErrorCode returnCode = GatewayErrorCode.Any;
         return returnCode;
      }

      public GatewayErrorCode CrsrGetCurrJoin (DB_JOIN_CRSR dbJoinCrsr)
      {
         GatewayErrorCode returnCode = GatewayErrorCode.Any;
         return returnCode;
      }

      public GatewayErrorCode CrsrFetchBLOBsJoin (DB_JOIN_CRSR dbJoinCrsr)
      {
         GatewayErrorCode returnCode = GatewayErrorCode.Any;
         return returnCode;
      }

      public void Sql3StmtFree (Sql3Stmt  sql3Stmt)
      {

      }

      /// <summary>
      ///  AddForKeys ()
      /// </summary>
      /// <param name="sql3Dbd"></param>
      /// <param name="referencedDbhVec"></param>
      /// <param name="dbDefinition"></param>
      /// <returns>SQL3_CODE</returns>
      public SQL3_CODE AddForKeys (SQL3Dbd sql3Dbd, List<DataSourceDefinition> referencedDbhVec, DatabaseDefinition dbDefinition)
      {
         SQL3_CODE errorCode = SqliteConstants.SQL3_OK;
         //short idx = 0;

         //for (idx = 0; idx < sql3Dbd.dbd.DatabaseSourceDefinition.forKeys; ++idx)
         //{
         //   if (sql3Dbd.dbd.DatabaseSourceDefinition.forKey[idx].flags.is_set((char)FOR_KEY.CREATE_IN_DB))
         //   {
         //      errorCode = AddForKey(sql3Dbd, idx, referencedDbhVec, dbDefinition);
         //   }
         //   if (errorCode != SqliteConstants.SQL3_OK)
         //      break;
         //}
         return (errorCode);
      }

      /// <summary>
      ///  AddForKey ()
      /// </summary>
      /// <param name="sql3Dbd"></param>
      /// <param name="fkeyidx"></param>
      /// <param name="referencedDbhVec"></param>
      /// <param name="dbDefinition"></param>
      /// <returns>SQL3_CODE</returns>
      public SQL3_CODE AddForKey (SQL3Dbd sql3Dbd, int fkeyidx, List<DataSourceDefinition> referencedDbhVec, DatabaseDefinition dbDefinition)
      {
         return 0;
      }


      /// <summary>
      ///  Sql3GetRangeFl ()
      /// </summary>
      /// <param name="dbCrsr"></param>
      /// <param name="range"></param>
      /// <param name="fld"></param>
      /// <param name="dateFl"></param>
      /// <param name="datetimeRange"></param>
      public char Sql3GetFieldRangeType(GatewayAdapterCursor dbCrsr, RangeData range,
                                  DBField fld, bool dateFl, char datetimeRange)
      {
         char              rangeType  = (char)SqliteConstants.NO_RNG;
         int               len        = fld.StorageFldSize();
         bool              maxEqMin   = false;
         List<RangeData>   dateTimeRangeData;
         RangeData         dateRange;
         bool              saveDiscardMin = false,
                           saveDiscardMax = false,
                           modified       = false,
                           slctModified   = false;

         DataSourceDefinition dbh = dbCrsr.Definition.DataSourceDefinition;

         /* 1.If the min & max values exists and matches. OR
            2. If a date fld is not PartOfDateTime_ */
         if ((range.Min.Value.Value != null && range.Max.Value.Value != null && 
              range.Min.Value.Value.Equals(range.Max.Value.Value)) &&
             (range.Min.Value.IsNull == range.Min.Value.IsNull) &&
             !(dateFl && (datetimeRange == SqliteConstants.DATE_RNG) && fld.Storage == FldStorage.DateString))
         {
            maxEqMin = true;
         }
         else
            maxEqMin = false;

         if (fld.PartOfDateTime > 0)
         {
            if (fld.Storage == FldStorage.DateString)
            {
               if (maxEqMin)
               {
                  maxEqMin = false;
                  if (range.Min.Type == RangeType.RangeMinMax)
                  {
                     range.Min.Type = RangeType.RangeParam;
                     range.Max.Type = RangeType.RangeParam;
                     slctModified = true;
                  }
               }
            }

            if (fld.Storage == FldStorage.TimeString)
            {
               dateTimeRangeData = dbCrsr.Ranges;

               for (int dt_idx = 0; dt_idx < dbCrsr.Ranges.Count; ++dt_idx)
               {
                  dateRange = dateTimeRangeData[dt_idx];
                  if (dbCrsr.Definition.FieldsDefinition[dateRange.FieldIndex].Isn == fld.PartOfDateTime)
                  {
                     saveDiscardMin = range.Min.Discard;
                     saveDiscardMax = range.Max.Discard;
                     modified = true;

                     if (dateRange.Min.Type != RangeType.RangeNoVal && range.Min.Type != RangeType.RangeNoVal)
                     {
                        range.Min.Discard = true;
                     }

                     if (dateRange.Max.Type != RangeType.RangeNoVal && range.Max.Type != RangeType.RangeNoVal)
                     {
                        range.Max.Discard = true;
                     }
                     break;
                  }
               }
            }
         }


         // There are 8 situations for the different combinations of MIN, MAX and NULL:
         if ((range.Min.Type == RangeType.RangeMinMax)
             && !(dateFl && (datetimeRange == SqliteConstants.DATE_RNG)) && !range.Min.Discard) /* max_typ is irrelevant */
         {
            if (range.Min.Value.IsNull)
               rangeType = (char)SqliteConstants.NULL_RNG;
            else
               rangeType = (char)SqliteConstants.MIN_EQ_MAX_RNG;
         }
         else if (((range.Min.Type == RangeType.RangeParam && range.Max.Type == RangeType.RangeParam) ||
                   (range.Min.Type == RangeType.RangeMinMax
                   && (dateFl && (datetimeRange == SqliteConstants.DATE_RNG)))) && (!range.Min.Discard && !range.Max.Discard))
         {
            if (!range.Min.Value.IsNull && !range.Max.Value.IsNull)
               if (maxEqMin)
                  rangeType = (char)SqliteConstants.MIN_EQ_MAX_RNG;
               else if (fld.IsString() && !string.IsNullOrEmpty(range.Min.Value.Value.ToString()) && string.IsNullOrEmpty(range.Max.Value.Value.ToString())) 
               {
                  rangeType = (char)SqliteConstants.MIN_RNG;
               }
               else if (fld.IsString() && string.IsNullOrEmpty(range.Min.Value.Value.ToString()) && !string.IsNullOrEmpty(range.Max.Value.Value.ToString())) 
                  rangeType = (char)SqliteConstants.MAX_RNG;
               else
                  rangeType = (char)SqliteConstants.MIN_AND_MAX_RNG;
            else if (range.Min.Value.IsNull && !range.Max.Value.IsNull)
               rangeType = (char)SqliteConstants.NULL_AND_MAX_RNG;
            else if (!range.Min.Value.IsNull && range.Max.Value.IsNull)
               rangeType = (char)SqliteConstants.MIN_AND_NULL_RNG;
            else
               rangeType = (char)SqliteConstants.NULL_RNG;
         }
         else if (range.Min.Type == RangeType.RangeParam && !range.Min.Discard && ((range.Max.Type != RangeType.RangeParam) ||
                  (range.Max.Type == RangeType.RangeParam && range.Max.Discard)))
            if (range.Min.Value.IsNull)
               rangeType = (char)SqliteConstants.NULL_RNG;
            else
               rangeType = (char)SqliteConstants.MIN_RNG;
         else if (range.Max.Type == RangeType.RangeParam && !range.Max.Discard && ((range.Min.Type != RangeType.RangeParam) ||
                  (range.Min.Type == RangeType.RangeParam && range.Min.Discard)))
            if (range.Max.Value.IsNull)
               rangeType = (char)SqliteConstants.NULL_RNG;
            else
               rangeType = (char)SqliteConstants.MAX_RNG;


         if (modified)
         {
            range.Min.Discard = saveDiscardMin;
            range.Max.Discard = saveDiscardMax;

         }

         if (slctModified)
         {
            range.Min.Type = range.Min.Type = RangeType.RangeMinMax;
         }


         return (rangeType);
      }


      public long SQL3UpdateWhereFromDbpos (GatewayAdapterCursor db_crsr, DbPos dbPos, string stmt, int stmtSizeInChars, long pos)
      {
         return 0;
      }

      /// <summary>
      ///  SQL3SqldaGetKey ()
      /// </summary>
      /// <param name="dbCrsr"></param>
      /// <param name="sortkey"></param>
      /// <param name="sqlVar"></param>
      /// <param name="setValue"></param>
      public void SQL3SqldaGetKey(GatewayAdapterCursor dbCrsr, DBKey sortkey, List<Sql3SqlVar> sqlVar, bool setValue)
      {
         GatewayCursor        crsr;
         int                  segIdx = 0;
         short                varIdx = 0;
         DBField              fld = null;
         DataSourceDefinition dbh = dbCrsr.Definition.DataSourceDefinition;
         DBSegment            seg;
         Sql3Field            sql3Field = new Sql3Field();
         bool                 keyUnique = false,
                              keyNullable = false;
         SQL3Dbd              sql3Dbd;
         string               fieldName;
         int                  dbIdx;
         int                  timeIdx;
         DBSegment            timeSeg;
         string               fullDate;
         bool                 dateFl;
         bool                 updateBlob = false;

         GatewayCursorTbl.TryGetValue(dbCrsr, out crsr);
         DbdTbl.TryGetValue(dbh, out sql3Dbd);

         Logger.Instance.WriteDevToLog("SQL3SqldaGetKey(): >>>>> ");
         
         keyUnique = sortkey.CheckMask(KeyMasks.UniqueKeyModeMask);
         keyNullable = CheckKeyNullable(dbCrsr, true);

         /* get a pointer to the first segment */

         if (setValue)
         {
            InitializeCrsrNullIndicator(crsr, dbCrsr, false, out updateBlob);
         }

         /* for all segments of the key add a sqlvar */
         for (segIdx = 0; segIdx < sortkey.Segments.Count; ++segIdx)
         {
            seg = sortkey.Segments[segIdx];
            fieldName = seg.Field.DbName;
            sql3Field.Name = fieldName;

            fld = seg.Field;

            Logger.Instance.WriteDevToLog(string.Format("SQL3SqldaGetKey(): fld_idx = {0}, name = {1}", dbCrsr.GetFieldIndex(seg.Field), sql3Field.Name));

            sql3Field.FieldLen = fld.Length;
            if (fld.Storage == FldStorage.AlphaZString)
               sql3Field.FieldLen--;
            if (fld.Storage == FldStorage.UnicodeZString)
               sql3Field.FieldLen -= 2;

            sql3Field.SqlVar = sqlVar[varIdx++];
            sql3Field.Fld = seg.Field;
            sql3Field.GatewayAdapterCursor = dbCrsr;
            sql3Field.DataSourceDefinition = dbCrsr.Definition.DataSourceDefinition;

            sql3Field.Storage = (FldStorage)fld.Storage;
            sql3Field.Whole = fld.Whole;
            sql3Field.Dec = fld.Dec;
            sql3Field.AllowNull = fld.AllowNull;

            

            if (fld.PartOfDateTime != 0)
            {
               if (fld.Storage == FldStorage.TimeString)
                  sql3Field.PartOfDateTime = SqliteConstants.TIME_OF_DATETIME;
               else
               {
                  for (timeIdx = 0; timeIdx < sortkey.Segments.Count; ++timeIdx)
                  {
                     timeSeg = sortkey.Segments[timeIdx];
                     if (timeSeg.Field.Isn == fld.PartOfDateTime)
                        break;
                  }
                  if (timeIdx < sortkey.Segments.Count)
                     sql3Field.PartOfDateTime = timeIdx;
                  else
                     sql3Field.PartOfDateTime = SqliteConstants.NORMAL_OF_DATETIME;
               }
            }
            else
            {
               sql3Field.PartOfDateTime = SqliteConstants.NORMAL_OF_DATETIME;
            }

            if (setValue)
            {
               if ((fld.Storage == FldStorage.DateString && Sql3DateType(dbh, fld) != DateType.DATE_TO_SQLCHAR) ||
                   (fld.Storage == FldStorage.TimeString && Sql3DateType(dbh, fld) == DateType.DATE_TO_DATE))
                  dateFl = true;
               else
                  dateFl = false;

               dbIdx = Sql3GetDbCrsrIndex(dbCrsr, seg.Field);
               //assert (db_idx != NULL_INDEX);
               sql3Field.NullIndicator = crsr.NullIndicator[dbCrsr.GetFieldIndex(seg.Field)];
               sql3Field.Buf = dbCrsr.CurrentRecord.GetValue(dbCrsr.GetFieldIndex(fld));

               if (dateFl)
               {
                  SqlvarFill(ref sql3Field, true, true, false);
                  if (fld.PartOfDateTime != 0)
                  {
                     Sql3DateToInternal((string)sql3Field.Buf, out fullDate, fld.StorageFldSize());
                  }
                  else
                     if (fld.Storage == FldStorage.TimeString)
                     {
                        Sql3TimeToInternal((string)sql3Field.Buf, out fullDate);
                     }
                     else
                     {
                        Sql3DateToInternal((string)sql3Field.Buf, out fullDate, fld.StorageFldSize());
                     }

                  sql3Field.SqlVar.SqlData = fullDate;
               }
               else
               {
                  SqlvarValFill(sql3Field.SqlVar, sql3Field, varIdx - 1, false, SqliteConstants.QUOTES_TRUNC);
               }
            }
            else
            {
               SqlvarFill(ref sql3Field, false, false, false);
            }
         }

         /* In deferred trans, dont include Rowid in wherer-clause for newly inserted rec.*/
         if (keyUnique == false || (keyUnique == true && keyNullable == true))
         {
            if (!sql3Dbd.IsView)
            {
               /* add one for the tid*/
               Logger.Instance.WriteDevToLog(string.Format("SQL3SqldaGetKey(): using sqlvar[{0}] for TID", varIdx));

               sql3Field.SqlVar = sqlVar[varIdx++];
               sql3Field.Name = SqliteConstants.SQL3_ROWID_ST_A;
               sql3Field.Storage = 0;
               sql3Field.Whole = 0;
               sql3Field.Dec = 0;
               sql3Field.GatewayAdapterCursor = dbCrsr;
               sql3Field.DataSourceDefinition = dbCrsr.Definition.DataSourceDefinition;
               sql3Field.AllowNull = false;
               sql3Field.FieldLen = SqliteConstants.SQL3_ROWID_LEN_EXTERNAL;
               sql3Field.PartOfDateTime = SqliteConstants.NORMAL_OF_DATETIME;

               if (dbCrsr.CursorType == CursorType.PartOfOuter)
                  sql3Field.NullIndicator = crsr.NullIndicator[varIdx - 1];
               else
                  sql3Field.NullIndicator = SQL3NotNull;

               sql3Field.SqlVar.SqlType = Sql3Type.SQL3TYPE_ROWID;
               sql3Field.SqlVar.IsBlob = false;
               sql3Field.SqlVar.DataSourceType = "SQL3TYPE_ROWID";
               sql3Field.SqlVar.typeAffinity = TypeAffinity.TYPE_AFFINITY_INTEGER;

               SqlvarFill(ref sql3Field, false, false, false);

               Logger.Instance.WriteDevToLog("SQL3SqldaGetKey():  table with non unique sort key - using TID as unique rowid");
            }
         }

         Logger.Instance.WriteDevToLog("SQL3SqldaGetKey(): <<<<< ");

         return;

      }

      /// <summary>
      /// Prepare SqlVar structure.
      /// </summary>
      /// <param name="pSQL3_field"></param>
      /// <param name="bindVar"></param>
      /// <param name="charDateBind"></param>
      /// <param name="forInsert"></param>
      public void SqlvarFill (ref Sql3Field sql3Field, bool bindVar, bool charDateBind, bool forInsert)
      {
         GatewayCursor  crsr;
         string         type;
         int            sqlvarLen    = sql3Field.FieldLen;
         string         dataTypeStr = string.Empty;

         GatewayCursorTbl.TryGetValue(sql3Field.GatewayAdapterCursor, out crsr);

         Logger.Instance.WriteDevToLog(string.Format("SqlvarFill(): >>>>> sql3Field.Name = {0}, sql3Field.Storage = {1}, sql3Field.FieldLen = {2}", sql3Field.Name, sql3Field.Storage, sql3Field.FieldLen));

         sql3Field.SqlVar.SqlName = sql3Field.Name;

         if (forInsert && sql3Field.IsBlob())
         {
            Sql3GetBlobType (sql3Field.Fld, out type, out sqlvarLen, out sql3Field.SqlVar.SqlType, out sql3Field.SqlVar.dateType, out sql3Field.SqlVar.typeAffinity);

            ////For blob sqlvar
            sql3Field.SqlVar.IsBlob = true;

            if (forInsert)
               sqlvarLen = sql3Field.FieldLen;
         }
         else if (sql3Field.SqlVar.SqlType != Sql3Type.SQL3TYPE_ROWID)
         {
            Sql3GetType (sql3Field.DataSourceDefinition, sql3Field.Fld, out type, out sqlvarLen, 
                            out sql3Field.SqlVar.SqlType, out sql3Field.SqlVar.dateType, 
                            out dataTypeStr, charDateBind, bindVar, out sql3Field.SqlVar.typeAffinity);
         }
         
         if (sql3Field.SqlVar.SqlType == Sql3Type.SQL3TYPE_DECIMAL || 
              sql3Field.SqlVar.SqlType == Sql3Type.SQL3TYPE_NUMERIC)
         {
            sqlvarLen  = sqlvarLen + 1;
         }

         if (sql3Field.SqlVar.SqlType == Sql3Type.SQL3TYPE_STR)
            sqlvarLen += 1;

         if (sql3Field.SqlVar.SqlType == Sql3Type.SQL3TYPE_WSTR)
            sqlvarLen += 2;


         if (sql3Field.SqlVar.SqlType !=  Sql3Type.SQL3TYPE_ROWID &&
            sql3Field.Fld.Attr == (char)StorageAttributeType.Numeric)
         {
            sql3Field.SqlVar.DataPrecision = sql3Field.Whole + sql3Field.Dec;
            sql3Field.SqlVar.DataScale     = sql3Field.Dec;
         }

         sql3Field.SqlVar.SqlData = sql3Field.Buf;

         if (sql3Field.Storage == FldStorage.NumericString)
         {
            if( sql3Field.Fld.DataSourceDefinition == DatabaseDefinitionType.Normal)
            {
               sql3Field.SqlVar.SqlType = Sql3Type.SQL3TYPE_STR;
               sql3Field.SqlVar.SqlLen++;
            }
         }

         if (sql3Field.Storage == FldStorage.TimeString && sql3Field.PartOfDateTime == SqliteConstants.TIME_OF_DATETIME)
         {
            sql3Field.SqlVar.SqlData = new String(' ', sqlvarLen);
         }

         sql3Field.SqlVar.SqlLen = sqlvarLen;
         sql3Field.SqlVar.NullIndicator = sql3Field.NullIndicator;
         sql3Field.SqlVar.DataSourceType = dataTypeStr;
         sql3Field.SqlVar.Fld = sql3Field.Fld;
         sql3Field.SqlVar.PartOfDateTime = sql3Field.PartOfDateTime;


         Logger.Instance.WriteDevToLog("SqlvarFill(): <<<<<");
      }

      public void SqlvarValFill (Sql3SqlVar sqlVar, Sql3Field pSQL3Field, int varIdx, bool prepared, int quotes)
      {
         Logger.Instance.WriteDevToLog("SqlvarValFill(): >>>>>");

         if (!prepared)
            SqlvarFill(ref pSQL3Field, true, true, false);

         if (Logger.Instance.LogLevel >= Logger.LogLevels.Development && Logger.Instance.LogLevel != Logger.LogLevels.Basic)
            SQLLogging.SQL3LogNumberFld(sqlVar);

         Sql3AddValSqldata(out sqlVar.SqlData, sqlVar.SqlLen, sqlVar.SqlType, sqlVar.SqlLen, pSQL3Field.Fld, pSQL3Field.Buf.ToString(),
                              SqliteConstants.QUOTES_TRUNC, sqlVar.IsMinRange);

         Logger.Instance.WriteDevToLog(string.Format("SqlvarValFill(): <<<<< ind - {0}", pSQL3Field.SqlVar.NullIndicator));
      }

      /// <summary>
      ///  FilCreate ()
      /// </summary>
      /// <param name="sql3Dbd"></param>
      /// <param name="referencedDbhVec"></param>
      /// <param name="dbDefinition"></param>
      /// <returns>SQL3_CODE</returns>
      public SQL3_CODE FilCreate (SQL3Dbd sql3Dbd, List<DataSourceDefinition> referencedDbhVec, DatabaseDefinition dbDefinition)
      {
         int            idx = 0;
         DBField        fld = null;
         string         name = null,
                        dbType = null,
                        type;

         SQL3_CODE      errorCode = SqliteConstants.SQL3_OK;
         DataSourceDefinition            dbh = sql3Dbd.DataSourceDefinition;
         int            len = 0;
         Sql3Type       datatype;
         DateType       dateType;
         int            sqlvarLen;
         string         defVal;
         string         defValUpper;
         bool           defaultExist;
         SQL3Connection sql3Connection;
         int            dbDefaultLen;
         bool           first = true;
         string         sql3Stmt;
         string         dataTypeStr;
         TypeAffinity   typeAffinity;

         Logger.Instance.WriteDevToLog(string.Format("FilCreate(): >>>>> table name - {0}", sql3Dbd.FullName));

         sql3Connection = ConnectionTbl[sql3Dbd.DatabaseName];

         sql3Stmt = string.Format("CREATE TABLE {0} (", sql3Dbd.TableName);

         sql3Dbd.flds = dbh.Fields.Count;

         /* for all the fields in the file */
         for (idx = 0; idx < dbh.Fields.Count; ++idx)
         {
            fld = dbh.Fields[idx];
            if (!((fld.Storage == FldStorage.TimeString) && (fld.PartOfDateTime != 0)))
            {
               name = dbh.Fields[idx].DbName;
               len = fld.Length;

               if (!first)
               {
                  sql3Stmt += ",";
               }
               else
               {
                  first = false;
               }

               Logger.Instance.WriteDevToLog(string.Format("FilCreate(): idx = {0}, field_type[0] = {1},len = {2}", idx, fld.Storage, len));

               sql3Stmt += string.Format("{0} ", name);
               dbType = fld.DbType;


               if (string.IsNullOrEmpty(dbType))
               {
                  Sql3GetType(dbh, fld, out type, out sqlvarLen, out datatype, out dateType, out dataTypeStr, false, false, out typeAffinity);
                  sql3Stmt += type;
               }
               else
               {
                  sql3Stmt += dbType;
               }

               if (!fld.AllowNull)
               {
                  sql3Stmt += " NOT NULL";
               }
               else
               {
                  sql3Stmt += " NULL";
               }

               /*  default value */
               if (string.IsNullOrEmpty(fld.DbDefaultValue))
                  dbDefaultLen = 0;
               else
                  dbDefaultLen = fld.DbDefaultValue.Length;

               if (dbDefaultLen > 0)
               {
                  defaultExist = true;

                  defVal = fld.DbDefaultValue;
                  if (!string.IsNullOrEmpty(defVal))
                  {
                     if (dbDefaultLen <= (int)SqliteConstants.NO_DEFAULT_STR.Length)
                     {
                        defValUpper = defVal.ToUpper();
                        if (defValUpper == SqliteConstants.NO_DEFAULT_STR)
                           defaultExist = false;
                        else
                           defaultExist = true;
                     }
                     if (defaultExist)
                     {
                        if (fld.Attr == (char)StorageAttributeType.Unicode)
                        {
                           if (defVal.Contains("\'"))
                           {
                              sql3Stmt += string.Format(" DEFAULT {0}", defVal);
                           }
                           else
                           {
                              sql3Stmt += string.Format(" DEFAULT \'{0}\'", defVal);
                           }
                        }
                        else
                           sql3Stmt += string.Format(" DEFAULT {0}", defVal);
                     }
                  }
               }
            }
         }

         Statement = sql3Stmt;

         if (sql3Dbd.mode != DbOpen.Reindex)
            errorCode = AddIndexes(sql3Dbd, IndexingMode.CREATE_MODE, true);
         else
            errorCode = AddIndexes(sql3Dbd, IndexingMode.REINDEX_OPEN_MODE, true);

         if (errorCode != SqliteConstants.SQL3_OK)
            errorCode = (int)GatewayErrorCode.BadCreate;
         else
            errorCode = AddForKeys(sql3Dbd, referencedDbhVec, dbDefinition);

         if (errorCode != SqliteConstants.SQL3_OK)
            errorCode = (int)GatewayErrorCode.BadCreate;

         Statement += ")";

         Logger.Instance.WriteDevToLog(string.Format("FilCreate(): <<<<< sql3_stmt = {0}", Statement));

         if (errorCode != SqliteConstants.SQL3_OK)
            errorCode = (int)GatewayErrorCode.BadCreate;

         
         errorCode = SQLiteLow.LibExecuteStatement(Statement, sql3Connection);

         Logger.Instance.WriteDevToLog(string.Format("FilCreate(): <<<<< errcode = {0}", errorCode));

         return (errorCode);
      }

      /// <summary>
      ///  FileExist ()
      /// </summary>
      /// <param name="sql3Dbd"></param>
      /// <returns>SQL3_CODE</returns>
      public SQL3_CODE FileExist (SQL3Dbd sql3Dbd)
      {
         SQL3_CODE errorCode = SqliteConstants.SQL3_OK;

         Logger.Instance.WriteDevToLog("FileExist(): >>>>>");

         errorCode = SQLiteLow.LibFilExist(ConnectionTbl[sql3Dbd.DatabaseName], sql3Dbd.FullName);

         Logger.Instance.WriteDevToLog(string.Format("FileExist(): <<<<< errcode = {0}", errorCode));

         return (errorCode);
      }

      /// <summary>
      ///  AddIndexes ()
      /// </summary>
      /// <param name="sql3Dbd"></param>
      /// <param name="create"></param>
      /// <param name="primaryKeyOnly"></param>
      /// <returns>SQL3_CODE</returns>
      public SQL3_CODE AddIndexes (SQL3Dbd sql3Dbd, IndexingMode create, bool primaryKeyOnly)
      {
         SQL3_CODE      errorCode = SqliteConstants.SQL3_OK;
         int            idx = 0,
                        clusterdIdx = SqliteConstants.NULL_PRIMARY_KEY;
         SQL3Connection sql3Connection = null;

         sql3Connection = ConnectionTbl[sql3Dbd.DatabaseName];

         Logger.Instance.WriteDevToLog(string.Format("AddIndexes(): >>>>> number = {0}", sql3Dbd.DataSourceDefinition.Keys.Count));

         /* Find which one is the Clustered index */
         for (idx = 0; idx < sql3Dbd.DataSourceDefinition.Keys.Count; ++idx)
         {
            if (sql3Dbd.DataSourceDefinition.Keys[idx].CheckMask(KeyMasks.KeyTypeReal) && sql3Dbd.DataSourceDefinition.Keys[idx].CheckMask(KeyMasks.KeyClusteredMask))
            {
               clusterdIdx = idx;
               break;
            }
         }

         if (errorCode == SqliteConstants.SQL3_OK)
         {
            /* For all the indexes */
            for (idx = 0; idx < sql3Dbd.DataSourceDefinition.Keys.Count; ++idx)
            {
               if (sql3Dbd.DataSourceDefinition.Keys[idx].CheckMask(KeyMasks.KeyTypeReal))
               {
                  if (primaryKeyOnly == false)
                     Statement = string.Empty;
                  errorCode = AddIndex(sql3Dbd, idx, idx == clusterdIdx, create, primaryKeyOnly);
               }
               if (errorCode != SqliteConstants.SQL3_OK)
               {
                  break;
               }
            }
         }

         Logger.Instance.WriteDevToLog(string.Format("AddIndexes(): <<<<< errorCode = {0}", errorCode));

         return (errorCode);
      }

      /// <summary>
      ///  AddIndex ()
      /// </summary>
      /// <param name="sql3Dbd"></param>
      /// <param name="idx"></param>
      /// <param name="clusteredIndex"></param>
      /// <param name="create"></param>
      /// <param name="primaryKeyOnly"></param>
      /// <returns>SQL3_CODE</returns>
      public SQL3_CODE AddIndex(SQL3Dbd sql3Dbd, int idx, bool clusteredIndex, IndexingMode create, bool primaryKeyOnly )
      {
         SQL3_CODE      errorCode = SqliteConstants.SQL3_OK;
         string         name;
         int            segIdx;
         DBSegment      seg;
         DataSourceDefinition      dbh = sql3Dbd.DataSourceDefinition;
         DBKey          key = dbh.Keys[idx];
         string         uniStr;
         string         unique = "UNIQUE",
                        noUnique = "";
         string         keyName = string.Empty;
         SQL3Connection sql3Connection;

         sql3Connection = (SQL3Connection)ConnectionTbl[sql3Dbd.DatabaseName];

         Logger.Instance.WriteDevToLog(string.Format("AddIndex(): >>>>> number = {0}", idx));

         if ((key.CheckMask(KeyMasks.KeyDropReIndex) && create == IndexingMode.REINDEX_CLOSE_MODE) ||
             (!key.CheckMask(KeyMasks.KeyDropReIndex) && create == IndexingMode.REINDEX_OPEN_MODE) ||
              create == IndexingMode.CREATE_MODE)
         {
            /* For a key with Primary key constraint, dont create the index, but only add the 
               Primary key Constraint. */
            if (key.CheckMask(KeyMasks.KeyPrimaryMask) && primaryKeyOnly)
            {
               //* get the key name 
               keyName = Sql3GetKeyName(sql3Dbd.DataSourceDefinition.Name, key);

               Statement += string.Format(", CONSTRAINT {0} PRIMARY KEY (", keyName);

               //* For all segments of the index 
               for (segIdx = 0; segIdx < key.Segments.Count; ++segIdx)
               {
                  seg = key.Segments[segIdx];
                  if (!((seg.Field.Storage == FldStorage.TimeString) &&
                        (seg.Field.PartOfDateTime != 0)))
                  {
                     //* include a comma after each segment 
                     if (segIdx > 0)
                        Statement += ", ";

                     //* get the field name for the segment 
                     name = seg.Field.DbName;
                     Statement += name;
                     if (seg.CheckMask(SegMasks.SegDirDescendingMask))
                     {
                        Statement += " DESC";
                     }
                  }

               }

               Statement += ")";

               if (errorCode == SqliteConstants.SQL3_SQL_NOTFOUND)
                  errorCode = SqliteConstants.SQL3_OK;
            }
            else if (!key.CheckMask(KeyMasks.KeyPrimaryMask) && !primaryKeyOnly)
            {
               if (key.CheckMask(KeyMasks.UniqueKeyModeMask))
                  uniStr = unique;
               else
                  uniStr = noUnique;


               keyName = Sql3GetKeyName(sql3Dbd.DataSourceDefinition.Name, key);

               Statement += string.Format("CREATE {0} INDEX {1} ", uniStr, keyName);
               Statement += string.Format("ON {0} (", sql3Dbd.TableName);

               /* For all segments of the index */
               for (segIdx = 0; segIdx < key.Segments.Count; ++segIdx)
               {
                  seg = key.Segments[segIdx];
                  if (!((seg.Field.Storage == FldStorage.TimeString) &&
                        (seg.Field.PartOfDateTime != 0)))
                  {
                     /* include a comma after each segment */
                     if (segIdx > 0)
                     {
                        Statement += ", ";
                     }

                     /* get the field name for the segment */
                     name = seg.Field.DbName;
                     Statement += name;

                     if (seg.CheckMask(SegMasks.SegDirDescendingMask))
                     {
                        Statement += " DESC";
                     }
                  }
               }

               Statement += ")";

               errorCode = SQLiteLow.LibExecuteStatement(Statement, sql3Connection);

               if (errorCode == SqliteConstants.SQL3_SQL_NOTFOUND)
                  errorCode = SqliteConstants.SQL3_OK;
            }
         }
         else
         {
            Logger.Instance.WriteDevToLog("AddIndex(): found CONSTRAINT - not added");
         }

         Logger.Instance.WriteDevToLog(string.Format("AddIndex(): <<<<< errorCode = %d", errorCode));

         return (errorCode);
      }

      /// <summary>
      ///  DropIndexes ()
      /// </summary>
      /// <param name="sql3Dbd"></param>
      /// <returns>SQL3_CODE</returns>
      public SQL3_CODE DropIndexes (SQL3Dbd sql3Dbd)
      {
         SQL3_CODE errorCode = SqliteConstants.SQL3_OK;
         short     idx = 0;

         Logger.Instance.WriteDevToLog(string.Format("DropIndexes(): >>>>> number of indexes to drop : {0}", sql3Dbd.DataSourceDefinition.Keys.Count));

         for (idx = 0; idx < sql3Dbd.DataSourceDefinition.Keys.Count; ++idx)
         {
            if (sql3Dbd.DataSourceDefinition.Keys[idx].CheckMask(KeyMasks.KeyTypeReal))
            {
               errorCode = DropIndex(sql3Dbd, idx);
            }

            if (errorCode != SqliteConstants.SQL3_OK)
               break;
         }

         Logger.Instance.WriteDevToLog(string.Format("DropIndexes(): <<<<< errorCode = {0}", errorCode));

         return (errorCode);
      }

      /// <summary>
      ///  DropIndexes ()
      /// </summary>
      /// <param name="sql3Dbd"></param>
      /// <returns>SQL3_CODE</returns>
      public SQL3_CODE DropIndex (SQL3Dbd sql3Dbd, short idx)
      {
         SQL3_CODE      errorCode = SqliteConstants.SQL3_OK;
         string         name      = string.Empty;
         string         nameBuf   = string.Empty;
         string         tblName   = string.Empty;
         SQL3Connection sql3Connection = null;

         sql3Connection = ConnectionTbl[sql3Dbd.DatabaseName];

         Logger.Instance.WriteDevToLog(string.Format("DropIndex(): >>>>> key to drop = {0}", idx));

         if (sql3Dbd.DataSourceDefinition.Keys[idx].CheckMask(KeyMasks.KeyDropReIndex) || (sql3Dbd.mode != DbOpen.Reindex))
         {
            name = sql3Dbd.DataSourceDefinition.Keys[idx].KeyDBName;

            Sql3SeperateTable(sql3Dbd.FullName, out tblName);

            if (!string.IsNullOrEmpty(name))
            {
               nameBuf += name;
            }
            if (sql3Dbd.DataSourceDefinition.Keys[idx].CheckMask(KeyMasks.KeyPrimaryMask))
            {
               nameBuf += string.Format("ALTER TABLE {0} DROP {1}", tblName, name);
               errorCode = SQLiteLow.LibDrop(ConnectionTbl[sql3Dbd.DatabaseName], nameBuf, DropObject.SQL3_DROP_PRMKEY);
            }
            else
               errorCode = SQLiteLow.LibDrop(ConnectionTbl[sql3Dbd.DatabaseName], nameBuf, DropObject.SQL3_DROP_INDEX);
         }

         Logger.Instance.WriteDevToLog(string.Format("DropIndex(): <<<<< errorCode = {0}", errorCode));
         
         return errorCode;
      }

      public SQL3_CODE StartPosOpen (GatewayAdapterCursor dbCrsr, bool lastPos, bool dirOrig)
      {
         SQL3_CODE      errcode = SqliteConstants.SQL3_OK;
         GatewayCursor  crsr;
         SQL3Cursor     sql3Cursor;
         SQL3Dbd        sql3Dbd;
         int            keySqlvars = 0;
         bool           hasRange = ((dbCrsr.Ranges.Count > 0 || dbCrsr.SqlRng != null) ? true : false),
                        has_key = (dbCrsr.Definition.Key != null ? true : false);
         int            crsrHdl;
         int            cnt = 0;


         GatewayCursorTbl.TryGetValue(dbCrsr, out crsr);
         DbdTbl.TryGetValue(dbCrsr.Definition.DataSourceDefinition, out sql3Dbd);

         Logger.Instance.WriteDevToLog(string.Format("StartPosOpen(): >>>>> crsr.StrtposCnt = {0}, lastPos = {1}", crsr.StrtposCnt, lastPos));

         if (hasRange)
            crsr.CRange = Sql3CursorAlloc("Range", crsr.CRange);
         else
            crsr.CRead = Sql3CursorAlloc("Read", crsr.CRead);

         crsrHdl = crsr.OuterJoin == true ? 0 : -1;
         if (hasRange)
         {
            if (dbCrsr.CursorType == CursorType.Join)
            {
               cnt = BuildRangesStmt(dbCrsr, true, crsrHdl);
               if (!crsr.OuterJoin)
                  crsr.JoinRngs = cnt;

               crsr.StmtJoinRanges = string.Empty;
               if (!string.IsNullOrEmpty(Statement))
                  crsr.StmtJoinRanges = Statement;
            }
         }

         /* build the getkey statement and sqlda and execute it */
         if (has_key || sql3Dbd.IsView)
            errcode = Sql3GetKeyForStartPos(dbCrsr, lastPos);

         if (errcode == SqliteConstants.SQL3_SQL_NOTFOUND)
            return errcode;

         // resync the pCursor because SQL3_cursor_tbl might have moved by the mem_exp inside sql3_cursor_alloc 
         // which is called from sql3_get_key_for_startpos
         if (hasRange)
            sql3Cursor = CursorTbl[crsr.CRange];
         else
            sql3Cursor = CursorTbl[crsr.CRead];

         sql3Cursor.IsStartPos = true;

         if (dirOrig)
            keySqlvars = BuildStartPosStmt(dbCrsr, false);
         else
            keySqlvars = BuildStartPosStmt(dbCrsr, true);

         if (dbCrsr.SqlRng == null)
            crsr.SqlRngs = 0;
         else
         {
            Sql3ResizeBufferForWhereClause(dbCrsr);
            crsr.SqlRngs = SQL3StmtBuildSqlRngs(dbCrsr, Statement);
            if (string.IsNullOrEmpty(crsr.StmtSqlRng) ||
               crsr.StmtSqlRng != Statement)
            {
               crsr.StmtSqlRng = string.Empty;
               crsr.StmtSqlRng = Statement;
            }
         }

         if (hasRange)
         {
            crsr.Rngs = BuildRangesStmt(dbCrsr, false, crsrHdl);
            if (crsr.Rngs == SqliteConstants.INVALID_RANGE)
            {
               Logger.Instance.WriteDevToLog("StartPosOpen(): <<<<< range statement error");
            }
            else
            {
               /* TEMPLATE 2 */
               crsr.StmtRanges = string.Empty;
               if (!string.IsNullOrEmpty(Statement))
                  crsr.StmtRanges = Statement;
            }

            if (crsr.StartPos != null)
               crsr.StartPos.SQL3SqldaFree();
            else
               crsr.StartPos = new Sql3Sqldata(this);

            if (has_key || sql3Dbd.IsView)
            {
               crsr.StartPos.SQL3SqldaAlloc(crsr.Rngs + crsr.SqlRngs + keySqlvars);

               /* fill the sqlvars with range parameters */
               if (crsr.Rngs > 0)
               {
                  cnt = crsr.StartPos.SQL3SqldaAllRanges(dbCrsr, crsr, false);
               }

               /* fill the sqlvars with sql ranges values */
               if (crsr.SqlRngs > 0)
               {
                  cnt += crsr.StartPos.SQL3SqldaSqlRange(dbCrsr, crsr.StartPos.SqlVars[crsr.Rngs], false);
               }

               /* copy the key sqlvars to the startpos sqlda */
               if (keySqlvars > 0 && errcode == SqliteConstants.SQL3_OK)
               {
                  cnt += crsr.Key.SQL3SqldaNoNullCopy(crsr.StartPos.SqlVars, cnt);
               }
            }
            else
            {
               crsr.StartPos.SQL3SqldaAlloc(crsr.Rngs + crsr.SqlRngs + 1);

               /* fill the sqlvars with range parameters */
               if (crsr.Rngs > 0)
               {
                  cnt = crsr.StartPos.SQL3SqldaAllRanges(dbCrsr, crsr, false);
               }

               /* fill the sqlvars with sql ranges values */
               if (crsr.SqlRngs > 0)
               {
                  cnt += crsr.StartPos.SQL3SqldaSqlRange(dbCrsr, crsr.StartPos.SqlVars[crsr.Rngs], false);
               }

               /* copy the key sqlvars to the startpos sqlda */
               if (errcode == SqliteConstants.SQL3_OK)
               {
                  if (lastPos)
                  {
                     cnt += crsr.StartPos.SQL3SqldaFromDbpos(dbCrsr, cnt, crsr.LastPos, false, false);
                  }
                  else
                  {
                     cnt += crsr.StartPos.SQL3SqldaFromDbpos(dbCrsr, cnt, dbCrsr.Definition.StartPosition, false, false);
                  }
               }
            }
         }
         else if (keySqlvars > 0)
         {
            if (crsr.StartPos != null)
               crsr.StartPos.SQL3SqldaFree();
            else
               crsr.StartPos = new Sql3Sqldata(this);
            if (has_key || sql3Dbd.IsView)
            {
               crsr.StartPos.SQL3SqldaAlloc(keySqlvars);
               // copy the key sqlvars to the startpos sqlda 
               cnt = crsr.Key.SQL3SqldaNoNullCopy(crsr.StartPos.SqlVars, 0);
            }
            else
            {
               crsr.StartPos.SQL3SqldaAlloc(1);
               //Allocate the sqlda for rowid
               if (lastPos)
               {
                  cnt = crsr.StartPos.SQL3SqldaFromDbpos(dbCrsr, 0, crsr.LastPos, false, false);
               }
               else
               {
                  cnt = crsr.StartPos.SQL3SqldaFromDbpos(dbCrsr, 0, dbCrsr.Definition.StartPosition, false, false);
               }
            }
         }

         sql3Cursor.StartPosLevel = 0;

         if (errcode == SqliteConstants.SQL3_OK)
         {
            errcode = StartPosReopen(dbCrsr, dirOrig);
         }

         Logger.Instance.WriteDevToLog(string.Format("StartPosOpen(): <<<<< errcode = {0}", errcode));

         return errcode;
      }

      public SQL3_CODE StartPosReopen (GatewayAdapterCursor dbCrsr, bool dirOrig)
      {
         SQL3_CODE      errcode        = SqliteConstants.SQL3_SQL_NOTFOUND;
         GatewayCursor  crsr;
         SQL3Cursor     sql3Cursor;
         Sql3Stmt       sql3Stmt;
         SQL3Dbd        sql3Dbd;
         bool           hasRange       = (dbCrsr.Ranges.Count > 0 || dbCrsr.SqlRng != null) ? true : false,
                        strt_pos       = !(dbCrsr.Definition.StartPosition.IsZero);
         string         rowid          = string.Empty;
         int            paramsCnt;
         bool           whereExist,
                        has_key        = (dbCrsr.Definition.Key != null ? true : false);
         int            keySqlvars;
         string         rowidStmt;
         string         prefix         = string.Empty;     /* added for MAGIC8 */
         string         noPrefix       = string.Empty;

         GatewayCursorTbl.TryGetValue(dbCrsr, out crsr);
         string order = crsr.StmtOrderBy;


         DbdTbl.TryGetValue(dbCrsr.Definition.DataSourceDefinition, out sql3Dbd);

         Logger.Instance.WriteDevToLog(string.Format("StartPosReopen(): >>>>> crsr.StrtposCnt = {0}", crsr.StrtposCnt));

         if (dbCrsr.CursorType == CursorType.Join)
         {
            prefix = SQL3PrefixBuf;
            prefix = string.Format("{0}.", crsr.DbhPrefix[0]);
         }
         else
         {
            prefix = noPrefix;
         }

         if (hasRange)
         {
            crsr.CRange = Sql3CursorAlloc("Range", crsr.CRange);
            sql3Cursor = CursorTbl[crsr.CRange];
         }
         else
         {
            crsr.CRead = Sql3CursorAlloc("Read", crsr.CRead);
            sql3Cursor = CursorTbl[crsr.CRead];
         }

         Logger.Instance.WriteDevToLog(string.Format("StartPosReopen(): sql3Cursor.StartPosLevel = {0}, stmt = {1}:", sql3Cursor.StartPosLevel, crsr.StmtStartpos[sql3Cursor.StartPosLevel]));

         if (sql3Dbd.IsView)
            rowid = string.Empty;

         /* if the startpos phrase is an empty string (greater then field is null) search for the next non empty one */
         for (; sql3Cursor.StartPosLevel < crsr.StrtposCnt; sql3Cursor.StartPosLevel++)
         {
            Logger.Instance.WriteDevToLog(string.Format("StartPosReopen(): looping level {0}", sql3Cursor.StartPosLevel));
            
            if (crsr.StmtStartpos[sql3Cursor.StartPosLevel].Length != 0)
               break;
         }

         /* if no more phrases, don't reopen */
         if (sql3Cursor.StartPosLevel < crsr.StrtposCnt)
         {
            errcode = SqliteConstants.SQL3_OK;

            Logger.Instance.WriteDevToLog(string.Format("StartPosReopen(): using phrase level {0} = {1}", sql3Cursor.StartPosLevel, crsr.StmtStartpos[sql3Cursor.StartPosLevel]));
               
            if (dirOrig)
               order = crsr.StmtOrderBy;
            else
               order = crsr.StmtOrderByRev;

            if (!sql3Dbd.IsView)
            {
               if (dbCrsr.CursorType == CursorType.Join)
                  rowid = string.Empty;
               else
               {
                  rowidStmt = string.Format("{0}{1}", prefix, SqliteConstants.SQL3_ROWID_ST_A);

                  if (dbCrsr.Definition.FieldsDefinition.Count == 0 && (crsr.StmtExtraFields.Length == 0))
                  {
                     rowid = string.Format(" {0}", rowidStmt);
                  }
                  else
                  {
                     rowid = string.Format(", {0}", rowidStmt);
                  }
               }
            }


            crsr.SStrt = Sql3StmtAlloc("sStrt", crsr.SStrt, sql3Dbd.DatabaseName);

            sql3Stmt = StmtTbl[crsr.SStrt];
            sql3Cursor.StmtIdx = sql3Stmt.Idx;

            if (sql3Stmt.IsOpen)
               SQLiteLow.LibClose(ConnectionTbl[sql3Dbd.DatabaseName], sql3Stmt);

            Statement = string.Format("SELECT {0} ", crsr.StmtFields);

            if (crsr.StmtExtraFields.Length != 0)
            {
               if (dbCrsr.Definition.FieldsDefinition.Count == 0)
               {
                  Statement += string.Format("%s", crsr.StmtExtraFields);

               }
               else
               {
                  Statement += string.Format(",{0}", crsr.StmtExtraFields);
               }
            }

            Statement += string.Format("{0} FROM {1}", rowid, crsr.StmtAllTablesWithOtimizer);

            whereExist = false;
            if (!crsr.StmtStartpos[sql3Cursor.StartPosLevel].Equals("NULLWHERE"))
            {
               if (hasRange && !string.IsNullOrEmpty(crsr.StmtRanges))
               {
                  whereExist = true;
                  Statement += string.Format(" WHERE ({0}) AND ({1})", crsr.StmtRanges, crsr.StmtStartpos[sql3Cursor.StartPosLevel]);
               }
               else
               {
                  Statement += string.Format(" WHERE ({0})", crsr.StmtStartpos[sql3Cursor.StartPosLevel]);
               }

               whereExist = true;

            }
            if (!string.IsNullOrEmpty(crsr.StmtSqlRng))
            {
               if (crsr.StmtSqlRng.Trim().Length != 0)
               {
                  if (whereExist)
                  {
                     Statement += string.Format(" AND ({0})", crsr.StmtSqlRng);
                  }
                  else
                  {
                     Statement += string.Format(" WHERE ({0})", crsr.StmtSqlRng);
                  }
                  whereExist = true;
               }
            }

            if (dbCrsr.CursorType == CursorType.Join && !string.IsNullOrEmpty(crsr.StmtJoinCond))
            {
               if (whereExist)
               {
                  Statement += string.Format(" AND ({0})", crsr.StmtJoinCond);
               }
               else
               {
                  Statement += string.Format(" WHERE ({0})", crsr.StmtJoinCond);
               }

               whereExist = true;
            }
            if (!string.IsNullOrEmpty(order))
            {
               Statement += string.Format(" ORDER BY {0}", order);
            }

            /* prepare the statement */
            sql3Stmt.Buf = Statement;

            keySqlvars = (has_key) ? crsr.Key.Sqld : 0;
            if (hasRange)
            {
               if (keySqlvars > 0 || sql3Dbd.IsView)
               {
                  paramsCnt = keySqlvars - sql3Cursor.StartPosLevel;

                  for (short i = 0; i < keySqlvars; i++)
                     if (crsr.Key.SqlVars[i].PartOfDateTime == SqliteConstants.TIME_OF_DATETIME)
                        paramsCnt--;

                  crsr.StartPos.Sqld = SQL3StartposCountParams(dbCrsr, paramsCnt);
               }
               else
               {
                  if (strt_pos)
                  {
                     crsr.StartPos.SQL3SqldaFromDbpos(dbCrsr, crsr.Rngs, dbCrsr.Definition.StartPosition, false, false);
                  }
                  else
                  {
                     crsr.StartPos.SQL3SqldaFromDbpos(dbCrsr, crsr.Rngs, crsr.LastPos, false, false);
                  }
               }
            }
            else
            {
               if (keySqlvars > 0 || sql3Dbd.IsView)
               {
                  paramsCnt = keySqlvars - sql3Cursor.StartPosLevel;

                  for (short i = 0; i < keySqlvars; i++)
                     if (crsr.Key.SqlVars[i].PartOfDateTime == SqliteConstants.TIME_OF_DATETIME)
                        paramsCnt--;

                  crsr.StartPos.Sqld = SQL3StartposCountParams(dbCrsr, paramsCnt);
               }
               else
               {
                  if (strt_pos)
                  {
                     crsr.StartPos.SQL3SqldaFromDbpos(dbCrsr, crsr.Rngs, dbCrsr.Definition.StartPosition, false, false);
                  }
                  else
                  {
                     crsr.StartPos.SQL3SqldaFromDbpos(dbCrsr, crsr.Rngs, crsr.LastPos, false, false);
                  }
               }

            }

            if (errcode == SqliteConstants.SQL3_OK)
            {
               sql3Cursor.OutputSqlda = crsr.Output;
               sql3Cursor.InputSqlda = crsr.StartPos;

               errcode = SQLiteLow.LibPrepareAndExecute(ConnectionTbl[sql3Dbd.DatabaseName], sql3Stmt, sql3Cursor);

               if (Logger.Instance.LogLevel >= Logger.LogLevels.Support)
               {
                  SQL3StmtBuildWithValues(sql3Stmt.Buf, dbCrsr.Definition.DataSourceDefinition, sql3Cursor.InputSqlda, sql3Stmt, false);
                  Logger.Instance.WriteSupportToLog(string.Format("\tSTMT: {0}", sql3Stmt.StmtWithValues), true);
               }

               sql3Cursor.InputSqlda = null;
            }
         }

         Logger.Instance.WriteDevToLog(string.Format("StartPosReopen(): <<<<< errcode = {0}, level = {1}", errcode, sql3Cursor.StartPosLevel));

         return errcode;
      }

      /// <summary>
      ///  NoStartPosOpen ()
      /// </summary>
      /// <param name="dbCrsr"></param>
      /// <param name="dirOrig"></param>
      /// <returns>SQL3_CODE</returns>
      public SQL3_CODE NoStartPosOpen (GatewayAdapterCursor dbCrsr, bool dirOrig)
      {
         SQL3_CODE      errorCode = SqliteConstants.SQL3_OK;
         GatewayCursor  crsr ;
         SQL3Cursor     sql3Cursor;
         Sql3Stmt       sql3Stmt = null;
         SQL3Dbd        sql3Dbd = null;
         string         order = null;


         GatewayCursorTbl.TryGetValue(dbCrsr, out crsr);
         DbdTbl.TryGetValue(dbCrsr.Definition.DataSourceDefinition, out sql3Dbd);

         Logger.Instance.WriteDevToLog(string.Format("NoStartPosOpen(): >>>>> sql3Dbd.IsView = {0}, crsr.Rngs = {1}", sql3Dbd.IsView, crsr.Rngs));

         crsr.CRead = Sql3CursorAlloc("Read", crsr.CRead);
         sql3Cursor = CursorTbl[crsr.CRead];

         if (dirOrig)
         {
            crsr.SReadA = Sql3StmtAlloc("sReadA", crsr.SReadA, sql3Dbd.DatabaseName);
            sql3Stmt = StmtTbl[crsr.SReadA];
            order = crsr.StmtOrderBy;
         }
         else
         {
            crsr.sReadD = Sql3StmtAlloc("sReadD", crsr.sReadD, sql3Dbd.DatabaseName);
            sql3Stmt = StmtTbl[crsr.sReadD];
            order = crsr.StmtOrderByRev;
         }

         Logger.Instance.WriteDevToLog("NoStartPosOpen(): no ranges");

         if (!sql3Stmt.IsPrepared)
         {
            sql3Cursor.StmtIdx = sql3Stmt.Idx;

            Statement = string.Format("SELECT {0} ", crsr.StmtFields);

            if (!string.IsNullOrEmpty(crsr.StmtExtraFields))
            {
               if (dbCrsr.Definition.FieldsDefinition.Count > 0)
               {
                  Statement += string.Format(", {0}", crsr.StmtExtraFields);
               }
               else
               {
                  Statement += string.Format("{0}", crsr.StmtExtraFields);
               }
            }
            if (!sql3Dbd.IsView)
            {
               if (dbCrsr.Definition.FieldsDefinition.Count == 0 && crsr.StmtExtraFields.Length == 0)
               {
                  Statement += string.Format("{0}", "rowid");
               }
               else if (dbCrsr.CursorType != CursorType.Join)
               {
                  Statement += string.Format(", {0}", "rowid");
               }
            }

            Statement += string.Format(" FROM {0}", crsr.StmtAllTablesWithOtimizer);

            if (dbCrsr.CursorType == CursorType.Join && crsr.StmtJoinCond != null)
            {
               Statement += string.Format(" WHERE {0}", crsr.StmtJoinCond);
            }

            if (order != null)
            {
               Statement += string.Format(" ORDER BY {0}", order);
            }

            // prepare the statement 

            sql3Stmt.Buf = Statement;

            sql3Cursor.OutputSqlda = crsr.Output;

            //if (pSQL3_dbd.arrayBufferSize == SqliteConstants.NULL_ARRAY_SIZE)
            //   pCursor.rows = sql3_calc_array_size(0, db_crsr);
            //else
            //   pCursor->rows = pSQL3_dbd->array_buffer_size;

            errorCode = SQLiteLow.LibPrepareAndExecute(ConnectionTbl[sql3Dbd.DatabaseName], sql3Stmt, sql3Cursor);
         }
         else
         {
            sql3Cursor.StmtIdx = sql3Stmt.Idx;

            if (!sql3Stmt.IsOpen)
            {
               errorCode = SQLiteLow.LibPrepareAndExecute(ConnectionTbl[sql3Dbd.DatabaseName], sql3Stmt, sql3Cursor);
            }
         }

         Logger.Instance.WriteDevToLog("NoStartPosOpen(): <<<<<");

         return errorCode;
      }

      public SQL3_CODE RangeOpen (GatewayAdapterCursor db_crsr, bool dirOrig)
      {
         SQL3_CODE         errcode    = SqliteConstants.SQL3_OK;
         GatewayCursor     crsr;
         SQL3Cursor        sql3Cursor; 
         Sql3Stmt          sql3Stmt;
         SQL3Dbd           sql3Dbd;
         string            rowid       = SqliteConstants.SQL3_ROWID_ST_A,/* added for MAGIC8 */
                           where       = "WHERE",
                           and         = "AND",
                           order       = string.Empty;
         bool              doPrepare   = false;
         bool              whereExist  = false;
         int               crsrHdl;
         int               cnt         = 0;

         GatewayCursorTbl.TryGetValue(db_crsr, out crsr);
         DbdTbl.TryGetValue(db_crsr.Definition.DataSourceDefinition, out sql3Dbd);

         Logger.Instance.WriteDevToLog(string.Format("RangeOpen(): >>>>> sql3Dbd.IsView = {0}, crsr->rngs = {1}", sql3Dbd.IsView, crsr.Rngs));

         crsr.CRange = Sql3CursorAlloc("Range", crsr.CRange);
         sql3Cursor = CursorTbl[crsr.CRange];

         sql3Cursor.IsRange = true;

         if (db_crsr.SqlRng == null)
               crsr.SqlRngs = 0;
         else
         {
            Sql3ResizeBufferForWhereClause(db_crsr);
            crsr.SqlRngs = SQL3StmtBuildSqlRngs (db_crsr, Statement);
            if (string.IsNullOrEmpty(crsr.StmtSqlRng) || crsr.StmtSqlRng != Statement)
            {
               doPrepare = true;
               crsr.StmtSqlRng = Statement;
            }
         }

         crsrHdl = (crsr.OuterJoin) ? 0 : -1;
         if (db_crsr.CursorType == CursorType.Join)
         {
            // build the JOIN RANGES and copy it to GTWY_cursor 
            cnt = BuildRangesStmt (db_crsr, true, crsrHdl);
            if (! crsr.OuterJoin)
               crsr.JoinRngs = cnt;

            crsr.StmtJoinRanges = string.Empty;

            if (!string.IsNullOrEmpty(Statement))
            {
               crsr.StmtJoinRanges = Statement;
            }
            else
            {
               crsr.StmtJoinRanges = string.Empty;
            }
 
         }

         // build the where clause and copy it to GTWY_cursor
         crsr.Rngs = BuildRangesStmt (db_crsr, false, crsrHdl);

         if (crsr.Rngs == SqliteConstants.INVALID_RANGE)
         {

            Logger.Instance.WriteDevToLog("RangeOpen(): <<<<< range statement error");
            return 0;
         }

         if ((crsr.StmtRanges != null) && 
            (crsr.StmtRanges != Statement || !string.IsNullOrEmpty(crsr.StmtStartpos[0])))
            doPrepare = true;

         if (dirOrig)
         {
            crsr.SRngA = Sql3StmtAlloc("sRngA", crsr.SRngA, sql3Dbd.DatabaseName);
            sql3Stmt = StmtTbl[crsr.SRngA];
            order = crsr.StmtOrderBy;
         }
         else
         {
            crsr.SRngD = Sql3StmtAlloc ("sRngD", crsr.SRngD, sql3Dbd.DatabaseName);
            sql3Stmt = StmtTbl[crsr.SRngD];
            order = crsr.StmtOrderByRev;
         }

         if (doPrepare || !StmtTbl[sql3Stmt.Idx].IsPrepared)
         {
            if (sql3Stmt.IsOpen)
            {
               SQLiteLow.LibClose(ConnectionTbl[sql3Dbd.DatabaseName], sql3Stmt);
            }
            else
            {
               if (sql3Stmt.IsPrepared)
               {
                  //sql3_lib_release_stmt(pSQL3_stmt, SQLITE_FINALIZE);
               }
            }

            sql3Cursor.StmtIdx = sql3Stmt.Idx;

            if (string.IsNullOrEmpty(Statement) && (db_crsr.CursorType != CursorType.Join ||
                                          (db_crsr.CursorType == CursorType.Join &&
                                           string.IsNullOrEmpty(crsr.StmtJoinCond))))
            {
               where = string.Empty;
            }
            else
            {
               whereExist = true;
            }

            Logger.Instance.WriteDevToLog(string.Format("RangeOpen(): crsr->rngs = {0}", crsr.Rngs));

            crsr.StmtRanges = Statement;

            if (string.IsNullOrEmpty(crsr.StmtJoinCond) || string.IsNullOrEmpty(Statement))
            {
               and = string.Empty;
            }
            if (string.IsNullOrEmpty(crsr.StmtJoinCond))
            {
               crsr.StmtJoinCond = string.Empty;
            }

            if (db_crsr.CursorType == CursorType.Join)
            {
               rowid = string.Empty;
            }

            Statement = string.Format("SELECT {0}", crsr.StmtFields);

            if (!string.IsNullOrEmpty(crsr.StmtExtraFields))
            {
               if (db_crsr.Definition.FieldsDefinition.Count == 0)
               {
                  Statement += string.Format("{0}", crsr.StmtExtraFields);
               }
               else
               {
                  Statement += string.Format(", {0} ", crsr.StmtExtraFields);
               }
            }

            if (sql3Dbd.IsView == false)
            {
               if (db_crsr.Definition.FieldsDefinition.Count == 0 && crsr.StmtExtraFields.Length == 0)
               {
                  Statement += string.Format(" {0} ", "rowid");
               }
               else
               {
                  Statement += string.Format(" {0}{1} ", rowid[0] == 0 ? "" : ",", rowid);
               }
            }
         
            Statement += string.Format("FROM {0} ", crsr.StmtAllTablesWithOtimizer);
      
            if (whereExist)
            {
               Statement += string.Format("{0} {1} {2} {3} ", where, crsr.StmtRanges, and, crsr.StmtJoinCond);
            }

            if (!string.IsNullOrEmpty(crsr.StmtSqlRng))
            {
               if(crsr.StmtSqlRng.Trim().Length != 0)
               {
                  if (whereExist)
                  {
                     Statement += string.Format(" AND ({0}) ", crsr.StmtSqlRng);
                  }
                  else
                  {
                     Statement += string.Format(" WHERE ({0}) " ,crsr.StmtSqlRng);
                  }
                  whereExist = true;
               }
            }

            if (order != null)
            {
               Statement += string.Format(" ORDER BY {0}", order);
            }

            if (string.IsNullOrEmpty(crsr.StmtJoinCond))
            {
               crsr.StmtJoinCond = string.Empty;
            }
      
            // prepare the statement 
            sql3Stmt.Buf = Statement;

            /* allocate the ranges sqlda */
            if (crsr.Rngs + crsr.SqlRngs > 0)
            {
               crsr.Ranges.SQL3SqldaAlloc(crsr.Rngs + crsr.SqlRngs);
            }

            /* fill the SQLDA with input parameters */
            if (crsr.Rngs > 0)
            {
               crsr.Ranges.SQL3SqldaAllRanges(db_crsr, crsr, false);
            }

            //assert (cnt == crsr->rngs);
            /* fill the sqlvars with sql ranges values */
            if (crsr.SqlRngs > 0)
            {
               cnt += crsr.Ranges.SQL3SqldaSqlRange (db_crsr, crsr.Ranges.SqlVars[crsr.Rngs], false); 
            }

            if (crsr.Ranges != null)
            {
               if (Logger.Instance.LogLevel >= Logger.LogLevels.Development && Logger.Instance.LogLevel != Logger.LogLevels.Basic)
               {
                  SQLLogging.SQL3LogSqlda(crsr.Ranges, "parameter sqlda for ranges");
               }
            }

            sql3Cursor.InputSqlda  = crsr.Ranges;
            sql3Cursor.OutputSqlda = crsr.Output;
      
            if (sql3Dbd.arrayBufferSize == SqliteConstants.NULL_ARRAY_SIZE)
            {
               //pCursor.rows = sql3_calc_array_size(0, db_crsr);
            }
            else
            {
               //pCursor->rows = pSQL3_dbd->array_buffer_size;
            }

            errcode = SQLiteLow.LibPrepare(ConnectionTbl[sql3Dbd.DatabaseName], sql3Stmt, sql3Cursor);
         }
         else
         {
            sql3Cursor.StmtIdx = sql3Stmt.Idx;
            sql3Cursor.InputSqlda = crsr.Ranges;

            // call SQL3SqldaRange & SQL3SqldaSqlRange to copy new values of the variables to 
            // bind, without reallocating the Range Sqlda variables.
            if (crsr.Rngs > 0)
               cnt = crsr.Ranges.SQL3SqldaAllRanges (db_crsr, crsr, true);

            /* fill the sqlvars with sql ranges values */
            if (crsr.SqlRngs > 0 && errcode == SqliteConstants.SQL3_OK)
            {
               cnt += crsr.Ranges.SQL3SqldaSqlRange (db_crsr, crsr.Ranges.SqlVars[crsr.Rngs], true);
            }

            if (!sql3Stmt.IsOpen)
            {
               errcode = SQLiteLow.LibPrepare(ConnectionTbl[sql3Dbd.DatabaseName], sql3Stmt, sql3Cursor);
            }

         }   

         int noOfRecordsUpdated = 0;
         if (errcode == SqliteConstants.SQL3_OK)
         {
            errcode = SQLiteLow.LibExecuteWithParams(sql3Cursor.InputSqlda, sql3Stmt, ConnectionTbl[sql3Dbd.DatabaseName], out noOfRecordsUpdated, DatabaseOperations.Where);

            if (Logger.Instance.LogLevel >= Logger.LogLevels.Support)
            {
               SQL3StmtBuildWithValues(sql3Stmt.Buf, db_crsr.Definition.DataSourceDefinition, sql3Cursor.InputSqlda, sql3Stmt, false);
               Logger.Instance.WriteSupportToLog(string.Format("\tSTMT: {0}", sql3Stmt.StmtWithValues), true);
            }
            
         }

         Logger.Instance.WriteDevToLog("RangeOpen(): <<<<<");

         return errcode;
      }


      /// <summary>
      ///  CountExtraFlds ()
      /// </summary>
      /// <param name="dbCrsr"></param>
      /// <returns>int</returns>
      public int CountExtraFlds (GatewayAdapterCursor dbCrsr)
      {
         GatewayCursor        crsr;
         SQL3Dbd              sql3Dbd;
         int                  idx = 0, 
                              cnt = 0,
                              posCnt = 0;
         DBKey                key = null;
         DBSegment            seg = null;
         DataSourceDefinition dbh = dbCrsr.Definition.DataSourceDefinition;


         GatewayCursorTbl.TryGetValue(dbCrsr, out crsr);
         DbdTbl.TryGetValue(dbh, out sql3Dbd);

         Logger.Instance.WriteDevToLog("CountExtraFlds(): >>>>>");
         
         crsr.XtraSortkeyCnt = 0;

         /* if there is a sort key */
         if (dbCrsr.Definition.Key != null)
         {
            Logger.Instance.WriteDevToLog("CountExtraFlds(): key_idx is not null");

            if (sql3Dbd != null && sql3Dbd.IsView)
            {
               key = dbCrsr.Definition.Key;

               /* run thru the key segs */

               for (idx = 0; idx < key.Segments.Count; idx++)
               {
                  seg = key.Segments[idx];
                  DBField fld = seg.Field;
                  if (!((fld.Storage == FldStorage.TimeString) &&
                        (fld.PartOfDateTime != 0)))
                  {
                     if (!Sql3IsDbCrsrField(dbCrsr, fld))
                        cnt++;
                  }
               }
            }

            crsr.XtraSortkeyCnt = cnt;
            Logger.Instance.WriteDevToLog(string.Format("CountExtraFlds(): meanwhile cnt = {0}", cnt));
         }
         if (sql3Dbd.IsView)
         {
            if (crsr.KeyArray == null)
               Sql3MakeDbpoSegArray(dbCrsr);

            key = crsr.PosKey;

            /* for all the segs in the dbpos key */
            for (idx = 0; idx < key.Segments.Count; idx++)
               /* if the segment is not in the magic view */
               if (crsr.KeyArray[idx] >= dbCrsr.Definition.FieldsDefinition.Count)
               {
                  cnt++;
                  posCnt++;
               }

            crsr.XtraPoskeyCnt = posCnt;
         }
         //For rowid
         else
            cnt++;

         Logger.Instance.WriteDevToLog(string.Format("CountExtraFlds(): <<<<< count = {0}", cnt));

         return cnt;
      }

      public bool LongFldsInCrsr (GatewayAdapterCursor db_crsr)
      {
         return false;
      }

      public bool IsLongFld (int fldIdx)
      {
         return false;
      }

      /// <summary>
      ///  GetPosSize ()
      /// </summary>
      /// <param name="sql3Dbd"></param>
      /// <returns>int</returns>
      public int GetPosSize (SQL3Dbd sql3Dbd)
      {
         int                    idx;
         DataSourceDefinition   dbh      = sql3Dbd.DataSourceDefinition;
         DBField                fld      = null;
         DBKey                  key      = null;
         DBSegment              seg      = null;
         short                  segLen;

         Logger.Instance.WriteDevToLog("GetPosSize(): >>>>> ");

         sql3Dbd.posLen = 0;

         if (sql3Dbd.IsView)
         {
            /* calculate the length of the PosKey key*/
            key = FindPoskey(sql3Dbd.DataSourceDefinition);

            for (idx = 0 ; idx < key.Segments.Count; idx++)
            {
               seg = key.Segments[idx];
               fld = seg.Field;

               if (!fld.DefaultStorage)
                  segLen = (short)seg.Field.Length;
               else
                   segLen = (short)fld.Length;

               sql3Dbd.posLen += sizeof(short); // to save data length of a seg which follows.

               if ((fld.Storage == FldStorage.DateString && Sql3DateType(dbh, fld) != DateType.DATE_TO_SQLCHAR) ||
                   (fld.Storage == FldStorage.TimeString && Sql3DateType(dbh, fld) == DateType.DATE_TO_DATE))
               {
                  if (fld.PartOfDateTime > 0 ||
                     (!string.IsNullOrEmpty(fld.DbType) && fld.DbType == "DATETIME"))
                     sql3Dbd.posLen += 19;
                  else 
                     sql3Dbd.posLen += 10;
               }
               // special case
               else if (fld.Storage == FldStorage.AlphaLString)
                   sql3Dbd.posLen += segLen + 1;
               else
                  sql3Dbd.posLen += segLen;
            }
         }
         else
         {
            sql3Dbd.posLen += sizeof(int); // to save data length of a seg which follows.
            sql3Dbd.posLen += SqliteConstants.SQL3_ROWID_LEN_EXTERNAL; // to save rowid.
         }

         Logger.Instance.WriteDevToLog(string.Format("GetPosSize(): <<<<< possize = {0}", sql3Dbd.posLen));

         return sql3Dbd.posLen;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="db_crsr"></param>
      /// <param name="charPrefix"></param>
      /// <param name="pos"></param>
      /// <returns></returns>
      public void BuildExtraFieldsStmt(GatewayAdapterCursor db_crsr, string charPrefix)
      {
         GatewayCursor  crsr;
         int            seg_idx       = 0;
         DBKey          key;
         DBSegment      seg;
         string         no_prefix   = string.Empty,
                        prefix      = null,
                        name        = null;
         
         SQL3Dbd pSQL3_dbd; ;
         int prefix_len = no_prefix.Length;
         bool first = true;

         GatewayCursorTbl.TryGetValue(db_crsr, out crsr);
         DbdTbl.TryGetValue(db_crsr.Definition.DataSourceDefinition, out pSQL3_dbd);

         Logger.Instance.WriteDevToLog("BuildExtraFieldsStmt(): >>>>> ");

         Statement = string.Empty;
         
         if (charPrefix != null)
         {
            prefix_len = SQL3PrefixBuf.Length;
            prefix = SQL3PrefixBuf;
            prefix = string.Format("{0}.", charPrefix);
         }
         else
         {
            prefix = no_prefix;
         }

         /* if there is a sort key */
         if (db_crsr.Definition.Key != null && pSQL3_dbd != null && pSQL3_dbd.IsView)
         {
            key = db_crsr.Definition.Key;
               
            /* run thru the key segs */
            for (seg_idx = 0; seg_idx < key.Segments.Count; seg_idx++)
            {
               seg = key.Segments[seg_idx];
               if (!Sql3IsDbCrsrField (db_crsr, seg.Field))
               {
                  if (!((seg.Field.Storage == FldStorage.TimeString) && 
                        (seg.Field.PartOfDateTime != 0)))
                  {
                     /* get the field name */
                     name = seg.Field.DbName;

                     Logger.Instance.WriteDevToLog(string.Format("BuildExtraFieldsStmt(): adding extra field {0}", name));
                     
                     if (first == true)
                     {
                        Statement += string.Format("{0}{1}", prefix, name);
                        first = false;
                     }
                     else
                     {
                        Statement += string.Format(",{0}{1}", prefix, name);
                     }
                  }
               }
            }
         }

         if (pSQL3_dbd.IsView)
         {
            if (crsr.PosKey != null)
            {
               if (crsr.KeyArray == null)
               {
                  Sql3MakeDbpoSegArray(db_crsr);
               }

               key = crsr.PosKey;
                     
               /* run thru the key segs */
               for (seg_idx = 0; seg_idx < key.Segments.Count; seg_idx++)
               {
                  seg = key.Segments[seg_idx];
                  /* if the segment is not in the magic view */
                  if (crsr.KeyArray[seg_idx] >= db_crsr.Definition.FieldsDefinition.Count)
                  {
                     if (!((seg.Field.Storage == FldStorage.TimeString) && 
                           (seg.Field.PartOfDateTime != 0)))
                     {
                        /* get the field name */
                        name = seg.Field.DbName;

                        Logger.Instance.WriteDevToLog(string.Format("BuildExtraFieldsStmt(): adding extra field {0}", name));

                        if (first == true)
                        {
                           Statement += string.Format("{0}{1}", prefix, name);
                           first = false;
                        }
                        else
                        {
                           Statement += string.Format(",{0}{1}", prefix, name);
                        }
                     }
                  }
               }
            }
         }
         else if (db_crsr.CursorType == CursorType.PartOfJoin||
                  (db_crsr.CursorType == CursorType.MainPartOfJoin) ||
                  (db_crsr.CursorType == CursorType.Join) || 
                  (db_crsr.CursorType == CursorType.PartOfOuter))
         {
            if (first == true)
            {
               Statement += string.Format("{0}{1}", prefix, SqliteConstants.SQL3_ROWID_ST_A);
               first = false;
            }
            else
            {
               Statement += string.Format(",{0}{1}", prefix, SqliteConstants.SQL3_ROWID_ST_A);
            }
         }

         Logger.Instance.WriteDevToLog("StmtAddExtraFields(): <<<<<");
      }

      /// <summary>
      /// BuildInsertStmt()
      /// </summary>
      /// <param name="db_crsr"></param>
      /// <returns></returns>
      public int BuildInsertStmt(GatewayAdapterCursor dbCrsr)
      {
         DataSourceDefinition dbh = null;
         SQL3Dbd              sql3Dbd;
         DBField              fld = null;
         int                  idx = 0;
         string               name = string.Empty;
         bool                 first = true;
         List<DBField>        fldInfo = dbCrsr.Definition.FieldsDefinition;
         short                cnt = 0;

         DbdTbl.TryGetValue(dbCrsr.Definition.DataSourceDefinition, out sql3Dbd);

         Logger.Instance.WriteDevToLog(string.Format("BuildInsertStmt(): >>>>> dbCrsr.Definition.FieldsDefinition.Count = {0}", dbCrsr.Definition.FieldsDefinition.Count));

         Statement = string.Empty;

         Statement += string.Format("INSERT INTO {0} (", sql3Dbd.TableName);

         for (idx = 0; idx < (int)dbCrsr.Definition.FieldsDefinition.Count; idx++)
         {
            
            /* get the fld for the storage and the len */
            fld = dbCrsr.Definition.FieldsDefinition[idx];

            if ((fld.Storage == FldStorage.TimeString) &&
                (fld.PartOfDateTime != 0))
               continue;

            /* not to put IDENTITY column name into INSERT statement */
            if (Sql3CheckDbtype(fld, SqliteConstants.IDENTITY_STR))
               continue;

            /*Don't include columns in INSERT stmt, if fld_update is FALSE for that fld.*/
            if (!dbCrsr.Definition.IsFieldUpdated[idx])
               continue;


            // Skip TIMESTAMP fld in an Insert stmt as we cannot insert an explicit value into it.
            if (Sql3CheckDbtype(fld, "TIMESTAMP"))
               continue;

            name = fld.DbName;

            if (first)
            {
               first = false;
            }
            else
            {
               Statement += ", ";
            }

            Statement += string.Format("{0} ", name);

            cnt++;
         }

         Statement += ") VALUES (";

         first = true;

         for (idx = 0; idx < (int)dbCrsr.Definition.FieldsDefinition.Count; idx++)
         {
            /* get offset into the field table */
            fld = dbCrsr.Definition.FieldsDefinition[idx];

            if ((fld.Storage == FldStorage.TimeString) &&
                (fld.PartOfDateTime != 0))
               continue;

            /* not to put IDENTITY column name into INSERT statement */
            if (Sql3CheckDbtype(fld, SqliteConstants.IDENTITY_STR))
               continue;

            /* Don't put values of flds in INSERT stmt, if fld_update is FALSE for that fld.*/
            if (!dbCrsr.Definition.IsFieldUpdated[idx])
               continue;

            // Skip TIMESTAMP fld in an Insert stmt as we cannot insert an explicit value into it.
            if (Sql3CheckDbtype(fld, "TIMESTAMP"))
               continue;

            if (!first)
            {
               Statement += ",";
            }

            if ((fld.Storage == FldStorage.DateString && Sql3DateType(dbh, fld) != DateType.DATE_TO_SQLCHAR) ||
                (fld.Storage == FldStorage.TimeString && Sql3DateType(dbh, fld) == DateType.DATE_TO_DATE))
            {
               if (fld.PartOfDateTime != 0)
               {
                  Statement += "DATETIME (?)";
               }
               else
               {
                  if (fld.Storage == FldStorage.TimeString)
                  {
                     Statement += "TIME (?)";
                  }
                  else
                  {
                     Statement += "DATE (?)";
                  }
               }
            }
            else
            {
               Statement += "?";
            }

            first = false;
         }

         Statement += ")";

         Logger.Instance.WriteDevToLog(string.Format("BuildInsertStmt(): <<<<< Stmt = {0}", Statement));

         return cnt;
      }

      public int BuildWhereViewStmt(GatewayAdapterCursor dbCrsr, DbPos dbPos, int stmtOrigPos)
      {
         short                idx = 0;
         int                  offset = 0;
         short                plen = 0;
         DataSourceDefinition dbh = dbCrsr.Definition.DataSourceDefinition;
         DBField              fld;
         GatewayCursor        crsr;
         DBKey                key;
         DBSegment            seg;
         string               name = " ";
         bool                 first = true;
         string               noPrefix = " ",
                              prefix = string.Empty;
         int                  segs;

         GatewayCursorTbl.TryGetValue(dbCrsr, out crsr);

         Logger.Instance.WriteDevToLog("BuildWhereViewStmt(): >>>>> ");
         
         if (dbCrsr.CursorType == CursorType.Join)
         {
            prefix = SQL3PrefixBuf;
            prefix += string.Format("{0}.", crsr.DbhPrefix[0]);
         }
         else
            prefix = noPrefix;

         crsr.DbPosBuf = dbPos.Get();

         segs = 0;

         key = crsr.PosKey;

         Logger.Instance.WriteDevToLog(string.Format("BuildWhereViewStmt(): Regular file, segs = {0}", key.Segments.Count));

         for (idx = 0; idx < key.Segments.Count; idx++)
         {

            byte [] buf = GetBytes(crsr.DbPosBuf, offset, sizeof(short));
            plen = BitConverter.ToInt16(buf, 0);
            offset += sizeof (short);
            

            seg = key.Segments[idx];

            fld = seg.Field;

            if (fld.Storage == FldStorage.TimeString && fld.PartOfDateTime != 0)
            {
               offset += plen;
               if (plen > 0)
                  segs++;
               continue;
            }

            if (first)
               first = false;
            else
            {
               Statement += string.Format("{0}", " AND ");
            }

            /* get the field name */
            name = fld.DbName;

            Logger.Instance.WriteDevToLog(string.Format("BuildWhereViewStmt(): field {0}", name));

            // if field is null
            if (plen == 0)
            {
               Statement += string.Format("{0}{1} IS NULL", prefix, name);

               Logger.Instance.WriteDevToLog(string.Format("BuildWhereViewStmt(): doing a NULL segment {0}", name));
            }
            else
            {
               Logger.Instance.WriteDevToLog(string.Format("BuildWhereViewStmt(): doing a NON NULL segment {0}", name));

               if ((fld.Storage == FldStorage.DateString && Sql3DateType(dbh, seg.Field) != DateType.DATE_TO_SQLCHAR) ||
                   (fld.Storage == FldStorage.TimeString && Sql3DateType(dbh, seg.Field) == DateType.DATE_TO_DATE))
               {
                  if (fld.PartOfDateTime != 0)
                  {
                     Statement += string.Format("DATETIME({0}{1}) = DATETIME(?)", prefix, name);
                  }
                  else
                  {
                     if (fld.Storage == FldStorage.TimeString)
                     {
                        Statement += string.Format("TIME({0}{1}) = TIME(?)", prefix, name);
                     }
                     else
                     {
                        Statement += string.Format("DATE({0}{1}) = DATE(?)", prefix, name);
                     }
                  }
               }
               else
               {
                  Statement += string.Format("{0}{1} = ?", prefix, name);
               }

               offset += plen;
               segs++;
            }
         }

         Logger.Instance.WriteDevToLog("BuildWhereViewStmt(): <<<<< ");
         
         return segs;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="db_crsr"></param>
      /// <param name="stmt"></param>
      /// <param name="stmtOrigPos"></param>
      /// <returns></returns>
      public int BuildWhereAllViewStmt (GatewayAdapterCursor dbCrsr, int stmtOrigPos)
      {
         CursorDefinition     dbd           = dbCrsr.Definition;
         DataSourceDefinition dbh           = dbCrsr.Definition.DataSourceDefinition;
         DBField              fld;
         DBKey                key;
         GatewayCursor        crsr;
         SQL3Dbd              sql3Dbd;
         int                  idx ;
         int                  segIdx;
         string               name           = string.Empty;
         string               noPrefix       = string.Empty,
                              prefix;
         bool                 []fieldInPos;
         bool                 dateFl;
         int                  segs;
	      int             		pos 		      = 0;


         GatewayCursorTbl.TryGetValue(dbCrsr, out crsr);
         DbdTbl.TryGetValue(dbh, out sql3Dbd);

         Logger.Instance.WriteDevToLog("BuildWhereAllViewStmt(): >>>>> ");

         if (dbCrsr.CursorType == CursorType.Join)
         {
            prefix = SQL3PrefixBuf;
            prefix += string.Format("{0}.", crsr.DbhPrefix[0]);
         }
         else
         {
            prefix = noPrefix;
         }

         /* allocate an indications array for adding only one time the fields which are in the
         db_pos and also in the data view */
         fieldInPos = new bool[dbCrsr.Definition.FieldsDefinition.Count];
         segs = 0;

         if (sql3Dbd.IsView)
         {
            if (crsr.KeyArray == null)
            {
               Sql3MakeDbpoSegArray (dbCrsr);
            }

            key = crsr.PosKey;
            /* for all the segs in the dbpos key */
            for (segIdx = 0; segIdx < key.Segments.Count; segIdx++)
            {
               /* if the segment is in the magic view */
               if (crsr.KeyArray[segIdx] < dbCrsr.Definition.FieldsDefinition.Count)
                  fieldInPos[crsr.KeyArray[segIdx]] = true;
            }
      
            segs = BuildWhereViewStmt (dbCrsr, dbCrsr.Definition.CurrentPosition, stmtOrigPos);
            pos = Statement.Length;
         }

         //if (db_crsr->del_upd_mode != UPD_POS_CHECK)
         {
            /* add the data view fields that are not in the db_pos */
            for (idx = 0; idx < dbCrsr.Definition.FieldsDefinition.Count; idx++)
            {
               fld = dbCrsr.Definition.FieldsDefinition[idx];
               /* The field should not be included in where clause if ,
                  1. Delete/Update mode is Position & Updated Fields but , fld_update is FALSE 
                  2. fld_update is TRUE but Update style of Field is Differential*/
               if (/*(db_crsr->del_upd_mode == UPD_CHANGED_CHECK && !db_crsr.Definition.IsFieldUpdated[idx]) ||*/
                   (dbCrsr.Definition.IsFieldUpdated[idx] && fld.DiffUpdate == 'Y'))
                  continue;

               /* if the field isn't in the db_pos and it isn't a long field */
               if (!fieldInPos[idx])//&& ! sql3_is_long_fld (dbd, fld_idx))
               {
                  if ((fld.Storage == FldStorage.TimeString) && (fld.PartOfDateTime != 0))
                  {
                     if (!(fld.AllowNull && dbCrsr.OldRecord.IsNull(idx)))
                        segs++;
                     continue;
                  }

                  if ((fld.Storage == FldStorage.DateString && Sql3DateType(dbh, fld) != DateType.DATE_TO_SQLCHAR) ||
                      (fld.Storage == FldStorage.TimeString && Sql3DateType(dbh, fld) == DateType.DATE_TO_DATE))
                     dateFl = true;
                  else
                     dateFl = false;

                  name = fld.DbName;

                  Statement += string.Format("{0}", " AND ");

                  if (fld.AllowNull && dbCrsr.OldRecord.IsNull(idx))
                  {
                     Statement += string.Format("{0}{1} IS NULL", prefix, name);
                  }
                  else
                  {
                     if (dateFl)
                     {
                        if (fld.PartOfDateTime != 0)
                        {
                           Statement += string.Format("DATETIME({0}{1}) = DATETIME(?)", prefix, name);
                        }
                        else
                        {
                           if (fld.Storage == FldStorage.TimeString)
                           {
                              Statement += string.Format("(TIME({0}{1}) = TIME(?))", prefix, name);
                           }
                           else
                           {
                              Statement += string.Format("(DATE({0}{1}) == DATE(?))", prefix, name);
                           }
                        }
                     }
                     else
                     {
                        Statement += string.Format("{0}{1} = ?", prefix, name);
                     }
                     segs++;
                  }
               }
            }

         }

         Logger.Instance.WriteDevToLog(string.Format("SQL3StmtBuildWhereAllView(): <<<<< pos = {0}", pos));

         return segs;
      }

      public int BuildRangesStmt(GatewayAdapterCursor dbCrsr, bool onlyLinks, int crsrHdl)
      {
         GatewayCursor  crsr;
         RangeData      range;
         RangeData      slct_time = new RangeData();
         int            idx,
                        cnt = 0,
                        len = 0;
         DBField        fld;
         DataSourceDefinition dbh = dbCrsr.Definition.DataSourceDefinition;
         string         getName,
                        name, // add 5 cause magic allow it, 25 - for TIME_NAME
                        noPrefix = string.Empty,
                        prefix = string.Empty;
         int            fldNum;
         string         maxOrg = string.Empty;
         bool           dateFl;
         char           datetimeRange = (char)SqliteConstants.DATE_RNG;
         int            dtIdx;
         char           rangeFl;
         char           timeRangeFl = (char)SqliteConstants.NULL_CHAR;
         RangeData      slctDate;
         bool           isTimeRange = false;
         bool           slctModified = false;
         string         sql3Literal = "?";

         GatewayCursorTbl.TryGetValue(dbCrsr, out crsr);

         Logger.Instance.WriteDevToLog(string.Format("BuildRangesStmt(): >>>>> number = {0}", dbCrsr.Ranges.Count));

         Statement = string.Empty;
         if (dbCrsr.CursorType == CursorType.Join)
         {
            prefix = SQL3PrefixBuf;
         }
         else
         {
            prefix = noPrefix;
         }

         /* run through all the ranges */
         for (idx = 0; idx < dbCrsr.Ranges.Count; ++idx)
         {
            maxOrg = string.Empty;
            range = dbCrsr.Ranges[idx];
            if (Logger.Instance.LogLevel >= Logger.LogLevels.Development)
               SQLLogging.LogRngSlct(range);

            datetimeRange = (char)SqliteConstants.DATE_RNG;

            /* get a FLD index and pointer */

            fld = dbCrsr.Definition.FieldsDefinition[range.FieldIndex];

            if (fld.PartOfDateTime != 0)
            {
               dateFl = true;
               if (fld.Storage == FldStorage.DateString)
               {
                  datetimeRange = (char)SqliteConstants.DATETIME_RNG;
               }
               else
               {
                  if (range.DatetimeRangeIdx == 0)
                  {
                     datetimeRange = (char)SqliteConstants.TIME_RNG;
                  }
                  else if (fld.Storage == FldStorage.TimeString)
                  {
                     isTimeRange = false;
                     for (dtIdx = 0; dtIdx < dbCrsr.Ranges.Count; ++dtIdx)
                     {
                        slctDate = dbCrsr.Ranges[dtIdx];
                        if (dbCrsr.Definition.FieldsDefinition[slctDate.FieldIndex].Isn == fld.PartOfDateTime)
                        {
                           if ((slctDate.Min.Type == RangeType.RangeNoVal && slct_time.Min.Type == RangeType.RangeParam) ||
                               (slctDate.Max.Type == RangeType.RangeNoVal && slct_time.Max.Type == RangeType.RangeParam))
                           {
                              datetimeRange = (char)SqliteConstants.TIME_RNG;
                              isTimeRange = true;
                              break;
                           }
                        }
                     }
                     if (isTimeRange == false)
                        continue;
                  }
                  else
                     continue;
               }
            }

            fldNum = Sql3GetDbCrsrIndex(dbCrsr, fld);
            // crsr_hdl != -1 means we build join ranges for the specified i/p file(crsr_hdl).
            // so, if the file to which the fld belongs is not same as i/p file, then skip.
            if (crsrHdl != -1 && crsr.SourceDbh != null)
               if (crsr.SourceDbh[fldNum] != crsrHdl)
                  continue;

            if (dbCrsr.CursorType == CursorType.Join)
            {
               // when building ranges for links and if the field belongs to main file, skip it
               if (onlyLinks && crsr.SourceDbh[fldNum] == 0)
               {
                  if (slctModified)
                  {
                     range.Min.Type = RangeType.RangeMinMax;
                     range.Max.Type = RangeType.RangeMinMax;
                  }
                  continue;
               }

               prefix += string.Format("{0}.", crsr.DbhPrefix[crsr.SourceDbh[fldNum]]);

            }

            /* get the field name */
            getName = fld.DbName;

            /* keep the name cause any other call to this->fm_ctxt->db_str->db_info will erase it */
            if ((fld.Storage == FldStorage.DateString && Sql3DateType(dbh, fld) != DateType.DATE_TO_SQLCHAR) ||
                (fld.Storage == FldStorage.TimeString && Sql3DateType(dbh, fld) == DateType.DATE_TO_DATE))
            {
               dateFl = true;
               if (fld.PartOfDateTime != 0)
               {
                  sql3Literal = string.Format("{0} ", SqliteConstants.DATETIME_LITERAL);
               }
               else
               {
                  datetimeRange = (char)SqliteConstants.DATE_RNG;
                  if (fld.Storage == FldStorage.TimeString)
                  {
                     sql3Literal = string.Format("{0} ", SqliteConstants.TIME_LITERAL);
                  }
                  else
                  {
                     sql3Literal = string.Format("{0} ", SqliteConstants.DATE_LITERAL);
                  }
               }
            }
            else
            {
               dateFl = false;
               sql3Literal = string.Format("{0} ", "?");
            }

            // Defect 117059 : For numeric string which is having bigger picture, we save the data in the byte[] format. But there is problem with sqlite c# apis 
            // while applying the ranges of this type of field. So while building the where clause, CAST the data as string.
            if (fld.Storage == FldStorage.NumericString && fld.DataSourceDefinition != DatabaseDefinitionType.Normal && fld.DbType.Contains("BINARY"))
            {
               name = string.Format("CAST({0} as TEXT)", getName);
            }
            else
            {
               name = getName;
            }

            /* if not first time round add AND */
            if (!string.IsNullOrEmpty(Statement))
               Statement = string.Format("{0} AND ", Statement);

            len = fld.StorageFldSize();

            if (!dbh.CheckMask(DbhMask.BinaryTableMask) ||
                (dbCrsr.GetFieldIndex(fld) == 0))
            {
               if (fld.Attr == (char)StorageAttributeType.Alpha)
               {
                  if (range.Max.Value.Value != null)
                  {
                     maxOrg = (string)range.Max.Value.Value;
                     SQL3ChangeToMaxSortChar((string)range.Max.Value.Value, len);
                  }
               }
            }

            if (dateFl)
            {
               switch (datetimeRange)
               {
                  case (char)SqliteConstants.DATE_RNG:

                     if (range.Max.Value.Value != null && !range.Max.Value.IsNull)
                     {
                        if ((range.Max.Value.Value.ToString() == "99991231") ||
                            (fld.Storage == FldStorage.TimeString && range.Max.Value.Value.ToString() == "235959"))
                        {
                           range.Max.Discard = true;
                        }
                     }
                     break;

                  case (char)SqliteConstants.DATETIME_RNG:
                     // 'TO' Range
                     if (range.Max.Value.Value != null && !range.Max.Discard && !range.Max.Value.IsNull)
                     {
                        if (range.Max.Value.Value.ToString() == "99991231")
                        {
                           range.Max.Discard = true;
                        }
                     }
                     break;
               }
            }

            rangeFl = Sql3GetFieldRangeType(dbCrsr, range, fld, dateFl, datetimeRange);

            if (slctModified)
            {
               range.Min.Type = RangeType.RangeMinMax;
               range.Max.Type = RangeType.RangeMinMax;
            }

            if (datetimeRange == SqliteConstants.DATETIME_RNG)
            {
               timeRangeFl = rangeFl;
               switch (rangeFl)
               {
                  case (char)SqliteConstants.MIN_RNG:
                     switch (timeRangeFl)
                     {
                        case (char)SqliteConstants.MIN_RNG:
                        case (char)SqliteConstants.NO_RNG:
                           break;

                        case (char)SqliteConstants.MAX_RNG:
                        case (char)SqliteConstants.MIN_AND_MAX_RNG:
                        case (char)SqliteConstants.MIN_EQ_MAX_RNG:
                           rangeFl = (char)SqliteConstants.MIN_AND_MAX_RNG;
                           break;

                        case (char)SqliteConstants.NULL_RNG:
                        case (char)SqliteConstants.MIN_AND_NULL_RNG:
                        case (char)SqliteConstants.NULL_AND_MAX_RNG:
                           rangeFl = timeRangeFl;
                           break;
                     }
                     break;

                  case (char)SqliteConstants.MAX_RNG:
                     switch (timeRangeFl)
                     {
                        case (char)SqliteConstants.MIN_RNG:
                        case (char)SqliteConstants.MIN_AND_MAX_RNG:
                        case (char)SqliteConstants.MIN_EQ_MAX_RNG:
                           rangeFl = (char)SqliteConstants.MIN_AND_MAX_RNG;
                           break;

                        case (char)SqliteConstants.MAX_RNG:
                        case (char)SqliteConstants.NO_RNG:
                           break;

                        case (char)SqliteConstants.NULL_RNG:
                        case (char)SqliteConstants.MIN_AND_NULL_RNG:
                        case (char)SqliteConstants.NULL_AND_MAX_RNG:
                           rangeFl = timeRangeFl;
                           break;
                     }
                     break;

                  case (char)SqliteConstants.MIN_AND_MAX_RNG:
                     switch (timeRangeFl)
                     {
                        case (char)SqliteConstants.MIN_RNG:
                        case (char)SqliteConstants.MAX_RNG:
                        case (char)SqliteConstants.MIN_AND_MAX_RNG:
                        case (char)SqliteConstants.MIN_EQ_MAX_RNG:
                        case (char)SqliteConstants.NO_RNG:
                           break;

                        case (char)SqliteConstants.NULL_RNG:
                        case (char)SqliteConstants.MIN_AND_NULL_RNG:
                        case (char)SqliteConstants.NULL_AND_MAX_RNG:
                           rangeFl = timeRangeFl;
                           break;
                     }
                     break;

                  case (char)SqliteConstants.MIN_EQ_MAX_RNG:
                     switch (timeRangeFl)
                     {
                        case (char)SqliteConstants.MIN_RNG:
                        case (char)SqliteConstants.MAX_RNG:
                        case (char)SqliteConstants.MIN_AND_MAX_RNG:
                        case (char)SqliteConstants.NO_RNG:
                           rangeFl = (char)SqliteConstants.MIN_AND_MAX_RNG;
                           break;

                        case (char)SqliteConstants.MIN_EQ_MAX_RNG:
                        case (char)SqliteConstants.NULL_RNG:
                        case (char)SqliteConstants.MIN_AND_NULL_RNG:
                        case (char)SqliteConstants.NULL_AND_MAX_RNG:
                           rangeFl = timeRangeFl;
                           break;
                     }
                     break;

                  case (char)SqliteConstants.NULL_RNG:
                     break;

                  case (char)SqliteConstants.MIN_AND_NULL_RNG:
                     switch (timeRangeFl)
                     {
                        case (char)SqliteConstants.MIN_RNG:
                        case (char)SqliteConstants.MAX_RNG:
                        case (char)SqliteConstants.MIN_AND_MAX_RNG:
                        case (char)SqliteConstants.MIN_EQ_MAX_RNG:
                        case (char)SqliteConstants.MIN_AND_NULL_RNG:
                        case (char)SqliteConstants.NO_RNG:
                           break;

                        case (char)SqliteConstants.NULL_RNG:
                        case (char)SqliteConstants.NULL_AND_MAX_RNG:
                           rangeFl = (char)SqliteConstants.NULL_RNG;
                           break;
                     }
                     break;

                  case (char)SqliteConstants.NULL_AND_MAX_RNG:
                     switch (timeRangeFl)
                     {
                        case (char)SqliteConstants.MIN_RNG:
                        case (char)SqliteConstants.MAX_RNG:
                        case (char)SqliteConstants.MIN_AND_MAX_RNG:
                        case (char)SqliteConstants.MIN_EQ_MAX_RNG:
                        case (char)SqliteConstants.NULL_AND_MAX_RNG:
                        case (char)SqliteConstants.NO_RNG:
                           break;

                        case (char)SqliteConstants.NULL_RNG:
                        case (char)SqliteConstants.MIN_AND_NULL_RNG:
                           rangeFl = (char)SqliteConstants.NULL_RNG;
                           break;
                     }
                     break;

                  case (char)SqliteConstants.NO_RNG:
                     rangeFl = timeRangeFl;
                     break;
               }
            }

            switch (rangeFl)
            {
               case (char)SqliteConstants.MIN_RNG:
                  cnt++;
                  if (datetimeRange == SqliteConstants.TIME_RNG)
                  {
                     Statement += string.Format(SqliteConstants.TIME_NAME, prefix, name);
                     Statement += string.Format("{0} {1}", ">=", sql3Literal);
                  }
                  else if (datetimeRange == SqliteConstants.DATE_RNG)
                  {
                     Statement += string.Format("{0}{1} >= {2}", prefix, name, sql3Literal);
                  }
                  else
                  {
                     switch (timeRangeFl)
                     {
                        case (char)SqliteConstants.MIN_RNG:
                        case (char)SqliteConstants.NO_RNG:
                           Statement += string.Format("{0}{1} >= {2} ", prefix, name, sql3Literal);
                           break;

                        case (char)SqliteConstants.MAX_RNG:
                        case (char)SqliteConstants.MIN_AND_MAX_RNG:
                        case (char)SqliteConstants.MIN_EQ_MAX_RNG:
                           Statement += string.Format("({0}{1} >= {2} AND ", prefix, name, sql3Literal);
                           Statement += string.Format(SqliteConstants.TIME_NAME, prefix, name, sql3Literal);
                           cnt++;
                           break;

                        case (char)SqliteConstants.NULL_RNG:
                        case (char)SqliteConstants.MIN_AND_NULL_RNG:
                        case (char)SqliteConstants.NULL_AND_MAX_RNG:
                           Statement += string.Format("({0}{1} >= {2} AND ", prefix, name, sql3Literal);
                           Statement += string.Format(SqliteConstants.TIME_NAME, prefix, name);
                           Statement += string.Format("{0}", " IS NULL) ");
                           break;

                        default:
                           break;
                     }
                  }
                  break;

               case (char)SqliteConstants.MAX_RNG:
                  cnt++;
                  if (datetimeRange == SqliteConstants.TIME_RNG)
                  {
                     Statement += string.Format(SqliteConstants.TIME_NAME, prefix, name);
                     Statement += string.Format("{0} {1}", "<=", sql3Literal);
                  }
                  else if (datetimeRange == SqliteConstants.DATE_RNG)
                  {
                     Statement += string.Format("{0}{1} <= {2} ", prefix, name, sql3Literal);
                  }
                  else
                  {
                     switch (timeRangeFl)
                     {
                        case (char)SqliteConstants.MIN_RNG:
                           Statement += "(";
                           Statement += string.Format(SqliteConstants.TIME_NAME, prefix, name);
                           Statement += string.Format(" >= {0} AND {1}{2} < {3}) ", sql3Literal, prefix, name, sql3Literal);
                           cnt++;
                           break;

                        case (char)SqliteConstants.MAX_RNG:
                        case (char)SqliteConstants.NO_RNG:
                           Statement += string.Format("{0}{1} <= {2} ", prefix, name, sql3Literal);
                           break;

                        case (char)SqliteConstants.MIN_AND_MAX_RNG:
                        case (char)SqliteConstants.MIN_EQ_MAX_RNG:
                           Statement += string.Format("{0}", "(");
                           Statement += string.Format(SqliteConstants.TIME_NAME, prefix, name);
                           Statement += string.Format(" >= {0} AND {1}{2} <= {3}) ", sql3Literal, prefix, name, sql3Literal);
                           cnt++;
                           break;

                        case (char)SqliteConstants.NULL_RNG:
                        case (char)SqliteConstants.MIN_AND_NULL_RNG:
                        case (char)SqliteConstants.NULL_AND_MAX_RNG:
                           Statement += string.Format("{0}", "(");
                           Statement += string.Format(prefix, name);
                           Statement += string.Format("IS NULL AND {0}{1} < {2}) ", prefix, name, sql3Literal);
                           break;

                        default:
                           break;
                     }
                  }
                  break;

               case (char)SqliteConstants.MIN_AND_MAX_RNG:
                  cnt++; cnt++;
                  if (datetimeRange != SqliteConstants.DATETIME_RNG)
                  {
                     if (dateFl)
                     {
                        if (datetimeRange == SqliteConstants.DATE_RNG)
                        {
                           Statement += string.Format("({0}{1} >= {2} ", prefix, name, sql3Literal);
                           Statement += string.Format("AND {0}{1} <= {2}) ", prefix, name, sql3Literal);
                        }
                        else
                        {
                           Statement += string.Format("{0}{1} BETWEEN {2} AND {3} ", prefix, name, sql3Literal, sql3Literal);
                        }
                     }
                     else if (datetimeRange == SqliteConstants.TIME_RNG)
                     {
                        Statement += string.Format(SqliteConstants.TIME_NAME, prefix, name);
                        Statement += string.Format("BETWEEN {0} AND {1} ", sql3Literal, sql3Literal);
                     }
                     else
                     {
                        Statement += string.Format("{0}{1} BETWEEN {2} AND {3} ", prefix, name, sql3Literal, sql3Literal);
                     }

                  }
                  else
                  {
                     switch (timeRangeFl)
                     {
                        case (char)SqliteConstants.MIN_RNG:
                           Statement += string.Format("{0}{1} >= {2} ", prefix, name, sql3Literal);
                           Statement += string.Format("AND {0}{1} < {2}) ", prefix, name, sql3Literal);
                           break;

                        case (char)SqliteConstants.MAX_RNG:
                        case (char)SqliteConstants.MIN_AND_MAX_RNG:
                        case (char)SqliteConstants.MIN_EQ_MAX_RNG:
                        case (char)SqliteConstants.NO_RNG:
                           Statement += string.Format("({0}{1} >= {2} ", prefix, name, sql3Literal);
                           Statement += string.Format("AND {0}{1} <= {2}) ", prefix, name, sql3Literal);
                           break;

                        case (char)SqliteConstants.NULL_RNG:
                        case (char)SqliteConstants.MIN_AND_NULL_RNG:
                        case (char)SqliteConstants.NULL_AND_MAX_RNG:

                           Statement += string.Format("({0}{1} >= {2} AND ", prefix, name, sql3Literal);
                           Statement += string.Format(SqliteConstants.TIME_NAME, prefix, name);
                           Statement += string.Format("{0}", " IS NULL) ");
                           cnt--;
                           break;

                        default:
                           break;
                     }
                  }
                  break;

               case (char)SqliteConstants.MIN_EQ_MAX_RNG:
                  cnt++;
                  if (datetimeRange == SqliteConstants.TIME_RNG)
                  {
                     Statement += string.Format(SqliteConstants.TIME_NAME, prefix, name);
                     Statement += string.Format("= {0}", sql3Literal);
                  }
                  else if (datetimeRange == (char)SqliteConstants.DATE_RNG)
                  {
                     Statement += string.Format("{0}{1} = {2} ", prefix, name, sql3Literal);
                  }
                  else
                  {
                     switch (timeRangeFl)
                     {
                        case (char)SqliteConstants.MIN_RNG:
                           Statement += string.Format("{0}{1} >= {2} ", prefix, name, sql3Literal);
                           Statement += string.Format("AND {0}{1} < {2}) ", prefix, name, sql3Literal);
                           cnt++;
                           break;

                        case (char)SqliteConstants.MAX_RNG:
                        case (char)SqliteConstants.MIN_AND_MAX_RNG:
                           Statement += string.Format("({0}{1} >= {2}? ", prefix, name, sql3Literal);
                           Statement += string.Format("AND {0}{1} < {2}) ", prefix, name, sql3Literal);
                           cnt++;
                           break;

                        case (char)SqliteConstants.MIN_EQ_MAX_RNG:
                        case (char)SqliteConstants.NO_RNG:
                           Statement += string.Format("{0}{1} = {2} ", prefix, name, sql3Literal);
                           break;

                        case (char)SqliteConstants.NULL_RNG:
                        case (char)SqliteConstants.MIN_AND_NULL_RNG:
                        case (char)SqliteConstants.NULL_AND_MAX_RNG:
                           Statement += string.Format("({0}{1} >= {2} AND ", prefix, name, sql3Literal);
                           Statement += string.Format(SqliteConstants.TIME_NAME, prefix, name);
                           Statement += string.Format("{0}", " IS NULL) ");
                           break;
                        default:
                           break;
                     }
                  }

                  break;

               case (char)SqliteConstants.NULL_RNG:
                  Statement += string.Format("{0}{1} IS NULL ", prefix, name);
                  break;

               case (char)SqliteConstants.MIN_AND_NULL_RNG:
                  cnt++;
                  if (datetimeRange == SqliteConstants.TIME_RNG)
                  {
                     Statement += string.Format(SqliteConstants.TIME_NAME, prefix, name);
                     Statement += string.Format(" >= {0} ", sql3Literal);
                  }
                  else
                  {
                     Statement += string.Format("{0}{1} >= {2} ", prefix, name, sql3Literal);
                  }

                  Statement += string.Format("AND {0}{1} IS NULL ", prefix, name);
                  break;

               case (char)SqliteConstants.NULL_AND_MAX_RNG:
                  cnt++;
                  if (dateFl && datetimeRange == SqliteConstants.DATE_RNG)
                  {
                     Statement += string.Format("{0}{1} IS NULL AND {2} < {3} ", prefix, name, name, sql3Literal);
                  }
                  else if (datetimeRange == SqliteConstants.TIME_RNG)
                  {
                     Statement += string.Format("{0}{1} IS NULL AND ", prefix, name);
                     Statement += string.Format(SqliteConstants.TIME_NAME, prefix, name);
                     Statement += string.Format("<= {0}", sql3Literal);
                  }
                  else
                  {
                     Statement += string.Format("{0}{1} IS NULL AND {2} <= {3} ", prefix, name, name, sql3Literal);
                  }
                  break;

               case (char)SqliteConstants.NO_RNG:
                  if (datetimeRange == SqliteConstants.DATETIME_RNG)
                  {
                     cnt++;
                     switch (timeRangeFl)
                     {
                        case (char)SqliteConstants.MIN_RNG:
                           Statement += string.Format(SqliteConstants.TIME_NAME, prefix, name);
                           Statement += string.Format(" >= {0} ", sql3Literal);
                           break;

                        case (char)SqliteConstants.MAX_RNG:
                           Statement += string.Format(SqliteConstants.TIME_NAME, prefix, name);
                           Statement += string.Format(" <= {0} ", sql3Literal);
                           break;

                        case (char)SqliteConstants.MIN_AND_MAX_RNG:
                           Statement += string.Format("{0}", "(");
                           Statement += string.Format(SqliteConstants.TIME_NAME, prefix, name);
                           Statement += string.Format(" >= {0} AND ", sql3Literal);
                           Statement += string.Format(SqliteConstants.TIME_NAME, prefix, name);
                           Statement += string.Format(" <= {0}) ", sql3Literal);
                           cnt++;
                           break;

                        case (char)SqliteConstants.MIN_EQ_MAX_RNG:
                           Statement += string.Format(SqliteConstants.TIME_NAME, prefix, name);
                           Statement += string.Format(" = {0} ", sql3Literal);
                           break;

                        case (char)SqliteConstants.NULL_RNG:
                           Statement += string.Format("{0}{1} IS NULL ", prefix, name);
                           cnt--;
                           break;

                        case (char)SqliteConstants.MIN_AND_NULL_RNG:
                           Statement += string.Format(SqliteConstants.TIME_NAME, prefix, name);
                           Statement += string.Format(" >= {0} AND ", sql3Literal);
                           Statement += string.Format(SqliteConstants.TIME_NAME, prefix, name);
                           Statement += string.Format("{0}", " IS NULL ");
                           break;

                        case (char)SqliteConstants.NULL_AND_MAX_RNG:
                           Statement += string.Format("{0}", "(");
                           Statement += string.Format(SqliteConstants.TIME_NAME, prefix, name);
                           Statement += string.Format("{0}", " IS NULL AND ");
                           Statement += string.Format(SqliteConstants.TIME_NAME, prefix, name);
                           Statement += string.Format(" <= {0}) ", sql3Literal);
                           break;

                        case (char)SqliteConstants.NO_RNG:
                        default:
                           break;
                     }
                  }
                  else
                  {
                     if (Statement.EndsWith ("AND "))
                     {
                        Statement = Statement.Remove(Statement.Length - 4);
                     }
                  }
                  break;

               default:
                  break;
            }

            if (!string.IsNullOrEmpty(maxOrg))
            {
               range.Max.Value.Value = maxOrg;
            }
         }

         Logger.Instance.WriteDevToLog(string.Format("BuildRangesStmt(): <<<<< {0}", Statement));

         return cnt;
      }
   
      public int BuildStartPosStmt (GatewayAdapterCursor dbCrsr, bool reverse)
      {
         GatewayCursor     crsr;
         SQL3Dbd           sql3Dbd;
         DBKey             sortkey,
                           poskey = null;
         List<DBSegment>   seg;
         DBSegment         segment;
         bool              keyUnique = false,
                           segAsc = false,
                           fldNull = false,
                           fldNullable = false,
                           keyNullable = false,
                           tsField = false;

         DataSourceDefinition dbh = dbCrsr.Definition.DataSourceDefinition;
         DBField           fld;
         /* TEMPLATE 7 */
         List<Sql3SqlVar>  sqlvar = null;
         string            fldName = string.Empty,
                           and = " AND";
         int               mjrIdx = 0,
                           field = 0,
                           realfield = 0,
                           sqlvars = 0,
                           level = 0,
                           realLevel = 0,
                           segs = 0,
                           first = 0,
                           last = 0;

         string            noPrefix = string.Empty,
                           prefix = string.Empty;
         bool              addPrefix;
         int               idx = SqliteConstants.NULL_CHAR;
         int               timeCnt = 0;
         int               realSegs = 0;
         bool              currLvlFinished;
         string            sql3Literal = "?",
                           fldNameU = string.Empty;
         string            oldStatement = string.Empty;


         Statement = string.Empty;
         GatewayCursorTbl.TryGetValue(dbCrsr, out crsr);

         Logger.Instance.WriteDevToLog(string.Format("BuildStartPosStmt(): >>>>> {0}", (reverse ? "REVERSE" : "ORIGINAL")));

         DbdTbl.TryGetValue(dbh, out sql3Dbd);
         if (dbCrsr.CursorType == CursorType.Join)
         {
            prefix = SQL3PrefixBuf;
            addPrefix = true;
         }
         else
         {
            addPrefix = false;
            prefix = noPrefix;
         }

         if (dbCrsr.Definition.Key != null || sql3Dbd.IsView)
         {
            if (dbCrsr.Definition.Key != null)
            {
               sortkey = dbCrsr.Definition.Key;
               keyUnique = sortkey.CheckMask(KeyMasks.UniqueKeyModeMask);
               keyNullable = CheckKeyNullable(dbCrsr, true);
            }
            else
            {
               poskey = crsr.PosKey;
               sortkey = poskey;
            }
            if (dbCrsr.Definition.Key != null)
            {
               /* TEMPLATE 7 */
               sqlvar = crsr.Key.SqlVars;

               if (sql3Dbd.IsView)
               {
                  if ((keyUnique == true) && (keyNullable == false))
                     segs = sortkey.Segments.Count;
                  else
                  {
                     poskey = crsr.PosKey;
                     if (keyUnique == false)
                        segs = sortkey.Segments.Count + poskey.Segments.Count;
                     else  /* key_nullable */
                        if (sortkey == poskey)
                           segs = sortkey.Segments.Count;
                        else
                           segs = sortkey.Segments.Count + poskey.Segments.Count;
                  }
               }
               else
               {
                  if (keyUnique == true && (keyNullable == false))
                     segs = sortkey.Segments.Count;
                  else
                  {
                     segs = sortkey.Segments.Count + 1;
                  }
               }
            }
            else
               segs = poskey.Segments.Count;

            Logger.Instance.WriteDevToLog(string.Format("BuildStartPosStmt(): segs = {0}", segs));

            List<int> mjrIdxTbl = new List<int>(segs);
            // a table of idx of fld's which are actually involved in startpos (skipping DT_time field).
            for (level = 0; level < segs; level++)
            {
               if (sqlvar[(int)level].PartOfDateTime == SqliteConstants.TIME_OF_DATETIME)
                  timeCnt++;
               else
                  mjrIdxTbl.Add(level); 
            }

            realSegs = segs - timeCnt;

            first = 0;
            last = realSegs - 1;

            for (level = first, realLevel = first; level < segs; level++)
            {
               Statement = String.Empty;
               /*--------------------------------------------------------------------*/
               /* Copied from Sybase                                                 */
               /* The major segment's direction is important for the decision        */
               /* wheather to nullify the whole level (if the major descends and it's*/
               /* value is null). For special files (sort/sp) we must skip this      */
               /* check for the first level, becuase this level's major segment is   */
               /* an internal segment that doesnt appear in the poskey/sortkey.      */
               /*--------------------------------------------------------------------*/
               if (sqlvar[segs - level - 1].PartOfDateTime == SqliteConstants.TIME_OF_DATETIME)
               {
                  sqlvars++;
                  continue;
               }

               mjrIdx = mjrIdxTbl[realSegs - realLevel - 1];
               /*-------------------------------------------------------------------*/
               /* fix bug #67772 - and bug #997687 (part of it)                     */
               /* previous assignment to seg_asc was not correct because there was  */
               /* no check whether the segment is part of the sortkey or poskey     */
               /*-------------------------------------------------------------------*/

               if (!sql3Dbd.IsView && mjrIdx >= sortkey.Segments.Count)
               {
                  //This is ROWID
               }
               else
               {
                  if (mjrIdx < sortkey.Segments.Count)
                  {
                     segment = sortkey.Segments[mjrIdx];
                     
                  }
                  else
                  {
                     segment = poskey.Segments[mjrIdx - sortkey.Segments.Count];
                  }

                  segAsc = segment.CheckMask(SegMasks.SegDirAscendingMask);
                  segAsc = reverse ? !segAsc : segAsc;

               }

               /* if the major segment is null */
               if ((sqlvar[mjrIdx].NullIndicator == 1) &&
                   (level != 0) &&
                   (!segAsc))
               {
                  Statement = "";
                  Logger.Instance.WriteDevToLog("BuildStartPosStmt(): zeros SQL3_stmt");
               }
               else
               /* build the where phrase for this level */
               {
                  currLvlFinished = false;
                  for (field = realfield = first; field < (segs - level); field++)
                  {
                     if (sqlvar[field].PartOfDateTime == SqliteConstants.TIME_OF_DATETIME)
                        continue;

                     if (realfield == (realSegs - realLevel))
                        currLvlFinished = true;

                     // If the last field is a DT_date fld, then we may need to skip this 
                     // field only if curr-level is already finished.
                     if (level != 0)
                        if (field != first && field == (segs - level - 1) &&
                            sqlvar[field].PartOfDateTime != SqliteConstants.NORMAL_OF_DATETIME)
                           if (currLvlFinished)
                              continue;

                     fldName = sqlvar[field].SqlName;
                     fldNull = ((sqlvar[field].NullIndicator == 1) ? true : false);

                     fld = sqlvar[field].Fld;

                     sql3Literal = string.Format("{0}", "?");

                     /* the "and" string is null if is the first field */
                     if (field == first)
                        and = string.Empty;
                     else
                        and = " AND";

                     /* is the segment ascending or descending */
                     tsField = false;       // fix for direct

                     if (sqlvar[field].SqlType == Sql3Type.SQL3TYPE_ROWID)
                     {
                        segAsc = true;
                        if (dbCrsr.CursorType == CursorType.Join)
                           prefix = string.Format("{0}.", crsr.DbhPrefix[0]);

                        fldNameU = string.Format("{0}{1}", prefix, fldName);

                     }
                     else
                     {
                        if (dbCrsr.Definition.Key != null)
                        {
                           if (field < sortkey.Segments.Count)
                           {
                              /* a sortkey field */
                              seg = sortkey.Segments;
                              segAsc = ((seg[field].CheckMask(SegMasks.SegDirAscendingMask)) ? true : false);
                              fld = seg[field].Field;
                           }
                           else
                           {
                              /* a poskey field */
                              seg = poskey.Segments;
                              segAsc = ((seg[field - sortkey.Segments.Count].CheckMask(SegMasks.SegDirAscendingMask)) ? true : false);
                              fld = seg[field - sortkey.Segments.Count].Field;
                           }
                        }
                        else
                        {
                           seg = poskey.Segments;
                           segAsc = (((seg[field].CheckMask(SegMasks.SegDirAscendingMask))) ? true : false);
                           fld = seg[field].Field;
                        }

                        if (addPrefix)
                        {
                           if (!tsField) // fix for direct
                              idx = Sql3GetDbCrsrIndex(dbCrsr, fld);
                           if (tsField || idx == SqliteConstants.NULL_INDEX)  // fix for direct
                           {
                              prefix = string.Format("{0}.", crsr.DbhPrefix[0]);
                           }
                           else
                           {
                              prefix = string.Format("{0}.", crsr.DbhPrefix[crsr.SourceDbh[idx]]);
                           }

                        }

                        fldNameU = string.Format("{0}{1}", prefix, fldName);
                        // Defect 117640 : For numeric string which is having bigger picture, we save the data in the byte[] format. But there is problem with sqlite c# apis 
                        // while applying the where clause of this type of field. So while building the where clause, CAST the data as string.
                        if (fld.Storage == FldStorage.NumericString && fld.DataSourceDefinition != DatabaseDefinitionType.Normal && fld.DbType.Contains("BINARY"))
                        {
                           fldNameU = string.Format("CAST({0} as TEXT)", fldName);
                        }
                        else
                        {
                           fldNameU = string.Format("{0}{1}", prefix, fldName);
                        }

                        if ((fld.Storage == FldStorage.DateString && Sql3DateType(dbh, fld) != DateType.DATE_TO_SQLCHAR) ||
                            (fld.Storage == FldStorage.TimeString && Sql3DateType(dbh, fld) == DateType.DATE_TO_DATE))
                        {
                           if (fld.PartOfDateTime != 0)
                           {
                              sql3Literal = string.Format("{0} ", SqliteConstants.DATETIME_LITERAL);
                              fldNameU = string.Format("DATETIME({0}{1})", prefix, fldName);
                           }
                           else
                           {
                              if (fld.Storage == FldStorage.TimeString)
                              {
                                 sql3Literal = string.Format("{0} ", SqliteConstants.TIME_LITERAL);
                                 fldNameU = string.Format("TIME({0}{1})", prefix, fldName);
                              }
                              else
                              {
                                 sql3Literal = string.Format("{0} ", SqliteConstants.DATE_LITERAL);
                                 fldNameU = string.Format("DATE({0}{1})", prefix, fldName);
                              }

                           }
                        }
                     }

                     segAsc = (reverse ? !segAsc : segAsc);

                     if (fldName == SqliteConstants.SQL3_ROWID_ST_A)
                     {
                        fldNullable = false;
                     }
                     else
                     {
                        if (!tsField) // fix for direct
                           fldNullable = ((fld.AllowNull || crsr.OuterJoin) ? true : false);
                        else
                           fldNullable = false;
                     }

                     Logger.Instance.WriteDevToLog(string.Format("BuildStartPosStmt(): fld_name = {0}, nullable = {1}, fld_null = {2}, seg_asc = {3}", fldName,
                                    (fldNullable ? "TRUE" : "FALSE"), (fldNull ? "TRUE" : "FALSE"),
                                    (segAsc ? "TRUE" : "FALSE")));
                     
                     /* last level (and not first)*/
                     if ((realLevel == last) && (realLevel != first))
                     {
                        if (!segAsc)
                        {
                           if (fldNullable)
                           {
                              if (fldNull)
                              {
                                 Statement += "1 = 2 ";
                              }
                              else
                              {
                                 Statement += string.Format("({0} < {1}", fldNameU, sql3Literal);
                                 Statement += string.Format(" OR {0} IS NULL)", fldNameU);
                              }
                           }
                           else
                           {
                              Statement += string.Format("{0} < {1}", fldNameU, sql3Literal);
                           }
                        }
                        else
                        {
                           if (fldNullable)
                              if (fldNull)
                              {
                                 Statement += string.Format("{0} IS NOT NULL ", fldNameU);
                              }
                              else
                              {
                                 Statement += string.Format("{0} {1} > {2}", and, fldNameU, sql3Literal);
                              }
                           else
                           {
                              Statement += string.Format("{0} > {1}", fldNameU, sql3Literal);
                           }
                        }
                     }
                     /*************** not last level, not last field ***************/
                     else if (realfield < (realSegs - realLevel - 1))
                     {
                        /* not last level */
                        /* not last field */
                        if (/* DDD */ fldNullable && fldNull)
                        {
                           Statement += string.Format("{0} {1} IS NULL ", and, fldNameU);
                        }
                        else
                        {
                           Statement += string.Format("{0} {1} = {2}", and, fldNameU, sql3Literal);
                        }
                     }
                     /*************** last field, first level ***************/
                     else if (realLevel == first)
                     {
                        /* last field */
                        /* first level */
                        if (!segAsc)
                        {
                           if (fldNullable)
                           {
                              if (fldNull)
                              {
                                 if (first == last)
                                 {
                                    Statement += "NULLWHERE";
                                 }
                                 else
                                 {
                                    continue;
                                 }
                              }
                              else
                              {
                                 Statement += string.Format("{0} ({1} <= {2} ", and, fldNameU, sql3Literal);
                                 Statement += string.Format(" OR {1} IS NULL)", fldNameU);
                              }
                           }
                           else
                           {
                              Statement += string.Format("{0} {1} <= {2}", and, fldNameU, sql3Literal);
                           }
                        }
                        else
                        /* first level seg desc */
                        {
                           if (fldNullable)
                           {
                              if (fldNull)
                              {
                                 Statement += string.Format("{0} {1} IS NULL", and, fldNameU);
                              }
                              else
                              {
                                 Statement += string.Format("{0} {1} >= {2}", and, fldNameU, sql3Literal);
                              }
                           }
                           else
                           {
                              Statement += string.Format("{0} {1} >= {2}", and, fldNameU, sql3Literal);
                           }
                        }
                     }
                     /*************** all other levels other than first ***************/
                     else
                     {
                        if (!segAsc)
                        {
                           if (fldNullable)
                           {
                              if (fldNull)
                              {
                                 Statement += string.Format("{0} {1} IS NOT NULL ", and, fldNameU);
                              }
                              else
                              {
                                 Statement += string.Format("{0} ({1} < {2}", and, fldNameU, sql3Literal);
                                 Statement += string.Format(" OR {0} IS NULL)", fldNameU);
                              }
                           }
                           else
                           {
                              Statement += string.Format("{0} {1} < {2}", and, fldNameU, sql3Literal);
                           }
                        }
                        else
                        {
                           if (fldNullable)
                           {
                              if (fldNull)
                                 continue;
                              else
                              {
                                 Statement += string.Format("{0} {1} > {2}", and, fldNameU, sql3Literal);
                              }
                           }
                           else
                           {
                              Statement += string.Format("{0} {1} > {2}", and, fldNameU, sql3Literal);
                           }
                        }
                     }
                     if (!(fldNullable && fldNull))
                     {
                        if (tsField)
                        {

                           //crsr.DbPosBuf = db_crsr.Definition.StartPosition.Get();
                           //Sql3Bi
                           //sql3_binary_to_str (SQL3_stmt + pos, SQL3_buffer_size - pos, (Uchar *)crsr->db_pos_buf, SQL3_TIMESTAMP_LEN);
                           //pos += (2 + SQL3_TIMESTAMP_LEN * 2);
                           //MEMCPY (sqlvar[field].sqldata, sqlvar[field].sqllen, crsr->db_pos_buf, 8);
                        }
                        else
                        {
                           // TODO : no need to do this whole else part, as we have data in sqlvar..
                           ;
                        }
                     }

                     if ((!Statement.Equals(oldStatement)) && (realLevel == first))
                        sqlvars++;
                     oldStatement = Statement;
                     realfield++;
                  }
               }

               crsr.StmtStartpos[realLevel] = Statement;

               Logger.Instance.WriteDevToLog(string.Format("BuildStartPosStmt(): level: {0} = {1}", realLevel, crsr.StmtStartpos[realLevel]));
               realLevel++;
            }
         }
         else
         {
            if (reverse)
            {
               crsr.StmtStartpos[0] = "rowid <= ? ";
            }
            else
            {
               crsr.StmtStartpos[0] = "rowid >= ? ";
            }

            Logger.Instance.WriteDevToLog(string.Format("BuildStartPosStmt(): level: {0} = {1}", level, crsr.StmtStartpos[level]));
            level = 1;
            sqlvars++;
         }

         crsr.StrtposCnt = level - timeCnt;

         Logger.Instance.WriteDevToLog(string.Format("BuildStartPosStmt(): <<<<< sqlvars = {0}", sqlvars));
         return (sqlvars);
      }

      /// <summary>
      /// Implementation for reverse order
      /// </summary>
      /// <param name="sql3Stmt"></param>
      /// <returns></returns>
      public string SQL3StmtReverseOrder (string sql3Stmt)
      {
         Logger.Instance.WriteDevToLog("SQL3StmtReverseOrder(): >>>>>");

         string []fromStr1 = {">", "<"};
         string []toStr1 = {"<", ">" };

         sql3Stmt = StrUtil.searchAndReplace(sql3Stmt, fromStr1, toStr1);

         string []fromStr2 = {" ASC", " DESC", " DESC,"};
         string []toStr2 = {" DESC", " ASC", " ASC, "};

         sql3Stmt = StrUtil.searchAndReplace(sql3Stmt, fromStr2, toStr2);

         Logger.Instance.WriteDevToLog(string.Format("SQL3StmtReverseOrder(): <<<<< \n\tSQL: {0}", sql3Stmt));

         return sql3Stmt;
      }

      /// <summary>
      ///  BuildOrderByStmt ()
      /// </summary>
      /// <param name="dbCrsr"></param>
      /// <param name="stmt"></param>
      /// <param name="key"></param>
      /// <returns>int</returns>
      public int BuildOrderByStmt (GatewayAdapterCursor dbCrsr, ref string stmt, DBKey key)
      {
         GatewayCursor       crsr;
         DataSourceDefinition dbh = dbCrsr.Definition.DataSourceDefinition;
         int                 segIdx = 0;
         int                 pos = 0;
         DBSegment           seg = null;
         bool                firstSeg = true;
         string              name,
                             noPrefix  = "",
                             prefix     = null;
         SQL3Dbd sql3Dbd;
         bool                addPrefix;
         int                 idx;
         string              rowidStmt,
                             stmtptr,
                             fullName;
         bool                keyNullable = false;

         GatewayCursorTbl.TryGetValue(dbCrsr, out crsr);
         DbdTbl.TryGetValue(dbh, out sql3Dbd);

         Logger.Instance.WriteDevToLog("BuildOrderByStmt(): >>>>>");
         
         if (dbCrsr.CursorType == CursorType.Join)
         {
            prefix = SQL3PrefixBuf;
            addPrefix = true;
         }
         else
         {
            addPrefix = false;
            prefix = noPrefix;
         }

         /* if there is a key on the file */
         if (key != null)
         {
            Logger.Instance.WriteDevToLog("BuildOrderByStmt(): key is not NULL PTR");

            keyNullable = CheckKeyNullable(dbCrsr, true);

            /* for all segments of the current key */
            for (segIdx = 0 ; segIdx < key.Segments.Count ; ++segIdx)
            {
               seg = key.Segments[segIdx];
               DBField fld = seg.Field;

               if (!((fld.Storage == FldStorage.TimeString) && (fld.PartOfDateTime != 0)))
               {
                  name = fld.DbName;

                  /* if this is not the first segment then add a comma */
                  /* add the field name */
                  if (addPrefix)
                  {
                     idx = Sql3GetDbCrsrIndex (dbCrsr, fld);
                     if (idx == SqliteConstants.NULL_INDEX)
                     {
                        prefix += string.Format("{0}.", crsr.DbhPrefix[0]);
                     }
                     else
                     {
                        prefix += string.Format("{0}.", crsr.DbhPrefix[crsr.SourceDbh[idx]]);
                     }
                  }

                  /*--------------------------------------------------------------*/
                  /* if this field already exists on the order by clause, skip it */
                  /* Note that we must check that the field's name appears,       */
                  /* surrounded by two white-spaces.                              */
                  /*--------------------------------------------------------------*/
                  stmtptr = stmt;
                  fullName = prefix;
                  fullName += name;

                  if (stmtptr.Contains(fullName + " "))
                  {
                     continue;
                  }

                  /* TEMPLATE 7*/
                  if (!firstSeg)
                  {
                     if (dbCrsr.Definition.Direction == Order.Ascending)
                     {
                        if (seg.CheckMask(SegMasks.SegDirAscendingMask))
                        {
                           stmt += string.Format(",{0}{1} ASC ", prefix, name);
                        }
                        else
                        {
                           stmt += string.Format(",{0}{1} DESC", prefix, name);
                        }
                     }
                     else
                     {
                        if (seg.CheckMask(SegMasks.SegDirAscendingMask))
                        {
                           stmt += string.Format(",{0}{1} DESC", prefix, name);
                        }
                        else
                        {
                           stmt += string.Format(",{0}{1} ASC ", prefix, name);
                        }
                     }
                  }
                  else
                  {
                     if (dbCrsr.Definition.Direction == Order.Ascending)
                     {
                        if (seg.CheckMask(SegMasks.SegDirAscendingMask))
                        {
                           stmt += string.Format("{0}{1} ASC ", prefix, name);
                        }
                        else
                        {
                           stmt += string.Format("{0}{1} DESC", prefix, name);
                        }
                     }
                     else
                     {
                        if (seg.CheckMask(SegMasks.SegDirAscendingMask))
                        {
                           stmt += string.Format("{0}{1} DESC", prefix, name);
                        }
                        else
                        {
                           stmt += string.Format("{0}{1} ASC ", prefix, name);
                        }
                     }
                     firstSeg = false;
                  }
               }
            }

            /* if duplicate keys are allowedb  */
            if (key.CheckMask(KeyMasks.DuplicateKeyModeMask) || (keyNullable && (key != crsr.PosKey)))
            {
               if (sql3Dbd.IsView)
               {
                  pos = BuildOrderByStmt (dbCrsr, ref stmt, crsr.PosKey);
               }
               else
               {
                  if (dbCrsr.CursorType == CursorType.Join)
                  {
                     prefix += string.Format("{0}.", crsr.DbhPrefix[0]);
                  }

                  if (!firstSeg)
                  {
                     if (dbCrsr.Definition.Direction == Order.Ascending)
                     {
                        stmt += string.Format(",{0}rowid ASC ", prefix);
                     }
                     else
                     {
                        stmt += string.Format(",{0}rowid DESC", prefix);
                     }
                  }
                  else
                  {
                     if (dbCrsr.Definition.Direction == Order.Ascending)
                     {
                        stmt += string.Format("{0}rowid ASC ", prefix);
                     }
                     else
                     {
                        stmt += string.Format("{0}rowid DESC", prefix);
                     }
                  }
               }
            }
         }
         /* if there is no key on the file ,use the PosKey as the sortkey*/
         else if (sql3Dbd.IsView)
         {
            /* TEMPLATE 7*/
            if (dbCrsr.Definition.CursorMode != CursorMode.Batch)
               pos = BuildOrderByStmt (dbCrsr, ref stmt, crsr.PosKey);
         }
         else 
         {
            if (dbCrsr.Definition.CursorMode != CursorMode.Batch)    
            {
               if (dbCrsr.CursorType == CursorType.Join)
               {
                  prefix = SQL3PrefixBuf;
                  prefix = string.Format("{0}.", crsr.DbhPrefix[0]);
               }
               else
               {
                  prefix = noPrefix;
               }

               rowidStmt = string.Format("{0}{1}", prefix, SqliteConstants.SQL3_ROWID_ST_A);

               if (dbCrsr.Definition.Direction == Order.Ascending)
               {
                  stmt += string.Format("{0}  ASC", rowidStmt);
               }
               else
               {
                  stmt += string.Format("{0} DESC", rowidStmt);
               }
            }
         }

         Logger.Instance.WriteDevToLog(string.Format("BuildOrderByStmt(): <<<<<\n\tSQL: {0}", stmt));

         return pos;
      }

      public bool IsSql3DateToDate (DatabaseDefinition dbDefinition)
      {
         return false;
      }

      /// <summary>
      ///  BuildFieldListStmt ()
      /// </summary>
      /// <param name="dbCrsr"></param>
      /// <param name="fieldlist"></param>
      /// <param name="addPrefix"></param>
      /// <returns>int</returns>
      public int BuildFieldListStmt (GatewayAdapterCursor dbCrsr, ref string fieldList, bool addPrefix)
      {
         GatewayCursor        crsr;
         DataSourceDefinition dbh = dbCrsr.Definition.DataSourceDefinition;
         int                  fldNum = 0;
         string               name = string.Empty;
         string               noPrefix = "",
                              prefix;
         DBField              fld;
         bool                 first = true;

         GatewayCursorTbl.TryGetValue(dbCrsr, out crsr);
         Logger.Instance.WriteDevToLog("BuildFieldListStmt(): >>>>>");
         
         //fieldlist[pos] = STR_U('\0');

         if (! addPrefix)
         {
            prefix = noPrefix;
         }
         else
         {
            prefix = SQL3PrefixBuf;
         }

         /* for all fields in the DB_CRSR field list */
         for (fldNum = 0; fldNum < dbCrsr.Definition.FieldsDefinition.Count; ++fldNum)
         {
            /* get an index to the field */
            fld = dbCrsr.Definition.FieldsDefinition[fldNum];

            if (!((fld.Storage == FldStorage.TimeString) && (fld.PartOfDateTime != 0)))
            {
               /* get the field name */
               name = fld.DbName;

               /* only for join add the prefix to the field */
               if (addPrefix)
               {
                  prefix = string.Format("{0}.", crsr.DbhPrefix[crsr.SourceDbh[fldNum]]);
               }
               /* if not first field add a comma */
               if (!first)
               {
                  fieldList += ",";
               }
               else
               {
                  first = false;
               }

               if ((fld.Storage == FldStorage.DateString && Sql3DateType(dbh, fld) != DateType.DATE_TO_SQLCHAR) ||
                   (fld.Storage == FldStorage.TimeString && Sql3DateType(dbh, fld) == DateType.DATE_TO_DATE))
               {
                  if (fld.PartOfDateTime != 0)
                  {
                     fieldList += string.Format("DATETIME({0}{1}) ", prefix, name);
                  }
                  else
                  {
                     if (fld.Storage == FldStorage.TimeString)
                     {
                        fieldList += string.Format("TIME({0}{1}) ", prefix, name);
                     }
                     else
                     {
                        fieldList += string.Format("DATE({0}{1}) ", prefix, name);
                     }
                  }
               }
               else
               {
                  fieldList += string.Format("{0}{1} ", prefix, name);
               }
            }
         }

         Logger.Instance.WriteDevToLog(string.Format("BuildFieldListStmt(): <<<<<\n\tSQL: {0}", fieldList));

         return 0;
      }
   
      public int BuildAllDBHFieldListStmt (GatewayAdapterCursor db_crsr, string fieldlist)
      {
         return 0;
      }

      public int BuildKeyFieldListStmt(GatewayAdapterCursor dbCrsr, out string fieldlist, DBKey key, bool hasPrefix)
      {
         GatewayCursor        crsr;
         DBSegment            seg;
         DataSourceDefinition dbh         = dbCrsr.Definition.DataSourceDefinition;
         int                  segIdx      = 0;
         string               name        = string.Empty;
         int                  pos         = 0;
         string               noPrefix    = " ",
                              prefix      = string.Empty;
         bool                 keyUnique   = false,
                              keyNullable = false,
                              addPrefix   = false;
         SQL3Dbd              sql3Dbd;
         int                  idx;

         fieldlist = string.Empty;
         GatewayCursorTbl.TryGetValue(dbCrsr, out crsr);
         DbdTbl.TryGetValue(dbh, out sql3Dbd);

         Logger.Instance.WriteDevToLog(string.Format("BuildKeyFieldListStmt(): >>>>> hasPrefix = {0}", hasPrefix));

         if (dbCrsr.Definition.Key != null)
         {
            keyUnique = key.CheckMask(KeyMasks.UniqueKeyModeMask);
            keyNullable = CheckKeyNullable(dbCrsr, true);

            /* get a pointer to the first segment */

            if (dbCrsr.CursorType == CursorType.Join)
            {
               prefix = SQL3PrefixBuf;
               addPrefix = true;
            }
            else
            {
               addPrefix = false;
               prefix = noPrefix;
            }

            /* for all segments in the key */
            for (segIdx = 0; segIdx < key.Segments.Count; ++segIdx)
            {
               seg = key.Segments[segIdx];
               if ((seg.Field.Storage == FldStorage.TimeString) &&
                  (seg.Field.PartOfDateTime != 0))
                  continue;

               /* if not first segment add a comma */
               if (segIdx > 0)
               {
                  fieldlist += ",";
               }

               /* get the field name */
               name = seg.Field.DbName;
               if (addPrefix)
               {
                  idx = Sql3GetDbCrsrIndex(dbCrsr, seg.Field);
                  if (idx == SqliteConstants.NULL_INDEX)
                  {
                     prefix += string.Format("{0}.", crsr.DbhPrefix[0]);
                  }
                  else
                  {
                     prefix += string.Format("{0}.", crsr.DbhPrefix[crsr.SourceDbh[idx]]);
                  }

               }

               /* add it to the string */
               fieldlist += string.Format("{0}{1}", prefix, name);
            }

            if ((keyUnique == false) ||
              ((keyUnique == true) && (keyNullable == true)))
            {
               if (!sql3Dbd.IsView)
               {

                  if (dbCrsr.CursorType == CursorType.Join)
                  {
                     prefix += string.Format("{0}.", crsr.DbhPrefix[0]);
                  }/* endif */

                  fieldlist += string.Format(",{0}{1}", prefix, SqliteConstants.SQL3_ROWID_ST_A);
               }
            }
         }

         Logger.Instance.WriteDevToLog(string.Format("BuildKeyFieldListStmt(): <<<<<\n\tSQL: {0}", fieldlist));

         return pos;
      }
   
      public int BuildDeleteStmt (GatewayAdapterCursor dbCrsr)
      {
         GatewayCursor  crsr;
         SQL3Dbd        sql3Dbd;
         int            cnt = 0;

         GatewayCursorTbl.TryGetValue(dbCrsr, out crsr);
         DbdTbl.TryGetValue(dbCrsr.Definition.DataSourceDefinition, out sql3Dbd);

         Statement = string.Empty;

         Logger.Instance.WriteDevToLog("BuildDeleteStmt(): >>>>>");

         // Build the WHERE clause of the DELETE statement.
         if (sql3Dbd.IsView)
         {
            /*----------------------------------------------------------------------------*/
            /* Update/Delete stratigy should be considered only for Deferred Transaction  */
            /*                                                                            */
            /* If Physical Transaction,                                                   */
            /*    If Logical Locking = TRUE && Physical Locking = FALSE                   */
            /*          Use All fields in Where clause                                    */
            /*    Else                                                                    */
            /*          Use DBPOS in WHERE clause                                         */
            /*  Else (Deferred Trnasaction)                                               */
            /*    Use fields according to Update/Delete Strategy                          */
            /*----------------------------------------------------------------------------*/

            cnt = BuildWhereViewStmt(dbCrsr, dbCrsr.Definition.CurrentPosition, 0);
         }
         else
         {
            Statement += string.Format(" {0} = ?", SqliteConstants.SQL3_ROWID_ST_A);
            cnt++;
         }

         /* build the DELETE statement */
         crsr.StmtWhereKey = Statement;

         Statement = string.Format("DELETE FROM {0} WHERE {1}", sql3Dbd.TableName, crsr.StmtWhereKey);

         Logger.Instance.WriteDevToLog(string.Format("SQL3StmtBuildDelete(): <<<<<\n\tSQL: {0}", Statement));

         return cnt;
      }
   
      /// <summary>
      /// 
      /// </summary>
      /// <param name="dbCrsr"></param>
      /// <returns></returns>
      public int BuildUpdateStmt (GatewayAdapterCursor dbCrsr)
      {
         GatewayCursor          crsr;
         SQL3Dbd                sql3Dbd;
         DataSourceDefinition   dbh       = dbCrsr.Definition.DataSourceDefinition;
         DBField                fld;
         int                    idx       = 0;
         string                 name      = string.Empty,
                                data      = "?";
         bool                   first     = true;
         int                    cnt       = 0;
         int                    blobIdx   = 0;
         bool                   set2Null  = false;

         Logger.Instance.WriteDevToLog("BuildUpdateStmt(): >>>>>");
         
         GatewayCursorTbl.TryGetValue(dbCrsr, out crsr);
         DbdTbl.TryGetValue(dbh, out sql3Dbd);

         Statement = string.Empty;

         Statement += string.Format("UPDATE {0} SET ", sql3Dbd.TableName);

         /* for all fields in the DB_CRSR field list */
         for (idx = 0 ; idx < dbCrsr.Definition.FieldsDefinition.Count ; ++idx)
         {
            /* get an index to the field */
            fld = dbCrsr.Definition.FieldsDefinition[idx];

            if (! dbCrsr.Definition.IsFieldUpdated[idx])
            {
               if(fld.IsBlob())
                  blobIdx++;
               continue;
            }
      
            /* not to put IDENTITY column name into INSERT statement */
            if (Sql3CheckDbtype (fld, SqliteConstants.IDENTITY_STR))
               continue;

            name = fld.DbName;

            if ((fld.Storage == FldStorage.TimeString) && (fld.PartOfDateTime != 0))
            {
               if(!(fld.AllowNull && dbCrsr.CurrentRecord.IsNull(idx)))
                  cnt++;
               continue;
            }
            if (fld.IsBlob())
            {
               blobIdx++;
            }

            /* if not first field add a comma */
            if (first)
            {
               first = false;
            }
            else
            {
               Statement += ", ";
            }

            if (fld.AllowNull && dbCrsr.CurrentRecord.IsNull(idx))
            {
               data = string.Format("{0}", "NULL");
               set2Null = true;
            }
            else
            {
               if ((fld.Storage == FldStorage.DateString && Sql3DateType(dbh, fld) != DateType.DATE_TO_SQLCHAR) ||
                   (fld.Storage == FldStorage.TimeString && Sql3DateType(dbh, fld) == DateType.DATE_TO_DATE))
               {
                  if (fld.PartOfDateTime != 0)
                  {
                     data = string.Format("{0}", "DATETIME(?)");
                  }
                  else
                  {
                     if (fld.Storage == FldStorage.TimeString)
                     {
                        data = string.Format("{0}", " TIME(?)");
                     }
                     else
                     {
                        data = string.Format("{0}", " DATE(?)");
                     }
                  }
               }
               else
               {
                  data = string.Format("{0}", "?");
               }

               cnt++;
            }

            /* For Differential update */
            if (fld.DiffUpdate == 'Y')
            {
               Statement += string.Format("{0} = IFNULL({1}, 0) + {2}", name, name, data);
            }
            else
            {
               Statement += string.Format("{0} = {1}", name, data);
            }
         }


         // cnt will be zero if 1. only blob INTEGER PRIMARY KEY field is updated. 2. all flds are updated with NULL.
         if (cnt > 0 || set2Null)
         {
            if (sql3Dbd.IsView)
            {
               // Build the WHERE clause of the UPDATE statement.
               Statement += " WHERE ";
               
               /*----------------------------------------------------------------------------*/
               /* Update/Delete stratigy should be considered only for Deferred Transaction  */
               /*                                                                            */
               /* If Physical Transaction,                                                   */
               /*    If Logical Locking = TRUE && Physical Locking = FALSE                   */
               /*          Use All fields in Where clause                                    */
               /*    Else                                                                    */
               /*          Use DBPOS in WHERE clause                                         */
               /*  Else (Deferred Trnasaction)                                               */
               /*    Use fields according to Update/Delete Strategy                          */
               /*----------------------------------------------------------------------------*/
              
                cnt += BuildWhereViewStmt(dbCrsr, dbCrsr.Definition.CurrentPosition, 0);
              
            }
            else
            {
               Statement += string.Format(" WHERE {0} = ?", SqliteConstants.SQL3_ROWID_ST_A);
               cnt++;
            }
         }

         Logger.Instance.WriteDevToLog(string.Format("BuildUpdateStmt(): <<<<<\n\tSQL: {0}", Statement));
         
         return cnt;
      }

      public int BuildWhereKeyStmt (GatewayAdapterCursor db_crsr, string stmt, int stmtSizeInChars, DBKey key, bool hasPrefix)
      {
         return 0;
      }
   
      public int BuildDbposToWhereKeyStmt (GatewayAdapterCursor db_crsr, string stmt, int stmtSizeInChars, DBKey key, bool hasPrefix)
      {
         return 0;
      }

      public void SQL3BuildWhereFullWithValues (GatewayAdapterCursor db_crsr, string stmt, int stmtSizeInChars)
      {

      }

      /// <summary>
      ///  Sql3CursorAlloc ()
      /// </summary>
      /// <param name="name"></param>
      /// <param name="crsrHdl"></param>
      /// <param name="cursorIdx"></param>
      /// <returns>int</returns>
      public int Sql3CursorAlloc (string name, int cursorIdx)
      {
         int         i            = 0;
         SQL3Cursor  sql3Cursor   = null;

         Logger.Instance.WriteDevToLog(string.Format("Sql3CursorAlloc(): >>>>> cursor name - {0}", name));

         if (cursorIdx == SqliteConstants.NULL_CURSOR)
         {
            Logger.Instance.WriteDevToLog("Sql3CursorAlloc(): cursor does not exist, allocating new");

            /* for all SQL3 cursors in the cursor table */
            for (i = 0 ; i < CursorTbl.Count; ++i)
            {
               sql3Cursor = CursorTbl[i];
               if (sql3Cursor != null && sql3Cursor.InUse == false)
                  break;
            }

            /* if past the last cursor the allocate space for a new one */
            if (i == CursorTbl.Count)
            {
               sql3Cursor = new SQL3Cursor();
               CursorTbl.Add(sql3Cursor);
            }

            sql3Cursor.Name = string.Format("%s%d", name, i);
            sql3Cursor.InUse = true;
            sql3Cursor.InputSqlda = null;
            sql3Cursor.OutputSqlda = null;
            sql3Cursor.StartPosLevel = SqliteConstants.NULL_LEVEL;
            sql3Cursor.DoOpen = true;
            sql3Cursor.StmtIdx = SqliteConstants.NULL_STMT;
            sql3Cursor.IsRange = false; //SC

         }
         else
         {
            i = cursorIdx;
            Logger.Instance.WriteDevToLog("Sql3CursorAlloc(): cursor exist, doing nothing");
         }

         Logger.Instance.WriteDevToLog(string.Format("Sql3CursorAlloc(): <<<<< cursor index = {0}", i));

         return i;
      }

      /// <summary>
      /// Sql3CursorRelease()
      /// </summary>
      /// <param name="cursorIdx"></param>
      public void Sql3CursorRelease(int cursorIdx)
      {
         if (CursorTbl[cursorIdx].InputSqlda != null)
         {
            CursorTbl[cursorIdx].InputSqlda.SQL3SqldaFree();
         }

         if (CursorTbl[cursorIdx].OutputSqlda != null)
         {
            CursorTbl[cursorIdx].OutputSqlda.SQL3SqldaFree();
         }

         CursorTbl[cursorIdx] = null;
      }
      
      /// <summary>
      ///  Sql3DbdAlloc ()
      /// </summary>
      /// <returns>int</returns>
      public int Sql3DbdAlloc(DataSourceDefinition dbh)
      {
         int i = 0;
         SQL3Dbd sql3dbd = null;

         Logger.Instance.WriteDevToLog("Sql3DbdAlloc(): >>>>>");

         /* for all SQL3 dbd's in the dbd table */
         sql3dbd = new SQL3Dbd();
         DbdTbl.Add(dbh, sql3dbd);
         

         sql3dbd.arrayBufferSize = SqliteConstants.NULL_ARRAY_SIZE;
         sql3dbd.IsView          = false;
         sql3dbd.IsPhysicalLock  = false;
         sql3dbd.TableName = string.Empty;
         sql3dbd.DatabaseName = string.Empty;
         sql3dbd.FullName = string.Empty;

         Logger.Instance.WriteDevToLog(string.Format("Sql3DbdAlloc(): <<<<< sql3dbd index = {0}", i));

         return i;
      }

      /// <summary>
      ///  Sql3StmtAlloc ()
      /// </summary>
      /// <param name="stmtName"></param>
      /// <param name="crsrHdl"></param>
      /// <param name="stmtIdx"></param>
      /// <param name="connection"></param>
      /// <returns>int</returns>
      public int Sql3StmtAlloc (string stmtName, int stmtIdx, string databaseLocation)
      {
         int  i = 0;
         Sql3Stmt sql3Stmt = null;

         Logger.Instance.WriteDevToLog(string.Format("Sql3StmtAlloc: >>>>> stmt name - {0}", stmtName));

         if (stmtIdx == SqliteConstants.NULL_STMT)
         {
            Logger.Instance.WriteDevToLog("Sql3StmtAlloc: stmt does not exist, allocating new");

            /* for all SQL3 statements in the statement table */
            for (i = 0 ; i < StmtTbl.Count; ++i)
            {
               sql3Stmt = StmtTbl[i];
               if (sql3Stmt.InUse == false)
                  break;
            }

            /* if past the last statement the allocate space for a new one */
            if (i == StmtTbl.Count)
            {
               sql3Stmt = new Sql3Stmt();
#if !SQLITE_CIPHER_CPP_GATEWAY
               sql3Stmt.DataReader = null;
               sql3Stmt.sqliteCommand = null;
#endif
               StmtTbl.Add(sql3Stmt);
            }

            sql3Stmt.Idx = i;
            sql3Stmt.Name = string.Format("%s%d", stmtName, i);
            sql3Stmt.InUse = true;
            sql3Stmt.IsPrepared = false;
            sql3Stmt.IsOpen = false;
         }
         else
         {
            i = stmtIdx;
            Logger.Instance.WriteDevToLog("Sql3StmtAlloc: stmt already exist, doing nothing");
         }

         Logger.Instance.WriteDevToLog(string.Format("Sql3StmtAlloc: <<<<< statement index = {0}", i));

         return i;
      }

   
      /// <summary>
      /// SQL3BuffToSqlind()
      /// </summary>
      /// <param name="crsr"></param>
      /// <param name="db_crsr"></param>
      /// <param name="ForInsert"></param>
      /// <param name="updateBlob"></param>
      public void InitializeCrsrNullIndicator (GatewayCursor crsr, GatewayAdapterCursor dbCrsr, bool ForInsert, out bool updateBlob)
      {
         updateBlob = false;
         int fldIdx = 0;
         DBField fld = null;

         Logger.Instance.WriteDevToLog(string.Format("InitializeCrsrNullIndicator(): >>>>> # of fields = {0}", dbCrsr.Definition.FieldsDefinition.Count));

         for (fldIdx = 0; fldIdx < dbCrsr.Definition.FieldsDefinition.Count; ++fldIdx)
         {
            fld = dbCrsr.Definition.FieldsDefinition[fldIdx];
            // the fld_update indicates (in crsr_insert) if this field was selected in the 
            // Magic task. if not, then it should not appear in the insert command.
            if (ForInsert && !dbCrsr.Definition.IsFieldUpdated[fldIdx])
               continue;

            if (fld.AllowNull)
            {
               if (dbCrsr.CurrentRecord.IsNull(fldIdx))
               {
                  crsr.NullIndicator[fldIdx] = 1;
                  Logger.Instance.WriteDevToLog(string.Format("InitializeCrsrNullIndicator(): field index  %d is null", fldIdx));
               }
               else
                  crsr.NullIndicator[fldIdx] = 0;
            }
            else
            {
               crsr.NullIndicator[fldIdx] = 0;
            }

            if (fld.IsBlob()/* && ! ((Uchar *)(db_crsr->null_buf))[crsr_idx]*/)
                  updateBlob = true;
         }

         Logger.Instance.WriteDevToLog(string.Format("InitializeCrsrNullIndicator(): <<<<< fldIdx = {0}", fldIdx));
      }

      public SQL3_CODE SQL3SelectUsingKey (GatewayAdapterCursor db_crsr, DBKey key)
      {
         SQL3_CODE returnCode = 0;
         return returnCode;
      }

      public SQL3_CODE  Sql3GetNewTs (GatewayAdapterCursor db_crsr, string tsFldName, string tsSqldata, out int tsSqlind)
      {
         tsSqlind = 0;
         SQL3_CODE returnCode = 0;
         return returnCode;
      }

      public bool Sql3IsDbCrsrField(GatewayAdapterCursor dbCrsr, DBField fld)
      {
         int i = 0;

         for (i = 0; i < dbCrsr.Definition.FieldsDefinition.Count; i++)
         {
            {
               if (fld == dbCrsr.Definition.FieldsDefinition[i])
                  return true;
            }
         }

         return false;
      }
   
      /// <summary>
      /// Sql3AllKeySegmentsInView()
      /// </summary>
      /// <param name="db_crsr"></param>
      /// <returns></returns>
      public bool Sql3AllKeySegmentsInView (GatewayAdapterCursor dbCrsr)
      {
         GatewayCursor crsr;
         SQL3Dbd Sql3Dbd;
         DBSegment seg = null;
         short segIdx = 0;
         bool allSegsInView = true;

         GatewayCursorTbl.TryGetValue(dbCrsr, out crsr);
         DbdTbl.TryGetValue(dbCrsr.Definition.DataSourceDefinition, out Sql3Dbd);

         DBKey PosKey = crsr.PosKey;
         if (crsr.PosKey != null)//fil_type != MAGIC_SP_FILE &&
         {
            PosKey = crsr.PosKey;

            if (crsr.KeyArray == null)
               Sql3MakeDbpoSegArray(dbCrsr);

            /*Key segment is not in view, if fld_update is FALSE for any key segment*/
            for (segIdx = 0; segIdx < PosKey.Segments.Count; segIdx++)
            {
               seg = PosKey.Segments[segIdx];
               // if the pos key field is not in magic data view and its not either 
               // MAGICKEY or IDENTITY field.
               if ((crsr.KeyArray[segIdx] >= dbCrsr.Definition.FieldsDefinition.Count) &&
                  (seg.Field != Sql3Dbd.magicFld && seg.Field != Sql3Dbd.identityFld))
               {
                  allSegsInView = false;
                  break;
               }

               if (seg.Field != Sql3Dbd.magicFld && seg.Field != Sql3Dbd.identityFld)
               {
                  for (int i = 0; i < dbCrsr.Definition.FieldsDefinition.Count; i++)
                  {
                     if (dbCrsr.Definition.FieldsDefinition[i]== seg.Field && !dbCrsr.Definition.IsFieldUpdated[i])
                     {
                        allSegsInView = false;
                        break;
                     }
                  }
               }

               if (!allSegsInView)
                  break;
            }
         }

         return allSegsInView;
      }

      /// <summary>
      ///  BuildDbPos ()
      /// </summary>
      /// <param name="dbPos"></param>
      /// <param name="dbCrsr"></param>
      /// <param name="sqlda"></param>
      /// <returns>bool</returns>
      public bool BuildDbPos (DbPos dbPos, GatewayAdapterCursor dbCrsr, Sql3Sqldata sqlda)
      {
         GatewayCursor        crsr;
         SQL3Dbd              sql3Dbd;
         long                 dbPosBufSize;
         DataSourceDefinition dbh = dbCrsr.Definition.DataSourceDefinition;
         DBKey                posKey = null;
         DBSegment            seg = null;
         DBField              fld = null;
         int                  segIdx = 0;
         int                  pos = 0;
         short                slen = 0;
         int                  sqlvarKey = 0;
         List<Sql3SqlVar>     sqlvar = sqlda.SqlVars;
         bool                 isPosNull = true;

         GatewayCursorTbl.TryGetValue(dbCrsr, out crsr);
         DbdTbl.TryGetValue(dbh, out sql3Dbd);

         byte[] dbPosBuf = crsr.DbPosBuf;
         dbPosBufSize = sql3Dbd.posLen;

         Logger.Instance.WriteDevToLog("BuildDbPos(): >>>>>");

         if (sql3Dbd.IsView)
         {
            posKey = crsr.PosKey;

            for (segIdx = 0; segIdx < posKey.Segments.Count; segIdx++)
            {
               seg = posKey.Segments[segIdx];
               fld = seg.Field;

               if (Logger.Instance.LogLevel >= Logger.LogLevels.Development)
               {
                  Logger.Instance.WriteDevToLog(string.Format("BuildDbPos(): using sqlvar[{0}]", crsr.KeyArray[segIdx]));

                  if (fld.IsNumber())
                  {
                     if (fld.StorageFldSize() == 2)
                     {
                        Logger.Instance.WriteDevToLog(string.Format("BuildDbPos(): field = {0}, data = {1}", sqlvar[crsr.KeyArray[segIdx]].SqlName, short.Parse(sqlvar[crsr.KeyArray[segIdx]].SqlData.ToString())));
                     }
                     else if (fld.StorageFldSize() == 4)
                     {
                        Logger.Instance.WriteDevToLog(string.Format("BuildDbPos(): field = {0}, data = {1}", sqlvar[crsr.KeyArray[segIdx]].SqlName, long.Parse(sqlvar[crsr.KeyArray[segIdx]].SqlData.ToString())));
                     } 
                     else
                     {
                        Logger.Instance.WriteDevToLog(string.Format("BuildDbPos(): field = {0}, data = {1}", sqlvar[crsr.KeyArray[segIdx]].SqlName, sqlvar[crsr.KeyArray[segIdx]].SqlData.ToString()));
                     }
                  }
                  else
                  {
                     Logger.Instance.WriteDevToLog(string.Format("BuildDbPos(): field = {0}, data = {1}", sqlvar[crsr.KeyArray[segIdx]].SqlName, sqlvar[crsr.KeyArray[segIdx]].SqlData.ToString()));
                  }
               }

               /* if the field allow null & the field is null */
               if ((sqlvar[crsr.KeyArray[segIdx]].NullIndicator) == 1)
               {
                  slen = 0;

                  //convert short in bytes and copy in dbPos
                  CopyBytes(BitConverter.GetBytes(slen), dbPosBuf, pos);

                  pos += sizeof (short);
                  Logger.Instance.WriteDevToLog(string.Format("BuildDbPos(): NULL dbpos field {0}, slen = {1}", dbCrsr.GetFieldIndex(seg.Field), slen));
               }
               else
               {
                  isPosNull = false;
                  if (!fld.DefaultStorage)
                     slen = (short)seg.Field.Length;
                  else
                     slen = (short)fld.Length;

                  if ((fld.Storage == FldStorage.DateString && Sql3DateType(dbh, fld) != DateType.DATE_TO_SQLCHAR) ||
                        (fld.Storage == FldStorage.TimeString && Sql3DateType(dbh, fld) == DateType.DATE_TO_DATE))
                  {
                     if (fld.PartOfDateTime > 0 || (fld.DbType != null && fld.DbType == "DATETIME"))
                        slen = 19;
                     else
                        slen = 10;
                  }

                  //convert short in bytes and copy in dbPos
                  CopyBytes(BitConverter.GetBytes(slen), dbPosBuf, pos);

                  pos += sizeof (short);

                  // DB to Magic : pad the alpha fields (part of the key segments) with spaces. 
                  // Magic to DB : trim the field before sending it to database (insert/update)
                  if (sqlvar[crsr.KeyArray[segIdx]].SqlType == Sql3Type.SQL3TYPE_STR)
                  {
                     String str = (String)sqlvar[crsr.KeyArray[segIdx]].SqlData;
                     //convert string in bytes and copy in dbPos
                     CopyBytes(Encoding.ASCII.GetBytes(str), dbPosBuf, pos);
                  }
                  else if (fld.DbType.Contains("REAL") && fld.Length == 8)
                  {
                     double dbl = (float)sqlvar[crsr.KeyArray[segIdx]].SqlData;
                     //convert dbl in bytes
                     CopyBytes(BitConverter.GetBytes(dbl), dbPosBuf, pos);
                  }
                  else if (sqlvar[crsr.KeyArray[segIdx]].SqlType == Sql3Type.SQL3TYPE_DBTIME)
                  {
                     //Sql3DBTime ((string)sqlvar[crsr.KeyArray[seg_idx]].Sqldata, db_pos_buf + psize, db_pos_buf_size - psize);							
                  }
                  else
                  {
                     byte[] val = ConvertBytes(sqlvar[crsr.KeyArray[segIdx]].SqlData);
                     CopyBytes(val, dbPosBuf, pos);
                  }

                  
                  pos += slen /* + sizeof (short) */;
                  Logger.Instance.WriteDevToLog(string.Format("BuildDbPos(): NON NULL dbpos field %d, slen = %d", dbCrsr.GetFieldIndex(seg.Field), slen));
               }
            }

            /* --- */
            /*----------------------------------------------------*/
            /* Bugfix 298106 - When there no segment, db_pos_buf  */
            /* is NULL, so we need not copy it to slen_help       */
            /*----------------------------------------------------*/
            if (dbPosBuf != null)
            {
               //MEMCPY (&slen_help, sizeof(slen_help), db_pos_buf,sizeof(short));
            }

            dbPos.Set(dbPosBuf);
         }
         else
         {
            Int32 sql3RowId = 0;
            bool DbPosIsZero = false;

            if ((dbCrsr.CursorType == CursorType.PartOfJoin ||
               dbCrsr.CursorType == CursorType.MainPartOfJoin ||
               dbCrsr.CursorType == CursorType.PartOfOuter) &&
               crsr.KeyArray != null)
            {
               sqlvarKey = crsr.KeyArray[0];
               if ((sqlda.SqlVars[sqlvarKey].NullIndicator) == 1)
               {
                  //DB_POS_ZERO (db_pos);
                  DbPosIsZero = true;
               }
            }
            else 
               sqlvarKey = sqlda.Sqld - 1;

            if (!DbPosIsZero)
            {
               sql3RowId = (Int32)sqlda.SqlVars[sqlvarKey].SqlData;
               /* Using the last sqlvar of the output sqlda, the one storing the rowid */            
               
               int rowIdLen = SqliteConstants.SQL3_ROWID_LEN_EXTERNAL;

               //convert length of RowId in bytes
               pos = CopyBytes(BitConverter.GetBytes(rowIdLen), dbPosBuf, 0);
               
               //convert RowId in bytes
               CopyBytes(BitConverter.GetBytes(sql3RowId), dbPosBuf, pos);

               dbCrsr.Definition.CurrentPosition.Set(dbPosBuf);
               isPosNull = false;         

               dbPos.Set(dbPosBuf);
            }

            Logger.Instance.WriteDevToLog(string.Format("BuildDbPos(): rowid = {0}", crsr.Output.SqlVars[crsr.Output.Sqld - 1].SqlData));
         }

         Logger.Instance.WriteDevToLog("BuildDbPos(): <<<<<");

         return (isPosNull);
      }

      public int SQL3BuildWhereKeyWithValues (GatewayAdapterCursor db_crsr, DBKey key, string stmt, int stmtSizeInChars, int pos)
      {
         return 0;
      }

      /// <summary>
      /// SQL3DbposBuildFromBuf()
      /// </summary>
      /// <param name="dbPos"></param>
      /// <param name="db_crsr"></param>
      /// <param name="magicKey"></param>
      /// <param name="isInsert"></param>
      public void SQL3DbposBuildFromBuf (DbPos dbPos, GatewayAdapterCursor dbCrsr, long magicKey, bool isInsert )
      {
         GatewayCursor  crsr;
         SQL3Dbd        sql3Dbd ;
         long           dbPosBufSize;
         object         buf = null;
         DataSourceDefinition dbh = dbCrsr.Definition.DataSourceDefinition;
         DBKey          posKey = null;
         DBSegment      seg = null;
         DBField        fld = null;
         int            segIdx = 0;
         int            psize = 0;
         short          slen = 0;
         bool           dbCrsrField;
         string         type = string.Empty;
         DateType       dateType;
         int            sqlvarLen;
         Sql3Type       dataType;
         object         OldVal = string.Empty,
                        tmpBuf = string.Empty,
                        DiffVal = string.Empty;
         string         dateInfo = string.Empty;
         SQL3Cursor     sql3Cursor = null;
         string         dataTypeStr = string.Empty;
         TypeAffinity   typeAffinity = TypeAffinity.TYPE_AFFINITY_NONE;

         Logger.Instance.WriteDevToLog("DbposBuildFromBuf(): >>>>>");
         
         GatewayCursorTbl.TryGetValue(dbCrsr, out crsr);
         DbdTbl.TryGetValue(dbh, out sql3Dbd);

         byte[] dbPosBuf = crsr.DbPosBuf;
         dbPosBufSize = sql3Dbd.posLen;

         if (crsr.PosKey != null) // && pSQL3_dbd->type != MAGIC_SP_FILE &&        
         {
            if (sql3Dbd.IsView)
            {
               posKey = crsr.PosKey;


               /* build an array of offsets of the key segments into the output sqlda */
               if (crsr.KeyArray == null)
                  Sql3MakeDbpoSegArray(dbCrsr);

               for (segIdx = 0; segIdx < posKey.Segments.Count; segIdx++)
               {
                  seg = posKey.Segments[segIdx];
                  fld = seg.Field;

                  dbCrsrField = false;

                  if (Sql3IsDbCrsrField(dbCrsr, fld))
                  {
                     dbCrsrField = true;

                     /* If diffUpdate is TRUE for a seg-field, then copy the final value(X=X+diff) 
                        of a seg-field instead of diff value(diff) in db_pos. */
                     if (dbCrsr.Definition.DifferentialUpdate[crsr.KeyArray[segIdx]] && isInsert == false)
                     {
                        tmpBuf = dbCrsr.OldRecord.GetValue(crsr.KeyArray[segIdx]);
                        OldVal = tmpBuf;

                        tmpBuf = dbCrsr.CurrentRecord.GetValue(crsr.KeyArray[segIdx]);
                        DiffVal = tmpBuf;

                        Sql3GetType(dbh, fld, out type, out sqlvarLen, out dataType, out dateType, out dataTypeStr, false, true, out typeAffinity);

                        //TODO (Snehal): Handle it while handling of views.
                        //switch (datatype)
                        //{
                        //   case Sql3Type.SQL3TYPE_I2:
                        //      shrt   = *((short *)OldVal) + *((short *)DiffVal);
                        //      buf   = (Uchar *) &shrt;
                        //      break;
                        //   case Sql3Type.SQL3TYPE_I4:
                        //      lng   = *((long *)OldVal) + *((long *)DiffVal);
                        //      buf   = (Uchar *) &lng;
                        //      break;
                        //   case Sql3Type.SQL3TYPE_R4:
                        //      flt   = *((float *)OldVal) + *((float *)DiffVal);
                        //      buf   = (Uchar *) &flt;
                        //      break;
                        //   case Sql3Type.SQL3TYPE_R8:
                        //      dbl   = *((double *)OldVal) + *((double *)DiffVal);
                        //      buf   = (Uchar *) &dbl;
                        //      break;
                        //}


                     }
                     else
                     {
                        buf = dbCrsr.CurrentRecord.GetValue(crsr.KeyArray[segIdx]);
                     }
                  }
                  else
                  {
                     buf = crsr.Output.SqlVars[crsr.KeyArray[segIdx]].SqlData;
                  }

                  /* Bugfix 440028 - It was not put in this file. [ It was put is MS6p\ms6_l2p.cpp*/
                  /* If the seg is an IDENTITY column */
                  if ((seg.Field == sql3Dbd.identityFld) && isInsert)
                  {
                     Sql3GetNewIdentity(dbCrsr, (string)buf, seg.Field, seg.Field.Length);
                  }

                  if (Logger.Instance.LogLevel >= Logger.LogLevels.Development)
                  {
                     Logger.Instance.WriteDevToLog(string.Format("SQL3DbposBuildFromBuf(): using field {0}", dbCrsr.GetFieldIndex(seg.Field)));
                     if (fld.IsNumber())
                     {
                        if (fld.StorageFldSize() == 2)
                        {
                           short shortVal = short.Parse(buf.ToString());
                           Logger.Instance.WriteDevToLog(string.Format("SQL3DbposBuildFromBuf(): data = {0}", shortVal));
                        }
                        else
                        {
                           long longVal = long.Parse(buf.ToString());
                           Logger.Instance.WriteDevToLog(string.Format("SQL3DbposBuildFromBuf(): data = {0}", longVal));
                        }
                     }
                     else
                     {
                        Logger.Instance.WriteDevToLog(string.Format("SQL3DbposBuildFromBuf(): data = {0}", buf));
                     }
                  }

                  /* if the field allow null & the field is null */
                  /* TEMPLATE */
                  if (dbCrsrField && dbCrsr.CurrentRecord.IsNull(crsr.KeyArray[segIdx]))
                  {
                     slen = 0;
                     //convert short in bytes and copy in dbPos
                     CopyBytes(BitConverter.GetBytes(slen), dbPosBuf, psize);
                     psize += sizeof(short);

                     Logger.Instance.WriteDevToLog(string.Format("SQL3DbposBuildFromBuf(): NULL dbpos field {0}, slen = {1}, psize = {2}", crsr.KeyArray[segIdx], slen, psize));
                  }
                  else
                  {
                     /* TEMPLATE */
                     /* use the length indicator and field length */
                     if (!fld.DefaultStorage)
                        slen = (short)seg.Field.Length;
                     else
                        slen = (short)fld.Length;

                     if ((fld.Storage == FldStorage.DateString && Sql3DateType(dbh, fld) != DateType.DATE_TO_SQLCHAR) ||
                         (fld.Storage == FldStorage.TimeString && Sql3DateType(dbh, fld) == DateType.DATE_TO_DATE))
                     {
                        if (fld.PartOfDateTime > 0 || (fld.DbType != null && fld.DbType == "DATETIME"))
                           slen = 19;
                        else
                           slen = 10;

                        if (fld.Storage == FldStorage.DateString)
                           Sql3DateToInternal((string)buf, out dateInfo, fld.Length);
                        else
                           Sql3TimeToInternal((string)buf, out dateInfo);

                        buf = dateInfo;

                        CopyBytes(BitConverter.GetBytes(slen), dbPosBuf, psize);
                        psize += sizeof(short);

                        byte[] val = ConvertBytes(dateInfo);
                        CopyBytes(val, dbPosBuf, psize);

                     }
                     else if ((fld.Storage == FldStorage.TimeString) &&
                              (Sql3DateType(dbh, fld) == DateType.DATE_TO_DATE)) // MGTIME
                     {
                        Sql3DateToInternal((string)buf, out dateInfo, fld.Length);

                        slen = 19;

                        CopyBytes(BitConverter.GetBytes(slen), dbPosBuf, psize);
                        psize += sizeof(short);

                        byte[] val = ConvertBytes(dateInfo);
                        CopyBytes(val, dbPosBuf, psize);

                     }
                     else
                     {
                        if (fld.Storage == FldStorage.AlphaLString)
                           slen++;

                        byte[] val = ConvertBytes(slen);
                        CopyBytes(val, dbPosBuf, psize);

                        psize += sizeof(short);

                        // special treat to magickey in dbpos
                        if (Sql3FieldInfoFlag(dbh, seg.Field, "MAGICKEY"))
                        {
                           val = ConvertBytes(magicKey);
                           CopyBytes(val, dbPosBuf, psize);
                        }
                        else
                        {
                           val = ConvertBytes(buf);
                           CopyBytes(val, dbPosBuf, psize);
                        }
                     }

                     psize += slen;
                     Logger.Instance.WriteDevToLog(string.Format("SQL3DbposBuildFromBuf(): NON NULL dbpos field {0}, slen = {1}, psize = {2}", crsr.KeyArray[segIdx], slen, psize));
                  }
               }

               if (isInsert && crsr.CInsert != SqliteConstants.NULL_CURSOR)
               {
                  sql3Cursor = CursorTbl[crsr.CInsert];
               }

               /* --- */

               Logger.Instance.WriteDevToLog(string.Format("SQL3DbposBuildFromBuf(): Setting Dbpos,length - {0}", psize));

               dbPos.Set(dbPosBuf);
            }
            else
            {
               /* use the last sqlvar of the output sqlda, the one storing the rowid */
               Logger.Instance.WriteDevToLog("SQL3DbposBuildFromBuf(): THIS MESSAGE SHOULD NOT BE PRINTED");
            }
         }

         Logger.Instance.WriteDevToLog("SQL3DbposBuildFromBuf(): <<<<<");

         return;
      }

      /// <summary>
      /// Sql3GetDbCrsrIndex()
      /// </summary>
      /// <param name="db_crsr"></param>
      /// <param name="fldIdx"></param>
      /// <returns></returns>
      public int Sql3GetDbCrsrIndex (GatewayAdapterCursor dbCrsr, DBField fld)
      {
         int i = 0;

         Logger.Instance.WriteDevToLog(string.Format("Sql3GetDbCrsrIndex(): >>>>> fldName = {0}, dbCrsr.Definition.FieldsDefinition.Count = {1}", fld.DbName, dbCrsr.Definition.FieldsDefinition.Count));

         for (i = 0; i < dbCrsr.Definition.FieldsDefinition.Count; i++)
            if (dbCrsr.Definition.FieldsDefinition[i] == fld)
               break;

         if (i == dbCrsr.Definition.FieldsDefinition.Count)
            i = SqliteConstants.NULL_INDEX;

         Logger.Instance.WriteDevToLog(string.Format("Sql3GetDbCrsrIndex(): <<<<< returning  {0}", i));
         
         return i;
      }

      /// <summary>
      /// Sql3MakeDbpoSegArray()
      /// </summary>
      /// <param name="dbCrsr"></param>
      /// <returns></returns>
      public List<int> Sql3MakeDbpoSegArray (GatewayAdapterCursor dbCrsr)
      {
         GatewayCursor crsr;
         int segIdx = 0;

         GatewayCursorTbl.TryGetValue(dbCrsr, out crsr);

         DBKey PosKey = crsr.PosKey;
         int last = dbCrsr.Definition.FieldsDefinition.Count + crsr.XtraSortkeyCnt;

         Logger.Instance.WriteDevToLog(string.Format("Sql3MakeDbpoSegArray(): >>>>> segs - {0}", PosKey.Segments.Count));

         if (crsr.KeyArray == null)
         {
            crsr.KeyArray = new List<int>();
            for (segIdx = 0; segIdx < PosKey.Segments.Count; segIdx++)
            {
               DBSegment seg = PosKey.Segments[segIdx];
               DBField fld = seg.Field;
               if (Sql3IsDbCrsrField(dbCrsr, fld))
               {
                  crsr.KeyArray.Add(Sql3GetDbCrsrIndex(dbCrsr, fld));
               }
               else
               {
                  crsr.KeyArray.Add(last++);
               }
            }
         }

         Logger.Instance.WriteDevToLog("Sql3MakeDbpoSegArray(): <<<<< ");

         return crsr.KeyArray;
      }

      public int Sql3ResizeBufferForCrsr (GatewayAdapterCursor db_crsr)
      {
         return 0;
      }

      /// <summary>
      ///  Sql3FieldInfoFlag ()
      /// </summary>
      /// <param name="dbh"></param>
      /// <param name="fldIdx"></param>
      /// <param name="param"></param>
      /// <returns>bool</returns>
      public bool Sql3FieldInfoFlag (DataSourceDefinition dbh, DBField fld, string param)
      {
         bool returnValue = false;
         string infoVal;
         string paramValue;

         if (!string.IsNullOrEmpty(fld.DbInfo))
         {
            infoVal = fld.DbInfo;

            CheckDatabaseInfoFlag(infoVal, param, out paramValue);

            if (!string.IsNullOrEmpty(paramValue))
               if (paramValue == "Y" || paramValue == "y")
                  returnValue = true;
               else
                  returnValue = false;
         }

         return (returnValue);
      }

      /// <summary>
      ///  Sql3GetKeyForStartPos ()
      /// </summary>
      /// <param name="dbCrsr"></param>
      /// <param name="lastPos"></param>
      /// <returns>SQL3_CODE</returns>
      public SQL3_CODE Sql3GetKeyForStartPos(GatewayAdapterCursor dbCrsr, bool lastPos)
      {
         GatewayCursor  crsr;
         SQL3Dbd        sql3Dbd;
         SQL3_CODE      errCode = SqliteConstants.SQL3_OK;
         SQL3_CODE      retCode = SqliteConstants.RET_OK;
         SQL3Cursor     cursor;
         Sql3Stmt       sql3Stmt = null;
         DBKey          sortKey = null;
         DBKey          posKey = null;
         bool           iskeyUnique = false,
                        isKeyNullable = false;
         SQL3Connection connection = null;
         int            addPos = 0,
                        cnt = 0,
                        rngsCnt = 0,
                        poskeySegs = 0,
                        nullSegs = 0;
         int            crsrHdl;
         string         prefix = string.Empty;
         string         noPrefix = "";
         string         rowidStmt;


         GatewayCursorTbl.TryGetValue(dbCrsr, out crsr);
         DbdTbl.TryGetValue(dbCrsr.Definition.DataSourceDefinition, out sql3Dbd);

         connection = ConnectionTbl[sql3Dbd.DatabaseName];

         Logger.Instance.WriteDevToLog(string.Format("Sql3GetKeyForStartPos(): >>>>> database = %S, table name = %S, last_pos = %d",
                                       connection.DbName, sql3Dbd.FullName, lastPos));
         
         crsr.CGkey = Sql3CursorAlloc("Gkey", crsr.CGkey);

         cursor = CursorTbl[crsr.CGkey];

         if (crsr.PosKey != null)
         {
            posKey = crsr.PosKey;
            poskeySegs = crsr.PosKey.Segments.Count; 
         }

         if (dbCrsr.CursorType == CursorType.Join)
         {
            prefix += string.Format("{0}.", crsr.DbhPrefix[0]);
         }
         else
         {
            prefix += string.Format("{0}", noPrefix);
         }

         /* prepare a sqlda to receive the sort key data */
         if (crsr.Key != null)
         {
            crsr.Key.SQL3SqldaFree();
         }
         else
            crsr.Key = new Sql3Sqldata(this);

         if (dbCrsr.Definition.Key != null)
         {
            sortKey = dbCrsr.Definition.Key;
            iskeyUnique = sortKey.CheckMask(KeyMasks.UniqueKeyModeMask);

            isKeyNullable = CheckKeyNullable(dbCrsr, true);

            if (sql3Dbd.IsView == false)
            {
               if (iskeyUnique && (isKeyNullable == false))
               {
                  /* a sqlvar for each segment */
                  crsr.Key.SQL3SqldaAlloc(sortKey.Segments.Count);
               }
               else
               {
                  /* If Deferred Trans, dont include Rowid in wherer-clause for newly inserted rec. */
                  //if (db_crsr.Definition.StartPosition.trans_cache_pos)
                  //   crsr->key = SQL3SqldaAlloc(sortkey->segs);
                  //else
                  crsr.Key.SQL3SqldaAlloc(sortKey.Segments.Count + 1);
               }
            }
            else
            {
               // An extra sqlvar for the rowid
               // Startpos key will contain segments from sortkey and rowid
               // crsr->key = SQL3SqldaAlloc ((SINT)(sortkey->segs + 1));
               if ((iskeyUnique == true) && (isKeyNullable == false))
                  crsr.Key.SQL3SqldaAlloc(sortKey.Segments.Count);
               else
               {
                  if (iskeyUnique == false)
                  {
                     crsr.Key.SQL3SqldaAlloc(sortKey.Segments.Count + posKey.Segments.Count);
                     addPos = posKey.Segments.Count;
                  }
                  else
                  {
                     if (sortKey == posKey)
                        crsr.Key.SQL3SqldaAlloc(sortKey.Segments.Count);
                     else
                     {
                        crsr.Key.SQL3SqldaAlloc(sortKey.Segments.Count + posKey.Segments.Count);
                        addPos = posKey.Segments.Count;
                     }
                  }
               }
            }
         }
         else
         {
            crsr.Key.SQL3SqldaAlloc(posKey.Segments.Count);
            addPos = posKey.Segments.Count;
         }

         Logger.Instance.WriteDevToLog(string.Format("Sql3GetKeyForStartPos(): view = {0}, poskey = {1}, sortkey = {2}", sql3Dbd.IsView ? "TRUE" : "FALSE",
            posKey != null ? posKey.KeyDBName : "null", sortKey.KeyDBName));
         
         /* not a view where the sort and position keys are the same */
         if (!(sql3Dbd.IsView && (posKey == sortKey)) || (dbCrsr.Definition.Key != null))
         {
            /* update the sqlvar structures in the sqlda */
            SQL3SqldaGetKey(dbCrsr, sortKey, crsr.Key.SqlVars, false);

            /* build the key field list */
            BuildKeyFieldListStmt(dbCrsr, out Statement, sortKey, SqliteConstants.NO_PREFIX);
            crsr.StmtKeyFields = string.Empty;
            crsr.StmtKeyFields = Statement;

               crsr.SGKey = Sql3StmtAlloc("sGkey", crsr.SGKey, sql3Dbd.DatabaseName);
               sql3Stmt = StmtTbl[crsr.SGKey];

               /* prepare the statement */
               if (sql3Dbd.IsView)
               {
                  crsr.StmtWhereKey = string.Empty;
                  Statement = string.Empty;

                  if (lastPos)
                     cnt = BuildWhereViewStmt(dbCrsr, crsr.LastPos, 0);
                  else
                     cnt = BuildWhereViewStmt(dbCrsr, dbCrsr.Definition.StartPosition, 0);

                  crsr.StmtWhereKey = Statement;

                  Statement = string.Format("SELECT {0} FROM {1} WHERE", crsr.StmtKeyFields, crsr.StmtAllTables);

                  if (dbCrsr.CursorType == CursorType.Join && !string.IsNullOrEmpty(crsr.StmtJoinRanges))
                  {
                     Statement += string.Format(" {0} AND ({1})", crsr.StmtJoinRanges, crsr.StmtWhereKey);
                  }
                  else
                  {
                     Statement += string.Format(" ({0})", crsr.StmtWhereKey);
                  }

                  if (dbCrsr.CursorType == CursorType.Join && !string.IsNullOrEmpty(crsr.StmtJoinCond))
                  {
                     Statement += string.Format(" AND ({0})", crsr.StmtJoinCond);
                  }

            }
            else
            {
               rowidStmt = string.Format("{0}{1}", prefix, SqliteConstants.SQL3_ROWID_ST_A);

               if (dbCrsr.CursorType == CursorType.Join && !string.IsNullOrEmpty(crsr.StmtJoinCond))
               {
                  Statement = string.Format("SELECT {0} FROM {1} WHERE {2} = ? AND {3}", crsr.StmtKeyFields, crsr.StmtAllTables, rowidStmt, crsr.StmtJoinCond);
               }
               else
               {
                  Statement = string.Format("SELECT {0} FROM {1} WHERE {2} = ?", crsr.StmtKeyFields, crsr.StmtAllTables, rowidStmt);
               }

                  if (dbCrsr.CursorType == CursorType.Join && !string.IsNullOrEmpty(crsr.StmtJoinRanges))
                  {
                     Statement += string.Format(" AND ({0})", crsr.StmtJoinRanges);
                  }
                  cnt++;
               }

               sql3Stmt.Buf = Statement;

               cursor.StmtIdx = sql3Stmt.Idx;
               cursor.OutputSqlda = crsr.Key;

               /* it is better to use command here because */
               /* If Temp table , use Cursor by default */

               rngsCnt = crsr.JoinRngs;

            if (cursor.InputSqlda != null)
               cursor.InputSqlda.SQL3SqldaFree();
            else
               cursor.InputSqlda = new Sql3Sqldata(this);

               if (cnt + rngsCnt > 0)
               {
                  if (sql3Dbd.IsView)
                  {

                     /*If there are NULL segments in the dbpos then we do not build sqlvars for them.*/
                     nullSegs = Sql3NullSegsCountInDbpos(dbCrsr, crsr.LastPos);

                     if (lastPos)
                        poskeySegs = poskeySegs - nullSegs;
                     else
                        poskeySegs = poskeySegs - nullSegs;

                     cursor.InputSqlda.SQL3SqldaAlloc(poskeySegs + rngsCnt);

                     if (rngsCnt != 0)
                     {
                        crsrHdl = (crsr.OuterJoin) ? 0 : -1;
                        cnt = cursor.InputSqlda.SQL3SqldaRange(dbCrsr, SqliteConstants.ONLY_LINKS_RNG, false);
                     }

                     if (lastPos)
                     {
                        cnt = cursor.InputSqlda.SQL3SqldaFromDbpos(dbCrsr, rngsCnt, crsr.LastPos, false, false);
                     }
                     else
                     {
                        cnt = cursor.InputSqlda.SQL3SqldaFromDbpos(dbCrsr, rngsCnt, dbCrsr.Definition.StartPosition, false, false);

                     }
                     //assert (poskey_segs == cnt);
                  }
                  else
                  {
                     if (dbCrsr.CursorType == CursorType.Join && !string.IsNullOrEmpty(crsr.StmtJoinRanges))
                     {
                        cursor.InputSqlda.SQL3SqldaAlloc(1 + crsr.JoinRngs);

                        if (lastPos)
                        {
                           cnt = cursor.InputSqlda.SQL3SqldaFromDbpos(dbCrsr, 0, crsr.LastPos, false, false);
                        }
                        else
                        {
                           cnt = cursor.InputSqlda.SQL3SqldaFromDbpos(dbCrsr, 0, dbCrsr.Definition.StartPosition, false, false);
                        }

                        if (crsr.JoinRngs > 0)
                        {
                           cnt = cursor.InputSqlda.SQL3SqldaRange(dbCrsr, SqliteConstants.ONLY_LINKS_RNG, false);
                        }
                     }
                     else
                     {
                        cursor.InputSqlda.SQL3SqldaAlloc(rngsCnt + 1);

                        if (rngsCnt > 0)
                        {
                           cnt = cursor.InputSqlda.SQL3SqldaRange(dbCrsr, SqliteConstants.ONLY_LINKS_RNG, false);

                        }

                        if (lastPos)
                        {
                           cnt = cursor.InputSqlda.SQL3SqldaFromDbpos(dbCrsr, rngsCnt, crsr.LastPos, false, false);
                        }
                        else
                        {
                           cnt = cursor.InputSqlda.SQL3SqldaFromDbpos(dbCrsr, rngsCnt, dbCrsr.Definition.StartPosition, false, false);
                        }
                     }
                  }
               }

               //// For the stmt execution, skip the poskey fld (as we already have its value with us).
               if (addPos > 0)
               {
                  crsr.Key.Sqld = crsr.Key.Sqld - addPos;
                  crsr.Key.Sqln = crsr.Key.Sqln - addPos;
               }


               errCode = SQLiteLow.LibPrepareAndExecute(ConnectionTbl[sql3Dbd.DatabaseName], sql3Stmt, cursor);

               if (errCode == SqliteConstants.SQL3_OK)
               {
                  if (Logger.Instance.LogLevel >= Logger.LogLevels.Support)
                  {
                     SQL3StmtBuildWithValues(sql3Stmt.Buf, dbCrsr.Definition.DataSourceDefinition, cursor.InputSqlda, sql3Stmt, false);
                     Logger.Instance.WriteSupportToLog(string.Format("\tSTMT: {0}", sql3Stmt.StmtWithValues), true);
                  }
               }

               if (errCode == SqliteConstants.SQL3_OK)
               {
                  errCode = SQLiteLow.LibFetch(cursor, crsr.Key);

                  if (addPos > 0)
                  {
                     if (lastPos)
                     {
                        cnt = crsr.Key.SQL3SqldaFromDbpos(dbCrsr, crsr.Key.Sqld, crsr.LastPos, true, false);

                     }
                     else
                     {
                        cnt = crsr.Key.SQL3SqldaFromDbpos(dbCrsr, crsr.Key.Sqld, dbCrsr.Definition.StartPosition, true, false);
                     }
                  }

                  crsr.Key.Sqld = crsr.Key.Sqld + addPos;
                  crsr.Key.Sqln = crsr.Key.Sqln + addPos;

                  if (Logger.Instance.LogLevel >= Logger.LogLevels.Development && Logger.Instance.LogLevel != Logger.LogLevels.Basic)
                  {
                     if (errCode == SqliteConstants.SQL3_OK)
                        SQLLogging.SQL3LogSqlda(crsr.Key, "output sqlda for the sortkey");
                  }

               if (sql3Stmt.IsOpen)
               {
                  SQLiteLow.LibClose(ConnectionTbl[sql3Dbd.DatabaseName], sql3Stmt);
               }
            }
            if (cursor.InputSqlda != null)
               cursor.InputSqlda.SQL3SqldaFree();
               retCode = errCode;
            }
            else
            /* a view where the sort key is the same as the position key */
            {

               Logger.Instance.WriteDevToLog("Sql3GetKeyForStartPos(): view with poskey == sortkey");
               
               /* we already have the necessary values in the dbpos */

               /* we already have the necessary values in the dbpos */
               if (lastPos)
               {
                  crsr.Key.Sqld = crsr.Key.SQL3SqldaFromDbpos(dbCrsr, 0, crsr.LastPos, true, false);
               }
               else
               {
                  crsr.Key.Sqld = crsr.Key.SQL3SqldaFromDbpos(dbCrsr, 0, dbCrsr.Definition.StartPosition, true, false);
               }
            }

         if (Logger.Instance.LogLevel >= Logger.LogLevels.Development && Logger.Instance.LogLevel != Logger.LogLevels.Basic)
         {
            if (errCode == SqliteConstants.SQL3_OK)
            {
               SQLLogging.SQL3LogSqlda(crsr.Key, "output sqlda for the sortkey");
            }
         }

         Logger.Instance.WriteDevToLog(string.Format("Sql3GetKeyForStartPos(): <<<<< retcode = {0}", retCode));

         return (retCode);
      }


      public int SQL3StartposCountParams (GatewayAdapterCursor dbCrsr, int endvar)
      {
         GatewayCursor crsr;

         GatewayCursorTbl.TryGetValue(dbCrsr, out crsr);
         int cnt = crsr.Rngs + crsr.SqlRngs,
                        i = 0,
                        j = 0;

         Logger.Instance.WriteDevToLog(string.Format("SQL3StartposCountParams(): >>>>> endvar = {0}", endvar));

         for (i = 0; j < endvar; i++)
         {
            if (crsr.Key.SqlVars[i].PartOfDateTime != SqliteConstants.TIME_OF_DATETIME)
               j++;
            if (crsr.Key.SqlVars[i].NullIndicator != 1)
               cnt++;
         }

         if (endvar > 0)
         {
            if (i < crsr.Key.Sqld && crsr.Key.SqlVars[i].PartOfDateTime == SqliteConstants.TIME_OF_DATETIME &&
                crsr.Key.SqlVars[i].NullIndicator != 1) /*if time is last, add it*/
               cnt++;
         }

         Logger.Instance.WriteDevToLog(string.Format("SQL3StartposCountParams(): <<<<< cnt = {0}", cnt));
         
         return (cnt);
      }

      /// <summary>
      /// Sql3GetUniqueKey()
      /// </summary>
      /// <returns></returns>
      public long Sql3GetUniqueKey()
      {
         long SecondsFromMidnight;
         long NewUniqueKey;

         Logger.Instance.WriteDevToLog("Sql3GetUniqueKey(): >>>>> ");

         SecondsFromMidnight = 3600 * (DateTime.Today.Hour) + 60 * (DateTime.Today.Minute) + DateTime.Today.Second;
         NewUniqueKey = 32768 * (SecondsFromMidnight - 43200) + new Random().Next();

         Logger.Instance.WriteDevToLog(string.Format("Sql3GetUniqueKey(): <<<<< Unique key - {0}", NewUniqueKey));

         return NewUniqueKey;
      }


      /// <summary>
      /// 
      /// </summary>
      /// <param name="connection"></param>
      /// <param name="objectName"></param>
      /// <returns></returns>
      public SQL3_CODE Sql3DropObject (SQL3Connection connection, string objectName)
      {
         string  tableName = string.Empty;
         TableType objectType = TableType.Undefined;

         SQL3_CODE errorCode = SqliteConstants.SQL3_OK;

         Logger.Instance.WriteDevToLog(string.Format("Sql3DropObject(): >>>>> object name is {0} ", objectName));

         Sql3SeperateTable(objectName, out tableName);

         errorCode = SQLiteLow.LibTableType(connection, tableName, out objectType);


         if (errorCode == SqliteConstants.SQLITE_OK)
         {
            Logger.Instance.WriteDevToLog(string.Format("Sql3DropObject(): object type is {0} ", objectType));

            if (objectType == TableType.Table)
            {
               errorCode = SQLiteLow.LibDrop(connection, tableName, DropObject.SQL3_DROP_TABLE);
            }
         }
         else
         {
            Logger.Instance.WriteDevToLog("Sql3DropObject(): object does not exist");
         }

         Logger.Instance.WriteDevToLog(string.Format("Sql3DropObject(): <<<<< errorCode = {0}", errorCode));

         return errorCode;
      }

      /// <summary>
      ///  SQL3CheckIdentityField ()
      /// </summary>
      /// <param name="sql3Dbd"></param>
      /// <returns>bool</returns>
      public bool SQL3CheckIdentityField (SQL3Dbd sql3Dbd)
      {
         for (int idx = 0; idx < sql3Dbd.DataSourceDefinition.Fields.Count; idx++)
         {
            if (Sql3CheckDbtype(sql3Dbd.DataSourceDefinition.Fields[idx], SqliteConstants.IDENTITY_STR))
            {
               sql3Dbd.identityFld = sql3Dbd.DataSourceDefinition.Fields[idx];
               break;
            }
         }

         return (sql3Dbd.identityFld != null ? true : false);
      }

      /// <summary>
      /// Get the table's full name.
      /// </summary>
      /// <param name="fullTableName"></param>
      /// <param name="tableName"></param>
      public void Sql3SeperateTable (string fullTableName, out string tableName)
      {
         tableName = fullTableName;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="info"></param>
      /// <param name="infoLen"></param>
      /// <param name="keyword"></param>
      public void Sql3GetDbInfoStrValue (List<string>info, int infoLen, string keyword)
      {

      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="dbCrsr"></param>
      /// <param name="pSQL3Stmt"></param>
      /// <param name="sqlda"></param>
      /// <param name="uniqueKey"></param>
      /// <returns></returns>
      public SQL3_CODE Sql3InsertValuesAndExec (GatewayAdapterCursor dbCrsr, Sql3Stmt pSQL3Stmt, Sql3Sqldata sqlda, out long uniqueKey)
      {
         SQL3_CODE            returnCode;
         DataSourceDefinition dbh = dbCrsr.Definition.DataSourceDefinition;
         SQL3Dbd              sql3Dbd;
         DBField              fld;
         int                  idx = 0,
                              timeIdx;
         List<DBField>        fldInfo = dbCrsr.Definition.FieldsDefinition;
         string               buf = string.Empty;
         string               fullDate = string.Empty;
         Sql3SqlVar           sqlvar = null;
         short                varIdx = 0;
         SQL3Connection       sql3Connection = null;
         int                  uniqueKeyVarIdx = -1;
         bool                 isMinRange = false,
                              identityColumn = false;
         int                  noOfUpdatedRecords  = 0;
         
         uniqueKey = 0;

         DbdTbl.TryGetValue(dbh, out sql3Dbd);

         Logger.Instance.WriteDevToLog("Sql3InsertValuesAndExec(): >>>>>");

         sql3Connection = ConnectionTbl[sql3Dbd.DatabaseName];

         for (idx = 0; idx < (int)dbCrsr.Definition.FieldsDefinition.Count; idx++)
         {
            fld = dbCrsr.Definition.FieldsDefinition[idx];

            if (fld.Storage == FldStorage.TimeString &&
                fld.PartOfDateTime != 0)
               continue;

            /* not to put IDENTITY column value into INSERT statement */
            if (Sql3CheckDbtype(dbh, fld, SqliteConstants.IDENTITY_STR))
            {
               identityColumn = true;
               continue;
            }

            /*Don't put values of flds in INSERT stmt, if fld_update is FALSE for that fld.*/
            if (!dbCrsr.Definition.IsFieldUpdated[idx])
               continue;

            // Skip TIMESTAMP fld in an Insert stmt as we cannot insert an explicit value into it.
            if (Sql3CheckDbtype(dbh, fld, "TIMESTAMP"))
               continue;

            sqlvar = sqlda.SqlVars[varIdx++];
            if (Sql3FieldInfoFlag(dbh, fld, "MAGICKEY"))
            {
               uniqueKeyVarIdx = varIdx - 1;
               uniqueKey = Sql3GetUniqueKey();
            }
            else
            {
               if (!dbCrsr.CurrentRecord.IsNull(idx))
               {
                  if ((fld.Storage == FldStorage.DateString && Sql3DateType(dbh, fld) != DateType.DATE_TO_SQLCHAR) ||
                      (fld.Storage == FldStorage.TimeString && Sql3DateType(dbh, fld) == DateType.DATE_TO_DATE))
                  {
                     if (fld.PartOfDateTime != 0)
                     {
                        for (timeIdx = 0; timeIdx < dbCrsr.Definition.FieldsDefinition.Count; timeIdx++)
                        {
                           if (dbh.Fields[dbCrsr.Definition.FieldsDefinition[timeIdx].IndexInRecord].Isn == fld.PartOfDateTime)
                              break;
                        }
                        Sql3DateTimeToInternal((string)dbCrsr.CurrentRecord.GetValue(idx),
                                               (string)dbCrsr.CurrentRecord.GetValue(timeIdx), out fullDate, fld.Length);
                     }
                     else
                     {
                        if (fld.Storage == FldStorage.TimeString)
                        {
                           Sql3TimeToInternal((string)dbCrsr.CurrentRecord.GetValue(idx), out fullDate);
                        }
                        else
                        {
                           Sql3DateToInternal((string)dbCrsr.CurrentRecord.GetValue(idx), out fullDate, fld.Length);
                        }
                     }

                     sqlvar.SqlData = fullDate;
                  }
                  else if (fld.IsBlob())
                  {
                     GatewayBlob pblob_type;
                     pblob_type = (GatewayBlob)dbCrsr.CurrentRecord.GetValue(idx);
                     sqlvar.SqlData = pblob_type.Blob;
                  }
                  else
                  {
                     if (sqlvar.SqlType == Sql3Type.SQL3TYPE_DBTIME)
                        isMinRange = true;
                     else
                        isMinRange = false;

                     Sql3AddValSqldata(out sqlvar.SqlData, sqlvar.SqlLen, sqlvar.SqlType, sqlvar.SqlLen, fld,
                                          dbCrsr.CurrentRecord.GetValue(idx).ToString(), SqliteConstants.QUOTES_TRUNC, isMinRange);
                  }
               }
            }
         }

         ServerErrCode = SqliteConstants.SQL3_OK;

         returnCode = SQLiteLow.LibExecuteWithParams(sqlda, pSQL3Stmt, ConnectionTbl[sql3Dbd.DatabaseName], out noOfUpdatedRecords, DatabaseOperations.Insert);

         //Execute the statement. 
         //On successful execution SQLITE_DONE will be returned.
#if SQLITE_CIPHER_CPP_GATEWAY
         if (returnCode != (int)SQLiteErrorCode.Done)
#else
         if (returnCode != (int)SQLiteErrorCode.Ok)
#endif
         {
            Logger.Instance.WriteDevToLog("Sql3InsertValuesAndExec(): sqlite3_step() FAILED");

            SQLiteLow.LibErrorhandler (sql3Connection);
            return (returnCode);
         }

#if SQLITE_CIPHER_CPP_GATEWAY
         if (identityColumn && returnCode == (int)SQLiteErrorCode.Done)
            returnCode = (int)GatewayErrorCode.FilterAfterInsert;
#else
         if (identityColumn && returnCode == (int)SQLiteErrorCode.Ok)
            returnCode = (int)GatewayErrorCode.FilterAfterInsert;
#endif

         Logger.Instance.WriteDevToLog("Sql3InsertValuesAndExec(): <<<<<");

         return returnCode;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="stmt"></param>
      /// <param name="dbh"></param>
      /// <param name="datatype"></param>
      /// <param name="fld"></param>
      /// <param name="val"></param>
      /// <param name="qoutes"></param>
      /// <param name="time_val"></param>
      /// <returns></returns>
      public void Sql3AddVal (ref string stmt, DataSourceDefinition dbh, Sql3Type datatype, DBField fld, string val, int qoutes, string time_val )
      {
         string type;
         DateType date_type = DateType.NORMAL_TYPE;
         int sqlvar_len;
         char tiny;
         long len = 0;
         double dbl;
         string tmp_date = string.Empty;
         string str_date = string.Empty;
         string db_type;
         double max_val_for_digits = 0;
         int digits = 0;
         string numeric; // 1 for the Decimal place + 1 for '\0'
         string db_type_upper;
         int rowid;
         string dataTypeString;
         TypeAffinity typeAffinity;

         Logger.Instance.WriteDevToLog("Sql3AddVal(): >>>>>");

         if (fld != null)
         {
            len = fld.StorageFldSize();

            if (fld.IsUnicode())
               len = len / 2;

            if (fld.Storage == FldStorage.AlphaZString ||
                  fld.Storage == FldStorage.UnicodeZString)
               len--;

            if (datatype == Sql3Type.SQL3TYPE_EMPTY)
            {
               Sql3GetType(dbh, fld, out type, out sqlvar_len, out datatype, out date_type, out dataTypeString, false, false, out typeAffinity);
            }

            Logger.Instance.WriteDevToLog(string.Format("Sql3AddVal(): SQL3 type field = {0}, field length = {1}, datatype - {2}", fld.Isn, len, datatype));
         }

         switch (datatype)
         {
            case Sql3Type.SQL3TYPE_BOOL:
               stmt += (val == "1" ? "true" : "false");
               break;
            //case SQL3TYPE_BYTES:
            //     if (fld->Storage() == STORAGE_MEMO_STRING ||
            //         fld->Storage() == STORAGE_MEMO_MAGIC)
            //   {
            //      actual_len = sql3_varbinary_to_str (stmt + pos, StmtSizeInChars - pos, (Uchar *)val, len);
            //      pos += 2 + actual_len*2;
            //   }
            //   else
            //   {
            //      sql3_binary_to_str (stmt + pos, StmtSizeInChars - pos, (Uchar *)val, len);
            //      pos += 2 + len*2;
            //   }
            //   break;
            case Sql3Type.SQL3TYPE_I2:
               stmt += short.Parse(val);
               break;

            case Sql3Type.SQL3TYPE_I4:
               stmt += long.Parse(val);
               break;

            case Sql3Type.SQL3TYPE_R4:
               stmt += float.Parse(val);
               break;
            case Sql3Type.SQL3TYPE_R8:
               dbl = Double.Parse((string)val);
               db_type = fld.DbType;
               if (!string.IsNullOrEmpty(db_type))
               {
                  db_type_upper = db_type.ToUpper();

                  if (db_type_upper.Contains("NUMERIC") || db_type_upper.Contains("DECIMAL"))
                  {
                     for (digits = 0; digits < fld.Whole; digits++)
                        max_val_for_digits = max_val_for_digits * 10 + 9;

                     double divisor = 10;
                     for (digits = 0; digits < fld.Dec; digits++, divisor *= 10)
                        max_val_for_digits += (double)9 / divisor;

                     if (dbl > max_val_for_digits)
                        dbl = max_val_for_digits;
                  }
               }

               stmt += dbl;
               break;

            /*------------------------------------------------------------------------*/
            /* Ambica : BugFix # 759832 : Decimal Data Type used , If Storage is      */
            /*                            NUMERIC_STRING & Decimal used                 */
            /*------------------------------------------------------------------------*/
            case Sql3Type.SQL3TYPE_DECIMAL:
            case Sql3Type.SQL3TYPE_NUMERIC:
               numeric = string.Empty;
               numeric = (string)val;
               numeric = numeric.Trim();
               len = numeric.Length;
               db_type = fld.DbType;

               if (!string.IsNullOrEmpty(db_type))
               {
                  db_type_upper = db_type.ToUpper();

                  if (db_type_upper.Contains("NUMERIC") ||
                        db_type_upper.Contains("DECIMAL"))
                  {
                     string max_numeric = string.Empty; // 1 for the Decimal place + 1 for '\0'
                     max_numeric.PadRight(fld.Whole + fld.Dec + 1, '9');

                     char[] charArr = max_numeric.ToCharArray();
                     charArr[fld.Whole] = '.';
                     max_numeric = charArr.ToString();

                     if ((numeric.CompareTo(max_numeric) > 0)
                        || (fld.Dec == 0 && numeric.Length > max_numeric.Length))
                     {
                        numeric = max_numeric;
                     }
                  }
               }

               stmt += numeric;
               break;
            /*------------------------------------------------------------*/
            case Sql3Type.SQL3TYPE_UI1:
               tiny = char.Parse(val);
               stmt += tiny;
               break;

            case Sql3Type.SQL3TYPE_STR:
               switch (qoutes)
               {
                  case SqliteConstants.QUOTES:
                  case SqliteConstants.QUOTES_TRUNC:
                     if (qoutes == SqliteConstants.QUOTES_TRUNC)
                     {
                        val = val.Trim();
                        len = val.Length;
                     }

                     db_type = fld.DbType; ;
                     if (!string.IsNullOrEmpty(db_type))
                     {
                        db_type_upper = db_type.ToUpper();
                     }

                     stmt += "\'";
                     stmt += val;
                     stmt += "\'";

                     break;

                  case SqliteConstants.NO_QUOTES:
                  case SqliteConstants.NO_QUOTES_TRUNC:
                     if (qoutes == SqliteConstants.NO_QUOTES_TRUNC)
                     {
                        val = val.Trim();
                        len = val.Length;
                     }

                     stmt += val;
                     stmt += "";
                     break;
               }
               break;

            case Sql3Type.SQL3TYPE_WSTR:
            case Sql3Type.SQL3TYPE_BSTR:
               switch (qoutes)
               {
                  case SqliteConstants.QUOTES:
                  case SqliteConstants.QUOTES_TRUNC:
                     if (qoutes == SqliteConstants.QUOTES_TRUNC)
                     {
                        val = val.Trim();
                        len = val.Length;
                     }

                     stmt += "\'";
                     stmt += val;
                     stmt += "\'";

                     break;

                  case SqliteConstants.NO_QUOTES:
                  case SqliteConstants.NO_QUOTES_TRUNC:
                     if (qoutes == SqliteConstants.NO_QUOTES_TRUNC)
                     {
                        val = val.Trim();
                        len = val.Length;
                     }

                     stmt += val;
                     stmt += "";
                     break;
               }
               break;

            case Sql3Type.SQL3TYPE_DBTIMESTAMP:
               if (fld.Storage == FldStorage.DateString)
               {
                  if (fld.PartOfDateTime != SqliteConstants.NORMAL_OF_DATETIME)
                  {
                     SQLiteLow.LibDateCrack(val, out str_date, str_date.Length, 19, time_val);
                  }
                  else
                  {
                     SQLiteLow.LibDateCrack(val, out str_date, str_date.Length, fld.StorageFldSize(), time_val);
                  }

                  if ((date_type != DateType.DATE_TO_SQLCHAR) && (time_val == null))
                  {
                     if (fld.PartOfDateTime == SqliteConstants.NORMAL_OF_DATETIME)
                     {
                        tmp_date = string.Format("\'{0}-{1}-{2}\'", str_date.Substring(0, 4), str_date.Substring(5, 2), str_date.Substring(8, 2));
                     }
                     else
                     {
                        tmp_date = string.Format("\'{0}-{1}-{2} {3}:{4}:{5}\'", str_date.Substring(0, 4), str_date.Substring(5, 2), str_date.Substring(8, 2),
                                                                                str_date.Substring(11, 2), str_date.Substring(14, 2), str_date.Substring(17, 2));
                     }

                     stmt += tmp_date;
                  }
                  else
                  {
                     stmt += "\'";
                     stmt += str_date;
                     stmt += "\'";
                  }
               }
               else if (fld.Storage == FldStorage.TimeString)
               {
                  SQLiteLow.LibDateCrack(val, out str_date, str_date.Length, 19, time_val);
                  tmp_date = string.Format("\'2000-01-01 {0}:{1}:{2}\'", str_date.Substring(11, 2), str_date.Substring(14, 2), str_date.Substring(17, 2));
                  stmt += tmp_date;
               }
               else
               {
                  stmt += "\'";
                  stmt += val;
                  stmt += "\'";
               }
               break;

            case Sql3Type.SQL3TYPE_DBDATE:
               if (fld.Storage == FldStorage.DateString)
               {
                  SQLiteLow.LibDateCrack(val, out str_date, str_date.Length, 10, time_val);
                  tmp_date = string.Format("\'{0}-{1}-{2}\'", str_date.Substring(0, 4), str_date.Substring(5, 2), str_date.Substring(8, 2));
                  stmt += tmp_date;
               }
               break;

            case Sql3Type.SQL3TYPE_DBTIME:
               if (fld.Storage == FldStorage.TimeString)
               {
                  stmt += string.Format("\'{0}\'", val);
               }
               break;

            case Sql3Type.SQL3TYPE_ROWID:
               rowid = int.Parse(val);
               stmt += rowid;
               break;

            default:
               break;
         }

         Logger.Instance.WriteDevToLog("Sql3AddVal(): <<<<<");
      }

      /// <summary>
      /// Sql3_add_val_sqldata()
      /// </summary>
      /// <param name="sqldata"></param>
      /// <param name="sqldatasize"></param>
      /// <param name="dbd"></param>
      /// <param name="datatype"></param>
      /// <param name="datalen"></param>
      /// <param name="fld_idx"></param>
      /// <param name="val"></param>
      /// <param name="quotes"></param>
      /// <param name="isMinRange"></param>
      /// <returns></returns>
      public int Sql3AddValSqldata(out object sqldata, int sqldatasize, Sql3Type datatype, int datalen, 
                                       DBField fld, object val, int quotes, bool isMinRange)
      {
         long     len;
         int      actualLen         = 0;
         double   dbl;
         string   dbType            = string.Empty;
         double   maxValForDigits   = 0;
         int      digits            = 0;
         string   numeric           = string.Empty; // 1 for the Decimal place + 1 for '\0'
         string   dbTypeUpper       = string.Empty;

         Logger.Instance.WriteDevToLog("Sql3AddValSqldata(): >>>>>");

         Logger.Instance.WriteDevToLog(string.Format("Sql3AddValSqldata(): SQL3 type field Name = {0}, field length = {1}, datatype - {2}", fld.DbName, datalen, datatype));
         
         sqldata = string.Empty;

         switch (datatype)
         {
            case Sql3Type.SQL3TYPE_BOOL:
               sqldata = val;
               break;

            case Sql3Type.SQL3TYPE_BYTES:
               sqldata = val;
               actualLen = datalen;
               break;

            case Sql3Type.SQL3TYPE_I2:
            case Sql3Type.SQL3TYPE_I4:
            case Sql3Type.SQL3TYPE_I8:
            case Sql3Type.SQL3TYPE_ROWID:
               sqldata = val;
               break;
            case Sql3Type.SQL3TYPE_R4:
               sqldata = val;
               break;
            case Sql3Type.SQL3TYPE_R8:
               dbl = Double.Parse((string)val);
               dbType = fld.DbType;
               if (!string.IsNullOrEmpty(dbType))
               {
                  dbTypeUpper = dbType.ToUpper();

                  if (dbTypeUpper.Contains("NUMERIC") || dbTypeUpper.Contains("DECIMAL"))
                  {
                     for (digits = 0; digits < fld.Whole; digits++)
                        maxValForDigits = maxValForDigits * 10 + 9;

                     double divisor = 10;
                     for (digits = 0; digits < fld.Dec; digits++, divisor *= 10)
                        maxValForDigits += (double)9 / divisor;

                     if (dbl > maxValForDigits)
                        dbl = maxValForDigits;
                  }
               }

               sqldata = dbl.ToString();
               break;

            /*------------------------------------------------------------------------*/
            /* Ambica : BugFix # 759832 : Decimal Data Type used , If Storage is      */
            /*                            NUMERIC_STRING & Decimal used                 */
            /*------------------------------------------------------------------------*/
            case Sql3Type.SQL3TYPE_DECIMAL:
            case Sql3Type.SQL3TYPE_NUMERIC:
               datalen--;
               numeric = string.Empty;
               numeric = (string)val;
               numeric = numeric.Trim();
               len = numeric.Length;
               dbType = fld.DbType;

               if (!string.IsNullOrEmpty(dbType))
               {
                  dbTypeUpper = dbType.ToUpper();

                  if (dbTypeUpper.Contains("NUMERIC") ||
                      dbTypeUpper.Contains("DECIMAL"))
                  {
                     string max_numeric = string.Empty; // 1 for the Decimal place + 1 for '\0'
                     max_numeric.PadRight(fld.Whole + fld.Dec + 1, '9');

                     char[] charArr = max_numeric.ToCharArray();
                     charArr[fld.Whole] = '.';
                     max_numeric = charArr.ToString();

                     if ((numeric.CompareTo(max_numeric) > 0)
                        || (fld.Dec == 0 && numeric.Length > max_numeric.Length))
                     {
                        numeric = max_numeric;
                     }
                  }
               }

               sqldata = string.Format("{0}", numeric);
               actualLen = numeric.Length;
               break;

            case Sql3Type.SQL3TYPE_UI1:
               sqldata = val;
               break;

            case Sql3Type.SQL3TYPE_STR:
               datalen--;
               if (fld.Storage != FldStorage.NumericFloat)
               {
                  switch (quotes)
                  {
                     case SqliteConstants.QUOTES:
                     case SqliteConstants.QUOTES_TRUNC:
                        sqldata = val;
                        if (quotes == SqliteConstants.QUOTES_TRUNC)
                        {
                           if (fld.Attr == (char)StorageAttributeType.Numeric && fld.DataSourceDefinition == DatabaseDefinitionType.Normal)
                           {
                              sqldata = ((string)sqldata).Trim();
                           }
                           else if (fld.Attr == (char)StorageAttributeType.Unicode)
                           {
                              //TODO (snehal)
                              //trunc_len = WstrTrimLen((Wchar *)val, datalen / sizeof(Wchar));
                              //datalen = trunc_len;
                              //sqldata[trunc_len] = CHAR_U('\0');
                           }
                           else
                           {
                              val = ((string)val).Trim();
                              datalen = ((string)val).Length;
                           }
                        }
                        break;
                     case SqliteConstants.NO_QUOTES:
                     case SqliteConstants.NO_QUOTES_TRUNC:
                        sqldata = val;

                        if (quotes == SqliteConstants.NO_QUOTES_TRUNC)
                        {
                           if (fld.Attr == (char)StorageAttributeType.Unicode)
                           {
                              //trunc_len = WstrTrimLen((Wchar *)val, datalen / sizeof(Wchar));
                              //datalen = trunc_len;
                              //sqldata[trunc_len] = CHAR_U('\0');
                           }
                           else
                           {
                              val = ((string)val).Trim();
                              datalen = ((string)val).Length;
                           }
                        }
                        break;
                  }
                  actualLen = datalen;
               }
               else
               {
                  if (isMinRange)
                  {
                     string min_numeric = string.Empty; // 1 for the Decimal place + 1 for '\0'         
                     min_numeric.PadLeft(fld.Whole + fld.Dec + 1, '9');
                     char[] charArray = min_numeric.ToCharArray();
                     charArray[0] = '-';
                     min_numeric = charArray.ToString();


                     charArray = min_numeric.ToCharArray();
                     charArray[fld.Whole + 1] = '.';

                     min_numeric = charArray.ToString();

                     double min_dbl = double.Parse(min_numeric);

                     // We map a Float storage field which is mapped to DECIMAL/NUMERIC as a CHAR, 
                     // and we retrieve it as a FLOAT.
                     if (fld.Length == 8)
                     {
                        if (double.Parse(((string)val)) < min_dbl)
                        {
                           sqldata = string.Format("{0}", min_numeric);
                           actualLen = sqldata.ToString().Length;
                        }
                        else
                        {
                           sqldata = val;
                        }
                     }
                     else
                     {
                        if (float.Parse(((string)val)) < min_dbl)
                        {
                           sqldata = string.Format("{0}", min_numeric);
                           actualLen = min_numeric.Length;
                        }
                        else
                        {
                           sqldata = val;
                           actualLen = sqldata.ToString().Length;
                        }
                     }
                  }
                  else
                  {
                     string max_numeric; // 1 for the Decimal place + 1 for '\0'
                     max_numeric = string.Empty;
                     max_numeric.PadLeft(fld.Whole + fld.Dec + 1, '9');


                     char[] charArray = max_numeric.ToCharArray();
                     charArray[fld.Whole] = '.';

                     max_numeric = charArray.ToString();

                     double max_dbl = double.Parse(max_numeric);

                     // We map a Float storage field which is mapped to DECIMAL/NUMERIC as a CHAR, 
                     // and we retrieve it as a FLOAT.
                     if (fld.Length == 8)
                     {
                        if (double.Parse(((string)val)) > max_dbl)
                        {
                           sqldata = string.Format("{0}", max_numeric);
                           actualLen = sqldata.ToString().Length;
                        }
                        else
                        {
                           sqldata = val;
                           actualLen = sqldata.ToString().Length;
                        }
                     }
                     else
                     {
                        if (float.Parse(((string)val)) > max_dbl)
                        {
                           sqldata = string.Format("{0}", max_numeric);
                           actualLen = sqldata.ToString().Length;
                        }
                        else
                        {
                           sqldata = val;
                           actualLen = sqldata.ToString().Length;
                        }
                     }
                  }
               }
               break;

            case Sql3Type.SQL3TYPE_WSTR:
               datalen -= 2;

               switch (quotes)
               {
                  case SqliteConstants.QUOTES:
                  case SqliteConstants.QUOTES_TRUNC:
                     sqldata = val;
                     if (quotes == SqliteConstants.QUOTES_TRUNC)
                     {
                        //trunc_len = WstrTrimLen((Wchar *)val, datalen / sizeof(Wchar));
                        //datalen = trunc_len * sizeof (Wchar);
                        //((Wchar *)sqldata)[trunc_len] = CHAR_U('\0');
                     }
                     break;
                  case SqliteConstants.NO_QUOTES:
                  case SqliteConstants.NO_QUOTES_TRUNC:
                     sqldata = val;
                     if (quotes == SqliteConstants.NO_QUOTES_TRUNC)
                     {
                        //trunc_len = WstrTrimLen((Wchar *)val, datalen / sizeof(Wchar));
                        //datalen = trunc_len * sizeof (Wchar);
                        //((Wchar *)sqldata)[trunc_len] = CHAR_U('\0');
                     }
                     break;
               }
               actualLen = datalen;
               break;

            case Sql3Type.SQL3TYPE_DBTIMESTAMP:
               if (fld.Storage == FldStorage.DateString || fld.Storage == FldStorage.TimeString)
               {
                  sqldata = val;
               }
               break;

            case Sql3Type.SQL3TYPE_DBDATE:
               sqldata = val;
               actualLen = datalen;
               break;

            case Sql3Type.SQL3TYPE_DBTIME:
               sqldata = string.Empty;

               Sql3Time(((string)val), isMinRange, sqldata.ToString(), datalen);
               actualLen = SqliteConstants.SQL3_TIME_LEN;
               break;

            default:
               break;
         }

         Logger.Instance.WriteDevToLog("Sql3AddValSqldata(): <<<<<");

         return actualLen;

      }

      public void Sql3GetFullName (out string fullName, int fullNameSize, DataSourceDefinition dbh, DatabaseDefinition dbDefinition, string fname)
      {
         fullName = string.Empty;
         fullName = string.Concat(fullName, fname);
      }

      public int Sql3DuplicateApos (string stmt, int stmtSizeInChars, string val, int valLen, char dupliChar)
      {
         return 0;
      }

      public void Sql3ConvertToFullate(string outDate, int outDateSize, string inDate, int len, bool until)
      {

      }

      public void Sql3ConvertToFullDate (string outDate, int outDateSize, string inDate, int len, bool until)
      {

      }

      /// <summary>
      /// Sql3DateToInternal()
      /// </summary>
      /// <param name="inDate"></param>
      /// <param name="outDate"></param>
      /// <param name="len"></param>
      public void Sql3DateToInternal(string inDate, out string outDate, int len)
      {
         string   year = string.Empty;
         string   month = string.Empty;
         string   day = string.Empty;
         short    next = 0;
         int      yearNo;

         Logger.Instance.WriteDevToLog("Sql3DateToInternal(): >>>>>");

         if (len == 8)
         {
            year = inDate.Substring(0, 4);
            next = 4;
         }
         else
         {
            yearNo = int.Parse(inDate.Substring(0, 2));
            if (yearNo < 50)
            {
               year = string.Format("20{0}", inDate.Substring(0, 2));
            }
            else
            {
               year = string.Format("19{0}", inDate.Substring(0, 2));
            }
            next = 2;
         }

         month = inDate.Substring(next, 2);
         day = inDate.Substring(next + 2, 2);

         outDate = string.Format("{0}-{1}-{2}", year, month, day);

         Logger.Instance.WriteDevToLog(string.Format("Sql3DateToInternal(): <<<<<< {0}", outDate));
      }

      /// <summary>
      /// Sql3TimeToInternal()
      /// </summary>
      /// <param name="in_time"></param>
      /// <param name="outTime"></param>
      public void Sql3TimeToInternal(string in_time, out string outTime)
      {
         string hour    = string.Empty;
         string minute  = string.Empty;
         string second  = string.Empty;


         Logger.Instance.WriteDevToLog("Sql3TimeToInternal(): >>>>>");

         hour = in_time.Substring(0, 2);
         minute = in_time.Substring(2, 2);
         second = in_time.Substring(4, 2);

         outTime = string.Format("{0}:{1}:{2}", hour, minute, second);

         Logger.Instance.WriteDevToLog(string.Format("Sql3TimeToInternal(): <<<<<< {0}", outTime));
         
      }

      public void Sql3TimepartToInternal(string inTime, out string outTime, int outTimeLen)
      {
         string hour = string.Empty;
         string min  = string.Empty;
         string sec  = string.Empty;

         Logger.Instance.WriteDevToLog("Sql3TimepartToInternal(): >>>>>");

         hour = inTime.Substring(0, 2);
         min = inTime.Substring(2, 2);
         sec = inTime.Substring(4, 2);


         outTime = string.Format("{0}:{1}:{2}", hour, min, sec);

         Logger.Instance.WriteDevToLog(string.Format("Sql3TimepartToInternal(): <<<<<< {0}", outTime));
      }

      /// <summary>
      /// Sql3DateTimeToInternal()
      /// </summary>
      /// <param name="inDate"></param>
      /// <param name="inTime"></param>
      /// <param name="out_date"></param>
      /// <param name="len"></param>
      public void Sql3DateTimeToInternal(string inDate, string inTime, out string out_date, int len)
      {
         string year   = string.Empty;
         string month  = string.Empty;
         string day    = string.Empty;
         string hour   = string.Empty;
         string minute = string.Empty;
         string second = string.Empty;
         short  next;
         int    yearNo;

         Logger.Instance.WriteDevToLog("Sql3DateTimeToInternal(): >>>>>");

         if (len == 8)
         {
            year = inDate.Substring(0, 4);
            next = 4;
         }
         else
         {
            yearNo = int.Parse(inDate.Substring(2, 2));
            if (yearNo < 50)
               year = string.Format("20{0}", inDate.Substring(0, 2));
            else
               year = string.Format("19{0}", inDate.Substring(0, 2));

            yearNo = int.Parse(inDate.Substring(0, 2));

            next = 2;
         }

         month = inDate.Substring(next, 2);
         day = inDate.Substring(next + 2, 2);

         if (inTime == null)
         {
            hour = "00";
            minute = "00";
            second = "00";
         }
         else
         {
            hour = inTime.Substring(0, 2);
            minute = inTime.Substring(2, 2);
            second = inTime.Substring(4, 2);
         }

         out_date = string.Format("{0}-{1}-{2}T{3}:{4}:{5}", year, month, day, hour, minute, second);

         Logger.Instance.WriteDevToLog(string.Format("Sql3DateTimeToInternal(): <<<<<< {0}", out_date));
      }

      /// <summary>
      /// Sql3Time()
      /// </summary>
      /// <param name="inTime"></param>
      /// <param name="isMinRange"></param>
      /// <param name="outTime"></param>
      /// <param name="OutTimeLen"></param>
      public void Sql3Time (string inTime, bool isMinRange, string outTime, long OutTimeLen)
      {
         string hour;
         string minute;
         string second;
         short  hh, mm, ss;

         Logger.Instance.WriteDevToLog("Sql3Time (): >>>>>");

         hour = inTime.Substring(0, 2);
         minute = inTime.Substring(2, 2);
         second = inTime.Substring(4, 2);

         hh = short.Parse(hour);
         mm = short.Parse(minute);
         ss = short.Parse(second);


         /*uniPaaS Max-val (995959) convert to MSSQL high-val (23.59.59)*/
         if (hh == 99)
            hh = 23;

         if (isMinRange)
         {
            outTime = string.Format("%2.2d:%2.2d:%2.2d.0000000", hh, mm, ss);
         }
         else
         {
            outTime = string.Format("%2.2d:%2.2d:%2.2d.9999999", hh, mm, ss);
         }

         OutTimeLen = SqliteConstants.SQL3_TIME_LEN + 1;

         Logger.Instance.WriteDevToLog("Sql3Time (): <<<<<");
      }

      public void Sql3DBTime(string inTime, string outTime, int outTimeLen)
      {

      }

      public void Sql3ConvertToFullDatetime(string outDateTime, int outDateTimeSize, string inDate, int len, string inTime)
      {

      }

      public void Sql3ConvertToFullTime(string inTime, out string outTime, int outTimeSize)
      {
         string hour = string.Empty;
         string min = string.Empty;
         string sec = string.Empty;

         Logger.Instance.WriteDevToLog("Sql3ConvertToFullTime(): >>>>>");

         hour = inTime.Substring(0, 2);
         min = inTime.Substring(2, 2);
         sec = inTime.Substring(4, 2);

         outTime = string.Format("\'{0}:{1}:{2}:000\'", hour, min, sec);

         Logger.Instance.WriteDevToLog(string.Format("Sql3ConvertToFullTime(): <<<<<< {0}", outTime));
      }

      public void Sql3ConvertToFullMgTime(string in_date, out string outMgtime)
      {
         string   mgtime  = string.Empty;
         string   hour    = string.Empty;
         string   minute  = string.Empty;
         string   seconds = string.Empty;

         Logger.Instance.WriteDevToLog("Sql3ConvertToFullMgTime(): >>>>>");


         hour = in_date.Substring(0, 2);
         minute = in_date.Substring(2, 2);
         seconds = in_date.Substring(4, 2);

         mgtime += string.Format("{ts \'2000-01-01 %2.2s:%2.2s:%2.2s.000\'}", hour, minute, seconds);

         outMgtime = mgtime;
         Logger.Instance.WriteDevToLog("Sql3ConvertToFullMgTime(): <<<<<");
      }

      public void Sql3AddFullDate(out string stmt, string fullDate)
      {
         stmt = string.Empty;
      }

      public bool Sql3CheckTableName(string fname)
      {
         return (fname.Length <= SqliteConstants.SQL3_MAX_OBJECTNAME);
      }

      /// <summary>
      ///  Sql3DateType ()
      /// </summary>
      /// <param name="dbh"></param>
      /// <param name="fldIdx"></param>
      /// <returns>DateType</returns>
      public DateType Sql3DateType(DataSourceDefinition dbh, DBField fld)
      {
         string textType;
         string dateTypeStr;
	      int	   sqlvarLen;
         Sql3Type dataType;
	      DateType		dateType;
         TypeAffinity typeAffinity;

         Sql3GetType(dbh, fld, out textType, out sqlvarLen, out dataType, out dateType,out dateTypeStr, false, false, out typeAffinity); 
         return dateType;
      }

      /// <summary>
      /// sql3ReadAllBlobs
      /// </summary>
      /// <param name="db_crsr"></param>
      /// <returns></returns>
      public SQL3_CODE Sql3ReadAllBlobs(GatewayAdapterCursor dbCrsr)
      {
         GatewayCursor      crsr;
         SQL3Dbd            sql3Dbd;
         int                fldNum = 0;
         DBField            fld;
         GatewayBlob          blobType;
         SQL3_CODE          errorCode  = SqliteConstants.SQL3_OK;
         int                blobIdx    = 0;
         SQL3Connection     connection;

         GatewayCursorTbl.TryGetValue(dbCrsr, out crsr);

         Logger.Instance.WriteDevToLog("Sql3ReadAllBlobs(): >>>>>");

         DbdTbl.TryGetValue(dbCrsr.Definition.DataSourceDefinition, out sql3Dbd);

         connection = ConnectionTbl[sql3Dbd.DatabaseName];
 
         /* for all fields in the DB_CRSR field list */
         for (fldNum = 0 ; fldNum < dbCrsr.Definition.FieldsDefinition.Count && errorCode == SqliteConstants.SQL3_OK; ++fldNum)
         {
            fld  = dbCrsr.Definition.FieldsDefinition[fldNum];

            if (fld.IsBlob())
            {
               blobType = (GatewayBlob)dbCrsr.CurrentRecord.GetValue(fldNum);
               {
                  if (blobType.BlobSize > 0)
                  {
                     Logger.Instance.WriteDevToLog(string.Format("Sql3ReadAllBlobs(): blob no : {0} \n", blobIdx));
                     blobType.Blob = crsr.Output.SqlVars[fldNum].SqlData;
                  }
                  else 
                  {
                     Logger.Instance.WriteDevToLog("Sql3ReadAllBlobs(): size of blob not bigger then 0, null is return");
                     crsr.NullIndicator[fldNum] = 1;
                  }  
                  blobIdx++;
               }
            }
         }

         Logger.Instance.WriteDevToLog("Sql3ReadAllBlobs(): <<<<<");
         return errorCode;
      }
      
      /// <summary>
      /// Sql3GetBlobType()
      /// </summary>
      /// <param name="fld"></param>
      /// <param name="textType"></param>
      /// <param name="sqlVarLen"></param>
      /// <param name="dataType"></param>
      /// <param name="dateType"></param>
      /// <param name="typeAffinity"></param>
      public void Sql3GetBlobType(DBField fld, out string textType, out int sqlVarLen, out Sql3Type dataType, out DateType dateType,
                                  out TypeAffinity typeAffinity)
      {
         
         string dbType = fld.DbType;

         if (dbType == "CLOB" || fld.Storage == FldStorage.AnsiBlob)
         {
            textType = dbType;
            dataType =  Sql3Type.SQL3TYPE_STR;
            typeAffinity =  TypeAffinity.TYPE_AFFINITY_TEXT;
         }
         else if (dbType == "TEXT" || fld.Storage == FldStorage.UnicodeBlob)
         {
            textType = dbType;
            dataType =  Sql3Type.SQL3TYPE_WSTR;
            typeAffinity = TypeAffinity.TYPE_AFFINITY_TEXT;
         }
         else
         {
            textType = "BLOB";
            dataType = Sql3Type.SQL3TYPE_BYTES;
            typeAffinity = TypeAffinity.TYPE_AFFINITY_NONE;
         }

         sqlVarLen = 0;
         dateType =  DateType.NORMAL_TYPE;

         Logger.Instance.WriteDevToLog(string.Format("Sql3GetBlobType(): mapping blob to {0} len {1}", textType, sqlVarLen));
      }
      
      public int Sql3CountExtraPosFlds (GatewayAdapterCursor db_crsr)
      {
         return 0;
      }

      public void SQL3SqldaExtraOutput (Sql3Sqldata sqlda, DB_JOIN_CRSR dbJoinCrsr)
      {

      }

      public void SQL3StmtBuildJoinCond (DB_JOIN_CRSR dbJoinCrsr, string stmt, int stmtSizeInChars)
      {

      }

      public void SQL3StmtBuildOuterJoin (DB_JOIN_CRSR dbJoinCrsr, string stmt, int stmtSizeInChars,bool opt,
                                          bool with_updlock)
      {

      }
      
      public void Sql3BuildDbhIdxInCrsr (DB_JOIN_CRSR dbJoinCrsr, int fields)
      {

      }

      public void Sql3BuildAllPrefixes (DB_JOIN_CRSR dbJoinCrsr)
      {

      }

      public List<int> sql3MakeDbPosSegArray(GatewayAdapterCursor db_crsr)
      {
         return new List<int>();
      }

      public List<int> sql3MakeDbPosSegArrayInJoin (DB_JOIN_CRSR dbJoinCrsr)
      {
         return new List<int>();
      }

   
      public void SQL3BuildSqlRngs (GatewayAdapterCursor db_crsr, string stmt, int stmtSizeInChars)
      {

      }

      public int SQL3StmtBuildSqlRngs (GatewayAdapterCursor db_crsr, string stmt)
      {
         return 0;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="stmt"></param>
      /// <param name="dbh"></param>
      /// <param name="sqlda"></param>
      /// <param name="pSQL3Stmt"></param>
      /// <param name="Insert"></param>
      public void SQL3StmtBuildWithValues (string stmt, DataSourceDefinition dbh,Sql3Sqldata sqlda, Sql3Stmt pSQL3Stmt, bool Insert)
      {
         short sqlvarIdx;
         DBField fld = null;

         if (sqlda == null || sqlda.Sqld == 0)
         {
            if (pSQL3Stmt.StmtWithValues != null && stmt != pSQL3Stmt.StmtWithValues)
            {
               pSQL3Stmt.StmtWithValues = stmt;
            }
            return;
         }

         Logger.Instance.WriteDevToLog("SQL3StmtBuildWithValues(): >>>>>");
         Statement = string.Empty;

         string[] stmArr = stmt.Split('?');
         int stmtArrIndex = 0;
         for (sqlvarIdx = 0; sqlvarIdx < sqlda.Sqld; sqlvarIdx++)
         {
            if (Insert && sqlda.SqlVars[sqlvarIdx].SqlType == Sql3Type.SQL3TYPE_ROWID)
               continue;
            if (sqlda.SqlVars[sqlvarIdx].PartOfDateTime == SqliteConstants.TIME_OF_DATETIME)
               continue;

            Statement += stmArr[stmtArrIndex++];
            if (sqlda.SqlVars[sqlvarIdx].NullIndicator == 1 && Insert)
            {
               Statement += "NULL";
            }
            else
            {
               fld = sqlda.SqlVars[sqlvarIdx].Fld;

               if (fld != null && fld.IsBlob())
               {
                  Statement += "\'\'";
               }
               else
               {
                  if (sqlda.SqlVars[sqlvarIdx].PartOfDateTime > 0 && sqlda.SqlVars[sqlvarIdx].PartOfDateTime < sqlda.Sqld)
                  {
                     Sql3AddVal(ref Statement, dbh, sqlda.SqlVars[sqlvarIdx].SqlType, fld, sqlda.SqlVars[sqlvarIdx].SqlData.ToString(), SqliteConstants.QUOTES, sqlda.SqlVars[sqlda.SqlVars[sqlvarIdx].PartOfDateTime].SqlData.ToString());
                  }
                  else
                  {
                     Sql3AddVal(ref Statement, dbh, sqlda.SqlVars[sqlvarIdx].SqlType, fld, sqlda.SqlVars[sqlvarIdx].SqlData.ToString(), SqliteConstants.QUOTES, null);
                  }
               }
            }
         }

         if (stmtArrIndex < stmArr.Length)
            Statement += stmArr[stmArr.Length - 1];

         pSQL3Stmt.StmtWithValues = Statement;

         Logger.Instance.WriteDevToLog("SQL3StmtBuildWithValues(): <<<<<");
      }

      public void Sql3ResizeBufferForWhereClause (GatewayAdapterCursor db_crsr)
      {

      }
   
      public void SQL3BuildJoinTableNamesStmt (DB_JOIN_CRSR dbJoinCrsr)
      {

      }

      public void SQL3ChangeToMaxSortChar(string str, int len)
      {

      }

      public bool Sql3CheckDbtype (DBField fld, string typeStr)
      {
         bool   isType  = false;
         string dbType;
         string dbTypeUpper;
         
         dbType = fld.DbType;

         if (string.IsNullOrEmpty(dbType))
         {
            dbTypeUpper = dbType.ToUpper();

            if (dbTypeUpper.Contains(typeStr))
               isType = true;
         }

         return (isType);
      }

      /// <summary>
      /// Sql3CheckDbtype()
      /// </summary>
      /// <param name="dbh"></param>
      /// <param name="fldIdx"></param>
      /// <param name="typeStr"></param>
      /// <returns></returns>
      public bool Sql3CheckDbtype (DataSourceDefinition dbh, DBField fld,  string typeStr)
      {
         bool     isType = false;
         string   dbType;
         string   dbTypeUpper;

         dbType = fld.DbType;
         if (!string.IsNullOrEmpty(dbType))
         {
            dbTypeUpper = dbType.ToUpper();

            if (dbTypeUpper.Contains(typeStr))
               isType = true;

         }

         return (isType);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="dbCrsr"></param>
      public void Sql3GetNewIdentity (GatewayAdapterCursor db_crsr, string idSqldata, DBField fld, int len)
      {

      }

      public void SQL3SetNewIdentity(GatewayAdapterCursor dbCrsr)
      {
         GatewayCursor        crsr;
         SQL3Dbd              sql3Dbd;
         object               buf;
         DataSourceDefinition dbh = dbCrsr.Definition.DataSourceDefinition;
         DBKey                posKey = null;
         DBSegment            seg = null;
         int                  segIdx = 0;

         GatewayCursorTbl.TryGetValue(dbCrsr, out crsr);
         DbdTbl.TryGetValue(dbh, out sql3Dbd);

         Logger.Instance.WriteDevToLog("SQL3SetNewIdentity(): >>>>> ");

         if (crsr.PosKey != null)
         {
            posKey = crsr.PosKey;

            /* build an array of offsets of the key segments into the output sqlda */
            if (crsr.KeyArray == null)
               Sql3MakeDbpoSegArray(dbCrsr);

            for (segIdx = 0; segIdx < posKey.Segments.Count; segIdx++)
            {
               seg = posKey.Segments[segIdx];
               DBField fld = seg.Field;
               /* If the seg is an IDENTITY column */
               if (seg.Field == sql3Dbd.identityFld)
               {
                  if (Sql3IsDbCrsrField(dbCrsr, fld))
                     buf = dbCrsr.CurrentRecord.GetValue(crsr.KeyArray[segIdx]);
                  else
                     buf = crsr.Output.SqlVars[crsr.KeyArray[segIdx]].SqlData;
               }
            }
         }

         Logger.Instance.WriteDevToLog("SQL3SetNewIdentity(): <<<<< ");
      }

      /// <summary>
      ///  ConnectDb ()
      /// </summary>
      /// <param name="dbDefinition"></param>
      /// <returns>SQL3_CODE</returns>
      public SQL3_CODE ConnectDb(DatabaseDefinition dbDefinition)
      {
         bool      connectionExist = false;
         SQL3_CODE returnCode = SqliteConstants.SQL3_OK;
         SQL3Connection  sql3Connection = null;

         Logger.Instance.WriteDevToLog(string.Format("ConnectDb(): >>>>> database = {0} SQL3 connection cnt = {1}", dbDefinition.Location, ConnectionTbl.Count));

         if ( string.IsNullOrEmpty(dbDefinition.Location))
            returnCode = SqliteConstants.NO_DATABASE_NAME_GIVEN;
         else
         {
            Dictionary<string, SQL3Connection>.Enumerator connectionEnumerator = ConnectionTbl.GetEnumerator();
            while (connectionEnumerator.MoveNext())
            {
               sql3Connection = connectionEnumerator.Current.Value;
               if (sql3Connection.DbName == dbDefinition.Location)
               {
                  connectionExist = true;
                  break;
               }
            }

            if (!connectionExist)
            {
               Logger.Instance.WriteDevToLog("ConnectDb(): Allocating a new DB_CONNECTION");
               SQL3Connection connection;
#if SQLITE_CIPHER_CPP_GATEWAY
               connection = new SQL3Connection(dbDefinition.Location, dbDefinition.UserPassword);
#else
               connection = new SQL3Connection(dbDefinition.Location);
#endif
               ConnectionTbl.Add(dbDefinition.Location, connection);
               returnCode = SQLiteLow.LibConnect(connection);
            }
         }

         Logger.Instance.WriteDevToLog(string.Format("ConnectDb(): <<<<< database = {0}", dbDefinition.Location));

         return returnCode;      
      }

      public bool Sql3Is2LettersKeyword (string str, int cnt)
      {
         return false;
      }

      public int Sql3NullSegsCountInDbpos (GatewayAdapterCursor dbCrsr, DbPos dbPos)
      {
         int            idx     = 0,
                        offset  = 0,
                        plen    = 0,
                        cnt     = 0;
         GatewayCursor  crsr;
         DBField        fld;
         DBKey          key;
         DBSegment      seg;

         DataSourceDefinition dbh = dbCrsr.Definition.DataSourceDefinition;

         GatewayCursorTbl.TryGetValue(dbCrsr, out crsr);
         key = crsr.PosKey;

         for (idx = 0; idx < key.Segments.Count; idx++)
         {
            byte[] buf = GetBytes(crsr.DbPosBuf, offset, sizeof(short));
            plen = BitConverter.ToInt16(buf, 0);
            offset += sizeof(short);

            seg = key.Segments[idx];
            fld = seg.Field;

            /* if field is null */
            if (plen == 0)
               cnt++;

            offset += plen;
         }

         return (cnt);
      }

      /// <summary>
      /// Sql3LibTotalChanges()
      /// </summary>
      /// <param name="statement"></param>
      /// <returns></returns>
#if SQLITE_CIPHER_CPP_GATEWAY
      public int Sql3LibTotalChanges(SQL3Connection conn)
      {
         return SQLite3DLLImports.Instance.sqlite3_total_changes(conn.ConnHandleUnmanaged);
      }
#else
      public int Sql3LibTotalChanges(Sql3Stmt statement)
      {
         return statement.DataReader.RecordsAffected;
      }
#endif


      public int Sql3GetColumnLength (string datatype)
      {
         return 0;
      }

      public GatewayErrorCode Sql3GetDbError ()
      {
         GatewayErrorCode retcode = SqliteConstants.RET_OK;

         switch (ServerErrCode)
         {
            case (int)SQLiteErrorCode.Constraint:
#if SQLITE_CIPHER_CPP_GATEWAY
               if (LastErr.Contains(SqliteConstants.SQL3_DUP_INDEX_ERR_UNMANAGED))
#else
               if (LastErr.Contains(SqliteConstants.SQL3_DUP_INDEX_ERR))
#endif
                  retcode = GatewayErrorCode.DuplicateKey;
               else
                  retcode = GatewayErrorCode.ConstraintFail;
               break;
            default:
               break;
            // to find particular error messages , handle cases here 

         }
         return retcode;
      }

      public int SQL3CountKeySqlvars (GatewayAdapterCursor db_crsr, DBKey key)
      {
         return 0;
      }

      /// <summary>
      ///   FindPoskey() 
      /// </summary>
      /// <param name = "dbh"></param>
      /// <returns>int</returns>
      public void SQL3SqldaInputKey (GatewayAdapterCursor db_crsr, DBKey key, Sql3SqlVar sqlvar)
      {

      }

      /// <summary>
      ///   CheckKeyNullable() 
      /// </summary>
      /// <param name = "db_crsr"></param>
      /// <param name = "sortkey"></param>
      /// <returns>bool</returns>
      public bool CheckKeyNullable (GatewayAdapterCursor dbCrsr, bool sortkey)
      {
         GatewayCursor     crsr;
         bool              nullable = false;
         DBKey             key;
         List<DBSegment>   seg;
         DBField           fld;
         short             field = 0;

         GatewayCursorTbl.TryGetValue(dbCrsr, out crsr);

         Logger.Instance.WriteDevToLog("CheckKeyNullable(): >>>>>");

         if (dbCrsr.Definition.Key != null)
         {
            if (sortkey == true)
            {
               key = dbCrsr.Definition.Key;
            }
            else
            {
               key = crsr.PosKey;
            }

            seg = key.Segments;

            for (field = 0; field < key.Segments.Count; field++)
            {
               fld = seg[field].Field;
               nullable = fld.AllowNull ? true : false;
               if (nullable == false)
                  break;
            }
         }
         else
            nullable = true;

         Logger.Instance.WriteDevToLog("CheckKeyNullable(): <<<<<");

         return nullable;
      }

      /// <summary>
      ///   FindPoskey() 
      /// </summary>
      /// <param name = "dbh"></param>
      /// <returns>int</returns>
      public DBKey FindPoskey (DataSourceDefinition dbh)
      {
         int         idx         = 0;
         int         segs        = 1000;
         DBKey       posKey      = null;
         DBSegment   seg         = null;
         DBField     fld         = dbh.Fields[0];
         int         timePartCnt = 0;
         int         segIdx      = 0;
         int         keySegs     = 0;

         /* for all the keys in the DBH */
         for (idx = 0; idx < dbh.Keys.Count; ++idx)
         {
            /* if the key is unique */

            if (dbh.Keys[idx].CheckMask(KeyMasks.UniqueKeyModeMask) && !dbh.Keys[idx].CheckMask(KeyMasks.KeySortMask))
            {
               /* if the user requested this key as PosKey=Y and the key is unique */

               if (dbh.RowIdentifier == (char)DBHRowIdentifier.UniqueKey &&
                  dbh.PositionIsn == dbh.Keys[idx].Isn)
               {
                  posKey = dbh.Keys[idx];
                  break;
               }

               timePartCnt = 0;

               for (segIdx = 0; segIdx < dbh.Keys[idx].Segments.Count; segIdx++)
               {
                  seg = dbh.Keys[idx].Segments[segIdx];
                  fld = seg.Field;

                  if (fld.Storage == FldStorage.TimeString && fld.PartOfDateTime > 0)
                     timePartCnt++;
               }

               keySegs = dbh.Keys[idx].Segments.Count - timePartCnt;

               if (keySegs < segs)
               {
                  /* save the smallest one */
                  segs = dbh.Keys[idx].Segments.Count - timePartCnt;
                  posKey = dbh.Keys[idx];
               }
            }
         }
         /* return the smallest key */
         return (posKey);
      }

      /// <summary>
      ///   CheckDatabaseInfoFlag() will check if key exists in Info string , if yes, it returns it's value
      /// </summary>
      /// <param name = "info">info</param>
      /// <param name = "key">key to be searched</param>
      /// <param name = "keyVal">keyval that will be returned as out param</param>
      /// <returns>bool</returns>
      private bool CheckDatabaseInfoFlag(String info, String key, out String keyVal)
      {
         String         str;
         bool           keyFound = false;
         StringBuilder  tmpStr   = new StringBuilder(2);
         char           ch       = '=';

         info = info.ToUpper();
         key = key.ToUpper();
         if (info.Contains(key))
         {
            int idx = info.IndexOf(key) + key.Length;
            str = info.Substring(idx);
            idx = str.IndexOf(ch);
            str = str.Substring(idx + 1);
            for (int i = 0; i < str.Length; i++)
            {
               if (str[i] == ' ')
                  continue;
               tmpStr.Append(str[i]);
               break;
            }
            keyVal = tmpStr.ToString();
         }
         else
            keyVal = null;
         return keyFound;
      }

      /// <summary>
      ///  Gets the suitable SQLite type for MgXP field's attribute and storage.
      /// </summary>
      /// <param name="dbh"></param>
      /// <param name="fldIdx"></param>
      /// <param name="textType"></param>
      /// <param name="sqlVarLen"></param>
      /// <param name="dataType"></param>
      /// <param name="dateType"></param>
      /// <param name="dataTypeStr"></param>
      /// <param name="charDateBind"></param>
      /// <param name="bindVar"></param>
      /// <param name="typeAffinity"></param>
      public void Sql3GetType(DataSourceDefinition dbh, DBField fld, out string textType, out int sqlVarLen, out Sql3Type dataType, 
                              out DateType dateType,out string dataTypeStr, bool charDateBind, bool bindVar, out TypeAffinity typeAffinity)
      {

         int    fldLen;
         string stmtBit = "BOOLEAN",
                stmtInt = "INT",
                stmtSmallInt = "SMALLINT",
                stmtTinyInt = "TINYINT",
                stmtDecimal = "DECIMAL",
                stmtNumeric = "NUMERIC",
                stmtFloat = "FLOAT",
                stmtReal = "REAL",
                stmtDateTime = "DATETIME",
                stmtDateTime4 = "DATETIME",
                stmtChar = "CHARACTER",
                stmtNChar = "NCHAR",
                stmtNVarChar = "NVARCHAR",
                stmtBinary = "BINARY",
                stmtImage = "IMAGE",
                stmtText = "TEXT",
                stmtNText = "NTEXT",
                stmtDate = "DATE",
                stmtTime = "TIME",
                stmtBlob = "BLOB",
                stmtClob = "CLOB";

         Logger.Instance.WriteDevToLog(string.Format("Sql3GetType(): >>>>> storage : {0}", fld.Storage));
         
         textType = string.Empty;
         dataType = Sql3Type.SQL3TYPE_EMPTY;
         dataTypeStr = string.Empty;
         typeAffinity = TypeAffinity.TYPE_AFFINITY_NONE;
         sqlVarLen = fld.Length;
         dateType = DateType.NORMAL_TYPE;

         fldLen = fld.Length;

         // TODO (Snehal) : Use this macro to find fldLen
         //fld_len = STORAGE_FLD_SIZE1(fld);

         string dbTypeUpper = string.Empty;

         if (fld.Storage == FldStorage.UnicodeZString)
         {
            fldLen = fldLen / 2;
         }

         string dbType = fld.DbType;

         if (!string.IsNullOrEmpty(dbType))
         {
            dbTypeUpper = dbType.ToUpper();
         }

         switch (fld.Storage)
         {
            case FldStorage.NumericSigned:
               switch (fldLen)
               {
                  case 1:
                     Logger.Instance.WriteDevToLog("Sql3GetType(): mapping STORAGE_NUMERIC_SIGNED to TINYINT 1");
                     
                     textType = stmtTinyInt + "(1)"; ;
                     dataType = Sql3Type.SQL3TYPE_UI1;
                     dataTypeStr = "SQL3TYPE_BOOL";
                     typeAffinity = TypeAffinity.TYPE_AFFINITY_INTEGER;
                     break;

                  case 2:
                     if (dbTypeUpper.Contains("BIT"))
                     {
                        textType = stmtBit;
                        dataType = Sql3Type.SQL3TYPE_BOOL;
                        dataTypeStr = "SQL3TYPE_BOOL";
                        typeAffinity = TypeAffinity.TYPE_AFFINITY_INTEGER;
                     }
                     else
                     {
                        Logger.Instance.WriteDevToLog("Sql3GetType(): mapping STORAGE_NUMERIC_SIGNED to SMALL 2");
                        
                        textType = stmtSmallInt;
                        dataType = Sql3Type.SQL3TYPE_I2;
                        dataTypeStr = "SQL3TYPE_I2";
                        typeAffinity = TypeAffinity.TYPE_AFFINITY_INTEGER;
                     }

                     break;
                  case 4:
                     if (dbTypeUpper.Contains("BIT"))
                     {
                        textType = stmtBit;
                        dataType = Sql3Type.SQL3TYPE_BOOL;
                        dataTypeStr = "SQL3TYPE_BOOL";
                        typeAffinity = TypeAffinity.TYPE_AFFINITY_INTEGER;
                     }
                     else
                     {
                        Logger.Instance.WriteDevToLog("Sql3GetType(): mapping STORAGE_NUMERIC_SIGNED to INT 4");

                        textType = stmtInt;
                        dataType = Sql3Type.SQL3TYPE_I4;
                        dataTypeStr = "SQL3TYPE_I4";
                        typeAffinity = TypeAffinity.TYPE_AFFINITY_INTEGER;
                     }
                     break;
               }

               break;

            case FldStorage.AlphaZString:
               typeAffinity = TypeAffinity.TYPE_AFFINITY_TEXT;

               if (dbTypeUpper.Contains("SMALLDATETIME"))
               {
                  textType = stmtDateTime4;

                  Logger.Instance.WriteDevToLog("Sql3GetType(): mapping STORAGE_ALPHA_ZSTRING to DATETIME");

                  dateType = DateType.DATETIME4_TO_CHAR;

                  if (charDateBind)
                  {
                     dataType = Sql3Type.SQL3TYPE_STR;
                     dataTypeStr = "SQL3TYPE_STR";
                  }
                  else
                  {

                  }
               }
               if (dbTypeUpper.Contains("DATETIME"))
               {
                  textType = stmtDateTime;
                  Logger.Instance.WriteDevToLog("Sql3GetType(): mapping STORAGE_ALPHA_ZSTRING to DATETIME");
                  
                  dateType = DateType.DATETIME_TO_CHAR;

                  if (charDateBind)
                  {
                     dataType = Sql3Type.SQL3TYPE_STR;
                     dataTypeStr = "SQL3TYPE_STR";
                  }
               }

               if (dbTypeUpper.Contains("NCHAR"))
               {
                  Logger.Instance.WriteDevToLog(string.Format("Sql3GetType(): mapping STORAGE_ALPHA_ZSTRING to NCHAR({0})", fldLen));
                  
                  textType = string.Format("{0}({1})", stmtNChar, fldLen);
                  dataType = Sql3Type.SQL3TYPE_STR;
                  dataTypeStr = "SQL3TYPE_CHAR";
               }
               else if (dbTypeUpper.Contains("NVARCHAR"))
               {
                  Logger.Instance.WriteDevToLog(string.Format("Sql3GetType(): mapping STORAGE_ALPHA_ZSTRING to NVARCHAR({0})", fldLen));
                  
                  textType = string.Format("{0}({1})", stmtNVarChar, fldLen);
                  dataType = Sql3Type.SQL3TYPE_STR;
                  dataTypeStr = "SQL3TYPE_VARCHAR";
               }
               else if (dbTypeUpper.Contains("NTEXT"))
               {
                  Logger.Instance.WriteDevToLog("Sql3GetType(): mapping STORAGE_ALPHA_ZSTRING to NTEXT");
                  
                  textType = stmtNText;
                  dataType = Sql3Type.SQL3TYPE_STR;
                  dataTypeStr = "SQL3TYPE_LONGVARCHAR";
               }
               else if (fldLen  < SqliteConstants.SQL3_MAX_CHAR_LEN)
               {
                  if (dbTypeUpper.Contains("BINARY") ||
                     dbTypeUpper.Contains("IMAGE") ||
                     dbTypeUpper.Contains("TIMESTAMP"))
                  {
                     Logger.Instance.WriteDevToLog(string.Format("Sql3GetType(): mapping STORAGE_ALPHA_ZSTRING to BINARY({0})", fldLen));
                     
                     textType = string.Format("{0}({1})", stmtBinary, fldLen);
                     dataType = Sql3Type.SQL3TYPE_BYTES;
                     dataTypeStr = "SQL3TYPE_BINARY";
                  }
                  else
                  {
                     Logger.Instance.WriteDevToLog(string.Format("Sql3GetType(): mapping STORAGE_ALPHA_ZSTRING to CHAR({0})", fldLen));
                     
                     textType = string.Format("{0}({1})", stmtChar, fldLen);
                     dataType = Sql3Type.SQL3TYPE_STR;
                     dataTypeStr = "SQL3TYPE_CHAR";
                  }

               }
               else
               {
                  if (dbTypeUpper.Contains("IMAGE"))
                  {
                     Logger.Instance.WriteDevToLog("Sql3GetType(): mapping STORAGE_ALPHA_ZSTRING to IMAGE");
                     
                     textType = stmtImage;
                     dataType = Sql3Type.SQL3TYPE_BYTES;
                     dataTypeStr = "SQL3TYPE_LONGVARBINARY";
                  }
                  else
                  {
                     Logger.Instance.WriteDevToLog("Sql3GetType(): mapping STORAGE_ALPHA_ZSTRING to TEXT");
                     
                     textType = stmtText;
                     dataType = Sql3Type.SQL3TYPE_STR;
                     dataTypeStr = "SQL3TYPE_LONGVARCHAR";
                  }
               }

               break;

            case FldStorage.NumericFloat:
               typeAffinity = TypeAffinity.TYPE_AFFINITY_REAL;

               if (dbTypeUpper.Contains("REAL"))
               {
                  Logger.Instance.WriteDevToLog("Sql3GetType(): mapping STORAGE_NUMERIC_FLOAT to REAL");
                  
                  textType = stmtReal;
                  dataType = Sql3Type.SQL3TYPE_R4;
                  dataTypeStr = "SQL3TYPE_R4";
               }
               else if (dbTypeUpper.Contains("DOUBLE") ||
                       dbTypeUpper.Contains("DOUBLE PRECISION") ||
                       dbTypeUpper.Contains("FLOAT"))
               {
                  Logger.Instance.WriteDevToLog(string.Format("Sql3GetType(): mapping STORAGE_NUMERIC_FLOAT to {0}", dbTypeUpper));
                  
                  textType = dbTypeUpper;
                  dataType = Sql3Type.SQL3TYPE_R8;
                  dataTypeStr = "SQL3TYPE_R8";
               }
               else if (dbTypeUpper.Contains("DECIMAL"))
               {
                  Logger.Instance.WriteDevToLog(string.Format("Sql3GetType(): mapping STORAGE_NUMERIC_FLOAT to {0}", dbTypeUpper));
                  
                  textType = dbTypeUpper;
                  dataType = Sql3Type.SQL3TYPE_R8;
                  dataTypeStr = "SQL3TYPE_R8";
               }
               else
               {
                  if (fldLen == 4)
                  {
                     Logger.Instance.WriteDevToLog("Sql3GetType(): mapping STORAGE_NUMERIC_FLOAT to REAL 4");
                     
                     textType = stmtReal;
                     dataType = Sql3Type.SQL3TYPE_R4;
                     dataTypeStr = "SQL3TYPE_R4";
                  }
                  else
                  {
                     Logger.Instance.WriteDevToLog("Sql3GetType(): mapping STORAGE_NUMERIC_FLOAT to FLOAT");
                     
                     textType = stmtFloat;
                     dataType = Sql3Type.SQL3TYPE_R8;
                     dataTypeStr = "SQL3TYPE_R8";
                     sqlVarLen = 8;
                  }
               }
               break;

            case FldStorage.NumericString:
               typeAffinity = TypeAffinity.TYPE_AFFINITY_TEXT;

               if (fld.DataSourceDefinition == DatabaseDefinitionType.Normal)
               {

                  if (dataTypeStr.Contains("DECIMAL"))
                  {
                     textType = string.Format("{0}({1},{2})", stmtDecimal, fld.Whole + fld.Dec, fld.Dec);
                     dataType = Sql3Type.SQL3TYPE_DECIMAL;
                     dataTypeStr = "SQL3TYPE_DECIMAL";
                  }
                  else
                  {
                     textType = string.Format("{0}({1},{2})", stmtNumeric, fld.Whole + fld.Dec, fld.Dec);
                     dataType = Sql3Type.SQL3TYPE_NUMERIC;
                     dataTypeStr = "SQL3TYPE_NUMERIC";
                  }
               }
               else
               {
                  //Defect 117059 : For numeric string if dbType = Binary, then map this field with binary , otherwise map it to string.
                  //This need to done becuase in the given defect example, user has created table using sqlite gateway and accessing the same table
                  //using local gateway. But in the sqlite gateway, we create the Numeric String field as Binary() field. So data is created in byte[]
                  //format. Now there is problem for applying the ranges on such field using Local gateway as local gateway map numericstring with string
                  // data type. So to solve this if user has provided the dbType,then according to the dbType do the mapping.
                  if (fld.DbType.Contains("BINARY"))
                  {
                     Logger.Instance.WriteDevToLog(string.Format("Sql3GetType(): mapping UNSUPPORTED TYPE to BINARY {0}", fldLen));

                     textType = string.Format("{0}({1})", stmtBinary, fldLen);
                     dataType = Sql3Type.SQL3TYPE_BYTES;
                     dataTypeStr = "SQL3TYPE_BINARY";
                     typeAffinity = TypeAffinity.TYPE_AFFINITY_NONE;
                  }
                  else
                  {
                     Logger.Instance.WriteDevToLog(string.Format("Sql3GetType(): mapping UNSUPPORTED TYPE to CHAR {0}", fldLen));

                     textType = string.Format("{0}({1})", stmtChar, fldLen);
                     dataType = Sql3Type.SQL3TYPE_STR;
                     dataTypeStr = "SQL3TYPE_CHAR";

                  }
               }

               break;

            case FldStorage.BooleanInteger:
               typeAffinity = TypeAffinity.TYPE_AFFINITY_INTEGER;

               if (fldLen == 1)
               {
                  Logger.Instance.WriteDevToLog("Sql3GetType(): mapping STORAGE_BOOLEAN_INTEGER to BIT");
                  
                  textType = stmtBit;
                  dataType = Sql3Type.SQL3TYPE_BOOL;
                  dataTypeStr = "SQL3TYPE_BOOL";
               }
               else
               {
                  Logger.Instance.WriteDevToLog("Sql3GetType(): mapping STORAGE_BOOLEAN_INTEGER len 2 to SMALLINT");
                  
                  textType = stmtSmallInt;
                  dataType = Sql3Type.SQL3TYPE_I2;
                  dataTypeStr = "SQL3TYPE_I2";
               }
               break;

            case FldStorage.DateInteger:
            case FldStorage.TimeInteger:
               Logger.Instance.WriteDevToLog("Sql3GetType(): mapping one of magic integers to INT 4");
               
               typeAffinity = TypeAffinity.TYPE_AFFINITY_INTEGER;
               textType = stmtInt;
               dataType = Sql3Type.SQL3TYPE_I4;
               dataTypeStr = "SQL3TYPE_I4";

               break;

            case FldStorage.DateString:
               if (dbTypeUpper.Contains("CHAR"))
               {
                  Logger.Instance.WriteDevToLog(string.Format("Sql3GetType(): mapping STORAGE_DATE_STRING to CHAR(%d)", fldLen));
                  
                  textType = string.Format("{0}({1})", stmtChar, fldLen);
                  dataType = Sql3Type.SQL3TYPE_STR;
                  dateType = DateType.DATE_TO_SQLCHAR;
                  dataTypeStr = "SQL3TYPE_CHAR";
                  typeAffinity = TypeAffinity.TYPE_AFFINITY_TEXT;
                  sqlVarLen = fldLen;
                  break;
               }

               if (dbTypeUpper.Contains("BINARY") ||
                  dbTypeUpper.Contains("VARBINARY") ||
                  dbTypeUpper.Contains("TIMESTAMP"))
               {
                  Logger.Instance.WriteDevToLog("Sql3GetType(): mapping STORAGE_DATE_STRING to BINARY(8)");
                  textType = string.Format("{0}(8)", stmtBinary);
                  dataType = Sql3Type.SQL3TYPE_BYTES;
                  dataTypeStr = "SQL3TYPE_BINARY";
                  break;
               }

               if (dbTypeUpper.Contains("DATETIME") || fld.PartOfDateTime > 0)
               {
                  Logger.Instance.WriteDevToLog("Sql3GetType(): mapping STORAGE_DATE_STRING to DATETIME");
                  
                  textType = stmtDateTime;
                  dataType = Sql3Type.SQL3TYPE_DBTIMESTAMP;
                  sqlVarLen = 19;
                  dataTypeStr = "SQL3TYPE_DBTIMESTAMP";
                  dateType = DateType.DATE_TO_DATE;
                  typeAffinity = TypeAffinity.TYPE_AFFINITY_TEXT;
                  break;
               }
               if (dbTypeUpper.Contains("DATE") || fld.PartOfDateTime == 0)
               {
                  Logger.Instance.WriteDevToLog("Sql3GetType(): mapping STORAGE_DATE_STRING to DATE");
                  
                  textType = stmtDate;
                  dataType = Sql3Type.SQL3TYPE_DBDATE;
                  dataTypeStr = "SQL3TYPE_DBDATE";
                  sqlVarLen = 10;
                  dateType = DateType.DATE_TO_DATE;
                  typeAffinity = TypeAffinity.TYPE_AFFINITY_TEXT;
                  break;
               }

               Logger.Instance.WriteDevToLog("Sql3GetType(): mapping STORAGE_DATE_STRING to DATETIME");

               textType = stmtDateTime;
               dataType = Sql3Type.SQL3TYPE_DBTIMESTAMP;
               sqlVarLen = 19;
               dateType = DateType.DATE_TO_DATE;
               typeAffinity = TypeAffinity.TYPE_AFFINITY_TEXT;
               break;

            case FldStorage.TimeString:
               if (dbTypeUpper.Contains("BINARY") || dbTypeUpper.Contains("VARBINARY"))
               {
                  Logger.Instance.WriteDevToLog(string.Format("Sql3GetType(): mapping STORAGE_TIME_STRING to BINARY({0})", fldLen));
                  
                  textType = string.Format("{0}({1})", stmtBinary, fldLen);
                  dataType = Sql3Type.SQL3TYPE_BYTES;
                  dataTypeStr = "SQL3TYPE_BINARY";
                  break;
               }

               if ((dbTypeUpper.Contains("MGTIME") || dbTypeUpper.Contains("DATETIME")) && fld.PartOfDateTime == 0)
               {
                  Logger.Instance.WriteDevToLog(string.Format("Sql3GetType(): mapping STORAGE_TIME_STRING to DATETIME", fldLen));
                  
                  textType = stmtDateTime;
                  sqlVarLen = 16;
                  dateType = DateType.DATE_TO_DATE;
                  break;
               }

               if (dbTypeUpper.Contains("TIME"))
               {
                  Logger.Instance.WriteDevToLog("Sql3GetType(): mapping STORAGE_TIME_STRING to TIME");

                  textType = stmtTime;
                  dataType = Sql3Type.SQL3TYPE_DBTIME;
                  dateType = DateType.DATE_TO_DATE;
                  dataTypeStr = "SQL3TYPE_CHAR";
                  typeAffinity = TypeAffinity.TYPE_AFFINITY_TEXT;
                  sqlVarLen = 16;
                  break;
               }

               Logger.Instance.WriteDevToLog(string.Format("Sql3GetType(): mapping STORAGE_TIME_STRING to CHAR({0})", fldLen));
               
               textType = string.Format("{0}({1})", stmtChar, fldLen);
               dataType = Sql3Type.SQL3TYPE_STR;
               dataTypeStr = "SQL3TYPE_CHAR";
               typeAffinity = TypeAffinity.TYPE_AFFINITY_TEXT;
               break;

            case FldStorage.UnicodeZString:
               typeAffinity = TypeAffinity.TYPE_AFFINITY_TEXT;


               // For unicode
               if (dbTypeUpper.Contains("NCHAR"))
               {
                  Logger.Instance.WriteDevToLog(string.Format("Sql3GetType(): mapping STORAGE_UNICODE_STRING to NCHAR({0})", fldLen));
                  
                  textType = string.Format("{0}({1})", stmtNChar, fldLen);
                  dataType = Sql3Type.SQL3TYPE_WSTR;
                  dataTypeStr = "SQL3TYPE_WCHAR";
               }
               else if (dbTypeUpper.Contains("NVARCHAR"))
               {
                  Logger.Instance.WriteDevToLog(string.Format("Sql3GetType(): mapping STORAGE_UNICODE_STRING to NVARCHAR({0})", fldLen));
                  
                  textType = string.Format("{0}({1})", stmtNVarChar, fldLen);
                  dataType = Sql3Type.SQL3TYPE_WSTR;
                  dataTypeStr = "SQL3TYPE_WVARCHAR";
               }
               else if (dbTypeUpper.Contains("NTEXT"))
               {
                  Logger.Instance.WriteDevToLog("Sql3GetType(): mapping STORAGE_UNICODE_STRING to NTEXT");
                  
                  textType = stmtNText;
                  dataType = Sql3Type.SQL3TYPE_WSTR;
                  dataTypeStr = "SQL3TYPE_WSTR";
               }
               else if (fldLen <= SqliteConstants.SQL3_MAX_NCHAR_LEN)
               {
                  Logger.Instance.WriteDevToLog(string.Format("Sql3GetType(): mapping STORAGE_UNICODE_STRING to NVARCHAR(%d)", fldLen));
                  
                  textType = string.Format("{0}({1})", stmtNVarChar, fldLen);
                  dataType = Sql3Type.SQL3TYPE_WSTR;
                  dataTypeStr = "SQL3TYPE_WCHAR";
               }
               else
               {
                  Logger.Instance.WriteDevToLog("Sql3GetType(): mapping STORAGE_UNICODE_STRING to NTEXT");
                  
                  textType = stmtNText;
                  dataType = Sql3Type.SQL3TYPE_WSTR;
                  dataTypeStr = "SQL3TYPE_WLONGVARCHAR";
               }
               break;

            case FldStorage.Blob:
            case FldStorage.AnsiBlob:
            case FldStorage.UnicodeBlob:

               Logger.Instance.WriteDevToLog("Sql3GetType(): mapping blob to INT 4");
               
               if (dbType == "CLOB" || fld.Storage == FldStorage.AnsiBlob)
               {
                  textType = stmtClob;
                  dataType = Sql3Type.SQL3TYPE_STR;
                  typeAffinity = TypeAffinity.TYPE_AFFINITY_TEXT;
               }
               else if (dbType == "TEXT" || fld.Storage == FldStorage.UnicodeBlob)
               {
                  textType = stmtText;
                  dataType = Sql3Type.SQL3TYPE_WSTR;
                  typeAffinity = TypeAffinity.TYPE_AFFINITY_TEXT;
               }
               else
               {
                  textType = stmtBlob;
                  dataType = Sql3Type.SQL3TYPE_BYTES;
                  typeAffinity = TypeAffinity.TYPE_AFFINITY_NONE;
               }

               sqlVarLen = SqliteConstants.SQL3_MAX_READ_BUFFER;
               break;

            default:
               typeAffinity = TypeAffinity.TYPE_AFFINITY_NONE;
               if (fldLen <= SqliteConstants.SQL3_MAX_BINARY_LEN)
               {
                  Logger.Instance.WriteDevToLog(string.Format("Sql3GetType(): mapping UNSUPPORTED TYPE to BINARY {0}", fldLen));

                  textType = string.Format("{0}({1})", stmtBinary, fldLen);
                  dataType = Sql3Type.SQL3TYPE_BYTES;
                  typeAffinity = TypeAffinity.TYPE_AFFINITY_NONE;
                  dataTypeStr = "SQL3TYPE_BYTES";
               }
               else
               {
                  Logger.Instance.WriteDevToLog("Sql3GetType(): mapping UNSUPPORTED TYPE to IMAGE");
                  
                  textType = stmtText;
                  dataType = Sql3Type.SQL3TYPE_BYTES;
                  dataTypeStr = "SQL3TYPE_LONGVARBINARY";
               }
               break;
         }

         Logger.Instance.WriteDevToLog("Sql3GetType(): <<<<<");
      }

      /// <summary>
      ///  Initializes GatewayCursor
      /// </summary>
      /// <param name="db_crsr"></param>
      /// <returns>GatewayCursor</returns>
      public GatewayCursor InitGatewayCursor (GatewayAdapterCursor dbCrsr)
      {
         GatewayCursor  crsr     = null;
         DataSourceDefinition dbh = dbCrsr.Definition.DataSourceDefinition;
         if (!GatewayCursorTbl.TryGetValue(dbCrsr, out crsr))
         {
            crsr = new GatewayCursor();
            GatewayCursorTbl.Add(dbCrsr, crsr);
         }
       
         /* Initialize the new gateway cursor */
         crsr.CrsrInit(dbCrsr, this);
         return crsr;
      }

      /// <summary>
      ///  Convert obj into bytes.
      /// </summary>
      /// <param name="obj"></param>
      /// <returns>bytes</returns>
      public byte[] ConvertBytes(Object obj)
      {
         byte[] retVal = null;
         if  (obj.GetType() == typeof(bool))
         {
            retVal = BitConverter.GetBytes((bool)obj);
         }
         else if  (obj.GetType() == typeof(char))
         {
            retVal = BitConverter.GetBytes((char)obj);
         }
         else if (obj.GetType() == typeof(string))
         {
            retVal = Encoding.Default.GetBytes((string)obj);
         }
         else if  (obj.GetType() == typeof(double))
         {
            retVal = BitConverter.GetBytes((double)obj);
         }
         else if  (obj.GetType() == typeof(float))
         {
            retVal = BitConverter.GetBytes((float)obj);
         }
         else if  (obj.GetType() == typeof(int))
         {
            retVal = BitConverter.GetBytes((int)obj);
         }
         else if  (obj.GetType() == typeof(long))
         {
            retVal = BitConverter.GetBytes((long)obj);
         }
         else if  (obj.GetType() == typeof(short))
         {
            retVal = BitConverter.GetBytes((short)obj);
         }

         return retVal;
      }

      /// <summary>
      ///  CopyBytes GatewayCursor
      /// </summary>
      /// <param name="src"></param>
      /// <param name="dest"></param>
      /// <param name="startIndex"></param>
      /// <returns>pos</returns>
      public int CopyBytes(byte[] src, byte[] dest, int startIndex)
      {
         for (int i = 0; i < src.Length; i++)
         {
            dest[startIndex + i] = src[i];
         }
         return src.Length;
      }

      /// <summary>
      ///  CopyBytes GatewayCursor
      /// </summary>
      /// <param name="src"></param>
      /// <param name="dest"></param>
      /// <param name="startIndex"></param>
      /// <returns>pos</returns>
      public byte[] GetBytes(byte[] src, int startIndex, int length)
      {
         byte[] dest = new byte[length];
         
         for (int i = 0; i < length; i++)
         {
            dest[i] = src[startIndex+i];
         }
         return dest;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="byteValue"></param>
      /// <param name="attribute"></param>
      /// <returns></returns>
      public object ConvertFromBytes(byte [] byteValue, StorageAttribute attribute)
      {
         object returnValue = null;

         switch (attribute)
         {
            case StorageAttribute.ALPHA:

               int alphaStringLength = 0;
               for (alphaStringLength = 0; alphaStringLength < byteValue.Length; alphaStringLength++)
               {
                  if (byteValue[alphaStringLength] == 0)
                     break;
               }
               returnValue = Encoding.ASCII.GetString(byteValue, 0, alphaStringLength);
               break;

            case StorageAttribute.UNICODE:
               int unicodeStringLength = 0;
               for (unicodeStringLength = 0; unicodeStringLength < byteValue.Length; unicodeStringLength++)
               {
                  if (byteValue[unicodeStringLength] == 0)
                     break;
               }
               returnValue = Encoding.Unicode.GetString(byteValue, 0, unicodeStringLength);
               break;

            case StorageAttribute.BLOB:
               break;

            case StorageAttribute.BOOLEAN:
               returnValue = BitConverter.ToInt16(byteValue, 0);
               break;

            case StorageAttribute.DATE:
               break;

            // TODO (Snehal) : Handle Double value.
            case StorageAttribute.NUMERIC:
               if(byteValue.Length == 2)
               {
                  returnValue = BitConverter.ToInt16(byteValue, 0);
               }
               else if(byteValue.Length == 4)
               {
                  returnValue = BitConverter.ToInt32(byteValue, 0);
               }
               else if(byteValue.Length > 4)
               {
                  returnValue = BitConverter.ToInt64(byteValue, 0);
               }
               break;

            case StorageAttribute.TIME:
               break;

         }

         return returnValue;
      }

      /// <summary>
      /// Rename table
      /// </summary>
      /// <param name="dbh"></param>
      /// <param name="dbDefinition"></param>
      /// <param name="sourceFileName"></param>
      /// <param name="destinationFileName"></param>
      /// <returns></returns>
      public GatewayErrorCode FileRename(DataSourceDefinition sourceDbh, DataSourceDefinition destinationDbh, DatabaseDefinition dbDefinition)
      {
         SQL3_CODE errorCode = SqliteConstants.SQL3_OK;
         GatewayErrorCode retcode = GatewayErrorCode.Any;
         SQL3Dbd sql3Dbd = new SQL3Dbd();
         SQL3Connection sql3Connection = null;
         string fullName = string.Empty;

         string sourceFileName = sourceDbh.Name;
         string destinationFileName = destinationDbh.Name;

         Logger.Instance.WriteSupportToLog(string.Format("FilRename(): >>>>> database = {0}, file : {1}", dbDefinition.Name, sourceFileName), true);

         int connectDbRetCode = ConnectDb(dbDefinition);

         switch (connectDbRetCode)
         {
            case SqliteConstants.NO_DATABASE_NAME_GIVEN:
               Logger.Instance.WriteSupportToLog("FilRename(): <<<<< no database name given\n", true);
               return GatewayErrorCode.BadOpen;
            case SqliteConstants.SQLITE_NOTADB:
               Logger.Instance.WriteSupportToLog(string.Format("sql3_fil_rename(): <<<<< retcode = {0}\n", connectDbRetCode), true);
               return GatewayErrorCode.GetUserPassword;
         }

         sql3Connection = ConnectionTbl[dbDefinition.Location];

         sql3Dbd.DatabaseName = dbDefinition.Location;
         sql3Dbd.DataSourceDefinition = destinationDbh;

         string fullSourceFileName = string.Empty;
         string fullDestinationName = string.Empty;
         Sql3GetFullName(out fullSourceFileName, fullSourceFileName.Length, sourceDbh, dbDefinition, sourceFileName);
         Sql3GetFullName(out fullDestinationName, fullDestinationName.Length, destinationDbh, dbDefinition, destinationFileName);
         sql3Dbd.TableName = destinationFileName;
         sql3Dbd.FullName = fullDestinationName;

         sql3Dbd.share = DbShare.Write;

         /* if the source and destination names are the same, rebuild indexes */
         if (fullSourceFileName == fullDestinationName)
         {
            sql3Dbd.DataSourceDefinition = sourceDbh;
            sql3Dbd.TableName = sourceFileName;
            sql3Dbd.FullName = sourceFileName;

            /* sql3_check_file_name(&sql3_dbd.full_name); */
            errorCode = DropIndexes(sql3Dbd);
            sql3Dbd.DataSourceDefinition = destinationDbh;
            sql3Dbd.TableName = destinationFileName;
            sql3Dbd.FullName = fullDestinationName;

            /* sql3_check_file_name(&sql3_dbd.full_name); */
            errorCode = AddIndexes(sql3Dbd, IndexingMode.CREATE_MODE, false);

            if (errorCode != SqliteConstants.SQL3_OK && errorCode != SqliteConstants.SQL3_SQL_NOTFOUND)
            {
               retcode = GatewayErrorCode.CannotRename;
            }

         }
         else
         {
            /* drop a table of the same name if it exists */

            errorCode = Sql3DropObject(ConnectionTbl[sql3Dbd.DatabaseName], destinationFileName);

            /* QCR# 766709: We should not go for creating a table if it   */
            /* already exist because of some reason like dropping of that */
            /* table failed coz its Pri. Key is reffered in other table   */
            if (errorCode != SqliteConstants.SQL3_OK && errorCode != SqliteConstants.SQL3_SQL_NOTFOUND)
            {
               retcode = GatewayErrorCode.CannotRename;
            }
            else
            {
               errorCode = FilCreate(sql3Dbd, null, dbDefinition);
            }

            if (errorCode != SqliteConstants.SQL3_OK)
            {
               retcode = GatewayErrorCode.CannotRename;
            }
            else
            {
               BuildRenameStatement(destinationDbh, sourceFileName, destinationFileName);
            }

            if (errorCode == SqliteConstants.SQL3_OK)
            {
               /* execute the rename statement */
               errorCode = SQLiteLow.LibExecuteStatement(Statement, sql3Connection);

               if (errorCode != SqliteConstants.SQL3_OK)
                  if (errorCode != SqliteConstants.SQL3_SQL_NOTFOUND)
                     retcode = GatewayErrorCode.CannotRename;

               if (errorCode == SqliteConstants.SQL3_OK || errorCode == SqliteConstants.SQL3_SQL_NOTFOUND)
               {
                  errorCode = Sql3DropObject(ConnectionTbl[sql3Dbd.DatabaseName], fullSourceFileName);
               }

               if (errorCode == SqliteConstants.SQL3_OK)
               {
                  errorCode = AddIndexes(sql3Dbd, IndexingMode.CREATE_MODE, false);

                  if (errorCode != SqliteConstants.SQL3_OK && errorCode != SqliteConstants.SQL3_SQL_NOTFOUND)
                     retcode = GatewayErrorCode.CannotRename;
               }

               if (errorCode == SqliteConstants.SQL3_OK)
               {
                  errorCode = AddForKeys(sql3Dbd, null, dbDefinition);

                  if (errorCode != SqliteConstants.SQL3_OK && errorCode != SqliteConstants.SQL3_SQL_NOTFOUND)
                     retcode = GatewayErrorCode.CannotRename;
               }

            }
            else
               retcode = GatewayErrorCode.CannotRename;
         }

         Logger.Instance.WriteSupportToLog(string.Format("sql3_fil_rename(): <<<<< retcode = {0}\n", retcode), true);
         return retcode;
      }

      /// <summary>
      /// Build Rename Statement
      /// </summary>
      /// <param name="dbh"></param>
      /// <param name="sourceFileName"></param>
      /// <param name="destinationFileName"></param>
      public void BuildRenameStatement(DataSourceDefinition dbh, string sourceFileName, string destinationFileName)
      {
         int idx = 0;
         string name = string.Empty;
         string buf = string.Empty;
         bool first = true;
         Logger.Instance.WriteDevToLog("StmtBuildRename(): >>>>>");

         Statement = string.Empty;

         /* for all fields in the DBH */
         for (idx = 0 ; idx < dbh.Fields.Count ; ++idx)
         {
            DBField fld = dbh.Fields[idx];
            if ((fld.Storage== FldStorage.TimeString) && (fld.PartOfDateTime != 0))
               continue;

            /* check whether the field is IDENTITY */
            if (Sql3CheckDbtype (fld, SqliteConstants.IDENTITY_STR))
               continue;

            /* check whether the field is TIMESTAMP */
            if (Sql3CheckDbtype (fld, "TIMESTAMP"))
                continue;

            /* get the field name */
            name = fld.DbName;

            /* add it to the string */
            /* if not first field add a comma */
            if (!first)
            {
               buf += string.Format(",{0}", name);
            }
            else
            {
               buf += string.Format("{0}", name);
               first = false;
            }
         }

         /* build the base select statement */
         Statement += string.Format("INSERT INTO {0} ", destinationFileName);
         Statement += string.Format("({0}) SELECT {1} ", buf, buf);
         Statement += string.Format("FROM {0}",sourceFileName);

         Logger.Instance.WriteDevToLog(string.Format("StmtBuildRename(): <<<<<\n\tSQL: {0}", Statement));
         
         return;
      }

      /// <summary>
      /// Get the key Name. If key is magic key(e.g. Key of the tempoarary datasource which is created in datasource converter),
      /// then generate key name otherwise return dbName of Key.
      /// </summary>
      /// <param name="tableName"></param>
      /// <param name="key"></param>
      /// <returns></returns>
      public string Sql3GetKeyName(string tableName, DBKey key)
      {
         string keyName = string.Empty;
         if (key.CheckMask(KeyMasks.MagicKeyMask))
         {
            string keyNumber = key.Isn.ToString();
            keyNumber = keyNumber.PadLeft(3, '0');
            keyName = "KEY" + keyNumber + tableName;
         }
         else
         {
            keyName = key.KeyDBName;
         }

         return keyName;
      }

      /// <summary>
      /// Converts an sql value to gateway value, according to it's fundamental data type.
      /// </summary>
      /// <param name="value"></param>
      /// <param name="storageAttribute"></param>
      /// <returns></returns>
      private object ConvertFundamentalDatatypeValueToGatewayValue(object value, StorageAttribute storageAttribute, DBField dbField)
      {
         object returnedValue = value;
         switch (storageAttribute)
         {
            case StorageAttribute.BOOLEAN:
               if (value is Boolean)
               {
                  returnedValue = ((Boolean)value) ? (short)1 : (short)0;
               }
               else if (value is int)
               {
                  returnedValue = ((int)value == 1) ? (short)1 : (short)0;
               }
               break;
            case StorageAttribute.DATE:
               if (value is DateTime)
               {
                  returnedValue = ((DateTime)value).ToString("yyMMdd");
               }
               else if (value is string)
               {
                  returnedValue = ((string)value).Replace("-", "");
               }
               break;
            case StorageAttribute.BLOB:
               {
                  returnedValue = new GatewayBlob(value, dbField.BlobContent); 
               }
               break;
            case StorageAttribute.TIME:
            case StorageAttribute.NUMERIC:
            case StorageAttribute.ALPHA:
            case StorageAttribute.UNICODE:
            default:
               break;
         }

         return returnedValue;
      }

      /// <summary>
      /// Compute the field storage of the provided value according to it's type & storage attribute.
      /// </summary>
      /// <param name="storageAttribute"></param>
      /// <param name="value"></param>
      /// <param name="dbField"></param>
      /// <returns></returns>
      private FldStorage computeStorageByTargetFieldAttribute(StorageAttribute storageAttribute, object value, ref DBField dbField)
      {
         FldStorage computedFieldStorage = dbField.Storage;

         switch (storageAttribute)
         {
            case StorageAttribute.ALPHA:
               computedFieldStorage = FldStorage.AlphaZString;
               break;
            case StorageAttribute.NUMERIC:
               if (value is int)
               {
                  computedFieldStorage = FldStorage.NumericSigned;
                  dbField.Length = 4;
               }
               else if (value is double)
               {
                  computedFieldStorage = FldStorage.NumericFloat;
               }
               else
               {
                  computedFieldStorage = FldStorage.NumericString;
               }
               break;
            case StorageAttribute.DATE:
               if (value is string)
               {
                  computedFieldStorage = FldStorage.DateString;
               }
               else // int
               {
                  computedFieldStorage = FldStorage.DateInteger;
               }
               break;
            case StorageAttribute.TIME:
               if (value is string)
               {
                  computedFieldStorage = FldStorage.TimeString;
               }
               else // int
               {
                  computedFieldStorage = FldStorage.TimeInteger;
               }
               break;
            case StorageAttribute.BOOLEAN:
               computedFieldStorage = FldStorage.BooleanInteger;
               break;
            case StorageAttribute.UNICODE:
               computedFieldStorage = FldStorage.UnicodeZString;
               dbField.Length *= 2;
               break;
         }

         return computedFieldStorage;
      }

      /// <summary>
      /// In case the connection is not in transaction and TransToOpen flag is set, start transaction on
      /// the connection
      /// </summary>
      /// <param name="sql3Connection"></param>
      /// <returns></returns>
      private SQL3_CODE BeginTransactionIfNeeded(SQL3Connection sql3Connection)
      {
         SQL3_CODE errorCode = SqliteConstants.SQL3_OK;
         if (TransToOpen && !sql3Connection.InTransaction)
         {
            if (SQLiteLow.LibBeginTransaction(sql3Connection) != SqliteConstants.SQL3_OK)
               errorCode = SqliteConstants.SQL3_TRANS_ERROR;
            else
               sql3Connection.InTransaction = true;
         }
         return errorCode;
      }
   }
}
