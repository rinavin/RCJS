using System;
using com.magicsoftware.util;

namespace com.magicsoftware.unipaas.management.gui
{
   public interface PropParentInterface
   {
      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      int getCompIdx();

      /// <summary>
      /// 
      /// </summary>
      /// <param name="propId"></param>
      /// <returns></returns>
      Property getProp(int propId);


      /// <summary>
      /// 
      /// </summary>
      /// <param name="propId"></param>
      /// <returns></returns>
      bool checkIfExistProp(int propId);

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      MgFormBase getForm();

      /// <summary>
      ///   return true if this is first refresh
      /// </summary>
      /// <returns></returns>
      bool IsFirstRefreshOfProps();

      /// <summary>
      /// 
      /// </summary>
      /// <param name="expId"></param>
      /// <param name="resType"></param>
      /// <param name="length"></param>
      /// <param name="contentTypeUnicode"></param>
      /// <param name="resCellType"></param>
      /// <param name="alwaysEvaluate"></param>
      /// <param name="wasEvaluated"></param>
      /// <returns></returns>
      String EvaluateExpression(int expId, StorageAttribute resType, int length, bool contentTypeUnicode, StorageAttribute resCellType,
                                bool alwaysEvaluate, out bool wasEvaluated);
   }
}