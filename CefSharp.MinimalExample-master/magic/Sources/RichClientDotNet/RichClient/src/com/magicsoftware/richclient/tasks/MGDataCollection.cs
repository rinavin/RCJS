using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.data;
using com.magicsoftware.richclient.gui;
using com.magicsoftware.richclient.util;
using com.magicsoftware.unipaas.management.tasks;
using com.magicsoftware.unipaas.util;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.richclient.remote;
using System.Diagnostics;
using util.com.magicsoftware.util;
using com.magicsoftware.richclient.commands;
using com.magicsoftware.richclient.commands.ClientToServer;

namespace com.magicsoftware.richclient.tasks
{
   internal delegate void TaskDelegate(Task task, object extraData);

   /// <summary>
   ///   this class handles a table of MGData objects
   /// </summary>
   internal class MGDataCollection : IMGDataTable
   {
      private static MGDataCollection _instance; // singleton
      private readonly List<MGData> _mgDataTab = new List<MGData>();

      //the current MGDATA
      private int _iteratorMgdIdx;
      private int _iteratorTaskIdx;

      /// <summary>
      /// singleton
      /// </summary>
      private MGDataCollection()
      {
      }

      internal int currMgdID { get; set; }

      internal MGData StartupMgData { get; set; }

      #region IMGDataTable Members

      /// <summary>
      ///   searches all the MGData objects till it finds the task with the given id
      /// </summary>
      /// <param name = "id">the id of the requested task</param>
      public ITask GetTaskByID(String id)
      {
         Task task = null;

         for (int i = 0;
              i < getSize() && task == null;
              i++)
         {
            MGData mgd = getMGData(i);
            if (mgd == null)
               // the window connected to the MGData was closed
               continue;
            task = mgd.getTask(id);
         }

         return task;
      }

      #endregion

      /// <summary>
      /// singleton
      /// </summary>
      internal static MGDataCollection Instance
      {
         get
         {
            if (_instance == null)
            {
               lock (typeof(MGDataCollection))
               {
                  if (_instance == null)
                     _instance = new MGDataCollection();
               }
            }
            return _instance;
         }
      }

      /// <summary>
      ///   add MGData object to the table
      /// </summary>
      /// <param name = "mgd">the MGData object to add</param>
      /// <param name = "idx">the index within the table for the new MGData</param>
      internal void addMGData(MGData mgd, int idx, bool isStartup)
      {
         if (idx < 0 || idx > getSize())
            throw new ApplicationException("Illegal MGData requested");

         if (isStartup)
            StartupMgData = mgd;

         if (idx == getSize())
            _mgDataTab.Add(mgd);
         else
         {
            MGData oldMgd = getMGData(idx);
            if (oldMgd != null && !oldMgd.IsAborting)
            {
               oldMgd.getFirstTask().stop();
               oldMgd.abort();
            }
            _mgDataTab[idx] = mgd;
         }
      }

      /// <summary>
      ///   returns MGData object by its index
      /// </summary>
      internal MGData getMGData(int idx)
      {
         MGData mgd = null;
         if (idx >= 0 && idx < getSize())
            mgd = _mgDataTab[idx];
         return mgd;
      }

      /// <summary>
      ///   returns available index
      /// </summary>
      internal int getAvailableIdx()
      {
         int idx = 0;

         for (;
            idx < _mgDataTab.Count;
            idx++)
         {
            if (_mgDataTab[idx] == null)
               break;
         }

         return idx;
      }

      /// <summary>
      ///   returns the index of MGData in MGDataTable
      /// </summary>
      /// <param name = "mgd">to find into table</param>
      internal int getMgDataIdx(MGData mgd)
      {
         return _mgDataTab.IndexOf(mgd);
      }

      /// <summary>
      ///   free memory from unneeded MGData and its descendant MGDs
      /// </summary>
      /// <param name = "index">number of MGData/window to be deleted</param>
      /// <returns></returns>
      internal void deleteMGDataTree(int index)
      {
         MGData mgd, childMgd;
         int i;

         if (index < 0 || index >= getSize())
            throw new ApplicationException("in deleteMGData() illegal index: " + index);

         mgd = getMGData(index);
         if (mgd != null)
         {
            // TODO: when closing a frameset we should clear the MGDs of the frames
            // from the MGDTab
            if (index > 0 && mgd.getParentMGdata() != null)
            {
               _mgDataTab[index] = null;
               ClientManager.Instance.clean(index);
            }
            for (i = 0;
                 i < getSize();
                 i++)
            {
               childMgd = getMGData(i);
               if (childMgd != null && childMgd.getParentMGdata() == mgd)
                  deleteMGDataTree(i);
            }
         }
      }

