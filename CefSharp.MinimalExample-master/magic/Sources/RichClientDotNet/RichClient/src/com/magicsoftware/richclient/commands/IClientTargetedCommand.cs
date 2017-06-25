using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.rt;
using com.magicsoftware.richclient.commands.ServerToClient;

namespace com.magicsoftware.richclient.commands
{
   /// <summary>
   /// Interface for commands that are sent to the client from outside,
   /// such as server commands.
   /// </summary>
   interface IClientTargetedCommand : IClientCommand
   {
      /// <summary>
      /// Run the command on the client.
      /// </summary>
      /// <param name="res"></param>
      void Execute(IResultValue res);

      /// <summary>
      /// Gets a value denoting whether the execution of the command will block
      /// the execution of everything else on the client until the command execution ends.
      /// </summary>
      bool IsBlocking { get; }

      /// <summary>
      /// Gets or sets a value denoting the frame identifier for the command execution.
      /// This value is expected to be set by the commands table, before running the command.
      /// </summary>
      int Frame { get; set; }

      /// <summary>
      /// Gets a value denoting whether executing the command will activate a window
      /// other than the current. This will reflect on the timing of the command execution.
      /// </summary>
      bool WillReplaceWindow { get; }

      /// <summary>
      /// The tag of the task in whose context the command is executed.
      /// </summary>
      string TaskTag { get; }

      string Obj { get; }

      bool ShouldExecute { get; }
      /// <summary>
      /// Manipulates inner state according to an XML attribute value.
      /// </summary>
      /// <param name="attribute">The name of the attribute.</param>
      /// <param name="value">The value of the attribute as a string read from the XML.</param>
      void HandleAttribute(string attribute, string value);
   }
}
