using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.controls;
using System.Diagnostics;

namespace com.magicsoftware.unipaas.gui.low
{
   /// <summary>
   /// structure similar to ctx->win_ - holds all open forms for each context according to its creation
   /// </summary>
   static public class ContextForms
   {
      /// <summary>
      /// holds forms for each context
      /// </summary>
      static Dictionary<long, List<GuiForm>> forms = new Dictionary<long, List<GuiForm>>();

      /// <summary>
      /// add form to context
      /// </summary>
      /// <param name="form"></param>
      static public void AddForm(GuiForm form)
      {
         long contextid = Manager.GetCurrentContextID();
         List<GuiForm> formsList = null;
         if (!forms.TryGetValue(contextid, out formsList))
         {
            formsList = new List<GuiForm>();
            forms[contextid] = formsList;
         }
         formsList.Add(form);
      }

      /// <summary>
      /// remove form
      /// </summary>
      /// <param name="form"></param>
      static public void RemoveForm(GuiForm form)
      {
         long contextid = Manager.GetCurrentContextID();
         List<GuiForm> formsList = null;
         if (forms.TryGetValue(contextid, out formsList))
         {
            formsList.Remove(form);
            if (formsList.Count == 0)
               forms.Remove(contextid);

         }
      }

      /// <summary>
      /// check is this form is last opened form  of a context
      /// </summary>
      /// <param name="form"></param>
      /// <returns></returns>
      static public bool IsLastForm(GuiForm form)
      {
         long contextid = Manager.GetCurrentContextID();
         List<GuiForm> formsList = null;
         if (forms.TryGetValue(contextid, out formsList))
         {
            int index = formsList.IndexOf(form);
            return index == formsList.Count - 1;
         }
         Debug.Assert(true);
         return true;
      }

      /// <summary>
      /// returns true if form belongs to current context
      /// </summary>
      /// <param name="form"></param>
      /// <returns></returns>
      static public bool BelongsToCurrentContext(GuiForm form)
      {
         long contextid = Manager.GetCurrentContextID();
         List<GuiForm> formsList = null;
         bool result = false;
         if (forms.TryGetValue(contextid, out formsList))
            result = formsList.Contains(form);
         Debug.Assert(true);
         return result;
      }
      /// <summary>
      /// find next form of a context
      /// </summary>
      /// <param name="form"></param>
      /// <returns></returns>
      static public GuiForm GetNextForm(GuiForm form)
      {
         long contextid = Manager.GetCurrentContextID();
         List<GuiForm> formsList = null;
         if (forms.TryGetValue(contextid, out formsList))
         {
            int index = formsList.IndexOf(form) + 1;
            if (index < formsList.Count)
               return formsList[index];
         }
         return null;
      }

      /// <summary>
      /// returns true if there is a blocking batcj task above this one
      /// </summary>
      /// <param name="form"></param>
      /// <returns></returns>
      public static bool IsBlockedByMdiForm(GuiForm form)
      {
         long contextid = Manager.GetCurrentContextID();
         List<GuiForm> formsList = null;
         if (forms.TryGetValue(contextid, out formsList))
         {
            bool upperform = false;
            foreach (var item in formsList)
            {
              
               if (upperform)
               {
                  //we found blocking batch that is child of current form
                  if (item.ShouldBlockAnsestorActivation && !item.IsDisposed)
                     return true;
               }
               //go until we find out form in the list
               if (item == form)
                  upperform = true;

            }
         }
         return false;
      }

      /// <summary>
      /// move the form to be the last form in the context
      /// </summary>
      /// <param name="form"></param>
      public static void MoveToEnd(GuiForm form)
      {
         long contextid = Manager.GetCurrentContextID();
         List<GuiForm> formsList = null;
         if (forms.TryGetValue(contextid, out formsList))
         {
            int index = formsList.IndexOf(form) ;
            if (index < formsList.Count - 1) //not last form
            {
               formsList.Remove(form);
               formsList.Add(form); //move to the end
            }
         }
      
      }

      /// <summary>
      /// get last child batch blocking this form
      /// </summary>
      /// <param name="form"></param>
      /// <returns></returns>
      public static GuiForm GetBlockingFormToActivate(GuiForm form)
      {

         long contextid = Manager.GetCurrentContextID();
         List<GuiForm> formsList = null;
         if (forms.TryGetValue(contextid, out formsList))
         {
            for (int i = formsList.Count - 1; i >= 0; i--)
            {
               
               GuiForm currentForm = formsList[i];
               if (currentForm == form) //we checked all children of current form
                  break;
               //we found blocking batch that is child of current form
               if (currentForm.ShouldBlockAnsestorActivation && !currentForm.IsDisposed)
                  return currentForm;

            }
            
         }
         return null;
      }

   }
}