      /// <summary>
      ///   get the current MGData
      /// </summary>
      /// <returns> MGData object</returns>
      internal MGData getCurrMGData()
      {
         return getMGData(currMgdID);
      }

      /// <summary>
      ///   searches all the MGData objects till it finds a main program with the given ctl idx
      /// </summary>
      /// <param name = "contextID"></param>
      /// <param name = "ctlIdx">the idx of the requested component</param>
      public ITask GetMainProgByCtlIdx(Int64 contextID, int ctlIdx)
      {
         Task task = null;

         for (int i = 0;
              i < getSize() && task == null;
              i++)
         {
            MGData mgd = getMGData(i);
            if (mgd == null)
               // the window connected to the MGData was closed
               continue;
            task = mgd.getMainProg(ctlIdx);
         }

         return task;
      }
      internal Task GetMainProgByCtlIdx(int ctlIdx)
      {
         return (Task)GetMainProgByCtlIdx(-1, ctlIdx); //TODO (-1): RC is unaware to context ids at the moment
      }

      /// <summary>
      ///   start the tasks iterator
      /// </summary>
      internal void startTasksIteration()
      {
         _iteratorMgdIdx = 0;
         _iteratorTaskIdx = 0;
      }

      /// <summary>
      ///   get the next task using the tasks iterator
      /// </summary>
      internal Task getNextTask()
      {
         Task task = null;

         MGData mgd = getMGData(_iteratorMgdIdx);
         if (mgd == null)
            return null;
         task = mgd.getTask(_iteratorTaskIdx);
         if (task == null)
         {
            do
            {
               _iteratorMgdIdx++;
            } while (_iteratorMgdIdx < getSize() && getMGData(_iteratorMgdIdx) == null);
            _iteratorTaskIdx = 0;
            return getNextTask();
         }
         _iteratorTaskIdx++;
         return task;
      }

      /// <summary>
      ///   build XML string of the MGDataTABLE object,
      ///   ALL MGData in the table
      /// </summary>
      /// <param name="message">a message being prepared.</param>
      /// <param name="serializeTasks">if true, tasks in the current execution will also be serialized.</param>
      internal void buildXML(StringBuilder message, Boolean serializeTasks)
      {
         for (int i = 0; i < getSize(); i++)
         {
            MGData mgd = getMGData(i);
            if (mgd != null && !mgd.IsAborting)
               mgd.buildXML(message, serializeTasks);
         }

         FlowMonitorQueue.Instance.buildXML(message);
      }

      /// <summary> Removes all the pending server commands </summary>
      internal void RemoveServerCommands()
      {
         for (int i = 0; i < getSize(); i++)
         {
            MGData mgd = getMGData(i);
            if (mgd != null)
               mgd.CmdsToServer.clear();
         }
      }

      /// <summary>
      ///   get size of MGData table
      /// </summary>
      /// <returns> size of table</returns>
      internal int getSize()
      {
         return _mgDataTab.Count;
      }

      /// <summary>
      ///   perform any data error recovery action on all dataviews
      /// </summary>
      internal void processRecovery()
      {
         for (int i = 0; i < getSize(); i++)
         {
            MGData mgd = getMGData(i);
            if (mgd != null)
            {
               for (int j = 0; j < mgd.getTasksCount(); j++)
               {
                  Task task = mgd.getTask(j);
                  if (task != null)
                     ((DataView)task.DataView).processRecovery();
               }
            }
         }
      }

      /// <summary>
      ///   find all tasks which were triggered by a specific task
      /// </summary>
      /// <param name = "triggeringTask">the task who triggered all other tasks.</param>
      /// <returns> vector containing all tasks which were triggered by the parameter task</returns>
      internal List<Task> getTriggeredTasks(Task triggeringTask)
      {
         var list = new List<Task>();
         Task task;
         String tag = triggeringTask.getTaskTag();

         startTasksIteration();
         while ((task = getNextTask()) != null)
            if (tag == task.PreviouslyActiveTaskId)
               list.Add(task);

         return list;
      }

      /// <summary>
      /// returns task list according to the predicate
      /// </summary>
      /// <param name="p"></param>
      /// <returns></returns>
      internal List<Task> GetTasks(Predicate<Task> p)
      {
         List<Task> taskList = new List<Task>();
         Task task;

         foreach (MGData mgd in _mgDataTab)
         {
            if (mgd == null || mgd.IsAborting)
               continue;

            for (int i = 0; i < mgd.getTasksCount(); i++)
            {
               task = mgd.getTask(i);
               if (task != null && p(task))
                  taskList.Add(task);
            }

         }
         return taskList;
      }


