using System;
using System.Collections.Generic;
using com.magicsoftware.richclient.util;
using com.magicsoftware.util;
using com.magicsoftware.gatewaytypes;

namespace com.magicsoftware.richclient.local.data.gateways
{
   public class GatewayResult : ReturnResultBase
   {
      public GatewayErrorCode ErrorCode { get; set; }
      private static string defaultString = string.Empty;

      /// <summary>
      /// parameters for error display
      /// </summary>
      internal object[] ErrorParams { get; set; } 

      /// <summary>
      /// CTOR
      /// </summary>
      internal GatewayResult()
      {
         ErrorCode = GatewayErrorCode.Any;
      }

      /// <summary>
      /// 
      /// </summary>
      internal override bool Success { get { return ErrorCode == GatewayErrorCode.Any; } }

      /// <summary>
      /// get the error code
      /// </summary>
      /// <returns></returns>
      internal override  Object GetErrorId()
      {
         return ErrorCode; 
      }

      /// <summary>
      /// get the error description from the gateway error code
      /// </summary>
      internal override string ErrorDescription
      {
         get
         {
            //temporary 
            return ErrorParams == null ? GetErrorCodeDesciptionString() : String.Format("{0} {1} \r\n {2} ", GetErrorCodeDesciptionString(), ErrorParams[0], ErrorParams[1]); ;
         }
      }


      /// <summary>
      /// translate error code to string
      /// </summary>
      /// <returns></returns>
      private string GetErrorCodeDesciptionString()
      {
         string errorMessage = ClientManager.Instance.getMessageString(ErrorCodeToMsgInterfaceId(ErrorCode));

         if (String.IsNullOrEmpty(errorMessage))
            errorMessage = "No error string defined for error " + ErrorCode.ToString();

         return errorMessage;
      }

