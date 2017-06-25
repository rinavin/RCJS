using System;
using System.Collections;
using System.Collections.Generic;
using com.magicsoftware.richclient.data;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.richclient.util;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.unipaas.util;
using com.magicsoftware.util;
using com.magicsoftware.richclient.gui;
using com.magicsoftware.richclient.remote;
using util.com.magicsoftware.util;
using com.magicsoftware.util.Xml;
using com.magicsoftware.richclient.commands;
using com.magicsoftware.richclient.commands.ClientToServer;

namespace com.magicsoftware.richclient.rt
{
   internal class Recompute
   {
      private PropTable _ctrlProps; // affected control properties
      private PropTable _formProps; // affected form properties
      private bool _isOwnerFld = true; // denotes if the current parsed field tag is the owner field
      internal Field OwnerFld; // the field that owns this recompute structure
      internal RcmpBy RcmpMode { get; set; } // Who (and when) performs rcmp (by client,
      private ArrayList _rcmpOrder; // affected fields and links. Can't use List<T> - holds both fields and links
      private List<Task> _subForms; // subforms (tasks) affected by this recompute
      private bool _subFormsOnlyRecomp;
      private bool _hasServerLinksRecomputes; //true, if the field has any link recomputes and it is a server recompute
      internal Task Task { get; set; } // a reference to the task of this Recompute

      /// <summary>
      ///   to fill all relevant data for <fld ...> ...</fld> tag
      /// </summary>
      /// <param name = "dataView">reference to all DataView object, which consist relevant fields </param>
      /// <param name = "taskRef">to parent task, which need be initialized in ControlTable.Control.task </param>
      /// <returns> index of the end of current <fld ...>=> </fld> </returns>
      internal void fillData(DataView dataView, Task taskRef)
      {
         XmlParser parser = ClientManager.Instance.RuntimeCtx.Parser;

         Task = taskRef;
         _isOwnerFld = true; // start of new <fld> tag
         while (initInnerObjects(parser, parser.getNextTag(), dataView))
         {
         }
      }

      /// <summary>
      ///   Fill inner members and needed links for the current 'fld'
      /// </summary>
      /// <param name = "((DataView)dataView)">reference to all DataView object, which consist relevant fields </param>
      private bool initInnerObjects(XmlParser parser, String foundTagName, DataView dataView)
      {
         switch (foundTagName)
         {
            case XMLConstants.MG_TAG_FLD:
               if (_isOwnerFld)
                  // init <fld id = recompute_by=> first time, init reference by id
               {
                  fillFldField(parser, dataView);
                  _isOwnerFld = false; // must to be after FillFldField
               }
               else
                  fillFldField(parser, dataView); // it's not need ((DataView)dataView), it's not first <fld >
               break;

            case ConstInterface.MG_TAG_LINK:
               fillLink(parser, dataView);
               break;

            case XMLConstants.MG_TAG_CONTROL:
               if (_ctrlProps == null)
                  _ctrlProps = new PropTable();

               // fill the prop table using the existing properties
               _ctrlProps.fillDataByExists(Task);
               
               //if virtual field causes recompute of repeatable control 
               if (_ctrlProps.getCtrlRef() != null && _ctrlProps.getCtrlRef().IsRepeatable && OwnerFld.IsVirtual &&
                   (!(OwnerFld.hasInitExp())))
                  OwnerFld.causeTableInvalidation(true);
               break;

            case XMLConstants.MG_TAG_FORM_PROPERTIES:
               if (_formProps == null)
                  _formProps = new PropTable();

               // fill the prop table using the existing properties
               _formProps.fillDataByExists(Task);
               break;

            case ConstInterface.MG_TAG_FLD_END:
               parser.setCurrIndex2EndOfTag();
               return false;

            case XMLConstants.MG_TAG_DCVALUES:
               string segment = parser.ReadToEndOfCurrentElement();
               var handler = new DCValuesRecomputeSaxHandler(Task);
               MgSAXParser.Parse(segment, handler);
               AddRecomputeItem(handler.DcValuesRecomputeAction);
               break;

            default:
               Logger.Instance.WriteExceptionToLog(
                  "There is no such tag in Recompute. Insert else if to Recompute.initInnerObjects for " + foundTagName);
               return false;
         }
         return true;
      }

