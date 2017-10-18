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
   internal class TaskCallbacks
   {
      IJavascriptCallback refreshDataCallback;
      internal IJavascriptCallback RefreshDataCallback
      {
         get { return refreshDataCallback; }
         set { refreshDataCallback = value; }
      }
      // IJavascriptCallback refreshTableUICallback;
      IJavascriptCallback getValueCallback;
      internal IJavascriptCallback GetValueCallback
      {
         get { return getValueCallback; }
         set { getValueCallback = value; }
      }
     

      IJavascriptCallback refreshTableDataCallback;
      internal IJavascriptCallback RefreshTableDataCallback
      {
         get { return refreshTableDataCallback; }
         set { refreshTableDataCallback = value; }
      }

      IJavascriptCallback openSubformCallback;
      internal IJavascriptCallback OpenSubformCallback
      {
         get { return openSubformCallback; }
         set { openSubformCallback = value; }
      }
   }

   /// <summary>
   /// main interface to angular
   /// </summary>
   public class MagicBoundObject
   {
      Dictionary<string, TaskCallbacks> taskCallbackDictionary = new Dictionary<string, TaskCallbacks>();

      //IJavascriptCallback refreshDataCallback;
      //IJavascriptCallback refreshTableUICallback;
      //IJavascriptCallback getValueCallback;
      //IJavascriptCallback showMessageBoxCallback;
      IJavascriptCallback openFormCallback;
      IJavascriptCallback showMessageBoxCallback;
      IJavascriptCallback executeCommandsCallback;
      internal IJavascriptCallback ExecuteCommandsCallback
      {
         get {return executeCommandsCallback; }
         set { executeCommandsCallback = value; }
      }
      internal IJavascriptCallback ShowMessageBoxCallback
      {
         get { return showMessageBoxCallback; }
         set { showMessageBoxCallback = value; }
      }

      private Control controlToInvoke;
      public Control ControlToInvoke
      {
         get { return controlToInvoke; }
         set { controlToInvoke = value; }
      }
      public MagicBoundObject()
      {
         JSBridge.Instance.getControlValueDelegate = GetValue;
         JSBridge.Instance.refreshUIDelegate = RefreshDisplay;
         JSBridge.Instance.showMessageBoxDelegate = ShowMessageBox;
         JSBridge.Instance.refreshTableUIDelegate = RefreshTableDisplay;
         JSBridge.Instance.openFormDelegate = OpenForm;
         JSBridge.Instance.executeCommandsDelegate = ExecuteCommands;
         JSBridge.Instance.openSubformDelegate = OpenSubform;
      }

      /// <summary>
      /// register callback for opening form
      /// </summary>
      /// <param name="javascriptCallback"></param>
      public void registerOpenFormCallback(IJavascriptCallback javascriptCallback)
      {
         openFormCallback = javascriptCallback;
      }

      /// <summary>
      /// register callback for getting value from angular control
      /// </summary>
      /// <param name="javascriptCallback"></param>
      public void registerGetValueCallback(string taskId, IJavascriptCallback javascriptCallback)
      {
         getTaskCallbacks(taskId).GetValueCallback = javascriptCallback;

      }

      /// <summary>
      /// register callback for refreshing UI
      /// </summary>
      /// <param name="javascriptCallback"></param>
      public void registerRefreshUI(string taskId, IJavascriptCallback javascriptCallback)
      {

         getTaskCallbacks(taskId).RefreshDataCallback = javascriptCallback;
         //ClientManagerProxy.TaskFinishedInitialization(taskId);
      }

      /// <summary>
      /// register callback for showing message box
      /// </summary>
      /// <param name="javascriptsCallback"></param>
      public void registerShowMessageBox(IJavascriptCallback javascriptsCallback)
      {

         ShowMessageBoxCallback = javascriptsCallback;
      }

      /// <summary>
      /// register callback for opening a subform
      /// </summary>
      /// <param name="javascriptsCallback"></param>
      public void registerOpenSubformCallback(string taskId, IJavascriptCallback javascriptsCallback)
      {

         getTaskCallbacks(taskId).OpenSubformCallback = javascriptsCallback;
      }

      TaskCallbacks getTaskCallbacks(string taskId)
      {
         TaskCallbacks result;
         if (!taskCallbackDictionary.TryGetValue(taskId, out result))
            taskCallbackDictionary[taskId] = result = new TaskCallbacks();
         return result;
      }
      public void registerRefreshTableUI(string taskId, IJavascriptCallback javascriptCallback)
      {
         getTaskCallbacks(taskId).RefreshTableDataCallback = javascriptCallback;
      }

      public String GetValue(string taskId, string rowId, string controlName)
      {
         SyncExecutor syncExecutor = new SyncExecutor();
         TaskCallbacks callbacks = getTaskCallbacks(taskId);
         object result = "";
         if (callbacks != null && callbacks.GetValueCallback != null)
            result = syncExecutor.ExecuteSync(ControlToInvoke, callbacks.GetValueCallback, rowId, controlName);
         return result.ToString();
      }

      /// <summary>
      /// Execute Show MessageBox
      /// </summary>
      /// <param name="msg"></param>
      private void ShowMessageBox( string msg)
      {
         SyncExecutor syncExecutor = new SyncExecutor();
        
         if (ShowMessageBoxCallback != null)
            syncExecutor.ExecuteSync(ControlToInvoke, ShowMessageBoxCallback, msg);
      }

      public void Start(IJavascriptCallback javascriptCallback)
      {
         executeCommandsCallback = javascriptCallback;
         string[] args = Environment.GetCommandLineArgs();
         if (args.Length == 1)
            args = new string[] { };
         // if (args != null && args.Length > 1)
         //     args = new string[] { args[1]};

         ClientManagerProxy.Start(args);
      }

      public string getTaskId(string parentId, string subformName)
      {
         return ClientManagerProxy.GetTaskId(parentId, subformName);
      }

      public void InsertEvent(string taskId, string eventName, string controlName, string line)
      {

         ClientManagerProxy.AddEvent(taskId, eventName, controlName, line);
      }

      private void RefreshDisplay(string taskId, string UIDesctiption)
      {
         //TaskCallbacks callbacks = getTaskCallbacks(taskId);
         //if (callbacks != null && callbacks.RefreshDataCallback != null)
         //   callbacks.RefreshDataCallback.ExecuteAsync(UIDesctiption);
      }
      

      private void RefreshTableDisplay(string taskId, string UIDesctiption)
      {
         //TaskCallbacks callbacks = getTaskCallbacks(taskId);
         //if (callbacks != null && callbacks.RefreshDataCallback != null)
         //   callbacks.RefreshTableDataCallback.ExecuteAsync(UIDesctiption);
      }

      private void ExecuteCommands(string commands)
      {
         if (commands != null)
            this.ExecuteCommandsCallback.ExecuteAsync(commands);
        
      }

      /// <summary>
      /// Execute open Form
      /// </summary>
      /// <param name="msg"></param>
      private void OpenForm(string formName)
      {
         SyncExecutor syncExecutor = new SyncExecutor();
         if (openFormCallback != null)
            syncExecutor.ExecuteInUI(ControlToInvoke, openFormCallback, formName);
         //openFormCallback.ExecuteAsync(formName);
      }

      /// <summary>
      /// Execute open subform
      /// </summary>
      private void OpenSubform(string subformName, string parenttaskId, string formName, string taskId, string taskDescription)
      {
         SyncExecutor syncExecutor = new SyncExecutor();
         TaskCallbacks callbacks = getTaskCallbacks(parenttaskId);
         if (callbacks != null && callbacks.OpenSubformCallback != null)
            syncExecutor.ExecuteInUI(ControlToInvoke, callbacks.OpenSubformCallback, subformName, formName, taskId, taskDescription);
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
