using System;
using System.Collections.Generic;
using System.Text;

namespace RuntimeDesigner.RuntimeDesignerStrategies
{
   /// <summary>
   /// set data for property
   /// </summary>
   interface ISetPropertyData
   {
      void SetData(object oldValue, ref object newValue);
      object AdjustResettedValue(object value);
   }
}
