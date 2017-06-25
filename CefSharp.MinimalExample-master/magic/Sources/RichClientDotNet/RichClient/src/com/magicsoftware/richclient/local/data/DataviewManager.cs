using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.tasks;

using com.magicsoftware.util;
using com.magicsoftware.richclient.remote;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.local.data.gateways;
using com.magicsoftware.richclient.commands;

namespace com.magicsoftware.richclient.local.data
{
   /// <summary>
   /// dataview manager of the task
   /// </summary>
   internal class DataviewManager : DataviewManagerBase
   {
      internal RemoteDataviewManager RemoteDataviewManager { get; private set; }
      internal LocalDataviewManager LocalDataviewManager { get; private set; }
     

      internal bool HasLocalData { get; set; }
      internal bool HasRemoteData { get; set; }
      internal bool HasLocalLinks { get; set; }
      

      private TaskServiceBase TaskService
      {
         get
         {
            return ((Task)Task).TaskService;
         }
      }
      /// <summary>
      /// CTOR
      /// </summary>
      /// <param name="task"></param>
      internal DataviewManager(Task task)
         : base(task)
      {
         // for now we work connected only
         
         RemoteDataviewManager = new RemoteDataviewManager(task);
         LocalDataviewManager = new LocalDataviewManager(task){LocalManager = ClientManager.Instance.LocalManager};
      }

      // the current manager that active
      internal DataviewManagerBase CurrentDataviewManager 
      {
         get
         {
            if (HasRemoteData)
               return RemoteDataviewManager;

            if (HasLocalData)
               return LocalDataviewManager;
            
            return VirtualDataviewManager;
         }
      }

      /// <summary>
      /// used for handling tasks with vitrtuals only
      /// </summary>
       DataviewManagerBase VirtualDataviewManager
      {
         get
         {
            return TaskService.GetDataviewManagerForVirtuals(Task);

         }
      }

       /// <summary>
       /// !!
       /// </summary>
       /// <param name="command"></param>
       internal override ReturnResult Execute(IClientCommand command)
       {
          return CurrentDataviewManager.Execute(command);
       }
   
   }
}
