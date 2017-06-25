using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Xml;
using com.magicsoftware.unipaas.gui;
using com.magicsoftware.unipaas.gui.low;
using com.magicsoftware.unipaas.util;
using com.magicsoftware.util;

#if PocketPC
using com.magicsoftware.richclient.mobile.util;
using com.magicsoftware.richclient;
#endif

namespace com.magicsoftware.unipaas.management.gui
{
   public class FormUserState
   {
      /// <summary>
      /// This class holds the mdi forms details.
      /// </summary>
      class MDIFormDetails
      {
         internal Rectangle rect;
         internal int windowState;
      }

      //hold the MDI form details which will be applied to the subsequent MDI forms.
      MDIFormDetails mdiFormDetails = null;

      private const String ROOT_STR = "ApplicationPersistentData";

      private const String STR_FORM = "Form";
      private const String STR_FRAMESETS = "Framesets";
      private const String STR_FRAMESET = "Frameset";
      private const String STR_FRAME = "Frame";
      private const String STR_STARTUP_STYLE = "StartupStyle";
      private const String STR_LEFT = "Left";
      private const String STR_TOP = "Top";
      private const String STR_WIDTH = "Width";
      private const String STR_HEIGHT = "Height";
      private const String STR_LINKED_PARENT_IDX = "LinkedParentIdx";
      private const String STR_SPLITTERSTYLE = "SplitterStyle";
      private const String STR_TABLECOLS = "TableColumns";
      private const String STR_COLUMN = "Column";
      private const String STR_LAYER = "Layer";
      private const String STR_CONTROL = "Control";
      private const String STR_WIDTH_FOR_FILL_TABLE_PLACEMENT = "WidthForFillTablePlacement";

      private const String TAG_ID = "Id";
      private const String TAG_Version = "Version";
      private const String TAG_VALUE = "Value";
      private static FormUserState _instance; //singleton

      private readonly String _dirPath;
      private String _fileName;
      private XmlDocument _xmlDoc;

      /// <summary>
      ///   CTOR
      /// </summary>
      private FormUserState()
      {
         IsDisabled = false;

#if PocketPC
         _dirPath = OSEnvironment.getAssemblyFolder();
#else
         _dirPath = OSEnvironment.get("APPDATA")
                    + Path.DirectorySeparatorChar
                    + (UtilStrByteMode.isLocaleDefLangJPN()
                          ? "MSJ"
                          : "MSE")
                    + Path.DirectorySeparatorChar
                    + Manager.Environment.GetGUID()
                    + Path.DirectorySeparatorChar;
#endif
      }

      public bool IsDisabled { get; set; }

      public static FormUserState GetInstance()
      {
         if (_instance == null)
         {
            lock (typeof (FormUserState))
            {
               if (_instance == null)
                  _instance = new FormUserState();
            }
         }
         return _instance;
      }

      /// <summary>
      /// 
      /// </summary>
      public static void DeleteInstance()
      {
         Debug.Assert(_instance != null);
         _instance = null;
      }

      /// <summary>
      ///   read the previously saved form state in local file
      /// </summary>
      public void Read(String filename)
      {
         Debug.Assert(_xmlDoc == null); // FormUserState.Read must be called only once (on process' initialization).

         _fileName = filename + ".xml";

         try
         {
            if (File.Exists(_dirPath + _fileName))
            {
               _xmlDoc = new XmlDocument();
               _xmlDoc.Load(_dirPath + _fileName);
            }
         }
         catch (Exception exception)
         {
            Events.WriteExceptionToLog(exception);
            _xmlDoc = null;
         }

         //If the file is not already available, or the loading of the file failed due to some reason, create a new blank Document.
         if (_xmlDoc == null)
         {
            try
            {
               _xmlDoc = XmlServices.CreateDocument(ROOT_STR);
            }
            catch (Exception exception)
            {
               Events.WriteExceptionToLog(exception);
            }
         }
      }

