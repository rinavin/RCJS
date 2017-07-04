using CefSharp.MinimalExample.WinForms.Controls;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CefSharp.MinimalExample.WinForms
{
	/// <summary>
	/// Synchronized execution class
	/// </summary>
   class SyncExecutor
   {
		/// <summary>
		/// Executes callback synchronizely 
		/// </summary>
		/// <param name="controlToInvoke"></param>
		/// <param name="param"></param>
		/// <param name="callback"></param>
		/// <returns></returns>
		public object ExecuteSync(Control controlToInvoke, IJavascriptCallback callback, params object[] param)
		{
			object result = "null";
			AutoResetEvent JSSyncAutoResetEvent = new AutoResetEvent(false);
			controlToInvoke.InvokeOnUiThreadIfRequired(() =>
			{
				var task = callback.ExecuteAsync(param);

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

			return result;
		}
   }
}
