using System;

namespace com.magicsoftware.util
{
   class AutoResetFlag
   {
      bool value;

      public AutoResetFlag()
      {
         value = false;
      }

      public bool IsSet { get { return value; } }

      public IDisposable Set()
      {
         value = true;
         return new FlagReset(this);
      }

      class FlagReset : IDisposable
      {
         AutoResetFlag flag;
         public FlagReset(AutoResetFlag flag)
         {
            this.flag = flag;
         }
         public void Dispose()
         {
            flag.value = false;
         }
      }

      public static implicit operator bool(AutoResetFlag flag)
      {
         return flag.IsSet;
      }
   }
}