      /// <summary>
      ///   save the form state to local file
      /// </summary>
      public void Write()
      {
         Debug.Assert(_xmlDoc != null);

         try
         {
            // create directory if not exists
            if (!Directory.Exists(_dirPath))
               Directory.CreateDirectory(_dirPath);

            // save document to file
            Debug.Assert(_xmlDoc.DocumentElement != null, "_xmlDoc.DocumentElement != null");
            if (!_xmlDoc.DocumentElement.IsEmpty)
               _xmlDoc.Save(_dirPath + _fileName);
         }
         catch (Exception exception)
         {
            Events.WriteExceptionToLog(exception);
         }
         finally
         {
            _xmlDoc = null;
         }
      }

      /// <summary>
      ///   saves form state of the form in this class
      /// </summary>
      /// <param name = "mgform"></param>
      internal void Save(MgFormBase mgform)
      {
         try
         {
            // When closing the MDI form, save its details --- irrespective of whether PersistentState=Y/N.
            if (mgform.getTask().isMainProg())
            {
               if (mdiFormDetails == null)
                  mdiFormDetails = new MDIFormDetails();

               mdiFormDetails.windowState = Commands.getLastWindowState(mgform);
               mdiFormDetails.rect = Commands.getFormBounds(mgform);
            }

            XmlElement formNode = GetNode(mgform);
            // if formNode is null, return
            if (formNode == null)
               return;

            // remove the previously saved form properties
            RemoveFormProp(formNode);

            // Add user state info of the form
            SaveFormProp(formNode, mgform);
         }
         catch (Exception exception)
         {
            Events.WriteExceptionToLog(exception);
         }
      }

      /// <summary>
      ///   remove the form properties
      /// </summary>
      /// <param name = "formNode"></param>
      private void RemoveFormProp(XmlElement formNode)
      {
         Debug.Assert(_xmlDoc != null);

         // remove all childs in formNode except CONTROL node.
         for (int index = formNode.ChildNodes.Count - 1;
              index >= 0;
              index--)
         {
            var childNode = (XmlElement) formNode.ChildNodes[index];
            if (childNode.Name != STR_CONTROL)
               XmlServices.removeElement(_xmlDoc, childNode);
         }
      }

      /// <summary>
      ///   saves form properties in form state of the form
      /// </summary>
      /// <param name = "formNode"></param>
      /// <param name = "mgform"></param>
      private void SaveFormProp(XmlElement formNode, MgFormBase mgform)
      {
         Debug.Assert(_xmlDoc != null);

         XmlServices.setAttribute(formNode, TAG_Version, mgform.getProp(PropInterface.PROP_TYPE_PERSISTENT_FORM_STATE_VERSION).GetComputedValue());

         // donot save form coordinates for nested forms
         if (!mgform.isSubForm() && !mgform.IsChildWindow)
         {
            int windowState = Commands.getLastWindowState(mgform);

            // get the form bounds
            Rectangle rect = Commands.getFormBounds(mgform);

            // Add LEFT node
            XmlServices.AddNode(_xmlDoc, formNode, STR_LEFT, rect.Left.ToString());

            // Add TOP node
            XmlServices.AddNode(_xmlDoc, formNode, STR_TOP, rect.Top.ToString());

            // Add WIDTH node
            XmlServices.AddNode(_xmlDoc, formNode, STR_WIDTH, rect.Width.ToString());

            // Add HEIGHT node
            XmlServices.AddNode(_xmlDoc, formNode, STR_HEIGHT, rect.Height.ToString());

            XmlServices.AddNode(_xmlDoc, formNode, STR_STARTUP_STYLE, windowState.ToString());
         }

         // Add framesets
         if (mgform.IsFrameSet)
            SaveFramesetsProp(formNode, mgform);

         // Add TableColumns
         SaveTableProp(formNode, mgform);
      }

