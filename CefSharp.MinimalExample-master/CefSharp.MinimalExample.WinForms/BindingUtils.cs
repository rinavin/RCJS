using CefSharp.MinimalExample.WinForms.Controls;
using com.magicsoftware.richclient;
using com.magicsoftware.unipaas;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

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
      IJavascriptCallback refreshDataCallback;
      IJavascriptCallback getValueCallback;
      private Control controlToInvoke;
      public Control ControlToInvoke
      {
         get { return controlToInvoke; }
         set { controlToInvoke = value; }
      }
      public MagicBoundObject()
      {

      }      

      /// <summary>
      /// register callback for getting value from angular control
      /// </summary>
      /// <param name="javascriptCallback"></param>
      public void registerGetValueCallback(IJavascriptCallback javascriptCallback)
      {
         getValueCallback = javascriptCallback;
         JSBridge.Instance.getControlValueDelegate = GetValue;
      }

      /// <summary>
      /// register callback for refreshing UI
      /// </summary>
      /// <param name="javascriptCallback"></param>
      public void registerRefreshUI(IJavascriptCallback javascriptCallback)
      {
         JSBridge.Instance.refreshUIDelegate = RefreshDisplay;
         refreshDataCallback = javascriptCallback;
      }

      public String GetValue(string controlName)
      {
         object result = "null";
         AutoResetEvent JSSyncAutoResetEvent = new AutoResetEvent(false);
         ControlToInvoke.InvokeOnUiThreadIfRequired( () =>
         {
            var task = getValueCallback.ExecuteAsync(controlName);
           
            task.ContinueWith(t =>
            {
               if (!t.IsFaulted)
               {
                  var response = t.Result;
                  result = response.Success ? (response.Result ?? "null") : response.Message;
                  JSSyncAutoResetEvent.Set();
               }
            }, TaskScheduler.FromCurrentSynchronizationContext());
         });
         JSSyncAutoResetEvent.WaitOne();

         return result.ToString();
      }
      

      public void Start()
      {
         //JSBridge.Instance.refreshUIDelegate = RefreshDisplay;
         //refreshDataCallback = javascriptCallback;       
         Runme.Start();
      
      }

      public void InsertEvent(int controlIdx)
      {
         Runme.AddClickEvent(controlIdx);
      }

      private void RefreshDisplay(string UIDesctiption)
      {
         refreshDataCallback.ExecuteAsync(UIDesctiption);      
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
