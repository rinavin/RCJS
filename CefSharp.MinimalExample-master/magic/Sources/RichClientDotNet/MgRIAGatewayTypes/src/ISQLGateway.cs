using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.util;
using com.magicsoftware.gatewaytypes.data;

namespace com.magicsoftware.gatewaytypes
{
   public interface ISQLGateway
   {
      GatewayErrorCode FileOpen(DataSourceDefinition dbh, DatabaseDefinition dbPtr, string fileName, Access access, DbShare share, DbOpen mode, List<DataSourceDefinition> referencedDbhVec);
      GatewayErrorCode Trans(int transmode);
      GatewayErrorCode FilExist(DataSourceDefinition dbh, DatabaseDefinition db_ptr, string fname);
      GatewayErrorCode FileDelete(DataSourceDefinition dbh, DatabaseDefinition db_ptr, string fname);
      GatewayErrorCode FilRecCount(DataSourceDefinition dbh, out int count);
      GatewayErrorCode FileClose(DataSourceDefinition dbh);
      GatewayErrorCode FileRename(DataSourceDefinition sourceDbh, DataSourceDefinition destinationDbh, DatabaseDefinition dbDefinition);
      GatewayErrorCode CrsrPrepare(GatewayAdapterCursor dbCrsr, DatabaseDefinition dbPtr);
      GatewayErrorCode CrsrRelease(GatewayAdapterCursor dbCrsr);
      GatewayErrorCode CrsrOpen(GatewayAdapterCursor dbCrsr);
      GatewayErrorCode CrsrClose(GatewayAdapterCursor dbCrsr);
      GatewayErrorCode CrsrFetch(GatewayAdapterCursor dbCrsr);
      GatewayErrorCode CrsrGetCurr(GatewayAdapterCursor dbCrsr);
      GatewayErrorCode CrsrKeyChk(GatewayAdapterCursor dbCrsr);
      GatewayErrorCode CrsrUpdate(GatewayAdapterCursor dbCrsr);
      GatewayErrorCode CrsrDelete(GatewayAdapterCursor db_crsr);
      GatewayErrorCode CrsrInsert(GatewayAdapterCursor dbCrsr);
      GatewayErrorCode CrsrUnlock(GatewayAdapterCursor dbCrsr);
      GatewayErrorCode CrsrFetchBLOBs(GatewayAdapterCursor dbCrsr);
      GatewayErrorCode CrsrPrepareJoin(DB_JOIN_CRSR dbJoinCrsr, DatabaseDefinition dbPtr);
      GatewayErrorCode CrsrBeginJoin(DB_JOIN_CRSR dbJoinCrsr);
      GatewayErrorCode CrsrOpenJoin(DB_JOIN_CRSR dbJoinCrsr);
      GatewayErrorCode CrsrCloseJoin(DB_JOIN_CRSR dbJoinCrsr);
      GatewayErrorCode CrsrEndJoin(DB_JOIN_CRSR dbJoinCrsr);
      GatewayErrorCode CrsrReleaseJoin(DB_JOIN_CRSR dbJoinCrsr);
      GatewayErrorCode CrsrFetchJoin(DB_JOIN_CRSR dbJoinCrsr);
      GatewayErrorCode CrsrGetCurrJoin(DB_JOIN_CRSR dbJoinCrsr);
      GatewayErrorCode CrsrFetchBLOBsJoin(DB_JOIN_CRSR dbJoinCrsr);
      GatewayErrorCode LastError(DatabaseDefinition dbDefinition, bool clear, ref int DBMS_code, ref string buf);
      GatewayErrorCode DbDisconnect(string databaseLocation, out string tableName);
      GatewayErrorCode SQLExecute(DatabaseDefinition dbDefinition, string sqlStatement, StorageAttribute[] storageAttributes, out object[] statementReturnedValues, ref DBField[] dbFields);
   }
}
