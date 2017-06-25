using System;
using System.Collections.Generic;
using System.Text;

namespace com.magicsoftware.unipaas.gui.low
{
   /// <summary>
   /// Manages Focus of Print Preview Form when other runtime window is activated
   /// </summary>
   public class PrintPreviewFocusManager
   {
      #region Properties

      IntPtr printPreviewFormHandle;
      /// <summary>
      /// Handle of Print Preview form to be activated
      /// </summary>
       public IntPtr PrintPreviewFormHandle
      {
         get
         {
            lock (lockObject)
               return printPreviewFormHandle;
         }
         set
         {
            lock (lockObject)
               printPreviewFormHandle = value;
         }
      }

      bool shouldPrintPreviewBeFocused;
      /// <summary>
      /// Indicates whether Print Preview form is to be activated or not.
      /// </summary>
      public bool ShouldPrintPreviewBeFocused
      {
         get
         {
            lock (lockObject)
               return shouldPrintPreviewBeFocused;
         }
         set
         {
            lock (lockObject)
               shouldPrintPreviewBeFocused = value;
         }
      }

      bool shouldResetPrintPreviewInfo;
      /// <summary>
      /// Indicates whether Print Preview info be reset
      /// </summary>
      public bool ShouldResetPrintPreviewInfo
      {
         get
         {
            lock (lockObject)
               return shouldResetPrintPreviewInfo;
         }
         set
         {
            lock (lockObject)
               shouldResetPrintPreviewInfo = value;
         }
      }

      bool isInModalFormOpening;
      /// <summary>
      /// Indicates that it application is in process of opening Modal window of Runtime
      /// </summary>
      public bool IsInModalFormOpening
      {
         get
         {
            lock (lockObject)
               return isInModalFormOpening;
         }
         set
         {
            lock (lockObject)
               isInModalFormOpening = value;
         }
      }

      #endregion

      /// <summary>
      /// lock object
      /// </summary>
      object lockObject = new object();

      static PrintPreviewFocusManager instance;
      /// <summary>
      /// Singleton instance
      /// </summary>
      /// <returns></returns>
      public static PrintPreviewFocusManager GetInstance()
      {
         if (instance == null)
         {
            lock (typeof(PrintPreviewFocusManager))
            {
               if (instance == null)
               {
                  instance = new PrintPreviewFocusManager();
               }
            }
         }
         return instance;
      }

      #region Ctors

      private PrintPreviewFocusManager()
      {
         ShouldResetPrintPreviewInfo = true;
      }

      #endregion
   }

   /// <summary>
   /// Guards Print Preview related flags 
   /// </summary>
   public class PrintPreviewInfoResetGuard: IDisposable
   {
      public PrintPreviewInfoResetGuard()
      {
         PrintPreviewFocusManager.GetInstance().ShouldResetPrintPreviewInfo = false;
      }

      public void Dispose()
      {
         PrintPreviewFocusManager.GetInstance().ShouldResetPrintPreviewInfo = true;
      }
   }
}
