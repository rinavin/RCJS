using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.richclient.rt;

namespace com.magicsoftware.richclient.util
{
   static class RecomputeIdFactory
   {
      public static RecomputeId GetRecomputeId(Type type, int id)
      {
         return new RecomputeId(type, id);
      }

      public static RecomputeId GetRecomputeId(MgControlBase control)
      {
         return new RecomputeId(typeof(DcValues), control.getDitIdx());
      }

      public static RecomputeId GetRecomputeId(IDataviewHeader dataviewHeader)
      {
         return new RecomputeId(typeof(IDataviewHeader), dataviewHeader.Id);
      }

      public static RecomputeId GetDataviewHeaderComputeId(int dataviewHeaderId)
      {
         return new RecomputeId(typeof(IDataviewHeader), dataviewHeaderId);
      }
   }
}
