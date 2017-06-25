using System;
using System.Collections.Generic;
using System.Text;
using SQL3_CODE = System.Int32;
using System.IO;
using com.magicsoftware.util;
using System.Data.SQLite;
using util.com.magicsoftware.util;
using System.Data;
using com.magicsoftware.gatewaytypes;
#if SQLITE_CIPHER_CPP_GATEWAY
using System.Runtime.InteropServices;
using System.Globalization;
#endif

namespace MgSqlite.src
{
   /// <summary>
   ///  This class represents a functionality required to call low level API's of SQLite DB for SQLite Gateway.
   /// </summary>
   internal class SQLiteLow
   {

      SQLiteGateway SQLiteGateway;
#if SQLITE_CIPHER_CPP_GATEWAY
      CultureInfo usCultureInfo;
#endif

      /// <summary>
      ///  Constructor
      /// </summary>
      internal SQLiteLow(SQLiteGateway gatewayObj)
      {
         SQLiteGateway = gatewayObj;
#if SQLITE_CIPHER_CPP_GATEWAY
         usCultureInfo = new CultureInfo("en-US");
#endif
      }

      /// <summary>
      ///  This method will open SQLite Connection
      /// </summary>
      /// <param name="sql3Connection"></param>
      /// <returns>SQL3_CODE</returns>
      internal SQL3_CODE LibConnect(SQL3Connection sql3Connection)
      {
         SQL3_CODE returnCode = 0;         

         //TODO (Snehal) : DataBase File exists or not accordingly "New" Parameter should be set. So find other better solution to check whether
         //                dataBase file exists or not. Maybe we can use API to do this.
#if SQLITE_CIPHER_CPP_GATEWAY

         if (UtilStrByteMode.isLocaleDefLangDBCS())
            returnCode = SQLite3DLLImports.Instance.sqlite3_open16(sql3Connection.DbName, ref sql3Connection.ConnHandleUnmanaged);
         else
            returnCode = SQLite3DLLImports.Instance.sqlite3_open(sql3Connection.DbName, ref sql3Connection.ConnHandleUnmanaged);

         if (returnCode != SqliteConstants.SQLITE_OK)
            LibErrorhandler(sql3Connection);
         else if (!String.IsNullOrEmpty(sql3Connection.DbPassword))
         {
            returnCode = SQLite3DLLImports.Instance.sqlite3_key(sql3Connection.ConnHandleUnmanaged, sql3Connection.DbPassword, sql3Connection.DbPassword.Length);

            //If wrong password is given by the sqlite3_key does not give error. We get only after firing a query on the databse.
            //So check whether given password is correct or not, execute sample query.
            //RetCode = sqlite3_exec(pConnection->connection_hdl, "select 1 from sqlite_master", 0, 0, &LAST_err);
            IntPtr errmsg = new IntPtr(0);
            returnCode = SQLite3DLLImports.Instance.sqlite3_exec(sql3Connection.ConnHandleUnmanaged, "select 1 from sqlite_master", ref errmsg);

            if (returnCode != SqliteConstants.SQLITE_OK)
               LibErrorhandler(sql3Connection);
         }
#else
         string connectionString;
         if (File.Exists(sql3Connection.DbName))
         {
            connectionString = string.Format("Data Source={0};Version=3;New=False;Compress=True;UTF16Encoding=True;", sql3Connection.DbName);
         }
         else
         {
            connectionString = string.Format("Data Source={0};Version=3;New=True;Compress=True;UTF16Encoding=True;", sql3Connection.DbName);
         }

         try
         {
            sql3Connection.connectionHdl = new SQLiteConnection(connectionString);
            sql3Connection.connectionHdl.Open();

            // Defect 117640 : While binding the data it needs to convert the data according to the encoding of database.
            // So get the encoding of database save it on Sql3connection.
            String encoding  = (String)LibFetchRow(sql3Connection, "PRAGMA encoding", Sql3Type.SQL3TYPE_STR, out returnCode);
            if (encoding == "UTF-8")
            {
               sql3Connection.encoding = Encoding.UTF8;
            }
            else
            {
               sql3Connection.encoding = Encoding.Unicode;
            }

         }
         catch(SQLiteException exception)
         {
            returnCode = (SQL3_CODE)exception.ErrorCode;
            sql3Connection.SqliteException = exception;
         }
#endif
         return returnCode;
      }

      /// <summary>
      /// DisConnect the connection.
      /// </summary>
      /// <param name="sql3Connection"></param>
      /// <returns></returns>
      internal SQL3_CODE LibDisconnect(SQL3Connection sql3Connection)
      {
         SQL3_CODE returnCode = 0;

#if SQLITE_CIPHER_CPP_GATEWAY
         returnCode = SQLite3DLLImports.Instance.sqlite3_close(sql3Connection.ConnHandleUnmanaged);
#else
         try
         {
            sql3Connection.connectionHdl.Close();
            sql3Connection.connectionHdl = null;
         }
         catch (SQLiteException exception)
         {
            returnCode = (SQL3_CODE)exception.ErrorCode;
            sql3Connection.SqliteException = exception;
         }         
#endif
         return returnCode;
      }

      /// <summary>
      ///  This will close SQLite connection/
      /// </summary>
      /// <param name="connection"></param>
      /// <param name="sql3Stmt"></param>
      /// <returns>SQL3_CODE</returns>
      internal SQL3_CODE LibClose(SQL3Connection pConnection, Sql3Stmt sql3Stmt)
      {
         SQL3_CODE retcode = SqliteConstants.SQL3_OK;

         Logger.Instance.WriteDevToLog(string.Format("LibClose(): >>>>> stmt name = {0}", sql3Stmt.Name));
         
#if SQLITE_CIPHER_CPP_GATEWAY
         if (sql3Stmt.IsOpen)
            retcode = LibReleaseStmt(sql3Stmt);
#else
         try
         {
            if (sql3Stmt.IsOpen)
               sql3Stmt.DataReader.Close();

            sql3Stmt.IsOpen = false;
         }
         catch(SQLiteException exception)
         {
            retcode = (SQL3_CODE)exception.ErrorCode;
            pConnection.SqliteException = exception;
         }
#endif

         Logger.Instance.WriteDevToLog(string.Format("LibClose(): <<<<< retcode = {0}", retcode));

         return retcode;
      }

      /// <summary>
      ///  
      /// </summary>
      /// <param name="sql3Stmt"></param>
      /// <returns>SQL3_CODE</returns>
      internal SQL3_CODE LibReleaseStmt(Sql3Stmt sql3Stmt)
      {
         SQL3_CODE retcode = SqliteConstants.SQL3_OK;         

         if (sql3Stmt.IsPrepared)
         {
#if SQLITE_CIPHER_CPP_GATEWAY
            bool FinalizeStmt = true;
            if (FinalizeStmt)
            {
               retcode = SQLite3DLLImports.Instance.sqlite3_finalize(sql3Stmt.StmtHandleUnmanaged);
               sql3Stmt.IsOpen = false;
               sql3Stmt.IsPrepared = false;
            }
            else
            {
               retcode = SQLite3DLLImports.Instance.sqlite3_reset(sql3Stmt.StmtHandleUnmanaged);
               sql3Stmt.IsOpen = false;
            }
#else
            if (sql3Stmt.DataReader != null && !sql3Stmt.DataReader.IsClosed)
            {
               sql3Stmt.DataReader.Close();
            }

            sql3Stmt.sqliteCommand.Dispose();
            sql3Stmt.IsOpen = false;
            sql3Stmt.IsPrepared = false;           
#endif
         }
         return retcode;
      }

      /// <summary>
      ///  
      /// </summary>
      /// <param name="sql3Connection"></param>
      /// <returns></returns>
      internal void LibErrorhandler(SQL3Connection sql3Connection)
      {
         string buf;

#if SQLITE_CIPHER_CPP_GATEWAY
         SQLiteGateway.LastErrCode = (int)SQLite3DLLImports.Instance.sqlite3_errcode(sql3Connection.ConnHandleUnmanaged);
         SQLiteGateway.ServerErrCode = (int)SQLite3DLLImports.Instance.sqlite3_errcode(sql3Connection.ConnHandleUnmanaged);
         buf = SQLite3DLLImports.Instance.sqlite3_errmsg(sql3Connection.ConnHandleUnmanaged);
#else
         SQLiteGateway.LastErrCode = (int)sql3Connection.SqliteException.ErrorCode;
         SQLiteGateway.ServerErrCode = (int)sql3Connection.SqliteException.ErrorCode;
         buf = sql3Connection.SqliteException.Message;
#endif
         if (!string.IsNullOrEmpty(buf))
         {
            SQLiteGateway.LastErr = buf;
         }

         Logger.Instance.WriteDevToLog(string.Format("\t\t Message String: {0}", SQLiteGateway.LastErr));
      }

      /// <summary>
      /// Executes the provided sql statemnt. This is a wrapper function to LibExecuteStatement(string,SQL3Connection,bool,out object[])
      /// Use this method if you wish to simply execute the statement, without addressing its results.
      /// </summary>
      /// <param name="statement"></param>
      /// <param name="sqlConnection"></param>
      /// <returns></returns>
      internal SQL3_CODE LibExecuteStatement(string statement, SQL3Connection sqlConnection)
      {
         object[] valueArray = null;
         return LibExecuteStatement(statement, sqlConnection, false, out valueArray);
      }

