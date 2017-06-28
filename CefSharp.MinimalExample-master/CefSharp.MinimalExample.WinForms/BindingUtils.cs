using com.magicsoftware.richclient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CefSharp.MinimalExample.WinForms
{
   class BindingUtils
   {
   }
   public class BoundObject
   {
      public class AsyncBoundObject
      {
         //We expect an exception here, so tell VS to ignore
         // [DebuggerHidden]
         public void Error()
         {
            throw new Exception("This is an exception coming from C#");
         }

         //We expect an exception here, so tell VS to ignore
         // [DebuggerHidden]
         public int Div(int divident, int divisor)
         {

            return divident + divisor;
         }
      }
   }

   public class MagicBoundObject
   {

      public MagicBoundObject()
      {

      }
      IJavascriptCallback jsc;

      public void Start(IJavascriptCallback javascriptCallback)
      {
         //const int taskDelay = 1;
         jsc = javascriptCallback;
         //Task.Run(async () =>
         //{
          //  await Task.Delay(taskDelay);

           // using (javascriptCallback)
           // {
               Runme.Start(RefreshCallback);
               //NOTE: Classes are not supported, simple structs are
               //var response = new CallbackResponseStruct("This callback from C# was delayed " + taskDelay + "ms");
               //var response = "This callback from C# was delayed " + taskDelay + "ms";
               // await javascriptCallback.ExecuteAsync(response);
           // }
         //});
      }

      public void InsertEvent(int controlIdx)
      {
         com.magicsoftware.richclient.Runme.AddClickEvent(controlIdx);
      }

      private void RefreshCallback(string UIDesctiption)
      {
         jsc.ExecuteAsync(UIDesctiption);      
      }
   }
   public class BoundObject1
   {
      public string MyProperty { get; set; }
      public void MyMethod()
      {
         // Do something really cool here.
      }
      IJavascriptCallback jsc;

      public void TestCallback(IJavascriptCallback javascriptCallback)
      {
         const int taskDelay = 1500;
         jsc = javascriptCallback;
         Task.Run(async () =>
         {
            await Task.Delay(taskDelay);

            using (javascriptCallback)
            {

               //NOTE: Classes are not supported, simple structs are
               //var response = new CallbackResponseStruct("This callback from C# was delayed " + taskDelay + "ms");
               var response = "This callback from C# was delayed " + taskDelay + "ms";
               await javascriptCallback.ExecuteAsync(response);
            }
         });
      }

   }
}
