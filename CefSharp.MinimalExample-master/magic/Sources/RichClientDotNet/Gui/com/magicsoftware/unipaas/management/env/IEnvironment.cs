using System;
using System.Text;

namespace com.magicsoftware.unipaas.management.env
{
   /// <summary>
   /// functionality required by the GUI namespace from the Environment class.
   /// </summary>
   public interface IEnvironment
   {
      char Language { get; }

      bool SpecialNumpadPlusChar { get; set; }

      bool SpecialOldZorder { get; set; }

      bool SpecialRestoreMaximizedForm { get; set; }

      bool SpecialIgnoreBGinModify { get; set; }

      bool IgnoreReplaceDecimalSeparator { get; set; }

      char GetDateMode(int compIdx);
      
      int GetCentury(int compIdx);

      char GetDate();

      char GetTime();

      char GetDecimal();

      bool CanReplaceDecimalSeparator();

      char GetThousands();

      int GetDefaultColor();

      int GetDefaultFocusColor();

      int GetTooltipTimeout();

      bool GetSpecialEditLeftAlign();

      String GetGUID();

      String GetControlsPersistencyPath();

      bool GetImeAutoOff();
      
      bool GetLocalAs400Set();

      bool GetLocalFlag(char f);

      int GetSignificantNumSize();

      int GetDebugLevel();

      Encoding GetEncoding();

      // instruct how to refer a forward slash - either as a relative web url, or as a file in the file system.
      String ForwardSlashUsage { get; }

      String GetDropUserFormats();

      bool GetSpecialEngLogon();

      bool GetSpecialIgnoreButtonFormat();

      bool GetSpecialSwfControlNameProperty();

      bool GetSpecialLogInternalExceptions();

      bool GetSpecialTextSizeFactoring();

      bool GetSpecialFlatEditOnClassicTheme();

      bool GetSpecialOldZorder();

      bool GetSpecialSwipeFlickeringRemoval();

      bool GetSpecialDisableMouseWheel();
   }
}
