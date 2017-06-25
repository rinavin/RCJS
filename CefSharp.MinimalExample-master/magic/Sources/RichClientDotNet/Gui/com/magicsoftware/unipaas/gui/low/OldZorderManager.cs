using System;
using System.Collections.Generic;
using System.Text;

namespace com.magicsoftware.unipaas.gui.low
{
   /// <summary>
   /// this class is responsible for managing issues related to the SpecialOldZorder flag
   /// </summary>
   public class OldZorderManager
   {
      private static OldZorderManager _instance;
      internal static OldZorderManager getInstance()
      {
         if (_instance == null)
            _instance = new OldZorderManager();
         return _instance;
      }

      private OldZorderManager()
      {
      }

      public bool DisableSpecialZorderSetting { get; set; }

      /// <summary>
      /// prevent zorder changes
      /// </summary>
      public bool PreventZorderChanges { get; set; }

      /// <summary>
      /// should use OldZorder algorithm while computing orphans zorder issues
      /// </summary>
      public bool UseOldZorderAlgorithm
      {
         get
         {
            return (Manager.Environment.SpecialOldZorder || PreventZorderChanges ) && !DisableSpecialZorderSetting;
         }
      }

      
   }
}
