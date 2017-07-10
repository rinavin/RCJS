﻿using CefSharp.MinimalExample.WinForms.Controls;
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
      IJavascriptCallback showMessageBoxCallback;
      internal IJavascriptCallback ShowMessageBoxCallback
      {
         get { return showMessageBoxCallback; }
         set { showMessageBoxCallback = value; }
      }
   }


   public class MagicBoundObject
	{
      Dictionary<string, TaskCallbacks> taskCallbackDictionary = new Dictionary<string, TaskCallbacks>();

		//IJavascriptCallback refreshDataCallback;
		//IJavascriptCallback refreshTableUICallback;
		//IJavascriptCallback getValueCallback;
		//IJavascriptCallback showMessageBoxCallback;

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
		}

		/// <summary>
		/// register callback for showing message box
		/// </summary>
		/// <param name="javascriptsCallback"></param>
		public void registerShowMessageBox(string taskId, IJavascriptCallback javascriptsCallback)
		{
			
         getTaskCallbacks(taskId).ShowMessageBoxCallback = javascriptsCallback;
		}

      TaskCallbacks getTaskCallbacks(string taskId)
      {
         TaskCallbacks result;
         if (!taskCallbackDictionary.TryGetValue(taskId, out result))
            taskCallbackDictionary[taskId] = result = new TaskCallbacks();
         return result;
      }
      //public void registerRefreshTableUI(string taskId, IJavascriptCallback javascriptCallback)
      //{
      //	JSBridge.Instance.refreshTableUIDelegate = RefreshTableDisplay;
      //	refreshTableUICallback = javascriptCallback;
      //}

      public String GetValue(string taskId, string controlName)
		{
			SyncExecutor syncExecutor = new SyncExecutor();
         TaskCallbacks callbacks = getTaskCallbacks(taskId);
         object result = "";
         if (callbacks != null && callbacks.GetValueCallback != null)
            result = syncExecutor.ExecuteSync(ControlToInvoke, callbacks.GetValueCallback, controlName);
			return result.ToString();
		}

		/// <summary>
		/// Execute Show MessageBox
		/// </summary>
		/// <param name="msg"></param>
		private void ShowMessageBox(string taskId, string msg)
		{
			SyncExecutor syncExecutor = new SyncExecutor();
         TaskCallbacks callbacks = getTaskCallbacks(taskId);
         if (callbacks != null && callbacks.ShowMessageBoxCallback!= null)
			   syncExecutor.ExecuteSync(ControlToInvoke, callbacks.ShowMessageBoxCallback, msg);
		}

		public void Start()
		{
			Runme.Start();
		}
     
      public string getTaskId(string parentId, string subformName)
      {
         return Runme.GetTaskId(parentId, subformName);
      }

      public void InsertEvent(string taskId, string eventName, string controlName, int line)
		{
        
			Runme.AddEvent(taskId, eventName, controlName, line);
		}

		private void RefreshDisplay(string taskId, string UIDesctiption)
		{
         TaskCallbacks callbacks = getTaskCallbacks(taskId);
         if (callbacks != null && callbacks.RefreshDataCallback != null)
            callbacks.RefreshDataCallback.ExecuteAsync(UIDesctiption);
		}


		//private void RefreshTableDisplay(string UIDesctiption)
		//{
		//	refreshTableUICallback.ExecuteAsync(UIDesctiption);
		//}
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
