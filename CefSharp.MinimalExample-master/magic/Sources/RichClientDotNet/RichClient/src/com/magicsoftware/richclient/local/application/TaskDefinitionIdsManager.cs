using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.unipaas;
using com.magicsoftware.util;
using com.magicsoftware.richclient.util;
using com.magicsoftware.unipaas.util;
using System.Diagnostics;
using com.magicsoftware.unipaas.management.tasks;
using util.com.magicsoftware.util;
using com.magicsoftware.util.Xml;
using com.magicsoftware.richclient.sources;
#if PocketPC
using com.magicsoftware.richclient.mobile.util;
#endif

namespace com.magicsoftware.richclient.local.application
{
   /// <summary>
   /// include all task definition ids for all tasks with offline enable = true
   /// </summary>
   internal class TaskDefinitionIdsManager
   {
      private static readonly Dictionary<TaskDefinitionId, TaskDefinitionIdInfo> _TaskDefinitionIdsCache = new Dictionary<TaskDefinitionId, TaskDefinitionIdInfo>();

      internal TaskDefinitionIdsManager()
      {
      }

      /// <summary>
      /// add task definition id
      /// </summary>
      /// <param name="taskDefinitionId"></param>
      /// <param name="xmlId"></param>
      /// <param name="defaultTagList"></param>
      /// <param name="executionRightIdx"></param>
      internal void AddTaskDefinitionId(TaskDefinitionId taskDefinitionId, string xmlId, string defaultTagList, int executionRightIdx)
      {
         lock (_TaskDefinitionIdsCache)
         {
            TaskDefinitionIdInfo taskDefinitionIdInfo = new TaskDefinitionIdInfo(xmlId, defaultTagList, executionRightIdx);
            _TaskDefinitionIdsCache.Add(taskDefinitionId, taskDefinitionIdInfo);
            
            // get the task's file from server to client
            ApplicationSourcesManager.GetInstance().ReadSource(xmlId, false, false);
         }
      }

      /// <summary>
      ///   fill the file mapping when passed through the cache: placed into the cache by the runtime-engine, passed
      ///   as '<fileurl val = "/..."'
      /// </summary>
      /// <param name = "TAG_URL"></param>
      internal void FillFromUrl(String tagName)
      {
         XmlParser parser = ClientManager.Instance.RuntimeCtx.Parser;
         String XMLdata = parser.getXMLdata();
         int endContext = parser.getXMLdata().IndexOf(XMLConstants.TAG_TERM, parser.getCurrIndex());
         if (endContext != -1 && endContext < XMLdata.Length)
         {
            // find last position of its tag
            String tagAndAttributes = parser.getXMLsubstring(endContext);
            parser.add2CurrIndex(tagAndAttributes.IndexOf(tagName) + tagName.Length);

            List<String> tokensVector = XmlParser.getTokens(parser.getXMLsubstring(endContext), XMLConstants.XML_ATTR_DELIM);
            Debug.Assert((tokensVector[0]).Equals(XMLConstants.MG_ATTR_VALUE));
            String cachedTaskDefinitionIdsUrl = (tokensVector[1]);

            byte[] content = ApplicationSourcesManager.GetInstance().ReadSource(cachedTaskDefinitionIdsUrl, true);
            if (tagName.Equals(ConstInterface.MG_TAG_TASKDEFINITION_IDS_URL))
               ClientManager.Instance.LocalManager.ApplicationDefinitions.TaskDefinitionIdsManager.FillFrom(content);

            endContext = XMLdata.IndexOf(XMLConstants.TAG_OPEN, endContext);
            if (endContext != -1)
               parser.setCurrIndex(endContext);
         }
      }

      /// <summary>
      /// get the file name
      /// </summary>
      /// <param name="taskDefinitionId"></param>
      /// <returns></returns>
      internal string GetXmlId(TaskDefinitionId taskDefinitionId)
      {
         if (_TaskDefinitionIdsCache.ContainsKey(taskDefinitionId))
         {
            TaskDefinitionIdInfo TaskDefinitionIdInfo = _TaskDefinitionIdsCache[taskDefinitionId];
            return TaskDefinitionIdInfo.XmlId;
         }
         else
            return null;
      }