      /// <summary>
      /// Executes the provided sql statement and fill the statements results (first record only).
      /// </summary>
      /// <param name="statement"></param>
      /// <param name="sqlConnection"></param>
      /// <param name="shouldReadValues">If set to true, fill the statement's results to statementResults</param>
      /// <param name="statementResults"></param>
      /// <returns></returns>
      internal SQL3_CODE LibExecuteStatement(string statement, SQL3Connection sqlConnection,
         bool shouldReadValues, out object[] statementResults)
      {
         statementResults = new object[0];
         SQL3_CODE retcode = SqliteConstants.SQLITE_OK;

         Logger.Instance.WriteDevToLog("LibExecuteStatement(): >>>>> ");
         Logger.Instance.WriteSupportToLog(string.Format("STMT : {0}", statement), true);

#if SQLITE_CIPHER_CPP_GATEWAY
         IntPtr ppStmt = new IntPtr(0);

         retcode = SQLite3DLLImports.Instance.sqlite3_prepare16_v2(sqlConnection.ConnHandleUnmanaged, statement, ref ppStmt);

         if (retcode != SqliteConstants.SQLITE_OK)
         {
            LibErrorhandler(sqlConnection);
            return (retcode);
         }

         retcode = SQLite3DLLImports.Instance.sqlite3_step(ppStmt);
         int columnCount = SQLite3DLLImports.Instance.sqlite3_column_count(ppStmt);

         if (shouldReadValues && columnCount > 0)
         {
            statementResults = new object[columnCount];

            // Check that the statement execution returned at least 1 row
            if (retcode != SqliteConstants.SQLITE_DONE)
            {
               for (int i = 0; i < columnCount; i++)
               {
                  FundamentalDatatypes dataType = (FundamentalDatatypes)SQLite3DLLImports.Instance.sqlite3_column_type(ppStmt, i);
                  switch (dataType)
                  {
                     case FundamentalDatatypes.SQLITE_INTEGER:
                        statementResults[i] = SQLite3DLLImports.Instance.sqlite3_column_int(ppStmt, i);
                        break;
                     case FundamentalDatatypes.SQLITE_FLOAT:
                        statementResults[i] = SQLite3DLLImports.Instance.sqlite3_column_double(ppStmt, i);
                        break;
                     case FundamentalDatatypes.SQLITE_TEXT:
                        statementResults[i] = SQLite3DLLImports.Instance.sqlite3_column_text16(ppStmt, i);
                        break;
                     case FundamentalDatatypes.SQLITE_BLOB:
                        {
                           IntPtr blobDataPointer = SQLite3DLLImports.Instance.sqlite3_column_blob(ppStmt, i);
                           if (blobDataPointer == IntPtr.Zero)
                              statementResults[i] = null;
                           else
                           {
                              int blobLength = SQLite3DLLImports.Instance.sqlite3_column_bytes(ppStmt, i);
                              statementResults[i] = new byte[blobLength];
                              Marshal.Copy(blobDataPointer, (byte[])statementResults[i], 0, blobLength);
                           }
                        }
                        break;
                     case FundamentalDatatypes.SQLITE_NULL:
                     default:
                        break;
                  }
               }
            }
         }

         if (retcode != SqliteConstants.SQLITE_DONE && retcode != SqliteConstants.SQLITE_ROW)
         {
            LibErrorhandler(sqlConnection);
            SQLite3DLLImports.Instance.sqlite3_finalize(ppStmt);
            return (retcode);
         }

         retcode = SQLite3DLLImports.Instance.sqlite3_finalize(ppStmt);
         if (retcode != SqliteConstants.SQLITE_OK)
            return (retcode);
#else
         SQLiteCommand sqCommand = null;
         SQLiteDataReader sqReader  = null;

         try
         {
            sqCommand = new SQLiteCommand(cmdbuf, pConnection.connectionHdl);
            sqCommand.Prepare();
         }
         catch (SQLiteException exception)
         {
            retcode = (SQL3_CODE)exception.ErrorCode;
            pConnection.SqliteException = exception;
         }

         if (retcode != SqliteConstants.SQL3_OK)
         {
            Logger.Instance.WriteDevToLog("LibExecuteImmed(): sqCommand.Prepare() FAILED");

            LibErrorhandler(pConnection);
            sqCommand.Dispose();
            return (retcode);
         }

         try
         {
            sqReader = sqCommand.ExecuteReader();
         }
         catch (SQLiteException exception)
         {
            retcode = (SQL3_CODE)exception.ErrorCode;
            pConnection.SqliteException = exception;
         }

         if (retcode != SqliteConstants.SQLITE_OK)
         {
            Logger.Instance.WriteDevToLog("LibExecuteImmed():sqCommand.ExecuteReader() FAILED");
            LibErrorhandler(pConnection);
            sqCommand.Dispose();
            return (retcode);
         }

         Logger.Instance.WriteDevToLog(string.Format("LibExecuteImmed(): <<<<<  retcode = {0}", retcode));

         sqReader.Close();
         sqCommand.Dispose();
#endif
         return retcode;
      }

      /// <summary>
      ///  
      ///  
      /// </summary>
      /// <param name="mainProg"></param>
      /// <returns></returns>
      internal SQL3_CODE LibExecuteWithParams(Sql3Sqldata sqlda, Sql3Stmt pSQL3Stmt, SQL3Connection pConnection, out int noOfUpdatedRecords, DatabaseOperations operation)
      {
         SQL3_CODE retcode;
         noOfUpdatedRecords = 0;

         Logger.Instance.WriteDevToLog("LibExecuteWithParams(): >>>>> ");

         Logger.Instance.WriteSupportToLog(string.Format("STMT: {0}", pSQL3Stmt.Buf), true);

         retcode = LibBindParams(pSQL3Stmt, sqlda, pConnection, operation);

         if (retcode != SqliteConstants.SQLITE_OK)
         {
            Logger.Instance.WriteDevToLog("LibExecuteWithParams(): LibBindParam() FAILED");
            LibErrorhandler(pConnection);
            return (retcode);
         }

#if SQLITE_CIPHER_CPP_GATEWAY
         //Execute the statement. 
         //On successful execution SQLITE_DONE will be returned.
         if (operation == DatabaseOperations.Insert || operation == DatabaseOperations.Delete ||
            operation == DatabaseOperations.Update)
         {
            retcode = SQLite3DLLImports.Instance.sqlite3_step(pSQL3Stmt.StmtHandleUnmanaged);

            if (retcode != SqliteConstants.SQLITE_DONE)
            {
               //When sqlite3_step () fails, sqlite3_reset () returns an appropriate error code with prepared statement.
               //Also it clears the existing binding.
               SQLite3DLLImports.Instance.sqlite3_reset(pSQL3Stmt.StmtHandleUnmanaged);
               LibErrorhandler(pConnection);
               return retcode;
            }
            else
            {
               SQLite3DLLImports.Instance.sqlite3_reset(pSQL3Stmt.StmtHandleUnmanaged);
               noOfUpdatedRecords = SQLite3DLLImports.Instance.sqlite3_changes(pConnection.ConnHandleUnmanaged);
            }
         }
         else
            pSQL3Stmt.IsOpen = true;
#else
         //In case if we are opening the cursor i.e. stmt is already prepared and we are executing the statement 
         //with new binding values, the actual execution part takes place while fetching the records.
         //Where as in case of DELETE/UPDATE, statement should get executed here only.
         //sqlite3_step () will be performed in esqlc_fetch ()
         try
         {
            if (pSQL3Stmt.DataReader != null && !pSQL3Stmt.DataReader.IsClosed)
            {
               pSQL3Stmt.DataReader.Close();
            }

            pSQL3Stmt.DataReader = pSQL3Stmt.sqliteCommand.ExecuteReader();
            noOfUpdatedRecords = pSQL3Stmt.DataReader.RecordsAffected;
            pSQL3Stmt.IsOpen = true;
            
         }
         catch (SQLiteException exception)
         {
            retcode = (SQL3_CODE)exception.ErrorCode;
            pConnection.SqliteException = exception;
         }
#endif

#if SQLITE_CIPHER_CPP_GATEWAY
         if (retcode != SqliteConstants.SQLITE_DONE && retcode != SqliteConstants.SQL3_OK)
#else
         if (retcode != SqliteConstants.SQL3_OK)
#endif
         {
            LibErrorhandler(pConnection);
         }

         Logger.Instance.WriteDevToLog(string.Format("LibExecuteWithParams(): <<<<<  retcode = {0}", retcode));

         return (retcode);
      }

      /// <summary>
      ///  
      ///  
      /// </summary>
      /// <param name="connection"></param>
      /// <param name="ptable"></param>
      /// <param name="rcount"></param>
      /// <returns></returns>
      internal SQL3_CODE LibFilRecCount(int connection, string ptable, out int rcount)
      {
         rcount = 0;
         SQL3_CODE returnCode = 0;
         return returnCode;
      }

      /// <summary>
      ///  
      ///  
      /// </summary>
      /// <param name="connection"></param>
      /// <param name="pObject"></param>
      /// <param name="objectType"></param>
      /// <returns></returns>
      internal SQL3_CODE LibDrop(SQL3Connection pConnection, string pObject, DropObject objectType)
      {
         SQL3_CODE errorCode = SqliteConstants.SQL3_OK;
         SQLiteGateway.Statement = string.Empty;

         Logger.Instance.WriteDevToLog(string.Format("LibDrop(): >>>>> drop  {0}", pObject));

         switch (objectType)
         {
            case  DropObject.SQL3_DROP_TABLE:
               SQLiteGateway.Statement += string.Format("DROP TABLE {0} ", pObject);
               break;

            case DropObject.SQL3_DROP_VIEW:
               SQLiteGateway.Statement += string.Format("DROP VIEW {0} ", pObject);
               break;

            case DropObject.SQL3_DROP_INDEX:
               SQLiteGateway.Statement += string.Format("DROP INDEX {0} ", pObject);
               break;
            case DropObject.SQL3_DROP_PRMKEY:
               SQLiteGateway.Statement += string.Format("{0} ", pObject);
               break;
            default:
               Logger.Instance.WriteDevToLog("LibDrop(): unkown object_type");
               break;
         }

         errorCode = LibExecuteStatement(SQLiteGateway.Statement, pConnection);

         Logger.Instance.WriteDevToLog(string.Format("LibDrop(): <<<<< retcode - {0}", errorCode));
         
         return errorCode;
      }