      /// <summary>
      ///   saves frames of a frameform
      /// </summary>
      /// <param name = "formNode"></param>
      /// <param name = "mgform"></param>
      private void SaveFramesetsProp(XmlElement formNode, MgFormBase mgform)
      {
         Debug.Assert(_xmlDoc != null);

         // Add framesets Node
         XmlElement framesetsNode = XmlServices.AddElement(_xmlDoc, formNode, STR_FRAMESETS);

         List<MgControlBase> frameSetCtrls = mgform.getCtrls(MgControlType.CTRL_TYPE_FRAME_SET);

         foreach (MgControlBase frameset in frameSetCtrls)
         {
            // add a node for a frameset
            XmlElement framesetNode = XmlServices.AddElement(_xmlDoc, framesetsNode, STR_FRAMESET);

            // add splitterStyle as attribute to frameset
            int splitterStyle = frameset.getProp(PropInterface.PROP_TYPE_FRAMESET_STYLE).getValueInt();
            XmlServices.setAttribute(framesetNode, STR_SPLITTERSTYLE, splitterStyle.ToString());

            // add linkedParent as attribute to frameset
            int linkedParentIdx = Commands.getLinkedParentIdx(frameset);
            XmlServices.setAttribute(framesetNode, STR_LINKED_PARENT_IDX, linkedParentIdx.ToString());

            // get bounds of the frameset
            var arraylist = (List<Rectangle>) Commands.getFramesBounds(frameset);

            // for every split nodes, add a child node in frameset node 
            foreach (Rectangle rect in arraylist)
            {
               // add the child node
               XmlElement frameNode = XmlServices.AddElement(_xmlDoc, framesetNode, STR_FRAME);

               // add the width or height as attribute to this child split node
               if (splitterStyle == MgSplitContainer.SPLITTER_STYLE_HORIZONTAL)
                  XmlServices.setAttribute(frameNode, STR_WIDTH, rect.Width.ToString());
               else
                  XmlServices.setAttribute(frameNode, STR_HEIGHT, rect.Height.ToString());
            }
         }
      }

      /// <summary>
      ///   saves table properties of a line mode form
      /// </summary>
      /// <param name = "formNode"></param>
      /// <param name = "mgForm"></param>
      private void SaveTableProp(XmlElement formNode, MgFormBase mgForm)
      {
         Debug.Assert(_xmlDoc != null);

         if (mgForm.isLineMode())
         {
            // Add table column node
            XmlElement tableColumnNode = XmlServices.AddElement(_xmlDoc, formNode, STR_TABLECOLS);

            MgControlBase tableCtrl = mgForm.getTableCtrl();

            List<int[]> columnsState = Commands.getColumnsState(tableCtrl);

            for (int idx = 0;
                 idx < columnsState.Count;
                 idx++)
            {
               // Add column node
               XmlElement columnNode = XmlServices.AddElement(_xmlDoc, tableColumnNode, STR_COLUMN);

               // Add layer of column
               int propLayer = columnsState[idx][0];
               XmlServices.setAttribute(columnNode, STR_LAYER, propLayer.ToString());

               // Add width of column
               int propWidth = columnsState[idx][1];
               XmlServices.setAttribute(columnNode, STR_WIDTH, propWidth.ToString());

               // Add widthForFillTablePlacement of column
               int propWidthForFillTablePlacement = columnsState[idx][2];
               XmlServices.setAttribute(columnNode, STR_WIDTH_FOR_FILL_TABLE_PLACEMENT, propWidthForFillTablePlacement.ToString());
            }
         }
      }

