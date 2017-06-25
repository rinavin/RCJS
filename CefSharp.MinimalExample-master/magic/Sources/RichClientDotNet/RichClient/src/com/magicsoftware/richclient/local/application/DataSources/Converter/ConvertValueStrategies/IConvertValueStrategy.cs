using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.gatewaytypes.data;
using com.magicsoftware.gatewaytypes;

namespace com.magicsoftware.richclient.local.application.datasources.converter.convertValueStrategies
{
   /// <summary>
   /// Interface for Value conversion.
   /// </summary>
   internal interface IConvertValueStrategy
   {
      void Convert(DBField sourceField, DBField destinationField, FieldValue sourceValue, FieldValue destinationValue);
   }
}
