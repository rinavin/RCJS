using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace com.magicsoftware.unipaas
{
    /// <summary>
    /// this class will be used for communication with JS
    /// it will hold delegates with callbacks for the JS 
    /// </summary>
    public class JSBridge
    {
        JSBridge()
        {

        }
        public delegate void RefreshUIDelegate(string taskId, string UIDesctiption);
        public delegate string GetControlValueDelegate(string taskId, string rowId, string controlName);
        public delegate void ShowMessageBoxDelegate(string msg);
        public delegate void ExecuteCommandsDelegate(string commands);
        public delegate void OpenFormDelegate(string formName);
        public delegate void OpenSubformDelegate(string subformName, string parenttaskId, string formName, string taskId, string taskDesciption);


        public RefreshUIDelegate refreshUIDelegate;
        public RefreshUIDelegate refreshTableUIDelegate;
        public GetControlValueDelegate getControlValueDelegate;
        public ShowMessageBoxDelegate showMessageBoxDelegate;
        public OpenFormDelegate openFormDelegate;
        public OpenSubformDelegate openSubformDelegate;
        public ExecuteCommandsDelegate executeCommandsDelegate;


      public void executeCommands(string commands)
      {
         if (executeCommandsDelegate != null && commands != null)
            executeCommandsDelegate(commands);
      }
      public void RefreshUI(string taskId, string UIDesctiption)
        {
            if (refreshUIDelegate != null)
                refreshUIDelegate(taskId, UIDesctiption);
        }

      public void RefreshTableUI(string taskId, string UIDesctiption)
      {
         if (refreshTableUIDelegate != null)
            refreshTableUIDelegate(taskId, UIDesctiption);
      }

      public string GetControlValue(string taskId, string rowId, string controlName)
        {
            if (getControlValueDelegate != null)
                return getControlValueDelegate(taskId, rowId, controlName);
            return "";
        }

      public void ShowMessageBox(string msg)
      {
         if (showMessageBoxDelegate != null)
            showMessageBoxDelegate(msg);
      }

      public void OpenForm(string name)
      {
         if (openFormDelegate != null)
            openFormDelegate(name); //"Demo2"
      }

      public void OpenSubForm(string subformName, string parenttaskId, string formName, string taskId, string taskDesciption)
      {
         if (openSubformDelegate != null)
            openSubformDelegate(subformName, parenttaskId, formName, taskId, taskDesciption);
      }

        private static JSBridge instance;
        public static JSBridge Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (typeof(JSBridge))
                    {
                        if (instance == null)
                            instance = new JSBridge();
                    }
                }
                return instance;
            }

            private set
            {
                instance = value;
            }
        }
    }
}