      /// <summary>
      ///  LibBeginTransaction()
      /// </summary>
      /// <param name="connection"></param>
      /// <returns>SQL3_CODE</returns>
      internal SQL3_CODE LibBeginTransaction(SQL3Connection connection)
      {
         SQL3_CODE errorCode = SqliteConstants.SQL3_OK;         

#if SQLITE_CIPHER_CPP_GATEWAY
         IntPtr errMsg = new IntPtr(0);
         errorCode = SQLite3DLLImports.Instance.sqlite3_exec(connection.ConnHandleUnmanaged, "BEGIN TRANSACTION", ref errMsg);
#else
         try
         {
            connection.Transaction = connection.connectionHdl.BeginTransaction();
         }
         catch (SQLiteException exception)
         {
            errorCode = (int)exception.ErrorCode;
         }         
#endif
         return errorCode;
      }

#if SQLITE_CIPHER_CPP_GATEWAY
      internal SQL3_CODE LibCommitTransaction(SQL3Connection connection)
      {
         SQL3_CODE    errcode = SqliteConstants.SQLITE_OK;
         IntPtr      errMsg = new IntPtr(0);

         errcode = SQLite3DLLImports.Instance.sqlite3_exec(connection.ConnHandleUnmanaged, "COMMIT TRANSACTION", ref errMsg);

         return errcode;
      }

      internal SQL3_CODE LibRollbackTransaction(SQL3Connection connection)
      {
         SQL3_CODE errcode = SqliteConstants.SQLITE_OK;
         IntPtr errMsg = new IntPtr(0);

         errcode = SQLite3DLLImports.Instance.sqlite3_exec(connection.ConnHandleUnmanaged, "ROLLBACK", ref errMsg);

         return errcode;
      }
#endif


      /// <summary>
      ///  LibFilExist()
      /// </summary>
      /// <param name="connection"></param>
      /// <param name="table"></param>
      /// <returns>SQL3_CODE</returns>
      internal SQL3_CODE LibFilExist(SQL3Connection connection, string table)
      {
         SQL3_CODE  returnCode;
         string     tblName;
         string     tableName;

         Logger.Instance.WriteDevToLog("LibFilExist()>>>>> ");
       
         SQLiteGateway.Sql3SeperateTable (table, out tblName);

         Logger.Instance.WriteSupportToLog(string.Format("LibFilExist() : table - {0}", tblName), true);

         tblName = tblName.ToUpper();

         SQLiteGateway.Statement = string.Format("SELECT name FROM sqlite_master WHERE upper(name) = '{0}'", tblName);

         Logger.Instance.WriteSupportToLog(string.Format("LibFilExist(): \n\t{0}", SQLiteGateway.Statement), true);
         
         tableName = (string)LibFetchRow(connection, SQLiteGateway.Statement, Sql3Type.SQL3TYPE_STR, out returnCode);

         if (returnCode != SqliteConstants.SQL3_OK)
         {
            if (returnCode ==  SqliteConstants.SQL3_SQL_NOTFOUND)
            {
               Logger.Instance.WriteDevToLog(string.Format("LibFilExist(): file %S does not exist", table));
            }
         }
         else
            if (string.IsNullOrEmpty(tableName))
               returnCode = SqliteConstants.SQL3_SQL_NOTFOUND;

         if (Logger.Instance.LogLevel >= Logger.LogLevels.Development)
         {
            if (returnCode == SqliteConstants.SQLITE_OK)
            {
               Logger.Instance.WriteDevToLog(string.Format("LibFilExist(): <<<<< {0} exists", table));
            }
            else
            {
               Logger.Instance.WriteDevToLog(string.Format("LibFilExist(): <<<<< returnCode = {0}", returnCode));
            }
         }

         return returnCode;
      }

      /// <summary>
      ///  LibTableType()
      /// </summary>
      /// <param name="connection"></param>
      /// <param name="tableName"></param>
      /// <param name="tableType"></param>
      /// <returns>SQL3_CODE</returns>
      internal SQL3_CODE LibTableType(SQL3Connection connection, string tableName, out TableType tableType)
      {
         tableType = TableType.Table;
         SQL3_CODE returncode = SqliteConstants.SQL3_OK;
         string    tblName;
         string sql3Stmt = string.Empty;
         string charTableType = string.Empty;
         string tblType = string.Empty;

         Logger.Instance.WriteDevToLog(string.Format("LibTableType() >>>>> table - {0}", tableName));

         if (!string.IsNullOrEmpty(tableName))
         {
            tblName = tableName;
            tblName = tableName.ToUpper();

            sql3Stmt = string.Format("SELECT type FROM sqlite_master WHERE upper(name) = '{0}'", tblName);
         }

         Logger.Instance.WriteSupportToLog(string.Format("STMT: \t{0}", sql3Stmt), true);

         tblType = (string)LibFetchRow(connection, sql3Stmt, Sql3Type.SQL3TYPE_STR, out returncode);

         switch (tblType)
         {
            case "table":
               tableType = TableType.Table;
               break;

             case "view":
               tableType = TableType.View;
               break;
         }

         if (returncode == SqliteConstants.SQL3_SQL_NOTFOUND)
         {
            Logger.Instance.WriteDevToLog(string.Format("LibTableType(): table {0} does not exist", tableName));
         }

         if (Logger.Instance.LogLevel >= Logger.LogLevels.Development)
         {
            if (returncode == SqliteConstants.SQLITE_OK)
            {
               Logger.Instance.WriteDevToLog(string.Format("LibTableType(): <<<<< table type is {0}", tableType));
            }
            else
            {
               Logger.Instance.WriteDevToLog(string.Format("LibTableType(): <<<<< FAILED, retcode = {0}", returncode));
            }
         }
         
         return returncode;
      }

      /// <summary>
      ///  LibFetchRow()
      /// </summary>
      /// <param name="connection"></param>
      /// <param name="stmt"></param>
      /// <param name="dataType"></param>
      /// <param name="returnCode"></param>
      /// <returns>object</returns>
#if SQLITE_CIPHER_CPP_GATEWAY
      internal object LibFetchRow(SQL3Connection pConnection, string stmt, Sql3Type dataType, out SQL3_CODE returnCode)
      {
         object output = null;
         returnCode = SqliteConstants.SQL3_OK; ;
         int columns;
         IntPtr ppStmt = new IntPtr(0);
         string textData;

         returnCode = SQLite3DLLImports.Instance.sqlite3_prepare16_v2(pConnection.ConnHandleUnmanaged, stmt, ref ppStmt);
         if (returnCode != SqliteConstants.SQLITE_OK)
         {
            LibErrorhandler(pConnection);
            SQLite3DLLImports.Instance.sqlite3_finalize(ppStmt);
            return output;
         }

         columns = SQLite3DLLImports.Instance.sqlite3_column_count(ppStmt);

         if (columns <= 0)
            return output;

         returnCode = SQLite3DLLImports.Instance.sqlite3_step(ppStmt);

         if (returnCode != SqliteConstants.SQLITE_ROW)
         {
            if (returnCode == SqliteConstants.SQLITE_DONE)
               returnCode = SqliteConstants.SQL3_SQL_NOTFOUND;
            else
            {
               LibErrorhandler(pConnection);
            }

            SQLite3DLLImports.Instance.sqlite3_finalize(ppStmt);
            returnCode = SqliteConstants.SQL3_SQL_NOTFOUND;
            return output;
         }

         switch (dataType)
         {
            case Sql3Type.SQL3TYPE_WSTR:
               output = SQLite3DLLImports.Instance.sqlite3_column_text16(ppStmt, 0);
               break;
            case Sql3Type.SQL3TYPE_I2:
               textData = SQLite3DLLImports.Instance.sqlite3_column_text(ppStmt, 0);
               output = short.Parse(textData);
               break;
            case Sql3Type.SQL3TYPE_I4:
               textData = SQLite3DLLImports.Instance.sqlite3_column_text(ppStmt, 0);
               output = int.Parse(textData);
               break;
            case Sql3Type.SQL3TYPE_I8:
            case Sql3Type.SQL3TYPE_ROWID:
               textData = SQLite3DLLImports.Instance.sqlite3_column_text(ppStmt, 0);
               output = Int64.Parse(textData);
               break;
            case Sql3Type.SQL3TYPE_R4:
               textData = SQLite3DLLImports.Instance.sqlite3_column_text(ppStmt, 0);
               string systemNumberDecimalSeperator = usCultureInfo.NumberFormat.NumberDecimalSeparator;

               if (!systemNumberDecimalSeperator.Equals('.') && textData.Contains("."))
                  textData = textData.Replace(".", systemNumberDecimalSeperator.ToString());

               output = double.Parse(textData, usCultureInfo);
               break;
            case Sql3Type.SQL3TYPE_R8:
               output = SQLite3DLLImports.Instance.sqlite3_column_double(ppStmt, 0);
               break;
            case Sql3Type.SQL3TYPE_STR:
               output = SQLite3DLLImports.Instance.sqlite3_column_text(ppStmt, 0);
               break;
            case Sql3Type.SQL3TYPE_BOOL:
            case Sql3Type.SQL3TYPE_DBTIMESTAMP:
            case Sql3Type.SQL3TYPE_DBDATE:
               output = SQLite3DLLImports.Instance.sqlite3_column_text(ppStmt, 0);
               break;
         }

         returnCode = SQLite3DLLImports.Instance.sqlite3_reset(ppStmt);
         returnCode = SQLite3DLLImports.Instance.sqlite3_finalize(ppStmt);

