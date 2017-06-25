using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.util;
using com.magicsoftware.gatewaytypes;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.local.data.gateways;
using com.magicsoftware.richclient.local.data;
using System.Diagnostics;

namespace com.magicsoftware.richclient.local.ErrorHandling
{
   /// <summary>
   /// 
   /// </summary>
   internal class LocalErrorHandlerManager
   {
      Dictionary<GatewayErrorCode, ErrorHandlingInfo> GatewayErrorHandlingInfoTable = null;
      Dictionary<String, ErrorHandlingInfo> RtErrorHandlingInfoTable = null;

      /// <summary>
      /// local dataview manager
      /// </summary>
      internal LocalDataviewManager DataviewManager { get; set; }

      /// <summary>
      /// 
      /// </summary>
      internal LocalErrorHandlerManager()
      {
         InitErrorHandlingInfoTables();
      }

      /// 
      /// </summary>
      private void InitErrorHandlingInfoTables()
      {
          GatewayErrorHandlingInfoTable = new Dictionary<GatewayErrorCode, ErrorHandlingInfo>() 
                                        { 
                                        { GatewayErrorCode.TransactionAbort ,       new ErrorHandlingInfo { Quit = true}},
                                        { GatewayErrorCode.Fatal ,                  new ErrorHandlingInfo { Quit = true}},
                                        { GatewayErrorCode.BadIni,                  new ErrorHandlingInfo { Quit = true}},
                                        { GatewayErrorCode.BadName,                 new ErrorHandlingInfo { Quit = true}},
                                        { GatewayErrorCode.StringBadName,           new ErrorHandlingInfo { }},
                                        { GatewayErrorCode.BadCreate,               new ErrorHandlingInfo { Quit = true}},
                                        { GatewayErrorCode.Damaged,                 new ErrorHandlingInfo { Quit = true}},
                                        { GatewayErrorCode.RecordLocked,            new ErrorHandlingInfo { }},
                                        { GatewayErrorCode.RecordLockedNow,         new ErrorHandlingInfo { }},
                                        { GatewayErrorCode.RecordLockedNoBuf,       new ErrorHandlingInfo { }},
                                        { GatewayErrorCode.Unlocked,                new ErrorHandlingInfo { }},
                                        { GatewayErrorCode.BadOpen,                 new ErrorHandlingInfo { Quit = true}},
                                        { GatewayErrorCode.BadClose,                new ErrorHandlingInfo { Quit = true}},                                        
                                        { GatewayErrorCode.FileLocked,              new ErrorHandlingInfo { }},                                        
                                        { GatewayErrorCode.ResourceLocked,          new ErrorHandlingInfo { }},
                                        { GatewayErrorCode.NoDef,                   new ErrorHandlingInfo { }},
                                        { GatewayErrorCode.DuplicateKey,            new ErrorHandlingInfo { }},
                                        { GatewayErrorCode.ReadOnly,                new ErrorHandlingInfo { DisplayType = VerifyDisplay.Box}},
                                        { GatewayErrorCode.DeadLock,                new ErrorHandlingInfo { }},
                                        { GatewayErrorCode.TransactionCommit,       new ErrorHandlingInfo { }},
                                        { GatewayErrorCode.TransactionOpen,         new ErrorHandlingInfo { }},
                                        { GatewayErrorCode.BadDef,                  new ErrorHandlingInfo { Quit = true}},
                                        { GatewayErrorCode.InvalidOwner,            new ErrorHandlingInfo { Quit = true}},
                                        { GatewayErrorCode.ClearOwnerFail,          new ErrorHandlingInfo { Quit = true}},
                                        { GatewayErrorCode.AlterTable,              new ErrorHandlingInfo { }},
                                        { GatewayErrorCode.SortTable,               new ErrorHandlingInfo { }},
                                        { GatewayErrorCode.CannotRemove,            new ErrorHandlingInfo { Quit = true}},
                                        { GatewayErrorCode.CannotRename,            new ErrorHandlingInfo { Quit = true}},
                                        { GatewayErrorCode.BadSqlCommand,           new ErrorHandlingInfo { }},
                                        { GatewayErrorCode.BadQuery,                new ErrorHandlingInfo { }},
                                        { GatewayErrorCode.Stop,                    new ErrorHandlingInfo { }},
                                        { GatewayErrorCode.ExecSql,                 new ErrorHandlingInfo { }},
                                        { GatewayErrorCode.UpdateFail,              new ErrorHandlingInfo { }},
                                        { GatewayErrorCode.InsertFail,              new ErrorHandlingInfo { }},
                                        { GatewayErrorCode.DeleteFail,              new ErrorHandlingInfo { }},
                                        { GatewayErrorCode.WarnCacheTooBig,         new ErrorHandlingInfo { Quit = true}},
                                        { GatewayErrorCode.NoRowsAffected,          new ErrorHandlingInfo { }},
                                        { GatewayErrorCode.TargetFileExist,         new ErrorHandlingInfo { }},
                                        { GatewayErrorCode.FileIsView,              new ErrorHandlingInfo { Quit = true}},
                                        

                                        { GatewayErrorCode.FileNotExist,            new ErrorHandlingInfo { }},
                                        { GatewayErrorCode.CannotCopy,              new ErrorHandlingInfo { }},
                                        { GatewayErrorCode.MaxConnection,           new ErrorHandlingInfo { }},
                                        { GatewayErrorCode.ConstraintFail,          new ErrorHandlingInfo { }},
                                        { GatewayErrorCode.TriggerFail,             new ErrorHandlingInfo { }},


                                        { GatewayErrorCode.NotExist,                new ErrorHandlingInfo { }},
                                        { GatewayErrorCode.ModifyWithinTransaction, new ErrorHandlingInfo { DisplayType = VerifyDisplay.Box }},
                                        { GatewayErrorCode.LoginPassword,           new ErrorHandlingInfo { }},
                                        { GatewayErrorCode.DatasourceOpen,          new ErrorHandlingInfo { }},
                                        { GatewayErrorCode.DatasourceNotExist,      new ErrorHandlingInfo { }},
                                        { GatewayErrorCode.GetUserPassword,         new ErrorHandlingInfo { Quit = true}},

                                        };

          RtErrorHandlingInfoTable = new Dictionary<String, ErrorHandlingInfo>()
                                        { 
                                        { MsgInterface.RT_STR_NON_MODIFIABLE ,            new ErrorHandlingInfo { }},
                                        { MsgInterface.FMERROR_STR_BAD_LOCK_OPEN ,        new ErrorHandlingInfo { }},
                                        { MsgInterface.FMERROR_STR_DB_PROT ,              new ErrorHandlingInfo { Quit = true}},
                                        { MsgInterface.FMERROR_STR_NO_DATABASE ,          new ErrorHandlingInfo { Quit = true}},
                                        { MsgInterface.FMERROR_STR_EXTUSE_MULU_CNFLCT ,   new ErrorHandlingInfo { Quit = true}},
                                        { MsgInterface.FMERROR_STR_DB_GW_VERSION_CNFLCT , new ErrorHandlingInfo { Quit = true}},
                                        { MsgInterface.FMERROR_STR_UNUSABLE_FILE ,        new ErrorHandlingInfo { }},
                                        { MsgInterface.FMERROR_STR_NO_HDLS ,              new ErrorHandlingInfo { Quit = true}},
                                        { MsgInterface.FMERROR_STR_REOPEN ,               new ErrorHandlingInfo { Quit = true}},
                                        { MsgInterface.FMERROR_STR_TRANS_OPEN_FAILED,     new ErrorHandlingInfo { }},
                                        {MsgInterface.RT_STR_NO_RECS_IN_RNG,              new ErrorHandlingInfo { Quit = true}},
                                        {MsgInterface.LOCATE_STR_ERR_EOF,                 new ErrorHandlingInfo {}},
                                        {MsgInterface.STR_DATAVIEW_TO_DATASOURCE_OPERATION_FAILED,  new ErrorHandlingInfo {DisplayType = VerifyDisplay.None}},
                                        {MsgInterface.STR_CLIENT_DB_DEL_OPERATION_FAILED, new ErrorHandlingInfo {DisplayType = VerifyDisplay.None}},
                                        {MsgInterface.STR_CLIENT_DB_DISCONNECT_DATASOURCE_OPEN, new ErrorHandlingInfo {DisplayType = VerifyDisplay.Status}},
                                        {MsgInterface.STR_CLIENT_DB_DISCONNECT_DATASOURCE_NOT_EXIST, new ErrorHandlingInfo {DisplayType = VerifyDisplay.Status}},
                                         };
      }

