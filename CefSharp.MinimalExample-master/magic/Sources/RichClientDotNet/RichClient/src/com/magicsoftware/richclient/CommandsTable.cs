using System;
using System.Text;
using com.magicsoftware.richclient.rt;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.richclient.util;
using com.magicsoftware.util;
using com.magicsoftware.richclient.commands;
using com.magicsoftware.richclient.commands.ClientToServer;
using com.magicsoftware.richclient.commands.ServerToClient;

namespace com.magicsoftware.richclient
{
   /// <summary>
   ///   commands table
   /// </summary>
   internal class CommandsTable
   {
      private readonly MgArrayList _cmds;
      private int _iterationIdx;

      /// <summary>
      ///   CTOR
      /// </summary>
      internal CommandsTable()
      {
         _cmds = new MgArrayList();
      }

      /// <summary>
      ///   To parse input string and fill  _cmds
      /// </summary>
      /// <returns> index of end last  <command ...> tag</returns>
      protected internal void fillData()
      {
         while (initInnerObjects(ClientManager.Instance.RuntimeCtx.Parser.getNextTag()))
         {
         }
      }

      /// <summary>
      ///   To allocate and fill inner objects of the class
      /// </summary>
      /// <param name = "foundTagName">  name of inner tag </param>
      private bool initInnerObjects(String foundTagName)
      {
         if (foundTagName == null)
            return false;
         if (foundTagName.Equals(ConstInterface.MG_TAG_COMMAND))
         {
            IClientTargetedCommand cmd = new XMLBasedCommandBuilder().fillData() as IClientTargetedCommand;

            if (cmd.WillReplaceWindow)
               cmd.Execute(null);
            // Expression needed for CMD_TYPE_RESULT only
            else
               _cmds.Add(cmd);
         }
         else
            return false;
         return true;
      }

      /// <summary>
      ///   Execute all commands in command table and delete them
      /// </summary>
      /// <param name="res">result.</param>
      protected internal void Execute(IResultValue res)
      {
         IClientTargetedCommand cmd;
         int i, j;
         int currFrame = ClientManager.Instance.getCmdFrame(); //cmdFrame++;  save a local value of the frame
         ClientManager.Instance.add1toCmdFrame();
         int firstBlocking = -1;

         // frames are used here to make sure that when this method is called recursively
         // the inner call will not execute commands that should be executed by some outer
         // call to the method

         // First step - move all result commands before any blocking commands (QCR #693428)
         // it can't be that they belong to the blocking task.
         for (i = 0; i < getSize(); i++)
         {
            cmd = getCmd(i) as IClientTargetedCommand;

            if (cmd.IsBlocking && firstBlocking == -1)
               firstBlocking = i;
            else if (cmd is ResultCommand && firstBlocking != -1)
            {
               _cmds.Insert(firstBlocking, cmd);
               _cmds.RemoveAt(i + 1);
            }
         }

         // set the frame for commands which has no frame yet
         for (i = 0; i < getSize(); i++)
         {
            cmd = getCmd(i) as IClientTargetedCommand;
            cmd.Frame = currFrame;

            // If a command blocks the caller, following commands are moved to
            // the next active MGdata, so they will be executed (QCR #438387)
            if (cmd.IsBlocking)
            {
               for (j = i + 1; j < getSize(); j++)
                  ClientManager.Instance.addUnframedCmd(getCmd(j));
               _cmds.SetSize(i + 1);
               break;
            }
         }

         while (getSize() > 0)
         {
            cmd = extractCmd(currFrame) as IClientTargetedCommand;
            if (cmd != null)
            {
               if (cmd.ShouldExecute)
               {
                  cmd.Execute(res);
               }
               else
               {
                  ClientManager.Instance.addCommandsExecutedAfterTaskStarted(cmd);
               }
            }
            else
               break;
         }
      }