      static private Dictionary<GatewayErrorCode, string> errorsDictionary = new Dictionary<GatewayErrorCode, string> { 
         { GatewayErrorCode.BadIni,                   MsgInterface.FMERROR_STR_BAD_DB_INIT_FAILED }, 
         { GatewayErrorCode.BadName,                  MsgInterface.FMERROR_STR_BAD_NAME },
         { GatewayErrorCode.BadCreate,                MsgInterface.FMERROR_STR_BAD_BADCREATE },
         { GatewayErrorCode.Damaged,                  MsgInterface.FMERROR_STR_BAD_DAMAGED },
         { GatewayErrorCode.RecordLocked,             MsgInterface.FMERROR_STR_REC_LOCKED },
         { GatewayErrorCode.RecordLockedNow,          MsgInterface.FMERROR_STR_REC_LOCKED_NOW },
         { GatewayErrorCode.RecordLockedNoBuf,        MsgInterface.FMERROR_STR_REC_LOCKED_NOBUF },
         { GatewayErrorCode.Unlocked,                 MsgInterface.FMERROR_STR_UNLOCKED },
         { GatewayErrorCode.BadOpen,                  MsgInterface.FMERROR_STR_BAD_BADOPEN },
         { GatewayErrorCode.BadClose,                 MsgInterface.FMERROR_STR_BAD_BADCLOSE },
         { GatewayErrorCode.FileLocked,               MsgInterface.FMERROR_STR_FILE_LOCKED },
         { GatewayErrorCode.ResourceLocked,           MsgInterface.FMERROR_STR_RSRC_LOCKED },
         { GatewayErrorCode.NoDef,                    MsgInterface.FMERROR_STR_NODEF },
         { GatewayErrorCode.DuplicateKey,             MsgInterface.FMERROR_STR_DUP_KEY },
         { GatewayErrorCode.ReadOnly,                 MsgInterface.FMERROR_STR_READONLY },
         { GatewayErrorCode.DeadLock,                 MsgInterface.FMERROR_STR_DEADLOCK },
         { GatewayErrorCode.TransactionCommit,        MsgInterface.FMERROR_STR_COMMIT },
         { GatewayErrorCode.TransactionOpen,          MsgInterface.FMERROR_STR_TRANS_OPEN },
         { GatewayErrorCode.BadDef,                   MsgInterface.FMERROR_STR_BAD_BADDEF },
         { GatewayErrorCode.InvalidOwner,             MsgInterface.FMERROR_STR_INVALID_OWNR },
         { GatewayErrorCode.ClearOwnerFail,           MsgInterface.FMERROR_STR_CLR_OWNR_FAIL },
         { GatewayErrorCode.AlterTable,               MsgInterface.FMERROR_STR_DBMS_ALTER_FAIL },
         { GatewayErrorCode.SortTable,                MsgInterface.FMERROR_STR_DBMS_SORT_FAIL },
         { GatewayErrorCode.CannotRemove,             MsgInterface.FMERROR_STR_DB_CANOT_REMOVE },
         { GatewayErrorCode.CannotRename,             MsgInterface.FMERROR_STR_DB_CANOT_RENAME },
         { GatewayErrorCode.BadSqlCommand,            MsgInterface.FMERROR_STR_DB_BAD_SQL_CMD },
         { GatewayErrorCode.BadQuery,                 MsgInterface.FMERROR_STR_DB_BAD_QUERY },
         { GatewayErrorCode.Stop,                     MsgInterface.FMERROR_STR_ERR_EXEC_SQL },
         { GatewayErrorCode.ExecSql,                  MsgInterface.FMERROR_STR_ERR_EXEC_SQL },
         { GatewayErrorCode.UpdateFail,               MsgInterface.FMERROR_STR_ERR_UPDATE_FAIL },
         { GatewayErrorCode.InsertFail,               MsgInterface.FMERROR_STR_ERR_INSERT_FAIL },
         { GatewayErrorCode.DeleteFail,               MsgInterface.FMERROR_STR_ERR_DELETE_FAIL },
         { GatewayErrorCode.NotExist,                 MsgInterface.FMERROR_STR_FIL_NOT_EXIST },
         { GatewayErrorCode.WarnCacheTooBig,          MsgInterface.FMERROR_STR_CACHE_TOO_BIG },
         { GatewayErrorCode.NoRowsAffected,           MsgInterface.FMERROR_STR_NO_ROWS_AFFECTED },
         { GatewayErrorCode.TargetFileExist,          MsgInterface.FMERROR_STR_TARGET_FILE_EXIST },
         { GatewayErrorCode.FileIsView,               MsgInterface.FMERROR_STR_FILE_IS_VIEW },
         { GatewayErrorCode.FileNotExist,             MsgInterface.FMERROR_STR_FIL_NOT_EXIST },
         { GatewayErrorCode.CannotCopy,               MsgInterface.FMERROR_STR_DB_CANOT_COPY },
         { GatewayErrorCode.StringBadName,            MsgInterface.FMERROR_STR_BAD_NAME },
         { GatewayErrorCode.MaxConnection,            MsgInterface.FMERROR_STR_MAX_CONNS_REACHED },
         { GatewayErrorCode.ConstraintFail,           MsgInterface.FMERROR_STR_CONSTRAINT_FAIL },
         { GatewayErrorCode.TriggerFail,              MsgInterface.FMERROR_STR_TRIGGER_FAIL },
         { GatewayErrorCode.ModifyWithinTransaction,  MsgInterface.FMERROR_STR_MODIFY_WITHIN_TRANS },
         { GatewayErrorCode.LoginPassword,            MsgInterface.FMERROR_STR_BAD_LOGIN },
         { GatewayErrorCode.RecordUpdated,            defaultString},
         { GatewayErrorCode.Unmapped,                 defaultString},
         { GatewayErrorCode.WarnRetry,                defaultString},
         { GatewayErrorCode.RecordLockedMagic,        defaultString},
         { GatewayErrorCode.WarnCreated,              defaultString},
         { GatewayErrorCode.Capacity,                 defaultString},
         { GatewayErrorCode.TransactionAbort,         defaultString},
         { GatewayErrorCode.WarnLogActive,            defaultString},
         { GatewayErrorCode.InsertIntoAll,            defaultString},
         { GatewayErrorCode.FilterAfterInsert,        defaultString},
         { GatewayErrorCode.GetUserPasswordDst,       defaultString},
         { GatewayErrorCode.LostRecord,               defaultString},
         { GatewayErrorCode.IndexCreateFail,          defaultString},
         { GatewayErrorCode.ConnectFail,              defaultString},
         { GatewayErrorCode.Fatal,                    defaultString},
         { GatewayErrorCode.InErrorZone,              defaultString},
         { GatewayErrorCode.NoRecord,                 defaultString},
         { GatewayErrorCode.GetUserPassword,          MsgInterface.FMERROR_STR_INVALID_PASSWORD},
         { GatewayErrorCode.WarnCancel,               defaultString},
         { GatewayErrorCode.NotSupportedFunction,     defaultString},
         { GatewayErrorCode.DatasourceOpen,           MsgInterface.STR_CLIENT_DB_DISCONNECT_DATASOURCE_OPEN},
         { GatewayErrorCode.DatasourceNotExist,       MsgInterface.STR_CLIENT_DB_DISCONNECT_DATASOURCE_NOT_EXIST},
         { GatewayErrorCode.Any,                      defaultString},                  
      };

      /// <summary>
      /// find the string ID corresponding to the gateway error code
      /// </summary>
      /// <param name="errorCode"></param>
      /// <returns></returns>
      private string ErrorCodeToMsgInterfaceId(GatewayErrorCode errorCode)
      {
         return errorsDictionary[errorCode];
      }
   }
}
