using System;
using System.Collections.Generic;
using System.Text;

namespace com.magicsoftware.gatewaytypes
{
   /// <summary>
   /// 
   /// </summary>
   public enum TransactionModes
   {
       OpenRead        = 1,
       OpenWrite       = 4,
       Commit           = 8,
       Abort            = 16
   }
}
