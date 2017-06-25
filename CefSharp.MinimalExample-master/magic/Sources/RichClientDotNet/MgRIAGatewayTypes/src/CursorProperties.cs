using System;
using System.Collections.Generic;
using System.Text;

namespace com.magicsoftware.gatewaytypes
{
   /// <summary>
   /// This enum defines various flags for Cursor Properties.
   /// </summary>
   [Flags]
   public enum CursorProperties
   {
      Update               = 0x0001,
      Delete               = 0x0002,
      Insert               = 0x0004,
      CursorLock           = 0x0007,
      KeyCheck             = 0x0008, //remove
      StartPos             = 0x0010,
      ReadBlobs            = 0x0020,        /* Read the blobs on fetch */
      DirReversed          = 0x0040,
      CursorLink           = 0x0080,
      LocateCursor         = 0x0100,
      DummyFetch           = 0x0200,
      SyncData             = 0x0400, //remove??
      PreloadViewArrFetch  = 0x0800, // to let the gateway perform an array fetch (if it doesn't otherwise) if PreloadView is set.
      ClientLinkGetCurr    = 0x1000, //remove??
   }
}