      /// <summary>
      ///   read form-state of the form, and applies them
      /// </summary>
      /// <param name = "mgform"></param>
      internal void Apply(MgFormBase mgform)
      {
         try
         {
            // When opening the MDI form, use the details of the previous MDI form, if available.
            //This details have preference over the current form's persistent state.
            if (mgform.getTask().isMainProg() && mdiFormDetails != null)
            {
               ApplyFormBounds(mgform, mdiFormDetails.rect.Left, mdiFormDetails.rect.Top, mdiFormDetails.rect.Width, mdiFormDetails.rect.Height, mdiFormDetails.windowState);
               return;
            }

            XmlElement formNode = GetNode(mgform);
            if (formNode == null)
               return;

            Int32 oldPersistentFormStateVersion = 0;
            Int32.TryParse(formNode.GetAttribute(TAG_Version), out oldPersistentFormStateVersion);

            if (oldPersistentFormStateVersion != mgform.getProp(PropInterface.PROP_TYPE_PERSISTENT_FORM_STATE_VERSION).GetComputedValueInteger())
               return;

            // QCR # 734135, check if form has no data to apply. All child elements are control tags.
            List<XmlElement> childNodes = XmlServices.getMatchingChildrens(formNode, STR_CONTROL, null, null);
            if (formNode.ChildNodes.Count == childNodes.Count)
               return;

            XmlElement framesetsNode = null;
            XmlElement tableNode = null;
            int left = 0, top = 0, width = 0, height = 0;
            int startupStyle = 0;

            // traverse through all child node of form node and get the saved propVal and apply
            foreach (XmlElement currEle in formNode.ChildNodes)
            {
               if (currEle.Name == STR_STARTUP_STYLE)
                  startupStyle = int.Parse(currEle.GetAttribute(TAG_VALUE));
               else if (currEle.Name == STR_LEFT)
                  left = int.Parse(currEle.GetAttribute(TAG_VALUE)); // read val
               else if (currEle.Name == STR_TOP)
                  top = int.Parse(currEle.GetAttribute(TAG_VALUE)); // read val
               else if (currEle.Name == STR_WIDTH)
                  width = int.Parse(currEle.GetAttribute(TAG_VALUE)); // read val
               else if (currEle.Name == STR_HEIGHT)
                  height = int.Parse(currEle.GetAttribute(TAG_VALUE)); // read val
               else if (currEle.Name == STR_FRAMESETS)
                  framesetsNode = currEle;
               else if (currEle.Name == STR_TABLECOLS)
                  tableNode = currEle;
            }

            // donot apply form coordinates for nested forms
            if (!mgform.isSubForm() && !mgform.IsChildWindow)
            {
               //If the left-top corner is beyond the screen boundaries, ignore it.
               if (!mgform.IsMDIChild)
               {
                  MgRectangle rect = new MgRectangle();
                  Commands.getDesktopBounds(rect, null);

                  if (left >= rect.width)
                     left = GuiConstants.DEFAULT_VALUE_INT;
                  if (top >= rect.height)
                     top = GuiConstants.DEFAULT_VALUE_INT;
               }

               ApplyFormBounds(mgform, left, top, width, height, startupStyle);
            }

            // apply framesets
            if (framesetsNode != null)
               ApplyFramesetsProp(framesetsNode, mgform);

            // apply table
            if (tableNode != null)
               ApplyTableProp(tableNode, mgform);

            // #924756 - do not apply top left width height if the property has an expression attached
            if (startupStyle != Styles.WINDOW_STATE_MAXIMIZE && !mgform.IsChildWindow)
            {
               mgform.RefreshPropertyByExpression(PropInterface.PROP_TYPE_LEFT);
               mgform.RefreshPropertyByExpression(PropInterface.PROP_TYPE_TOP);
               mgform.RefreshPropertyByExpression(PropInterface.PROP_TYPE_WIDTH);
               mgform.RefreshPropertyByExpression(PropInterface.PROP_TYPE_HEIGHT);
            }
         }
         catch (Exception exception)
         {
            Events.WriteExceptionToLog(exception);
         }
      }

      /// <summary>
      /// Applies form bounds to the form.
      /// </summary>
      /// <param name="mgform"></param>
      /// <param name="left"></param>
      /// <param name="top"></param>
      /// <param name="width"></param>
      /// <param name="height"></param>
      /// <param name="windowState"></param>
      private void ApplyFormBounds(MgFormBase mgform, int left, int top, int width, int height, int windowState)
      {
         //Before setting the bounds, change the startup position to customized.
         Commands.addAsync(CommandType.PROP_SET_STARTUP_POSITION, mgform, 0, WindowPosition.Customized);

         // add top left width height as a message to gui thread
         Commands.addAsync(CommandType.PROP_SET_BOUNDS, mgform, 0, left, top, width, height, false, false);

         if (windowState != 0)
            Commands.addAsync(CommandType.SET_WINDOW_STATE, mgform, 0, windowState);

         Commands.addAsync(CommandType.EXECUTE_LAYOUT, (Object)mgform, true);
      }

