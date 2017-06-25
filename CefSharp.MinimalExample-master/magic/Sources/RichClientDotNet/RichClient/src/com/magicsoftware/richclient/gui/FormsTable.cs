using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.util;
using System.Windows.Forms;
using com.magicsoftware.unipaas.management.tasks;
using com.magicsoftware.unipaas;
using com.magicsoftware.unipaas.util;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.richclient.tasks;
using util.com.magicsoftware.util;
using com.magicsoftware.util.Xml;

namespace com.magicsoftware.richclient.gui
{
   /// <summary>
   /// save all forms on task
   /// </summary>
   public class FormsTable
   {
      private Task _task;
      private MgFormBase _parentForm;
      protected MgArrayList _formsStringXml { get; private set; }

      /// <summary>
      /// count of forms
      /// </summary>
      public int Count
      {
         get 
         {
            return _formsStringXml.Count;
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="task"></param>
      /// <param name="parentForm"></param>
      public FormsTable(TaskBase task, MgFormBase parentForm)
      {
         this._task = (Task)task;
         this._parentForm = parentForm;
      }

      /// <summary>
      ///   parse the form structure
      /// </summary>
      /// <param name = "taskRef">reference to the ownerTask of this form</param>
      public void fillData()
      {
         _formsStringXml = new MgArrayList();

         XmlParser parser = Manager.GetCurrentRuntimeContext().Parser;
       
         while (initInnerObjects(parser.getNextTag()))
         {
         }
      }

      /// <summary>
      /// init the form of the task
      /// </summary>
      /// <param name="formDisplayIndex"></param>
      internal void InitFormFromXmlString(int formDisplayIndex)
      {         
         // while the form isn't exist the first form will be open. (it is the same in non offline task) 
         String formStrXml = this[1];

         if (formDisplayIndex > 0 && formDisplayIndex <= _formsStringXml.Count)
            formStrXml = this[formDisplayIndex];

         if (formStrXml != null)
         {

            ClientManager.Instance.RuntimeCtx.Parser.push(); // allow recursive parsing
            ClientManager.Instance.RuntimeCtx.Parser.PrepareFormReadString(formStrXml);

            _task.FormInitData(_parentForm);

            ClientManager.Instance.RuntimeCtx.Parser.pop();
            
         }
      }

      /// <summary>
      ///   To allocate and fill inner objects of the class
      /// </summary>
      /// <param name="foundTagName"></param>
      /// <returns></returns>
      private bool initInnerObjects(String foundTagName)
      {
         if (foundTagName == null)
            return false;

         XmlParser parser = Manager.GetCurrentRuntimeContext().Parser;

         if (foundTagName.Equals(XMLConstants.MG_TAG_FORMS))
         {
            parser.setCurrIndex(parser.getXMLdata().IndexOf(XMLConstants.TAG_CLOSE, parser.getCurrIndex()) +
                                1); // move Index to end of <dvheader> +1 for '>'
         }
         if (foundTagName.Equals(XMLConstants.MG_TAG_FORM))
         {
            String formStringXml = ClientManager.Instance.RuntimeCtx.Parser.ReadToEndOfCurrentElement();
            _formsStringXml.Add(formStringXml);
            return true;
         }       
         else if (foundTagName.Equals('/' + XMLConstants.MG_TAG_FORM))
         {
            parser.setCurrIndex2EndOfTag();
            return true;
         }
         else if (foundTagName.Equals('/' + XMLConstants.MG_TAG_FORMS))
         {
            PrepareMainDisplayForConnectedTask();

            parser.setCurrIndex2EndOfTag();
            return false;
         }
         return true;
      }

      /// <summary>
      /// for remote task we have all the information that we need to calculate the form display
      /// </summary>
      private void PrepareMainDisplayForConnectedTask()
      {         
         if (_task.TaskService is RemoteTaskService)
            _task.PrepareTaskForm();
      }     
      
      /// <summary>
      /// return the form by send the display index
      /// </summary>
      /// <param name="formDisplayIndex"></param>
      /// <returns></returns>
      public String this[int formDisplayIndex]
      {
         get
         {
            if (formDisplayIndex > 0 && formDisplayIndex <= _formsStringXml.Count)
               return (String)_formsStringXml[formDisplayIndex - 1];

            return null;
         }
      }
   }
}