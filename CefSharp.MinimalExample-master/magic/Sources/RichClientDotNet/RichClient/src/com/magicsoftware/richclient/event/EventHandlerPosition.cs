using com.magicsoftware.richclient.rt;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.tasks;
using Task = com.magicsoftware.richclient.tasks.Task;
using com.magicsoftware.util;
using util.com.magicsoftware.util;

namespace com.magicsoftware.richclient.events
{
   /// <summary>
   ///   an object of this class points to the position of an event handler in the
   ///   chain of the event handlers
   /// </summary>
   internal class EventHandlerPosition
   {
      // CONSTANTS
      private const int PHASE_CONTROL_SPECIFIC = 1;
      private const int PHASE_CONTROL_NON_SPECIFIC = 2;
      private const int PHASE_GLOBAL = 3;
      private const int PHASE_GLOBAL_SPECIFIC = 4;

      private int _handlerIdx; // the index of the handler within the task
      private HandlersTable _handlersTab;
      private Task _orgPrevTask; // reference to the previous checked task
      private Task _orgTask; // reference to the current checked task
      private int _phase; // the phase of the search: control specific, control non-specific, global
      private Task _prevTask; // reference to the previous checked task
      private RunTimeEvent _rtEvt; // the event
      private Task _task; // reference to the current checked task

      /// <summary>
      ///   CTOR
      /// </summary>
      internal EventHandlerPosition()
      {
      }

      /// <summary>
      ///   init the position to start a new chain of search
      /// </summary>
      internal void init(RunTimeEvent rtEvent)
      {
         _rtEvt = rtEvent;
         _task = _rtEvt.getTask();
         if (_task.isMainProg())
         {
            //phase = PHASE_CONTROL_NON_SPECIFIC;
            _prevTask = _rtEvt.getMainPrgCreator();
            if (_prevTask != null && _prevTask.isMainProg())
               _prevTask = null;
         }

         if (rtEvent.getType() == ConstInterface.EVENT_TYPE_USER_FUNC)
            _phase = PHASE_CONTROL_NON_SPECIFIC;
         else
         {
            if (rtEvent.getType() == ConstInterface.EVENT_TYPE_USER_FUNC)
               _phase = PHASE_CONTROL_NON_SPECIFIC;
            else
               _phase = PHASE_CONTROL_SPECIFIC;
         }

         _orgTask = _task;
         _orgPrevTask = _prevTask;

         _handlersTab = _task.getHandlersTab();
         if (_handlersTab == null)
            goUpTaskChain();
         _handlerIdx = -1;
      }

      /// <summary>
      ///   changes the current run time event and returns the next event handler
      /// </summary>
      protected internal EventHandler getNext(RunTimeEvent evt)
      {
         _rtEvt = evt;
         return getNext();
      }

      /// <summary>
      ///   returns the next event handler
      /// </summary>
      internal EventHandler getNext()
      {
         EventHandler handler;

         if (_handlersTab == null)
            return null;

         if (_rtEvt.getType() == ConstInterface.EVENT_TYPE_INTERNAL &&
             _rtEvt.getInternalCode() != InternalInterface.MG_ACT_VARIABLE)
         {
            // special treatment for TASK, RECORD and CONTROL level Prefix/Suffix events
            switch (_rtEvt.getInternalCode())
            {
               case InternalInterface.MG_ACT_TASK_PREFIX:
               case InternalInterface.MG_ACT_TASK_SUFFIX:
               case InternalInterface.MG_ACT_REC_PREFIX:
               case InternalInterface.MG_ACT_REC_SUFFIX:
               case InternalInterface.MG_ACT_CTRL_PREFIX:
               case InternalInterface.MG_ACT_CTRL_SUFFIX:
               case InternalInterface.MG_ACT_CTRL_VERIFICATION:

                  if (_handlerIdx == -1)
                  {
                     for (_handlerIdx = _handlersTab.getSize() - 1; _handlerIdx >= 0; _handlerIdx--)
                     {
                        handler = _handlersTab.getHandler(_handlerIdx);
                        if (handler.isNonSpecificHandlerOf(_rtEvt) || handler.isSpecificHandlerOf(_rtEvt))
                           return handler;
                     }
                  }
                  // an event handler was not found or was returned in a previous call to this method
                  return null;

               default:
                  // other internal events should continue
                  break;
            }
         }

         while (setNextHandlerIdx())
         {
            handler = _handlersTab.getHandler(_handlerIdx);
            switch (_phase)
            {
               case PHASE_CONTROL_SPECIFIC:
                  if (handler.isSpecificHandlerOf(_rtEvt))
                     return handler;
                  continue;

               case PHASE_CONTROL_NON_SPECIFIC:
                  if (handler.isNonSpecificHandlerOf(_rtEvt))
                     return handler;
                  continue;

               case PHASE_GLOBAL_SPECIFIC:
                  if (handler.isGlobalSpecificHandlerOf(_rtEvt))
                     return handler;
                  continue;

               case PHASE_GLOBAL:
                  if (handler.isGlobalHandlerOf(_rtEvt))
                     return handler;
                  continue;

               default:
                  Logger.Instance.WriteExceptionToLog("in EventHandlerPosition.getNext() invalid phase: " + _phase);
                  break;
            }
         }
         return null;
      }

      /// <summary>
      ///   returns true if a next handler was found
      /// </summary>
      private bool setNextHandlerIdx()
      {
         // the handler idx is (-1) when starting to search in a task
         if (_handlerIdx < 0)
            _handlerIdx = _handlersTab.getSize() - 1;
         else
            _handlerIdx--;

         if (_handlerIdx < 0 || _task.isAborting())
         {
            // if there are no more handlers in the task then go up the chain of tasks
            if (goUpTaskChain())
               return setNextHandlerIdx();
               // no more tasks in the chain
            else
               return false;
         }
         return true;
      }