      /// <summary>
      ///   returns the next command to execute or null if there is no such command
      ///   the abort commands are returned first
      /// </summary>
      /// <param name = "frame">the frame from which to extract a command </param>
      private IClientCommand extractCmd(int frame)
      {
         int size = getSize();
         IClientTargetedCommand cmd = null;
         IClientTargetedCommand verifyCmd = null;
         int verifyIndex;
         Task verifyTask = null;
         Task abortTask = null;
         int startIterationIdx;
         String tid;

         if (size == 0)
            return null;

         if (_iterationIdx >= size)
            _iterationIdx = 0;

         startIterationIdx = _iterationIdx;

         // try to find an ABORT command and return it
         for (; _iterationIdx < size; _iterationIdx++)
         {
            cmd = getCmd(_iterationIdx) as AbortCommand;
            if (cmd != null && !cmd.TaskTag.StartsWith("-") && cmd.Frame == frame)
            {
               // if an abort command was found, execute all previous verify commands of the task or its' sub-tasks
               for (verifyIndex = 0; verifyIndex < _iterationIdx; verifyIndex++)
               {
                  verifyCmd = getCmd(verifyIndex) as VerifyCommand;
                  if (verifyCmd != null && verifyCmd.Frame == frame)
                  {
                     verifyTask = (Task)MGDataCollection.Instance.GetTaskByID(verifyCmd.TaskTag);
                     abortTask = (Task)MGDataCollection.Instance.GetTaskByID(cmd.TaskTag);

                     // Sometimes the task which issued the verify is not known to the client (e.g. a batch task).
                     if (verifyTask != null && verifyTask.isDescendentOf(abortTask))
                     {
                        _cmds.RemoveAt(verifyIndex);
                        _iterationIdx--;
                        return verifyCmd;
                     }
                  }
               }
               _cmds.RemoveAt(_iterationIdx);

               // for an abort CMD of task "tid", the server might send another abort CMD for task
               // "-tid". We should combine them and get rid of the second CMD.
               size = getSize();
               for (startIterationIdx = 0; startIterationIdx < size; startIterationIdx++)
               {
                  tid = ((IClientTargetedCommand)getCmd(startIterationIdx)).TaskTag;
                  if (tid != null && tid.Substring(1).Equals(cmd.TaskTag))
                  {
                     //cmd.combine((IClientTargetedCommand)getCmd(startIterationIdx));
                     _cmds.RemoveAt(startIterationIdx);
                     break;
                  }
               }

               return cmd;
            }
         }

         // at this stage no ABORT command was found in the table

         for (verifyIndex = 0; verifyIndex < size; verifyIndex++)
         {
            verifyCmd = getCmd(verifyIndex) as VerifyCommand;
            if (verifyCmd != null && verifyCmd.Frame == frame &&
                MGDataCollection.Instance.GetTaskByID(verifyCmd.TaskTag) != null)
            {
               _cmds.RemoveAt(verifyIndex);
               return verifyCmd;
            }
         }

         // at this stage no VERIFY command was found in the table

         for (_iterationIdx = 0; _iterationIdx < size; _iterationIdx++)
         {
            cmd = getCmd(_iterationIdx) as IClientTargetedCommand;
            if (cmd.Frame == frame)
            {
               _cmds.RemoveAt(_iterationIdx);
               return cmd;
            }
         }
         return null;
      }

      /// <summary>
      ///   returns the command by its index in the table
      /// </summary>
      /// <param name = "idx">the index of the requested command</param>
      internal IClientCommand getCmd(int idx)
      {
         var cmd = (IClientCommand)_cmds[idx];

         return cmd;
      }

      /// <summary>
      ///   build the XML structure of the commands and CLEAR the table
      /// </summary>
      protected internal void buildXML(StringBuilder message)
      {
         ClientOriginatedCommand cmd;
         int i;

         for (i = 0; i < _cmds.Count; i++)
         {
            cmd = (ClientOriginatedCommand)_cmds[i];
            message.Append(cmd.Serialize());
         }

         clear();
      }

      /// <summary>
      ///   add a command to the table
      /// </summary>
      /// <param name = "cmd">a command to to add to the table</param>
      internal void Add(IClientCommand cmd)
      {
         _cmds.Add(cmd);
      }


      /// <summary>
      /// extract and remove a command from the table
      /// </summary>
      /// <param name="commandIndex"> index of command in the table</param>
      /// <returns></returns>
      internal IClientCommand ExtractCommand(int commandIndex)
      {
         IClientCommand command = getCmd(commandIndex);
         _cmds.RemoveAt(commandIndex);

         return command;
      }

      /// <summary>
      ///   clear all the commands from the table
      /// </summary>
      protected internal void clear()
      {
         _cmds.Clear();
      }

      /// <summary>
      ///   returns the number of commands in the table
      /// </summary>
      protected internal int getSize()
      {
         return _cmds.Count;
      }
   }
}
