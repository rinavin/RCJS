using com.magicsoftware.unipaas;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace com.magicsoftware.richclient
{
   internal class TaskCallbacks
   {
      dynamic refreshDataCallback;
      internal dynamic RefreshDataCallback
      {
         get { return refreshDataCallback; }
         set { refreshDataCallback = value; }
      }
      // IJavascriptCallback refreshTableUICallback;
      dynamic getValueCallback;
      internal dynamic GetValueCallback
      {
         get { return getValueCallback; }
         set { getValueCallback = value; }
      }


      dynamic refreshTableDataCallback;
      internal dynamic RefreshTableDataCallback
      {
         get { return refreshTableDataCallback; }
         set { refreshTableDataCallback = value; }
      }

      dynamic openSubformCallback;
      internal dynamic OpenSubformCallback
      {
         get { return openSubformCallback; }
         set { openSubformCallback = value; }
      }

      dynamic closeFormCallback;
      internal dynamic CloseFormCallback
      {
         get { return closeFormCallback; }
         set { closeFormCallback = value; }
      }

      dynamic setFocusCallback;
      internal dynamic SetFocusCallback
      {
         get { return setFocusCallback; }
         set { setFocusCallback = value; }
      }
   }

   /// <summary>
   /// main interface to angular
   /// </summary>
   [ComVisible(true)]
   /// <summary> The main class of the Rich Client</summary>
   [Guid("89793050-CD62-4183-ACFF-AB036743333D")]
   [ClassInterface(ClassInterfaceType.AutoDispatch)] //signify that this calss also implements the IDspatch interface
   public class MagicBoundObject : IObjectWithSite
   {
      Dictionary<string, TaskCallbacks> taskCallbackDictionary = new Dictionary<string, TaskCallbacks>();

      //IJavascriptCallback refreshDataCallback;
      //IJavascriptCallback refreshTableUICallback;
      //IJavascriptCallback getValueCallback;
      //IJavascriptCallback showMessageBoxCallback;
      dynamic openFormCallback;
      dynamic showMessageBoxCallback;
      internal dynamic ShowMessageBoxCallback
      {
         get { return showMessageBoxCallback; }
         set { showMessageBoxCallback = value; }
      }

      //private Control controlToInvoke;
      //public Control ControlToInvoke
      //{
      //   get { return controlToInvoke; }
      //   set { controlToInvoke = value; }
      //}
      public MagicBoundObject()
      {
         JSBridge.Instance.getControlValueDelegate = GetValue;
         JSBridge.Instance.refreshUIDelegate = RefreshDisplay;
         JSBridge.Instance.showMessageBoxDelegate = ShowMessageBox;
         JSBridge.Instance.refreshTableUIDelegate = RefreshTableDisplay;
         JSBridge.Instance.openFormDelegate = OpenForm;
         JSBridge.Instance.closeFormDelegate = CloseForm;
         JSBridge.Instance.openSubformDelegate = OpenSubform;
         JSBridge.Instance.setFocusDelegate = SetFocus;
      }

      /// <summary>
      /// register callback for opening form
      /// </summary>
      /// <param name="javascriptCallback"></param>
      public void registerOpenFormCallback(object javascriptCallback)
      {

         openFormCallback = javascriptCallback;
      }

      /// <summary>
      /// register callback for getting value from angular control
      /// </summary>
      /// <param name="javascriptCallback"></param>
      public void registerGetValueCallback(string taskId, object javascriptCallback)
      {
         getTaskCallbacks(taskId).GetValueCallback = javascriptCallback;

      }

      /// <summary>
      /// register callback for refreshing UI
      /// </summary>
      /// <param name="javascriptCallback"></param>
      public void registerRefreshUI(string taskId, object javascriptCallback)
      {

         getTaskCallbacks(taskId).RefreshDataCallback = javascriptCallback;
         //ClientManagerProxy.TaskFinishedInitialization(taskId);
      }

      /// <summary>
      /// register callback for showing message box
      /// </summary>
      /// <param name="javascriptsCallback"></param>
      public void registerShowMessageBox(object javascriptsCallback)
      {

         ShowMessageBoxCallback = javascriptsCallback;
      }

      /// <summary>
      /// register callback for opening a subform
      /// </summary>
      /// <param name="javascriptsCallback"></param>
      public void registerOpenSubformCallback(string taskId, object javascriptsCallback)
      {

         getTaskCallbacks(taskId).OpenSubformCallback = javascriptsCallback;
      }

      /// <summary>
      /// register callback for close form
      /// </summary>
      /// <param name="javascriptsCallback"></param>
      public void registerCloseFormCallback(string taskId, object javascriptsCallback)
      {

         getTaskCallbacks(taskId).CloseFormCallback = javascriptsCallback;
      }

      public void registerSetFocusCallback(string taskId, object javascriptCallback)
      {
         getTaskCallbacks(taskId).SetFocusCallback = javascriptCallback;
      }

      TaskCallbacks getTaskCallbacks(string taskId)
      {
         TaskCallbacks result;
         if (!taskCallbackDictionary.TryGetValue(taskId, out result))
            taskCallbackDictionary[taskId] = result = new TaskCallbacks();
         return result;
      }
      public void registerRefreshTableUI(string taskId, object javascriptCallback)
      {
         getTaskCallbacks(taskId).RefreshTableDataCallback = javascriptCallback;
      }

      public String GetValue(string taskId, string controlName)
      {
         TaskCallbacks callbacks = getTaskCallbacks(taskId);
         object result = "";
         if (callbacks != null && callbacks.GetValueCallback != null)
            result = callbacks.GetValueCallback(controlName);
         return result.ToString();
      }

      /// <summary>
      /// Execute Show MessageBox
      /// </summary>
      /// <param name="msg"></param>
      private void ShowMessageBox(string msg)
      {
         if (ShowMessageBoxCallback != null)
            ShowMessageBoxCallback(msg);
      }

      public void Start()
      {
         string[] args = new string[] { };
         //string[] args = Environment.GetCommandLineArgs();
         //if (args.Length == 1)
         //   args = new string[] { };
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
         TaskCallbacks callbacks = getTaskCallbacks(taskId);
         if (callbacks != null && callbacks.RefreshDataCallback != null)
            callbacks.RefreshDataCallback(UIDesctiption);
      }


      private void RefreshTableDisplay(string taskId, string UIDesctiption)
      {
         TaskCallbacks callbacks = getTaskCallbacks(taskId);
         if (callbacks != null && callbacks.RefreshDataCallback != null)
            callbacks.RefreshTableDataCallback(UIDesctiption);
      }

      /// <summary>
      /// Execute open Form
      /// </summary>
      /// <param name="msg"></param>
      private void OpenForm(string formName, string taskId, string taskDesciption, bool isModal)
      {
         if (openFormCallback != null)
            openFormCallback(formName, taskId, taskDesciption, isModal);
         //openFormCallback.ExecuteAsync(formName);
      }

      private void CloseForm(string taskId)
      {
         TaskCallbacks callbacks = getTaskCallbacks(taskId);
         if (callbacks != null && callbacks.CloseFormCallback != null)
            callbacks.CloseFormCallback();
      }

      /// <summary>
      /// Execute open subform
      /// </summary>
      private void OpenSubform(string subformName, string parenttaskId, string formName, string taskId, string taskDescription)
      {
         TaskCallbacks callbacks = getTaskCallbacks(parenttaskId);
         if (callbacks != null && callbacks.OpenSubformCallback != null)
            callbacks.OpenSubformCallback(subformName, formName, taskId, taskDescription);
      }

      private void SetFocus(string taskId, string controlId)
      {
         TaskCallbacks callbacks = getTaskCallbacks(taskId);
         if (callbacks != null && callbacks.SetFocusCallback != null)
            callbacks.SetFocusCallback(controlId);
      }


      /*******************************************************************************************************
         *              INTEROP PACKAGING CODE                                                                 *
         *******************************************************************************************************/
      private object m_IUnkSite; // the site of the control is set automaticlly by the container 

      //implementation of the IObjectWithSite interface without it we can not communicate with the container
      [DispId(1)]
      public void SetSite(object pUnkSite)
      {

         //System.Diagnostics.Debugger.Launch();
         // per SetSite spec, we should release old interface
         // before storing a new one.
         if (m_IUnkSite != null)
         {
            //QCR #983341 - releasing com resources on the page
            releaseResources();
         }
         m_IUnkSite = pUnkSite;

         ////now that we have a site lets init the client
         //// if we already have the document
         //if (pUnkSite != null)
         //   webBrowser = (WebBrowser)pUnkSite;
         //{
         //   //try
         //   //{
         //   //   setWebBrowser();

         //   //   if (CheckInstallMshtml())
         //   //   {
         //   //      IOleClientSite site = (IOleClientSite)m_IUnkSite;
         //   //      site.GetContainer(out container);

         //   //      init();
         //   //   }
         //   //}
         //   //catch (ApplicationException e)
         //   //{
         //   //   writeErrorToLog(e.Message);
         //   //}
         //}
      }
      /*
         * also an implementation of the IObjectWithSite interface 
         * we supply this implementation for use of "outside" components although we do not
         * use it internally sine we have direct access to the site object
        */
      [DispId(2)]
      public void GetSite(ref Guid riid, IntPtr ppvSite)
      {
         const int e_fail = unchecked((int)0x80004005);

         if (!ppvSite.Equals((IntPtr)0))
         {
            IntPtr pvSite = (IntPtr)0;
            // be a good COM interface imp - NULL the destination ptr first
            Marshal.WriteIntPtr(ppvSite, pvSite);

            if (m_IUnkSite != null)
            {
               IntPtr pUnk = Marshal.GetIUnknownForObject(m_IUnkSite);
               Marshal.QueryInterface(pUnk, ref riid, out pvSite);
               Marshal.Release(pUnk); // GetIUnknownForObject AddRefs so Release

               if (!pvSite.Equals((IntPtr)0))
                  Marshal.WriteIntPtr(ppvSite, pvSite);
               else
                  Marshal.ThrowExceptionForHR(e_fail);
            }
         }
         else
            Marshal.ThrowExceptionForHR(e_fail);
      }

      /**
         * com registretion functiom mainly for safe for scriptitg and safe fron running
         * this function will be invoked when the regAsm util will be run on this objec
         * this method wil add safe for running and safe for scripting in the regisetry 
         */
      [ComRegisterFunctionAttribute()]
      static void RegisterServer(String zRegKey)
      {
         //System.Diagnostics.Debugger.Launch();

         try
         {
            RegistryKey root;
            RegistryKey rk;

            root = Registry.LocalMachine;
            rk = root.OpenSubKey(@"SOFTWARE\Classes\CLSID\{89793050-CD62-4183-ACFF-AB036743333D}\Implemented Categories\", true);
            rk.CreateSubKey("{7DD95802-9882-11CF-9FA9-00AA006C42C4}");
            rk.CreateSubKey("{7DD95801-9882-11CF-9FA9-00AA006C42C4}");


            rk.Close();
         }
         catch (SystemException)
         {
         }
      }

      /**
         * since in the registretion process we added SubKeys we need to remove them in the un-register
         * this function will be invoked when the regAsm util we be run with "/unregister" parameter
         */
      [ComUnregisterFunctionAttribute()]
      static void UnRegisterServer(String zRegKey)
      {
         try
         {
            RegistryKey root;
            RegistryKey rk;

            root = Registry.LocalMachine;
            rk = root.OpenSubKey(@"SOFTWARE\Classes\CLSID\{89793050-CD62-4183-ACFF-AB036743333D}\Implemented Categories\", true);
            rk.DeleteSubKey("{7DD95802-9882-11CF-9FA9-00AA006C42C4}");
            rk.DeleteSubKey("{7DD95801-9882-11CF-9FA9-00AA006C42C4}");
            rk.Close();
         }
         catch (SystemException)
         {
         }

      }

      /// <summary>releasing com resources on the page</summary>
      /// <returns></returns>
      protected internal void releaseResources()
      {
         try
         {
            //release the site
            while (Marshal.ReleaseComObject(m_IUnkSite) > 0) ;
            m_IUnkSite = null;
         }
         catch
         {
            //MessageBox.Show("in setSite exception thrown");
         }
      }

      /*******************************************************************************************************
         *              INTEROP PACKAGING CODE FINSHED                                                                *
         *******************************************************************************************************/
   }
}
