using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.util;
using util.com.magicsoftware.util;

namespace com.magicsoftware.richclient.rt
{
   class DCValuesRecomputeSaxHandler : MgSAXHandler
   {
      public DCValuesRecompute DcValuesRecomputeAction { get; private set; }

      Task task;

      public DCValuesRecomputeSaxHandler(Task task)
      {
         this.task = task;
      }

      public override void startElement(string elementName, System.Collections.Specialized.NameValueCollection attributes)
      {
         int ditIndex = Int32.Parse(attributes.Get(XMLConstants.MG_ATTR_DITIDX));
         DcValuesRecomputeAction = new DCValuesRecompute(task, ditIndex);
      }
   }
}
