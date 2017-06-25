using System;
using System.Collections.Generic;
using System.Text;

namespace com.magicsoftware.richclient.local.data.view
{
   [Flags]
   public enum BoudariesFlags
   {
      None = 0x00,
      Range = 0x01,
      Locate = 0x02,
   }
}
