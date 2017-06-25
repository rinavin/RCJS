using System;
using System.Collections.Generic;
using System.Text;
using util.com.magicsoftware.util;
using System.Diagnostics;

namespace com.magicsoftware.unipaas.management.data
{
   public abstract class ObjectReferenceBase : IDisposable
   {
      static int LastId = 0;

      public IReferencedObject Referent { get; private set; }
      bool isDisposed;
      int id;

      StackTrace _instantiationTrace;

      protected ObjectReferenceBase(IReferencedObject referent)
      {
         id = ++LastId;
         isDisposed = false;
         this.Referent = referent;
         if (Logger.Instance.LogLevel >= Logger.LogLevels.Development)
         {
            Logger.Instance.WriteDevToLog("Creating " + this.ToString());
            _instantiationTrace = new StackTrace(0, true);
         }
         referent.AddReference();
      }

      ~ObjectReferenceBase()
      {
         Logger.Instance.WriteDevToLog("Finalizing " + this);
         Dispose(false);
      }

      public void Dispose()
      {
         Logger.Instance.WriteDevToLog("Disposing " + this);
         GC.SuppressFinalize(this);
         Dispose(true);
      }

      private void Dispose(bool isDisposing)
      {
         //if (isDisposing && isDisposed)
         //   throw new InvalidOperationException("Reference was already disposed");

         if (!isDisposed)
         {
            if (!Referent.HasReferences)
            {
               if (Logger.Instance.LogLevel >= Logger.LogLevels.Development)
               {
                  Logger.Instance.WriteSupportToLog("Referent does not have any more references: " + Referent, false);
                  Logger.Instance.WriteStackTrace(_instantiationTrace, 15, "Instantiation trace:");
               }
               Debug.Assert(false, "Referent does not have any more references. See DEV level log.");
            }
            else
            Referent.RemoveReference();
         }

         isDisposed = true;
      }

      public abstract ObjectReferenceBase Clone(); 

      public override string ToString()
      {
         return "{Reference " + id + " to: " + Referent.ToString() + "}";
      }
   }
}
