using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using com.magicsoftware.support;
using com.magicsoftware.unipaas.dotnet;
using com.magicsoftware.unipaas.gui;
using com.magicsoftware.unipaas.gui.low;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.unipaas.management.tasks;
using com.magicsoftware.unipaas.util;
using com.magicsoftware.util;
using com.magicsoftware.util.Xml;
using util.com.magicsoftware.util;
using Gui.com.magicsoftware.unipaas.management.gui;
using System.Drawing;
using com.magicsoftware.win32;

#if PocketPC
using com.magicsoftware.richclient.mobile.util;
#endif

namespace com.magicsoftware.unipaas.management.gui
{
   /// <summary>
   ///   data for <control> ...</control> tag
   /// </summary>
   public abstract class MgControlBase : GuiMgControl, PropParentInterface
   {
      // fixed defect #:81498, while RT is set default value we need to refresh the control 
      // to the value will set to the control
      public bool InSetToDefaultValue { get; set; }

      protected int _ditIdx;
      private readonly List<MgControlBase> _linkedControls; // array of controls that are linked to this control

      protected Field _field; // reference to the field of the control
      protected Field _nodeIdField; // reference to the field of the node id 
      protected Field _nodeParentIdField; // reference to the field of the parent node id
      public FieldDef DNObjectReferenceField { get; private set; } //The field associated with the dot net object.

      public ObjectReference SourceTableReference { get; internal set; }
      public bool ClipBoardDataExists { get; set; }

      private Type _DNType_do_not_use_directly;
      public Type DNType //Property holding the dot net control type.
      {
         get
         {
            Debug.Assert(IsDotNetControl());

            // PROP_TYPE_ASSEMBLY_ID and PROP_TYPE_REAL_OBJECT_TYPE properties are unavailable
            // if assembly is not loaded
            try
            {
               return _DNType_do_not_use_directly ??
                      (_DNType_do_not_use_directly =
                       ReflectionServices.GetType(getProp(PropInterface.PROP_TYPE_ASSEMBLY_ID).getValueInt(),
                                                  getProp(PropInterface.PROP_TYPE_REAL_OBJECT_TYPE).getValue()));
            }
            catch (System.Exception ex)
            {
               Events.WriteExceptionToLog(ex);
               return null;
            }
         }
      }

      public StorageAttribute DataType { get; protected internal set; }

      private String _picStr;
      protected List<MgControlBase> _siblingVec; // array of controls that are sibling to

      protected MgArrayList _choiceDisps; // for SELECT controls, holds the
      protected MgArrayList _choiceLayerList; // the order on which tabs to be displayed
      protected MgArrayList _choiceLinks; // for SELECT controls, holds the
      protected MgArrayList _choiceNums; // for SELECT controls, with numeric
      private int _containerDitIdx = -1;
      protected MgArrayList _currReadOnly; // a history array for readonly param
      private bool _dataCtrl; // The data control flag states that this
      protected MgArrayList _dcTableRefs; // references to dcVals for each line of a control is a data control
      private bool _firstRefreshProperties = true;
      private MgFormBase _form;
      private bool _hasValidItmAndDispVal;
      private int _id = -1; // ControlIsn of the control (for subform)
      private int _controlIsn = -1; // for current isn 
      private bool _horAligmentIsInherited;
      private bool _isMultiline;
      private int _linkedParentDitIdx = -1; // not trigger any event handler, while parking in this control
      public bool KeyStrokeOn { get; set; } // true if the user pressed a key
      public bool ModifiedByUser { get; set; } // true if user modified the control
      protected MgArrayList _orgChoiceDisps; // for TAB Control save the org option (with the &)
      protected MgControlBase _parentTable; // reference to parent Magic Table for
      protected PIC _pic;
      private bool _picExpExists;
      protected MgArrayList _prevIsNulls; // for NULL 
      private String _prevPicExpResult;
      protected MgArrayList _prevValues; // references to previous values of the
      protected PropTable _propTab;
      private String _range;
      private bool _rangeChanged; // for SELECT controls, true if their range
      private String _rtfVal = String.Empty; // field to store the rtf text of rich edit control
      public String Value { get; private set; }
      public bool IsNull { get; internal set; }
      protected int _valExpId; // reference to the value expression
      private ValidationDetails _vd;
      private bool _defaultTreeImageInitialized = false; //true after initialization of the default tree image, relevant for tree control only
      private int _dcValId = DcValues.EMPTY_DCREF;
#if !PocketPC
      private string[] prevValueList = null;
      private string[] prevDispList = null;
#endif
      internal int refreshTableCommandCount;
      private bool valueFromDesigner = false;
      protected int parent;
      protected int veeIndx;
      

      /// <summary>
      ///   CTOR
      /// </summary>
      protected internal MgControlBase()
      {
         _linkedControls = new List<MgControlBase>();
         DataType = StorageAttribute.NONE;
      }

      /// <summary>
      ///   CTOR
      /// </summary>
      protected internal MgControlBase(MgControlType type, MgFormBase parentMgForm, int parentControl)
         : this()
      {
         initReferences(parentMgForm);
         Type = type;
         _linkedParentDitIdx = _containerDitIdx = parentControl;

         _propTab = new PropTable(this);

         createArrays(false);
      }

      protected MgFormBase Form
      {
         get
         {
            return _form;
         }
         set
         {
            GuiMgForm = _form = value;
         }
      }

      internal int Id
      {
         get
         {
            return _id;
         }
      }

      /// <summary>
      /// 
      /// </summary>
      public int ControlIsn
      {
         get
         {
            return _controlIsn;
         }
      }

      internal bool HorAligmentIsInherited
      {
         get { return _horAligmentIsInherited; }
      }
      public bool RefreshOnVisible { get; set; } // true, if the subform must be refreshed on become visible
      public bool TmpEditorIsShow { get; set; } //for tree control, is temporary editor is show ?
      public bool InControl { get; set; }
      public string PromptHelp { get; private set; } //the prompt help text assigned to control
      #region PropParentInterface Members
     


      /// <summary>
      /// 
      /// </summary>
      /// <param name="propId"></param>
      /// <returns></returns>
      public bool checkIfExistProp(int propId)
      {
         bool exist = false;

         if (_propTab != null)
         {
            Property prop = _propTab.getPropById(propId);
            exist = prop != null;
         }
         return exist;
      }


/// <summary>
/// return true if this subform is under frame form
/// </summary>
/// <returns></returns>
      public bool IsSubformUnderFrameForm()
      {
         bool isSubFrmUnderFrameForm = false;
         MgControlBase parentControl = getParent() as MgControlBase;
         if (parentControl != null && parentControl.isContainerControl())
            isSubFrmUnderFrameForm = true;

         return isSubFrmUnderFrameForm;
      }

      /// <summary>
      /// return true if the control is frame 
      /// </summary>
      /// <returns></returns>
      public bool IsFrame()
      {
         bool isFrame = false;
         // fixed defect #132963, check if this subform is under frame form
         if (Type == MgControlType.CTRL_TYPE_SUBFORM && (getForm().IsFrameSet && !IsSubformUnderFrameForm()))
            isFrame = true;

         return isFrame;
      }

      /// <summary>
      ///   get a property of the control
      /// </summary>
      /// <param name = "propId">the Id of the property</param>
      public Property getProp(int propId)
      {
         Property prop = null;

         if (_propTab != null)
         {
            prop = _propTab.getPropById(propId);

            // if the property doesn't exist then create a new property and give it the default value
            // in addition add this property to the properties table
            if (prop == null)
            {
               prop = PropDefaults.getDefaultProp(propId, GuiConstants.PARENT_TYPE_CONTROL, this);
               if (prop != null)
                  _propTab.addProp(prop, false);
            }
         }
         return prop;
      }

