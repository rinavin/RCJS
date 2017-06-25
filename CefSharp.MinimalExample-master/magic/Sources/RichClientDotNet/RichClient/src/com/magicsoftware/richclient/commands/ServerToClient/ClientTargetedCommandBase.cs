using System;
using System.Collections.Generic;
using System.Text;
using util.com.magicsoftware.util;
using com.magicsoftware.richclient.commands.ClientToServer;
using com.magicsoftware.richclient.util;

namespace com.magicsoftware.richclient.commands.ServerToClient
{
   /// <summary>
   /// Base class for ClientTargetedCommands. This class implements
   /// common properties and methods shared by all inheriting command classes.
   /// </summary>
   abstract class ClientTargetedCommandBase : IClientTargetedCommand
   {
      string _oldId = null;

      public abstract void Execute(rt.IResultValue res);

      public virtual bool IsBlocking { get { return false; } }

      public int Frame { get; set; }

      public bool WillReplaceWindow { get { return _oldId != null; } }

      public string TaskTag { get; protected set; }

      public string Obj { get; protected set; }

      public virtual bool ShouldExecute { get { return true; } }
      
      public virtual void HandleAttribute(string attribute, string value)
      {
         switch (attribute)
         {
            case XMLConstants.MG_ATTR_TASKID:
               TaskTag = value;
               break;

            case ConstInterface.MG_ATTR_OLDID:
               _oldId = value.TrimEnd(' ');
               break;
         }
      }
   }
}