         return output;
      }
#else
      internal object LibFetchRow(SQL3Connection pConnection, string stmt, Sql3Type dataType, out SQL3_CODE returnCode)
      {
         object output = null;
         returnCode = SqliteConstants.SQL3_OK; ;         
         
         Logger.Instance.WriteDevToLog("LibFetchRow(): >>>>>");

         int        columns;
         try
         {
            SQLiteCommand sqCommand = new SQLiteCommand(stmt, pConnection.connectionHdl);
            sqCommand.Prepare();
            SQLiteDataReader sqReader = sqCommand.ExecuteReader();

            columns = sqReader.FieldCount;

            if (columns <= 0)
               return output;

            if (!sqReader.Read())
            {
               returnCode = SqliteConstants.SQL3_SQL_NOTFOUND;
               return output;
            }

            switch (dataType)
            {
               case Sql3Type.SQL3TYPE_WSTR:
                  output = sqReader.GetString(0);
                  break;
               case Sql3Type.SQL3TYPE_I2:
                  output = sqReader.GetInt16(0);
                  break;
               case Sql3Type.SQL3TYPE_I4:
               case Sql3Type.SQL3TYPE_I8:
               case Sql3Type.SQL3TYPE_ROWID:
                  output = sqReader.GetInt32(0);
                  break;
               case Sql3Type.SQL3TYPE_R4:
                  output = sqReader.GetFloat(0);
                  break;
               case Sql3Type.SQL3TYPE_R8:
                  output = sqReader.GetDouble(0);
                  break;
               case Sql3Type.SQL3TYPE_STR:
                  output = sqReader.GetString(0);
                  break;
               case Sql3Type.SQL3TYPE_BOOL:
               case Sql3Type.SQL3TYPE_DBTIMESTAMP:
               case Sql3Type.SQL3TYPE_DBDATE:
                  output = sqReader.GetString(0);
                  break;
            }

            sqReader.Close();
            sqCommand.Dispose();
         }
         catch (SQLiteException exception)
         {
            returnCode = (SQL3_CODE)exception.ErrorCode;
            pConnection.SqliteException = exception;
         }

