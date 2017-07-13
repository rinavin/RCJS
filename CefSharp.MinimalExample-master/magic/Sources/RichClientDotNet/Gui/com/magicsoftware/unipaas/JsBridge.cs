﻿using System;
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
        public delegate string GetControlValueDelegate(string taskId, string controlName);
        public delegate void ShowMessageBoxDelegate(string taskId, string msg);
        public delegate void OpenFormDelegate(string formName);


        public RefreshUIDelegate refreshUIDelegate;
        public RefreshUIDelegate refreshTableUIDelegate;
        public GetControlValueDelegate getControlValueDelegate;
        public ShowMessageBoxDelegate showMessageBoxDelegate;
        public OpenFormDelegate openFormDelegate;

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

      public string GetControlValue(string taskId, string controlName)
        {
            if (getControlValueDelegate != null)
                return getControlValueDelegate(taskId, controlName);
            return "";
        }

      public void ShowMessageBox(string taskId, string msg)
      {
         if (showMessageBoxDelegate != null)
            showMessageBoxDelegate(taskId, msg);
      }

      public void OpenForm()
      {
         if (openFormDelegate != null)
            openFormDelegate("Demo1");
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
