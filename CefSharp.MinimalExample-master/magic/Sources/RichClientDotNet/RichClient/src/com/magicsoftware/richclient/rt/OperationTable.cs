using System;
using System.Collections.Generic;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.richclient.util;
using EventHandler = com.magicsoftware.richclient.events.EventHandler;

namespace com.magicsoftware.richclient.rt
{
   internal class OperationTable
   {
      private readonly List<Operation> _operations = new List<Operation>();
      private int _firstBlockOperIdx = Int32.MaxValue;

      /// <summary>
      ///   Function for filling own fields, allocate memory for inner objescts.
      ///   Parsing the input String.
      /// </summary>
      /// <param name = "taskRef">reference</param>
      /// <param name = "evtHandler">to event handler</param>
      internal void fillData(Task taskRef, EventHandler evtHandler)
      {
         Operation oper;
         int operType;
         int operIdx = -1;

         while (initInnerObjects(ClientManager.Instance.RuntimeCtx.Parser.getNextTag(), taskRef, evtHandler))
         {
         }

         /* QCR# 180535.
         * Since blank operations are not sent from the Server, there can be 
         * a mismatch in the BlockClose and BlockEnd idx because they contain 
         * the serverId.
         * We need to update these idx with the corresponding Client-side idx
         */
         for (int i = _firstBlockOperIdx; i < _operations.Count; i++)
         {
            oper = _operations[i];
            operType = oper.getType();

            if (operType == ConstInterface.MG_OPER_LOOP || operType == ConstInterface.MG_OPER_BLOCK ||
                operType == ConstInterface.MG_OPER_ELSE)
            {
               operIdx = serverId2operIdx(oper.getBlockClose(), i + 1);
               oper.setBlockClose(operIdx);

               operIdx = serverId2operIdx(oper.getBlockEnd(), operIdx);
               oper.setBlockEnd(operIdx);
            }
         }
      }

      /// <summary>
      ///   To allocate and fill inner objects of the class
      /// </summary>
      /// <param name = "foundTagName">possible  tag name, name of object, which need be allocated
      /// </param>
      /// <param name = "mgdata">references
      /// </param>
      /// <param name = "task">current
      /// </param>
      /// <param name = "evtHandler">to event handler
      /// </param>
      private bool initInnerObjects(String foundTagName, Task taskRef, EventHandler evtHandler)
      {
         if (foundTagName != null && foundTagName.Equals(ConstInterface.MG_TAG_OPER))
         {
            var oper = new Operation();
            oper.fillData(taskRef, evtHandler);
            _operations.Add(oper);
            if (_firstBlockOperIdx == Int32.MaxValue &&
                (oper.getType() == ConstInterface.MG_OPER_LOOP || oper.getType() == ConstInterface.MG_OPER_BLOCK))
               _firstBlockOperIdx = _operations.Count - 1;
            return true;
         }
         return false;
      }

      /// <summary>
      ///   get the number of operations in the table
      /// </summary>
      internal int getSize()
      {
         return _operations.Count;
      }

      /// <summary>
      ///   get an operation by its index
      /// </summary>
      /// <param name = "idx">the index of the operation in the table
      /// </param>
      internal Operation getOperation(int idx)
      {
         if (idx >= 0 && idx < _operations.Count)
            return _operations[idx];
         throw new ApplicationException("in OperationTable.getOperation() index out of bounds: " + idx);
      }

      /// <summary>
      ///   get the operation idx (i.e. the position in the OperationTab)
      ///   depending on the serverId
      /// </summary>
      /// <param name = "id:">the serverId to search
      /// </param>
      /// <param name = "startFrom:">the idx from where the search is to begin
      /// </param>
      /// <returns> the operation idx, if serverId is matched. Otherwise -1.
      /// </returns>
      internal int serverId2operIdx(int id, int startFrom)
      {
         Operation oper;
         int operIdx = -1;

         for (int idx = startFrom; idx < _operations.Count; idx++)
         {
            oper = _operations[idx];

            if (id == oper.getServerId())
            {
               operIdx = idx;
               break;
            }
         }

         return operIdx;
      }

      /// <summary>
      ///   get the operation idx (i.e. the position in the OperationTab)
      ///   depending on the serverId
      /// </summary>
      /// <param name = "id:">the serverId to search
      /// </param>
      /// <param name = "startFrom:">the idx from where the search is to begin
      /// </param>
      /// <returns> the operation idx, if serverId is matched. Otherwise -1.
      /// </returns>
      internal int serverId2FollowingOperIdx(int id, int startFrom)
      {
         Operation oper;
         int operIdx = -1;

         for (int idx = startFrom; idx < _operations.Count; idx++)
         {
            oper = _operations[idx];

            if (id < oper.getServerId())
            {
               operIdx = idx;
               break;
            }
         }

         return operIdx;
      }
   }
}