         if (returnCode != SqliteConstants.SQL3_OK)
         {
            LibErrorhandler(pConnection);
         }
         Logger.Instance.WriteDevToLog(string.Format("LibFetchRow(): <<<<< {0}", returnCode));
         return output;
      }
#endif

#if SQLITE_CIPHER_CPP_GATEWAY
      /// <summary>
      /// LibFillDataEncryptedGateway
      /// </summary>
      /// <param name="sqlda"></param>
      /// <param name="sql3Cursor"></param>
      /// <returns></returns>
      internal SQL3_CODE LibFillData(Sql3Sqldata sqlda, SQL3Cursor sql3Cursor)
      {
         int sqlvarIdx, i;
         Sql3Stmt sql3Stmt = SQLiteGateway.StmtTbl[sql3Cursor.StmtIdx];
         string tempDate;
         string fullDate = string.Empty;
         string stringData;
         short shortData = 0;
         int colLength = 0;
         int intData = 0;
         float fltData = 0;
         double dblData = 0;
         bool IsDataNull = false;
         int ColCount = 0;
         IntPtr blobData;

         ColCount = SQLite3DLLImports.Instance.sqlite3_column_count(sql3Stmt.StmtHandleUnmanaged);

         for (sqlvarIdx = 0, i = 0; i < ColCount; sqlvarIdx++)
         {
            if (sqlda.SqlVars[sqlvarIdx].PartOfDateTime == SqliteConstants.TIME_OF_DATETIME)
               continue;

            if (sqlda.SqlVars[sqlvarIdx].SqlType == Sql3Type.SQL3TYPE_WSTR || sqlda.SqlVars[sqlvarIdx].SqlType == Sql3Type.SQL3TYPE_STR)
            {
               colLength = SQLite3DLLImports.Instance.sqlite3_column_bytes16(sql3Stmt.StmtHandleUnmanaged, i);
               if (sqlda.SqlVars[sqlvarIdx].SqlType == Sql3Type.SQL3TYPE_STR)
                  colLength = colLength / 2;

            }
            else
               colLength = SQLite3DLLImports.Instance.sqlite3_column_bytes(sql3Stmt.StmtHandleUnmanaged, i);

            if (!sqlda.SqlVars[sqlvarIdx].IsBlob)
            {
               if (colLength > sqlda.SqlVars[sqlvarIdx].SqlLen)
                  colLength = sqlda.SqlVars[sqlvarIdx].SqlLen;
            }

            //Fetch blob data...
            if (sqlda.SqlVars[sqlvarIdx].IsBlob)
            {
               switch (sqlda.SqlVars[sqlvarIdx].SqlType)
               {
                  case Sql3Type.SQL3TYPE_STR:
                     if (UtilStrByteMode.isLocaleDefLangDBCS())
                        stringData = SQLite3DLLImports.Instance.sqlite3_column_text16(sql3Stmt.StmtHandleUnmanaged, i);
                     else
                        stringData = SQLite3DLLImports.Instance.sqlite3_column_text(sql3Stmt.StmtHandleUnmanaged, i);

                     if (stringData == null)
                        IsDataNull = true;
                     else
                     {
                        sqlda.SqlVars[sqlvarIdx].SqlData = stringData;
                        sqlda.SqlVars[sqlvarIdx].NullIndicator = 0;
                     }
                     break;
                  case Sql3Type.SQL3TYPE_WSTR:
                     stringData = SQLite3DLLImports.Instance.sqlite3_column_text16(sql3Stmt.StmtHandleUnmanaged, i);
                     if (stringData == null)
                        IsDataNull = true;
                     else
                     {
                        sqlda.SqlVars[sqlvarIdx].SqlData = stringData;
                        sqlda.SqlVars[sqlvarIdx].NullIndicator = 0;
                     }
                     break;

                  case Sql3Type.SQL3TYPE_BYTES:                     
                     blobData = SQLite3DLLImports.Instance.sqlite3_column_blob(sql3Stmt.StmtHandleUnmanaged, i);
                     if (blobData == IntPtr.Zero)
                        IsDataNull = true;
                     else
                     {
                        sqlda.SqlVars[sqlvarIdx].SqlData = new byte[colLength];
                        Marshal.Copy(blobData, (byte[])sqlda.SqlVars[sqlvarIdx].SqlData, 0, colLength);
                        sqlda.SqlVars[sqlvarIdx].NullIndicator = 0;
                     }
                     break;
               }
            }
            else
            {
               switch (sqlda.SqlVars[sqlvarIdx].SqlType)
               {
                  case Sql3Type.SQL3TYPE_WSTR:
                     stringData = SQLite3DLLImports.Instance.sqlite3_column_text16(sql3Stmt.StmtHandleUnmanaged, i);
                     sqlda.SqlVars[sqlvarIdx].SqlData = stringData;
                     if (sqlda.SqlVars[sqlvarIdx].SqlData == null)
                        IsDataNull = true;
                     break;
                  case Sql3Type.SQL3TYPE_BOOL:
                  case Sql3Type.SQL3TYPE_UI1:
                     stringData = SQLite3DLLImports.Instance.sqlite3_column_text(sql3Stmt.StmtHandleUnmanaged, i);
                     if (stringData == null)
                        IsDataNull = true;
                     else
                     {
                        shortData = Int16.Parse(stringData);
                        sqlda.SqlVars[sqlvarIdx].SqlData = shortData;
                     }
                     break;
                  case Sql3Type.SQL3TYPE_I2:
                     stringData = SQLite3DLLImports.Instance.sqlite3_column_text(sql3Stmt.StmtHandleUnmanaged, i);
                     if (stringData == null)
                        IsDataNull = true;
                     else
                     {
                        shortData = Int16.Parse(stringData);
                        sqlda.SqlVars[sqlvarIdx].SqlData = shortData;
                     }
                     break;
                  case Sql3Type.SQL3TYPE_I4:
                     stringData = SQLite3DLLImports.Instance.sqlite3_column_text(sql3Stmt.StmtHandleUnmanaged, i);
                     if (stringData == null)
                        IsDataNull = true;
                     else
                     {
                        intData = int.Parse(stringData);
                        sqlda.SqlVars[sqlvarIdx].SqlData = intData;
                     }
                     break;
                  case Sql3Type.SQL3TYPE_I8:
                  case Sql3Type.SQL3TYPE_ROWID:
                     stringData = SQLite3DLLImports.Instance.sqlite3_column_text(sql3Stmt.StmtHandleUnmanaged, i);
                     if (stringData == null)
                        IsDataNull = true;
                     else
                     {
                        intData = Int32.Parse(stringData);
                        sqlda.SqlVars[sqlvarIdx].SqlData = intData;
                     }
                     break;
                  case Sql3Type.SQL3TYPE_R4:
                     stringData = SQLite3DLLImports.Instance.sqlite3_column_text(sql3Stmt.StmtHandleUnmanaged, i);
                     if (stringData == null)
                        IsDataNull = true;
                     else
                     {
                        string systemNumberDecimalSeperator = usCultureInfo.NumberFormat.NumberDecimalSeparator;

                        if (!systemNumberDecimalSeperator.Equals('.') && stringData.Contains("."))
                           stringData = stringData.Replace(".", systemNumberDecimalSeperator.ToString());

                        fltData = float.Parse(stringData);
                        sqlda.SqlVars[sqlvarIdx].SqlData = fltData;
                     }
                     break;
                  case Sql3Type.SQL3TYPE_R8:
                     stringData = SQLite3DLLImports.Instance.sqlite3_column_text(sql3Stmt.StmtHandleUnmanaged, i);
                     if (stringData == null)
                        IsDataNull = true;
                     else
                     {
                        string systemNumberDecimalSeperator = usCultureInfo.NumberFormat.NumberDecimalSeparator;

                        if (!systemNumberDecimalSeperator.Equals('.') && stringData.Contains("."))
                           stringData = stringData.Replace(".", systemNumberDecimalSeperator.ToString());

                        dblData = double.Parse(stringData, usCultureInfo);
                        sqlda.SqlVars[sqlvarIdx].SqlData = dblData;
                     }
                     break;
                  case Sql3Type.SQL3TYPE_STR:
                     stringData = SQLite3DLLImports.Instance.sqlite3_column_text16(sql3Stmt.StmtHandleUnmanaged, i);
                     if (stringData == null)
                        IsDataNull = true;
                     else if (sqlda.SqlVars[sqlvarIdx].DataSourceType == "SQL3TYPE_NUMERIC" && sqlda.SqlVars[sqlvarIdx].SqlLen >= 20)
                     {
                        string systemNumberDecimalSeperator = usCultureInfo.NumberFormat.NumberDecimalSeparator;

                        if (!systemNumberDecimalSeperator.Equals('.') && stringData.Contains("."))
                           stringData = stringData.Replace(".", systemNumberDecimalSeperator.ToString());

                        dblData = double.Parse(stringData, usCultureInfo);
                        sqlda.SqlVars[sqlvarIdx].SqlData = dblData;
                     }
                     else
                        sqlda.SqlVars[sqlvarIdx].SqlData = stringData;
                     break;
                  case Sql3Type.SQL3TYPE_DBTIMESTAMP:
                  case Sql3Type.SQL3TYPE_DBDATE:
                  case Sql3Type.SQL3TYPE_DBTIME:
                     stringData = SQLite3DLLImports.Instance.sqlite3_column_text(sql3Stmt.StmtHandleUnmanaged, i);
                     if (stringData == null)
                        IsDataNull = true;
                     else
                        sqlda.SqlVars[sqlvarIdx].SqlData = stringData;
                     break;
                  case Sql3Type.SQL3TYPE_BYTES:
                     blobData = SQLite3DLLImports.Instance.sqlite3_column_blob(sql3Stmt.StmtHandleUnmanaged, i);
                     if (blobData == IntPtr.Zero)
                        IsDataNull = true;
                     else
                     {
                        sqlda.SqlVars[sqlvarIdx].SqlData = new byte[colLength];
                        Marshal.Copy(blobData, (byte[])sqlda.SqlVars[sqlvarIdx].SqlData, 0, colLength);
                        sqlda.SqlVars[sqlvarIdx].NullIndicator = 0;
                     }
                     //if (sql3Stmt.DataReader.IsDBNull(i))
                     //   IsDataNull = true;
                     //else
                     //{
                     //   // Defect 117059 : For Numeric strings having dbtype= Binary , we save data in byte[] format. While reading this byte array there is problem 
                     //   // With sql3Stmt.DataReader.GetBytes(...) function. So reading the data as string.
                     //   if (sqlda.SqlVars[sqlvarIdx].Fld.Storage == FldStorage.NumericString)
                     //   {
                     //      sqlda.SqlVars[sqlvarIdx].SqlData = sql3Stmt.DataReader.GetString(i);
                     //   }
                     //   else
                     //   {
                     //      sqlda.SqlVars[sqlvarIdx].SqlData = new byte[colLength];
                     //      sql3Stmt.DataReader.GetBytes(i, 0, (byte[])sqlda.SqlVars[sqlvarIdx].SqlData, 0, (int)colLength);
                     //   }
                     //}
                     break;

                  // QCR # 293500 : If it do not get correct sqltype of any field (May be due to some checker errors in datasource), then 
                  // to avoid the crash set it's sqldata to zero.
                  default:
                     sqlda.SqlVars[sqlvarIdx].SqlData = 0;
                     break;
               }
            }

            if (IsDataNull)
            {
               sqlda.SqlVars[sqlvarIdx].NullIndicator = 1;
               if (sqlda.SqlVars[sqlvarIdx].PartOfDateTime != SqliteConstants.NORMAL_OF_DATETIME)
                  sqlda.SqlVars[sqlda.SqlVars[sqlvarIdx].PartOfDateTime].NullIndicator = 1;

               switch (sqlda.SqlVars[sqlvarIdx].SqlType)
               {
                  case Sql3Type.SQL3TYPE_WSTR:
                     sqlda.SqlVars[sqlvarIdx].SqlData = "";
                     break;

                  case Sql3Type.SQL3TYPE_DBTIMESTAMP:
                     sqlda.SqlVars[sqlvarIdx].SqlData = new String(' ', sqlda.SqlVars[sqlvarIdx].SqlLen);
                     break;

                  case Sql3Type.SQL3TYPE_STR:
                  case Sql3Type.SQL3TYPE_DBDATE:
                     sqlda.SqlVars[sqlvarIdx].SqlData = "";
                     break;
                  case Sql3Type.SQL3TYPE_BOOL:
                     sqlda.SqlVars[sqlvarIdx].SqlData = 0;
                     break;
                  case Sql3Type.SQL3TYPE_BYTES:
                  case Sql3Type.SQL3TYPE_I2:
                  case Sql3Type.SQL3TYPE_I4:
                  case Sql3Type.SQL3TYPE_I8:
                  case Sql3Type.SQL3TYPE_UI1:
                  case Sql3Type.SQL3TYPE_R4:
                  case Sql3Type.SQL3TYPE_R8:
                     sqlda.SqlVars[sqlvarIdx].SqlData = 0;
                     break;
               }

               i++;
               IsDataNull = false;
               continue;
            }
            else
            {
               (sqlda.SqlVars[sqlvarIdx].NullIndicator) = 0;
            }

            if (sqlda.SqlVars[sqlvarIdx].PartOfDateTime != SqliteConstants.NORMAL_OF_DATETIME)
            {
               LibDateCrack(sqlda.SqlVars[sqlvarIdx].SqlData, out fullDate, fullDate.Length, sqlda.SqlVars[sqlvarIdx].SqlLen, null);
               tempDate = string.Format("{0}{1}{2}", fullDate.Substring(11, 2), fullDate.Substring(14, 2), fullDate.Substring(17, 2));
               sqlda.SqlVars[sqlda.SqlVars[sqlvarIdx].PartOfDateTime].SqlData = tempDate;

               // put null indicator for the time part
               if (sqlda.SqlVars[sqlvarIdx].NullIndicator == 1)
                  sqlda.SqlVars[sqlda.SqlVars[sqlvarIdx].PartOfDateTime].NullIndicator = 1;
               else
                  sqlda.SqlVars[sqlda.SqlVars[sqlvarIdx].PartOfDateTime].NullIndicator =
                              sqlda.SqlVars[sqlda.SqlVars[sqlvarIdx].PartOfDateTime].SqlLen;
            }

            i++;
         }

         return (int)SQLiteErrorCode.Ok;
      }
#else
      internal SQL3_CODE LibFillData(Sql3Sqldata sqlda, SQL3Cursor sql3Cursor)
      {
         int sqlvarIdx, i;
         Sql3Stmt sql3Stmt = SQLiteGateway.StmtTbl[sql3Cursor.StmtIdx];
         string tempDate;
         string fullDate = string.Empty;
         short shortData = 0;
         int colLength = 0;
         int intData = 0;
         float fltData = 0;
         double dblData = 0;
         bool IsDataNull = false;
         int ColCount = 0;

         ColCount = sql3Stmt.DataReader.FieldCount;

         for (sqlvarIdx = 0, i = 0; i < ColCount; sqlvarIdx++)
         {
            if (sqlda.SqlVars[sqlvarIdx].PartOfDateTime == SqliteConstants.TIME_OF_DATETIME)
               continue;

            if (sqlda.SqlVars[sqlvarIdx].IsBlob && sqlda.SqlVars[sqlvarIdx].SqlType == Sql3Type.SQL3TYPE_BYTES)
            {
               if (!sql3Stmt.DataReader.IsDBNull(i))
                  colLength = (int)sql3Stmt.DataReader.GetBytes(i, 0, null, 0, -1);

            }

            if (!sqlda.SqlVars[sqlvarIdx].IsBlob)
            {
               if (colLength > sqlda.SqlVars[sqlvarIdx].SqlLen)
                  colLength = sqlda.SqlVars[sqlvarIdx].SqlLen;
            }

            //Fetch blob data...
            if (sqlda.SqlVars[sqlvarIdx].IsBlob)
            {
               switch (sqlda.SqlVars[sqlvarIdx].SqlType)
               {
                  case Sql3Type.SQL3TYPE_STR:
                  case Sql3Type.SQL3TYPE_WSTR:
                     if (sql3Stmt.DataReader.IsDBNull(i))
                        IsDataNull = true;
                     else
                     {
                        sqlda.SqlVars[sqlvarIdx].SqlData = sql3Stmt.DataReader.GetString(i);
                        sqlda.SqlVars[sqlvarIdx].NullIndicator = 0;
                     }
                     break;

                  case Sql3Type.SQL3TYPE_BYTES:
                     if (sql3Stmt.DataReader.IsDBNull(i))
                        IsDataNull = true;
                     else
                     {
                        sqlda.SqlVars[sqlvarIdx].SqlData = new byte[colLength];
                        sql3Stmt.DataReader.GetBytes(i, 0, (byte [])sqlda.SqlVars[sqlvarIdx].SqlData, 0, (int)colLength);
                        sqlda.SqlVars[sqlvarIdx].NullIndicator = 0;
                     }
                     break;
               }
            }
            else
            {
               switch (sqlda.SqlVars[sqlvarIdx].SqlType)
               {
                  case Sql3Type.SQL3TYPE_WSTR:
                     if (sql3Stmt.DataReader.IsDBNull(i))
                        IsDataNull = true;
                     else
                     {
                        sqlda.SqlVars[sqlvarIdx].SqlData = sql3Stmt.DataReader.GetString(i);
                     }
                     break;
                  case Sql3Type.SQL3TYPE_BOOL:
                  case Sql3Type.SQL3TYPE_UI1:
                     if (sql3Stmt.DataReader.IsDBNull(i))
                        IsDataNull = true;
                     else
                     {
                        shortData = sql3Stmt.DataReader.GetInt16(i);
                        sqlda.SqlVars[sqlvarIdx].SqlData = shortData;
                     }
                     break;
                  case Sql3Type.SQL3TYPE_I2:
                     if (sql3Stmt.DataReader.IsDBNull(i))
                        IsDataNull = true;
                     else
                     {
                        shortData = sql3Stmt.DataReader.GetInt16(i);
                        sqlda.SqlVars[sqlvarIdx].SqlData = shortData;
                     }
                     break;
                  case Sql3Type.SQL3TYPE_I4:
                  case Sql3Type.SQL3TYPE_I8:
                  case Sql3Type.SQL3TYPE_ROWID:
                     if (sql3Stmt.DataReader.IsDBNull(i))
                        IsDataNull = true;
                     else
                     {
                        intData = sql3Stmt.DataReader.GetInt32(i);
                        sqlda.SqlVars[sqlvarIdx].SqlData = intData;
                     }
                     break;
                  case Sql3Type.SQL3TYPE_R4:
                     if (sql3Stmt.DataReader.IsDBNull(i))
                        IsDataNull = true;
                     else
                     {
                        fltData = sql3Stmt.DataReader.GetFloat(i);
                        sqlda.SqlVars[sqlvarIdx].SqlData = fltData;
                     }
                     break;
                  case Sql3Type.SQL3TYPE_R8:
                     if (sql3Stmt.DataReader.IsDBNull(i))
                        IsDataNull = true;
                     else
                     {
                        dblData = sql3Stmt.DataReader.GetDouble(i);
                        sqlda.SqlVars[sqlvarIdx].SqlData = dblData;
                     }
                     break;
                  case Sql3Type.SQL3TYPE_STR:
                     if (sqlda.SqlVars[sqlvarIdx].DataSourceType == "SQL3TYPE_NUMERIC" || sql3Stmt.DataReader.GetFieldType(i).Equals(typeof(System.Decimal)))
                     {
                        if (sql3Stmt.DataReader.IsDBNull(i))
                           IsDataNull = true;
                        else
                        {
                           sqlda.SqlVars[sqlvarIdx].SqlData = sql3Stmt.DataReader.GetDecimal(i).ToString();
                        }
                     }
                     else
                     {
                        if (sql3Stmt.DataReader.IsDBNull(i))
                           IsDataNull = true;
                        else
                        {
                           sqlda.SqlVars[sqlvarIdx].SqlData = sql3Stmt.DataReader.GetString(i);
                        }
                     }
                     break;
                  case Sql3Type.SQL3TYPE_DBTIMESTAMP:
                  case Sql3Type.SQL3TYPE_DBDATE:
                  case Sql3Type.SQL3TYPE_DBTIME:
                     if (sql3Stmt.DataReader.IsDBNull(i))
                        IsDataNull = true;
                     else
                     {
                        sqlda.SqlVars[sqlvarIdx].SqlData = sql3Stmt.DataReader.GetString(i);
                     }
                     break;
                  case Sql3Type.SQL3TYPE_BYTES:
                     if (sql3Stmt.DataReader.IsDBNull(i))
                        IsDataNull = true;
                     else
                     {
                        // Defect 117059 : For Numeric strings having dbtype= Binary , we save data in byte[] format. While reading this byte array there is problem 
                        // With sql3Stmt.DataReader.GetBytes(...) function. So reading the data as string.
                        if (sqlda.SqlVars[sqlvarIdx].Fld.Storage == FldStorage.NumericString)
                        {
                           sqlda.SqlVars[sqlvarIdx].SqlData = sql3Stmt.DataReader.GetString(i);
                        }
                        else
                        {
                           sqlda.SqlVars[sqlvarIdx].SqlData = new byte[colLength];
                           sql3Stmt.DataReader.GetBytes(i, 0, (byte[])sqlda.SqlVars[sqlvarIdx].SqlData, 0, (int)colLength);
                        }
                     }
                     break;

                  // QCR # 293500 : If it do not get correct sqltype of any field (May be due to some checker errors in datasource), then 
                  // to avoid the crash set it's sqldata to zero.
                  default:
                     sqlda.SqlVars[sqlvarIdx].SqlData = 0;
                     break;
               }
            }

            if (IsDataNull)
            {
               sqlda.SqlVars[sqlvarIdx].NullIndicator = 1;
               if (sqlda.SqlVars[sqlvarIdx].PartOfDateTime != SqliteConstants.NORMAL_OF_DATETIME)
                  sqlda.SqlVars[sqlda.SqlVars[sqlvarIdx].PartOfDateTime].NullIndicator = 1;

               switch (sqlda.SqlVars[sqlvarIdx].SqlType)
               {
                  case Sql3Type.SQL3TYPE_WSTR:
                     sqlda.SqlVars[sqlvarIdx].SqlData = "";
                     break;
                  
                  case Sql3Type.SQL3TYPE_DBTIMESTAMP:
                     sqlda.SqlVars[sqlvarIdx].SqlData = new String(' ', sqlda.SqlVars[sqlvarIdx].SqlLen);
                     break;

                  case Sql3Type.SQL3TYPE_STR:
                  case Sql3Type.SQL3TYPE_DBDATE:
                     sqlda.SqlVars[sqlvarIdx].SqlData = "";
                     break;
                  case Sql3Type.SQL3TYPE_BOOL:
                     sqlda.SqlVars[sqlvarIdx].SqlData = 0;
                     break;
                  case Sql3Type.SQL3TYPE_BYTES:
                  case Sql3Type.SQL3TYPE_I2:
                  case Sql3Type.SQL3TYPE_I4:
                  case Sql3Type.SQL3TYPE_I8:
                  case Sql3Type.SQL3TYPE_UI1:
                  case Sql3Type.SQL3TYPE_R4:
                  case Sql3Type.SQL3TYPE_R8:
                     sqlda.SqlVars[sqlvarIdx].SqlData = 0;
                     break;
               }

               i++;
               IsDataNull = false;
               continue;
            }
            else
            {
               (sqlda.SqlVars[sqlvarIdx].NullIndicator) = 0;
            }

            if (sqlda.SqlVars[sqlvarIdx].PartOfDateTime != SqliteConstants.NORMAL_OF_DATETIME)
            {
               LibDateCrack(sqlda.SqlVars[sqlvarIdx].SqlData, out fullDate, fullDate.Length, sqlda.SqlVars[sqlvarIdx].SqlLen, null);
               tempDate = string.Format("{0}{1}{2}", fullDate.Substring(11, 2), fullDate.Substring(14, 2), fullDate.Substring(17, 2));
               sqlda.SqlVars[sqlda.SqlVars[sqlvarIdx].PartOfDateTime].SqlData = tempDate;

               // put null indicator for the time part
               if (sqlda.SqlVars[sqlvarIdx].NullIndicator == 1)
                  sqlda.SqlVars[sqlda.SqlVars[sqlvarIdx].PartOfDateTime].NullIndicator = 1;
               else
                  sqlda.SqlVars[sqlda.SqlVars[sqlvarIdx].PartOfDateTime].NullIndicator =
                              sqlda.SqlVars[sqlda.SqlVars[sqlvarIdx].PartOfDateTime].SqlLen;
            }

            i++;
         }

         return (int)SQLiteErrorCode.Ok;
      }
#endif

