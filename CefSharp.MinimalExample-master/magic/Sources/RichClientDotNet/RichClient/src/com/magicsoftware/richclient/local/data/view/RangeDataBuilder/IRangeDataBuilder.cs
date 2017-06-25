using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.gatewaytypes;

namespace com.magicsoftware.richclient.local.data.view.RangeDataBuilder
{
   /// <summary>
   /// Interface for a builder to build the RangeData list, list that will be passed to the gateway
   /// </summary>
   interface IRangeDataBuilder
   {
      List<RangeData> Build(BoudariesFlags boundariesFlag);
   }
}
