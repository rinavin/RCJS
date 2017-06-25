using System;
using System.Collections.Generic;
using System.Text;

namespace com.magicsoftware.gatewaytypes
{
   public enum GatewayErrorCode
   {
      Any = 0,                                                                                              // DB_ERR_ANY
      RecordLocked = 1,                                                                                     // DB_ERR_REC_LOCKED
      DuplicateKey = 2,                                                                                     // DB_ERR_DUP_KEY
      ConstraintFail = 3,                 // new                                                            // DB_ERR_CONSTR_FAIL
      TriggerFail = 4,                // new                                                                // DB_ERR_TRIGGER_FAIL
      RecordUpdated = 5,                 // new                                                             // DB_ERR_REC_UPDATED
      NoRowsAffected = 6,                                                                                   // DB_ERR_NO_ROWS_AFFECTED
      UpdateFail = 7,                 // ori - will change to update/insert/deldete                         // DB_ERR_UPDATE_FAIL
      Unmapped = 8,                    //all errors that are not in the picklist (any is all).              // DB_ERR_UNMAPPED
      // put here errors that should be handled as ANY but are not in the user pick list.                   
      ExecSql = 9,                                                                                          // DB_ERR_EXEC_SQL                                        
      BadSqlCommand = 10,                                                                                   // DB_ERR_BAD_SQL_CMD
      BadIni = 11,                                                                                          // DB_ERR_BADINI
      BadName = 12,                                                                                         // DB_ERR_BADNAME
      Damaged = 13,                                                                                         // DB_ERR_DAMAGED
      Unlocked = 14,             /* internal in FM */                                                       // DB_ERR_UNLOCKED
      BadOpen = 15,                                                                                         // DB_ERR_BADOPEN
      BadClose = 16,                                                                                        // DB_ERR_BADCLOSE
      ResourceLocked = 17,                                                                                  // DB_ERR_RSRC_LOCKED
      RecordLockedNoBuf = 18,                                                                               // DB_ERR_REC_LOCKED_NOBUF
      NoDef = 19,                                                                                           // DB_ERR_NODEF
      RecordLockedNow = 20,      /* internal in mg_rtry */                                                  // DB_ERR_REC_LOCKED_NOW
      WarnRetry = 21,               /* internal in FM */                                                    // DB_WRN_RETRY
      RecordLockedMagic = 22,    /* internal in FM */                                                       // DB_ERR_REC_LOCKED_MAGIC
      ReadOnly = 23,                                                                                        // DB_ERR_READONLY
      WarnCreated = 24,                                                                                     // DB_WRN_CREATED
      Capacity = 25,            /* internal in FM */                                                        // DB_ERR_CAPACITY
      TransactionCommit = 26,                                                                               // DB_ERR_TRANS_COMMIT
      TransactionOpen = 27,                                                                                 // DB_ERR_TRANS_OPEN
      TransactionAbort = 28,                                                                                // DB_ERR_TRANS_ABORT
      BadDef = 29,                                                                                          // DB_ERR_BADDEF
      InvalidOwner = 30,                                                                                    // DB_ERR_INVALID_OWNR
      ClearOwnerFail = 31,                                                                                  // DB_ERR_CLR_OWNR_FAIL
      AlterTable = 32,                                                                                      // DB_ERR_ALTER_TBL
      SortTable = 33,                                                                                       // DB_ERR_SORT_TBL
      CannotRemove = 34,                                                                                    // DB_ERR_CANOT_REMOVE
      CannotRename = 35,                                                                                    // DB_ERR_CANOT_RENAME
      WarnLogActive = 36,                                                                                   // DB_WRN_LOG_ACTIVE
      TargetFileExist = 37,                                                                                 // DB_ERR_TARGET_FILE_EXIST
      FileIsView = 38,                                                                                      // DB_ERR_FILE_IS_VIEW
      CannotCopy = 39,                                                                                      // DB_ERR_CANOT_COPY
      Stop = 40,                                                                                            // DB_ERR_STOP
      StringBadName = 41,                                                                                   // DB_ERR_STR_BAD_NAME
      InsertIntoAll = 42,                                                                                   // DB_ERR_INSERT_INTO_ALL
      BadQuery = 43,                                                                                        // DB_ERR_BAD_QRY
      FilterAfterInsert = 44,                                                                               // DB_ERR_FILTER_AFTER_INSERT
      GetUserPasswordDst = 45,                                                                              // DB_GET_USER_PWD_DST
      WarnCacheTooBig = 46,                                                                                 // DB_WRN_CACHE_TOO_BIG
      LostRecord = 47,                                                                                      // DB_ERR_LOSTREC
      FileLocked = 48,                                                                                      // DB_ERR_FILE_LOCKED
      MaxConnection = 49,                // new                                                             // DB_ERR_MAX_CONN_EX
      DeadLock = 50,                                                                                        // DB_ERR_DEADLOCK
      BadCreate = 51,                  // tbl create fail                                                   // DB_ERR_BADCREATE
      FileNotExist = 52,                                                                                    // DB_ERR_FIL_NOT_EXIST
      Unused = 53,                     // instead of removed error. left here so that                       // DB_ERR_UNUSED
      // the gateways will be compatible                                                                    
      IndexCreateFail = 54,            // new                                                               // DB_ERR_IDX_CREATE_FAIL
      ConnectFail = 55,               // new                                                                // DB_ERR_CONNECT_FAIL
      Fatal = 56,                                                                                           // DB_ERR_FATAL
      InsertFail = 57,				 //not supposed to be in use                                               // DB_ERR_INSERT_FAIL
      DeleteFail = 58,				 //not supposed to be in use                                               // DB_ERR_DELETE_FAIL
      
      
      
                    
      /*-----------------------------------------------------------------*/
      /* ALL ERRORS that are not in the user list (in handler) should be */
      /* between FATAL AND IN_ERR_ZONE. all those ERRs could not be      */
      /* selected and handled specific. they will all be handled as ANY. */
      /* if an ERR should be handled as ANY, move it before the IN_ERR.  */
      /*-----------------------------------------------------------------*/
      // DB_ERR_LOGIN_PWD
      // not in error list and will not be handled by error handling.                                       // DB_ERR_NONE
      InErrorZone = 59,                                                                                     // DB_ERR_IN_ERR_ZONE

      NoRecord = 60,                                                                                        // DB_ERR_NOREC
      NotExist = 61,                                                                                        // DB_ERR_NOT_EXIST
      GetUserPassword = 62,                 //incorrect pass not in err hand.                               // DB_GET_USER_PWD
      WarnCancel = 63,                                                                                      // DB_WRN_CANCEL
      NotSupportedFunction = 64,                                                                            // DB_ERR_NOTSUPPORT_FUNC
      ModifyWithinTransaction = 65,                                                                         // DB_ERR_MODIFY_WITHIN_TRANS
      LoginPassword = 66,                                                                                   // DB_ERR_LOGIN_PWD
      DatasourceOpen = 67,
      DatasourceNotExist = 68,
      None = 69                     // to indicate a no error value for init.                               // DB_ERR_NONE
   }
}