      /// <summary>
      ///  LibConvert()
      /// </summary>
      /// <param name="buf"></param>
      /// <param name="dateBuf"></param>
      /// <param name="dateBufSize"></param>
      internal void LibConvert(string buf, string dateBuf, int dateBufSize)
      {

      }

      /// <summary>
      ///  LibGetLastInsertRowId()
      /// </summary>
      /// <param name="pConnection"></param>
      public Int64 LibGetLastInsertRowId(SQL3Connection connection)
      {
         int lastRowId;
#if SQLITE_CIPHER_CPP_GATEWAY
         lastRowId = (int)SQLite3DLLImports.Instance.sqlite3_last_insert_rowid(connection.ConnHandleUnmanaged);
#else
         string sql3Stmt = string.Empty;
         SQL3_CODE returncode = SqliteConstants.SQL3_OK;
         sql3Stmt = "SELECT last_insert_rowid()";

         lastRowId = (int)LibFetchRow(connection, sql3Stmt, Sql3Type.SQL3TYPE_ROWID, out returncode);
#endif
         return lastRowId;
      }

      /// <summary>
      ///  LibConvertMgtime()
      /// </summary>
      /// <param name="buf"></param>
      /// <param name="dateInfo"></param>
      //internal void LibConvertMgtime(string buf, DateInfo dateInfo)
      //{

      //}

      /// <summary>
      ///  Create date in the string format using date info.
      /// </summary>
      /// <param name="sqldata"></param>
      /// <param name="date"></param>
      /// <param name="dateSize"></param>
      /// <param name="len"></param>
      /// <param name="timeVal"></param>
      internal void LibDateCrack(object sqldata, out string date, int dateSize, int len, string timeVal)
      {
         date = string.Empty;
         //DATE_INFO *date_info = (DATE_INFO *)sqldata;

         if ((len != 6) && (len != 8))
         {
            date = (string)sqldata;
         }
         else
         {
            //if (time_val == NULL)
            //   SPRINTF(date, dateSize, "%4.4d/%2.2d/%2.2d %2.2d:%2.2d:%2.2d.%3.3d", date_info->year, date_info->month, date_info->day, 0, 0, 0, 0);
            //else
            //{
            //   memset(hour, 0, sizeof(hour));
            //   memset(min, 0, sizeof(min));
            //   memset(sec, 0, sizeof(sec));
            //   MEMCPY(hour, sizeof(hour), time_val, 2);
            //   MEMCPY(min, sizeof(min), time_val + 2, 2);
            //   MEMCPY(sec, sizeof(sec), time_val + 4, 2);
            //   SPRINTF(date, dateSize, "%4.4d/%2.2d/%2.2d %s:%s:%s.%3.3d", date_info->year, date_info->month, date_info->day,
            //                hour, min, sec, 0);
            //}
         }
      }