      /// <summary>
      ///   applies frames on a frameform
      /// </summary>
      /// <param name = "framesetsNode"></param>
      /// <param name = "mgform"></param>
      private void ApplyFramesetsProp(XmlElement framesetsNode, MgFormBase mgform)
      {
         List<MgControlBase> framesetCtrls = mgform.getCtrls(MgControlType.CTRL_TYPE_FRAME_SET);

         if (!isFramesetsDesignChanged(framesetsNode, framesetCtrls))
         {
            for (int index = 0;
                 index < framesetsNode.ChildNodes.Count;
                 index++)
            {
               var framesetNode = (XmlElement) framesetsNode.ChildNodes[index];
               var arrayList = new List<int>();

               // get splitter style
               int splitterstyle = int.Parse(framesetNode.GetAttribute(STR_SPLITTERSTYLE));

               // get widths or heights of all the splits in this frameset
               foreach (XmlElement frameNode in framesetNode.ChildNodes)
               {
                  int val = int.Parse(splitterstyle == MgSplitContainer.SPLITTER_STYLE_HORIZONTAL
                                         ? frameNode.GetAttribute(STR_WIDTH)
                                         : frameNode.GetAttribute(STR_HEIGHT));
                  arrayList.Add(val);
               }

               // apply the widths or heights of all the splits in this frameset
               Commands.addAsync(splitterstyle == MgSplitContainer.SPLITTER_STYLE_HORIZONTAL
                                    ? CommandType.SET_FRAMES_WIDTH
                                    : CommandType.SET_FRAMES_HEIGHT, framesetCtrls[index], 0, arrayList);
               GuiCommandQueue.getInstance().add(CommandType.EXECUTE_LAYOUT, framesetCtrls[index], true);
            }
         }
      }

      /// <summary>
      ///   returns if the framsets design has changed from last saved
      /// </summary>
      /// <param name = "framesetsNode"></param>
      /// <param name = "framesetCtrls"></param>
      /// <returns></returns>
      private bool isFramesetsDesignChanged(XmlElement framesetsNode, List<MgControlBase> framesetCtrls)
      {
         bool isDesignChanged = false;
         // check it the frame design has not changed.
         // compare- no of framesets and for each framset - linkedParentIdx & splitterstyle.
         if (framesetsNode.ChildNodes.Count != framesetCtrls.Count)
            isDesignChanged = true;
         else
         {
            for (int index = 0;
                 index < framesetsNode.ChildNodes.Count;
                 index++)
            {
               var framesetNode = (XmlElement) framesetsNode.ChildNodes[index];

               // NOTE: Currently a frameset has only 2 splits. If there is more, the number of splits
               // should also be matched.

               // get the saved linked parent
               int oldLinkedParentIdx = int.Parse(framesetNode.GetAttribute(STR_LINKED_PARENT_IDX));

               // get the form's actual linked parent
               int newLinkedParentIdx = Commands.getLinkedParentIdx(framesetCtrls[index]);

               // check mismatch
               if (oldLinkedParentIdx != newLinkedParentIdx)
               {
                  isDesignChanged = true;
                  break;
               }

               // get the saved style
               int oldSplitterstyle = int.Parse(framesetNode.GetAttribute(STR_SPLITTERSTYLE));

               // get the form's actual style
               int newSplitterstyle = framesetCtrls[index].getProp(PropInterface.PROP_TYPE_FRAMESET_STYLE).getValueInt();

               // check mismatch
               if (oldSplitterstyle != newSplitterstyle)
               {
                  isDesignChanged = true;
                  break;
               }
            }
         }

         return isDesignChanged;
      }

      /// <summary>
      ///   applies column properties on a line mode form
      /// </summary>
      /// <param name = "tableNode"></param>
      /// <param name = "mgForm"></param>
      private void ApplyTableProp(XmlElement tableNode, MgFormBase mgForm)
      {
         // read val for all columns and set them on form
         if (mgForm.HasTable() && !IsTableDesignChanged(mgForm, tableNode))
         {
            MgControlBase tableCtrl = mgForm.getTableCtrl();
            int columnCount = tableNode.ChildNodes.Count;
            var columnData = new List<int[]>(); // {layer, width, widthForFillTablePlacement}

            for (int index = 0;
                 index < columnCount;
                 index++)
            {
               var columnElement = (XmlElement) tableNode.ChildNodes[index];

               // get layer and width
               int colLayer = int.Parse(columnElement.GetAttribute(STR_LAYER));
               int colWidth = int.Parse(columnElement.GetAttribute(STR_WIDTH));
               int colWidthForFillTablePlacement;
               if (!int.TryParse(columnElement.GetAttribute(STR_WIDTH_FOR_FILL_TABLE_PLACEMENT), out colWidthForFillTablePlacement))
                  colWidthForFillTablePlacement = colWidth;

               columnData.Add(new[] { colLayer, colWidth, colWidthForFillTablePlacement });
            }

            Commands.addAsync(CommandType.REORDER_COLUMNS, tableCtrl, 0, columnData);
         }
      }

