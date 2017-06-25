using System;
using System.Collections.Generic;
using System.Text;

namespace RuntimeDesigner.RuntimeDesignerStrategies
{
   /// <summary>
   /// strategy for the runtime designer property descriptor CanReset method
   /// </summary>
   interface ICanResetStrategy
   {
      bool CanResetData(object defaultValue, object value);
   }
}