      /// <summary>
      /// LibPrepare()
      /// </summary>
      /// <param name="connection"></param>
      /// <param name="sql3Stmt"></param>
      /// <param name="sql3Cursor"></param>
      /// <returns>SQL3_CODE</returns>
      internal SQL3_CODE LibPrepare(SQL3Connection pConnection, Sql3Stmt sql3Stmt, SQL3Cursor sql3Cursor)
      {
         SQL3_CODE retcode = SqliteConstants.SQL3_OK;

         Logger.Instance.WriteDevToLog("LibPrepare(): >>>>>");

#if SQLITE_CIPHER_CPP_GATEWAY
         retcode = SQLite3DLLImports.Instance.sqlite3_prepare16_v2(pConnection.ConnHandleUnmanaged, sql3Stmt.Buf, ref sql3Stmt.StmtHandleUnmanaged);
#else
         try
         {
            sql3Stmt.sqliteCommand = new SQLiteCommand(sql3Stmt.Buf, pConnection.connectionHdl);
            sql3Stmt.sqliteCommand.Prepare();
         }
         catch (SQLiteException exception)
         {
            retcode = (SQL3_CODE)exception.ErrorCode;
            pConnection.SqliteException = exception;
         }
#endif

         if (retcode == SqliteConstants.SQL3_OK)
         {
            sql3Stmt.IsPrepared = true;
         }
         else
            LibErrorhandler(pConnection);

         Logger.Instance.WriteDevToLog(string.Format("LibPrepare(): <<<<< retcode = {0}", retcode));

         return retcode;

      }

      /// <summary>
      ///  LibPrepareAndExecute()
      /// </summary>
      /// <param name="connection"></param>
      /// <param name="sql3Stmt"></param>
      /// <param name="sql3Cursor"></param>
      /// <returns>SQL3_CODE</returns>
      internal SQL3_CODE LibPrepareAndExecute(SQL3Connection pConnection, Sql3Stmt sql3Stmt, SQL3Cursor sql3Cursor)
      {
         SQL3_CODE retcode  = SqliteConstants.SQL3_OK;
         bool      inputParams         = (sql3Cursor != null && sql3Cursor.InputSqlda != null &&
                                                      sql3Cursor.InputSqlda.Sqld != 0) ? true : false;
         
         Logger.Instance.WriteDevToLog(string.Format("LibPrepareAndExecute(): >>>>> stmt name = {0}", sql3Stmt.Name));

         Logger.Instance.WriteSupportToLog(string.Format(" \n\tSTMT: = {0}", sql3Stmt.Buf), true);

#if SQLITE_CIPHER_CPP_GATEWAY
         retcode = SQLite3DLLImports.Instance.sqlite3_prepare16_v2(pConnection.ConnHandleUnmanaged, sql3Stmt.Buf, ref sql3Stmt.StmtHandleUnmanaged);
         if (retcode == SqliteConstants.SQL3_OK)
         {
            if (inputParams)
            {
               retcode = LibBindParams(sql3Stmt, sql3Cursor.InputSqlda, pConnection, DatabaseOperations.Where);
            }
         }
#else
         try
         {
            sql3Stmt.sqliteCommand = new SQLiteCommand(sql3Stmt.Buf, pConnection.connectionHdl);
            sql3Stmt.sqliteCommand.Prepare();

            if (retcode == SqliteConstants.SQL3_OK)
            {
               if (inputParams)
               {
                  retcode = LibBindParams(sql3Stmt, sql3Cursor.InputSqlda, pConnection, DatabaseOperations.Where);
               }

               if (retcode == SqliteConstants.SQL3_OK)
               {
                  sql3Stmt.DataReader = sql3Stmt.sqliteCommand.ExecuteReader();
               }
                  
            }
            
         }
         catch (SQLiteException exception)
         {
            retcode = (SQL3_CODE)exception.ErrorCode;
            pConnection.SqliteException = exception;
         }
#endif
         if (retcode ==SqliteConstants.SQL3_OK)
         {
            sql3Stmt.IsPrepared = true;
            sql3Stmt.IsOpen = true;

         }
         else
            LibErrorhandler (pConnection);


         Logger.Instance.WriteDevToLog(string.Format("LibPrepareAndExecute(): <<<<< retcode = %d", retcode));

         return retcode;
      }

#if SQLITE_CIPHER_CPP_GATEWAY
      internal SQL3_CODE LibBindParams(Sql3Stmt sql3Stmt, Sql3Sqldata sqlda, SQL3Connection pConnection, DatabaseOperations operation)
      {
         SQL3_CODE returnCode = SqliteConstants.SQL3_OK;
         Sql3SqlVar sqlVar = null;
         int sqlVarIdx = 0;
         bool inputParams = (sqlda != null && sqlda.Sqld != 0) ? true : false;
         long TimePartCnt = 0;
         long bindSqld = 0;
         int i = 0, rowid = 0;
         long dataLen = 0;
         IntPtr pStmt = sql3Stmt.StmtHandleUnmanaged;
         double value;

         // no parameters to bind, exit.
         if (!inputParams)
            return returnCode;

         Logger.Instance.WriteDevToLog(string.Format("LibBindParams(): >>>>> stmt name = {0}", sql3Stmt.Name));
         
         for (i = 0; i < sqlda.Sqld; ++i)
         {
            if (sqlda.SqlVars[i].PartOfDateTime == SqliteConstants.TIME_OF_DATETIME)
               TimePartCnt++;
            if (sqlda.SqlVars[i].SqlType == Sql3Type.SQL3TYPE_ROWID && operation == DatabaseOperations.Insert)
               rowid++;
         }

         bindSqld = sqlda.Sqld - TimePartCnt - rowid;

         returnCode = SQLite3DLLImports.Instance.sqlite3_reset(pStmt);

         for (i = 0, sqlVarIdx = 0; i < bindSqld; sqlVarIdx++)
         {
            sqlVar = sqlda.SqlVars[sqlVarIdx];
            if (sqlVar.PartOfDateTime == SqliteConstants.TIME_OF_DATETIME)
            {
               continue;
            }

            //ToDo: Assign flag for rowid sqlvar
            //Skip for rowid
            if (sqlVar.SqlType == Sql3Type.SQL3TYPE_ROWID && operation == DatabaseOperations.Insert)
            {
               i++;
               continue;
            }

            if (sqlVar.NullIndicator == 1)
            {
               SQLite3DLLImports.Instance.sqlite3_bind_null(pStmt, i + 1);
               i++;
               continue;
            }

            switch (sqlVar.typeAffinity)
            {
               case TypeAffinity.TYPE_AFFINITY_TEXT:
                  //We should specify the datalen in case if data value is not NTS
                  //Else in case of -1 sqlite calculates the datalen with first occurance of null termination.
                  if (sqlVar.SqlType == Sql3Type.SQL3TYPE_DBTIMESTAMP || sqlVar.SqlType == Sql3Type.SQL3TYPE_BYTES ||
                     sqlVar.SqlType == Sql3Type.SQL3TYPE_DBDATE || sqlVar.SqlType == Sql3Type.SQL3TYPE_DBTIME)
                     dataLen = sqlVar.SqlLen;
                  else
                     dataLen = -1;

                  if (sqlVar.SqlType == Sql3Type.SQL3TYPE_WSTR)
                     returnCode = SQLite3DLLImports.Instance.sqlite3_bind_text16(pStmt, i + 1, (string)sqlVar.SqlData);
                  else if (sqlVar.SqlType == Sql3Type.SQL3TYPE_STR)
                  {
                     returnCode = SQLite3DLLImports.Instance.sqlite3_bind_text16(pStmt, i + 1, (string)sqlVar.SqlData);
                  }
                  else
                     returnCode = SQLite3DLLImports.Instance.sqlite3_bind_text16(pStmt, i + 1, (string)sqlVar.SqlData);
                  break;

               case TypeAffinity.TYPE_AFFINITY_NUMERIC:
                  value = double.Parse(sqlVar.SqlData.ToString());
                  SQLite3DLLImports.Instance.sqlite3_bind_double(pStmt, i + 1, value);
                  break;

               case TypeAffinity.TYPE_AFFINITY_INTEGER:
                  if (sqlVar.SqlType == Sql3Type.SQL3TYPE_BOOL)
                  {
                     if (sqlVar.SqlData.ToString().Equals("1"))
                        returnCode = SQLite3DLLImports.Instance.sqlite3_bind_int(pStmt, i + 1, 1);
                     else
                        returnCode = SQLite3DLLImports.Instance.sqlite3_bind_int(pStmt, i + 1, 0);
                  }
                  else if (sqlVar.SqlType == Sql3Type.SQL3TYPE_I8 || sqlVar.SqlType == Sql3Type.SQL3TYPE_ROWID)
                     returnCode = SQLite3DLLImports.Instance.sqlite3_bind_int64(pStmt, i + 1, Int64.Parse(sqlVar.SqlData.ToString()));
                  else
                     returnCode = SQLite3DLLImports.Instance.sqlite3_bind_int(pStmt, i + 1, int.Parse(sqlVar.SqlData.ToString()));
                  break;

               case TypeAffinity.TYPE_AFFINITY_REAL:
                  value = double.Parse(sqlVar.SqlData.ToString());
                  SQLite3DLLImports.Instance.sqlite3_bind_double(pStmt, i + 1, value);
                  break;

               case TypeAffinity.TYPE_AFFINITY_NONE:
                  SQLite3DLLImports.Instance.sqlite3_bind_blob(pStmt, i + 1, sqlVar.SqlData, sqlVar.SqlLen);
                  break;
            }
            i++;
         }

         return returnCode;
      }
#else
      internal SQL3_CODE LibBindParams(Sql3Stmt sql3Stmt, Sql3Sqldata sqlda, SQL3Connection pConnection, DatabaseOperations operation)
      {
         SQL3_CODE  returnCode = SqliteConstants.SQL3_OK;
         Sql3SqlVar sqlVar  = null;
         int sqlVarIdx = 0;
         bool       inputParams = (sqlda != null && sqlda.Sqld != 0) ? true : false;
         long       TimePartCnt          = 0;   
         long       bindSqld            = 0;
         int        i = 0, rowid = 0;
         long       dataLen              = 0;
         SQLiteParameter parameter;

         // no parameters to bind, exit.
         if (! inputParams)
            return returnCode;

         Logger.Instance.WriteDevToLog(string.Format("LibBindParams(): >>>>> stmt name = {0}", sql3Stmt.Name));
         


         for (i = 0; i < sqlda.Sqld; ++i)
         {
            if (sqlda.SqlVars [i].PartOfDateTime == SqliteConstants.TIME_OF_DATETIME)
               TimePartCnt ++;
            if (sqlda.SqlVars [i].SqlType == Sql3Type.SQL3TYPE_ROWID && operation == DatabaseOperations.Insert)
               rowid ++;
         }

         bindSqld = sqlda.Sqld - TimePartCnt - rowid;
         try
         {
            sql3Stmt.sqliteCommand.Parameters.Clear();
            for (i = 0, sqlVarIdx = 0; i < bindSqld; sqlVarIdx++)
            {
               sqlVar = sqlda.SqlVars[sqlVarIdx];
               if (sqlVar.PartOfDateTime == SqliteConstants.TIME_OF_DATETIME)
               {
                  continue;
               }

               //ToDo: Assign flag for rowid sqlvar
               //Skip for rowid
               if (sqlVar.SqlType == Sql3Type.SQL3TYPE_ROWID && operation == DatabaseOperations.Insert)
               {
                  i++;
                  continue;
               }

               if (sqlVar.NullIndicator == 1)
               {
                  parameter = sql3Stmt.sqliteCommand.CreateParameter();
                  parameter.Value = null;
                  sql3Stmt.sqliteCommand.Parameters.Add(parameter);
                  i++;
                  continue;
               }

               switch (sqlVar.typeAffinity)
               {
                  case TypeAffinity.TYPE_AFFINITY_TEXT:
                     //We should specify the datalen in case if data value is not NTS
                     //Else in case of -1 sqlite calculates the datalen with first occurance of null termination.
                     if (sqlVar.SqlType == Sql3Type.SQL3TYPE_DBTIMESTAMP || sqlVar.SqlType == Sql3Type.SQL3TYPE_BYTES ||
                      sqlVar.SqlType == Sql3Type.SQL3TYPE_DBDATE || sqlVar.SqlType == Sql3Type.SQL3TYPE_DBTIME)
                        dataLen = sqlVar.SqlLen;
                     else
                        dataLen = -1;

                     if (sqlVar.SqlType == Sql3Type.SQL3TYPE_WSTR)
                     {
                        parameter = sql3Stmt.sqliteCommand.CreateParameter();
                        parameter.Value = sqlVar.SqlData;
                        sql3Stmt.sqliteCommand.Parameters.Add(parameter);
                     }
                     else if (sqlVar.SqlType == Sql3Type.SQL3TYPE_STR)
                     {
                        sqlVar.SqlData1 = sqlVar.SqlData;

                        parameter = sql3Stmt.sqliteCommand.CreateParameter();
                        parameter.Value = sqlVar.SqlData1;
                        sql3Stmt.sqliteCommand.Parameters.Add(parameter);
                     }
                     else
                     {
                        parameter = sql3Stmt.sqliteCommand.CreateParameter();
                        parameter.Value = sqlVar.SqlData;
                        sql3Stmt.sqliteCommand.Parameters.Add(parameter);
                     }
                     break;

                  case TypeAffinity.TYPE_AFFINITY_NUMERIC:
                     parameter = sql3Stmt.sqliteCommand.CreateParameter();
                     parameter.Value = int.Parse(sqlVar.SqlData.ToString());
                     sql3Stmt.sqliteCommand.Parameters.Add(parameter);
                     break;

                  case TypeAffinity.TYPE_AFFINITY_INTEGER:

                     if (sqlVar.SqlType == Sql3Type.SQL3TYPE_BOOL)
                     {
                        if (sqlVar.SqlData.ToString().Equals("1"))
                        {
                           parameter = sql3Stmt.sqliteCommand.CreateParameter();
                           parameter.Value = true;
                           sql3Stmt.sqliteCommand.Parameters.Add(parameter);
                        }
                        else
                        {
                           parameter = sql3Stmt.sqliteCommand.CreateParameter();
                           parameter.Value = false;
                           sql3Stmt.sqliteCommand.Parameters.Add(parameter);
                        }
                     }
                     else
                     {
                        parameter = sql3Stmt.sqliteCommand.CreateParameter();
                        parameter.Value = int.Parse(sqlVar.SqlData.ToString());
                        sql3Stmt.sqliteCommand.Parameters.Add(parameter);

                     }
                     break;

                  case TypeAffinity.TYPE_AFFINITY_REAL:
                     parameter = sql3Stmt.sqliteCommand.CreateParameter();
                     parameter.Value = double.Parse(sqlVar.SqlData.ToString());
                     sql3Stmt.sqliteCommand.Parameters.Add(parameter);
                     break;

                  case TypeAffinity.TYPE_AFFINITY_NONE:
                     parameter = sql3Stmt.sqliteCommand.CreateParameter();
                     // For Numeric String with dbType=Binary, we save the data in the byte[] format and there  is probelm while
                     // binding the data in where cluase in the byte[] format. It does not find any matching records. To solve
                     // this problem bind the data in the string format.
                     if (sqlVar.Fld.Storage == FldStorage.NumericString)
                     {
                        if (operation == DatabaseOperations.Where)
                        {
                           parameter.Value = sqlVar.SqlData;
                        }
                        else
                        {
                           parameter.Value = pConnection.encoding.GetBytes(sqlVar.SqlData.ToString());
                        }

                     }
                     else
                     {
                        parameter.Value = sqlVar.SqlData;
                     }
                     sql3Stmt.sqliteCommand.Parameters.Add(parameter);
                     break;
               }
               i++;
            }
         }
         catch (SQLiteException exception)
         {
            returnCode = (SQL3_CODE)exception.ErrorCode;
            pConnection.SqliteException = exception;
            Logger.Instance.WriteDevToLog(string.Format("LibBindParams(): Binding failed for Type Affinity {0}", sqlVar.typeAffinity));
         }

         Logger.Instance.WriteDevToLog("LibBindParams(): <<<<<");
         return returnCode;
      }
#endif

