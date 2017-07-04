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
        public delegate void RefreshUIDelegate(string UIDesctiption);
        public delegate string GetControlValueDelegate(string controlName);
        public delegate void ShowMessageBoxDelegate(string msg);


        public RefreshUIDelegate refreshUIDelegate;
        public RefreshUIDelegate refreshTableUIDelegate;
        public GetControlValueDelegate getControlValueDelegate;
        public ShowMessageBoxDelegate showMessageBoxDelegate;

        public void RefreshUI(string UIDesctiption)
        {
            if (refreshUIDelegate != null)
                refreshUIDelegate(UIDesctiption);
        }

        public void RefreshTableUI(string UIDesctiption)
        {
            if (refreshTableUIDelegate != null)
                refreshTableUIDelegate(UIDesctiption);
        }

        public string GetControlValue(string controlName)
        {
            if (getControlValueDelegate != null)
                return getControlValueDelegate(controlName);
            return "";
        }

      public void ShowMessageBox(string msg)
      {
         if (showMessageBoxDelegate != null)
            showMessageBoxDelegate(msg);
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
