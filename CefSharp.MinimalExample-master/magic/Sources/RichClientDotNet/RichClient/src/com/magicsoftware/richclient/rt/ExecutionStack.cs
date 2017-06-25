using System;
using System.Collections;
using System.Text;
using com.magicsoftware.richclient.util;
using com.magicsoftware.unipaas.util;
using util.com.magicsoftware.util;

namespace com.magicsoftware.richclient.rt
{
   /// <summary> this class represents an execution stack of all operations that can be interupted by going to the server
   /// to continue execution (raise event or any call to user defined function)
   /// </summary>
   internal class ExecutionStack
   {
      private readonly Stack _execStack; // the execution stack itself

      /// <summary> default constructor</summary>
      internal ExecutionStack()
      {
         _execStack = new Stack();
      }

      /// <summary> constructor which copies the parameter execution stack</summary>
      /// <param name="inExecStack">- the execution stack to copy </param>
      internal ExecutionStack(ExecutionStack inExecStack)
      {
         _execStack = (Stack) inExecStack.getStack().Clone();
      }

      /// <summary> getter for the execution stack itself</summary>
      /// <returns> the stack </returns>
      internal Stack getStack()
      {
         return _execStack;
      }

      /// <summary> pushes one entry into the stack</summary>
      /// <param name="execEntry">- to be pushed </param>
      internal void push(ExecutionStackEntry execEntry)
      {
         _execStack.Push(execEntry);
      }

      /// <summary> pushes an entry to the execution stack constructed by its 3 elements which are recieved as parameters</summary>
      /// <param name="taskId"> </param>
      /// <param name="handlerId"> </param>
      /// <param name="operIdx"> </param>
      internal void push(String taskId, String handlerId, int operIdx)
      {
         var execEntry = new ExecutionStackEntry(taskId, handlerId, operIdx);

         _execStack.Push(execEntry);
      }

      /// <summary> pops an entry from the execution stack</summary>
      /// <returns> the popped entry
      /// </returns>
      internal ExecutionStackEntry pop()
      {
         return (ExecutionStackEntry) _execStack.Pop();
      }

      /// <summary> check if the execution stack is empty </summary>
      /// <returns> an indication if it's empty or not
      /// </returns>
      internal bool empty()
      {
         return (_execStack.Count == 0);
      }

      /// <summary> clears the execution stack</summary>
      internal void clear()
      {
         _execStack.Clear();
      }

      /// <returns> the size of the execution stack
      /// </returns>
      internal int size()
      {
         return _execStack.Count;
      }

      /// <summary> checks if two execution stacks are equal</summary>
      /// <param name="execStackCmp">- execution stack to compare to the current </param>
      /// <returns>s an indication whether the two are equal or not </returns>
      public override bool Equals(Object execStackCmp)
      {
         var tmpExecStack = new ExecutionStack(this);
         var tmpExecStackCmp = new ExecutionStack((ExecutionStack) execStackCmp);
         bool equalStacks = false;

         if (tmpExecStack.size() == tmpExecStackCmp.size())
         {
            equalStacks = true;
            while (!tmpExecStack.empty() && equalStacks)
               if (!tmpExecStack.pop().Equals(tmpExecStackCmp.pop()))
                  equalStacks = false;
         }

         return equalStacks;
      }

      /// <summary> push into the current execution stack the execution stack that we get as parameter, but in reverse order</summary>
      /// <param name="inExecStack">- the execution stack to be pushed </param>
      internal void pushUpSideDown(ExecutionStack inExecStack)
      {
         var tmpStack = new ExecutionStack(inExecStack);
         ExecutionStackEntry tmpStackEntry;

         while (!tmpStack.empty())
         {
            tmpStackEntry = tmpStack.pop();
            _execStack.Push(tmpStackEntry);
         }
      }

      /// <summary> reverse the order of the entries in the current execution stack</summary>
      internal void reverse()
      {
         var tmpExecStack = new ExecutionStack(this);
         ExecutionStackEntry tmpExecStackEntry;

         clear();

         while (!tmpExecStack.empty())
         {
            tmpExecStackEntry = tmpExecStack.pop();
            push(tmpExecStackEntry);
         }
      }

      /// <summary> builds the xml of the current execution stack to be sent to the server</summary>
      /// <param name="Message">- the message which the xml needs to be appended to
      /// </param>
      internal void buildXML(StringBuilder message)
      {
         StringBuilder forMessage = new StringBuilder();
         while (_execStack.Count != 0)
         {
            var ExecEntry = (ExecutionStackEntry) _execStack.Pop();
            // offline tasks, and all tasks before them, should not be passed to the server
            uint taskTag = Convert.ToUInt32(ExecEntry.TaskId);
            if (taskTag > ConstInterface.INITIAL_OFFLINE_TASK_TAG)
            {
               // clean the buffer and continue with the next task
               forMessage.Remove(0, forMessage.Length);
               continue;
            }

            forMessage.Append("\n " + XMLConstants.TAG_OPEN + ConstInterface.MG_TAG_EXEC_STACK_ENTRY);
            forMessage.Append(" " + XMLConstants.MG_ATTR_TASKID + "=\"" + ExecEntry.TaskId + "\"");
            forMessage.Append(" " + ConstInterface.MG_ATTR_HANDLERID + "=\"" + ExecEntry.HandlerId + "\"");
            forMessage.Append(" " + ConstInterface.MG_ATTR_OPER_IDX + "=\"" + ExecEntry.OperIdx + "\"");
            forMessage.Append(" " + XMLConstants.TAG_TERM);
         }

         message.Append(forMessage);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      public override int GetHashCode()
      {
         return base.GetHashCode();
      }
   }
}