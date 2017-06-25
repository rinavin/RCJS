using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.gatewaytypes.data;

namespace com.magicsoftware.richclient.local.data.gateways.storage
{ 
   /// <summary>
   /// 
   /// </summary>
   interface IConvertMgValueToGateway
   {
      object ConvertMgValueToGateway(DBField dbField, object runtimeMgValue);
   }

}
