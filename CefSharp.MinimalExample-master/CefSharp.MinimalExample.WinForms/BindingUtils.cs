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
		IJavascriptCallback refreshTableUICallback;
		IJavascriptCallback getValueCallback;
		IJavascriptCallback showMessageBoxCallback;

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

		/// <summary>
		/// register callback for showing message box
		/// </summary>
		/// <param name="javascriptsCallback"></param>
		public void registerShowMessageBox(IJavascriptCallback javascriptsCallback)
		{
			JSBridge.Instance.showMessageBoxDelegate = ShowMessageBox;
			showMessageBoxCallback = javascriptsCallback;
		}

		public void registerRefreshTableUI(IJavascriptCallback javascriptCallback)
		{
			JSBridge.Instance.refreshTableUIDelegate = RefreshTableDisplay;
			refreshTableUICallback = javascriptCallback;
		}

		public String GetValue(string controlName)
		{
			SyncExecutor syncExecutor = new SyncExecutor();
			object result = syncExecutor.ExecuteSync(ControlToInvoke, getValueCallback, controlName);
			return result.ToString();
		}

		/// <summary>
		/// Execute Show MessageBox
		/// </summary>
		/// <param name="msg"></param>
		private void ShowMessageBox(string msg)
		{
			SyncExecutor syncExecutor = new SyncExecutor();
			syncExecutor.ExecuteSync(ControlToInvoke, showMessageBoxCallback, msg);
		}

		public void Start()
		{
			Runme.Start();
		}
     
      public string getTaskId(string parentId, string subformName)
      {
         return Runme.GetTaskId(parentId, subformName);
      }

      public void InsertEvent(string eventName, string controlName, int line)
		{
        
			Runme.AddEvent(eventName, controlName, line);
		}

		private void RefreshDisplay(string UIDesctiption)
		{
			refreshDataCallback.ExecuteAsync(UIDesctiption);
		}


		private void RefreshTableDisplay(string UIDesctiption)
		{
			refreshTableUICallback.ExecuteAsync(UIDesctiption);
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