      /// <summary>
      ///   this method returns the current id of a task according to its original id
      ///   it will usually return the same id as sent to it, but for subform after error and retry it will
      ///   convert the original id to the current.
      /// </summary>
      /// <param name = "taskId">- original task id</param>
      /// <returns> the current taskid for this task</returns>
      internal String getTaskIdById(String taskId)
      {
         // QCR #980454 - the task id may change during the task's lifetime, so taskId might
         // be holding the old one - find the task and refetch its current ID.
         ITask task = GetTaskByID(taskId);
         String tag = taskId;

         if (task != null)
            tag = task.getTaskTag();

         return tag;
      }

      /// <summary>
      ///   return all top level forms that exist
      /// </summary>
      internal List<MgFormBase> GetTopMostForms()
      {
         Task task;
         var forms = new List<MgFormBase>();

         //move on the tasks and add the forms to the arrayList
         startTasksIteration();
         while ((task = getNextTask()) != null)
         {
            MgFormBase form = task.getForm();

            if (!task.isAborting() && form != null && !form.isSubForm())
               forms.Add(form);
         }

         return forms;
      }

      /// <summary> Returns if any of the MgData contains a non-Offline task.</summary>
      /// <returns></returns>
      internal bool ContainsNonOfflineProgram()
      {
         bool containsNonOfflineProgram = false;

         foreach (MGData mgData in _mgDataTab)
         {
            //TODO: Not sure if the loop should break on reaching a null MgData.
            containsNonOfflineProgram = (mgData != null && mgData.ContainsNonOfflineProgram());

            if (containsNonOfflineProgram)
               break;
         }

         return containsNonOfflineProgram;
      }

      /// <summary>
      /// Gets the MGData that holds/should hold the startup program.
      /// The method takes into consideration whether the application uses MDI or not.
      /// </summary>
      /// <returns></returns>
      internal MGData GetMGDataForStartupProgram()
      {
         Debug.Assert(_mgDataTab.Count > 0, "The main program must be processed before invoking this method.");
         
         Task mainProg = GetMainProgByCtlIdx(0);
         Debug.Assert(mainProg != null, "The main program must be processed before invoking this method.");

         Debug.Assert(mainProg.getMgdID() == 0, "Main program is expected to be on MGData 0. Is this an error?");

         // if the main program does not have MDI frame, the startup MGData index is 0.
         if (!mainProg.HasMDIFrame)
            return _mgDataTab[0];
         
         // If the main program has MDI, the startup MGData index is 1. If it already exists,
         // return it.
         if (_mgDataTab.Count >= 2)
            return _mgDataTab[1];

         // The startup MGData is 1, but it does not exist yet, so create it.
         MGData mgd = new MGData(1, null, false);
         addMGData(mgd, 1, true);
         return mgd;
      }

      /// <summary> Stops all the non-offline tasks and its children. </summary>
      internal void StopNonOfflineTasks()
      {
         for (int i = 0; i < _mgDataTab.Count; i++)
         {
            MGData mgData = _mgDataTab[i];
            if (mgData != null && !mgData.IsAborting)
            {
               Task firstTask = mgData.getFirstTask();
               if (!firstTask.isMainProg() && !firstTask.IsOffline)
                  StopTaskTree(mgData.getFirstTask());
            }
         }
      }

      /// <summary> Stops the specified task and all the tasks which are invoked by it. </summary>
      /// <param name="task"></param>
      /// <returns></returns>
      private bool StopTaskTree(Task task)
      {
         bool taskStopped = false;

         if (task.hasSubTasks())
         {
            for (int i = 0; i < task.SubTasks.getSize(); )
            {
               //If the task was not stopped, then only increment the counter, because, if 
               //the task was stopped, it is removed from task.SubTasks and so, the next 
               //task is available at the same index.
               if (!StopTaskTree(task.SubTasks.getTask(i)))
                  i++;
            }
         }

         //Do not execute abort command for the subform. Subform task are stopped 
         //when its container task will be aborted.
         if (!task.IsSubForm)
         {
            MGData mgData = task.getMGData();

            IClientCommand abortCommand = CommandFactory.CreateAbortCommand(task.getTaskTag());
            mgData.CmdsToClient.Add(abortCommand);

            // execute the command
            mgData.CmdsToClient.Execute(null);
            taskStopped = true;
         }

         return taskStopped;
      }

      /// <summary>
      /// execute the delegate for each tasks
      /// </summary>
      /// <param name="taskDelegate"></param>
      internal void ForEachTask(TaskDelegate taskDelegate, object extraData)
      {
         List<Task> tasks = new List<Task>();

         // create tasks array
         foreach (MGData mgData in _mgDataTab)
         {
            if (mgData != null)
            {          
               for (int i =0; i < mgData.getTasksCount(); i++)
               {
                  Task t = mgData.getTask(i);
                  tasks.Add(t);               
               }
            }
         }

         // execute the delegate for each task
         foreach (Task task in tasks)
            taskDelegate(task, extraData);
        
      }    
   }
}
