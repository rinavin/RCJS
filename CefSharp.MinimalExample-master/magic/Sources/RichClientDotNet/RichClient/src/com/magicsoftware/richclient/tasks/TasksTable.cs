using System;
using System.Text;
using com.magicsoftware.util;
using com.magicsoftware.unipaas.util;
using util.com.magicsoftware.util;

namespace com.magicsoftware.richclient.tasks
{
   internal class TasksTable
   {
      private readonly MgArrayList _tasks;

      /// <summary>
      ///   CTOR
      /// </summary>
      internal TasksTable()
      {
         _tasks = new MgArrayList();
      }

      /// <summary>
      ///   parse a set of tasks
      /// </summary>
      /// <param name = "mgdata">to parent -> MGData </param>
      /// <param name="openingTaskDetails">additional information of opening task</param>
      internal void fillData(MGData mgdata, OpeningTaskDetails openingTaskDetails)
      {
         while (initInnerObjects(ClientManager.Instance.RuntimeCtx.Parser.getNextTag(), mgdata, openingTaskDetails))
         {
         }
      }

      /// <summary>
      ///   Start task,parsing
      /// </summary>
      private bool initInnerObjects(String foundTagName, MGData mgdata, OpeningTaskDetails openingTaskDetails)
      {
         if (foundTagName == null)
            return false;

         if (foundTagName.Equals(XMLConstants.MG_TAG_TASK))
         {
            var task = new Task();
            ClientManager.Instance.TasksNotStartedCount++;
            _tasks.Add(task);
            task.fillData(mgdata, openingTaskDetails);
         }
         else
            return false;
         return true;
      }

      /// <summary>
      ///   add a new task to the table
      /// </summary>
      /// <param name = "task">the new task to add </param>
      internal void addTask(Task task)
      {
         _tasks.Add(task);
      }

      /// <summary>
      ///   removes a task from the table
      /// </summary>
      /// <param name = "task">a reference to the task to remove </param>
      internal void removeTask(Task task)
      {
         _tasks.Remove(task);
      }

      /// <summary>
      ///   get a task by its Id
      /// </summary>
      /// <param name = "tasktag">the requested task id (current id or original id)</param>
      internal Task getTask(String tasktag)
      {
         for (int i = 0; i < _tasks.Count; i++)
         {
            var task = (Task) _tasks[i];
            if (tasktag == task.getTaskTag())
               return task;
         }
         return null;
      }

      /// <summary>
      ///   get a task by its index
      /// </summary>
      /// <param name = "idx">task index in the table</param>
      internal Task getTask(int idx)
      {
         if (idx >= 0 && idx < _tasks.Count)
            return (Task) _tasks[idx];
         return null;
      }

      /// <summary>
      ///   get the number of tasks in the table
      /// </summary>
      internal int getSize()
      {
         return _tasks.Count;
      }

      /// <summary>
      ///   build the XML string for the tasks in the table
      /// </summary>
      /// <param name = "message">the xml message to append to </param>
      internal void buildXML(StringBuilder message)
      {
         for (int i = 0; i < getSize(); i++)
            getTask(i).buildXML(message);
      }

      /// <summary>
      ///   set task of event
      /// </summary>
      /// <param name = "task"></param>
      /// <param name = "index"> </param>
      internal void setTaskAt(Task task, int index)
      {
         // ensure that the vector size is sufficient
         if (_tasks.Count <= index)
            _tasks.SetSize(index + 1);
         _tasks[index] = task;
      }
   }
}