      /// <summary>
      ///   returns the form of the control
      /// </summary>
      public MgFormBase getForm()
      {
         return Form;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      public int getCompIdx()
      {
         return getForm().getTask().getCompIdx();
      }

      /// <summary>
      ///   return true if this is first refresh
      /// </summary>
      /// <returns></returns>
      public bool IsFirstRefreshOfProps()
      {
         return _firstRefreshProperties;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="expId"></param>
      /// <param name="resType"></param>
      /// <param name="length"></param>
      /// <param name="contentTypeUnicode"></param>
      /// <param name="resCellType"></param>
      /// <param name="alwaysEvaluate"></param>
      /// <param name="wasEvaluated"></param>
      /// <returns></returns>
      public String EvaluateExpression(int expId, StorageAttribute resType, int length, bool contentTypeUnicode,
                                       StorageAttribute resCellType, bool alwaysEvaluate, out bool wasEvaluated)
      {
         return getForm().getTask().EvaluateExpression(expId, resType, length, contentTypeUnicode, resCellType, alwaysEvaluate, out wasEvaluated);
      }

      #endregion

      /// <summary>
      /// Returns variable index in the task tree
      /// </summary>
      /// <returns></returns>
      public virtual int GetVarIndex()
      {
         return 0;
      }

      /// <summary>
      /// get a property of the control that already was computed
      /// This method does not create a property if it isn't exist.
      /// May we need to create a property and use its default value.
      /// In Phase 2 in RefreshProperties method we'll create and compute properties, 
      /// perhaps we need create all properties not only those that are different from default.
      /// </summary>
      /// <param name = "propId">the Id of the property</param>
      public Property GetComputedProperty(int propId)
      {
         Property prop = null;

         if (_propTab != null)
            prop = _propTab.getPropById(propId);

         return prop;
      }

      /// <summary>
      /// Get the index of the help attached to the control.
      /// </summary>
      /// <returns>Help index</returns>
      public int getHelpIndex()
      {
         int hlpIndex = -1;

         if (_propTab != null)
         {
            Property prop = _propTab.getPropById(PropInterface.PROP_TYPE_HELP_SCR);
            if (prop != null)
               hlpIndex = Convert.ToInt32(prop.getValue());
         }

         return hlpIndex;
      }

      /// <summary>
      ///   Initializing of inner reference to the task of the control
      /// </summary>
      /// <param name = "taskRef">reference to the task of the control</param>
      private void initReferences(MgFormBase mgForm)
      {
         Form = mgForm;
      }

      /// <summary>
      ///   in the first time that we refresh the control need to create arrays
      /// </summary>
      /// <param name = "forceCreateArrays">create arrays in any case</param>
      protected internal void createArrays(bool forceCreateArrays)
      {
         if (_firstRefreshProperties || forceCreateArrays)
         {
            // for SELECT controls, always initialize DC table since we use it to merge choices from the
            // prop list with choices from the DATA CONTROL.
            if (SupportsDataSource())
            {
               _dcTableRefs = new MgArrayList();
            }
            _prevValues = new MgArrayList();
            // The property PROP_SET_READ_ONLY is set only if it is different from its previous value. By default 
            // this property is false, so we do not set it. 
            // But for the phantom task if this property was true, and we exit the phantom and again enter into
            // this phantom the property becomes false.
            // So we need to save the previous _currReadOnly value for the phantom task. 
            if (!forceCreateArrays)
               _currReadOnly = new MgArrayList();
            _choiceDisps = new MgArrayList();
            _orgChoiceDisps = new MgArrayList();
            _choiceLinks = new MgArrayList();
            _choiceNums = new MgArrayList();
            _prevIsNulls = new MgArrayList();
            _choiceLayerList = new MgArrayList();

            if (!IsRepeatable && !isTableControl())
               updateArrays(1);
         }
      }

      /// <summary>
      /// Evaluates the promptHelp assigned to control and sets it to 'PromptHelp' property
      /// </summary>
      private void EvaluatePromptHelp()
      {
         if (String.IsNullOrEmpty(this.PromptHelp))
         {
            TaskBase task = getForm().getTask();
            Property promptProp = getProp(PropInterface.PROP_TYPE_PROMPT);
            if (promptProp != null)
            {
               int idx = Int32.Parse(promptProp.getValue());
               string prompt = ((PromptpHelp)task.getHelpItem(idx)).PromptHelpText;
               this.PromptHelp = prompt;
            }
         }
      }

      /// <summary>
      /// Sets the focus on the control and refreshes prompt.
      /// </summary>
      /// <param name="ctrl">Control to have focus</param>
      /// <param name="line">line number</param>
      /// <param name="refreshPrompt"></param>
      /// <param name="activateForm">whether to activate a form after setfocus or not</param>
      public void SetFocus(MgControlBase ctrl, int line, bool refreshPrompt, bool activateForm)
      {         
         EvaluatePromptHelp();

         if (refreshPrompt)
            ctrl.refreshPrompt();

         Manager.SetFocus(ctrl.getForm().getTask(), ctrl, line, activateForm);
      }

      /// <summary>
      ///   update array's size according to the newSize
      /// </summary>
      /// <param name = "newSize"></param>
      public void updateArrays(int newSize)
      {
         _prevValues.SetSize(newSize);
         _prevIsNulls.SetSize(newSize);

         _currReadOnly.SetSize(newSize);
         _choiceDisps.SetSize(newSize);
         _orgChoiceDisps.SetSize(newSize);
         _choiceLinks.SetSize(newSize);
         _choiceLayerList.SetSize(newSize);
         _choiceNums.SetSize(newSize);
         if (SupportsDataSource())
            _dcTableRefs.SetSize(newSize);
         _propTab.updatePrevValueArray(newSize);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="newSize"></param>
      public void updatePrevValArrays(int newSize)
      {
         _propTab.updatePrevValueArray(newSize);
      }

      /// <summary>
      /// 
      /// </summary>
      public void ResetTablePrevValArray()
      {
         Property prop = _propTab.getPropById(PropInterface.PROP_TYPE_ROW_BG_COLOR);
         prop.ResetPrevValueArray();
      }

      /// <summary>
      ///   To parse input string and fill inner data
      /// </summary>
      /// <param name = "taskRef">to parent task</param>
      /// <param name = "ditidx">number of the Control in Control Table</param>
      public virtual void fillData(MgFormBase mgForm, int ditIdx)
      {
         XmlParser parser = Manager.GetCurrentRuntimeContext().Parser;
         if (Form == null)
            initReferences(mgForm);
         _ditIdx = ditIdx;

         while (initInnerObjects(parser.getNextTag()))
         {
         }

         HeaderControlUpdateRepeatable();

         // If the datatype is BLOB, its picture would be blank. But after version #97
         // of AppSerializer.cs (changed for #485722), blank picture is not serialized.
         // So, we need to explicitly set it to blank here.
         if (DataType == StorageAttribute.BLOB && _picStr == null)
            _picStr = "";

         if (_picStr != null && DataType != 0)
            setPIC(_picStr);
         else
         {
            switch (Type)
            {
               case MgControlType.CTRL_TYPE_IMAGE:
               case MgControlType.CTRL_TYPE_BROWSER:
               case MgControlType.CTRL_TYPE_BUTTON:
                  if (DataType == 0)
                     DataType = StorageAttribute.ALPHA;
                  break;
               case MgControlType.CTRL_TYPE_TABLE:
               case MgControlType.CTRL_TYPE_COLUMN:
               case MgControlType.CTRL_TYPE_SUBFORM:
               case MgControlType.CTRL_TYPE_GROUP:
               case MgControlType.CTRL_TYPE_FRAME_SET:
               case MgControlType.CTRL_TYPE_FRAME_FORM:
               case MgControlType.CTRL_TYPE_CONTAINER:
               case MgControlType.CTRL_TYPE_LINE:
               case MgControlType.CTRL_TYPE_RICH_TEXT:
               case MgControlType.CTRL_TYPE_DOTNET:
                  break;
               default:
                  Events.WriteExceptionToLog(
                     string.Format("in Control.fillData(): missing datatype or picture string for control: {0}", Name));
                  break;
            }
         }
         if (isTableControl())
            Form.setTableCtrl(this);
         else if (isTreeControl())
            Form.setTreeCtrl(this);

         createArrays(false);
         if (isContainerControl())
            getForm().setContainerCtrl(this);
         if (isFrameFormControl())
            getForm().setFrameFormCtrl(this);
      }

      /// <summary>
      ///   To allocate and fill inner objects of the class
      /// </summary>
      /// <param name = "foundTagName">name of tag, of object, which need be allocated</param>
      /// <returns> boolean if next tag is inner tag</returns>
      protected virtual bool initInnerObjects(String foundTagName)
      {
         if (foundTagName == null)
            return false;

         XmlParser parser = Manager.GetCurrentRuntimeContext().Parser;
         if (foundTagName.Equals(XMLConstants.MG_TAG_PROP))
         {
            if (_propTab == null)
               _propTab = new PropTable(this);
            _propTab.fillData(this, 'C'); // the parent Object of the Property is Control

            // since multiline property is not recomputed, we can save its value.
            Property prop = getProp(PropInterface.PROP_TYPE_MULTILINE);
            if (prop != null)
               _isMultiline = prop.getValueBoolean();
         }
         else if (foundTagName.Equals(XMLConstants.MG_TAG_CONTROL))
            parseAttributes();
         else if (foundTagName.Equals("SourceTable"))
            ParseSourceTable();
         else if (foundTagName.Equals("/" + XMLConstants.MG_TAG_CONTROL))
         {
            parser.setCurrIndex2EndOfTag(); // setCurrIndex(XmlParser.isNextTagClosed(MG_TAG_CONTROL));
            return false;
         }
         else
         {
            Events.WriteExceptionToLog(string.Format("There is no such tag in Control. Insert else if to Control.initInnerObjects for {0}", foundTagName));
            return false;
         }

         return true;
      }

      void ParseSourceTable()
      {
         XmlParser parser = Manager.GetCurrentRuntimeContext().Parser;
         parser.setCurrIndex2EndOfTag();
         string objRefStr = parser.ReadToEndOfCurrentElement();
         ObjectReference objRef = ObjectReference.FromXML(objRefStr.Trim());
         SourceTableReference = objRef;
         // Skip the closing </prop> tag
         parser.setCurrIndex2EndOfTag();
      }

      /// <summary>
      ///   get the attributes of the control from the XML
      /// </summary>
      private void parseAttributes()
      {
         XmlParser parser = Manager.GetCurrentRuntimeContext().Parser;
         int endContext = parser.getXMLdata().IndexOf(XMLConstants.TAG_CLOSE, parser.getCurrIndex());
         if (endContext != -1 && endContext < parser.getXMLdata().Length)
         {
            // last position of its tag
            String tag = parser.getXMLsubstring(endContext);
            parser.add2CurrIndex(tag.IndexOf(XMLConstants.MG_TAG_CONTROL) + XMLConstants.MG_TAG_CONTROL.Length);

            List<string> tokensVector = XmlParser.getTokens(parser.getXMLsubstring(endContext),
                                                            XMLConstants.XML_ATTR_DELIM);

            for (int j = 0; j < tokensVector.Count; j += 2)
            {
               string attribute = (tokensVector[j]);
               string valueStr = (tokensVector[j + 1]);

               SetAttribute(attribute, valueStr);
            }
            parser.setCurrIndex(++endContext); // to delete ">" too
            return;
         }
         Events.WriteExceptionToLog("in Control.FillName() out of string bounds");
      }

      /// <summary>
      /// set the control attributes in parsing
      /// </summary>
      protected virtual bool SetAttribute(string attribute, string valueStr)
      {
         bool isTagProcessed = true;

         switch (attribute)
         {
            case XMLConstants.MG_ATTR_TYPE:
               Type = (MgControlType)valueStr[0];
               break;
            case XMLConstants.MG_ATTR_DATA_CTRL:
               _dataCtrl = XmlParser.getBoolean(valueStr);
               break;
            case XMLConstants.MG_ATTR_LINKED_PARENT:
               setLinkedParentIdx(XmlParser.getInt(valueStr));
               break;
            case XMLConstants.MG_ATTR_CONTAINER:
               setContainer(XmlParser.getInt(valueStr));
               break;
            case XMLConstants.MG_ATTR_ID:
               _id = XmlParser.getInt(valueStr);
               break;
            case XMLConstants.MG_ATTR_CONTROL_ISN:
               _controlIsn = XmlParser.getInt(valueStr);
               break;
            case XMLConstants.MG_ATTR_CONTROL_Z_ORDER:
               ControlZOrder = XmlParser.getInt(valueStr);
               break;
            case XMLConstants.MG_HOR_ALIGMENT_IS_INHERITED:
               _horAligmentIsInherited = XmlParser.getInt(valueStr) == 1 ?  true : false;
               break;
            default:
               isTagProcessed = false;
               break;
         }

         return isTagProcessed;
      }

      /// <summary> set the image on the control </summary>
      /// <param name="fileName"></param>
      internal void setImage(String fileName)
      {
         if (fileName == null)
            fileName = "";
         fileName = Events.TranslateLogicalName(fileName);

         Property loadImageFrom = getProp(PropInterface.PROP_TYPE_LOAD_IMAGE_FROM);

         // For all controls other than ImageControl, file is always from server, so get 
         // the local path (after getting the file on the client-side.
         // For image control, PROP_TYPE_LOAD_IMAGE_FROM decides whether the file is from server or not.
         if (loadImageFrom == null || loadImageFrom.getValueInt() == (int)ExecOn.Server)
            fileName = Events.GetLocalFileName(fileName, Form.getTask()); // load image through cache mechanism.
         else if (this.isImageControl() && !fileName.StartsWith("@"))
            // Otherwise (i.e. 'Load Image From' <> 'Server'): #713012: ==> handle as a client/local file:
            // If the given filename has no explicit protocol (i.e. not already "http://", "https://" or "file://"),
            //   MgGui.dll will now handle the path as a local file, by internally prefixing the "file://" protocol to the path.
            // Other classes (in MgGui.dll, MgxpaRIA.exe and MgxpaRuntime.exe/MgCore.dll) are responsible to ignore the "file://" protocol 
            //   (which can be entered by developers and/or end-users - the "file://" protocol is de-facto allowed by Window's File Explorer), 
            //   regardless of the (current) bugfix. 
            // QCR #942927. this.isImageControl(): because only image control is allowed to select client/server (all others )
            // should load image from server only.
            HandleFiles.PrefixFileProtocol(ref fileName);

         Property imageStyleProp = getProp(PropInterface.PROP_TYPE_IMAGE_STYLE);
         Commands.addAsync(CommandType.PROP_SET_IMAGE_FILE_NAME, this, getDisplayLine(false), fileName, imageStyleProp.getValueInt());

         //We need to execute the layout only if the radio is not on table. for table, it is done 
         //from LgRadioContainer.setSpecificControlProperties().
         if (this.isRadio() && !IsRepeatable)
            Commands.addAsync(CommandType.EXECUTE_LAYOUT, this, getDisplayLine(false), false);
      }

      /// <summary>
      ///   set container
      /// </summary>
      /// <param name = "containerIdx"></param>
      private void setContainer(int containerIdx)
      {
         _containerDitIdx = containerIdx;
         MgControlBase container = null;
         if (_containerDitIdx != -1)
            container = Form.getCtrl(_containerDitIdx);
         if (container != null && container.Type == MgControlType.CTRL_TYPE_TABLE)
            _parentTable = container;

         IsRepeatable = (_parentTable != null && !isColumnControl());
      }

      /// <summary>
      ///   get the name of the Control
      /// </summary>
      public String getName()
      {
         if (isRepeatableOrTree())
            return getName(Form.DisplayLine);

         return Name;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      protected internal List<MgControlBase> getLinkedControls()
      {
         return _linkedControls;
      }

      /// <summary>
      ///   The control is repeatable or tree control
      /// </summary>
      /// <returns></returns>
      public bool isRepeatableOrTree()
      {
         return (IsRepeatable || isTreeControl());
      }

      /// <summary> returns current display line of control</summary>
      /// <param name="useLineForItems">if true, use current line for tree</param>
      public int getDisplayLine(bool useLineForItems)
      {
         int line = 0;
         if (IsRepeatable || (useLineForItems && (isTreeControl() || isTableControl())))
            line = Form.DisplayLine;
         return line;
      }

      /// <summary>
      /// checks if combobox is @D owner draw combobox
      /// </summary>
      /// <returns></returns>
      public bool isOwnerDrawComboBox()
      {
         return (isComboBox() && (getProp(PropInterface.PROP_TYPE_STYLE_3D).getValue() == "1"));
      }

      /// <summary>
      ///   return true if we are in text control or in tree with editor opened
      /// </summary>
      /// <returns></returns>
      public bool isTextOrTreeEdit()
      {
         return (isTextControl() || isRichEditControl() ||
                 (isTreeControl() && getForm().getTask().isStateEnabled(Constants.ACT_STT_TREE_EDITING)));
      }

      /// <summary>
      ///   checks visibility considering parent visibility
      /// </summary>
      /// <returns></returns>
      public bool isVisible()
      {
         bool result = GetComputedBooleanProperty(PropInterface.PROP_TYPE_VISIBLE, true);
         if (result)
            result = isParentPropValue(PropInterface.PROP_TYPE_VISIBLE);
         return result;
      }

      /// <summary>
      ///   checks enabled considering parent enabled
      /// </summary>
      /// <returns></returns>
      public bool isEnabled()
      {
         bool result = GetComputedBooleanProperty(PropInterface.PROP_TYPE_ENABLED, true);
         if (result)
            result = isParentPropValue(PropInterface.PROP_TYPE_ENABLED);
         return result;
      }

      /// <summary> Returns if Paste is allowed on the Control. </summary>
      /// <returns></returns>
      public bool IsPasteAllowed()
      {
         bool isPasteAllowed = false;

         if (isTextOrTreeEdit() && isModifiable())
         {
            if (!ClipBoardDataExists && Manager.ClipboardRead() != null)
               ClipBoardDataExists = true;
            if (ClipBoardDataExists)
               isPasteAllowed = true;
         }

         return isPasteAllowed;
      }

      /// <summary>
      ///   returns true if the control is modifiable and the task mode is not query
      /// </summary>
      public bool isModifiable()
      {
         bool result;

         bool modifiable = GetComputedBooleanProperty(PropInterface.PROP_TYPE_MODIFIABLE, true);
         bool taskInQuery = getForm().getTask().getMode() == Constants.TASK_MODE_QUERY;
         bool modifyInQuery = GetComputedBooleanProperty(PropInterface.PROP_TYPE_MODIFY_IN_QUERY, false);

         result = modifiable && (!taskInQuery || modifyInQuery);

         if (result && _field != null)
            result = _field.DbModifiable || getForm().getTask().getMode() == Constants.TASK_MODE_CREATE;

         return result;
      }

      /// <summary>
      ///   return true if the ctrl is multiline.
      /// </summary>
      /// <returns></returns>
      public bool isMultiline()
      {
         return _isMultiline;
      }

      /// <summary>
      ///   returns the current dc values of this data control from the current record
      /// </summary>
      protected DcValues getDcVals()
      {
         DataViewBase dataview = getForm().getTask().DataView;
         DcValues dcv = null;

         if (_dcValId == DcValues.EMPTY_DCREF)
            dcv = (DataType == StorageAttribute.BLOB_VECTOR
                                    ? dataview.getEmptyChoiceForVectors()
                                    : dataview.getEmptyChoice());
         else if (_dcValId > -1)
            dcv = (DcValues)dataview.getDcValues(_dcValId);

         return dcv;
      }

      /// <summary>
      ///   return the current isNull according to the control's current line number
      /// </summary>
      /// <returns> the PrevIsNull value</returns>
      protected internal bool getPrevIsNull()
      {
         int line = getDisplayLine(true);
         Object currObj = _prevIsNulls[line];
         Boolean curr;
         if (currObj == null)
         {
            curr = false;
            _prevIsNulls[line] = curr;
         }
         else
            curr = (Boolean)currObj;
         return curr;
      }

      /// <summary>
      ///   sets the prevIsNull value of the prevIsNull array
      ///   the newValue can be NULL or bool
      /// </summary>
      /// <param name = "newValue">the new value to be set</param>
      protected internal void setPrevIsNull(bool newValue)
      {
         _prevIsNulls[getDisplayLine(true)] = newValue;
      }

      /// <param name = "picStr"></param>
      internal void setPIC(String picStr)
      {
         // for push button, translate the format.
         if (isButton())
            _picStr = IsImageButton()
                         ? "" + XMLConstants.FILE_NAME_SIZE
                         : Events.Translate(picStr);
         else
            _picStr = picStr;

         _pic = new PIC(_picStr, DataType, getForm().getTask().getCompIdx());
         if (_dataCtrl && _picStr.IndexOf('H') > -1)
            _pic.setHebrew();
      }

      /// <summary>
      ///   get the picture of the control
      /// </summary>
      /// <returns> PIC reference</returns>
      public PIC getPIC()
      {
         return _pic;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      public Field getNodeIdField()
      {
         return _nodeIdField;
      }

      /// <returns> the parent of the control can be Table\Tab\Choice control\Form</returns>
      public override Object getParent()
      {
         Object retParent = Form;

         if (_containerDitIdx != -1)
            retParent = Form.getCtrl(_containerDitIdx);
         else if (Form.isSubForm())
            retParent = Form.getSubFormCtrl();
         return retParent;
      }

      /// <summary>
      ///   returns the field attached to the control
      /// </summary>
      public Field getField()
      {
         return _field;
      }

      /// <summary>
      ///   Set the field of the control by the field object
      /// </summary>
      /// <param name = "field"></param>
      internal void setField(Field field)
      {
         _field = field;
         if (field != null)
            _field.SetControl(this);
      }

      /// <summary>
      ///   Set the field of the control by a string field identifier
      /// </summary>
      /// <param name = "valueStr">the field identifier: "parentId,fieldIdx"/param>
      public virtual void setField(String fieldStrID)
      {
         Field returnField = getFieldByValueStr(fieldStrID);
         if (returnField != null)
            _field = returnField;
         else
            throw new ApplicationException("in Control.setField(): illegal field identifier: " + fieldStrID);

         _field.SetControl(this);
      }

      /// <summary>
      ///  Set the DN object ref field of the control by a string field identifier
      /// </summary>
      /// <param name="fieldStrID"></param>
      internal void SetDNObjectReferenceField(String fieldStrID)
      {
         DNObjectReferenceField = getFieldByValueStr(fieldStrID);
         Debug.Assert(DNObjectReferenceField != null, string.Format("In MgControlBase.SetDNObjectReferenceField(): illegal field identifier: {0}", fieldStrID));
      }

      /// <summary>
      ///   set the picture of the control
      /// </summary>
      /// <param name = "format">the format string</param>
      /// <param name = "expId">an expression of the format</param>
      protected internal void setPicStr(String format, int expId)
      {
         _picStr = format;
         if (expId > 0)
            _picExpExists = true;
      }

      /// <summary>
      ///   sets the range of the control and clears the mask
      /// </summary>
      /// <param name = "newRange">is the new range</param>
      protected internal void setRange(String newRange)
      {
         _range = newRange;
         _vd = null;
      }

      /// <summary>
      ///   set the value expression
      /// </summary>
      /// <param name = "expId">a reference to the expression</param>
      protected internal void setValExp(int expId)
      {
         _valExpId = expId;
      }

      /// <returns> the a default value for the type of the field of the control
      ///   this value can be used in order to create an empty display value with the mask.
      /// </returns>
      internal String getDefaultValueForEdit()
      {
         return FieldDef.getMagicDefaultValue(DataType);
      }

      /// <summary>
      ///   get copy of validation details with new & old value without changing real validation details
      /// </summary>
      /// <param name = "oldVal">value of the control</param>
      /// <param name = "newVal">value of the control</param>
      internal ValidationDetails buildCopyPicture(String oldVal, String newVal)
      {
         if (isTableControl() || isColumnControl())
            return null;
         ValidationDetails copyVD = getCopyOfVD();
         copyVD.setValue(newVal);
         copyVD.setOldValue(oldVal);
         return copyVD;
      }

      /// <summary>
      ///   build copy of validation details, without influence to the real validation details
      /// </summary>
      private ValidationDetails getCopyOfVD()
      {
         if (_vd == null)
            return new ValidationDetails(Value, Value, _range, _pic, this);
         else
            return new ValidationDetails(_vd);
      }

      /// <param name = "valueStr"></param>
      /// <returns></returns>
      private Field getFieldByValueStr(String valueStr)
      {
         return (Field)getForm().getTask().getFieldByValueStr(valueStr, out parent, out veeIndx);
      }

      /// <summary>
      ///   Set the field of the node id (for tree control only) by a string field identifier
      /// </summary>
      /// <param name = "valueStr">the field identifier: "parentId,fieldIdx"</param>
      protected internal void setNodeId(String valueStr)
      {
         Field returnField = getFieldByValueStr(valueStr);
         if (returnField != null)
            _nodeIdField = returnField;
         else
            throw new ApplicationException("in Control.setNodeId(): illegal nodeIdfield identifier: " + valueStr);
      }

      /// <summary>
      ///   Set the field of the parent node id (for tree control only) by a string field identifier
      /// </summary>
      /// <param name = "valueStr">the field identifier: "parentId,fieldIdx"</param>
      protected internal void setNodeParentId(String valueStr)
      {
         Field returnField = getFieldByValueStr(valueStr);
         if (returnField != null)
            _nodeParentIdField = returnField;
         else
            throw new ApplicationException("in Control.setNodeId(): illegal parentNodeIdfield identifier: " + valueStr);
      }

      /// <summary>
      ///   returns true if has container
      /// </summary>
      /// <returns></returns>
      public bool hasContainer()
      {
         return _containerDitIdx != -1;
      }

      /// <summary>
      ///   gets layer of control
      /// </summary>
      /// <returns></returns>
      protected internal int getLayer()
      {
         return Layer;
      }

      /// <summary>
      ///   computes the pic using the result of the format expression
      /// </summary>
      /// <param name = "picExpResult">the result of the format expression</param>
      /// <returns> PIC reference </returns>
      protected internal PIC computePIC(String picExpResult)
      {
         // for push button, translate the format.
         string picResult = picExpResult != null && isButton()
                               ? Events.Translate(picExpResult)
                               : picExpResult;

         // construct a new picture only if the expression result is different than the previous result
         if (_prevPicExpResult == null || !_prevPicExpResult.Equals(picResult))
         {
            _pic = new PIC(picResult, DataType, getForm().getTask().getCompIdx());
            _prevPicExpResult = picResult;
         }

         return _pic;
      }

      /// <summary>
      /// gets the index of the current choice in the choice control
      /// </summary>
      /// <returns>index of the current choice</returns>
      public int[] getCurrentIndexOfChoice()
      {
         int[] selectedIndice = new int[] { -1 };

         String val = Value;

         if (!string.IsNullOrEmpty(val))
         {
            string[] indice = val.Split(new char[] { ',' });
            selectedIndice = new int[indice.Length];

            for (int iCtr = 0; iCtr < indice.Length; iCtr++)
            {
               int idx = Convert.ToInt32(indice[iCtr]);
               selectedIndice[iCtr] = idx >= 0 ? idx : -1;
            }
         }

         return selectedIndice;
      }

      /// <summary>
      /// Returns true is multi selection list box else false.
      /// </summary>
      /// <returns></returns>
      public bool IsMultipleSelectionListBox()
      {
         bool isMultiSelectListBox = false;
         if (isListBox())
         {
            Property prop = getProp(PropInterface.PROP_TYPE_SELECTION_MODE);
            if (prop != null)
               isMultiSelectListBox = prop.getValueInt() == (int)ListboxSelectionMode.Multiple ? true : false;
         }

         return isMultiSelectListBox;
      }

      /// <summary>
      /// Returns true if control is .net choice control
      /// </summary>
      /// <returns></returns>
      internal bool IsDotNetChoiceControl()
      {
         return (IsDotNetControl() && _propTab.propExists(PropInterface.PROP_TYPE_DN_CONTROL_DATA_SOURCE_PROPERTY)
                 && !IsDataViewControl());
      }

      /// <summary>
      /// Returns true if control picture is date or time
      /// </summary>
      /// <returns></returns>
      internal bool IsDateTimePicture()
      {
         return _pic.getAttr() == StorageAttribute.DATE || _pic.getAttr() == StorageAttribute.TIME;
      }

      /// <summary>
      /// Get the attribute when control is .net control
      /// if linkfield is set get the attribute from DcVals,
      /// else get the attr from data attached to data prop of ctrl.
      /// else return data type as unicode.
      /// </summary>
      /// <returns></returns>
      internal StorageAttribute GetDNChoiceCtrlDataType()
      {
         StorageAttribute dataType = StorageAttribute.NONE;
         DataViewBase dataview = getForm().getTask().DataView;
         DcValues dcv = dataview.getDcValues(_dcValId);

         if (IsDotNetChoiceControl())
         {
            if (dcv != null && (_dcValId != DcValues.EMPTY_DCREF))
               dataType = dcv.GetAttr();
            else if (_field != null)
            {
               Debug.Assert(DataType != StorageAttribute.BLOB_VECTOR);

               dataType = DataType;
               if (dataType == StorageAttribute.DOTNET)
                  dataType = DNConvert.getDefaultMagicTypeForDotNetType(_field.DNType);
            }
            else if (getProp(PropInterface.PROP_TYPE_LABEL) != null)
               dataType = StorageAttribute.UNICODE;
         }
         return dataType;
      }

      /// <summary>
      ///   this control support data source
      /// </summary>
      /// <returns></returns>
      public bool SupportsDataSource()
      {
         return (isSelectionCtrl() || isTabControl() || isRadio() || IsDotNetChoiceControl());
      }

      /// <summary>
      /// Returns the array of indice which corresponds to a specific link value
      /// </summary>
      /// <param name = "mgVal">the requested value</param>
      /// <param name="line"></param>
      /// <param name = "isNull">true if mgVal represents a null value</param>
      /// <returns> array of indice</returns>
      protected int[] getIndexOfChoice(String mgVal, int line, bool isNull)
      {
         bool splitCommaSeperatedVals = false;

         //if the control is listbox with single selection mode, and the value is comma separated, then we should not split value on comma.
         splitCommaSeperatedVals = IsMultipleSelectionListBox() ? true : false;

         computeChoice(line);
         bool isVector = (getField() != null && getField().getType() == StorageAttribute.BLOB_VECTOR);
         return getDcVals().getIndexOf(mgVal, isVector, isNull, (String[])_choiceLinks[line], (NUM_TYPE[])_choiceNums[line], splitCommaSeperatedVals);
      }

      /// <returns> true if this is a SELECT control which has a PROP_TYPE_DISPLAY_LIST property containing valid
      ///   values.
      /// </returns>
      internal bool hasDispVals(int line)
      {
         String[] choice = getDispVals(line, false);
         return (getProp(PropInterface.PROP_TYPE_DISPLAY_LIST) != null && choice != null && choice.Length > 0);
      }

      /// <returns> parsed content of PROP_TYPE_DISPLAY_LIST</returns>
      public String[] getDispVals(int line, bool execComputeChoice)
      {
         if (execComputeChoice)
            computeChoice(line);

         if (isTabControl())
            return (String[])_orgChoiceDisps[line];

         return (String[])_choiceDisps[line];
      }

      /// <summary>
      /// Get max count of display items for choice ctrl.
      /// </summary>
      /// <returns>returns display count</returns>
      public int getMaxDisplayItems()
      {
         int maxLayer = ((String[])_choiceLayerList[0]).Length;

         if (maxLayer == 0)
         {
            String[] dispValsSrcData = getDcVals().getDispVals();

            maxLayer = ((String[])_choiceDisps[0]).Length;
            if (dispValsSrcData != null)
               maxLayer += dispValsSrcData.Length;
         }
         return maxLayer;
      }


      /// <summary>
      /// 
      /// </summary>
      /// <param name="line"></param>
      /// <returns></returns>
      private void emptyChoice(int line)
      {
         var emptyStr = new String[0];
         _choiceDisps[line] = emptyStr;
         _orgChoiceDisps[line] = emptyStr;
         _choiceLinks[line] = emptyStr;
         _choiceLayerList[line] = emptyStr;
      }

      /// <summary>
      ///   computes the choices of a SELECT control that originate from the control's properties (in contrast to
      ///   the ones originating from the underlaying table of a data control)
      /// </summary>
      private void computeChoice(int line)
      {
         var fromHelp = new[] { "\\\\", "\\-", "\\," };
         var toHelp = new[] { "XX", "XX", "XX" };
         TaskBase task = getForm().getTask();
         DataViewBase dataview = task.DataView;
         DcValues dcv = dataview.getDcValues(_dcValId);
         int currDcId = getDcRef();
         Property dispProp, linkProp;
         String choiceDispStr = null, choiceLinkStr = "";
         String token;
         String helpStr, helpToken;
         String[] sTok;
         StringBuilder tokenBuffer;
         int size, i, currPos = 0, nextPos = 0, tokenPos;
         bool optionsValid = true;
         int trimToLength = -1;
         // Get the proper attribute for .Net choice ctrl
         StorageAttribute dataType = IsDotNetChoiceControl() ? GetDNChoiceCtrlDataType() : DataType;
         bool isItemsListTreatedAsDisplayList = false;

         try
         {
            // re-compute values only if they are not valid or the DC was changed (due to recompute)
            if (_choiceLinks[line] == null || isDataCtrl() && currDcId != _dcValId)
            {
               //In case of blob vector, datatype should be the type of the vector cell.
               if (dataType == StorageAttribute.BLOB_VECTOR)
                  dataType = getField().getCellsType();

               string[] tableVals = dcv.getDispVals();

               dispProp = getProp(PropInterface.PROP_TYPE_DISPLAY_LIST);
               linkProp = getProp(PropInterface.PROP_TYPE_LABEL);

               if (dispProp != null)
                  choiceDispStr = dispProp.getValue();

               if (linkProp != null)
                  choiceLinkStr = linkProp.getValue();

               //If 'choiceLinkStr' is null, then it must be initialized to "".Null value is not permissible.
               if (choiceLinkStr == null || StrUtil.rtrim(choiceLinkStr).Length == 0)
                  choiceLinkStr = "";

               if (choiceDispStr == null || StrUtil.rtrim(choiceDispStr).Length == 0)
               {
                  // If no PROP_TYPE_DISPLAY exist:
                  // (1) non DC controls and empty DC just use the link value as display
                  if (!isDataCtrl() || tableVals == null || tableVals.Length == 0)
                     choiceDispStr = choiceLinkStr;
                  else
                     choiceLinkStr = choiceDispStr = "";

                  isItemsListTreatedAsDisplayList = true;
               }

               // translate the value strings, only for the disp values
               choiceDispStr = Events.Translate(choiceDispStr);

               if (dataType == StorageAttribute.NUMERIC)
                  choiceLinkStr = StrUtil.searchAndReplace(choiceLinkStr, "\\-", "-");

               helpStr = StrUtil.searchAndReplace(choiceLinkStr, fromHelp, toHelp);
               sTok = StrUtil.tokenize(helpStr, ",");
               int linkSize = (helpStr != ""
                              ? sTok.Length
                              : 0);

               String helpStrDisp = StrUtil.searchAndReplace(choiceDispStr, fromHelp, toHelp);
               sTok = StrUtil.tokenize(helpStrDisp, ",");
               int displaySize = (helpStrDisp != ""
                              ? sTok.Length
                              : 0);

               if (linkSize != displaySize && displaySize != 0)
               {
                  // discard display list
                  choiceDispStr = choiceLinkStr;
                  isItemsListTreatedAsDisplayList = true;
               }

               size = linkSize;
               var choiceLink = new String[size];
               _choiceLinks[line] = choiceLink;

               if (dataType == StorageAttribute.NUMERIC || dataType == StorageAttribute.DATE ||
                   dataType == StorageAttribute.TIME)
                  _choiceNums[line] = new NUM_TYPE[size];

               // Add display values

               string[] orgChoiceDisp = ChoiceUtils.GetDisplayListFromString(choiceDispStr, false, true, !isItemsListTreatedAsDisplayList);
               _orgChoiceDisps[line] = orgChoiceDisp;

               string[] choiceDisp = ChoiceUtils.GetDisplayListFromString(choiceDispStr, (isSelectionCtrl() || isTabControl()), true, !isItemsListTreatedAsDisplayList);
               _choiceDisps[line] = choiceDisp;

               if (getField() != null)
                  trimToLength = getField().getSize();

               // Add link values.
               for (i = 0, currPos = 0, nextPos = 0; i < size && optionsValid; i++)
               {
                  nextPos = currPos;
                  nextPos = helpStr.IndexOf(',', nextPos);

                  if (nextPos == currPos)
                     token = helpToken = "";
                  else if (nextPos == -1)
                  {
                     token = choiceLinkStr.Substring(currPos);
                     helpToken = helpStr.Substring(currPos);
                  }
                  else
                  {
                     token = choiceLinkStr.Substring(currPos, (nextPos) - (currPos));
                     helpToken = helpStr.Substring(currPos, (nextPos) - (currPos));
                  }

                  currPos = nextPos + 1;

                  switch (dataType)
                  {
                     case StorageAttribute.ALPHA:
                     case StorageAttribute.MEMO:
                     case StorageAttribute.UNICODE:
                     case StorageAttribute.BLOB_VECTOR:
                        token = StrUtil.ltrim(token);
                        helpToken = StrUtil.ltrim(helpToken);
                        if (helpToken.IndexOf('\\') >= 0)
                        {
                           tokenBuffer = new StringBuilder();
                           for (tokenPos = 0; tokenPos < helpToken.Length; tokenPos++)
                              if (helpToken[tokenPos] != '\\')
                                 tokenBuffer.Append(token[tokenPos]);
                              else if (tokenPos == helpToken.Length - 1)
                                 tokenBuffer.Append(' ');

                           token = tokenBuffer.ToString();
                        }
                        token = StrUtil.makePrintableTokens(token, StrUtil.SEQ_2_STR);
                        if (isSelectionCtrl() || isTabControl())
                           token = ChoiceUtils.RemoveAcclCharFromOptions(new StringBuilder(token));

                        // choiceLink size cannot be greater than size of the field, so trim it.
                        if (UtilStrByteMode.isLocaleDefLangDBCS() && dataType == StorageAttribute.ALPHA)
                        {
                           if (trimToLength != -1 && UtilStrByteMode.lenB(token) > trimToLength)
                              token = UtilStrByteMode.leftB(token, trimToLength);
                        }
                        else
                        {
                           if (trimToLength != -1 && token.Length > trimToLength)
                              token = token.Substring(0, trimToLength);
                        }

                        choiceLink[i] = token;
                        break;

                     case StorageAttribute.NUMERIC:
                     case StorageAttribute.DATE:
                     case StorageAttribute.TIME:
                        PIC picture = PIC.buildPicture(dataType, token, task.getCompIdx(), false);
                        optionsValid = optionIsValid(token);
                        choiceLink[i] = DisplayConvertor.Instance.disp2mg(token.Trim(), choiceLinkStr, picture,
                                                                               task.getCompIdx(),
                                                                               BlobType.CONTENT_TYPE_UNKNOWN);
                        ((NUM_TYPE[])_choiceNums[line])[i] = new NUM_TYPE(choiceLink[i]);
                        break;

                     case StorageAttribute.BOOLEAN:
                        choiceLink[i] = Convert.ToString(1 - i);
                        break;

                     default:
                        break;
                  }
               }

               if (!optionsValid)
                  emptyChoice(line);
            }
         }
         catch (Exception e)
         {
            optionsValid = false;
            emptyChoice(line);
            Events.WriteExceptionToLog(string.Format("{0} : {1}", e.GetType(), e.Message));
         }

         _hasValidItmAndDispVal = optionsValid;
      }

      /// <summary>
      ///   return the topmost form of the subform
      /// </summary>
      /// <returns></returns>
      public MgFormBase getTopMostForm()
      {
         return (getForm().getTopMostForm());
      }

      /// <summary>
      ///   replace the url on browser control
      /// </summary>
      private void setUrl()
      {
         Commands.addAsync(CommandType.PROP_SET_URL, this, 0, Value, 0);
      }

      /// <summary>  
      /// process opening of combo box's dropdown list.
      /// </summary>
      /// <param name = "line">table control's row</param>
      /// <returns></returns>
      public void processComboDroppingdown(int line)
      {
         if (isParkable(true, false) && getDisplayLine(true) == line && isModifiable())
         {
            Commands.addAsync(CommandType.COMBO_DROP_DOWN, this, line, false);
            Commands.beginInvoke();
         }
      }

      /// <summary>
      ///   returns the value in internal representation without changing the control value
      /// </summary>
      /// <param name = "dispVal">the displayed value with the masking characters</param>
      public String getMgValue(String dispVal)
      {
         String mgVal = null;

         if (isCheckBox())
            mgVal = dispVal;
         else if (isSelectionCtrl() || isTabControl() || isRadio())
         {
            int line = getDisplayLine(true);

            if (line < 0)
               line = 0;

            if (dispVal == "")
               mgVal = dispVal;
            else
               mgVal = getLinkValue(dispVal, line);

            if (mgVal == null)
            {
               // The data control will never return an illegal value as a result of a user selection
               // but as a result of an init expression, update operation or values of data in
               // the record. Therefore, the same illegal value should be taken from its source,
               // which is a field or an expression. (Ehud 19-jun-2001)
               if (_field != null)
               {
                  bool isNull = false;
                  ((TaskBase)_field.getTask()).getFieldValue(_field, ref mgVal, ref isNull);
               }
               else if (_valExpId > 0)
               {
                  bool wasEvaluated;
                  mgVal = EvaluateExpression(_valExpId, DataType, _pic.getSize(), true, StorageAttribute.SKIP, false, out wasEvaluated);
               }
            }
         }
         else
         {
            char blobContentType = (_field != null
                                       ? _field.getContentType()
                                       : BlobType.CONTENT_TYPE_UNKNOWN);
            mgVal = DisplayConvertor.Instance.disp2mg(dispVal, _range, _pic, getForm().getTask().getCompIdx(), blobContentType);
         }
         return mgVal;
      }

      /// <summary>
      /// Returns comma separated values for indice passed.
      /// </summary>
      /// <param name="selectedIndice">comma separated values</param>
      /// <param name="line"></param>
      /// <returns>comma separated values</returns>
      protected String getLinkValue(string selectedIndice, int line)
      {
         int size = 0;
         String result = string.Empty;
         List<String> temp = new List<String>();

         computeChoice(line);

         var choiceLink = (String[])_choiceLinks[line];

         if (choiceLink != null)
            // compute size of displayed values array
            size = choiceLink.Length;

         int[] linkIndice = getLinkIdxFromLayer(Misc.GetIntArray(selectedIndice));

         foreach (var idx in linkIndice)
         {

            // first come displayed values, than values form source table
            if (idx >= size)
            {
               DcValues dcv = getDcVals();
               temp.Add(dcv.getLinkValue(idx - size));
            }
            else
            {
               // computeChoice(line);
               if (idx >= 0 && idx < choiceLink.Length)
                  temp.Add(choiceLink[idx]);
               else
                  temp.Add(null);
            }
         }

         //If vector is attached to control, then split the values and set them into a vector and return that vector.
         if (DataType == StorageAttribute.BLOB_VECTOR)
         {
            var vec = new VectorType(getField());

            for (int indx = 0; indx < temp.Count; indx++)
               vec.setVecCell(indx + 1, temp[indx], temp[indx] == null);

            result = vec.ToString();
         }
         else
         {
            if (temp.Count > 1)
               result = String.Join(",", temp.ToArray());
            else
               result = temp[0];
         }

         return result;
      }

      /// <summary>
      ///   set text on the control
      /// </summary>
      private void setText()
      {
         Debug.Assert(Misc.IsWorkThread());

         // if the value was set from the runtime designer, the text shouldn't be set
         if (!valueFromDesigner)
         {
            int line = getDisplayLine(true);

            String mlsTranslatedValue = Value;
            if (isButton())
               mlsTranslatedValue = Events.Translate(Value);

            Commands.addAsync(CommandType.PROP_SET_TEXT, this, line, mlsTranslatedValue, 0);
         }
      }

      /// <summary>
      ///   Process Rich Text actions.
      /// </summary>
      /// <param name="act"></param>
      public void ProcessRichTextAction(int act)
      {
         if (!isRichEditControl())
            return;

         switch (act)
         {
            case InternalInterface.MG_ACT_ALIGN_LEFT:
               setAlignment(AlignmentTypeHori.Left);
               break;

            case InternalInterface.MG_ACT_ALIGN_RIGHT:
               setAlignment(AlignmentTypeHori.Right);
               break;

            case InternalInterface.MG_ACT_CENTER:
               setAlignment(AlignmentTypeHori.Center);
               break;

            case InternalInterface.MG_ACT_BULLET:
               setBullet();
               break;

            case InternalInterface.MG_ACT_INDENT:
               setIndent();
               break;

            case InternalInterface.MG_ACT_UNINDENT:
               setUnindent();
               break;

            case InternalInterface.MG_ACT_CHANGE_COLOR:
               changeColor();
               break;

            case InternalInterface.MG_ACT_CHANGE_FONT:
               changeFont();
               break;
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="newRtfVal"></param>
      private void setRtfval(String newRtfVal)
      {
         _rtfVal = newRtfVal;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="alignment"></param>
      internal void setAlignment(AlignmentTypeHori alignment)
      {
         if (isRichEditControl())
            Commands.addAsync(CommandType.SET_ALIGNMENT, this, 0, alignment);
      }

      /// <summary>
      /// 
      /// </summary>
      internal void setBullet()
      {
         if (isRichEditControl())
            Commands.addAsync(CommandType.BULLET, this);
      }

      /// <summary>
      /// 
      /// </summary>
      internal void setIndent()
      {
         if (isRichEditControl())
            Commands.addAsync(CommandType.INDENT, this);
      }

      /// <summary>
      /// 
      /// </summary>
      internal void setUnindent()
      {
         if (isRichEditControl())
            Commands.addAsync(CommandType.UNINDENT, this);
      }

      /// <summary>
      /// 
      /// </summary>
      internal void changeColor()
      {
         if (isRichEditControl())
         {
            Commands.addAsync(CommandType.CHANGE_COLOR, this);
            Commands.beginInvoke();
         }
      }

      /// <summary>
      /// 
      /// </summary>
      internal void changeFont()
      {
         if (isRichEditControl())
         {
            Commands.addAsync(CommandType.CHANGE_FONT, this);
            Commands.beginInvoke();
         }
      }

      /// <summary>
      ///   set the radio button checked
      /// </summary>
      /// <param name = "radioLine">is the line of the radio that need to be Set</param>
      private void setRadioChecked(int radioLine)
      {
         if (_siblingVec != null && radioLine >= 0)
         {
            // set all the brothers control to be with value false
            for (int i = 0; i < _siblingVec.Count; i++)
            {
               MgControlBase siblingCtrl = _siblingVec[i];
               // Since we want to deselect all radio buttons, we should send -1, 
               // otherwise it should be the index of radio button in panel.
               siblingCtrl.setChecked(-1, "0");
            }
         }
         setChecked(radioLine, "1");

         //set control to focus
         if (radioLine != GuiConstants.DEFAULT_VALUE_INT)
            setControlToFocus();
      }

      /// <summary>
      ///   Sets the t\f to the check box control
      /// </summary>
      /// <param name="lineInRadio">index of a radio button inside the radio panel</param>
      /// <param name="mgVal"></param>
      /// <returns></returns>
      private void setChecked(int lineInRadio, String mgVal)
      {
         Commands.addAsync(CommandType.PROP_SET_CHECKED, this, getDisplayLine(false), lineInRadio, DisplayConvertor.toBoolean(mgVal));
         Commands.beginInvoke();
      }

      /// <summary>
      /// </summary>
      public void setControlToFocus()
      {
         if (getField() != null)
            getField().ControlToFocus = this;
      }

      /// <summary>
      ///   replace the image of an image control with the image at the given URL
      /// </summary>
      internal void setImage()
      {
         String newVal = Value;

         // TODO: Null Display value should be shown on Image control. Currently blank data is set 
         // to fix the exception that occurs when null display value is sent as image data(#978635).
         if (newVal == null || IsNull)
            newVal = "";
         Property imageStyleProp = getProp(PropInterface.PROP_TYPE_IMAGE_STYLE);

         if (DataType == StorageAttribute.BLOB)
            Commands.addAsync(CommandType.PROP_SET_IMAGE_DATA, this, getDisplayLine(false), BlobType.getBytes(newVal),
                              imageStyleProp.getValueInt());
         else
            setImage(newVal);
      }

      /// <summary>
      ///   set CheckBox Value to the check box control
      /// </summary>
      /// <param name="line"></param>
      /// <param name="mgVal"></param>
      /// <returns></returns>
      private void setCheckBoxValue(int line, String mgVal)
      {
         MgCheckState checkState = MgCheckState.UNCHECKED;

         Boolean IsThreeStates = checkProp(PropInterface.PROP_TYPE_THREE_STATES, false);
         if (IsThreeStates && IsNull)
            checkState = MgCheckState.INDETERMINATE;
         else
            checkState = (DisplayConvertor.toBoolean(mgVal)
                             ? MgCheckState.CHECKED
                             : MgCheckState.UNCHECKED);

         Commands.addAsync(CommandType.PROP_SET_CHECK_BOX_CHECKED, this, line, checkState);
         Commands.beginInvoke();
      }

      /// <summary>
      /// </summary>
      /// <param name = "line"></param>
      /// <param name = "valueChanged"></param>
      /// <returns></returns>
      private bool refreshAndSetItemListByDataSource(int line, bool valueChanged)
      {
         if (SupportsDataSource())
         {
            int currDcId = getDcRef();

            // If the range was changed we have to recalculate the correct choice option
            if (_rangeChanged)
               valueChanged = true;

            _rangeChanged = false;

            if (isDataCtrl() && currDcId != _dcValId)
            {
               // Performance improvement: server writes optionList to the HTML. No need to apply
               // them
               // for the first time.
               // force update of the value whenever the set of options is replaced
               // if (form.formRefreshed() || forceRefresh)
               {
                  valueChanged = true;
                  refreshAndSetItemsList(line, true);
               }
               setDcRef(_dcValId);
            }
         }
         return valueChanged;
      }

      /// <summary>
      ///   set the dcref for the current table line
      /// </summary>
      /// <param name = "dcId">the new reference to the dcvals</param>
      public void setDcRef(int dcId)
      {
         _dcTableRefs[getDcLineNum()] = dcId;
      }

      /// <summary>
      ///   replace the image list of an button control with the image at the given URL
      /// </summary>
      internal void setImageList(String url)
      {
         if (url == null)
            url = "";
         //Converting the logical name to actual value.
         url = Events.TranslateLogicalName(url);

         /* QCR #754233 (KL: 01-Feb-2010)                                        */
         /* This is a temporary fix to load the image from the default resource. */
         /* We currently do not support loading images from satellite assemblies.*/
         /* Once we do so, this code will need some changes.                     */
         if (!url.StartsWith("@"))
            url = Events.GetLocalFileName(url, Form.getTask()); // load image through cache mechanism

         Commands.addAsync(CommandType.PROP_SET_IMAGE_LIST, this, getDisplayLine(false), url, Form.PBImagesNumber);
      }

      /// <summary>
      ///   Update boolean property(ex:visible\enable) of control and it's children
      /// </summary>
      /// <param name = "propId">property id </param>
      /// <param name = "commandType">command type that belong to the prop id</param>
      /// <param name = "val"></param>
      /// <param name = "updateThis"></param>
      public virtual void updatePropertyLogicNesting(int propId, CommandType commandType, bool val, bool updateThis)
      {
         if (val && haveToCheckParentValue())
            val = isParentPropValue(propId);
         if (updateThis)
            Commands.addAsync(commandType, this, getDisplayLine(false), val, !IsFirstRefreshOfProps());
         updateChildrenPropValue(propId, commandType, val);
      }


      internal void SetEnabled(bool val)
      {
         Commands.addAsync(CommandType.PROP_SET_ENABLE, this, getDisplayLine(false), val, !IsFirstRefreshOfProps());
      }
      /// <summary>
      ///   Updates visibility of controls linked to the control
      /// </summary>
      /// <param name = "propId"></param>
      /// <param name = "commandType"></param>
      /// <param name = "val"></param>
      public void updateChildrenPropValue(int propId, CommandType commandType, bool val)
      {
         if (isTableControl())
            // in table all children are contained,
            // table's visibility\enable will affect them
            return;

         for (int i = 0; i < _linkedControls.Count; i++)
         {
            MgControlBase child = _linkedControls[i];

            // Defect 117294. Do not continue for the first time. It affects on the placement.
            if (child.IsFirstRefreshOfProps())
               continue;
            bool childValue = child.GetComputedBooleanProperty(propId, true);

            childValue = childValue & val;
            bool childOnCurrentLayer = isChildOnCurrentLayer(child);
            if (commandType == CommandType.PROP_SET_VISIBLE)
            {
               if (childValue)
                  childValue = childOnCurrentLayer;
               Commands.addAsync(commandType, child, getDisplayLine(false), childValue);
               child.updateChildrenPropValue(propId, commandType, childValue);
            }
         }

         if (isSubform())
            updateSubformChildrenPropValue(propId, commandType, val);
      }

      /// <summary>
      ///   return true if this control is descendant of specified control
      /// </summary>
      /// <returns></returns>
      public bool isDescendentOfControl(MgControlBase control)
      {
         bool isContained = false;

         if (control != null)
         {
            Object parent = getParent();
            if (parent is MgControlBase)
            {
               if (parent == control)
                  isContained = true;
               else
               {
                  // If current control belongs to table, then it is also a descendant of the column control to
                  // which it belongs. According to our hierarchy, the control is direct descendant of table 
                  // control; hence, we need to handle column control
                  MgControlBase parentControl = parent as MgControlBase;
                  if (control.Type == MgControlType.CTRL_TYPE_COLUMN && parentControl != null && parentControl.Type == MgControlType.CTRL_TYPE_TABLE &&
                      control.Layer == this.Layer)
                     isContained = true;
                  else
                     isContained = ((MgControlBase)parent).isDescendentOfControl(control);
               }
            }
         }

         return isContained;
      }

      /// <summary>
      ///   Updates visibility or enable property of subform controls
      /// </summary>
      /// <param name = "propId">Id of visibility or enable property</param>
      /// <param name = "commandType">type of the GUI command</param>
      /// <param name = "val">value for the property</param>
      virtual protected void updateSubformChildrenPropValue(int propId, CommandType commandType, bool val)
      {
      }

      /// <summary>
      ///   checks child's layer is equal to current layer of the parent, relavant for tab control for all other
      ///   controls returns true
      /// </summary>
      /// <param name = "child"></param>
      /// <returns> true if parent is not tab control or parent is tab control with same layer</returns>
      internal bool isChildOnCurrentLayer(MgControlBase child)
      {
         bool ret = true;

         // check that the control has same layer that is currently set on it's parent tab control
         if (isChoiceControl())
         {
            int parentLayer = 0;
            if (Value != null)
               parentLayer = getCurrentLinkIdx() + 1;
            int childLayer = child.getLayer();
            if (parentLayer != childLayer && childLayer != 0)
               // sons of layer 0 displayed always
               ret = false;
         }
         return ret;
      }

      /// <summary>
      ///   Return current link Idx of choice controls, or zero for others
      /// </summary>
      /// <returns></returns>
      protected int getCurrentLinkIdx()
      {
         int currLinkIdx = 0;
         if (isChoiceControl())
         {
            int[] currentLayers = getCurrentIndexOfChoice();
            int[] currLinkIndice = getLinkIdxFromLayer(currentLayers);
            currLinkIdx = currLinkIndice[0];
         }
         return currLinkIdx;
      }

      /// <summary>
      /// gets layer from the selected indice passed.
      /// </summary>
      /// <param name="linkIdx"></param>
      /// <returns>array of layer values.</returns>
      protected int[] getLayerFromLinkIdx(int[] indice)
      {
         int[] layers = new int[indice.Length];

         for (int iCtr = 0; iCtr < indice.Length; iCtr++)
         {
            int idx = indice[iCtr];

            //By default, layer = linkIdx unless it is a Tab control 
            layers[iCtr] = idx;

            if (isTabControl() && idx >= 0)
            {
               int line = getDisplayLine(false);

               if (line < 0)
                  line = 0;

               String[] layerList = (String[])_choiceLayerList[line];

               if (layerList != null && layerList.Length > 0)
                  layers[iCtr] = Array.IndexOf(layerList, (idx + 1).ToString());
            }
         }

         return layers;
      }

      /// <summary>
      /// return the calculated visible layer list, to be used by the runtime designer
      /// </summary>
      /// <returns></returns>
      internal MgArrayList GetTabControlChoiceLayerList()
      {
         return _choiceLayerList;
      }

      /// <summary>
      /// Gets the selected indice for selected values.
      /// </summary>
      /// <param name="layers">an array of current layers tab controls. It can be array because of multiple selection.</param>
      /// <returns>array of indice</returns>
      private int[] getLinkIdxFromLayer(int[] layers)
      {
         int[] linkIndice = new int[layers.Length];

         //For each tab control in multiple selection
         for (int iCtr = 0; iCtr < layers.Length; iCtr++)
         {
            int layer = layers[iCtr];
            linkIndice[iCtr] = layer;

            if (isTabControl())
            {
               if (layer < 0)
                  linkIndice[iCtr] = 0;
               else
               {
                  String[] layerList = null;
                  int line = getDisplayLine(false);

                  if (line < 0)
                     line = 0;

                  layerList = (String[])_choiceLayerList[line];
                  if (layerList != null && layerList.Length > 0 && layer < layerList.Length)
                     linkIndice[iCtr] = (Int32.Parse(layerList[layer]) - 1);
               }
            }
         }

         return linkIndice;
      }

      /// <summary>
      ///   recursive method, checks if parent of the child is visible\enable, ultil there is no parent or found
      ///   parent that is hidden
      /// </summary>
      /// <param name = "propId"></param>
      /// <returns></returns>
      public bool isParentPropValue(int propId)
      {
         MgControlBase parent = null;
         MgControlBase column = Form.getControlColumn(this);

         parent = getLinkedParent(true);
         if (parent == null)
            // no more parents
            return true;
         else if (!parent.GetComputedBooleanProperty(propId, true))
            // parent is hidden
            return false;
         else if (!parent.isChildOnCurrentLayer(this))
            // parent is tab control with current layer
            // different from child's layer
            return false;
         //for table children check if column visible
         else if (column != null && !column.GetComputedBooleanProperty(propId, true))
            return false;
         else
            return parent.isParentPropValue(propId); // check grand parent
      }

      /// <summary>
      ///   Get control to which this control is linked
      /// </summary>
      /// <param name = "checkSubformFather">if true check for Subform control</param>
      /// <returns></returns>
      public MgControlBase getLinkedParent(bool checkSubformFather)
      {
         if (_linkedParentDitIdx != -1)
            return Form.getCtrl(_linkedParentDitIdx);
         if (checkSubformFather && Form.isSubForm())
            return Form.getSubFormCtrl();
         return null;
      }

      /// <summary>
      ///   sets the current line value of the readOnly history array
      /// </summary>
      /// <param name = "newVal">the new value to be set</param>
      protected internal void SetCurrReadOnly(bool newVal)
      {
         _currReadOnly[getDisplayLine(true)] = newVal;
      }

      /// <summary>
      ///   return the current readOnly value according to the control's current line number
      /// </summary>
      /// <returns> the readOnly value</returns>
      internal bool GetCurrReadOnly()
      {
         Boolean curr;

         int line = isTableControl() ? 0 : getDisplayLine(true);
         Object currObj = _currReadOnly[line];
         if (currObj == null)
         {
            curr = false;
            _currReadOnly[line] = curr;
         }
         else
            curr = (Boolean)currObj;

         return curr;
      }

      /// <summary>
      ///   returns true if this control is a Data Control
      /// </summary>
      public bool isDataCtrl()
      {
         return _dataCtrl;
      }

      /// <summary>
      ///   return the dcref according to the current line number
      /// </summary>
      /// <returns> the dcId</returns>
      public int getDcRef()
      {
         int line = getDcLineNum();
         Int32 dcId;
         Object dcIdObj = _dcTableRefs[getDcLineNum()];

         if (dcIdObj == null)
         {
            dcId = DcValues.EMPTY_DCREF;
            _dcTableRefs[line] = dcId;
         }
         else
            dcId = (Int32)dcIdObj;

         return dcId;
      }

      /// <summary>
      ///   returns the current line number for a data control
      /// </summary>
      private int getDcLineNum()
      {
         return IsRepeatable
                   ? Form.DisplayLine
                   : 0;
      }

      /// <summary>
      ///   check if the one choice option from items list is valid. The check is only for items list and not for the
      ///   data source options, (FYI: for the option from the DataCtrl, we are not calling this
      ///   method, NO CHECK IS DONE) in online :ci_check_range_str
      /// </summary>
      /// <param name = "option:">one option from the items list</param>
      /// <returns></returns>
      private bool optionIsValid(String option)
      {
         bool isValid = true;

         if (DataType == StorageAttribute.NUMERIC && option.Length > 0 && option.IndexOf('0') == -1)
         {
            var convertNum = new NUM_TYPE();
            convertNum.num_4_a_std(option);

            // If the conversion failed, then the number will be zeroed
            if (convertNum.num_is_zero())
               isValid = false;
         }
         return isValid;
      }

      /// <summary>
      ///   prepares and returns the ordered list based on layers in layerList
      /// </summary>
      /// <param name = "choiceDispList">unordered list</param>
      /// <returns></returns>
      private String[] getOrderedDispList(String[] choiceDispList, int line)
      {
         String[] result;
         String choiceLayerStr = "";
         var dispArryList = new List<String>();
         var layerArryList = new List<String>();
         Property layerProp;

         layerProp = getProp(PropInterface.PROP_TYPE_VISIBLE_LAYERS_LIST);

         if (layerProp != null)
            choiceLayerStr = layerProp.getValue();

         if (choiceLayerStr == null || StrUtil.rtrim(choiceLayerStr).Length == 0)
            choiceLayerStr = "";

         // DisplayList and LayerList must have some values.
         if (choiceLayerStr.Length > 0 && choiceDispList.Length > 0)
         {
            String[] layerList = choiceLayerStr.Split(',');
            String currLayerListVal = "";
            int posCopyIndex = -1;
            bool isNumber = false;

            // prepare the list based on layers in LayerList.
            for (int index = 0; index < layerList.Length; index++)
            {
               // Trim the currlayer.
               currLayerListVal = layerList[index].Trim();

               // Allow only first occurance, ignore all remaining.
               if (layerArryList.IndexOf(currLayerListVal) != -1)
                  continue;

               isNumber = IntUtil.TryParse(currLayerListVal, out posCopyIndex);

               // posCopyIndex is 1Based, so must not be zero. Also, it must not be
               // greater than no of elements in the DiplayList.
               if (isNumber && posCopyIndex > 0 && posCopyIndex <= choiceDispList.Length)
               {
                  dispArryList.Add(choiceDispList[posCopyIndex - 1].Trim());
                  layerArryList.Add(currLayerListVal);
               }
            }
         }

         // copy original lists if dispArryList empty
         if (dispArryList.Count == 0)
         {
            result = choiceDispList;
            _choiceLayerList[line] = new String[0];
         }
         else
         {
            // fill the arraylists
            result = new String[dispArryList.Count];
            dispArryList.CopyTo(result);

            _choiceLayerList[line] = new String[layerArryList.Count];
            layerArryList.CopyTo((String[])_choiceLayerList[line]);
         }

         return result;
      }

      /// <summary>
      ///   Create style from the properties on the form use by : 
      ///   PROP_TYPE_HEBREW, PROP_TYPE_FRAMESET_STYLE
      /// </summary>
      internal int properties2Style()
      {
         int style = 0;

         if (isTableControl())
         {
            if (getProp(PropInterface.PROP_TYPE_HEBREW).getValueBoolean())
               style |= Styles.PROP_STYLE_RIGHT_TO_LEFT;
         }

         if (isFrameSet())
         {
            var frameSetStyle = (FramesetStyle)getProp(PropInterface.PROP_TYPE_FRAMESET_STYLE).getValueInt();

            switch (frameSetStyle)
            {
               case FramesetStyle.Vertical:
                  style |= Styles.PROP_STYLE_VERTICAL;
                  break;
               case FramesetStyle.Horizontal:
                  style |= Styles.PROP_STYLE_HORIZONTAL;
                  break;
               case FramesetStyle.None:
                  Debug.Assert(false);
                  break;
            }
         }

         return style;
      }

      /// <summary>
      ///   reset the previous value
      /// </summary>
      public void resetPrevVal()
      {
         int line = getDisplayLine(true);

         if (line < 0)
            line = 0;
         _prevValues[line] = null;
         setPrevIsNull_ToNull();
      }

      /// <summary>
      /// reset _dcValId to the empty value
      /// </summary>
      public void ResetDcValueId()
      {
         _dcValId = DcValues.EMPTY_DCREF;
      }

      /// <summary>
      ///   sets the prevIsNull value of the prevIsNull array
      ///   the newValue can be NULL or bool
      /// </summary>
      protected internal void setPrevIsNull_ToNull()
      {
         _prevIsNulls[getDisplayLine(true)] = null;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns>true if an expression was set as the data</returns>
      public bool expressionSetAsData()
      {
         return _valExpId > 0;
      }

      /// <summary>
      ///   called whenever the choice list of a SELECT control has changed
      /// </summary>
      internal void clearRange(int line)
      {
         _orgChoiceDisps[line] = null;
         _choiceDisps[line] = null;
         _choiceLinks[line] = null;
         _choiceLayerList[line] = null;
      }

      /// <summary>
      ///   have to check parent visibility\enable
      /// </summary>
      /// <returns>true, if the control is connected to a container or the task is a subfom</returns>
      public bool haveToCheckParentValue()
      {
         bool ret = false;
         if (_linkedParentDitIdx != -1)
         {
            // controls that is not contained
            if (isContainedInLinkedParent() == false)
               ret = true;
            // contained tab children of all layers are on the same window,
            // so we must handle there visibility\enable
            else if (Form.getCtrl(_linkedParentDitIdx).isTabControl() || Form.getCtrl(_linkedParentDitIdx).isGroup())
               ret = true;
         }
         else if (Form.isSubForm())
            ret = true;

         return ret;
      }

      /// <summary>
      ///   returns true if control is contain in its linked parent
      /// </summary>
      /// <returns></returns>
      protected internal bool isContainedInLinkedParent()
      {
         return _containerDitIdx != -1 && _containerDitIdx == _linkedParentDitIdx;
      }

      /// <summary>
      ///   Combine two strings array into one. Each of the original arrays may be null.
      /// </summary>
      /// <param name = "firstStrings">the first strings array that will always occupy the first array cells</param>
      /// <param name = "lastStrings">the last strings array that will always occupy the last array cells</param>
      /// <returns> combined Strings array</returns>
      private static String[] combineStringArrays(String[] firstStrings, String[] lastStrings)
      {
         int firstLen = (firstStrings == null
                            ? 0
                            : firstStrings.Length);
         int lastLen = (lastStrings == null
                           ? 0
                           : lastStrings.Length);
         int length = firstLen + lastLen;
         var result = new String[length];

         if (firstLen > 0)
            Array.Copy(firstStrings, 0, result, 0, firstLen);
         if (lastLen > 0)
            Array.Copy(lastStrings, 0, result, firstLen, lastLen);
         return result;
      }

      /// <summary>
      ///   This method returns the control's context menu
      /// </summary>
      /// <param name = "createIfNotExist">This decided if Context menu is to be created or not.</param>
      /// <returns>matching mgMenu</returns>
      public MgMenu getContextMenu(bool createIfNotExist)
      {
         MgMenu mgMenu = null;

         int contextMenuNum = getContextMenuNumber();
         if (contextMenuNum > 0)
         {
            MgFormBase actualForm = Form;
            if (Form.isSubForm())
               actualForm = Form.getSubFormCtrl().getTopMostForm();
            mgMenu = Manager.GetMenu(getForm().getTask().ContextID,
                                     getForm().getTask().getCtlIdx(),
                                     contextMenuNum, MenuStyle.MENU_STYLE_CONTEXT, actualForm, createIfNotExist);
         }
         return mgMenu;
      }

      /// <summary>
      ///   If the control's property evaluates to zero, we should perform setMenu with the form's 
      ///   context menu.
      /// </summary>
      /// <returns> the form's context menu property if the passed propValue is zero</returns>
      internal int getContextMenuNumber()
      {
         Property context;
         if (isSubform() && GetSubformMgForm() != null)
         {
            MgFormBase subform = GetSubformMgForm();
            context = subform.GetComputedProperty(PropInterface.PROP_TYPE_CONTEXT_MENU);
         }
         else
            context = GetComputedProperty(PropInterface.PROP_TYPE_CONTEXT_MENU);

         int contextIdx = 0;
         MgControlBase parentControl = null;

         if (context != null)
         {
            bool result;
            if (Type != MgControlType.CTRL_TYPE_BUTTON && Type != MgControlType.CTRL_TYPE_RICH_TEXT)
               result = GetComputedBooleanProperty(PropInterface.PROP_TYPE_ENABLED, true) &&
                             GetComputedBooleanProperty(PropInterface.PROP_TYPE_ALLOW_PARKING, true);
            else
               result = GetComputedBooleanProperty(PropInterface.PROP_TYPE_ENABLED, true);
            if (result)
               contextIdx = context.GetComputedValueInteger();
         }
         if (contextIdx == 0)
         {
            parentControl = getImmediateParent();
            if (parentControl != null)
               contextIdx = parentControl.getContextMenuNumber();
            else
               contextIdx = Form.getContextMenuNumber();
         }
         return contextIdx;
      }

      /// <summary>
      ///   This method returns the control's immediate parent. Made specifically for cases in which
      ///   the parent is a column, in which case the control's linked parent will be the table
      /// </summary>
      /// <returns> the control's parent</returns>
      internal MgControlBase getImmediateParent()
      {
         MgControlBase parentColumn = null;
         MgControlBase parent = getLinkedParent(false);
         if (parent != null && parent.isTableControl() && !isColumnControl())
         {
            parentColumn = Form.getControlColumn(this);
            if (parentColumn != null)
               parent = parentColumn;
         }
         return parent;
      }

      /// <summary>
      /// return the boolean computed value of a property of the control
      /// </summary>
      /// <param name = "propId">the id of the property</param>
      /// <param name = "defaultRetVal">the default return value (when the property does not exist)</param>
      public bool GetComputedBooleanProperty(int propId, bool defaultRetVal)
      {
         bool result = defaultRetVal;
         Property prop = GetComputedProperty(propId);
         if (prop != null)
            result = prop.GetComputedValueBoolean();
         return (result);
      }

      /// <summary>
      /// return the boolean value of a property of the control - if the control is repeatable and the line is
      /// different than the forms current line then return the previous value of the control property at that line
      /// </summary>
      /// <param name = "propId">the id of the property</param>
      /// <param name = "defaultRetVal">the default return value (when the property does not exist)</param>
      /// <param name = "line">the line number in table in which the control is located</param>
      internal bool GetComputedBooleanProperty(int propId, bool defaultRetVal, int line)
      {
         bool result = defaultRetVal;

         if (isPropertyRepeatable(propId) && Form.DisplayLine != line)
         {
            Property prop = GetComputedProperty(propId);
            if (prop != null)
            {
               String val = prop.getPrevValue(line);
               if (val != null)
                  result = DisplayConvertor.toBoolean(val);
            }
         }
         else
            result = GetComputedBooleanProperty(propId, defaultRetVal);
         return result;
      }

      /// <summary>
      ///   return the boolean value of a property of the control
      /// </summary>
      /// <param name = "propId">the id of the property</param>
      /// <param name = "defaultRetVal">the default return value (when the property does not exist)</param>
      public bool checkProp(int propId, bool defaultRetVal)
      {
         bool result = defaultRetVal;
         Property prop = getProp(propId);

         if (prop != null)
            result = prop.getValueBoolean();

         return result;
      }

      /// <summary>
      ///   return the boolean value of a property of the control - if the control is repeatable and the line is
      ///   different than the forms current line then return the previous value of the control property at that
      ///   line
      /// </summary>
      /// <param name = "propId">the id of the property</param>
      /// <param name = "defaultRetVal">the default return value (when the property does not exist)</param>
      /// <param name = "line">the line number in table in which the control is located</param>
      public bool checkProp(int propId, bool defaultRetVal, int line)
      {
         bool result = defaultRetVal;

         if (isPropertyRepeatable(propId) && Form.DisplayLine != line)
         {
            Property prop = getProp(propId);
            if (prop != null)
            {
               String val = prop.getPrevValue(line);
               if (val != null)
                  result = DisplayConvertor.toBoolean(val);
            }
         }
         else
            result = checkProp(propId, defaultRetVal);
         return result;
      }

      /// <summary></summary>
      /// <returns></returns>
      internal bool getPrevIsNullsInArray()
      {
         return getPrevIsNull();
      }

      /// <summary>
      ///   returns true if propId property is repeatable for this control
      /// </summary>
      /// <param name = "propId"></param>
      /// <returns></returns>
      internal bool isPropertyRepeatable(int propId)
      {
         if (IsRepeatable)
            return true;
         if (isTreeControl() && Property.isRepeatableInTree(propId))
            return true;
         else if (isTableControl() && Property.isRepeatableInTable(propId))
            return true;
         return false;
      }

      /// <summary>
      /// </summary>
      /// <param name = "line"></param>
      /// <returns></returns>
      protected internal String getPrevValueInArray(int line)
      {
         return (String)_prevValues[line];
      }

      /// <summary> copy the value of one control to the other without modifying the prevValues</summary>
      /// <param name="fromControl">the source control</param>
      internal void copyValFrom(MgControlBase fromControl)
      {
         int line = getDisplayLine(true);
         String savedVal = Value;
         var savePrevValue = (String)_prevValues[line];
         bool savedIsNull = IsNull;
         bool savePrevIsNull = getPrevIsNull();

         String dispVal = Manager.GetCtrlVal(fromControl);
         String mgVal = fromControl.getMgValue(dispVal);

         resetPrevVal(); // force update of the display
         SetAndRefreshDisplayValue(mgVal, isNullValue(mgVal), true);

         setValueForEditSet(savedVal, savePrevValue, savedIsNull, savePrevIsNull);
      }

      /// <summary>
      ///   return the value of the control
      /// </summary>
      internal void setValueForEditSet(String savedValue, String savePrevValue,
                                       bool savedIsNull, bool savePrevIsNull)
      {
         // set value changes the value, we need to restore previous value so CTRL suffix will know that value is changed
         Value = savedValue;
         _prevValues[getDisplayLine(true)] = savePrevValue;

         IsNull = savedIsNull;
         setPrevIsNull(savePrevIsNull);
      }

      /// <summary>
      /// set the value of the control and update the display
      /// </summary>
      /// <param name = "mgVal">the value of the control in magic internal format to be displayed</param>
      /// <param name="isNull"></param>
      /// <param name="fromEditSet"></param>
      public void SetAndRefreshDisplayValue(String mgVal, Boolean isNull, bool fromEditSet)
      {
         bool dcRangeIsEmpty = true;

         //bool valueChanged = isDifferentValue(mgVal, isNull);

         //if (valueChanged)
         {
            if (mgVal == null)
            {
               Value = "";
               // QCR 308475 - controls that are Initialized with null should be given magic default value.
               if (isNull)
                  mgVal = FieldDef.getMagicDefaultNullDisplayValue(DataType) ?? FieldDef.getMagicDefaultValue(DataType);
               else
                  mgVal = FieldDef.getMagicDefaultValue(DataType);

               // If we still cannot get mgVal...this is a problem
               if (mgVal == null)
               {
                  Debug.Assert(false);
                  return;
               }
            }
            else if (isChoiceControl())
            {
               int line = getDisplayLine(true);

               if (line < 0)
                  line = 0;

               int[] layers = getLayerFromLinkIdx(getIndexOfChoice(mgVal, line, isNull));

               //We do not allow no selection for a tab control.
               //So, if there is no Tab to be selected, select the first Tab and 
               //also update the field's value accordingly.
               if (layers[0] < 0 && isTabControl())
               {
                  layers[0] = 0;

                  mgVal = getLinkValue(layers[0].ToString(), line);
                  isNull = false;

                  if (_field != null && mgVal != null)
                     ((TaskBase)_field.getTask()).UpdateFieldValueAndStartRecompute(_field, mgVal, isNull);
               }

               Value = Misc.GetCommaSeperatedString(layers);
               dcRangeIsEmpty = false;
            }

            // QCR #450248: an empty range in the data control caused deleting the value
            // of the control when leaving it. Now, the range of the control is used when
            // the data control range is empty.
            if (dcRangeIsEmpty)
            {
               if (DataType == StorageAttribute.DOTNET)
                  Value = mgVal;
               else if (!isCheckBox() && (_field == null || !isNull || !_field.hasNullDisplayValue() || fromEditSet))
               {
                  try
                  {
                     if ((isImageControl() && DataType == StorageAttribute.BLOB) || IsImageButton() || IsDotNetControl())
                        Value = mgVal;
                     else
                     {
                        if (_field == null && isNull && (DataType == StorageAttribute.NUMERIC || DataType == StorageAttribute.DATE || DataType == StorageAttribute.TIME || DataType == StorageAttribute.BOOLEAN))
                           Value = mgVal;
                        else
                        {
                           bool time_date_num_pic_Z_edt = InControl;
                           Value = DisplayConvertor.Instance.mg2disp(mgVal, _range, _pic, isSelectionCtrl(),
                                                                        getForm().getTask().getCompIdx(), time_date_num_pic_Z_edt);
                        }

                     }
                     // Defect # 138803 : Fix to provide backwards compatibility for uniPaaS. Do not call rtrimValue() when refreshing the display if the data type
                     // is not numeric or left justify property is No.
                     if (!_pic.isAttrNumeric() || _pic.isLeft())
                        rtrimValue();
                  }
                  catch (ApplicationException ex)
                  {
                     Events.WriteExceptionToLog(
                        new ApplicationException(string.Format("Control: '{0}', mgval: {1}", Name, mgVal), ex));
                     Value = "";
                     mgVal = FieldDef.getMagicDefaultValue(DataType);
                  }
               }
               else
               {
                  // truncate the "null display value" by the length of the mask
                  // blobs have pic the size == 0. no truncation.
                  if (isNull && !_pic.isAttrBlob() && mgVal.Length > _pic.getMaskLength())
                     mgVal = mgVal.Substring(0, _pic.getMaskLength());
                  Value = mgVal;
               }
            }

            //Translate logical name for image control.
            if ((isImageControl() && DataType != StorageAttribute.BLOB) || IsImageButton())
               mgVal = Events.TranslateLogicalName(mgVal);

            IsNull = isNull;
            RefreshDisplayValue(mgVal);
         }
      }

      /// <summary> Checks if the control's value is modified and if so, updates the ModifiedByUser </summary>
      /// <param name="newValue"></param>
      protected void UpdateModifiedByUser(String newValue)
      {
         // For image control and image button, the value would not be changed by the user. But, if it has
         // a logical name, it is replaced. Or if it is fileServer get, the value is replaced by a server url.
         // In both these cases, we should not mark it as modified.
         if (isButton() || isImageControl())
            return;

         bool ctrlCurrIsNull = IsNull;
         bool valChanged = isDifferentValue(newValue, false, false);
         // if the value didn't change then let's check if the nullity have changed
         if (!valChanged)
         {
            if (CanGetNullFromControlValue())
               valChanged = (ctrlCurrIsNull != isNullValue(newValue));
         }

         if (valChanged || KeyStrokeOn)
            ModifiedByUser = true;
      }

      /// <summary> Validates the new value.</summary>
      /// <param name="newValue">the value to be validated</param>
      /// <param name="vd">out: ValidationDetails</param>
      /// <returns>true, if the newValue is valid, else false</returns>
      public bool ValidateValue(String newValue, out ValidationDetails vd)
      {
         vd = null;

         string ctrlCurrValue = isRichEditControl()
                                   ? getRtfVal()
                                   : Value;

         try
         {
            vd = buildPicture(ctrlCurrValue, newValue);
         }
         catch (NullReferenceException e)
         {
            if (getPIC() == null)
               return true;
            throw e;
         }

         vd.evaluate();

         return (!vd.ValidationFailed);
      }

      /// <summary>
      ///   RTrims the control value
      /// </summary>
      private void rtrimValue()
      {
         if ((isImageControl() && DataType == StorageAttribute.BLOB) || IsImageButton() || IsDotNetControl())
            Value = Value.TrimEnd();
         else
         {
            int minimumValueLength = getMinimumValueLength();

            // RTrim the value beyond this index
            if (Value.Length > minimumValueLength)
            {
               String str = Value.Substring(minimumValueLength);

               // copy the minimum length value as is
               Value = Value.Substring(0, minimumValueLength);
               // rtrim the value beyond minimum length 
               Value = String.Concat(Value, StrUtil.rtrimWithNull(str, true));
            }

            // always trim the numeric value.
            if (isTextControl() && DataType == StorageAttribute.NUMERIC)
               Value = Value.TrimEnd();

         }
      }

      /// <summary>
      ///   returns the minimum value length
      /// </summary>
      private int getMinimumValueLength()
      {
         int minLength = _pic.getMaskLength();

         // traverse from right till a valid mask char is encountered
         while (minLength > 0 && !_pic.picIsMask(minLength - 1))
            minLength--;

         return minLength;
      }

      /// <summary>
      /// </summary>
      /// <returns></returns>
      internal bool isNullValue(String str)
      {
         bool retIsNullControl = false;
         if (CanGetNullFromControlValue() && str.Equals(""))
            retIsNullControl = true;
         return retIsNullControl;
      }

      /// <summary>
      ///   this control can get null value from the control himself
      /// </summary>
      /// <returns></returns>
      internal bool CanGetNullFromControlValue()
      {
         bool canGetNullFromControlValue = false;
         if (isCheckBox())
         {
            if (_field == null || _field.NullAllowed)
               canGetNullFromControlValue = checkProp(PropInterface.PROP_TYPE_THREE_STATES, false);
         }
         return canGetNullFromControlValue;
      }

      /// <summary>
      ///   returns true if a specific index corresponds to a null value.
      /// </summary>
      internal bool isChoiceNull(int idx)
      {
         if (idx >= 0)
         {
            DcValues dcv = getDcVals();
            return dcv.isNull(idx);
         }
         else
            return false;
      }

      /// <summary>
      ///   sets the property
      /// </summary>
      /// <param name = "propId"></param>
      /// <param name = "val"></param>
      public void setProp(int propId, String val)
      {
         if (_propTab == null)
            _propTab = new PropTable(this);

         _propTab.setProp(propId, val, this, GuiConstants.PARENT_TYPE_CONTROL);
      }

      /// <summary>
      ///   refresh the prompt of the control
      /// </summary>
      public void refreshPrompt()
      {
         EvaluatePromptHelp();

         if (!String.IsNullOrEmpty(this.PromptHelp))
            Manager.WriteToMessagePane(getForm().getTask(), this.PromptHelp, false);
      }

      /// <summary> Computes the control's value either from expression or field and refreshes the GUI </summary>
      /// <param name="forceRefresh"></param>
      public void ComputeAndRefreshDisplayValue(bool forceRefresh)
      {
         if (forceRefresh)
            resetPrevVal();

         if (_valExpId > 0)
         {
            bool wasEvaluated;
            String retExp = EvaluateExpression(_valExpId, DataType, _pic.getSize(), true, StorageAttribute.SKIP, false, out wasEvaluated);
            if (wasEvaluated)
               SetAndRefreshDisplayValue(retExp, retExp == null, false);
         }
         else if (_field != null)
         {
            String value = String.Empty;
            bool isNull = false;
            ((TaskBase)_field.getTask()).getFieldDisplayValue(_field, ref value, ref isNull);
            SetAndRefreshDisplayValue(value, isNull, false);
         }
         else if (!isTableControl() && !isColumnControl() && !isFrameSet())
            // there is no field or data expression attached to the control
            RefreshDisplayValue(Value);
      }

      /// <summary>
      /// Decides if the control should be refreshed while entering a control. Values for some data types
      /// are displayed differently while parked on the control.
      /// </summary>
      /// <returns></returns>
      public bool ShouldRefreshOnControlEnter()
      {
         return (Type == MgControlType.CTRL_TYPE_TEXT && getField() != null &&
                (getField().getType() == StorageAttribute.DATE ||
                 getField().getType() == StorageAttribute.TIME ||
                 getField().getType() == StorageAttribute.NUMERIC));
      }

      /// <summary>
      ///   refresh the displayed value only (without the other properties of the control)
      /// </summary>
      /// <param name = "mgVal">is the magic internal value of the presentation value</param>
      protected void RefreshDisplayValue(String mgVal)
      {
         int line = getDisplayLine(true);
         bool valueChanged = true;
         bool saveValueChanged = valueChanged;

         if (line < 0)
            line = 0;

         // ATTENTION:
         // the use of the function getName() instead of using the name member variable
         // is to enhance the use of controls which are contained in a table
         // update the display

         //When emptyDataview is on - the tree control has no rows - this is different from any other control
         //which always has at least one row
         if (isTreeControl() && getForm().getTask().DataView.isEmptyDataview())
            return;

         try
         {
            var prevValue = (String)_prevValues[line];
            bool prevNull = getPrevIsNull();

            if (StorageAttributeCheck.IsTypeAlphaOrUnicode(DataType))
            {
               if (StrUtil.rtrim(mgVal) == StrUtil.rtrim(prevValue) ||
                   mgVal != null && StrUtil.rtrim(mgVal).Equals(StrUtil.rtrim(prevValue)))
                  valueChanged = false;
               if (prevNull != IsNull)
                  valueChanged = true;
            }
            else
            {
               if (mgVal == prevValue || mgVal != null && mgVal.Equals(prevValue))
                  valueChanged = false;
               if (prevNull != IsNull)
                  valueChanged = true;
            }

            _prevValues[line] = mgVal;
            setPrevIsNull(IsNull);

            try
            {
               switch (Type)
               {
                  case MgControlType.CTRL_TYPE_DOTNET:
                     // if the source table prop is set then, when refreshing the Label prop
                     // the values in the link field and the display field are not available,
                     //  and hence call to refreshAndSetItemListByDataSource(...) for .Net choice ctrl.
                     if (IsDotNetChoiceControl())
                        valueChanged = refreshAndSetItemListByDataSource(line, valueChanged);
                     //Put gui command to refresh the display value.
                     if (valueChanged)
                        SetDNControlValue();
                     break;

                  case MgControlType.CTRL_TYPE_TABLE:
                  case MgControlType.CTRL_TYPE_COLUMN:
                  case MgControlType.CTRL_TYPE_LABEL:
                  case MgControlType.CTRL_TYPE_SUBFORM:
                  case MgControlType.CTRL_TYPE_GROUP:
                  case MgControlType.CTRL_TYPE_STATUS_BAR:
                  case MgControlType.CTRL_TYPE_FRAME_SET:
                  case MgControlType.CTRL_TYPE_CONTAINER:
                  case MgControlType.CTRL_TYPE_FRAME_FORM:
                  case MgControlType.CTRL_TYPE_SB_LABEL:
                  case MgControlType.CTRL_TYPE_LINE:
                     return;

                  case MgControlType.CTRL_TYPE_BROWSER:
                     if (valueChanged && Value != null)
                        setUrl();
                     return;

                  case MgControlType.CTRL_TYPE_IMAGE:
                  case MgControlType.CTRL_TYPE_SB_IMAGE:
                     if (valueChanged && Value != null)
                        setImage();
                     return;

                  case MgControlType.CTRL_TYPE_CHECKBOX:
                     if (!valueChanged)
                        return;
                     setCheckBoxValue(line, mgVal);
                     break;

                  case MgControlType.CTRL_TYPE_TAB:
                  case MgControlType.CTRL_TYPE_COMBO:
                  case MgControlType.CTRL_TYPE_LIST:
                     saveValueChanged = valueChanged;
                     valueChanged = refreshAndSetItemListByDataSource(line, valueChanged);
                     if (valueChanged)
                     {
                        //fixed bug#:776215, 800763,941841, the prevValue is save as mgval need to convert to layer
                        int[] prevDisplayValue = null;

                        //Fix bug#:784391
                        //If ItemList was update refreshAndSetItemListByDataSource then the value must be set 
                        if (saveValueChanged == valueChanged)
                        {
                           if (prevValue != null)
                           {
                              prevDisplayValue = getLayerFromLinkIdx(getIndexOfChoice(prevValue, line, prevNull));
                              // We may have a < 0 value in an array for a multi-selection listbox.
                              // Consider that list box is attached to the vector and is having items as "a,b,c,d,e"
                              // Now using VarSet, if we set 'a', 'g', 'e'. 
                              // Since we do not have 'g' in our list, the array will contain 0, -999999, 4
                              for (int i = 0; i < prevDisplayValue.Length; i++)
                              {
                                 if (prevDisplayValue[i] == GuiConstants.DEFAULT_VALUE_INT)
                                    prevDisplayValue[i] = GuiConstants.DEFAULT_LIST_VALUE;
                              }
                           }
                        }

                        GuiCommandQueue.getInstance().add(CommandType.PROP_SET_SELECTION, this, line, Value,
                                                          prevDisplayValue, InSetToDefaultValue);
                     }
                     break;

                  case MgControlType.CTRL_TYPE_TREE:
                     //if tree has no leaves, do not set value
                     if (valueChanged && Form.DisplayLine > 0)
                        setText();
                     break;

                  case MgControlType.CTRL_TYPE_BUTTON:
                  case MgControlType.CTRL_TYPE_TEXT:
                  case MgControlType.CTRL_TYPE_RICH_EDIT:
                  case MgControlType.CTRL_TYPE_RICH_TEXT:
                     if (valueChanged)
                     {
                        // if (form.formRefreshed() || forceRefresh || valExp != null &&
                        // valExp.computedByClientOnly())
                        if (IsImageButton())
                           setImageList(Value);
                        else
                           setText();

                        /* When leaving an Rich Edit control, we read its RTF text.    */
                        /* So, the value saved in MgControl should also be if in RTF.  */
                        /* Otherwise, we will get a difference even if the actual text */
                        /* is same.                                                    */
                        /* Hence, after setting a text on the Rich Edit, get the value */
                        /* from it.                                                    */
                        if (isRichEditControl())
                           setRtfval(Manager.GetCtrlVal(this));
                     }
                     else
                        return;
                     break;

                  case MgControlType.CTRL_TYPE_RADIO:
                     valueChanged = refreshAndSetItemListByDataSource(line, valueChanged);
                     if (!valueChanged)
                        return;
                     setRadioChecked(Int32.Parse(Value));
                     // setRadioChecked(value .length() == 0 ? DEFAULT_VALUE_INT : Integer.parseInt(value));
                     // if (!task.startProcess())
                     // guiManager.setFocus(this);
                     break;

                  default:
                     Events.WriteExceptionToLog(string.Format("in Control.RefreshDisplayValue() unknown type: {0}", Type));
                     return;
               }

               if (isChoiceControl() && valueChanged)
               {
                  //refresh visibility\enabled of mainCtrl and it's children
                  updatePropertyLogicNesting(PropInterface.PROP_TYPE_VISIBLE, CommandType.PROP_SET_VISIBLE,
                                                     checkProp(PropInterface.PROP_TYPE_VISIBLE, true), false);

               }
              
            }
            
            catch (Exception)
            {
               Events.WriteExceptionToLog(string.Format("in Control.RefreshDisplayValue() for control: {0}", Name));
            }
         }
         finally
         {
            if (valueChanged)
            {
               GetControlsData(line).ControlsValues[UniqueWebId] = Value;
            }
         }
      }

      public ControlsData GetControlsData(int line)
      {
         if (IsRepeatable)
         {
            MgFormBase.Row row = (MgFormBase.Row)getForm().Rows[line];
            return row.ControlsData;
         }
         else
            return getForm().ScreenControlsData;
      }

      public string UniqueWebId
      {
         get
         {
            if (String.IsNullOrEmpty(Name))
               return _controlIsn.ToString();
            else
               return Name;
               }
      }

      /// <summary></summary>
      public void RefreshDisplay()
      {
         RefreshDisplay(false);
      }

      /// <summary>
      ///   updates the display of the control including its value and its properties
      /// </summary>
      /// <param name = "onlyRepeatableProps">if control tree - refreshes only repeatable tree properties</param>
      public void RefreshDisplay(bool onlyRepeatableProps)
      {
         //#422250. Skip the refresh display of the parent's subform control if the form inside it is still in its RefreshDisplay.
         if (this.isSubform() && this.GetSubformMgForm() != null && this.GetSubformMgForm().inRefreshDisplay())
            return;

         // update the properties
         refreshProperties(onlyRepeatableProps);

         // compute and display the value. 
         if (ShouldComputeAndRefreshOnClosedForm())
         {
#if !PocketPC
            if (IsDataViewControl())
               getForm().DVControlManager.RefreshDisplay();
            else
#endif
               ComputeAndRefreshDisplayValue(false);
         }

         // executes the readonly command only if needed
         bool isParkableCtrl = isParkable(false, false);
         bool isReadOnly = (!isParkableCtrl || !isModifiable());
         if (GetCurrReadOnly() != isReadOnly)
         {
            SetCurrReadOnly(isReadOnly);
            Manager.SetReadOnlyControl(this, isReadOnly);
         }
      }

      /// <summary>
      /// For Rich Client compute and refresh the value of the control always, also if the form is not yet opened.
      /// For Online only for opened forms.
      /// </summary>
      /// <returns></returns>
      public virtual bool ShouldComputeAndRefreshOnClosedForm()
      {
         return true;
      }

      /// <summary> Refreshes the properties of the control. </summary>
      /// <param name="onlyRepeatableProps"></param>
      public void refreshProperties(bool onlyRepeatableProps)
      {
         if (_propTab != null)
         {
            try
            {
               if (_firstRefreshProperties)
                  createDefaultProps();

               RefreshPriorityProperties();

               // this function must call before the first refresh of the properties
               bool allPropsRefreshed = _propTab.RefreshDisplay(false, onlyRepeatableProps);
               if (!allPropsRefreshed)
                  Events.WriteExceptionToLog(string.Format("Control '{0}': Not all properties could be set", getName()));
               if (_firstRefreshProperties)
               {
                  // If the RTF text is set before setting the font, the RTF text gets effected.
                  // So, the text should be refreshed once font is being set.
                  // Further, we have problem if the TEXT is refreshed before WIDTH and HEIGHT.
                  // This sis because, if the text cannot be fitted into the then client area, scroll bars are added 
                  // to the RichTextBox. So, we skip the refresh of the TEXT property in Property.RefreshDisplay() and 
                  // then after refreshing all props, we refresh TEXT here.
                  if (isRichText())
                  {
                     Property prop = getProp(PropInterface.PROP_TYPE_TEXT);
                     prop.RefreshDisplay(true);
                  }

                  if (isContainer() || isFrameSet())
                     createLayout();

                  if (!isTreeControl() || !onlyRepeatableProps)
                     _firstRefreshProperties = false;
               }
            }
            catch (Exception ex)
            {
               var msg = new StringBuilder(string.Format("Control '{0}': {1}", getName(), ex.Message));
#if PocketPC
               msg.Append(OSEnvironment.EolSeq);
               msg.Append(OSEnvironment.EolSeq);
               msg.Append(ex.StackTrace);
#endif
               Events.WriteExceptionToLog(msg.ToString());
            }
         }
      }

      /// <summary>
      /// Set properties that should be before some properties
      /// </summary>
      private void RefreshPriorityProperties()
      {
         //In case of combobox, style should be set before height
         if (isComboBox())
         {
            Property prop = getProp(PropInterface.PROP_TYPE_STYLE_3D);
            prop.RefreshDisplay(true);
         }
         //In case of table control, multi column display should be set before row height
         if (isTableControl())
         {
            Property prop = getProp(PropInterface.PROP_TYPE_MULTI_COLUMN_DISPLAY);
            prop.RefreshDisplay(true);
         }
      }

      /// <summary>
      /// </summary>
      internal void refreshTabForLayerList(int line)
      {
         String currentLinkValue = null;
         int currentLayer = -1;

         // If the form has already opened, get the current link value before 
         // changing the layer list i.e. before calling refreshAndSetItemsList().
         // And then, after changing the layer list, select the TAB corresponding 
         // to the current link value.
         if (getForm().Opened)
         {
            int[] indice = getCurrentIndexOfChoice();
            currentLayer = indice[0];
            currentLinkValue = getLinkValue(currentLayer.ToString(), line);
         }

         refreshAndSetItemsList(line, false);

         if (getForm().Opened)
            SetAndRefreshDisplayValue(currentLinkValue, IsNull, false);
      }

      /// <summary>
      ///   refresh the item list of radio button
      /// </summary>
      /// <param name="line">Display line</param>
      /// <param name="execComputeChoice"></param>
      protected virtual void refreshAndSetItemsListForRadioButton(int line, bool execComputeChoice)
      {
         // 1. create the button for each items list
         String[] displayVals = refreshDispRange(execComputeChoice);

         // creates new radio buttons from display list.
         Commands.addAsync(CommandType.PROP_SET_ITEMS_LIST, this, line, displayVals, InSetToDefaultValue);

         // Optimization : We need to set properties only for screen mode program.
         // For a line mode program, properties are set by LgRadioContainer.
         if (!IsRepeatable)
         {
            // 3. after add the new radio buttons controls, need to refresh some property on them.
            // each time we remove the controls we must refresh the all properties that effect the buttons.
            // we get to this method also onLable() \ onDisplayList() \ onVisibleLayerList()            
            if (getProp(PropInterface.PROP_TYPE_IMAGE_FILENAME) != null)
               getProp(PropInterface.PROP_TYPE_IMAGE_FILENAME).RefreshDisplay(true);

            // 3. after add the radio button need to set the layout so the controls will be resize automatically
            Commands.addAsync(CommandType.RESUME_LAYOUT, this, line, false);
            Commands.addAsync(CommandType.EXECUTE_LAYOUT, this, line, false); // we need this in case we have a image for a radio.
         }
      }

      /// <summary>
      ///   refresh the item list and set it into the gui control if it is combo box , refresh also the visible
      ///   lines
      /// </summary>
      internal void refreshAndSetItemsList(int line, bool execComputeChoice)
      {
         if (execComputeChoice)
            clearRange(line);
         String[] optionsStrings = refreshDispRange(execComputeChoice);

         if (isSelectionCtrl() || isTabControl())
            Commands.addAsync(CommandType.PROP_SET_ITEMS_LIST, this, line, optionsStrings, InSetToDefaultValue);
         else if (isRadio())
            refreshAndSetItemsListForRadioButton(line, execComputeChoice);
         else if (IsDotNetChoiceControl())
            SetDNControlDataSource(line, execComputeChoice);

         if (Type == MgControlType.CTRL_TYPE_COMBO)
            getProp(PropInterface.PROP_TYPE_VISIBLE_LINES).RefreshDisplay(true);
         else if (isTabControl())
         {
            //TODO: this issue will be handel by Rina &Rinat in recompute
            //// whenever the ItemList/DisplayList changes, refresh the field dispaly to set the correct selection.
            //Field fld = this.getField();
            //if (fld != null)
            //   fld.updateDisplay();
            getProp(PropInterface.PROP_TYPE_IMAGE_LIST_INDEXES).RefreshDisplay(true);
         }
      }

      /// <summary>
      /// This function sets the the "DataSource" property of dot net control.
      /// </summary>
      /// <param name="line"></param>
      /// <param name="execComputeChoice"></param>
      private void SetDNControlDataSource(int line, bool execComputeChoice)
      {
#if !PocketPC

         String propName = String.Empty;
         PropertyInfo propInfo = null;

         //get the display list and item list.
         string[] dispList = null;
         string[] itmList = null;

         dispList = refreshDispRange(execComputeChoice);
         itmList = refreshItmRange(false);

         // if the lists haven't changed then don't create a source table again.
         if (StrUtil.StringsArraysEqual(prevValueList, itmList)
            && StrUtil.StringsArraysEqual(prevDispList, dispList))
            return;

         // create and populate the DataTable
         DNChoiceControlDataTable sourceTable = new DNChoiceControlDataTable();
         sourceTable.AddColumns(GetDNChoiceCtrlDataType(), itmList);
         sourceTable.AddRows(GetDNChoiceCtrlDataType(), itmList, dispList);

         // The Value changed event handlers are removed since the value changes event is fired
         // when the .Net control props in DataSource Property name, DisplayMember property name &
         // ValueMember property are bound to the DataTable.

         if (_field != null)
            Commands.RemoveDNControlValueChangedHandler(this);

         //Get and set "DataSource" property.
         propName = getProp(PropInterface.PROP_TYPE_DN_CONTROL_DATA_SOURCE_PROPERTY).getValue();
         propInfo = DNType.GetProperty(propName);
         ReflectionServices.SetPropertyValue(propInfo, this, new object[] { }, sourceTable.DataTblObj);

         propName = getProp(PropInterface.PROP_TYPE_DN_CONTROL_VALUE_MEMBER_PROPERTY).getValue();
         propInfo = DNType.GetProperty(propName);
         ReflectionServices.SetPropertyValue(propInfo, this, new object[] { }, GuiConstants.STR_VALUE_MEMBER);

         propName = getProp(PropInterface.PROP_TYPE_DN_CONTROL_DISPLAY_MEMBER_PROPERTY).getValue();
         propInfo = DNType.GetProperty(propName);
         ReflectionServices.SetPropertyValue(propInfo, this, new object[] { }, GuiConstants.STR_DISPLAY_MEMBER);

         if (_field != null)
            Commands.AddDNControlValueChangedHandler(this);

         // save the display and value lists.
         prevValueList = itmList;
         prevDispList = dispList;
#endif
      }

      /// <summary>
      ///   refresh the choice list of a SELECT control
      /// </summary>
      internal String[] refreshDispRange(bool execComputeChoice)
      {
         var selectCmd = new String[0];

         int line = getDisplayLine(true);

         if (line < 0)
            line = 0;

         if (SupportsDataSource())
         {
            DataViewBase dataview = getForm().getTask().DataView;
            DcValues dcv = dataview.getDcValues(_dcValId);
            String[] dispVal = getDispVals(line, execComputeChoice);
            if (_hasValidItmAndDispVal)
               selectCmd = combineStringArrays(dispVal, dcv.getDispVals());
         }
         else
         {
            //for control that has no Data source, need to take only the display value
            selectCmd = getDispVals(line, execComputeChoice);
         }

         _rangeChanged = true;
         selectCmd = getOrderedDispList(selectCmd, line);

         return selectCmd;
      }

      /// <summary>
      /// refresh the items list of the selection control.
      /// </summary>
      /// <param name="execComputeChoice"></param>
      /// <returns></returns>
      internal string[] refreshItmRange(bool execComputeChoice)
      {
         // This function should be called only for selection control.
         Debug.Assert(SupportsDataSource());

         var orderedItmList = new String[0];
         int line = getDisplayLine(false);

         if (line < 0)
            line = 0;

         if (execComputeChoice)
            computeChoice(line);

         String[] linkVals = (String[])_choiceLinks[line];

         DataViewBase dataview = getForm().getTask().DataView;
         DcValues dcv = dataview.getDcValues(_dcValId);

         if (_hasValidItmAndDispVal)
            orderedItmList = combineStringArrays(linkVals, dcv.GetLinkVals());

         orderedItmList = getOrderedDispList(orderedItmList, line);

         return orderedItmList;
      }

      /// <summary>
      /// Get the items values from data source & item list property.
      /// </summary>
      /// <returns></returns>
      public string[] GetItemsRange()
      {
         return refreshItmRange(false);
      }
      
      /// <summary>
      /// Get the display values from data source & display list property.
      /// </summary>
      /// <returns></returns>
      public string[] GetDisplayRange()
      {
         return refreshDispRange(false);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      private void createDefaultProps()
      {
         getProp(PropInterface.PROP_TYPE_VISIBLE);
         // Fixed bug#:781741 
         // in 1.9 -->
         // when ask about any property(PROP_TYPE_ENABLED) we was created the default property with the default value(true).
         // and when get to RefreshProps of the property "PROP_TYPE_ENABLED" we was add command to the queue in gui thread 
         // the command was was update the TagData.Enable to be "true"
         // in 2.0 -->
         // we useing a new method GetComputedBooleanProperty() that not created any default property and no get to refreshProp
         // on the property and the value on the Control.Enable was wrong 
         getProp(PropInterface.PROP_TYPE_ENABLED);

         //fixed bug#:988383 when color = 0 for image Button we need to to SystemColor.Control
         if (IsImageButton())
            getProp(PropInterface.PROP_TYPE_COLOR);

         if (isTreeControl())
            checkAndSetTreeDefaultImageFile();

         if (isRichText())
         {
            setProp(PropInterface.PROP_TYPE_ALLOW_PARKING, "0");
            setProp(PropInterface.PROP_TYPE_MODIFIABLE, "0");
         }

         if (isTableControl())
            getProp(PropInterface.PROP_TYPE_SCROLL_BAR);

         if (isTextControl())
            getProp(PropInterface.PROP_TYPE_FOCUS_COLOR);

      }

      /// <summary>
      /// Check if there is a tree image file defined.
      /// if not, assign magic's default tree image file when tree node has any image definition.
      /// </summary>
      private void checkAndSetTreeDefaultImageFile()
      {
         // When tree has no image list file assigned , we will use magic's default treeimages.
         // This is only if at least one of the 4 possible images status on the node has a value.
         // If file is assigned and there are no images definitions, we will see blank images beside the nodes.
         if (!_defaultTreeImageInitialized && getProp(PropInterface.PROP_TYPE_IMAGE_FILENAME) == null && treeNodeHasAnyImageDefinition())
            setProp(PropInterface.PROP_TYPE_IMAGE_FILENAME, "@treeimages");
         _defaultTreeImageInitialized = true;
      }

      /// <summary>
      /// Check each of the tree node possible images. expanded, collapse, selected expanded and selected collapse.
      /// If one of them has a value , return true.
      /// </summary>
      /// <returns></returns>
      private bool treeNodeHasAnyImageDefinition()
      {
         return (treeNodeImageDefForTypeExists(PropInterface.PROP_TYPE_EXPANDED_IMAGEIDX) ||
         treeNodeImageDefForTypeExists(PropInterface.PROP_TYPE_COLLAPSED_IMAGEIDX) ||
         treeNodeImageDefForTypeExists(PropInterface.PROP_TYPE_PARKED_IMAGEIDX) ||
         treeNodeImageDefForTypeExists(PropInterface.PROP_TYPE_PARKED_COLLAPSED_IMAGEIDX));
      }

      /// <summary>
      /// Check a single tree node image property.
      /// return true if it has a value.
      /// </summary>
      /// <param name="imagePropType"></param>
      /// <returns></returns>
      private bool treeNodeImageDefForTypeExists(int imagePropType)
      {
         bool imageDefinitionFound = false;

         Property imageProp;
         imageProp = getProp(imagePropType);
         // if the prop is not null there is still a way that the there is no image definition.
         // that is when there is no expression and the value of the property is 0.
         // * getValueInt will nt compute an expression because we call it when the expression is 0.
         if (imageProp != null && (imageProp.isExpression() || imageProp.getValueInt() != 0))
            imageDefinitionFound = true;

         return imageDefinitionFound;
      }

      /// <summary>
      ///   creates layout for container controls
      /// </summary>
      internal void createLayout()
      {
         MgRectangle rect = null;
         int? runtimeDesignerXDiff = null, runtimeDesignerYDiff = null;
         if (isFrameSet())
            Commands.addAsync(CommandType.RESUME_LAYOUT, this);
         else
         {
            if (isContainerControl())
            {
               MgControlBase frameForm = getLinkedParent(false);
               Debug.Assert(frameForm.isFrameFormControl());
               var autoFit = (AutoFit)frameForm.getProp(PropInterface.PROP_TYPE_AUTO_FIT).getValueInt();
               if (autoFit == AutoFit.None)
                  // if we replace frame with AUTO_FIT_NONE we do placement
                  // relatively to frame size
                  // if regular case it will just prevent placement
                  rect = frameForm.getRect();
               else
                  rect = Property.getOrgRect(this);
            }
            else
            {
               rect = Property.getOrgRect(this);
            }

            bool containsWidth, containsHeight;
            RuntimeDesignerContainsSize(out containsWidth, out containsHeight);
            //this is to prevent additional placement due to runtime designer change
            if (containsWidth)
               runtimeDesignerXDiff = 0;
            if (containsHeight)
               runtimeDesignerYDiff = 0; 

            // get coordinates that were defined on the controls originally,
            // i.e. before expression executed
            Commands.addAsync(CommandType.CREATE_PLACEMENT_LAYOUT, this, 0, rect.x, rect.y, rect.width, rect.height,
                              false, false, runtimeDesignerXDiff, runtimeDesignerYDiff);
         }
      }


      /// <summary>
      /// check if width/height are in defined by runtime Designer
      /// </summary>
      /// <param name="containsWidth"></param>
      /// <param name="containsHeight"></param>
      internal void RuntimeDesignerContainsSize(out bool containsWidth, out bool containsHeight)
      {
         containsWidth = false;
         containsHeight = false;
         Dictionary<MgControlBase, Dictionary<string, object>> parentDesignerInfoDictionary = getForm().DesignerInfoDictionary;
         if (parentDesignerInfoDictionary != null && parentDesignerInfoDictionary.ContainsKey(this))
         {
            Dictionary<string, object> d = parentDesignerInfoDictionary[this];
            containsWidth = d.ContainsKey(Constants.WinPropWidth);
            containsHeight = d.ContainsKey(Constants.WinPropHeight);
         }
      }
      /// <summary>
      ///   set Linked Parent Idx
      /// </summary>
      /// <param name="linkedParentDitIdx"></param>
      private void setLinkedParentIdx(int linkedParentDitIdx)
      {
         _linkedParentDitIdx = linkedParentDitIdx;
      }

      internal FieldDef getNodeParentIdField()
      {
         return _nodeParentIdField;
      }

      /// <summary>
      ///   get the validation details of the control
      /// </summary>
      /// <param name="oldVal"></param>
      /// <param name="val"></param>
      /// <returns></returns>
      public ValidationDetails buildPicture(String oldVal, String val)
      {
         if (isTableControl() || isColumnControl())
            return null;

         if (_picExpExists)
            _vd = null;

         if (_vd == null)
            _vd = new ValidationDetails(oldVal, val, _range, _pic, this);
         else
         {
            _vd.setValue(val);
            _vd.setOldValue(oldVal);
         }
         return _vd;
      }

      /// <summary>
      ///   Checks if the sent value is different than the control's existing value.
      /// </summary>
      /// <param name = "NewValue"></param>
      /// <param name="isNull"></param>
      /// <param name="checkNullValue"></param>
      /// <returns></returns>
      public bool isDifferentValue(String NewValue, Boolean isNull, bool checkNullValue)
      {
#if !PocketPC
         String ctrlCurrValue = (isRichEditControl()
                                    ? Commands.GetRtfValueBeforeEnteringControl(this, getDisplayLine(false))
                                    : Value);
#else
         String ctrlCurrValue = Value;
#endif

         bool ctrlCurrIsNull = IsNull;
         bool valChanged = !StrUtil.rtrim(NewValue).Equals(StrUtil.rtrim(ctrlCurrValue));

         if (!valChanged && checkNullValue)
            valChanged = isNull != ctrlCurrIsNull;

         return valChanged;
      }

      /// <summary>
      ///   returns true if column's sortable property is set to true
      /// </summary>
      /// <returns></returns>
      public bool isColumnSortable()
      {
         bool isSortable = false;

         if (Type == MgControlType.CTRL_TYPE_COLUMN && _propTab.propExists(PropInterface.PROP_TYPE_SORT_COLUMN))
            isSortable = (_propTab.getPropById(PropInterface.PROP_TYPE_SORT_COLUMN).getValueBoolean());

         return isSortable;
      }

      /// <summary>
      ///   returns first child MgControl of column i.e. field attached to column
      /// </summary>
      /// <returns></returns>
      public MgControlBase getColumnChildControl()
      {
         MgControlBase childControl = null;

         if (Type != MgControlType.CTRL_TYPE_COLUMN)
            return null;

         List<MgControlBase> linkedControls = _parentTable.getLinkedControls();

         foreach (MgControlBase control in linkedControls)
         {
            if (control.getLayer() == getLayer() && control.Type != MgControlType.CTRL_TYPE_COLUMN && !control.IsTableHeaderChild)
            {
               childControl = control;
               break;
            }
         }

         return childControl;
      }

      /// <summary>
      ///   Get control name for handler search propose.
      ///   The control itself, may not have a name for handler search (like column control, in which we need to use
      ///   the child control name for handlers search)
      /// </summary>
      /// <returns></returns>
      public String getControlNameForHandlerSearch()
      {
         MgControlBase childControl = null;

         childControl = getColumnChildControl();

         if (childControl != null)
            return childControl.Name;
         else
            return Name;
      }

      /// <summary>
      ///   set the sibling vec on the control
      /// </summary>
      /// <param name = "siblingVec"></param>
      internal void setSiblingVec(List<MgControlBase> siblingVec)
      {
         _siblingVec = siblingVec;
      }

      /// <summary>
      ///   add a control to the list of linked controls
      /// </summary>
      /// <returns></returns>
      internal void linkCtrl(MgControlBase ctrl)
      {
         _linkedControls.Add(ctrl);
      }

      /// <summary>
      ///   get control's rectangle
      /// </summary>
      /// <returns></returns>
      internal MgRectangle getRect()
      {
         var rect = new MgRectangle();
         rect.x = Int32.Parse(getProp(PropInterface.PROP_TYPE_LEFT).getValue());
         rect.y = Int32.Parse(getProp(PropInterface.PROP_TYPE_TOP).getValue());
         rect.width = Int32.Parse(getProp(PropInterface.PROP_TYPE_WIDTH).getValue());
         rect.height = Int32.Parse(getProp(PropInterface.PROP_TYPE_HEIGHT).getValue());
         rect = Form.uom2pixRect(rect);
         return rect;
      }

      /// <summary>
      /// Update IsRepeatable for controls on table header. It should be set to false
      /// </summary>
      internal void HeaderControlUpdateRepeatable()
      {
         if (!isColumnControl() && _parentTable != null && Layer > 0)
         {
            int controlY = Int32.Parse(getProp(PropInterface.PROP_TYPE_TOP).getOrgValue());
            int tableTitleY = Int32.Parse(_parentTable.getProp(PropInterface.PROP_TYPE_TOP).getOrgValue());
            int titleHeight = Int32.Parse(_parentTable.getProp(PropInterface.PROP_TYPE_TITLE_HEIGHT).getOrgValue());

            if (isLineControl())
            {
               int y2 = Int32.Parse(getProp(PropInterface.PROP_TYPE_HEIGHT).getOrgValue());
               controlY = Math.Min(controlY, y2);
            }

            IsRepeatable = !((controlY - tableTitleY) < titleHeight);
         }
      }

      /// <summary>
      ///   Creates the physical control defined by this object on the window
      /// </summary>
      internal void createControl(bool forceWindowControl)
      {
         if (!IsRepeatable && !IsTableHeaderChild)
         {
            Type dnType = null;
            int dnObjKey = 0;
            List<String> dnEventsNames = null;
            List<GuiMgControl> tableChildrenList = null;
            int columnCount = 0;
            int assemblyId = 0;
            bool isFrameSet = false;
            TableBehaviour tableBehaviour = TableBehaviour.UnlimitedItems;

            switch (Type)
            {
               case MgControlType.CTRL_TYPE_TABLE:
                  tableChildrenList = Form.getGuiTableChildren();
                  columnCount = Form.getColumnsCount();
                  tableBehaviour = GetTableBehaviour();
                  break;

               case MgControlType.CTRL_TYPE_SUBFORM:
                  if (GetSubformMgForm() != null && GetSubformMgForm().IsFrameSet)
                     isFrameSet = true;
                  break;

               case MgControlType.CTRL_TYPE_DOTNET:
                  getForm().getTask().GetDNControlEventsDetails(_ditIdx, out dnObjKey, out dnEventsNames);
                  dnType = DNType;
                  break;
            }

            Commands.addAsync(getCreateCommandType(), getParent(), this, 0, properties2Style(), dnEventsNames,
                              tableChildrenList, columnCount, isFrameSet, forceWindowControl,
                              assemblyId, dnType, dnObjKey, tableBehaviour);

            if (Type == MgControlType.CTRL_TYPE_DOTNET)
            {
               getForm().getTask().OnDNControlCreate(_ditIdx);
#if !PocketPC
               if (IsDataViewControl())
                  OnDVControlCreate();
#endif
            }

            if (isColumnControl())
            {
               setColumnOrgWidth();
               Form.setColumnOrgPos(getLayer() - 1);
            }
            else if (isTableControl())
            {
               SetTableOrgRowHeight();
               // RowPlacement needs to be sent only once. It is not recomputed.
               SetRowPlacement();
            }
         }
      }

      /// <summary> Set the row placement of the table </summary>
      private void SetRowPlacement()
      {
         Debug.Assert(this.isTableControl());

         bool hasRowPlacement = getProp(PropInterface.PROP_TYPE_ROW_PLACEMENT).getValueBoolean();

         if (hasRowPlacement)
            Commands.addAsync(CommandType.PROP_SET_ROW_PLACEMENT, this, 0, hasRowPlacement);
      }

      /// <summary> sends table's original row height to gui low </summary>
      private void SetTableOrgRowHeight()
      {
         Debug.Assert(this.isTableControl());

         // get coordinates that were defined on the controls originally
         int rowHeight = Int32.Parse(getProp(PropInterface.PROP_TYPE_ROW_HEIGHT).getOrgValue());
         rowHeight = Form.uom2pix(rowHeight, false);
         Commands.addAsync(CommandType.SET_TABLE_ORG_ROW_HEIGHT, this, 0, rowHeight);
      }

      /// <summary>
      ///   sends original width to GUI thred
      /// </summary>
      private void setColumnOrgWidth()
      {
         // get coordinates that were defined on the controls originally,
         // i.e. before expression executed
         int width = Int32.Parse(getProp(PropInterface.PROP_TYPE_WIDTH).getOrgValue());
         width = Form.uom2pix(width, true);
         Commands.addAsync(CommandType.SET_COLUMN_ORG_WIDTH, this, 0, width);
      }

      public String getRtfVal()
      {
         return _rtfVal;
      }

      /// <summary>
      ///   removes the reference to this control from the field
      /// </summary>
      public void removeRefFromField()
      {
         if (_field != null)
            _field.RemoveControl(this);
      }

      /// <summary>
      ///   return original column width
      /// </summary>
      /// <returns></returns>
      internal int getOrgWidth()
      {
         return Int32.Parse(getProp(PropInterface.PROP_TYPE_WIDTH).getOrgValue());
      }

      /// <summary>
      /// </summary>
      /// <returns>true if the current control is wide control</returns>
      public bool isWideControl()
      {
         return IsWideControl;
      }

      /// <summary>
      ///   returns the dit index of the control
      /// </summary>
      public int getDitIdx()
      {
         return _ditIdx;
      }

      /// <summary>
      ///   Returns true if the control allows wide mode. Wide mode is allowed for alpha, memo and unicode Text
      ///   controls that are not password or multi line
      /// </summary>
      /// <returns>boolean</returns>
      public bool IsWideAllowed()
      {
         bool allow = false;

         if (isTextOrTreeEdit())
         {
            switch (DataType)
            {
               case StorageAttribute.ALPHA:
               case StorageAttribute.MEMO:
               case StorageAttribute.UNICODE:
                  if ((!_propTab.propExists(PropInterface.PROP_TYPE_PASSWORD) || !checkProp(PropInterface.PROP_TYPE_PASSWORD, false)) && !isMultiline() && (_pic.getSize() == _pic.getMaskSize()))
                     allow = true;
                  break;
            }
         }
         return allow;
      }

      /// <summary>
      ///   open\close the wide
      /// </summary>
      public bool onWide(bool forceClose)
      {
         bool wideToggled = true;
         MgFormBase topMostForm = getTopMostForm();

         if (forceClose)
         {
            if (topMostForm.wideIsOpen())
               topMostForm.closeWide();
            else
               wideToggled = false;
         }
         else
         {
            if (topMostForm.wideIsOpen())
               topMostForm.closeWide();
            else
               topMostForm.openWide(this);
         }

         return wideToggled;
      }

      /// <summary>
      /// </summary>
      /// <param name = "parentForm">the form of the control</param>
      /// <param name = "parentControl">the control, that press wide on it</param>
      /// <param name = "rectWide"></param>
      protected internal void setWideProperties(MgFormBase parentForm, MgControlBase parentControl, MgRectangle rectWide)
      {
         IsWideControl = true;

         //1. update control data
         Form = parentForm;

         //2. update the field of the cont
         // set the filed of the control to the wide control
         //set the controlToFocus of the field to be the wide control
         setField(parentControl.getField());
         setControlToFocus();

         //remove the parent wide control from the refFiled , so when update control. it will update\refresh the
         //wide control and not the ParentControl wide
         parentControl.removeRefFromField();

         _pic = parentControl.getPIC();
         DataType = parentControl.DataType;

         var num = new NUM_TYPE();

         // set properties of wide Control
         num.NUM_4_LONG(rectWide.x);
         setProp(PropInterface.PROP_TYPE_LEFT, num.toXMLrecord());

         num.NUM_4_LONG(rectWide.y);
         setProp(PropInterface.PROP_TYPE_TOP, num.toXMLrecord());

         num.NUM_4_LONG(rectWide.width);
         setProp(PropInterface.PROP_TYPE_WIDTH, num.toXMLrecord());

         num.NUM_4_LONG(rectWide.height);
         setProp(PropInterface.PROP_TYPE_HEIGHT, num.toXMLrecord());

         setProp(PropInterface.PROP_TYPE_MULTILINE, "0");
         setProp(PropInterface.PROP_TYPE_VISIBLE, "1");
         String Mod = parentControl.isModifiable()
                         ? "1"
                         : "0";
         setProp(PropInterface.PROP_TYPE_MODIFIABLE, Mod);
         setProp(PropInterface.PROP_TYPE_BORDER, "1");

         String heb = _pic.isHebrew() ? "1" : "0";
         setProp(PropInterface.PROP_TYPE_HEBREW, heb);

         num.NUM_4_LONG((int)MultilineHorizontalScrollBar.WordWrap);
         setProp(PropInterface.PROP_TYPE_MULTILINE_WORDWRAP_SCROLL, num.toXMLrecord());

         //WordWrap work only if Multiline is true. So, we also need to Multiline = true.
         //NOTE: although Multiline is set to true, allowCR should be false (by-default it is false).
         setProp(PropInterface.PROP_TYPE_MULTILINE, "1");

         num.NUM_4_LONG(parentControl.getProp(PropInterface.PROP_TYPE_COLOR).getValueInt());
         setProp(PropInterface.PROP_TYPE_COLOR, num.toXMLrecord());

         num.NUM_4_LONG(parentControl.getProp(PropInterface.PROP_TYPE_FONT).getValueInt());
         setProp(PropInterface.PROP_TYPE_FONT, num.toXMLrecord());

         num.NUM_4_LONG(parentControl.getProp(PropInterface.PROP_TYPE_TRANSLATOR).getValueInt());
         setProp(PropInterface.PROP_TYPE_TRANSLATOR, num.toXMLrecord());

         //fixed defect 142530, handle GetSpecialEditLeftAlign for wide mode 
         int horAligment = parentControl.getProp(PropInterface.PROP_TYPE_HORIZONTAL_ALIGNMENT).getValueInt();
         if (Manager.GetSpecialEditLeftAlign())
            horAligment = (int)AlignmentTypeHori.Left;

         num.NUM_4_LONG(horAligment);
         setProp(PropInterface.PROP_TYPE_HORIZONTAL_ALIGNMENT, num.toXMLrecord());

      }

      /// <summary>check if auto wide needs to be opened if it needs to be opened
      /// then it raises an event</summary>
      /// <param name = "act"></param>
      /// <param name = "mgControl"></param>
      public void AutoWideModeCheck(int act)
      {
         if (checkProp(PropInterface.PROP_TYPE_AUTO_WIDE, false))
         {
            bool lenCheck = false;
            bool CheckAutoWide = true;

            switch (act)
            {
               case InternalInterface.MG_ACT_CHAR:
               case InternalInterface.MG_ACT_BEGIN_DROP:
               case InternalInterface.MG_ACT_CLIP_PASTE:
               case InternalInterface.MG_ACT_EDT_DELCURCH:
               case InternalInterface.MG_ACT_EDT_DELPRVCH:
                  lenCheck = true;
                  break;

               case InternalInterface.MG_ACT_EDT_PRVCHAR:
               case InternalInterface.MG_ACT_EDT_NXTCHAR:
               case InternalInterface.MG_ACT_EDT_ENDFLD:
               case InternalInterface.MG_ACT_EDT_NXTWORD:
               case InternalInterface.MG_ACT_EDT_PRVWORD:
               case InternalInterface.MG_ACT_EDT_BEGFLD:
               case InternalInterface.MG_ACT_EDT_MARKPRVCH:
               case InternalInterface.MG_ACT_EDT_MARKNXTCH:
               case InternalInterface.MG_ACT_EDT_MARKTOBEG:
               case InternalInterface.MG_ACT_EDT_MARKTOEND:
               case InternalInterface.MG_ACT_EDT_ENDLINE:
                  // Actions that not support in rich client
                  // case MG_ACT_EDT_MARKALL:
                  // case MG_ACT_REPLACE_TEXT:
                  // case MG_ACT_MARK_AND_REPLACE_TEXT:
                  lenCheck = false;
                  break;

               default:
                  // all others action are not check the auto Wide
                  CheckAutoWide = false;
                  break;
            }

            if (CheckAutoWide)
            {
               Commands.checkAutoWide(this, getDisplayLine(true), lenCheck);
            }
         }
      }
      /// <summary>
      /// sets dcValId 
      /// </summary>
      /// <param name="dcValId"></param>
      public void setDcValId(int dcValId)
      {
         _dcValId = dcValId;
      }

      /// <summary>
      ///   if the picture define as hebrew then set the lang to hebrew, otherwise restore the lang
      ///   The same we doing for online :edt_edit.cpp : SetKeyboardLanguage()
      /// </summary>
      /// <param name = "restoreLang"></param>
      public void SetKeyboardLanguage(bool restoreLang)
      {
         PIC ctrlPic = getPIC();
         bool setKeyBordLan = ctrlPic.isHebrew();

         //for RTFEdit and Choice controls, check the HEBREW property
         if (isRichEditControl() || isChoiceControl())
            setKeyBordLan = getProp(PropInterface.PROP_TYPE_HEBREW).getValueBoolean();

         if (setKeyBordLan)
            Commands.addAsync(CommandType.SET_ACTIVETE_KEYBOARD_LAYOUT, null, 0, setKeyBordLan, restoreLang);

         Commands.beginInvoke();
      }

      /// <summary>
      /// Initialize control properties for derived class
      /// </summary>
      public virtual void Init()
      {
      }

      /// <summary>
      /// get the behavior for the table control
      /// </summary>
      /// <returns></returns>
      protected virtual TableBehaviour GetTableBehaviour()
      {
         Debug.Assert(false);

         return TableBehaviour.UnlimitedItems;
      }

      /// <summary>(internal)
      /// checks if control is parkable
      /// </summary>
      /// <param name="moveByTab">check parkability for TAB forward/backward operations</param>
      /// <returns></returns>
      public virtual bool IsParkable(bool moveByTab)
      {
         return (isParkable(true, moveByTab));
      }

      /// <summary>
      ///   returns true if the control is parkable
      /// </summary>
      /// <param name = "checkEnabledAndVisible">check enabled and visible properties too</param>
      /// <param name = "moveByTab">check parkability for TAB forward/backward operations</param>
      public virtual bool isParkable(bool checkEnabledAndVisible, bool moveByTab)
      {
         bool result;

         result = (!isTableControl() && !isColumnControl() && !isFrameSet());
         TaskBase task = getForm().getTask();

         // non interactive -> not parkable.
         if (result)
            result = task.IsInteractive;

         if (result && task.DataView.isEmptyDataview() && ((_field != null) && _field.PartOfDataview))
            result = false;
         if (moveByTab)
            result = result && GetComputedBooleanProperty(PropInterface.PROP_TYPE_TAB_IN, true);
         if (result)
         {
            result = GetComputedBooleanProperty(PropInterface.PROP_TYPE_ALLOW_PARKING, true);
            if (result)
            {
               // QCR #441177. When we check the subform park ability, we need to check also it's visibility and enable properties.
               if (!isSubform())
                  result = ((_field != null && _field.getTask() == task) ||
                                             isBrowserControl() ||
                                             (IsDotNetControl() && _field == null));

               if (result && checkEnabledAndVisible)
                  result = (isEnabled() && isVisible());
               //TODO: Kaushal. I don't know, why we need to check parent table's enabled and visible 
               //properties explicitly. They will anyway be checked from isEnabled() and isVisible().
               //Short of time before 2.1 --- so, leaving as is for the moment. :)
               if (result && _parentTable != null && checkEnabledAndVisible)
                  result = (_parentTable.GetComputedBooleanProperty(PropInterface.PROP_TYPE_ENABLED, true) &&
                           _parentTable.GetComputedBooleanProperty(PropInterface.PROP_TYPE_VISIBLE, true));

               if (result && IsDotNetControl())
                  //we do not ussually call GUI methods directly, but we do it here because
                  // 1. Invoking GUIInteractive here will heavily effect performance
                  // 2. We do not invoke any gdi connected actions in the method
                  // 3. Timing ussue is not really important here, if change of control's parkability
                  // will not have immidiate effect, it is not so important
                  result = Manager.CanFocus(this);
            }
         }

         return result;
      }

      /// <summary>(internal)
      /// checks if control is DataView Control.
      /// </summary>
      public bool IsDataViewControl()
      {
         return (IsDotNetControl() && getProp(PropInterface.PROP_TYPE_DATAVIEWCONTROL).getValueBoolean());
      }

      /// <summary>
      ///   when access to control need to refresh the default button,                               
      ///   The definition: each form\subform is defain his default button.
      ///   if we on control the default button on the same control is the default button on the form.
      ///   if we on subform:
      ///   1.there is default button on the subform, this button is the default button on the form
      ///   2.there is NO default button on the subform , no button will define as default button on the form
      /// </summary>
      public void refreshDefaultButton()
      {
         Property defaultButtonProp = Form.getProp(PropInterface.PROP_TYPE_DEFAULT_BUTTON);
         defaultButtonProp.RefreshDisplay(true);
      }

      /// <summary>
      /// By default, click on tree is no different from click on any other control.
      /// </summary>
      /// <returns></returns>
      virtual protected bool ShouldDifferentiateControlHitOnTree()
      {
         return false;
      }

      /// <summary>
      ///  By default, click on tree is no different from click on any other control.
      /// </summary>
      /// <param name="leftClickWasPressed"></param>
      /// <returns></returns>
      virtual protected bool RaiseControlHitOnTree(bool leftClickWasPressed)
      {
         return false;
      }

      /// <summary>
      /// Raise ACT_CTRL_HIT on left mouse click if it meets following condition.
      /// </summary>
      /// <returns></returns>
      virtual protected bool RaiseControlHitOnLeftClickOfMouseDown()
      {
         bool raiseCtrlHit = false;
         if (!IsHyperTextButton() && !isRadio() && !isTabControl())
            raiseCtrlHit = true;
         return raiseCtrlHit;

      }

      /// <summary>
      /// Returns true if the 'Control Hit' event should be raised on mouse down.
      /// The method was extracted from processMouseDown, since we might have different
      /// behaviors for different types of tree controls.
      /// </summary>
      /// <param name="leftClickWasPressed"></param>
      /// <returns></returns>
      internal bool RaiseControlHitOnMouseDown(bool leftClickWasPressed)
      {
         bool raiseCtrlHit = false;

         if (ShouldDifferentiateControlHitOnTree())
         {
            // If tree has different behavior (online/RC) for ctrl hit, Check the specific
            // env to see if control hit is needed.
            if (RaiseControlHitOnTree(leftClickWasPressed))
               raiseCtrlHit = true;
         }
         // not tree, or default behavior.
         else if (leftClickWasPressed)
         {
            // QCR# 724383. Ctrl_hit action for HyperText button is handled from LinkClicked event on the button. 
            // So, it should not be handled for MouseDown event.
            //also for RadioControl & RadioControl, the CTRL_HIT is send from the processSelection
            if (RaiseControlHitOnLeftClickOfMouseDown())
               raiseCtrlHit = true;
         }

         return raiseCtrlHit;
      }

      /// <summary>
      /// Handle MG_ACT_HIT on the subform control
      /// </summary>
      public virtual void OnSubformClick()
      {
         Manager.EventsManager.addGuiTriggeredEvent(getForm().getTask(), InternalInterface.MG_ACT_HIT);
      }

      /// <summary>
      /// returns MgForm of the subform control task
      /// </summary>
      public virtual MgFormBase GetSubformMgForm()
      {
         return null;
      }

      /// <summary>
      /// Refreshes the subform of the control. (only for Online, not relevant for RC)
      /// </summary>
      public virtual void RefreshSubform()
      {
      }

      /// <summary>
      /// This function gets the property value of the DN control.
      /// </summary>
      /// <returns>Property value</returns>
      public object GetDNControlValue()
      {
         Debug.Assert(IsDotNetControl());

         //Get property info.
         string propName = _propTab.getPropById(PropInterface.PROP_TYPE_DN_CONTROL_VALUE_PROPERTY).getValue();
         PropertyInfo propInfo = (PropertyInfo)ReflectionServices.GetMemeberInfo(DNType, propName, false, null);

         //Get the property value.
         return ReflectionServices.GetPropertyValue(propInfo, this, null);
      }

      /// <summary>
      /// This function sets\updates the property of the DN control.
      /// </summary>
      private void SetDNControlValue()
      {
         Debug.Assert(IsDotNetControl());

         //Get property info.This is for Instance properties only, as third param 'isStatic' is passed 'false'.
         string propName = _propTab.getPropById(PropInterface.PROP_TYPE_DN_CONTROL_VALUE_PROPERTY).getValue();
         PropertyInfo propInfo = (PropertyInfo)ReflectionServices.GetMemeberInfo(DNType, propName, false, null);

         //Set property value.
         SetDNControlPropertyValue(propInfo, Value, DataType, _pic);
      }

#if !PocketPC
      /// <summary>
      /// This method creates a dataTable and binds it to DataView Control.
      /// </summary>
      private void OnDVControlCreate()
      {
         bool isDataViewControl = true;
         if (isDataViewControl)
         {
            DVDataTable dvDataTable = new DVDataTable();

            //All the field indexes selected for the DataTable will be a comma separated string. 
            String selectedFields = null;
            if (getProp(PropInterface.PROP_TYPE_DATAVIEWCONTROL_FIELDS) != null)
            {
               selectedFields = getProp(PropInterface.PROP_TYPE_DATAVIEWCONTROL_FIELDS).getValue();

               // Parse this string  and get each fields details from
               // ctrl.getForm().getTask().DataView._fieldsTab
               // and prepare columnList of DVDataTable. Call DVDataTable.AddColumns (columnList), to create
               // DataColumns in DataTable.
               dvDataTable.PrepareColumns(getForm().getTask(), selectedFields);
            }
            // delay databinding to dvcontrol
            //string propName = _propTab.getPropById(PropInterface.PROP_TYPE_DN_CONTROL_DATA_SOURCE_PROPERTY).getValue();
            //Commands.addAsync(CommandType.SET_DVCONTROL_DATASOURCE, this, 0, dvDataTable.DataTblObj, propName);

            DVControlManager dvControlManager = new DVControlManager(this, dvDataTable);
            getForm().DVControlManager = dvControlManager;
         }
      }
#endif
      /// <summary>
      /// This function sets the value of the given property on the dot net control.
      /// </summary>
      /// <param name="propInfo">object containing property to be updated</param>
      /// <param name="mgValue">value to be set on the property</param>
      /// <param name="storageAttr">datatype of the value passed</param>
      /// <param name="pic">picture of the value passed</param>
      internal void SetDNControlPropertyValue(PropertyInfo propInfo, String mgValue, StorageAttribute storageAttr, PIC pic)
      {
         //it is possible that the property does not exist, in this case just ignore it
         if (propInfo != null)
         {
            Type type = DNConvert.getDefaultDotNetTypeForMagicType(mgValue, storageAttr);

            //TODO:Handle case of binary blob and vector.

            //convert the mgValue to dot net data type
            Object dnObjectValue = null;

            if (storageAttr == StorageAttribute.DOTNET)
            {
               int key = BlobType.getKey(mgValue);
               Object obj = DNManager.getInstance().DNObjectsCollection.GetDNObj(key);
               dnObjectValue = DNConvert.doCast(obj, type);
            }
            else
               dnObjectValue = DNConvert.convertMagicToDotNet(mgValue, storageAttr, type);

            // set the property value
            // value changed event handler is removed and added back because,
            // when worker thread sets the value of a .Net control, there is no need to trap the change event and then,
            // update the variable back since worker thread has already updated the value.
#if !PocketPC
            if (_field != null)
               Commands.RemoveDNControlValueChangedHandler(this);
#endif
            try
            {
               ReflectionServices.SetPropertyValue(propInfo, this, new object[] { }, dnObjectValue);
            }
            catch (System.Exception ex)
            {
               Events.WriteExceptionToLog(ex);
            }

#if !PocketPC
            if (_field != null)
               Commands.AddDNControlValueChangedHandler(this);
#endif
         }
      }

      /// <summary>
      /// init the control info object for the runtime designer
      /// </summary>
      /// <returns></returns>
      internal ControlDesignerInfo BuildStudioValuesDictionary()
      {
         ControlDesignerInfo result = new ControlDesignerInfo();

         result.Properties = DesignerPropertiesBuilder.build(this);

         result.Isn = ControlIsn;
         result.Id = GetHashCode();
         result.LinkedIds = new List<int>();
         result.ControlType = Type;
         result.IsFrame = IsFrame();
         object parent = getLinkedParent(true);
         if (parent is MgControlBase)
            result.ParentId = parent.GetHashCode();

         EnvControlsPersistencyPath envControlsPersistencyPath = EnvControlsPersistencyPath.GetInstance();
         result.FileName = envControlsPersistencyPath.GetFullControlsPersistencyFileName(this.getForm());

         if (CreatelinkedControls())
         {
            // for subform control _linkedControls isn't relevant, 
            // we need to calc the LinkedIsns according to the children of the subform 
            if (Type == MgControlType.CTRL_TYPE_SUBFORM)
            {
               MgFormBase subformForm = GetSubformMgForm();
               if (subformForm != null)
               {
                  for (int i = 0; i < subformForm.CtrlTab.getSize(); i++)
                  {
                     MgControlBase controlBase = subformForm.CtrlTab.getCtrl(i);
                     if (controlBase.getParent() == this)
                        result.LinkedIds.Add(controlBase.GetHashCode());
                  }
               }
            }
            else
            {
               foreach (var item in _linkedControls)
               {
                  result.LinkedIds.Add(item.GetHashCode());
               }
            }
         }

         return result;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      bool CreatelinkedControls()
      {
         //for runtime designer : for table control don't create the links controls(isns) we don't create those control
         return (Type != MgControlType.CTRL_TYPE_TABLE && Type != MgControlType.CTRL_TYPE_FRAME_SET);
      }
      /// <summary>
      /// 
      /// </summary>
      /// <param name="id"></param>
      /// <returns></returns>
      public bool PropertyExists(int id)
      {
         return _propTab.propExists(id);
      }

      public override string ToString()
      {
         return "{" + GetType().Name + ": " + Type.ToString() + "}";
      }

      /// <summary>
      /// mark properties for which runtime designer values should be used
      /// </summary>
      /// <param name="props"></param>
      public void SetDesignerPropertiesValues(Dictionary<string, object> props)
      {
         Property prop;
         int propId = 0;

         foreach (var keyValue in props)
         {
            propId = DesignerPropertiesBuilder.GetMagicPropId(Type, keyValue.Key);

            // for the visibility property we must have a property, so other calculations will know when the control is invisible
            if (propId == PropInterface.PROP_TYPE_VISIBLE)
               prop = getProp(propId);
            else
               prop = _propTab.getPropById(propId);

            if (prop != null)
               prop.SetDesignerValue(keyValue.Key, keyValue.Value);

            if (propId == PropInterface.PROP_TYPE_FORMAT && isButton() && !IsImageButton())
            {
               valueFromDesigner = true;
            }
         }
      }
      /// <summary> Checks if all non mask chars are blanks (online PIC_CTX::is_all_blank)</summary>
      /// <param name="value">- The value to check</param>
      public bool isAllBlanks(String val)
      {
         bool allBlanks = true;
         int lenTocheck = Math.Min(_pic.getMaskSize(), val.Length);

         for (int pos = 0; pos < lenTocheck; pos++)
         {
            if (!_pic.picIsMask(pos) && val[pos] != ' ')
            {
               allBlanks = false;
               break;
            }
         }

         return allBlanks;
      }

        protected ImeParam imeParam0; // WM_IME_COMPOSITION
        protected ImeParam imeParam1; // WM_IME_START/END_COMPOSITION
        protected ImeParam imeParam2; // WM_KEYDOWN
        protected ImeParam imeParam3; // WM_CHAR

        public void SaveImeParam(ImeParam im)
        {
            switch (im.Msg)
            {
                case NativeWindowCommon.WM_IME_STARTCOMPOSITION:
                case NativeWindowCommon.WM_IME_ENDCOMPOSITION:
                    imeParam0 = im;
                    break;

                case NativeWindowCommon.WM_IME_COMPOSITION:
                    imeParam1 = im;
                    break;

                case NativeWindowCommon.WM_KEYDOWN:
                    imeParam2 = im;
                    break;
                case NativeWindowCommon.WM_CHAR:
                    imeParam3 = im;
                    break;
            }
        }

        public ImeParam GetImeParam(int msg)
        {
            ImeParam im = null;
            switch (msg)
            {
                case NativeWindowCommon.WM_IME_STARTCOMPOSITION:
                case NativeWindowCommon.WM_IME_ENDCOMPOSITION:
                    im = imeParam0;
                    imeParam0 = null;
                    break;

                case NativeWindowCommon.WM_IME_COMPOSITION:
                    im = imeParam1;
                    imeParam1 = null;
                    break;

                case NativeWindowCommon.WM_KEYDOWN:
                    im = imeParam2;
                    imeParam2 = null;
                    break;
                case NativeWindowCommon.WM_CHAR:
                    im = imeParam3;
                    imeParam3 = null;
                    break;
            }
            return im;
        }
    }
}