      /// <summary>
      ///   returns true if the table design has changed from last saved
      /// </summary>
      /// <param name = "mgForm"></param>
      /// <param name = "tableNode"></param>
      /// <returns></returns>
      private bool IsTableDesignChanged(MgFormBase mgForm, XmlElement tableNode)
      {
         bool isDesignChanged = false;

         int columnsCount = mgForm.getColumnsCount();

         // match the column count
         if (columnsCount != tableNode.ChildNodes.Count)
            isDesignChanged = true;

         return isDesignChanged;
      }

      /// <summary>
      /// </summary>
      /// <param name = "mgform"></param>
      internal void ApplyDefault(MgFormBase mgform)
      {
         // if is a main window
         if (!mgform.isSubForm() && !mgform.IsChildWindow)
         {
            mgform.getProp(PropInterface.PROP_TYPE_WIDTH).RefreshDisplay(true);
            mgform.getProp(PropInterface.PROP_TYPE_HEIGHT).RefreshDisplay(true);

            mgform.startupPosition(); // positions x,y

            mgform.getProp(PropInterface.PROP_TYPE_STARTUP_MODE).RefreshDisplay(true);
         }

         // restore width/height of frames in framesets
         if (mgform.IsFrameSet)
         {
            List<MgControlBase> frameSetCtrls = mgform.getCtrls(MgControlType.CTRL_TYPE_FRAME_SET);

            foreach (MgControlBase frameset in frameSetCtrls)
            {
               var widthList = new List<int>();
               var heightList = new List<int>();

               foreach (MgControlBase childCtrl in frameset.getLinkedControls())
               {
                  widthList.Add(childCtrl.getProp(PropInterface.PROP_TYPE_WIDTH).getValueInt());
                  heightList.Add(childCtrl.getProp(PropInterface.PROP_TYPE_HEIGHT).getValueInt());
               }

               Commands.addAsync(CommandType.SET_FRAMES_WIDTH, frameset, 0, widthList);
               Commands.addAsync(CommandType.SET_FRAMES_HEIGHT, frameset, 0, heightList);

               // execute layout of the frameset
               Commands.addAsync(CommandType.EXECUTE_LAYOUT, frameset, true);
            }
         }

         // restore order of columns in table
         if (mgform.isLineMode())
         {
            var columnData = new List<int>(); // {layer}
            List<MgControlBase> columnCtrls = mgform.getColumnControls();

            for (int idx = 0;
                 idx < columnCtrls.Count;
                 idx++)
            {
               MgControlBase columnCtrl = columnCtrls[idx];
               columnCtrl.getProp(PropInterface.PROP_TYPE_WIDTH).RefreshDisplay(true);
               columnData.Add(idx);
            }

            Commands.addAsync(CommandType.RESTORE_COLUMNS, mgform.getTableCtrl(), 0, columnData);
         }

         // apply default of all subforms (if any).
         List<MgControlBase> subformCtrls = mgform.getCtrls(MgControlType.CTRL_TYPE_SUBFORM);

         if (subformCtrls.Count > 0)
         {
            foreach (MgControlBase subformControl in subformCtrls)
            {
               MgFormBase subformForm = subformControl.GetSubformMgForm();
               if (subformForm != null)
                  ApplyDefault(subformForm);
            }
         }
      }

      /// <summary>
      ///   Delete the form state for a particular 'userStateId'
      /// </summary>
      /// <param name = "userStateId"></param>
      internal void Delete(String userStateId)
      {
         Debug.Assert(_xmlDoc != null);

         List<XmlElement> nodes = XmlServices.getMatchingChildrens(_xmlDoc.DocumentElement, STR_FORM, TAG_ID,
                                                                   userStateId);

         foreach (XmlElement node in nodes)
            XmlServices.removeElement(_xmlDoc, node);
      }

      /// <summary>
      ///   Delete user states of all forms.
      /// </summary>
      internal void DeleteAll()
      {
         Debug.Assert(_xmlDoc != null);

         XmlServices.removeElements(_xmlDoc, STR_FORM);
      }