      private void AddRecomputeItem(object item)
      {
         if (_rcmpOrder == null)
            _rcmpOrder = new ArrayList();
         _rcmpOrder.Add(item);
      }

      /// <summary>
      ///   fill the recomputed link data (only id)
      /// </summary>
      private void fillLink(XmlParser parser, DataView dataView)
      {
         int endContext = parser.getXMLdata().IndexOf(XMLConstants.TAG_TERM, parser.getCurrIndex());
         if (endContext != -1 && endContext < parser.getXMLdata().Length)
         {
            // last position of its tag 
            String tag = parser.getXMLsubstring(endContext);
            parser.add2CurrIndex(tag.IndexOf(ConstInterface.MG_TAG_LINK) + ConstInterface.MG_TAG_LINK.Length);

            List<string> tokensVector = XmlParser.getTokens(parser.getXMLsubstring(endContext), XMLConstants.XML_ATTR_DELIM);

            for (int j = 0; j < tokensVector.Count; j += 2)
            {
               string attribute = (tokensVector[j]);
               string valueStr = (tokensVector[j + 1]);

               if (attribute.Equals(XMLConstants.MG_ATTR_ID))
               {
                  AddRecomputeItem((dataView.getTask()).getDataviewHeaders().getDataviewHeaderById(XmlParser.getInt(valueStr)));
               }
               else
                  Logger.Instance.WriteExceptionToLog(
                     "There is no such tag in Recompute class. Insert case to Recompute.fillLink for " + attribute);
            }

            parser.setCurrIndex(endContext + XMLConstants.TAG_TERM.Length); // to delete "/>" too
            return;
         }
         else
            Logger.Instance.WriteExceptionToLog("in Recompute.fillLink() out of bounds");
      }

      /// <summary>
      ///   Init RecomputeBy member and init reference from the Field to the Recompute object
      /// </summary>
      /// <param name = "xmlParser.getXMLdata()">, input string full XML.innerText </param>
      /// <param name = "xmlParser.getCurrIndex()">, index of start first <fld ...> tag </param>
      /// <param name = "((DataView)dataView)">reference to all DataView object, which consist relevant fields </param>
      /// <returns> index of end of <fld ...> tag </returns>
      private void fillFldField(XmlParser parser, DataView dataView)
      {
         int endContext = parser.getXMLdata().IndexOf(_isOwnerFld
                                                            ? XMLConstants.TAG_CLOSE
                                                            : XMLConstants.TAG_TERM, parser.getCurrIndex());

         if (endContext != -1 && endContext < parser.getXMLdata().Length)
         {
            // last position of its tag 
            String tag = parser.getXMLsubstring(endContext);
            parser.add2CurrIndex(tag.IndexOf(XMLConstants.MG_TAG_FLD) + XMLConstants.MG_TAG_FLD.Length);

            List<string> tokensVector = XmlParser.getTokens(parser.getXMLsubstring(endContext),
                                                            XMLConstants.XML_ATTR_DELIM);
            initElements(tokensVector, dataView);
            if (_isOwnerFld)
            {
               parser.setCurrIndex(++endContext); // to delete ">" too
               return;
            }
            parser.setCurrIndex(endContext + XMLConstants.TAG_TERM.Length); // to delete "/>" too
            return;
         }
         Logger.Instance.WriteExceptionToLog("in Command.FillData() out of bounds");
      }