      ///// <summary>
      ///// return true if this error need to be handled
      ///// </summary>
      ///// <param name="returnResultBase"></param>
      ///// <returns></returns>
      //private bool ErroNeedToBeHandled(ReturnResultBase returnResultBase)
      //{
      //   Boolean isErroNeedToBeHandled = false;

      //   ReturnResultBase innerResult = returnResultBase.InnerResult;

      //   if (innerResult is GatewayResult)
      //   {
      //      GatewayResult GatewayResult = innerResult as GatewayResult;
      //      // macro ERROR_TO_BE_HANDLED
      //      isErroNeedToBeHandled = ((GatewayErrorCode)innerResult.GetErrorId() < GatewayErrorCode.InErrorZone);
      //   }
      //   else if (innerResult is ReturnResult)
      //   {
      //      ReturnResult ReturnResult = innerResult as ReturnResult;
      //      isErroNeedToBeHandled = (((String)innerResult.GetErrorId()).Equals(MsgInterface.FMERROR_STR_DB_PROT)) ||
      //         (((String)innerResult.GetErrorId()).Equals(MsgInterface.FMERROR_STR_NO_DATABASE)) ||
      //         (((String)innerResult.GetErrorId()).Equals(MsgInterface.FMERROR_STR_EXTUSE_MULU_CNFLCT)) ||
      //         (((String)innerResult.GetErrorId()).Equals(MsgInterface.FMERROR_STR_DB_GW_VERSION_CNFLCT)) ||
      //         (((String)innerResult.GetErrorId()).Equals(MsgInterface.FMERROR_STR_NO_HDLS)) ||
      //         (((String)innerResult.GetErrorId()).Equals(MsgInterface.FMERROR_STR_TRANS_OPEN_FAILED) ||
      //         (((String)innerResult.GetErrorId()).Equals(MsgInterface.RT_STR_NON_MODIFIABLE))
      //         );
      //   }
      //   return isErroNeedToBeHandled;
      //}
      /// <summary>
      /// 
      /// </summary>
      /// <param name="returnResultBase"></param>
      internal ErrorHandlingInfo HandleResult(ReturnResultBase returnResultBase)
      {
         VerifyDisplay displayType = VerifyDisplay.Box;

         ErrorHandlingInfo errorHandlingInfo = GetErrorInfo(returnResultBase);

         if (errorHandlingInfo != null)
         {
            // The "if ErroNeedToBeHandled" code need to be un comment while implement the error handling (rtry_handle_error)
            // mg_rtry.cpp "ERROR_TO_BE_HANDLED" 
            //if (ErroNeedToBeHandled(returnResultBase))

            // take the display type
            displayType = errorHandlingInfo.DisplayType;
            // if the error is quit then we display it to message box otherwise display to status bar(the default)
            if (errorHandlingInfo.Quit)
               displayType = VerifyDisplay.Box;

            switch (displayType)
            {
               case VerifyDisplay.Box:
                  DataviewManager.Task.ShowError(returnResultBase.ErrorDescription);
                  break;
               case VerifyDisplay.Status:
                  DataviewManager.Task.DisplayMessageToStatusBar(returnResultBase.ErrorDescription);
                  break;
               default:
                  break;
            }
         }
         return errorHandlingInfo;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="gatewayResult"></param>
      public ErrorHandlingInfo GetErrorInfo(ReturnResultBase returnResultBase)
      {
         ErrorHandlingInfo errorHandlingInfo = null;


         if (returnResultBase.InnerResult is GatewayResult)
            GatewayErrorHandlingInfoTable.TryGetValue((GatewayErrorCode)returnResultBase.InnerResult.GetErrorId(), out errorHandlingInfo);
         else if (returnResultBase.InnerResult is ReturnResult)
            RtErrorHandlingInfoTable.TryGetValue((string)returnResultBase.InnerResult.GetErrorId(), out errorHandlingInfo);
         else if(returnResultBase.InnerResult == null)
            RtErrorHandlingInfoTable.TryGetValue((string)returnResultBase.GetErrorId(), out errorHandlingInfo);
         else
            Debug.Assert(false);

         return errorHandlingInfo;
      }
   }
}
