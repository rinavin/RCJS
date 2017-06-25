using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using com.magicsoftware.unipaas.management.env;

namespace Tests.Gui
{
   public class TestEnvironment : IEnvironment
   {
      Encoding encoding = Encoding.GetEncoding(1252);
      public char dateMode = ' ';


      public char Language
      {
         get { throw new NotImplementedException(); }
      }

      /// <summary>
      /// This flag is not relevant. Hence, returning true as it is the default value
      /// </summary>
      public bool SpecialNumpadPlusChar { get { return true; } set { } }

      public bool SpecialRestoreMaximizedForm { get; set; }

      public bool IgnoreReplaceDecimalSeparator { get; set; } 

      public bool SpecialIgnoreBGinModify { get; set; }

      public char GetDateMode(int compIdx)
      {
         return 'E';
      }

      public int GetCentury(int compIdx)
      {
         return 20;
      }

      public char GetDate()
      {
         return '/';
      }

      public char GetTime()
      {
         return ':';
      }

      public char GetDecimal()
      {
         return '.';
      }

      public bool CanReplaceDecimalSeparator()
      {
         throw new NotImplementedException();
      }

      public char GetThousands()
      {
         return ',';
      }

      public int GetDefaultColor()
      {
         throw new NotImplementedException();
      }

      public int GetDefaultFocusColor()
      {
         throw new NotImplementedException();
      }

      public string GetGUID()
      {
         throw new NotImplementedException();
      }

      public string GetControlsPersistencyPath()
      {
         throw new NotImplementedException();
      }

      public bool GetImeAutoOff()
      {
          throw new NotImplementedException();
      }
      
       public bool GetLocalAs400Set()
      {
         throw new NotImplementedException();
      }

      public bool GetLocalFlag(char f)
      {
         throw new NotImplementedException();
      }
      
      public int GetSignificantNumSize()
      {
         return 10;
      }

      public int GetDebugLevel()
      {
         throw new NotImplementedException();
      }
     
      public System.Text.Encoding GetEncoding()
      {
         return encoding;
      }

      public string ForwardSlashUsage
      {
         get { throw new NotImplementedException(); }
      }

      public string GetDropUserFormats()
      {
         throw new NotImplementedException();
      }

      public bool GetSpecialEngLogon()
      {
         throw new NotImplementedException();
      }

      public bool GetSpecialIgnoreButtonFormat()
      {
         throw new NotImplementedException();
      }

      public bool GetSpecialSwfControlNameProperty()
      {
         throw new NotImplementedException();
      }

      public bool GetSpecialLogInternalExceptions()
      {
         throw new NotImplementedException();
      }

      public int GetTooltipTimeout()
      {
         throw new NotImplementedException();
      }

      public bool GetSpecialEditLeftAlign()
      {
          throw new NotImplementedException();
      }        


      public bool GetSpecialTextSizeFactoring()
      {
         throw new NotImplementedException();
      }

      public bool GetSpecialFlatEditOnClassicTheme()
      {
         throw new NotImplementedException();
      }


      public bool GetSpecialOldZorder()
      {
         throw new NotImplementedException();
      }

      

      public bool SpecialOldZorder
      {
         get
         {
            throw new NotImplementedException();
         }
         set
         {
            throw new NotImplementedException();
         }
      }


      public bool GetSpecialSwipeFlickeringRemoval()
      {
         throw new NotImplementedException();
      }

      public bool GetSpecialDisableMouseWheel()
      {
         throw new NotImplementedException();
      }
   }
}