      /// <summary>
      ///   get the next task in the tasks chain and returns false if no task was found.
      ///   this function changes the phase variable accordingly
      /// </summary>
      private bool goUpTaskChain()
      {
         MGData mgd = _task.getMGData();
         int ctlIdx = _task.getCtlIdx();

         //handlers for events  for  .NET object must be in the same task (not including control)
         if (_rtEvt.DotNetObject != null)
         {
            if (_phase != PHASE_CONTROL_NON_SPECIFIC) //QCR #940545, check only the same tasks
            {
               _phase++; //go to the next phase
               return true;
            }
            else
               return false;
         }

         switch (_phase)
         {
               /*case PHASE_CONTROL_SPECIFIC:
                phase = PHASE_CONTROL_NON_SPECIFIC;
                break;*/
            case PHASE_CONTROL_SPECIFIC:
            case PHASE_CONTROL_NON_SPECIFIC:
               // non specific handlers are searched till we hit our main program (inclusive)
               // afterwards we switch to global phase.
               if (!_task.isMainProg())
               {
                  getParentOrCompMainPrg();
                  break;
               }
               else
               {
                  // internal, internal, system and user events may cross component bounderies
                  if ((_rtEvt.getType() == ConstInterface.EVENT_TYPE_PUBLIC ||
                       _rtEvt.getType() == ConstInterface.EVENT_TYPE_INTERNAL ||
                       _rtEvt.getType() == ConstInterface.EVENT_TYPE_SYSTEM ||
                       _rtEvt.getType() == ConstInterface.EVENT_TYPE_USER) && ctlIdx != 0)
                  {
                     // load the RT parent of the previous task. If no prevtask exists then we are
                     // simply running on the main progs list (for example, when a main prg catches
                     // a timer event, no prevtask exists.
                     if (_prevTask == null)
                     {
                        _task = (Task)mgd.getNextMainProg(ctlIdx);
                        if (_task == null && ctlIdx != 0)
                           _task = MGDataCollection.Instance.GetMainProgByCtlIdx(ClientManager.Instance.EventsManager.getCompMainPrgTab().getCtlIdx(0));
                     }
                     else
                     {
                        // the component main program that was set in getParentOrCompMainPrg, is now replaced back to the path parent.
                        _task = _prevTask.PathParentTask;
                        _prevTask = null;
                     }
                     _rtEvt.setMainPrgCreator(null); //moving out of a main program to another task
                     break;
                  }

                  // here we scan the main progs according to the load sequence (first to last).
                  if (_phase == PHASE_CONTROL_SPECIFIC)
                  {
                     // specific search is over. start the non specific search from 
                     // the first task.
                     _phase = PHASE_GLOBAL_SPECIFIC;
                  }
                  else
                  {
                     // here we scan the main progs according to the load sequence (first to last).
                     _phase = PHASE_GLOBAL;
                  }
                  _task = MGDataCollection.Instance.GetMainProgByCtlIdx(ClientManager.Instance.EventsManager.getCompMainPrgTab().getCtlIdx(0));
                  _rtEvt.setMainPrgCreator(_task);
                  if (_task == null)
                     return false;
                  break;
               }

            case PHASE_GLOBAL_SPECIFIC:
            case PHASE_GLOBAL:
               _task = (Task)mgd.getNextMainProg(ctlIdx);
               if (_task == null)
               {
                  if (_phase == PHASE_GLOBAL)
                     return false;
                     // PHASE_GLOBAL_SPECIFIC
                  else
                  {
                     // specific search is over. start the non specific search from 
                     // the first task.
                     _phase = PHASE_CONTROL_NON_SPECIFIC;
                     _task = _orgTask;
                     _prevTask = _orgPrevTask;
                     break;
                  }
               }
               break;

            default:
               Logger.Instance.WriteExceptionToLog("in EventHandlerPosition.goUpTaskChain() invalid phase: " + _phase);
               break;
         }
         if (_task == null)
            return false;
         _handlersTab = _task.getHandlersTab();
         if (_handlersTab == null)
            return goUpTaskChain();
         _handlerIdx = -1;
         return true;
      }

      /// <summary>
      ///   if the current task and its parent are from different components then
      ///   set the task to the main program of the component of the current task.
      ///   otherwise, set the task to be the parent of the current task
      /// </summary>
      private void getParentOrCompMainPrg()
      {
         int ctlIdx = _task.getCtlIdx();
         Task parent;

         _prevTask = _task;
         // Retrieve the task's calling-parent. We need the task who invoked the current task
         // rather than the task's triggering parent. 
         // The Path Parent is the parent of that task as if the server had done build path. It is more logical to search using the trigger task tree
         // but since the online/server does not use the trigger tree, we decided not to use it here as well.
         // If the path parent is from a different comp, it means that between curr task and parent there should be a comp main prog. 
         parent = _task.PathParentTask;
         if (parent == null)
         {
            _task = null;
            return;
         }

         // check if the parent task is from another component
         if (ctlIdx != parent.getCtlIdx())
         {
            // replace the parent task to search with the comp main program. later on, the main prog will be replaced with 
            // the real PathParentTask. 
            _rtEvt.setMainPrgCreator(_task);
            _task = (Task) MGDataCollection.Instance.GetMainProgByCtlIdx(ctlIdx);
         }
         else
         {
            _rtEvt.setMainPrgCreator(null);
            _task = parent;
         }
      }
   }
}
