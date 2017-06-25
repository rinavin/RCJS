using System;
using System.Collections.Generic;
using System.Text;

namespace com.magicsoftware.richclient.commands.ClientToServer
{
   public enum ClientCommandType
   {
      // Client to Server
      Event = 'E',
      Transaction = 'T',
      Unload = 'U',
      Recompute = 'R',
      ExecOper = 'X',
      Evaluate = 'L',
      Menu = 'M',
      Query = 'Q',
      IniputForceWrite = 'I',
      VerifyCache = 'C',
      // Server to Client
      Verify = 'V',
      EnhancedVerify = 'E',
      Abort = 'A',
      Result = 'S',
      OpenURL = 'P',
      Expand = 'N',
      Hibernate = 'H',
      Resume = 'Z',
      AddRange = 'R',
      ClientRefresh = 'C',
      // local
      Dataview = 'D'
   }
}