      /// <summary>
      ///   Make initialization of private elements by found tokens
      /// </summary>
      /// <param name = "tokensVector">found tokens, which consist attribute/value of every found element </param>
      /// <param name = "((DataView)dataView)">reference to field objects, for <fld id = ...>
      ///                                                            if ((DataView)dataView)==null=> it's first found field, need to init reference
      ///                                                            from the Field
      ///                                                            if ((DataView)dataView)!=null=> add the data to currient Record, it's not first <fld> 
      /// </param>
      private void initElements(List<String> tokensVector, DataView dataView)
      {
         for (int j = 0; j < tokensVector.Count; j += 2)
         {
            string attribute = (tokensVector[j]);
            string valueStr = (tokensVector[j + 1]);

            switch (attribute)
            {
               case XMLConstants.MG_ATTR_ID:
                  {
                     int fldId = XmlParser.getInt(valueStr);
                     var fld = (Field) dataView.getField(fldId);
                     if (_isOwnerFld)
                     {
                        OwnerFld = fld;
                        OwnerFld.setRecompute(this);
                     }
                     else
                     {
                        fld.setRecomputed();
                        AddRecomputeItem(fld);
                     }
                     break;
                  }

               case ConstInterface.MG_ATTR_RECOMPUTEBY:
                  RcmpMode = (RcmpBy) valueStr[0];
                  break;

               case ConstInterface.MG_ATTR_SUB_FORM_RCMP:
                  _subFormsOnlyRecomp = true;
                  break;

               case ConstInterface.MG_ATTR_HAS_LINK_RECOMPUTES:
                  _hasServerLinksRecomputes = true;
                  break;

               case XMLConstants.MG_ATTR_NAME:
                  /* ignore the name */
                  break;

               default:
                  Logger.Instance.WriteExceptionToLog(
                     "There is no such tag in Recompute class. Insert case to Recompute.initElements for " + attribute);
                  break;
            }
         }
      }

      /// <summary>
      ///   execute recompute
      /// </summary>
      /// <param name = "rec">the record on which the recompute is executed</param>
      internal void execute(Record rec)
      {
         int i;
         Field fld;
         CommandsTable cmdsToServer = Task.getMGData().CmdsToServer;
         IClientCommand cmd;

         try
         {
            rec.setInRecompute(true);

            bool allowServerRecompute = _hasServerLinksRecomputes || (Task.getForm().AllowedSubformRecompute && checkRefreshSubForms());
            // SERVER
            if (RcmpMode != RcmpBy.CLIENT && allowServerRecompute)
            {
               bool inClient = _subFormsOnlyRecomp;
               Task.ExecuteClientSubformRefresh = false;

               // if the recompute is not only due to sub-forms go to server
               if (inClient)
               {
                  inClient = Task.prepareCache(true);
                  //if all sub-form are not update
                  if (inClient)
                     inClient = Task.testAndSet(true); //try to take dataviews from cache
               }
               if (!inClient)
               {
                  ((FieldsTable) Task.DataView.GetFieldsTab()).setServerRcmp(true);
                  cmd = CommandFactory.CreateRecomputeCommand(Task.getTaskTag(), OwnerFld.getId(), !Task.getForm().AllowedSubformRecompute);
                  cmdsToServer.Add(cmd);
                  RemoteCommandsProcessor.GetInstance().Execute(CommandsProcessorBase.SendingInstruction.TASKS_AND_COMMANDS);                  
               }

               if (Task.ExecuteClientSubformRefresh)
                  RefreshSubforms();
               else
               {
                  if (recPrefixSubForms())
                     recSuffixSubForms();
                  Task.CleanDoSubformPrefixSuffix();
               }
            }
               // CLIENT
            else
            {
               try
               {
                  FlowMonitorQueue.Instance.addRecompute(OwnerFld.getVarName());

                  // FORM PROPERTIES
                  if (_formProps != null)
                     _formProps.RefreshDisplay(false, false);

                  // CTRL PROPERTIES
                  if (_ctrlProps != null)
                     _ctrlProps.RefreshDisplay(false, false);

                  //re-cumpute client side fields and links
                  if (_rcmpOrder != null)
                  {
                     for (i = 0; i < _rcmpOrder.Count; i++)
                     {
                        if (_rcmpOrder[i] is Field)
                        {
                           fld = (Field)_rcmpOrder[i];
                           fldRcmp(fld, true);
                        }
                        else if (_rcmpOrder[i] is DataviewHeaderBase)
                        {
                           var curLnk = (DataviewHeaderBase)_rcmpOrder[i];
                           curLnk.getLinkedRecord(rec);

                           //if we have recomputed a link we should also start the recompute process on all of its fields
                           List<Field> linkFields =
                              ((FieldsTable)Task.DataView.GetFieldsTab()).getLinkFields(curLnk.Id);
                           rec.setInCompute(true);
                           bool saveInForceUpdate = rec.InForceUpdate;
                           rec.InForceUpdate = false;

                           for (int j = 0; j < linkFields.Count; j++)
                           {
                              fldRcmp(linkFields[j], false);
                              rec.clearFlag((linkFields[j]).getId(), Record.FLAG_UPDATED);
                              rec.clearFlag((linkFields[j]).getId(), Record.FLAG_MODIFIED);
                              rec.clearFlag((linkFields[j]).getId(), Record.FLAG_CRSR_MODIFIED);
                              rec.clearHistoryFlag((linkFields[j]).getId());
                           }
                           rec.InForceUpdate = saveInForceUpdate;

                           //start recompute process on the ret val of the link
                           Field retFld = curLnk.ReturnField;
                           if (retFld != null)
                              fldRcmp(retFld, false);

                           rec.setInCompute(false);
                           rec.setForceSaveOrg(true);
                        }
                        else if (_rcmpOrder[i] is DCValuesRecompute)
                        {
                           ((DCValuesRecompute)_rcmpOrder[i]).Recompute(Task, rec);
                        }
                     }
                  }

                  RefreshSubforms();
               }
               catch (Exception e)
               {
                  Logger.Instance.WriteExceptionToLog("in Recompute.execute(): " + e.Message);
               }
            } // END CLIENT BLOCK

         }
         finally
         {
            rec.buildLinksPosStr();
            rec.setInRecompute(false);
         }
      }