      /// <summary>
      ///   Searches for a node for 'mgForm'
      /// </summary>
      /// <param name = "mgForm"></param>
      /// <returns></returns>
      private XmlElement GetNode(MgFormBase mgForm)
      {
         return (!mgForm.isSubForm() // is a main window
                    ? GetTopMostNode(mgForm)
                    : GetSubformNode(mgForm));
      }

      /// <summary>
      ///   gets the topmost parent node of 'mgForm'.
      /// </summary>
      /// <param name = "mgForm"></param>
      /// <returns></returns>
      private XmlElement GetTopMostNode(MgFormBase mgForm)
      {
         Debug.Assert(_xmlDoc != null);

         MgFormBase topMostform = mgForm.getTopMostForm();
         String topMostFormUserStateId = topMostform.UserStateId;
         if (String.IsNullOrEmpty(topMostFormUserStateId))
            return null;

         // get the topmost node
         List<XmlElement> nodes = XmlServices.getMatchingChildrens(_xmlDoc.DocumentElement, STR_FORM, TAG_ID,
                                                                   topMostFormUserStateId);
         XmlElement topMostFormNode = (nodes.Count == 0
                                          ? CreateEmptyFormNode(_xmlDoc.DocumentElement, topMostform, false)
                                          : nodes[0]);
         return topMostFormNode;
      }

      /// <summary>
      ///   gets the subform node for 'mgForm'
      /// </summary>
      /// <param name = "mgForm"></param>
      /// <returns></returns>
      private XmlElement GetSubformNode(MgFormBase mgForm)
      {
         // get the topMost node
         XmlElement topMostFormNode = GetTopMostNode(mgForm);
         if (topMostFormNode == null)
            return null;

         return GetSubformNode(topMostFormNode, mgForm.getTopMostForm(), mgForm);
      }

      /// <summary>
      ///   gets the subform node for 'mgForm'
      /// </summary>
      /// <param name = "topMostNode"></param>
      /// <param name = "topMostForm"></param>
      /// <param name = "currForm"></param>
      /// <returns></returns>
      private XmlElement GetSubformNode(XmlElement topMostNode, MgFormBase topMostForm, MgFormBase currForm)
      {
         XmlElement formNode = null;

         // get path from currForm to 'mgform'
         List<MgFormBase> formPathList = GetMatchingPath(currForm, topMostForm);
         if (formPathList.Count > 0)
            formNode = topMostNode;

         // set path from topmost parent to current form
         formPathList.Reverse();

         // traverse and get the xml node.
         foreach (MgFormBase mgForm in formPathList)
         {
            // reset the variable

            string userStateId = mgForm.UserStateId;
            // one of the parent does not support form user state, so we will not save any of the childs.
            if (String.IsNullOrEmpty(userStateId))
               return null;

            // get the subform Node
            List<XmlElement> controlNodes = XmlServices.getMatchingChildrens(formNode, STR_CONTROL, TAG_ID,
                                                                 mgForm.getSubFormCtrl().Id.ToString());
            if (controlNodes.Count > 0)
            {
               XmlElement controlNode = controlNodes[0];

               // search for matching childs in containerNode
               List<XmlElement> formNodes = XmlServices.getMatchingChildrens(controlNode, STR_FORM, TAG_ID, userStateId);

               formNode = (formNodes.Count > 0
                             ? formNodes[0]
                             : CreateEmptyFormNode(controlNode, mgForm, false));
            }
            else
            {
               // No control node. In formNode, create a control node and add a mgform-node in it
               formNode = CreateEmptyFormNode(formNode, mgForm, true);
            }
         }

         return formNode;
      }

      /// <summary>
      ///   get the path from 'mgFormFrm' to 'mgFormTo'
      /// </summary>
      /// <param name = "mgFormFrm"></param>
      /// <param name = "mgFormTo"></param>
      /// <returns></returns>
      private List<MgFormBase> GetMatchingPath(MgFormBase mgFormFrm, MgFormBase mgFormTo)
      {
         var formPathList = new List<MgFormBase>();
         MgFormBase mgForm = mgFormFrm;

         // get the path from current Form to TopMost Form
         while (mgForm != null && mgForm != mgFormTo)
         {
            formPathList.Add(mgForm);

            if (mgForm.isSubForm())
               mgForm = mgForm.getSubFormCtrl().getForm();
            else
               throw new Exception("One of the parents is not a subform");
         }

         // mgFormTo not found
         if (mgForm == null)
            formPathList.Clear();

         return formPathList;
      }

