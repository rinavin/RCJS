using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.util;

namespace com.magicsoftware.unipaas.management.data
{
   public interface IRecord
   {
      int getId();
      void setOldRec();
      void SetFieldValue(int idx, bool isNull, String value);
      string GetFieldValue(int idx);
      bool IsNull(int idx);
      bool isFldModified(int fldIdx);
      bool IsFldModifiedAtLeastOnce(int fldIdx);
      DataModificationTypes getMode();

      void AddDcValuesReference(int controlId, int dcValuesId);
   }

  
}