      /// <summary>
      ///  
      /// </summary>
      /// <param name="sql3Cursor"></param>
      /// <param name="output"></param>
      /// <returns>SQL3_CODE</returns>
      internal SQL3_CODE LibFetch(SQL3Cursor sql3Cursor, Sql3Sqldata output)
      {
         SQL3_CODE retcode = SqliteConstants.SQL3_OK;

         Sql3Stmt pSQL3Stmt = SQLiteGateway.StmtTbl[sql3Cursor.StmtIdx];

         Logger.Instance.WriteDevToLog("LibFetch(): >>>>> ");

#if SQLITE_CIPHER_CPP_GATEWAY
         if ((retcode = SQLite3DLLImports.Instance.sqlite3_step(pSQL3Stmt.StmtHandleUnmanaged)) == SqliteConstants.SQLITE_DONE)
         {
            retcode = SQLite3DLLImports.Instance.sqlite3_reset(pSQL3Stmt.StmtHandleUnmanaged);
            return SqliteConstants.SQL3_SQL_NOTFOUND;
         }
         else if (retcode == SqliteConstants.SQLITE_ROW)
            retcode = LibFillData(output, sql3Cursor);
         else
            return retcode;
#else
         //Fetch a single row of data
         //In case of successful fetch SQLITE_ROW is returned.
         //In case of end of rowset SQLITE_DONE is returned.
         //In case of wrong execution SQLITE_ERROR is returned
         if (!pSQL3Stmt.DataReader.Read())
         {
            return SqliteConstants.SQL3_SQL_NOTFOUND;
         }
         //Fill up the data into output sqlda in case of successful row fetch.
         else
            retcode = LibFillData(output, sql3Cursor);
#endif

         if (Logger.Instance.LogLevel >= Logger.LogLevels.Support && retcode == SqliteConstants.SQL3_OK)
         {
            Logger.Instance.WriteSupportToLog(string.Format("\tFETCH {0}", sql3Cursor.Name), true);

            if (Logger.Instance.LogLevel >= Logger.LogLevels.Support && Logger.Instance.LogLevel != Logger.LogLevels.Basic)
            {
               for (int i = 0; i < output.Sqln; i++)
               {
                  SQLLogging.SQL3LogSqldataOutput(output.SqlVars[i]);
               }
            }
         }

         Logger.Instance.WriteDevToLog(string.Format("LibFetch():  <<<<< retcode = {0}", retcode));

         return retcode;
      }
   }
}