      /// <summary>
      /// get the default tag list
      /// </summary>
      /// <param name="taskDefinitionId"></param>
      /// <returns></returns>
      internal string GetDefaultTagList(TaskDefinitionId taskDefinitionId)
      {
         TaskDefinitionIdInfo TaskDefinitionIdInfo = _TaskDefinitionIdsCache[taskDefinitionId];
         return TaskDefinitionIdInfo.DefaultTagList;
      }

      ///<summary> get executionRightIdx </summary>
      ///<param name="taskDefinitionId">!!.</param>
      ///<returns>!!.</returns>
      internal int GetExecutionRight(TaskDefinitionId taskDefinitionId)
      {
         Debug.Assert(IsOfflineTask(taskDefinitionId));

         TaskDefinitionIdInfo TaskDefinitionIdInfo = _TaskDefinitionIdsCache[taskDefinitionId];
         return TaskDefinitionIdInfo.ExecutionRightIdx;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="fontxml"></param>
      internal void FillFrom(byte[] taskUrlxml)
      {
         //Clear the task definition's id table before filling in new values.
         if (_TaskDefinitionIdsCache.Count > 0)
            _TaskDefinitionIdsCache.Clear();

         if (taskUrlxml != null)
         {
            TaskInfoTableSaxHandler handler = new TaskInfoTableSaxHandler(AddTaskDefinitionId);
            handler.parse(taskUrlxml);
         }
      }

      /// <summary>
      /// checks if a task, identified by taskId, is an off-line task, by checking if it exists in the task IDs cache
      /// </summary>
      /// <param name="taskId"></param>
      /// <returns></returns>
      internal bool IsOfflineTask(TaskDefinitionId taskId)
      {
         return _TaskDefinitionIdsCache.ContainsKey(taskId);
      }

      /// <summary> returns TaskDefinitionId of a MainProgram of cltIdx</summary>
      /// <param name="ctlIdx"></param>
      /// <returns></returns>
      internal TaskDefinitionId GetMainPrgTaskDefinitionIdByCtlIdx(int ctlIdx)
      {
         TaskDefinitionId taskDefinitionId = null;

         foreach (TaskDefinitionId taskDefinitionKey in _TaskDefinitionIdsCache.Keys)
         {
            if (taskDefinitionKey.CtlIndex == ctlIdx && taskDefinitionKey.IsMainProgram())
            {
               taskDefinitionId = taskDefinitionKey;
               break;
            }
         }

         return taskDefinitionId;
      }

      ///<summary>
      ///  Check whether the task can be executed or not.
      ///  Currently this method check whether user is authorized to execute the task or not.
      ///</summary>
      ///<param name="taskDefinitionId">!!.</param>
      ///<returns>true - if task can be executed.</returns>
      internal bool CanExecuteTask(TaskDefinitionId taskDefinitionId)
      {
         bool canExecute = true;

         if (taskDefinitionId.IsProgram) // Rights shouldn't be checked for SubTasks.
         {
            // Get the task's execution right and if it is not available then get GlobalExecution rights from MP.
            int executionRightIdx = GetExecutionRight(taskDefinitionId);
            if (executionRightIdx <= 0)
            {
               TaskDefinitionId mainPrgTaskDefinitionId = GetMainPrgTaskDefinitionIdByCtlIdx(taskDefinitionId.CtlIndex);
               executionRightIdx = GetExecutionRight(mainPrgTaskDefinitionId);
            }

            if (executionRightIdx > 0)
               canExecute = ClientManager.Instance.getUserRights().getRight(taskDefinitionId.CtlIndex, executionRightIdx);
         }

         return canExecute;
      }

      /// <summary>
      /// 
      /// </summary>
      private class TaskDefinitionIdInfo
      {
         internal string DefaultTagList { get; set; }

         internal string XmlId { get; set; }

         internal int ExecutionRightIdx { get; set; }

         internal TaskDefinitionIdInfo(string xmlId, string defaultTagList, int executionRightIdx)
         {
            this.XmlId = xmlId;
            this.DefaultTagList = defaultTagList;
            this.ExecutionRightIdx = executionRightIdx;
         }
      }
   }
}