      /// <summary>
      ///   creates a template node for the mgform
      /// </summary>
      /// <param name = "containerNode"></param>
      /// <param name = "mgForm"></param>
      /// <param name = "addControlTag"></param>
      private XmlElement CreateEmptyFormNode(XmlElement containerNode, MgFormBase mgForm, bool addControlTag)
      {
         Debug.Assert(_xmlDoc != null);

         if (addControlTag && mgForm.getSubFormCtrl() != null)
         {
            // Add Subform Node
            XmlElement subformNode = XmlServices.AddElement(_xmlDoc, containerNode, STR_CONTROL);
            XmlServices.setAttribute(subformNode, TAG_ID, mgForm.getSubFormCtrl().Id.ToString());

            containerNode = subformNode;
         }

         // Add node for 'mgForm'
         XmlElement formNode = XmlServices.AddElement(_xmlDoc, containerNode, STR_FORM);
         XmlServices.setAttribute(formNode, TAG_ID, mgForm.UserStateId);

         return formNode;
      }

      /// <summary>
      /// saves form bounds with form node with attr 'userStateId'
      /// </summary>
      /// <param name="userStateId">userStateId of the form</param>
      /// <param name="formBounds">the form bounds</param>
      public void SaveFormBounds(string userStateId, Rectangle formBounds)
      {
         Debug.Assert(_xmlDoc != null); // FormUserState.Read must be called on process' initialization.

         try
         {
            try
            {
               Delete(userStateId);
            }
            catch (Exception exception)
            {
               //userStateId + not saved yet, nothing to delete
               Events.WriteExceptionToLog(exception);
            }

            XmlElement formNode = XmlServices.AddElement(_xmlDoc, _xmlDoc.DocumentElement, STR_FORM);
            XmlServices.setAttribute(formNode, TAG_ID, userStateId);

            // Add LEFT node
            XmlServices.AddNode(_xmlDoc, formNode, STR_LEFT, formBounds.Left.ToString());

            // Add TOP node
            XmlServices.AddNode(_xmlDoc, formNode, STR_TOP, formBounds.Top.ToString());

            // Add WIDTH node
            XmlServices.AddNode(_xmlDoc, formNode, STR_WIDTH, formBounds.Width.ToString());

            // Add HEIGHT node
            XmlServices.AddNode(_xmlDoc, formNode, STR_HEIGHT, formBounds.Height.ToString());
         }
         catch (Exception exception)
         {
            Events.WriteExceptionToLog(exception);
         }
      }

      /// <summary>
      /// get the saved form bounds of form with 'userStateId'
      /// </summary>
      /// <param name="userStateId">userStateId of the form</param>
      /// <returns>the form bounds</returns>
      public MgRectangle GetFormBounds(string userStateId)
      {
         Debug.Assert(_xmlDoc != null); // FormUserState.Read must be called on process' initialization.

         var rect = new MgRectangle();

         try
         {
            // get the form node
            List<XmlElement> nodes = XmlServices.getMatchingChildrens(_xmlDoc.DocumentElement, STR_FORM, TAG_ID,
                                                                      userStateId);
            if (nodes.Count == 0)
               return rect;

            XmlElement formNode = nodes[0];

            // traverse through all child node of form node and get the saved propVal and apply
            foreach (XmlElement currEle in formNode.ChildNodes)
            {
               if (currEle.Name == STR_LEFT)
                  rect.x = int.Parse(currEle.GetAttribute(TAG_VALUE)); // read val
               else if (currEle.Name == STR_TOP)
                  rect.y = int.Parse(currEle.GetAttribute(TAG_VALUE)); // read val
               else if (currEle.Name == STR_WIDTH)
                  rect.width = int.Parse(currEle.GetAttribute(TAG_VALUE)); // read val
               else if (currEle.Name == STR_HEIGHT)
                  rect.height = int.Parse(currEle.GetAttribute(TAG_VALUE)); // read val
            }
         }
         catch (Exception exception)
         {
            Events.WriteExceptionToLog(exception);
         }

         return rect;
      }
   }
}
