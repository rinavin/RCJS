using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.local.data.commands;
using com.magicsoftware.richclient.commands;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.local.data.gateways;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.gatewaytypes;
using com.magicsoftware.richclient.data;
using com.magicsoftware.richclient.commands.ClientToServer;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.unipaas;
using com.magicsoftware.util;
using System.Collections;
using Field = com.magicsoftware.richclient.data.Field;
using util.com.magicsoftware.util;
using com.magicsoftware.unipaas.management.exp;
using com.magicsoftware.unipaas.management.tasks;

namespace com.magicsoftware.richclient.local.data.commands
{
   /// <summary>
   /// local dataview command to provide the functionality of DataView output.
   /// </summary>
   class LocalDataViewOutputCommandBase : LocalDataViewCommandBase
   {
      
      DataViewOutputCommand command;
      
      private Task task;
      override protected Task Task
      {
         get { return task; }
      }

      private List<Field> selectedFields;
      internal List<Field> SelectedFields 
      { 
         get { return selectedFields; }
      }

      /// <summary>
      /// ctor
      /// </summary>
      /// <param name="command"></param>
      public LocalDataViewOutputCommandBase(DataViewOutputCommand dataViewOutputCommand)
         : base(dataViewOutputCommand)
      {
         command = dataViewOutputCommand;

         Task currtask = (Task)MGDataCollection.Instance.GetTaskByID(dataViewOutputCommand.TaskTag);
         task = (Task)GuiExpressionEvaluator.GetContextTask((TaskBase)currtask, dataViewOutputCommand.Generation);
      }

      /// <summary>
      /// 
      /// </summary>
      internal override ReturnResultBase Execute()
      {

         ReturnResult result = (ReturnResult)PrepareColumnsList();
         if (result.Success)
         {
            OnRecordFetchDelegate onRecordFetch = Serialize;

            Init();

            IClientCommand dataViewCommand = CommandFactory.CreateFetchAllDataViewCommand(Task.getTaskTag(), onRecordFetch);
            result = Task.DataviewManager.Execute(dataViewCommand);

            Terminate();
         }

         return result;
      }

      /// <summary>
      /// 
      /// </summary>
      internal virtual void Init() { }

      /// <summary>
      /// 
      /// </summary>
      internal virtual void Terminate() { }

      /// <summary>
      /// 
      /// </summary>
      internal virtual void Serialize(Record record) { }


      /// <summary>
      /// Prepares a columns list.
      /// </summary>
      private ReturnResultBase PrepareColumnsList()
      {
         ReturnResultBase result = new ReturnResult();
         selectedFields = new List<Field>();
         StringBuilder invalidVariableNames = new StringBuilder();
         String taskVarList = command.TaskVarNames;
         List<string> taskVariableNames;

         taskVarList = taskVarList.Trim();

         // Prepare column list of task variables
         var fieldsTable = Task.DataView.GetFieldsTab();

         taskVariableNames = CreateList(taskVarList); // task variables names.

         int i = 0;
         foreach (var taskVariableName in taskVariableNames)
         {
            bool variableFound = false;
            for (var j = 0; j < fieldsTable.getSize(); j++)
            {
               var field = (Field)fieldsTable.getField(j);
               if (taskVariableName == field.getVarName())
               {
                  if (field.getType() == StorageAttribute.BLOB_VECTOR || field.getType() == StorageAttribute.DOTNET)
                  {
                     variableFound = false;
                     break;
                  }
                  selectedFields.Add(field);
                  variableFound = true;
                  break;
               }
            }
            if (!variableFound)
            {
               invalidVariableNames.Append(taskVariableName);
               break;
            }

            i++;
         }

         //error handling
         if (selectedFields.Count < taskVariableNames.Count)
         {
            string error = String.Format("DataViewToDataSource - Illegal task variables specified : {0}", invalidVariableNames.ToString());
            Logger.Instance.WriteExceptionToLog(error);
            ClientManager.Instance.ErrorToBeWrittenInServerLog = error;
            result = new ReturnResult(MsgInterface.STR_DATAVIEW_TO_DATASOURCE_OPERATION_FAILED);
         }

         return result;
      }

      /// <summary>
      /// Parse a comma delimited string into a list.
      /// </summary>
      /// <param name="varStr">comma delimited string.</param>
      private List<String> CreateList(String varStr)
      {
         int i = 0;
         var retList = new List<String>();

         IEnumerator tokens = StrUtil.tokenize(varStr, ",").GetEnumerator();
         while (tokens.MoveNext())
         {
            var strToken = (String)tokens.Current;

            // if previous string ends with '\\', it means ',' is part of string & not a separator.
            if (retList.Count > 0 && retList[i - 1].EndsWith("\\"))
            {
               StringBuilder str = new StringBuilder();
               str.Append(retList[i - 1]);
               str.Remove(str.Length - 1, 1); //remove last char '\\'
               str.Append(","); //',' is part of string, so append it.
               str.Append(strToken);
               retList[i - 1] = str.ToString();
            }
            else
            {
               retList.Add(strToken);
               i++;
            }
         }
         return retList;
      }

   }
}