      /// <summary>
      // Subforms recompute
      /// </summary>
      private void RefreshSubforms()
      {
         if (Task.getForm().AllowedSubformRecompute)
         {
            List<Task> subformsToRefresh = GetSubformsToRefresh();
            foreach (Task subformTask in subformsToRefresh)
               Task.SubformRefresh(subformTask, true);
         }
      }

      /// <summary>
      ///   a helper function to recompute fields
      /// </summary>
      /// <param name = "fld">- the fiedl to recompute</param>
      private void fldRcmp(Field fld, bool computeField)
      {
         if (fld.IsVirtual ||
             ((Task.getMode() != Constants.TASK_MODE_QUERY ||
               ClientManager.Instance.getEnvironment().allowUpdateInQueryMode(Task.getCompIdx()))))
         {
            // prevents recursive compute for the same field
            if (!fld.isInEvalProcess())
            {
               if (computeField)
                  fld.compute(true);
               else
                  fld.setValueAndStartRecompute(fld.getValue(false), fld.isNull(), true, false, false);
               fld.updateDisplay();
            }
         }
      }

      /// <summary></summary>
      private void buildSubFormList()
      {
         int i;
         Task subForm;
         TasksTable subTasksTab;

         if (_subForms == null)
         {
            _subForms = new List<Task>();
            subTasksTab = Task.getSubTasks();
            if (subTasksTab != null)
            {
               for (i = 0; i < subTasksTab.getSize(); i++)
               {
                  subForm = subTasksTab.getTask(i);
                  // QCR #299153. Add only subforms (not subtasks).
                  if (subForm.getForm().getSubFormCtrl() != null && subForm.refreshesOn(OwnerFld.getId()))
                     _subForms.Add(subForm);
               }
            }
         }
      }

      /// <summary>
      /// Removes the specific task from subforms list
      /// </summary>
      /// <param name="subformTask"></param>
      internal void RemoveSubform (Task subformTask)
      {
         _subForms.Remove(subformTask);
      }

      /// <summary>
      /// Inserts the specific subform task into the subform list
      /// </summary>
      /// <param name="subformTask"></param>
      internal void AddSubform(Task subformTask)
      {
         if (_subForms == null)
            _subForms = new List<Task>();
         if (!_subForms.Contains(subformTask))
            _subForms.Add(subformTask);
      }

      /// <summary>
      ///   execute record suffix for the affected subforms if Refresh is needed
      ///   returns true if finished successfully and it is needed to refresh
      /// </summary>
      private bool recSuffixSubForms()
      {
         int i;
         Task subForm;
         bool successful = true;
         MgControl subformCtrl;

         // build the subforms list and save it for future use
         buildSubFormList();

         // execute "record suffix" for the subforms
         for (i = 0; successful && i < _subForms.Count; i++)
         {
            subForm = _subForms[i];
            subformCtrl = (MgControl)subForm.getForm().getSubFormCtrl();
            if (subForm.isStarted() && !subformCtrl.RefreshOnVisible &&
                subformCtrl.checkProp(PropInterface.PROP_TYPE_AUTO_REFRESH, true) && !subForm.InSelect && subForm.DoSubformPrefixSuffix)
            {
               ClientManager.Instance.EventsManager.handleInternalEvent(subForm, InternalInterface.MG_ACT_REC_SUFFIX);
               successful = !ClientManager.Instance.EventsManager.GetStopExecutionFlag();
            }
         }

         ((DataView) Task.DataView).setPrevCurrRec();
         return (successful);
      }

      // check if do refresh for any subform that the field affects
      private bool checkRefreshSubForms()
      {
         bool refresh;

         refresh = (_subFormsOnlyRecomp
                       ? false
                       : true);

         List<Task> subformsToRefresh = GetSubformsToRefresh();
         if (subformsToRefresh.Count > 0)
            refresh = true;

         return (refresh);
      }

      /// <summary>
      /// Gets a list of those subforms that must be refreshed.
      /// </summary>
      /// <returns>list of subforms to refresh</returns>
      private List<Task> GetSubformsToRefresh()
      {
         List<Task> subTasks = new List<Task>();
         MgControl subformCtrl;

         // build the subforms list and save it for future use
         buildSubFormList();

         foreach (Task subTask in _subForms)
         {
            subformCtrl = (MgControl)subTask.getForm().getSubFormCtrl();
            if (subTask.isStarted() && subformCtrl.checkProp(PropInterface.PROP_TYPE_AUTO_REFRESH, true) && !subTask.InSelect && !subTask.InEndTask)
            {
               // compute the visibility property for the subform control before use it in isVisible() method
               subformCtrl.checkProp(PropInterface.PROP_TYPE_VISIBLE, true);
               if (!subformCtrl.isVisible() && !subformCtrl.checkProp(PropInterface.PROP_TYPE_REFRESH_WHEN_HIDDEN, false))
                  subformCtrl.RefreshOnVisible = true;
               else
                  subTasks.Add(subTask);
            }
         }

         return subTasks;
      }


      /// <summary>
      ///   execute record prefix for the affected subforms if Refresh is needed
      ///   returns true if finished successfully and it is needed to refresh
      /// </summary>
      private bool recPrefixSubForms()
      {
         int i;
         Task subForm;
         bool successful = true;
         MgControl subformCtrl;

         // build the subforms list and save it for future use
         buildSubFormList();

         // execute "record prefix" for the subforms
         for (i = 0; successful && i < _subForms.Count; i++)
         {
            subForm = _subForms[i];
            subformCtrl = (MgControl)subForm.getForm().getSubFormCtrl();
            if (subForm.isStarted() && !subformCtrl.RefreshOnVisible &&
                subformCtrl.checkProp(PropInterface.PROP_TYPE_AUTO_REFRESH, true) && !subForm.InSelect && subForm.DoSubformPrefixSuffix)
            {
               ClientManager.Instance.EventsManager.handleInternalEvent(subForm, InternalInterface.MG_ACT_REC_PREFIX, true);
               successful = !ClientManager.Instance.EventsManager.GetStopExecutionFlag();
            }
         }

         return (successful);
      }

      /// <summary>
      ///   TODO: (ILAN) ADD REMARK HERE
      /// </summary>
      internal bool notifyServerOnChange()
      {
         return (RcmpMode == RcmpBy.SERVER_ON_CHANGE);
      }

      /// <returns> true if the client doesn't know to recompute this field</returns>
      internal bool isServerRcmp()
      {
         return (RcmpMode != RcmpBy.CLIENT);
      }

      #region Nested type: RcmpBy

      internal enum RcmpBy
      {
         CLIENT = 'C',
         SERVER_ON_CHANGE = 'O'
      }

      #endregion
   }

   
}
