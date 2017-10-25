using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;
using com.magicsoftware.unipaas.dotnet;
using com.magicsoftware.unipaas.env;
using com.magicsoftware.unipaas.gui;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.unipaas.management.tasks;
using com.magicsoftware.unipaas.util;
using com.magicsoftware.util;
using util.com.magicsoftware.util;
using com.magicsoftware.util.Xml;
using com.magicsoftware.unipaas.gui.low;

namespace com.magicsoftware.unipaas.management.gui
{
   /// <summary>
   /// used for calculating coordinates in pixels - allows the caller to define if th regular values should be used, or the studio values
   /// </summary>
   /// <param name="p"></param>
   /// <returns></returns>
   delegate int GetValueDelegate(Property p);

   [Flags]
   enum DesignerInfoFlags { None = 0, IsDesignerValue = 1, IsBackColor = 2, IsForeColor  = 4, IsGradientStyle = 8, IsTitleColor = 10};

   /// <summary>
   ///   this class represents a Property object of control, form or task.
   /// </summary>
   public class Property
   {
      private int _id = Int32.MinValue;
      private StorageAttribute _dataType; // Alpha | Numeric | Logical
      private String _val;
      private PIC _pic;
      private int _expId;
      private PropParentInterface _parentObj; // a reference to the parent object (Control, Form, TaskBase)
      private char _parentType; // Control|Form|TaskBase
      private readonly MgArrayList _prevValues; // reverence to property values in a table
      private Object _prevDotNetValue;
      private String _orgValue; // reverence to property values in a table
      private static PIC _numericPropertyPic;
      internal bool IsGeneric { get; private set; }

      public IPropertyRefreshRule RefreshRule { get; set; }

      // task definition associated with the property.
      private TaskDefinitionId _taskDefinitionId;
      public TaskDefinitionId TaskDefinitionId
      {
         get
         {
            return _taskDefinitionId;
         }
      }

      // studioValue for offline state
      public String StudioValue;

      /// <summary>
      /// the expression is already computed once
      /// </summary>
      private bool _expAlreadyComputedOnce;

      private PropertyInfo _propInfo; //property info of generic property
      private PropertyInfo PropInfo
      {
         get
         {
            if (_propInfo == null)
            {
               // obtain the .net type of the parent .net control.
               var parentControl = ((MgControlBase)_parentObj);
               Debug.Assert(parentControl != null, "parentControl != null");
               Type dnType = parentControl.DNType;
               // if a .net reference was attached to the control, it must be of the same type as the control.
               Debug.Assert(parentControl.DNObjectReferenceField == null || parentControl.DNObjectReferenceField.DNType == dnType);

               _propInfo = (PropertyInfo)ReflectionServices.GetMemeberInfo(dnType, _genericName, false, _hashCode);
            }
            return _propInfo;
         }
      }

      private String _genericName;
      private int? _hashCode;

      private DesignerInfoFlags designerInfoFlag;

      /// <summary>
      ///   CTOR
      /// </summary>
      protected internal Property()
      {
         _prevValues = new MgArrayList();
         RefreshRule = null;
      }

      /// <summary>
      ///   CTOR
      /// </summary>
      /// <param name = "cId">the id of the property</param>
      /// <param name = "cParentObj">the parent object of this property</param>
      protected internal Property(int cId, PropParentInterface cParentObj, char parType)
         : this()
      {
         setId(cId);
         _parentObj = cParentObj;
         _parentType = parType;
         setDataType();
      }

      /// <summary>
      ///   CTOR
      /// </summary>
      protected internal Property(int cId, PropParentInterface cParentObj, char parType, String val)
         : this(cId, cParentObj, parType)
      {
         setValue(val);
         setOrgValue();
      }

      /// <summary>
      ///   set the id of this property
      /// </summary>
      private void setId(int cId)
      {
         _id = cId;
      }

      /// <summary>
      ///   Need part input String to relevant for the class data
      /// </summary>
      /// <param name = "parentRef">reference to parent </param>
      /// <param name="parType"></param>
      /// <param name="task"></param>
      protected internal void fillData(PropParentInterface parentRef, char parType)
      {
         XmlParser parser = Manager.GetCurrentRuntimeContext().Parser;
         int endContext = parser.getXMLdata().IndexOf(XMLConstants.TAG_CLOSE, parser.getCurrIndex(), StringComparison.Ordinal);

         if (_parentObj == null && parentRef != null)
         {
            _parentObj = parentRef;
            _parentType = parType;
         }

         if (endContext != -1 && endContext < parser.getXMLdata().Length)
         {
            // last position of its tag
            String tag = parser.getXMLsubstring(endContext);
            parser.add2CurrIndex(tag.IndexOf(XMLConstants.MG_TAG_PROP) + XMLConstants.MG_TAG_PROP.Length);

            List<string> tokensVector = XmlParser.getTokens(parser.getXMLsubstring(endContext), XMLConstants.XML_ATTR_DELIM);
            if (Events.ShouldLog(Logger.LogLevels.Development))
               Events.WriteDevToLog(string.Format("In Prop.FillData(): {0}", Misc.CollectionToString(tokensVector)));
            initElements(tokensVector);

            SetInitialValueAsStudioValue();

            parser.setCurrIndex(endContext + XMLConstants.TAG_CLOSE.Length); // to delete "/>" too

            // deal with internal tags
            InitInnerObjects(parser);

            // for the properties of this types need set parameters to the parent - CONTROL (in this case)
            if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
               setControlAttributes();
            else if (_parentType == GuiConstants.PARENT_TYPE_FORM)
               _prevValues.SetSize(1);
         }
         else
            Events.WriteExceptionToLog("In Property.FillData(): Out of string bounds");
      }

      private void InitInnerObjects(XmlParser xmlParser)
      {
         string nextTag = xmlParser.getNextTag();
         switch (nextTag)
         {
            case XMLConstants.MG_TAG_TASKDEFINITIONID_ENTRY:
               string xmlBuffer = xmlParser.ReadToEndOfCurrentElement();
               InitTaskDefinitionId(xmlBuffer);

               int endContext = xmlParser.getXMLdata().IndexOf(XMLConstants.MG_TAG_PROP + XMLConstants.TAG_CLOSE, xmlParser.getCurrIndex());
               xmlParser.setCurrIndex(endContext + (XMLConstants.MG_TAG_PROP + XMLConstants.TAG_CLOSE).Length);
               break;

            default:
               break;
         }

      }

      /// <summary>
      ///   Make initialization of private elements by found tokens
      /// </summary>
      /// <param name = "tokensVector">found tokens, which consist attribute/value of every found element </param>
      private void initElements(List<String> tokensVector)
      {
         for (int j = 0;
              j < tokensVector.Count;
              j += 2)
         {
            string attribute = (tokensVector[j]);
            string valueStr = (tokensVector[j + 1]);

            switch (attribute)
            {
               case XMLConstants.MG_ATTR_ID:
                  setId(XmlParser.getInt(valueStr));
                  setDataType(); // The 'name' member must be found in this point
                  break;
               case XMLConstants.MG_ATTR_STUDIO_VALUE:
                  StudioValue = XmlParser.unescape(valueStr);
                  break;
               case XMLConstants.MG_ATTR_VALUE:
                  _val = XmlParser.unescape(valueStr);
                  _orgValue = _val;
                  break;
               case XMLConstants.MG_ATTR_EXP:
                  _expId = XmlParser.getInt(valueStr);
                  if (_expId > 0 && _id == PropInterface.PROP_TYPE_TOOLTIP)
                     setDataType();
                  break;
               case XMLConstants.MG_ATTR_IS_GENERIC:
                  /* for generic property*/
                  IsGeneric = XmlParser.getBoolean(valueStr);
                  break;
               case XMLConstants.MG_ATTR_NAME:
                  /* for generic property*/
                  if (IsGeneric)
                     _genericName = valueStr;
                  break;
               case XMLConstants.MG_ATTR_DATA_TYPE:
                  /* for generic property*/
                  if (IsGeneric)
                     _dataType = (StorageAttribute)valueStr[0];
                  break;
               case XMLConstants.MG_ATTR_HASHCODE:
                  /* for generic property*/
                  if (IsGeneric)
                     _hashCode = XmlParser.getInt(valueStr);
                  break;
               case XMLConstants.MG_ATTR_PICTURE:
                  /* for generic property*/
                  if (IsGeneric)
                  {
                     int compIdx = _parentObj.getCompIdx();
                     _pic = new PIC(valueStr, _dataType, compIdx);
                  }
                  break;
               case XMLConstants.MG_ATTR_CTL_IDX:
                  TaskDefinitionId.CtlIndex = XmlParser.getInt(valueStr);
                  break;
               case XMLConstants.MG_ATTR_PROGRAM_ISN:
                  TaskDefinitionId.ProgramIsn = XmlParser.getInt(valueStr);
                  break;
               default:
                  Events.WriteExceptionToLog(
                     string.Format(
                        "There is no such tag in Property class. Insert case to Property.initElements for {0}",
                        attribute));
                  break;
            }

            // if the property is already compute in the server , then set _expAlreadyComputedOnce
            if (IsComputedOnceOnServer())
               _expAlreadyComputedOnce = true;
         }
      }


      /// <summary>
      /// reset _expAlreadyComputedOnce flag
      /// </summary>
      public void ResetComputedOnceFlag()
      {
         _expAlreadyComputedOnce = false;
      }
      /// <summary>
      /// 
      /// </summary>
      /// <param name="xmlBuffer"></param>
      void InitTaskDefinitionId(string xmlBuffer)
      {
         TaskDefinitionIdTableSaxHandler handler = new TaskDefinitionIdTableSaxHandler(SetTaskDefinitionId);
         handler.parse(Encoding.UTF8.GetBytes(xmlBuffer));
      }

      /// <summary>
      /// callback for the sax parser
      /// </summary>
      /// <param name="taskDefinitionId"></param>
      /// <param name="xmlId"></param>
      /// <param name="defaultTagList"></param>
      public void SetTaskDefinitionId(TaskDefinitionId taskDefinitionId)
      {
         _taskDefinitionId = taskDefinitionId;
      }

      /// <summary>
      ///   set the data type and the picture by the id
      /// </summary>
      private void setDataType()
      {
         int compIdx = _parentObj.getCompIdx();

         _pic = null;

         if (_id == Int32.MinValue)
         {
            Events.WriteExceptionToLog(string.Format("To fill dataType member in Property.FillDataType must id!={0}", Int32.MinValue));
            return;
         }

         switch (_id)
         {
            case PropInterface.PROP_TYPE_TITLE_BAR:
            case PropInterface.PROP_TYPE_BORDER:
            case PropInterface.PROP_TYPE_DISPLAY_MENU:
            case PropInterface.PROP_TYPE_DISPLAY_TOOLBAR:
            case PropInterface.PROP_TYPE_AUTO_WIDE:
            case PropInterface.PROP_TYPE_HEBREW:
            case PropInterface.PROP_TYPE_MODIFY_IN_QUERY:
            case PropInterface.PROP_TYPE_MAXBOX:
            case PropInterface.PROP_TYPE_MINBOX:
            case PropInterface.PROP_TYPE_SYSTEM_MENU:
            case PropInterface.PROP_TYPE_END_CONDITION:
            case PropInterface.PROP_TYPE_SELECTION:
            case PropInterface.PROP_TYPE_ALLOW_MODIFY:
            case PropInterface.PROP_TYPE_ALLOW_CREATE:
            case PropInterface.PROP_TYPE_ALLOW_DELETE:
            case PropInterface.PROP_TYPE_ALLOW_QUERY:
            case PropInterface.PROP_TYPE_ALLOW_RANGE:
            case PropInterface.PROP_TYPE_ALLOW_LOCATE:
            case PropInterface.PROP_TYPE_ALLOW_SORT:
            case PropInterface.PROP_TYPE_TASK_PROPERTIES_ALLOW_INDEX:
            case PropInterface.PROP_TYPE_ALLOW_LOCATE_IN_QUERY:
            case PropInterface.PROP_TYPE_CONFIRM_UPDATE:
            case PropInterface.PROP_TYPE_FORCE_SUFFIX:
            case PropInterface.PROP_TYPE_FORCE_DELETE:
            case PropInterface.PROP_TYPE_MUST_INPUT:
            case PropInterface.PROP_TYPE_MODIFIABLE:
            case PropInterface.PROP_TYPE_VISIBLE:
            case PropInterface.PROP_TYPE_ENABLED:
            case PropInterface.PROP_TYPE_ALLOW_PARKING:
            case PropInterface.PROP_TYPE_CONFIRM_CANCEL:
            case PropInterface.PROP_TYPE_REPEATABLE:
            case PropInterface.PROP_TYPE_HIGHLIGHTING:
            case PropInterface.PROP_TYPE_PASSWORD:
            case PropInterface.PROP_TYPE_MULTILINE:
            case PropInterface.PROP_TYPE_TAB_IN:
            case PropInterface.PROP_TYPE_IS_CACHED:
            case PropInterface.PROP_TYPE_PRELOAD_VIEW:
            case PropInterface.PROP_TYPE_MULTILINE_VERTICAL_SCROLL:
            case PropInterface.PROP_TYPE_MULTILINE_ALLOW_CR:
            case PropInterface.PROP_TYPE_ALLOW_COL_RESIZE:
            case PropInterface.PROP_TYPE_ALLOW_REORDER:
            case PropInterface.PROP_TYPE_SHOW_LINES:
            case PropInterface.PROP_TYPE_SORT_COLUMN:
            case PropInterface.PROP_TYPE_COL_ALLOW_FILTERING:
            case PropInterface.PROP_TYPE_DISPLAY_STATUS_BAR:
            case PropInterface.PROP_TYPE_AUTO_REFRESH:
            case PropInterface.PROP_TYPE_COLUMN_PLACEMENT:
            case PropInterface.PROP_TYPE_PARK_ON_CLICK:
            case PropInterface.PROP_TYPE_HORIZONTAL_PLACEMENT:
            case PropInterface.PROP_TYPE_VERTICAL_PLACEMENT:
            case PropInterface.PROP_TYPE_SHOW_FULL_ROW:
            case PropInterface.PROP_TYPE_TRACK_SELECTION:
            case PropInterface.PROP_TYPE_HOT_TRACK:
            case PropInterface.PROP_TYPE_SHOW_BUTTONS:
            case PropInterface.PROP_TYPE_LINES_AT_ROOT:
            case PropInterface.PROP_TYPE_ALLOW_EMPTY_DATAVIEW:
            case PropInterface.PROP_TYPE_SCROLL_BAR:
            case PropInterface.PROP_TYPE_COLUMN_DIVIDER:
            case PropInterface.PROP_TYPE_LINE_DIVIDER:

            case PropInterface.PROP_TYPE_THREE_STATES:
            case PropInterface.PROP_TYPE_REFRESH_WHEN_HIDDEN:
            case PropInterface.PROP_TYPE_ALLOW_OPTION:
            case PropInterface.PROP_TYPE_RETAIN_FOCUS:
            case PropInterface.PROP_TYPE_PRINT_DATA:
            case PropInterface.PROP_TYPE_CLOSE_TASKS_BY_MDI_MENU:
            case PropInterface.PROP_TYPE_ALLOW_DRAGGING:
            case PropInterface.PROP_TYPE_ALLOW_DROPPING:
            case PropInterface.PROP_TYPE_ROW_PLACEMENT:
            case PropInterface.PROP_TYPE_SHOW_IN_WINDOW_MENU:
            case PropInterface.PROP_TYPE_DATAVIEWCONTROL:
            case PropInterface.PROP_TYPE_TASK_PROPERTIES_OPEN_TASK_WINDOW:
            case PropInterface.PROP_TYPE_TASK_PROPERTIES_ALLOW_EVENTS:
            case PropInterface.PROP_TYPE_TASK_PROPERTIES_INDEX_OPTIMIZATION:
            case PropInterface.PROP_TYPE_TASK_PROPERTIES_RANGE:
            case PropInterface.PROP_TYPE_TASK_PROPERTIES_LOCATE:
            case PropInterface.PROP_TYPE_TASK_PROPERTIES_POSITION:
            case PropInterface.PROP_TYPE_TASK_PROPERTIES_SQL_RANGE:
            case PropInterface.PROP_TYPE_TOP_BORDER:
            case PropInterface.PROP_TYPE_RIGHT_BORDER:
            case PropInterface.PROP_TYPE_BEFORE_900_VERSION:
            case PropInterface.PROP_TYPE_TOP_BORDER_MARGIN:
            case PropInterface.PROP_TYPE_FILL_WIDTH:
            case PropInterface.PROP_TYPE_MULTI_COLUMN_DISPLAY:
            case PropInterface.PROP_TYPE_SHOW_ELLIPISIS:
               _dataType = StorageAttribute.BOOLEAN;
               break;

            case PropInterface.PROP_TYPE_DEFAULT_BUTTON:
            case PropInterface.PROP_TYPE_IMAGE_FILENAME:
            case PropInterface.PROP_TYPE_EVAL_END_CONDITION:
            case PropInterface.PROP_TYPE_FRAME_NAME:
            case PropInterface.PROP_TYPE_NAME:
            case PropInterface.PROP_TYPE_TRASACTION_BEGIN:
            case PropInterface.PROP_TYPE_TASK_PROPERTIES_TRANSACTION_MODE:
            case PropInterface.PROP_TYPE_WALLPAPER:
            case PropInterface.PROP_TYPE_ATTRIBUTE:
            case PropInterface.PROP_TYPE_TRIGGER:
            case PropInterface.PROP_TYPE_SELECT_MODE:
            case PropInterface.PROP_TYPE_TASK_ID:
            case PropInterface.PROP_TYPE_PLACEMENT:
            case PropInterface.PROP_TYPE_TABBING_CYCLE:
            case PropInterface.PROP_TYPE_PARAMETERS:
            case PropInterface.PROP_TYPE_LINE_WIDTH:
            case PropInterface.PROP_TYPE_IMAGE_LIST_INDEXES:
            case PropInterface.PROP_TYPE_VISIBLE_LAYERS_LIST:
            case PropInterface.PROP_TYPE_OBJECT_TYPE:
            case PropInterface.PROP_TYPE_DN_CONTROL_VALUE_PROPERTY:
            case PropInterface.PROP_TYPE_DN_CONTROL_VALUE_CHANGED_EVENT:
            case PropInterface.PROP_TYPE_REAL_OBJECT_TYPE:
            case PropInterface.PROP_TYPE_DOTNET_OBJECT: //the .Net object (field): "generation,id"
            case PropInterface.PROP_TYPE_DATAVIEWCONTROL_FIELDS:
            case PropInterface.PROP_TYPE_TASK_PROPERTIES_RANGE_ORDER:
            case PropInterface.PROP_TYPE_TASK_PROPERTIES_LOCATE_ORDER:
            case PropInterface.PROP_TYPE_TASK_PROPERTIES_POSITION_USAGE:
            case PropInterface.PROP_TYPE_TASK_MODE:
            case PropInterface.PROP_TYPE_DN_CONTROL_DATA_SOURCE_PROPERTY:
            case PropInterface.PROP_TYPE_DN_CONTROL_DISPLAY_MEMBER_PROPERTY:
            case PropInterface.PROP_TYPE_DN_CONTROL_VALUE_MEMBER_PROPERTY:
               _dataType = StorageAttribute.ALPHA;
               break;

               // defect 143547: Column title is supposed to be unicode
            case PropInterface.PROP_TYPE_COLUMN_TITLE:
            case PropInterface.PROP_TYPE_DISPLAY_LIST:
            case PropInterface.PROP_TYPE_LABEL:
            case PropInterface.PROP_TYPE_FORMAT:
            case PropInterface.PROP_TYPE_RANGE:
            case PropInterface.PROP_TYPE_FORM_NAME:
            case PropInterface.PROP_TYPE_ADDITIONAL_INFORMATION:
            case PropInterface.PROP_TYPE_TEXT:
            case PropInterface.PROP_TYPE_HINT:
               _dataType = StorageAttribute.UNICODE;
               break;

            case PropInterface.PROP_TYPE_UOM:
            case PropInterface.PROP_TYPE_HOR_FAC:
            case PropInterface.PROP_TYPE_VER_FAC:
            case PropInterface.PROP_TYPE_TAB_ORDER:
            case PropInterface.PROP_TYPE_WALLPAPER_STYLE:
            case PropInterface.PROP_TYPE_INDEX:
            case PropInterface.PROP_TYPE_LINK_FIELD:
            case PropInterface.PROP_TYPE_DISPLAY_FIELD:
            case PropInterface.PROP_TYPE_PULLDOWN_MENU:
            case PropInterface.PROP_TYPE_CONTEXT_MENU:
            case PropInterface.PROP_TYPE_MINIMUM_HEIGHT:
            case PropInterface.PROP_TYPE_MINIMUM_WIDTH:
            case PropInterface.PROP_TYPE_CHECKBOX_MAIN_STYLE:
            case PropInterface.PROP_TYPE_TAB_CONTROL_SIDE:
            case PropInterface.PROP_TYPE_TAB_CONTROL_TABS_WIDTH:
            case PropInterface.PROP_TYPE_WINDOW_TYPE:
            case PropInterface.PROP_TYPE_STARTUP_POSITION:
            case PropInterface.PROP_TYPE_IMAGE_STYLE:
            case PropInterface.PROP_TYPE_AUTO_FIT:
            case PropInterface.PROP_TYPE_RAISE_AT:
            case PropInterface.PROP_TYPE_EXPAND_WINDOW:
            case PropInterface.PROP_TYPE_VISIBLE_LINES:
            case PropInterface.PROP_TYPE_CHOICE_COLUMNS:
            case PropInterface.PROP_TYPE_BORDER_STYLE:
            case PropInterface.PROP_TYPE_STYLE_3D:
            case PropInterface.PROP_TYPE_FONT:
            case PropInterface.PROP_TYPE_LAYER:
            case PropInterface.PROP_TYPE_WIDTH:
            case PropInterface.PROP_TYPE_HEIGHT:
            case PropInterface.PROP_TYPE_TITLE_HEIGHT:
            case PropInterface.PROP_TYPE_COLOR:
            case PropInterface.PROP_TYPE_BORDER_COLOR:
            case PropInterface.PROP_TYPE_FOCUS_COLOR:
            case PropInterface.PROP_TYPE_VISITED_COLOR:
            case PropInterface.PROP_TYPE_HOVERING_COLOR:
            case PropInterface.PROP_TYPE_ROW_HIGHLIGHT_COLOR:
            case PropInterface.PROP_TYPE_INACTIVE_ROW_HIGHLIGHT_COLOR:
            case PropInterface.PROP_TYPE_LEFT:
            case PropInterface.PROP_TYPE_TOP:
            case PropInterface.PROP_TYPE_PROMPT:
            case PropInterface.PROP_TYPE_TOOLTIP:
            case PropInterface.PROP_TYPE_HELP_SCR:
            case PropInterface.PROP_TYPE_LINES_IN_TABLE:
            case PropInterface.PROP_TYPE_SELECTION_ROWS:
            case PropInterface.PROP_TYPE_HORIZONTAL_ALIGNMENT:
            case PropInterface.PROP_TYPE_STARTUP_MODE:
            case PropInterface.PROP_TYPE_MULTILINE_WORDWRAP_SCROLL:
            case PropInterface.PROP_TYPE_ROW_HEIGHT:
            case PropInterface.PROP_TYPE_SET_COLOR_BY:
            case PropInterface.PROP_TYPE_ALTERNATING_BG_COLOR:
            case PropInterface.PROP_TYPE_WINDOW_WIDTH:
            case PropInterface.PROP_TYPE_MAIN_DISPLAY:
            case PropInterface.PROP_TYPE_EXPANDED_IMAGEIDX:
            case PropInterface.PROP_TYPE_COLLAPSED_IMAGEIDX:
            case PropInterface.PROP_TYPE_PARKED_COLLAPSED_IMAGEIDX:
            case PropInterface.PROP_TYPE_PARKED_IMAGEIDX:
            case PropInterface.PROP_TYPE_TRANSLATOR:
            case PropInterface.PROP_TYPE_FRAMESET_STYLE:
            case PropInterface.PROP_TYPE_FRAME_TYPE:
            case PropInterface.PROP_TYPE_SELECT_PROGRAM:
            case PropInterface.PROP_TYPE_ALLOWED_DIRECTION:
            case PropInterface.PROP_TYPE_ROW_HIGHLIGHT_STYLE:
            case PropInterface.PROP_TYPE_VERTICAL_ALIGNMENT:
            case PropInterface.PROP_TYPE_TABBING_ORDER:
            case PropInterface.PROP_TYPE_RADIO_BUTTON_APPEARANCE:
            case PropInterface.PROP_TYPE_LINE_STYLE:
            case PropInterface.PROP_TYPE_STATIC_TYPE:
            case PropInterface.PROP_TYPE_BUTTON_STYLE:
            case PropInterface.PROP_TYPE_GRADIENT_STYLE:
            case PropInterface.PROP_TYPE_GRADIENT_COLOR:
            case PropInterface.PROP_TYPE_LOAD_IMAGE_FROM:
            case PropInterface.PROP_TYPE_SUBFORM_TYPE:
            case PropInterface.PROP_TYPE_BOTTOM_POSITION_INTERVAL:
            case PropInterface.PROP_TYPE_SELECTION_MODE:
            case PropInterface.PROP_TYPE_ASSEMBLY_ID:
            case PropInterface.PROP_TYPE_PRGTSK_NUM:
            case PropInterface.PROP_TYPE_TITLE_COLOR:
            case PropInterface.PROP_TYPE_DIVIDER_COLOR:
            case PropInterface.PROP_TYPE_HOT_TRACK_COLOR:
            case PropInterface.PROP_TYPE_SELECTED_TAB_COLOR:
            case PropInterface.PROP_TYPE_TITLE_PADDING:
            case PropInterface.PROP_TYPE_HINT_COLOR:
            case PropInterface.PROP_TYPE_PERSISTENT_FORM_STATE_VERSION:
            case PropInterface.PROP_TYPE_ROW_BG_COLOR:
               if (_id == PropInterface.PROP_TYPE_TOOLTIP &&
                   (_expId > 0 || ((MgControlBase)_parentObj).getParent() is MgStatusBar))
                  _dataType = StorageAttribute.UNICODE;
               // pic will be set according to exp length
               else
               {
                  _dataType = StorageAttribute.NUMERIC;
                  if (_numericPropertyPic == null)
                     _numericPropertyPic = new PIC("N6", StorageAttribute.NUMERIC, compIdx);
                  _pic = _numericPropertyPic;
               }
               break;

            case PropInterface.PROP_TYPE_RETURN_ACTION:
               _dataType = StorageAttribute.NONE;
               break;

            case PropInterface.PROP_TYPE_DATA:
               _dataType = ((MgControlBase)_parentObj).DataType;
               break;

            case PropInterface.PROP_TYPE_NODE_ID:
            case PropInterface.PROP_TYPE_NODE_PARENTID:
               // the dataType of those properties will be updated after get the field.
               break;

            case PropInterface.PROP_TYPE_POP_UP:
            case PropInterface.PROP_TYPE_ORIENTATION_LOCK:
            case PropInterface.PROP_TYPE_ENTER_ANIMATION:
            case PropInterface.PROP_TYPE_EXIT_ANIMATION:
            case PropInterface.PROP_TYPE_NAVIGATION_DRAWER_MENU:
            case PropInterface.PROP_TYPE_ACTION_BAR_MENU:
            case PropInterface.PROP_TYPE_TITLE_BAR_COLOR:
            case PropInterface.PROP_TYPE_MOBILE_BORDER_WIDTH:
            case PropInterface.PROP_TYPE_BORDER_FOCUS_WIDTH:
            case PropInterface.PROP_TYPE_BORDER_FOCUS_COLOR:
            case PropInterface.PROP_TYPE_CORNER_RADIUS:
            case PropInterface.PROP_TYPE_OPEN_PICKER:
            case PropInterface.PROP_TYPE_OPEN_EDIT_DIALOG:
            case PropInterface.PROP_TYPE_DEFAULT_ALIGNMENT:
            case PropInterface.PROP_TYPE_KEYBOARD_TYPE:
            case PropInterface.PROP_TYPE_KEYBOARD_RETURN_KEY:
            case PropInterface.PROP_TYPE_ALLOW_SUGGESTIONS:
            case PropInterface.PROP_TYPE_MOBILE_IMAGE_LIST_FILE_NAME:
            case PropInterface.PROP_TYPE_SWIPE_REFRESH:
               //No handling is needed here as the properties are valid for mobile as for now.
               break;

            default:
               if (_id < GuiConstants.GENERIC_PROPERTY_BASE_ID)
                  Events.WriteExceptionToLog(string.Format("in Property.setDataType() no case for: {0}", _id));
               break;
         }
      }

      /// <summary>
      ///   set attributes of a the parent control
      /// </summary>
      private void setControlAttributes()
      {
         if (_parentObj == null || _parentType != GuiConstants.PARENT_TYPE_CONTROL)
         {
            Events.WriteExceptionToLog(
               "in Property.setControlAttributes() there is no parent or the parent is not a control");
            return;
         }
         var parentControl = (MgControlBase)_parentObj;
         switch (_id)
         {
            case PropInterface.PROP_TYPE_FRAME_NAME:
            case PropInterface.PROP_TYPE_NAME:
               parentControl.Name = _val;
               break;

            case PropInterface.PROP_TYPE_ATTRIBUTE:
               parentControl.DataType = (StorageAttribute)_val[0];
               break;

            case PropInterface.PROP_TYPE_FORMAT:
               parentControl.setPicStr(_val, _expId);
               break;

            case PropInterface.PROP_TYPE_RANGE:
               parentControl.setRange(getValue());
               break;

            case PropInterface.PROP_TYPE_DATA:
               if (_expId > 0)
                  parentControl.setValExp(_expId);
               else if (_val != null)
                  parentControl.setField(getValue());
               break;

            case PropInterface.PROP_TYPE_DOTNET_OBJECT:
               if (_val != null)
                  parentControl.SetDNObjectReferenceField(getValue());
               break;

            case PropInterface.PROP_TYPE_NODE_ID:
               if (_val != null)
               {
                  parentControl.setNodeId(getValue());
                  if (((MgControlBase)_parentObj).getNodeIdField() != null)
                     _dataType = ((MgControlBase)_parentObj).getNodeIdField().getType();
               }
               break;

            case PropInterface.PROP_TYPE_NODE_PARENTID:
               if (_val != null)
               {
                  parentControl.setNodeParentId(getValue());
                  FieldDef parentField = ((MgControlBase)_parentObj).getNodeParentIdField();
                  if (parentField != null)
                     _dataType = parentField.getType();
               }
               break;

            case PropInterface.PROP_TYPE_BUTTON_STYLE:
               if (_val != null)
                  parentControl.ButtonStyle = (CtrlButtonTypeGui)getValueInt();
               break;

            case PropInterface.PROP_TYPE_LAYER:
               if (_val != null)
                  parentControl.Layer = getValueInt();
               break;

            case PropInterface.PROP_TYPE_CHECKBOX_MAIN_STYLE:
               if (_val != null)
                  parentControl.CheckBoxMainStyle = (CheckboxMainStyle)getValueInt();
               break;

            case PropInterface.PROP_TYPE_RADIO_BUTTON_APPEARANCE:
               if (_val != null)
                  parentControl.RadioButtonAppearance = (RbAppearance)getValueInt();
               break;

            default:
               break;
         }
      }

      /// <summary>
      ///   returns a reference to the parent object of this property
      /// </summary>
      protected internal Object getParentObj()
      {
         return _parentObj;
      }

      /// <summary>
      ///   returns the type of the parent object
      /// </summary>
      protected internal char getParentType()
      {
         return _parentType;
      }

      /// <summary>
      ///   returns the id of the property
      /// </summary>
      public int getID()
      {
         return _id;
      }

      /// <summary>
      ///   set the value of the property and take care of the presentation the value must be in a magic internal
      ///   format
      /// </summary>
      /// <param name = "mgVal">the new value</param>
      public void setValue(String mgVal)
      {
         int compIdx = _parentObj.getCompIdx();

         if (_id == PropInterface.PROP_TYPE_DATA || _id == PropInterface.PROP_TYPE_NODE_ID ||
             _id == PropInterface.PROP_TYPE_NODE_PARENTID)
            return;

         if (_dataType == 0)
            setDataType();

         switch (_dataType)
         {
            case StorageAttribute.NUMERIC:
               if (mgVal == null)
                  mgVal = "FF00000000000000000000000000000000000000";
               // TODO: give the correct picture for each kind of property
               _val = DisplayConvertor.Instance.mg2disp(mgVal, "", _pic, compIdx, false).Trim();
               break;

            case StorageAttribute.ALPHA:
            case StorageAttribute.BLOB:
            case StorageAttribute.BLOB_VECTOR:
            case StorageAttribute.BOOLEAN:
            case StorageAttribute.UNICODE:
               _val = mgVal;
               break;
            case StorageAttribute.DOTNET:
               _val = mgVal;
               break;
            default:
               throw new ApplicationException("in Property.setValue() illegal data type: " + _dataType);
         }
      }

      /// <summary>
      /// set an int value directly, as is done when initializing the property
      /// </summary>
      /// <param name="val"></param>
      internal void SetValue(int val)
      {
         _val = val.ToString();
      }

      /// <summary>
      ///   set the value of the property and take care of the presentation the value must be in a magic internal format
      /// </summary>
      protected internal void setOrgValue()
      {
         _orgValue = _val;
      }

      /// <summary>
      /// 
      /// </summary>
      public void SetStudioValueAsOrgValue()
      {
         _orgValue = StudioValue;
      }

      /// <summary>
      ///   Calculates the height of a control or a form using the same algorithm applied in an Online
      ///   program. It is important to use the same algorithm in order to ensure that what we see in
      ///   the form editor is the same in RT.
      /// </summary>
      /// <returns></returns>
      private static int calcHeight(PropParentInterface propInterface, bool calcOrgValue)
      {
         MgFormBase form = propInterface.getForm();
         int height = 0;
         int top = 0;
         int bottom = 0;
         int pixBottom;
         int pixtop;
         int pixHeight;

         Property propHeight = propInterface.getProp(PropInterface.PROP_TYPE_HEIGHT);
         Property propTop = propInterface.getProp(PropInterface.PROP_TYPE_TOP);

         if (calcOrgValue)
         {
            if (propHeight != null && propHeight.getOrgValue() != null)
            {
               height = Int32.Parse(propHeight.getOrgValue());
            }
         }
         else
            height = propHeight.getValueInt();

         if (propInterface is MgControlBase)
         {
            if (propTop != null)
            {
               if (calcOrgValue)
               {
                  if (propTop != null && propTop.getOrgValue() != null)
                     top = Int32.Parse(propTop.getOrgValue());
               }
               else
                  top = propInterface.getProp(PropInterface.PROP_TYPE_TOP).getValueInt();
            }

            bottom = top + height;
            pixBottom = form.uom2pix(bottom, false);
            pixtop = form.uom2pix(top, false);
            pixHeight = pixBottom - pixtop;
         }
         else //if this is a form
         {
            pixHeight = form.uom2pix(height, false);
         }

         return pixHeight;
      }

      /// <summary>
      ///   get original rectangle
      /// </summary>
      /// <returns></returns>
      internal static MgRectangle getOrgRect(PropParentInterface propInterface)
      {
         var rect = new MgRectangle();
         MgFormBase form = propInterface.getForm();

         if (propInterface.getProp(PropInterface.PROP_TYPE_LEFT).getOrgValue() != null)
            rect.x = Int32.Parse(propInterface.getProp(PropInterface.PROP_TYPE_LEFT).getOrgValue());
         if (propInterface.getProp(PropInterface.PROP_TYPE_TOP).getOrgValue() != null)
            rect.y = Int32.Parse(propInterface.getProp(PropInterface.PROP_TYPE_TOP).getOrgValue());

         //rect.width = Int32.Parse(propInterface.getProp(PropInterface.PROP_TYPE_WIDTH).getOrgValue());
         //rect.height = Int32.Parse(propInterface.getProp(PropInterface.PROP_TYPE_HEIGHT).getOrgValue());

         rect = form.uom2pixRect(rect);
         // get coordinates that were defined on the controls originally,
         // i.e. before expression executed
         if (propInterface.getProp(PropInterface.PROP_TYPE_WIDTH).getOrgValue() != null)
            rect.width = calcWidth(propInterface, true);
         if (propInterface.getProp(PropInterface.PROP_TYPE_HEIGHT).getOrgValue() != null)
            rect.height = calcHeight(propInterface, true);
         if (form.DesignerInfoDictionary != null)
         {
            MgControlBase mgControl = propInterface as MgControlBase;
            if (mgControl != null && form.DesignerInfoDictionary.ContainsKey(mgControl))
            {
               Dictionary<string, object> props = form.DesignerInfoDictionary[(MgControlBase)propInterface];
               if (props.ContainsKey(Constants.WinPropWidth))
                  rect.width = (int)props[Constants.WinPropWidth];
               if (props.ContainsKey(Constants.WinPropHeight))
                  rect.height = (int)props[Constants.WinPropHeight];

            }
         }

         return rect;
      }

      /// <summary>
      ///   Calculates the width of a control or a form using the same algorithm applied in an Online
      ///   program. It is important to use the same algorithm in order to ensure that what we see in
      ///   the form editor is the same in RT.
      /// </summary>
      /// <returns></returns>
      private static int calcWidth(PropParentInterface propInterface, bool calcOrgValue)
      {
         MgFormBase form = propInterface.getForm();
         int width = 0;
         int left = 0;
         int right = 0;
         int pixRight;
         int pixLeft;
         int pixWidth;

         MgControlBase control = null;
         if (propInterface is MgControlBase)
            control = (MgControlBase)propInterface;

         Property propWidth = propInterface.getProp(PropInterface.PROP_TYPE_WIDTH);
         Property propLeft = propInterface.getProp(PropInterface.PROP_TYPE_LEFT);

         if (control != null && control.Type == MgControlType.CTRL_TYPE_TABLE && !propWidth.isExpression())
         {
            if (calcOrgValue)
            {
               Property propWindowWidth = propInterface.getProp(PropInterface.PROP_TYPE_WINDOW_WIDTH);
               if (propWindowWidth != null && propWindowWidth.getOrgValue() != null)
                  width = Int32.Parse(propInterface.getProp(PropInterface.PROP_TYPE_WINDOW_WIDTH).getOrgValue());
            }
            else
               // for table without expression width is set by WindowWidth property
               width = control.getProp(PropInterface.PROP_TYPE_WINDOW_WIDTH).getValueInt();
         }
         else
         {
            if (calcOrgValue)
            {
               if (propWidth != null && propWidth.getOrgValue() != null)
                  width = Int32.Parse(propWidth.getOrgValue());
            }
            else
               width = propWidth.getValueInt();
         }

         if (propInterface is MgControlBase && control.Type != MgControlType.CTRL_TYPE_SUBFORM)
         {
            if (calcOrgValue)
            {
               if (propLeft != null && propLeft.getOrgValue() != null)
                  left = Int32.Parse(propLeft.getOrgValue());
            }
            else
               left = propLeft.getValueInt();

            right = width + left;
            pixRight = form.uom2pix(right, true);
            pixLeft = form.uom2pix(left, true);
            pixWidth = pixRight - pixLeft;
         }
         else //if this is a form
         {
            pixWidth = form.uom2pix(width, true);
         }

         return pixWidth;
      }

      /// <summary>
      ///   change the bounds of the object
      /// </summary>
      private void onBounds(int id)
      {
         if (IsDesignerInfoFlagSet(DesignerInfoFlags.IsDesignerValue))
            return;

         MgFormBase form = getForm();
         MgControlBase control = null;
         int x, y, width, height;
         bool addBorderPlace = false;

         x = y = width = height = GuiConstants.DEFAULT_VALUE_INT;

         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
            control = (MgControlBase)_parentObj;
         if (_parentType == GuiConstants.PARENT_TYPE_FORM && form.isSubForm())
         {
            //if subform control has border and it has AUTO_FIT_AS_CALLED_FORM property
            //we must enlarge it, to add to its width and height enough room for the borders
            var autoFit = (AutoFit)form.getSubFormCtrl().getProp(PropInterface.PROP_TYPE_AUTO_FIT).getValueInt();
            bool border = form.getSubFormCtrl().getProp(PropInterface.PROP_TYPE_BORDER).getValueBoolean();
            if (autoFit == AutoFit.AsCalledForm && border)
               addBorderPlace = true;
         }

         switch (id)
         {
            case PropInterface.PROP_TYPE_LEFT:
               x = CalcLeftValue(control);
               break;

            case PropInterface.PROP_TYPE_TOP:
               y = CalcTopValue(control);
               break;

            case PropInterface.PROP_TYPE_WIDTH:
               width = CalcWidthValue(control);
               break;

            case PropInterface.PROP_TYPE_HEIGHT:
               height = CalcHeightValue(control);
               //Owner draw combobox minimum size should be 6, so if the height < 7, don't update it.
               if (control != null && control.isOwnerDrawComboBox() && height < 7)
                  return;
               break;

            default:
               Debug.Assert(false);
               break;
         }

         if (control != null && control.getParent() is MgStatusBar && id == PropInterface.PROP_TYPE_WIDTH)
            Commands.addAsync(CommandType.PROP_SET_SB_PANE_WIDTH, control, 0, 0, 0, width, 0, false, false);
         else
            Commands.addAsync(CommandType.PROP_SET_BOUNDS, getObjectByParentObj(), getLine(), x, y, width, height,
                              addBorderPlace, !_parentObj.IsFirstRefreshOfProps());
      }

      /// <summary>
      /// calculate the height in pixels
      /// </summary>
      /// <param name="control"></param>
      /// <param name="GetValueCallback"></param>
      /// <returns></returns>
      internal int CalcHeightValue(MgControlBase control)
      {
         int height;
         MgFormBase form = getForm();
         /*
       * For Status Bar, PROP_TYPE_HEIGHT holds value in pixels. So, do not re-convert it.
       */
         if (control != null && control is MgStatusBar)
            height = getValueInt();
         else if (_parentType == GuiConstants.PARENT_TYPE_CONTROL && control.isLineControl())
         {
            //for line control 'height' is actually the Y coordinate of the second point.
            //It should not be treated as height and calcHeight() should not be called.
            height = form.uom2pix(getValueInt(), false);
            //If the line is inside a container and no expression is attached to it, then the value is 
            //relative to the form. So, make it relative to its parent.
            if (control.hasContainer() && _expId == 0)
            {
               var parentControl = (MgControlBase)control.getParent();
               int parentY = form.uom2pix(parentControl.getProp(PropInterface.PROP_TYPE_TOP).getValueInt(), false);
               height = height - parentY;
            }
         }
         else
         {
            height = calcHeight(_parentObj, false);

            if (control != null && control.isTableControl())
            {
               if (control.checkProp(PropInterface.PROP_TYPE_BEFORE_900_VERSION, false))
               {
                  bool before900Version = control.getProp(PropInterface.PROP_TYPE_BEFORE_900_VERSION).getValueBoolean();

                  if (before900Version)
                     height += 2;
               }
            }
         }
         return height;
      }

      /// <summary>
      /// calculate the width in pixels
      /// </summary>
      /// <param name="control"></param>
      /// <param name="GetValueCallback"></param>
      /// <returns></returns>
      internal int CalcWidthValue(MgControlBase control)
      {
         int width;
         MgFormBase form = getForm();

         if (control != null && control.Type == MgControlType.CTRL_TYPE_COLUMN)
            width = control.getForm().computeColumnWidth(control.getLayer());
         else if (control != null && control.IsStatusPane()) // statusPane width is already in pixels.
            width = getValueInt();
         else if (_parentType == GuiConstants.PARENT_TYPE_CONTROL && control.isLineControl())
         {
            //for line control 'width' is actually the X coordinate of the second point.
            //It should not be treated as width and calcWidth() should not be called.
            width = form.uom2pix(getValueInt(), true);
            //If the line is inside a container and no expression is attached to it, then the value is 
            //relative to the form. So, make it relative to its parent.
            if (control.hasContainer() && _expId == 0)
            {
               var parentControl = (MgControlBase)control.getParent();
               int parentX = form.uom2pix(parentControl.getProp(PropInterface.PROP_TYPE_LEFT).getValueInt(), true);
               width = width - parentX;
            }
         }
         else
            width = calcWidth(_parentObj, false);
         return width;
      }

      /// <summary>
      /// calculate the Y coordinate in pixels
      /// </summary>
      /// <param name="control"></param>
      /// <param name="GetValueCallback"></param>
      /// <returns></returns>
      internal int CalcTopValue(MgControlBase control)
      {
         int y;
         MgFormBase form = getForm();

         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL && control.hasContainer() && _expId == 0)
         {
            var parentControl = (MgControlBase)control.getParent();
            int parentY = Int32.Parse(parentControl.getProp(PropInterface.PROP_TYPE_TOP).getOrgValue());
            parentY = form.uom2pix(parentY, false);
            y = form.uom2pix(getValueInt(), false);
            y = y - parentY;
         }
         else
            y = form.uom2pix(getValueInt(), false);
         return y;
      }

      /// <summary>
      /// calculate the X coordinate in pixels
      /// </summary>
      /// <param name="control"></param>
      /// <param name="GetValueCallback"></param>
      /// <returns></returns>
      internal int CalcLeftValue(MgControlBase control)
      {
         int x;
         MgFormBase form = getForm();

         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL && control.hasContainer() && _expId == 0)
         {
            var parentControl = (MgControlBase)control.getParent();
            int parentX = Int32.Parse(parentControl.getProp(PropInterface.PROP_TYPE_LEFT).getOrgValue());
            parentX = form.uom2pix(parentX, true);
            x = form.uom2pix(getValueInt(), true);
            x = x - parentX;
         }
         else
            x = form.uom2pix(getValueInt(), true);
         return x;
      }

      /// <summary>
      ///   Returns the form related to this property. If the property's parent is the form then return it,
      ///   otherwise, if the property's parent is a control then return the form to which this control belongs.
      /// </summary>
      /// <returns> MgForm</returns>
      private MgFormBase getForm()
      {
         MgFormBase form = null;

         if (_parentType == GuiConstants.PARENT_TYPE_FORM)
            form = (MgFormBase)_parentObj;
         else if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
            form = ((MgControlBase)_parentObj).getForm();

         return form;
      }

      /// <summary>
      /// </summary>
      /// <returns> the task by the parent object</returns>
      internal TaskBase GetTaskByParentObject()
      {
         TaskBase task = null;
         if (_parentType == GuiConstants.PARENT_TYPE_FORM)
            task = ((MgFormBase)_parentObj).getTask();
         else if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
            task = ((MgControlBase)_parentObj).getForm().getTask();
         else if (_parentType == GuiConstants.PARENT_TYPE_TASK)
            task = (TaskBase)_parentObj;
         return task;
      }

      /// <summary>
      ///   refresh the display for control properties and form properties only
      /// </summary>
      /// <param name = "forceRefresh">if true then refresh is forced regardless of the previous value</param>
      public void RefreshDisplay(bool forceRefresh)
      {
         RefreshDisplay(forceRefresh, Int32.MinValue);
      }

      /// <summary>
      ///   return TRUE if the property need to be skip from the refresh properties. for a control
      /// </summary>
      /// <returns></returns>
      private bool ShouldSkipRefreshControl()
      {
         bool skip = false;
         switch (_id)
         {
            case PropInterface.PROP_TYPE_HEBREW:
               var mgControl = (MgControlBase)_parentObj;
               if (mgControl.isTableControl())
                  skip = true;
               break;
            case PropInterface.PROP_TYPE_NODE_ID:
            case PropInterface.PROP_TYPE_NODE_PARENTID:
            case PropInterface.PROP_TYPE_DATA:
            case PropInterface.PROP_TYPE_HIGHLIGHTING:
            case PropInterface.PROP_TYPE_MUST_INPUT:
            case PropInterface.PROP_TYPE_PROMPT:
            case PropInterface.PROP_TYPE_HELP_SCR:
            case PropInterface.PROP_TYPE_SELECT_PROGRAM:
            case PropInterface.PROP_TYPE_SELECT_MODE:
            case PropInterface.PROP_TYPE_ALLOWED_DIRECTION:
            case PropInterface.PROP_TYPE_RETURN_ACTION:
            case PropInterface.PROP_TYPE_ATTRIBUTE:
            case PropInterface.PROP_TYPE_REPEATABLE:
            case PropInterface.PROP_TYPE_FRAME_NAME:
            case PropInterface.PROP_TYPE_TRIGGER:
            case PropInterface.PROP_TYPE_TAB_ORDER:
            //case PropInterface.PROP_TYPE_CHECKBOX_MAIN_STYLE:
            //case PropInterface.PROP_TYPE_TAB_CONTROL_SIDE:
            //case PropInterface.PROP_TYPE_MULTILINE:
            //case PropInterface.PROP_TYPE_BORDER:
            //case PropInterface.PROP_TYPE_PASSWORD:
            //case PropInterface.PROP_TYPE_HORIZONTAL_ALIGNMENT:
            //case PropInterface.PROP_TYPE_STYLE_3D:
            case PropInterface.PROP_TYPE_IMAGE_STYLE:
            case PropInterface.PROP_TYPE_IS_CACHED:
            case PropInterface.PROP_TYPE_LAYER:
            case PropInterface.PROP_TYPE_AUTO_FIT:
            //case PropInterface.PROP_TYPE_MULTILINE_VERTICAL_SCROLL:
            //case PropInterface.PROP_TYPE_MULTILINE_WORDWRAP_SCROLL:
            case PropInterface.PROP_TYPE_RAISE_AT:
            case PropInterface.PROP_TYPE_FRAMESET_STYLE:
            case PropInterface.PROP_TYPE_FRAME_TYPE:
            case PropInterface.PROP_TYPE_RANGE:
            case PropInterface.PROP_TYPE_WINDOW_WIDTH:
            case PropInterface.PROP_TYPE_WALLPAPER_STYLE:
            case PropInterface.PROP_TYPE_TABBING_ORDER:
            case PropInterface.PROP_TYPE_AUTO_REFRESH:
            case PropInterface.PROP_TYPE_PARAMETERS:
            case PropInterface.PROP_TYPE_OBJECT_TYPE:
            case PropInterface.PROP_TYPE_RETAIN_FOCUS:
            case PropInterface.PROP_TYPE_SUBFORM_TYPE:
            case PropInterface.PROP_TYPE_DOTNET_OBJECT:
            case PropInterface.PROP_TYPE_DN_CONTROL_VALUE_PROPERTY:
            case PropInterface.PROP_TYPE_REAL_OBJECT_TYPE:
            case PropInterface.PROP_TYPE_ASSEMBLY_ID:
            case PropInterface.PROP_TYPE_ADDITIONAL_INFORMATION:
            case PropInterface.PROP_TYPE_ROW_PLACEMENT:
            case PropInterface.PROP_TYPE_DATAVIEWCONTROL:
            case PropInterface.PROP_TYPE_DATAVIEWCONTROL_FIELDS:
            case PropInterface.PROP_TYPE_DN_CONTROL_DATA_SOURCE_PROPERTY:
            case PropInterface.PROP_TYPE_DN_CONTROL_DISPLAY_MEMBER_PROPERTY:
            case PropInterface.PROP_TYPE_DN_CONTROL_VALUE_MEMBER_PROPERTY:
            case PropInterface.PROP_TYPE_PRGTSK_NUM:
            case PropInterface.PROP_TYPE_INDEX:
            case PropInterface.PROP_TYPE_DISPLAY_FIELD:
            case PropInterface.PROP_TYPE_LINK_FIELD:
            case PropInterface.PROP_TYPE_BEFORE_900_VERSION:
               skip = true;
               break;

            case PropInterface.PROP_TYPE_BUTTON_STYLE:
               if (!((MgControlBase)_parentObj).isRadio())
                  skip = true;
               break;

            case PropInterface.PROP_TYPE_DN_CONTROL_VALUE_CHANGED_EVENT:
            case PropInterface.PROP_TYPE_TITLE_PADDING:
               if (!_parentObj.IsFirstRefreshOfProps())
                  skip = true;
               break;

            case PropInterface.PROP_TYPE_HEIGHT:
               //Ignore the height set by the developer for 3D combobox
               if (((MgControlBase)_parentObj).isComboBox() && !((MgControlBase)_parentObj).isOwnerDrawComboBox())
                  skip = true;
               break;

            case PropInterface.PROP_TYPE_BORDER_COLOR:
               if (!((MgControlBase)_parentObj).isGroup())
                  skip = true;
               break;

            default:
               break;
         }

         if (!skip)
         {
            // for tree control the show lines is a style
            var mgControl = (MgControlBase)_parentObj;
            if (mgControl.isTreeControl())
            {
               if (isRepeatableInTree(_id))
               {
                  if (mgControl.getForm().getTask().DataView.isEmptyDataview())
                  {
                     //When emptyDataview is on - the tree control has no rows - this is different from any other control
                     //which always has at least one row

                     skip = true;
                  }
                  // for tree control, do not refresh repeatable properties if the mgTree has not been created yet.
                  // the lines are not created at this stage
                  if (mgControl.getForm().getMgTree() == null)
                     skip = true;
               }

            }
            else if (mgControl.isFrameSet())
            {
               //for frame set control refresh the only needs properties 
               skip = true;

               switch (_id)
               {
                  case PropInterface.PROP_TYPE_WIDTH:
                  case PropInterface.PROP_TYPE_HEIGHT:
                  case PropInterface.PROP_TYPE_HORIZONTAL_PLACEMENT:
                  case PropInterface.PROP_TYPE_VERTICAL_PLACEMENT:
                  case PropInterface.PROP_TYPE_VISIBLE:
                     skip = false;
                     break;
               }
            }
            else if (mgControl.isContainerControl())
            {
               switch (_id)
               {
                  case PropInterface.PROP_TYPE_WIDTH:
                  case PropInterface.PROP_TYPE_HEIGHT:
                  case PropInterface.PROP_TYPE_LEFT:
                  case PropInterface.PROP_TYPE_TOP:
                     skip = true;
                     break;
               }
            }
         }
         return skip;
      }

      /// <summary>
      ///   return TRUE if the property need to be skip from the refresh properties. for a form
      /// </summary>
      /// <returns></returns>
      private bool ShouldSkipRefreshForm()
      {
         Debug.Assert(_parentType == GuiConstants.PARENT_TYPE_FORM);

         bool skip = false;
         MgFormBase mgFormBase = (MgFormBase)_parentObj;

         //Compute properties: all those properties will be refresh only one time
         switch (_id)
         {
            case PropInterface.PROP_TYPE_PULLDOWN_MENU:
            case PropInterface.PROP_TYPE_SYSTEM_MENU:
            case PropInterface.PROP_TYPE_MINBOX:
            case PropInterface.PROP_TYPE_MAXBOX:
            case PropInterface.PROP_TYPE_WINDOW_TYPE:
            case PropInterface.PROP_TYPE_STARTUP_POSITION:
            case PropInterface.PROP_TYPE_SHOW_IN_WINDOW_MENU:
               if (!_parentObj.IsFirstRefreshOfProps())
                  skip = true;
               break;

            case PropInterface.PROP_TYPE_UOM:
            case PropInterface.PROP_TYPE_HOR_FAC:
            case PropInterface.PROP_TYPE_VER_FAC:
            case PropInterface.PROP_TYPE_BORDER_STYLE:
            case PropInterface.PROP_TYPE_WALLPAPER_STYLE:
            case PropInterface.PROP_TYPE_LINES_IN_TABLE:
            case PropInterface.PROP_TYPE_HELP_SCR:
            case PropInterface.PROP_TYPE_DISPLAY_MENU:
            case PropInterface.PROP_TYPE_DISPLAY_TOOLBAR:
            case PropInterface.PROP_TYPE_TABBING_ORDER:
            case PropInterface.PROP_TYPE_ADDITIONAL_INFORMATION:
            case PropInterface.PROP_TYPE_PERSISTENT_FORM_STATE_VERSION:
               skip = true;
               break;

            case PropInterface.PROP_TYPE_STARTUP_MODE:   // Startup mode is applicable only to SDI & MDI Frame.
               if (mgFormBase.isSubForm() || !mgFormBase.IsMDIOrSDIFrame)
                  skip = true;
               break;

            case PropInterface.PROP_TYPE_LEFT:
            case PropInterface.PROP_TYPE_TOP:
            case PropInterface.PROP_TYPE_WIDTH:
            case PropInterface.PROP_TYPE_HEIGHT:
               skip = ShouldSkipNavigationProperties();
               break;

            default:
               break;
         }

         // refresh only the relevant subform properties (color, font, etc.)
         if (!skip && mgFormBase.isSubForm())
         {
            skip = true;
            switch (_id)
            {
               case PropInterface.PROP_TYPE_LEFT:
               case PropInterface.PROP_TYPE_TOP:
               case PropInterface.PROP_TYPE_WIDTH:
               case PropInterface.PROP_TYPE_HEIGHT:
                  MgControlBase mgControl = mgFormBase.getSubFormCtrl();
                  var autoFit = (AutoFit)mgControl.getProp(PropInterface.PROP_TYPE_AUTO_FIT).getValueInt();
                  switch (autoFit)
                  {
                     case AutoFit.None:
                     case AutoFit.AsControl:
                        skip = true;
                        break;
                     case AutoFit.AsCalledForm:
                        // need to set the bounds as the called form only for the width and height
                        skip = false;
                        if (_id == PropInterface.PROP_TYPE_LEFT || _id == PropInterface.PROP_TYPE_TOP)
                           skip = true;
                        break;
                  }
                  break;

               case PropInterface.PROP_TYPE_FONT:
               case PropInterface.PROP_TYPE_COLOR:
               case PropInterface.PROP_TYPE_GRADIENT_COLOR:
               case PropInterface.PROP_TYPE_GRADIENT_STYLE:
               case PropInterface.PROP_TYPE_WALLPAPER:
               case PropInterface.PROP_TYPE_DEFAULT_BUTTON:
               case PropInterface.PROP_TYPE_CONTEXT_MENU:
               case PropInterface.PROP_TYPE_HORIZONTAL_PLACEMENT:
               case PropInterface.PROP_TYPE_VERTICAL_PLACEMENT:
               case PropInterface.PROP_TYPE_ALLOW_DROPPING:
                  skip = false;
                  break;
            }
         }
         return skip;
      }

      /// <summary>
      ///   return TRUE if the property need to be skip from the refresh properties. for a task
      /// </summary>
      /// <returns></returns>
      private bool ShouldSkipRefreshTask()
      {
         bool skip = false;

         switch (_id)
         {
            case PropInterface.PROP_TYPE_ALLOW_CREATE:
            case PropInterface.PROP_TYPE_ALLOW_DELETE:
            case PropInterface.PROP_TYPE_ALLOW_MODIFY:
            case PropInterface.PROP_TYPE_ALLOW_QUERY:
            case PropInterface.PROP_TYPE_ALLOW_RANGE:
            case PropInterface.PROP_TYPE_ALLOW_LOCATE:
            case PropInterface.PROP_TYPE_ALLOW_SORT:
            case PropInterface.PROP_TYPE_TASK_PROPERTIES_ALLOW_INDEX:
            case PropInterface.PROP_TYPE_TABBING_CYCLE:
            case PropInterface.PROP_TYPE_CONFIRM_CANCEL:
            case PropInterface.PROP_TYPE_CONFIRM_UPDATE:
            case PropInterface.PROP_TYPE_END_CONDITION:
            case PropInterface.PROP_TYPE_EVAL_END_CONDITION:
            case PropInterface.PROP_TYPE_FORCE_SUFFIX:
            case PropInterface.PROP_TYPE_FORCE_DELETE:
            case PropInterface.PROP_TYPE_TASK_MODE:
            case PropInterface.PROP_TYPE_SELECTION:
            case PropInterface.PROP_TYPE_TRASACTION_BEGIN:
            case PropInterface.PROP_TYPE_TASK_PROPERTIES_TRANSACTION_MODE:
            case PropInterface.PROP_TYPE_PRINT_DATA:
            case PropInterface.PROP_TYPE_CLOSE_TASKS_BY_MDI_MENU:
            case PropInterface.PROP_TYPE_TASK_PROPERTIES_OPEN_TASK_WINDOW:
            case PropInterface.PROP_TYPE_TASK_PROPERTIES_ALLOW_EVENTS:
            case PropInterface.PROP_TYPE_TASK_PROPERTIES_CHUNK_SIZE:
            case PropInterface.PROP_TYPE_TASK_PROPERTIES_INDEX_OPTIMIZATION:
            case PropInterface.PROP_TYPE_TASK_PROPERTIES_RANGE:
            case PropInterface.PROP_TYPE_TASK_PROPERTIES_RANGE_ORDER:
            case PropInterface.PROP_TYPE_TASK_PROPERTIES_LOCATE:
            case PropInterface.PROP_TYPE_TASK_PROPERTIES_LOCATE_ORDER:
            case PropInterface.PROP_TYPE_TASK_PROPERTIES_POSITION:
            case PropInterface.PROP_TYPE_TASK_PROPERTIES_POSITION_USAGE:
            case PropInterface.PROP_TYPE_TASK_PROPERTIES_SQL_RANGE:
               skip = true;
               break;

            default:
               break;
         }
         return skip;
      }

      /// <summary>
      ///   check if the property should be ignored by RefreshDisplay() skip properties which has nothing to do with
      ///   the display
      /// </summary>
      /// <returns> true if the property should be ignored</returns>
      private bool ShouldSkipRefresh()
      {
         bool skip = false;
         switch (_parentType)
         {
            case GuiConstants.PARENT_TYPE_CONTROL:
               skip = ShouldSkipRefreshControl();
               break;

            case GuiConstants.PARENT_TYPE_FORM:
               skip = ShouldSkipRefreshForm();
               break;

            case GuiConstants.PARENT_TYPE_TASK:
               skip = ShouldSkipRefreshTask();
               break;

            default:
               break;
         }

         switch (_id)
         {
            case PropInterface.PROP_TYPE_POP_UP:
            case PropInterface.PROP_TYPE_ORIENTATION_LOCK:
            case PropInterface.PROP_TYPE_ENTER_ANIMATION:
            case PropInterface.PROP_TYPE_EXIT_ANIMATION:
            case PropInterface.PROP_TYPE_NAVIGATION_DRAWER_MENU:
            case PropInterface.PROP_TYPE_ACTION_BAR_MENU:
            case PropInterface.PROP_TYPE_TITLE_BAR_COLOR:
            case PropInterface.PROP_TYPE_MOBILE_BORDER_WIDTH:
            case PropInterface.PROP_TYPE_BORDER_FOCUS_WIDTH:
            case PropInterface.PROP_TYPE_BORDER_FOCUS_COLOR:
            case PropInterface.PROP_TYPE_CORNER_RADIUS:
            case PropInterface.PROP_TYPE_OPEN_PICKER:
            case PropInterface.PROP_TYPE_OPEN_EDIT_DIALOG:
            case PropInterface.PROP_TYPE_DEFAULT_ALIGNMENT:
            case PropInterface.PROP_TYPE_KEYBOARD_TYPE:
            case PropInterface.PROP_TYPE_KEYBOARD_RETURN_KEY:
            case PropInterface.PROP_TYPE_ALLOW_SUGGESTIONS:
            case PropInterface.PROP_TYPE_MOBILE_IMAGE_LIST_FILE_NAME:
               //The properties are only relevant for Mobile as for now, hence skip the refresh.   
               skip = true;
               break;
         }

         return skip;
      }

      /// <summary>
      ///   refresh the display for control properties and form properties only
      /// </summary>
      /// <param name = "forceRefresh">if true then refresh is forced regardless of the previous value</param>
      /// <param name = "currLine">line number in table when refreshing a column if the current line is refreshed set to
      ///   (Integer.MIN_VALUE)
      /// </param>
      public void RefreshDisplay(bool forceRefresh, int currLine)
      {
         RefreshDisplay(forceRefresh, currLine, true);
      }

      //private void BuildProperties(MgControlBase ctrl, int line)
      //{
      //   string name;
      //   string value;
      //   Dictionary<string, string> updatedProperties = new Dictionary<string, string>();
      //   switch (_id)
      //   {
      //      case PropInterface.PROP_TYPE_TEXT:

      //      case PropInterface.PROP_TYPE_VISIBLE:
      //         name = "visibility";
      //         value = getValueBoolean() ? "visible" : "hidden";
      //         break;
      //      case PropInterface.PROP_TYPE_ENABLED:
      //      case PropInterface.PROP_TYPE_FORMAT:
      //         ctrl = _parentObj as MgControlBase;
      //         ControlsData controlsData = ctrl.GetControlsData(line);

      //         if (!controlsData.ControlsMetaData.ContainsKey(ctrl.UniqueWebId))
      //            controlsData.ControlsMetaData[ctrl.UniqueWebId] = new ControlMetaData();
      //         controlsData.ControlsMetaData[ctrl.UniqueWebId].Properties[_id.ToString()] = _val;
      //         //TODO : move to builder
      //         //controlsData.ControlsMetaData[ctrl.UniqueWebId].Type = ((char)ctrl.Type).ToString();
      //         break;

      //   }
      //}
      //static Dictionary<int, string> TSprops =
      //{

      //}

      //String GetTSPropertyName()
      //{

      //}

      /// <summary>
      /// Reevaluate the property value (if it has an expression) and
      /// refresh the display for control properties and form properties only
      /// TODO: Separate reevaluation and refresh display.
      /// This method was (as its name implies) relevant only for GUI properties, i.e. properties that affect on the display. 
      /// Non-GUI properties (such as PROP_TYPE_TAB_IN) were skipped and not reevaluated. 
      /// The distinction between GUI properties and non-GUI properties is done by the method �ShouldSkipRefresh�E
      /// Now we have non-GUI properties that are reevaluated in this function: 
      /// PROP_TYPE_MODIFY_IN_QUERY, PROP_TYPE_CONTEXT_MENU, PROP_TYPE_TAB_IN, PROP_TYPE_PARK_ON_CLICK.
      /// For these properties we do not execute explicitly their expressions (it was done in 1.9 by CheckProp function),
      /// we only use their values by GetComputedBooleanProperty function. It is done for do not execute the property expression
      /// in GUI thread. In GUI thread we only use the property value. So it is needed to reevaluate the property value.
      /// </summary>
      /// <param name = "forceRefresh">if true then refresh is forced regardless of the previous value</param>
      /// <param name = "currLine">line number in table when refreshing a column if the current line is refreshed set to
      ///   (Integer.MIN_VALUE)
      /// </param>
      public void RefreshDisplay(bool forceRefresh, int currLine, bool checkSkipRefresh)
      {
         MgControlBase ctrl = null;
         int line = 0;
         String prevValue;
         bool valChanged = true;

         if (_expId == 0)
            return;
         if (_val == null && _expId == 0)
         {
            if (!IsGeneric && _dataType != StorageAttribute.DOTNET)
               Events.WriteExceptionToLog("in Property.RefreshDisplay() null value and expression");
            return;
         }

         if (checkSkipRefresh && ShouldSkipRefresh())
            return;

         if (_prevValues.Count == 0)
            setPrevArraySize();

         ComputeValue();
        

         // before update the property need to prepare data
         switch (_parentType)
         {
            case GuiConstants.PARENT_TYPE_CONTROL:
               ctrl = (MgControlBase)_parentObj;
               // recompute the PIC for format properties
               if (_id == PropInterface.PROP_TYPE_FORMAT && _expId > 0)
                  ctrl.computePIC(_val);

               if (ctrl.IsRepeatable && !ctrl.getForm().isRefreshRepeatableAllowed())
                  return;

               if (currLine == Int32.MinValue)
                  line = getLine();
               else
                  line = currLine;
               // check if the value of the property was changed to prevent sending redundant
               // javascript commands to the browser
               prevValue = ((String)_prevValues[line]);
               bool dotNetObjectChanged = false;
               Object newDotNetObj = null;
               if (IsGeneric && _dataType == StorageAttribute.DOTNET && _val != null)
               {
                  int key = BlobType.getKey(_val);
                  if (key > 0)
                  {
                     newDotNetObj = DNManager.getInstance().DNObjectsCollection.GetDNObj(key);
                     dotNetObjectChanged = newDotNetObj != _prevDotNetValue;
                  }
               }

               //Fixed bug #:160305 & 427851 while control is child of frame set force refresh for w\h
               if (ctrl.IsDirectFrameChild)
               {
                  if (ctrl.ForceRefreshPropertyWidth && _id == PropInterface.PROP_TYPE_WIDTH)
                  {
                     ctrl.ForceRefreshPropertyWidth = false;
                     forceRefresh = true;
                  }
                  else
                     if (ctrl.ForceRefreshPropertyHight && _id == PropInterface.PROP_TYPE_HEIGHT)
                  {
                     ctrl.ForceRefreshPropertyHight = false;
                     forceRefresh = true;
                  }
               }

               if (!forceRefresh && !dotNetObjectChanged
                   && ((Object)_val == (Object)prevValue || _val != null && _val.Equals(prevValue))
                   && _id != PropInterface.PROP_TYPE_MODIFIABLE
                   && _id != PropInterface.PROP_TYPE_ALLOW_PARKING
                   )
                  return;

               //to prevent recursion while updating column visibility
               if (ctrl.Type == MgControlType.CTRL_TYPE_COLUMN
                   && _id == PropInterface.PROP_TYPE_VISIBLE
                   && _val != null && _val.Equals(prevValue))
                  return;

               if (_val == prevValue || (_val != null && _val.Equals(prevValue)))
                  valChanged = false;

               _prevValues[line] = _val;
               if (IsGeneric && _dataType == StorageAttribute.DOTNET)
                  _prevDotNetValue = newDotNetObj;
               //if (_expId != 0) //properties without expression should be generated in the studio and not updated
               if (valChanged && ! String.IsNullOrEmpty(ctrl.Name)) // if no name - no need to update
               {
                  switch (_id)
                  {
                     case PropInterface.PROP_TYPE_TEXT:
                     case PropInterface.PROP_TYPE_VISIBLE:
                     case PropInterface.PROP_TYPE_ENABLED:
                     case PropInterface.PROP_TYPE_FORMAT:
                        ctrl = _parentObj as MgControlBase;
                        ControlsData controlsData = ctrl.GetControlsData(line);

                        if (!controlsData.ControlsMetaData.ContainsKey(ctrl.UniqueWebId))
                           controlsData.ControlsMetaData[ctrl.UniqueWebId] = new ControlMetaData();
                        controlsData.ControlsMetaData[ctrl.UniqueWebId].Properties[_id.ToString()] = _val;
                        //TODO : move to builder
                        //controlsData.ControlsMetaData[ctrl.UniqueWebId].Type = ((char)ctrl.Type).ToString();
                        break;

                  }
               }
              

               break;

            case GuiConstants.PARENT_TYPE_FORM:
               // check if the value of the property was changed to prevent sending redundant
               // javascript commands to the browser
               prevValue = ((String)_prevValues[0]);
               if (!forceRefresh && (_val == prevValue || _val != null && _val.Equals(prevValue)))
                  return;
               _prevValues[0] = _val;
               break;

            case GuiConstants.PARENT_TYPE_TASK: // TASK PROPERTIES
               Events.WriteExceptionToLog(
                  string.Format("Property.RefreshDisplay(): task property {0} wasn't handled", _id));
               return;

            default:
               Events.WriteExceptionToLog(
                  string.Format("Property.RefreshDisplay(): parentType unknown, property {0} wasn't handled", _id));
               return;
         }
        


         // Rinat RefreshDisplay for SWT
         switch (_id)
         {
            case PropInterface.PROP_TYPE_DN_CONTROL_VALUE_CHANGED_EVENT:
               OnRegisterDNControlValueChangedEvent();
               break;

            case PropInterface.PROP_TYPE_LEFT:
            case PropInterface.PROP_TYPE_TOP:
            case PropInterface.PROP_TYPE_WIDTH:
            case PropInterface.PROP_TYPE_HEIGHT:
               onBounds(_id);
               break;

            case PropInterface.PROP_TYPE_WALLPAPER:
               onWallpaper();
               break;

            case PropInterface.PROP_TYPE_IMAGE_FILENAME:
               onImageFileName();
               break;

            case PropInterface.PROP_TYPE_IMAGE_LIST_INDEXES:
               onImageIdxList();
               break;

            case PropInterface.PROP_TYPE_COLOR:
               onColor();
               break;

            case PropInterface.PROP_TYPE_BORDER_COLOR:
               onBorderColor();
               break;

            case PropInterface.PROP_TYPE_GRADIENT_COLOR:
               onGradientColor();
               break;

            case PropInterface.PROP_TYPE_GRADIENT_STYLE:
               onGradientStyle();
               break;

            case PropInterface.PROP_TYPE_VISITED_COLOR:
               onVisitedColor();
               break;

            case PropInterface.PROP_TYPE_FOCUS_COLOR:
               onFocuseColor();
               break;

            case PropInterface.PROP_TYPE_HOVERING_COLOR:
               onHoveringColor();
               break;

            case PropInterface.PROP_TYPE_ALTERNATING_BG_COLOR:
               onAlternatingColor();
               break;

            case PropInterface.PROP_TYPE_TITLE_COLOR:
               onTitleColor();
               break;

            case PropInterface.PROP_TYPE_HOT_TRACK_COLOR:
               onHotTrackColor();
               break;

            case PropInterface.PROP_TYPE_SELECTED_TAB_COLOR:
               onSelectedTabColor();
               break;

            case PropInterface.PROP_TYPE_DIVIDER_COLOR:
               onDividerColor();
               break;

            case PropInterface.PROP_TYPE_SET_COLOR_BY:
               onColorBy();
               break;

            case PropInterface.PROP_TYPE_FONT:
               onFont();
               break;

            case PropInterface.PROP_TYPE_ENABLED:
               onEnable(valChanged);
               break;

            case PropInterface.PROP_TYPE_VISIBLE:
               onVisible(valChanged);
               break;

            case PropInterface.PROP_TYPE_HORIZONTAL_PLACEMENT:
               onHorizontalPlacement();
               break;

            case PropInterface.PROP_TYPE_VERTICAL_PLACEMENT:
               onVerticalPlacement();
               break;

            case PropInterface.PROP_TYPE_NAME:
               OnControlName();
               break;

            case PropInterface.PROP_TYPE_FORM_NAME:
               onFormName();
               break;

            case PropInterface.PROP_TYPE_COLUMN_TITLE:
               onText(0);
               break;

            case PropInterface.PROP_TYPE_TEXT:
               if (!forceRefresh && _parentType == GuiConstants.PARENT_TYPE_CONTROL &&
                   _parentObj.IsFirstRefreshOfProps()
                   && ((MgControlBase)_parentObj).Type == MgControlType.CTRL_TYPE_RICH_TEXT)
               {
                  // If the RTF text cannot fit in the client area, scrollbars are added to the RichText control.
                  // In our case, we set the size to 0,0 in the constructor of the MgRichTextBox.
                  // So, if the TEXT prop is refreshed before WIDTH and HEIGHT, the size is 0,0 and so the scrollbars are added.
                  // Hence, we should not set the TEXT unless WIDTH and HEIGHT props are refreshed.
                  // This is achieved by skipping the refresh of the TEXT property here and then refreshing it in 
                  // MgControl.RefreshDisplay() (after all props are refreshed).
                  break;
               }
               else
                  onText(line);
               break;

            case PropInterface.PROP_TYPE_HINT:
               onHint();
               break;

            case PropInterface.PROP_TYPE_HINT_COLOR:
               onHintColor();
               break;

            case PropInterface.PROP_TYPE_ALLOW_PARKING:
            case PropInterface.PROP_TYPE_MODIFIABLE:
               onNavigation(valChanged);
               break;

            case PropInterface.PROP_TYPE_TOOLTIP:
               onTooltip();
               break;

            case PropInterface.PROP_TYPE_DISPLAY_LIST:
               onDisplayList(line);
               break;

            case PropInterface.PROP_TYPE_VISIBLE_LAYERS_LIST:
               onVisibleLayerList(line);
               break;

            case PropInterface.PROP_TYPE_SELECTION_ROWS:
               onSelectionRows();
               break;

            case PropInterface.PROP_TYPE_FORMAT:
               onFormat();
               break;

            case PropInterface.PROP_TYPE_LABEL:
               onLabel(line);
               break;

            case PropInterface.PROP_TYPE_MINIMUM_HEIGHT:
               onMinimumHeight();
               break;

            case PropInterface.PROP_TYPE_MINIMUM_WIDTH:
               onMinimumWidth();
               break;

            case PropInterface.PROP_TYPE_DEFAULT_BUTTON:
               onDefaultButton();
               break;

            case PropInterface.PROP_TYPE_VISIBLE_LINES:
               onVisibleLines();
               break;

            case PropInterface.PROP_TYPE_CHOICE_COLUMNS:
               onChoiceColumn(line);
               break;

            case PropInterface.PROP_TYPE_PLACEMENT:
               onPlacement();
               break;

            case PropInterface.PROP_TYPE_SHOW_LINES:
               onShowLines();
               break;

            case PropInterface.PROP_TYPE_ALLOW_COL_RESIZE:
               onAllowColumnResiz();
               break;

            case PropInterface.PROP_TYPE_ROW_HEIGHT:
               onRowHeight();
               break;

            case PropInterface.PROP_TYPE_TITLE_HEIGHT:
               onTitleHeight();
               break;

            case PropInterface.PROP_TYPE_BOTTOM_POSITION_INTERVAL:
               onBottomPositionInterval();
               break;

            case PropInterface.PROP_TYPE_ALLOW_REORDER:
               onAllowReOrder();
               break;

            case PropInterface.PROP_TYPE_SORT_COLUMN:
               onColumnSortable();
               break;

            case PropInterface.PROP_TYPE_COL_ALLOW_FILTERING:
               onColumnFilterable();
               break;

            case PropInterface.PROP_TYPE_COLUMN_PLACEMENT:
               onColumnPlacement();
               break;

            case PropInterface.PROP_TYPE_ROW_HIGHLIGHT_COLOR:
               onRowHighlightColor();
               break;
            case PropInterface.PROP_TYPE_INACTIVE_ROW_HIGHLIGHT_COLOR:
               onIncactiveRowHighlightColor();
               break;

            case PropInterface.PROP_TYPE_STARTUP_MODE:
               onStartupMode();
               break;

            case PropInterface.PROP_TYPE_PULLDOWN_MENU:
               refreshPulldownMenu();
               break;

            case PropInterface.PROP_TYPE_DISPLAY_STATUS_BAR:
               onDisplayStatusBar();
               break;

            case PropInterface.PROP_TYPE_AUTO_WIDE:
               onAutoWide();
               break;

            case PropInterface.PROP_TYPE_EXPANDED_IMAGEIDX:
               onExpandedImageIdx();
               break;

            case PropInterface.PROP_TYPE_COLLAPSED_IMAGEIDX:
               onCollapsedImageIdx();
               break;

            case PropInterface.PROP_TYPE_PARKED_COLLAPSED_IMAGEIDX:
               onParkedCollapsedImageIdx();
               break;

            case PropInterface.PROP_TYPE_PARKED_IMAGEIDX:
               onParkedImageIdx();
               break;

            case PropInterface.PROP_TYPE_TRANSLATOR:
               onTranslator(); // JPN: IME support
               break;

            case PropInterface.PROP_TYPE_HORIZONTAL_ALIGNMENT:
               onHorizantalAlignment();
               break;

            case PropInterface.PROP_TYPE_VERTICAL_ALIGNMENT:
               onVerticalAlignment();
               break;

            case PropInterface.PROP_TYPE_RADIO_BUTTON_APPEARANCE:
               onRadioButtonAppearance();
               break;

            case PropInterface.PROP_TYPE_MULTILINE:
               onMultiline();
               break;

            case PropInterface.PROP_TYPE_THREE_STATES:
               onThreeStates();
               break;

            case PropInterface.PROP_TYPE_PASSWORD:
               onPassword();
               break;

            case PropInterface.PROP_TYPE_MULTILINE_WORDWRAP_SCROLL:
               onMultilineWordWrapScroll();
               break;

            case PropInterface.PROP_TYPE_MULTILINE_VERTICAL_SCROLL:
               onMultilineVerticalScroll();
               break;

            case PropInterface.PROP_TYPE_MULTILINE_ALLOW_CR:
               onMuliLineAllowCR();
               break;
            case PropInterface.PROP_TYPE_BORDER_STYLE:
               onBorderStyle();
               break;
            case PropInterface.PROP_TYPE_STYLE_3D:
               onStyle3D();
               break;

            case PropInterface.PROP_TYPE_CHECKBOX_MAIN_STYLE:
               onCheckBoxMainStyle();
               break;

            case PropInterface.PROP_TYPE_BORDER:
               onBorder();
               break;

            case PropInterface.PROP_TYPE_MINBOX:
               onMinBox();
               break;

            case PropInterface.PROP_TYPE_MAXBOX:
               onMaxBox();
               break;

            case PropInterface.PROP_TYPE_STARTUP_POSITION:
               onStartupPosition();
               break;

            case PropInterface.PROP_TYPE_SYSTEM_MENU:
               onSystemMenu();
               break;

            case PropInterface.PROP_TYPE_TITLE_BAR:
               onTitleBar();
               break;

            case PropInterface.PROP_TYPE_WINDOW_TYPE:
               onWindowType();
               break;

            case PropInterface.PROP_TYPE_HEBREW:
               onRightToLeft();
               break;

            case PropInterface.PROP_TYPE_SHOW_FULL_ROW:
               onShowFullRow();
               break;

            case PropInterface.PROP_TYPE_SHOW_BUTTONS:
               onShowButtons();
               break;
            case PropInterface.PROP_TYPE_LINES_AT_ROOT:
               onLinesAsRoot();
               break;

            case PropInterface.PROP_TYPE_TRACK_SELECTION:
            case PropInterface.PROP_TYPE_HOT_TRACK:
               onHotTrack();
               break;

            case PropInterface.PROP_TYPE_TOP_BORDER:
               onTopBorder();
               break;

            case PropInterface.PROP_TYPE_RIGHT_BORDER:
               onRightBorder();
               break;

            case PropInterface.PROP_TYPE_SCROLL_BAR:
               onShowScrollbar();
               break;

            case PropInterface.PROP_TYPE_COLUMN_DIVIDER:
               onColumnDivider();
               break;

            case PropInterface.PROP_TYPE_LINE_DIVIDER:
               onLineDivider();
               break;

            case PropInterface.PROP_TYPE_ROW_HIGHLIGHT_STYLE:
               onRowHighLightType();
               break;

            case PropInterface.PROP_TYPE_LINE_STYLE:
               onLineStyle();
               break;

            case PropInterface.PROP_TYPE_LINE_WIDTH:
               onLineWidth();
               break;

            case PropInterface.PROP_TYPE_TAB_CONTROL_SIDE:
               onTabControlSide();
               break;

            case PropInterface.PROP_TYPE_TAB_CONTROL_TABS_WIDTH:
               onTabControlTabsWidth();
               break;

            case PropInterface.PROP_TYPE_STATIC_TYPE:
               onStaticType();
               break;

#if !PocketPC
            case PropInterface.PROP_TYPE_ALLOW_DRAGGING:
               onAllowDrag();
               break;

            case PropInterface.PROP_TYPE_ALLOW_DROPPING:
               onAllowDrop();
               break;
#endif

            case PropInterface.PROP_TYPE_SELECTION_MODE:
               onSelectionMode();
               break;

            case PropInterface.PROP_TYPE_REFRESH_WHEN_HIDDEN:
               onRefreshWhenHidden();
               break;

#if !PocketPC
            // non-GUI properties that must not put commands for GUI thread
            case PropInterface.PROP_TYPE_SHOW_IN_WINDOW_MENU:
               onShowInWindowMenu();
               break;
#endif

            case PropInterface.PROP_TYPE_MODIFY_IN_QUERY:
            case PropInterface.PROP_TYPE_CONTEXT_MENU:
            case PropInterface.PROP_TYPE_TAB_IN:
            case PropInterface.PROP_TYPE_PARK_ON_CLICK:
            case PropInterface.PROP_TYPE_LOAD_IMAGE_FROM: // this property is used for identifying loading of image (Server/Client).  
               // So no handling needed in RefreshDisplay
               break;

            case PropInterface.PROP_TYPE_TOP_BORDER_MARGIN:
               onTopBorderMargin();
               break;

            case PropInterface.PROP_TYPE_FILL_WIDTH:
               onFillWidth();
               break;

            case PropInterface.PROP_TYPE_MULTI_COLUMN_DISPLAY:
               onMultiColumnDisplay();
               break;

            case PropInterface.PROP_TYPE_SHOW_ELLIPISIS:
               onShowEllipsis();
               break;

            case PropInterface.PROP_TYPE_TITLE_PADDING:
               onTitlePadding();
               break;

            case PropInterface.PROP_TYPE_ROW_BG_COLOR:
               onRowBGColor(line);
               break;

            default:
               if (IsGeneric)
                  onGenericProperty();
               else
                  Events.WriteExceptionToLog(
                     string.Format("Property.RefreshDisplay(): Property {0} wasn't handled", _id));
               break;
         }
      }

      /// <summary>
      /// This function registers the DN control value changed event specified by the user.
      /// </summary>
      private void OnRegisterDNControlValueChangedEvent()
      {
         Debug.Assert(_parentObj != null);
         //Put a GUI Command to register the event specified by the user.
         Commands.addAsync(CommandType.REGISTER_DN_CTRL_VALUE_CHANGED_EVENT, (MgControlBase)_parentObj, _val);
      }

      /// <summary>
      /// the following properties need to calculate once only
      /// </summary>
      /// <returns></returns>
      private bool ShouldBeComputedOnce()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_TASK)
         {
            if (_id == PropInterface.PROP_TYPE_TASK_MODE || _id == PropInterface.PROP_TYPE_TABBING_CYCLE ||
                _id == PropInterface.PROP_TYPE_TASK_PROPERTIES_OPEN_TASK_WINDOW || _id == PropInterface.PROP_TYPE_TASK_PROPERTIES_ALLOW_EVENTS ||
                _id == PropInterface.PROP_TYPE_ALLOW_LOCATE_IN_QUERY ||
                _id == PropInterface.PROP_TYPE_PRINT_DATA || _id == PropInterface.PROP_TYPE_ALLOW_RANGE ||
                _id == PropInterface.PROP_TYPE_ALLOW_LOCATE || _id == PropInterface.PROP_TYPE_ALLOW_SORT ||
                _id == PropInterface.PROP_TYPE_TASK_PROPERTIES_ALLOW_INDEX ||
                _id == PropInterface.PROP_TYPE_MAIN_DISPLAY)
               return true;
         }
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
         {
            if (_id == PropInterface.PROP_TYPE_IS_CACHED || _id == PropInterface.PROP_TYPE_MULTILINE)
               return true;
         }
         return false;
      }

      /// <summary>
      /// if the value is computed in the server
      /// </summary>
      /// <returns></returns>
      private bool IsComputedOnceOnServer()
      {
         if (ShouldBeComputedOnce())
         {
            TaskBase task = GetTaskByParentObject();
            if (task != null)
               return !task.ShouldEvaluatePropertyLocally(_id);
         }
         return false;
      }

      /// <summary>
      /// update the value by the studio value
      /// </summary>
      /// <param name="task"></param>
      /// <param name="propId"></param>
      public static void UpdateValByStudioValue(PropParentInterface propParentInterface, int propId)
      {
         Property property = propParentInterface.getProp(propId);
         if (property != null)
            property.setValue(property.StudioValue);
      }

      /// <summary>
      ///   computes the value of the property and does not affect the display
      /// </summary>
      private void ComputeValue()
      {
         String result = null;
         int len = 255;
         bool wasEvaluated = false;

         if (_expId > 0)
         {
            // for offline : compute once the properties that was compute on the server
            if (ShouldBeComputedOnce() && _expAlreadyComputedOnce)
               return;

            _expAlreadyComputedOnce = true;

            // Should not reach here from Gui thread
            Debug.Assert(!Misc.IsGuiThread());

            // for following control properties always evaluate expressions
            bool alwaysEvaluate = false;
            if (_id == PropInterface.PROP_TYPE_ROW_PLACEMENT)
               alwaysEvaluate = true;
            else if (_parentType == GuiConstants.PARENT_TYPE_TASK)
               alwaysEvaluate = true;


            if (_id == PropInterface.PROP_TYPE_FORMAT)
            {
               len = 65535;
               result = StrUtil.rtrim(_parentObj.EvaluateExpression(_expId, _dataType, len, true, StorageAttribute.SKIP,
                                                                     alwaysEvaluate, out wasEvaluated));
            }
            else
            {
               try
               {
                  result = _parentObj.EvaluateExpression(_expId, _dataType, len, true, StorageAttribute.SKIP, alwaysEvaluate, out wasEvaluated);
               }
               catch (Exception e)
               {
                  var warningMsg = new StringBuilder("Exception: " + e.Message);
                  if (_parentObj is MgControlBase)
                     warningMsg.Append(" Control: " + ((MgControlBase)_parentObj).Name);
                  warningMsg.Append(" Property: " + _genericName);

                  Events.WriteWarningToLog(warningMsg.ToString());
               }
            }

            if (wasEvaluated)
            {
               result = updateResult(result);
               setValue(result);
            }

            // QCR # 286345 if expression was not eveluated - do not send its value to control - it causes unneseary refreshes
            else if (_val == null && _parentType != GuiConstants.PARENT_TYPE_CONTROL)
            {
               // If property doesn't have value (it can be when the property have only 
               // expression as a visibility for example) and we can't evaluate the expression 
               // because the dataview is not yet fetched, set the property's default value.
               result = PropDefaults.getDefaultValue(_id, _parentType, _parentObj);
               setValue(result);
            }
         }
      }

      /// <summary>
      ///   updates results of expression for some properties
      /// </summary>
      /// <param name = "result"> </param>
      /// <returns> </returns>
      private String updateResult(String result)
      {
         switch (_id)
         {
            // now relevant only for coordinates
            case PropInterface.PROP_TYPE_LEFT:
            case PropInterface.PROP_TYPE_WIDTH:
            case PropInterface.PROP_TYPE_TOP:
            case PropInterface.PROP_TYPE_HEIGHT:
            case PropInterface.PROP_TYPE_MINIMUM_WIDTH:
            case PropInterface.PROP_TYPE_MINIMUM_HEIGHT:
               result = updateNavigatorResult(result);
               break;

            case PropInterface.PROP_TYPE_FORMAT:
               //The format cannot be null. So, set it to blank.
               //Even Win32 Online did the same in RT::eval_format() in Scr_put.cpp file  
               if (result == null)
                  result = String.Empty;
               break;

            case PropInterface.PROP_TYPE_TASK_MODE:
               result = updateTaskModeResult(result);
               break;

            default:
               return result;
         }

         return result;
      }

      /// <summary>
      /// do the same as we doing for c++ Uchar RT::tsk_mode_exp (long exp, Uchar mode)
      /// </summary>
      /// <param name="result"></param>
      /// <returns></returns>
      internal string updateTaskModeResult(string result)
      {
         char code = ' ';
         if (!String.IsNullOrEmpty(result))
         {
            code = Char.ToUpper(result[0]);
            code = Char.ToUpper(code);
            switch (code)
            {
               case 'Q':
                  code = 'E';
                  break;
               case 'F':
                  code = 'O';
                  break;
               case 'O':
                  code = 'N';
                  break;
            }
         }

         return code.ToString();
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="result"></param>
      /// <returns></returns>
      private String updateNavigatorResult(String result)
      {
         // convert the xml number to be number 
         var tempPic = new PIC("N6.2", StorageAttribute.NUMERIC, _parentObj.getCompIdx());
         string dispVal = DisplayConvertor.Instance.mg2disp(result, "", tempPic, _parentObj.getCompIdx(), false);
         result = dispVal.Trim();

         MgFormBase form = null;
         if (_parentType == GuiConstants.PARENT_TYPE_FORM)
            form = (MgFormBase)_parentObj;
         else if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
            form = ((MgControlBase)_parentObj).getForm();

         var numberFormatInfo = (NumberFormatInfo)NumberFormatInfo.CurrentInfo.Clone();
         numberFormatInfo.NumberDecimalSeparator = Manager.Environment.GetDecimal().ToString();
         float val = Single.Parse(result, numberFormatInfo);

         // for coordinates result of expressions should be multiplied by factor
         switch (_id)
         {
            case PropInterface.PROP_TYPE_MINIMUM_WIDTH:
            case PropInterface.PROP_TYPE_LEFT:
            case PropInterface.PROP_TYPE_WIDTH:
               val *= form.getHorizontalFactor();
               break;

            case PropInterface.PROP_TYPE_TOP:
            case PropInterface.PROP_TYPE_HEIGHT:
            case PropInterface.PROP_TYPE_MINIMUM_HEIGHT:
               val *= form.getVerticalFactor();
               break;

            default:
               Debug.Assert(false);
               break;
         }

         //convert float to integer and to string
         dispVal = Convert.ToString((int)val);

         // convert the number to xml number      
         result = DisplayConvertor.Instance.disp2mg(dispVal, "", tempPic, _parentObj.getCompIdx(),
                                                         BlobType.CONTENT_TYPE_UNKNOWN);
         return result;
      }

      /// <summary>
      ///   get the value of the property
      /// </summary>
      public String getValue()
      {
         ComputeValue();
         return _val;
      }


      /// <summary>
      ///   returns the integer value of this property
      /// </summary>
      /// <returns> int value of this property</returns>
      public int getValueInt()
      {
         int rc = 0;
         ComputeValue();
         if (_val != null)
            rc = Int32.Parse(_val);
         return rc;
      }

      /// <summary>
      /// returns the integer value of this property without execute its expression
      /// </summary>
      /// <returns> int value of this property</returns>
      public int GetComputedValueInteger()
      {
         return Int32.Parse(_val);
      }

      /// <summary>
      ///   get rectangle value
      /// </summary>
      /// <returns></returns>
      internal MgRectangle getValueRect()
      {
         if (_val == null)
            return null;
         return DisplayConvertor.toRect(_val);
      }

      /// <summary>
      ///   returns a boolean value of the property valid only for logical type properties
      /// </summary>
      public bool getValueBoolean()
      {
         if (_dataType == StorageAttribute.BOOLEAN)
         {
            getValue();
            return DisplayConvertor.toBoolean(_val);
         }
         else
         {
            Events.WriteExceptionToLog(
               string.Format("Property.getValueBoolean() was called for non boolean type property: {0}", _id));
            return false;
         }
      }

      /// <summary>
      /// Get the computed value.
      /// </summary>
      /// <returns></returns>
      public String GetComputedValue()
      {
         return _val;
      }

      /// <summary>
      /// <summary>
      /// get the computed boolean value of the property without execute its expression
      /// </summary>
      public bool GetComputedValueBoolean()
      {
         if (_dataType == StorageAttribute.BOOLEAN)
            return DisplayConvertor.toBoolean(_val);
         else
         {
            Events.WriteExceptionToLog(
               string.Format("Property.getValueBoolean() was called for non boolean type property: {0}", _id));
            return false;
         }
      }

      ///   returns the previous value of the property in the specified line of the table
      /// </summary>
      /// <param name = "line">the requested line number</param>
      protected internal String getPrevValue(int line)
      {
         if (line < _prevValues.Count)
            return (String)_prevValues[line];
         return null;
      }

      /// <summary>
      ///   returns true if this property has an expression
      /// </summary>
      public bool isExpression()
      {
         return (_expId > 0);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      public int GetExpressionId()
      {
         return _expId;
      }

      /// <summary>
      ///   change the color of a window
      /// </summary>
      private void onColor()
      {
         // TODO: SWT doesn't allow changing the column's background color. The color should be changed on the
         // table items (rows)

         // set the background color
         //if (_parentType == GuiConstants.PARENT_TYPE_FORM || _parentType == GuiConstants.PARENT_TYPE_CONTROL)
         //   if (!IsDesignerInfoFlagSet(DesignerInfoFlags.IsBackColor))
         //      onBGColor();
         //// set the foreground color
         //if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
         //   if (!IsDesignerInfoFlagSet(DesignerInfoFlags.IsForeColor))
         //      onFGColor();
         int colorIndex = getValueInt();        
         if (colorIndex > 0)
         {
            Commands.addAsync(CommandType.SET_CLASS, getObjectByParentObj(), getLine(), "color", "mgcolor" + colorIndex);
         }
         


      }

      /// <summary>
      /// Change the Border color of the control
      /// </summary>
      private void onBorderColor()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
         {
            int colorIndex = GetComputedValueInteger();
            MgColor borderColor = null;

            if (colorIndex > 0)
               borderColor = Manager.GetColorsTable().getFGColor((colorIndex));

            Commands.addAsync(CommandType.PROP_SET_BORDER_COLOR, getObjectByParentObj(), getLine(), borderColor);
         }
         else
            throw new ApplicationException("in Property.onBorderColor()");
      }

      /// <summary>
      ///   change the gradient color of a window
      /// </summary>
      private void onGradientColor()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL || _parentType == GuiConstants.PARENT_TYPE_FORM)
         {
            //if (ctrl.SupportGradientColor())
            {
               MgColor gradientFromColor = Manager.GetColorsTable().getFGColor(getValueInt());
               MgColor gradientToColor = Manager.GetColorsTable().getBGColor(getValueInt());
               Commands.addAsync(CommandType.PROP_SET_GRADIENT_COLOR, getObjectByParentObj(), getLine(),
                                 gradientFromColor, gradientToColor);
            }
         }
         else
            throw new ApplicationException("in Property.onGradientColor()");
      }

      /// <summary>
      ///   change the gradient style of a window
      /// </summary>
      private void onGradientStyle()
      {
         if (!IsDesignerInfoFlagSet(DesignerInfoFlags.IsGradientStyle))
         {
            if (_parentType == GuiConstants.PARENT_TYPE_CONTROL || _parentType == GuiConstants.PARENT_TYPE_FORM)
               Commands.addAsync(CommandType.PROP_SET_GRADIENT_STYLE, getObjectByParentObj(), getLine(), getValueInt());
            else
               throw new ApplicationException("in Property.onGradientStyle()");
         }
      }

      /// <summary>
      ///   change the color of the row highlight of the table
      /// </summary>
      private void onRowHighlightColor()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
         {
            MgColor mgColor = Manager.GetColorsTable().getBGColor(getValueInt());
            Commands.addAsync(CommandType.PROP_SET_ROW_HIGHLIGHT_BGCOLOR, getObjectByParentObj(), 0, mgColor);

            mgColor = Manager.GetColorsTable().getFGColor(getValueInt());
            Commands.addAsync(CommandType.PROP_SET_ROW_HIGHLIGHT_FGCOLOR, getObjectByParentObj(), 0, mgColor);
         }
         else
            throw new ApplicationException("in Property.onRowHighlightColor()");
      }

      private void onIncactiveRowHighlightColor()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
         {
            MgColor mgColor = Manager.GetColorsTable().getBGColor(getValueInt());
            Commands.addAsync(CommandType.PROP_SET_INACTIVE_ROW_HIGHLIGHT_BGCOLOR, getObjectByParentObj(), 0, mgColor);

            mgColor = Manager.GetColorsTable().getFGColor(getValueInt());
            Commands.addAsync(CommandType.PROP_SET_INACTIVE_ROW_HIGHLIGHT_FGCOLOR, getObjectByParentObj(), 0, mgColor);
         }
         else
            throw new ApplicationException("in Property.onIncactiveRowHighlightColor()");
      }

      /// <summary>
      ///   change t the alternating color of the table
      /// </summary>
      private void onAlternatingColor()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
         {
            MgColor mgColor = Manager.GetColorsTable().getBGColor(getValueInt());
            Commands.addAsync(CommandType.PROP_SET_ALTENATING_COLOR, getObjectByParentObj(), 0, mgColor);
         }
         else
            throw new ApplicationException("in Property.onAlternatingColor()");
      }

      /// <summary>
      ///   change t the alternating color of the table
      /// </summary>
      private void onTitleColor()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
         {
            int colorIndex = getValueInt();
            MgColor bgColor = null;
            MgColor fgColor = null;

            if (colorIndex > 0)
            {
               bgColor = Manager.GetColorsTable().getBGColor(getValueInt());
               fgColor = Manager.GetColorsTable().getFGColor((getValueInt()));
            }

            bool addTitleColorCommand = true;

            var ctrl = (MgControlBase)_parentObj;
            if (ctrl.isTabControl() && IsDesignerInfoFlagSet(DesignerInfoFlags.IsTitleColor))
               addTitleColorCommand = false;

            if (addTitleColorCommand)
               Commands.addAsync(CommandType.PROP_SET_TITLE_COLOR, getObjectByParentObj(), 0, bgColor);

            if (ctrl.isTabControl())
               Commands.addAsync(CommandType.PROP_SET_TITLE_FGCOLOR, getObjectByParentObj(), 0, fgColor);
         }
         else
            throw new ApplicationException("in Property.onTitleColor()");
      }

      /// <summary>
      /// set hot track color of Tab control
      /// </summary>
      private void onHotTrackColor()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
         {
            var ctrl = (MgControlBase)_parentObj;
            if (ctrl.isTabControl())
            {
               int colorIndex = getValueInt();
               MgColor bgColor = null;
               MgColor fgColor = null;

               if (colorIndex > 0)
               {
                  bgColor = Manager.GetColorsTable().getBGColor(getValueInt());
                  fgColor = Manager.GetColorsTable().getFGColor((getValueInt()));
               }

               Commands.addAsync(CommandType.PROP_SET_HOT_TRACK_COLOR, getObjectByParentObj(), 0, bgColor);
               Commands.addAsync(CommandType.PROP_SET_HOT_TRACK_FGCOLOR, getObjectByParentObj(), 0, fgColor);
            }
         }
         else
            throw new ApplicationException("in Property.onHotTrackColor()");
      }

      /// <summary>
      /// set selected tab color for tab control
      /// </summary>
      private void onSelectedTabColor()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
         {
            var ctrl = (MgControlBase)_parentObj;
            if (ctrl.isTabControl())
            {
               int colorIndex = getValueInt();
               MgColor bgColor = null;
               MgColor fgColor = null;

               if (colorIndex > 0)
               {
                  bgColor = Manager.GetColorsTable().getBGColor(getValueInt());
                  fgColor = Manager.GetColorsTable().getFGColor((getValueInt()));
               }

               Commands.addAsync(CommandType.PROP_SET_SELECTED_TAB_COLOR, getObjectByParentObj(), 0, bgColor);
               Commands.addAsync(CommandType.PROP_SET_SELECTED_TAB_FGCOLOR, getObjectByParentObj(), 0, fgColor);
            }
         }
         else
            throw new ApplicationException("in Property.onSelectedTabColor()");
      }

      /// <summary>
      /// Change table divider color
      /// </summary>
      private void onDividerColor()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
         {
            int colorIndex = getValueInt();
            if (colorIndex > 0)
            {
               MgColor mgColor = Manager.GetColorsTable().getFGColor(getValueInt());
               Commands.addAsync(CommandType.PROP_SET_DIVIDER_COLOR, getObjectByParentObj(), 0, mgColor);
            }
            else
               Commands.addAsync(CommandType.PROP_SET_DIVIDER_COLOR, getObjectByParentObj(), 0, (Object)null);
         }
         else
            throw new ApplicationException("inProperty.onDividerColor()");
      }

      /// <summary>
      ///   change property setColorBy
      /// </summary>
      private void onColorBy()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
            Commands.addAsync(CommandType.PROP_SET_COLOR_BY, getObjectByParentObj(), 0, getValueInt());
         else
            throw new ApplicationException("in Property.onColorBy()");
      }

      /// <summary>
      ///   change the font on the control
      /// </summary>
      private void onFont()
      {
        

         if (_parentType == GuiConstants.PARENT_TYPE_FORM || _parentType == GuiConstants.PARENT_TYPE_CONTROL)
         {
            int fontIndex = getValueInt();
            Commands.addAsync(CommandType.SET_CLASS, getObjectByParentObj(), getLine(), "font", "mgfont" + fontIndex);

         }
         else
            throw new ApplicationException("in Property.onFont()");
      }

      /// <summary>
      ///   Sets the background color of a control
      /// </summary>
      private void onBGColor()
      {
         int colorIndex = getValueInt();

         MgColor mgColor = null;
         if (colorIndex > 0)
         {
            mgColor = Manager.GetColorsTable().getBGColor(colorIndex);
            Commands.addAsync(CommandType.PROP_SET_BACKGOUND_COLOR, getObjectByParentObj(), getLine(), mgColor, colorIndex);
         }
      }

      /// <summary>
      /// 
      /// </summary>
      private void onFGColor()
      {
         int colorIndex = getValueInt();

         MgColor mgColor = null;
         if (colorIndex > 0)
         {
            mgColor = Manager.GetColorsTable().getFGColor(colorIndex);
            Commands.addAsync(CommandType.PROP_SET_FOREGROUND_COLOR, getObjectByParentObj(), getLine(), mgColor);
         }
      }

      /// <summary>
      /// </summary>
      private void onFocuseColor()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
         {
            var ctrl = (MgControlBase)_parentObj;
            if (ctrl.isTextControl())
            {
               int colorIndex = getValueInt();

               //Task # 129468 : If property value of Focus color is 0 or invalid then get the value of Default Focus Color property defined in the Environment settings.
               if (colorIndex == 0 || colorIndex >= Manager.GetColorsTable().Count)
               {
                  colorIndex = Manager.Environment.GetDefaultFocusColor();
                  //If Default Focus color property is invalid then ignore focus color.
                  if (colorIndex >= Manager.GetColorsTable().Count)
                     colorIndex = 0;
               }
               if (colorIndex > 0)
                    Commands.addAsync(CommandType.SET_CLASS, getObjectByParentObj(), getLine(), "focuscolor", "mgFocusColor" + colorIndex);
            }
         }
      }

      /// <summary>
      /// </summary>
      private void onHoveringColor()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
         {
            var ctrl = (MgControlBase)_parentObj;
            if (ctrl.IsHyperTextButton())
            {
               int colorIndex = getValueInt();

               MgColor mgFGColor = Manager.GetColorsTable().getFGColor(colorIndex);
               MgColor mgBGColor = Manager.GetColorsTable().getBGColor(colorIndex);
               Commands.addAsync(CommandType.PROP_SET_HOVERING_COLOR, getObjectByParentObj(), getLine(), mgFGColor, mgBGColor, colorIndex);
            }
         }
      }

      /// <summary>
      /// </summary>
      private void onVisitedColor()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
         {
            var ctrl = (MgControlBase)_parentObj;
            if (ctrl.IsHyperTextButton())
            {
               int colorIndex = getValueInt();

               MgColor mgFGColor = Manager.GetColorsTable().getFGColor(colorIndex);
               MgColor mgBGColor = Manager.GetColorsTable().getBGColor(colorIndex);
               Commands.addAsync(CommandType.PROP_SET_VISITED_COLOR, getObjectByParentObj(), getLine(), mgFGColor, mgBGColor, colorIndex);
            }
         }
      }

      /// <summary>
      /// </summary>
      private void onWallpaper()
      {
         // Get the of the property
         if (_parentType == GuiConstants.PARENT_TYPE_FORM || _parentType == GuiConstants.PARENT_TYPE_CONTROL)
         {
            Property wallpapaerStyleProp = _parentObj.getProp(PropInterface.PROP_TYPE_WALLPAPER_STYLE);
            String wallpaper = getValue();
            if (wallpaper != null)
               wallpaper = wallpaper.Trim();
            String wallpaperTrans = Events.TranslateLogicalName(wallpaper);
            TaskBase task = GetTaskByParentObject();
            String localFileName = Events.GetLocalFileName(wallpaperTrans, task);

            Commands.addAsync(CommandType.PROP_SET_WALLPAPER, getObjectByParentObj(), 0, localFileName,
                              wallpapaerStyleProp.getValueInt());
         }
         else
            throw new ApplicationException("in Property.onWallpaper()");
      }

      /// <summary>
      /// </summary>
      private void onImageFileName()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
         {
            var ctrl = (MgControlBase)_parentObj;
            if (ctrl.isTreeControl() || ctrl.isTabControl() || (ctrl.getField() == null && ctrl.IsImageButton()))
               ctrl.setImageList(getValue());
            else if (ctrl.isImageControl() || ctrl.isRadio() || ctrl.isCheckBox())
               ctrl.setImage(getValue());
         }
         else
            throw new ApplicationException("in onImageFileName.onEnable()");
      }

      /// <summary>
      /// </summary>
      private void onImageIdxList()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
         {
            String val = getValue();
            if (val != null)
            {
               //translate string into int array
               int[] intArr = Misc.GetIntArray(val);
               Commands.addAsync(CommandType.PROP_SET_IMAGE_LIST_INDEXES, getObjectByParentObj(), getLine(), intArr);
            }
         }
         else
            throw new ApplicationException("in onImageIdxList.onEnable()");
      }

      /// <summary>
      /// </summary>
      private void onEnable(bool valChanged)
      {
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
         {
            var ctrl = (MgControlBase)_parentObj;
            MgControlBase currentEditingControl = ctrl.getForm().getTask().CurrentEditingControl;

            //ACT_TBL_NXTFLD is added in queue, whenever current control is hidden or disabled to set the focus to next control for both RC & OL 
            //which is correct behavior. But once current ctrl is already disabled/hidden , we should not put the same action
            //in queue again when same ctrl is disabled/hidden multiple times.
            if (_val != null && _val.Equals("0") && valChanged && currentEditingControl != null &&
                (ctrl == currentEditingControl || currentEditingControl.isDescendentOfControl(ctrl) || ctrl == currentEditingControl.getLinkedParent(false)))
            {
               MgControlBase parkedControl = ctrl.getForm().getTask().getLastParkedCtrl();
               if (parkedControl != null && parkedControl.InControl && (ctrl == parkedControl || parkedControl.isDescendentOfControl(ctrl)))
               {
                  Events.OnNonParkableLastParkedCtrl(ctrl);
                  ctrl.getForm().getTask().CurrentEditingControl = null;
               }
            }
            ctrl.SetEnabled(getValueBoolean());
         }
         else
            throw new ApplicationException("in Property.onEnable()");
      }

      /// <summary>
      /// </summary>
      private void onVisible(bool valChanged)
      {
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
         {
            var ctrl = (MgControlBase)_parentObj;
            MgControlBase currentEditingControl = ctrl.getForm().getTask().CurrentEditingControl;

            //ACT_TBL_NXTFLD is added in queue, whenever current control is hidden or disabled to set the focus to next control for both RC & OL 
            //which is correct behavior. But once current ctrl is already disabled/hidden , we should not put the same action
            //in queue again when same ctrl is disabled/hidden multiple times.
            if (_val != null && _val.Equals("0") && valChanged && currentEditingControl != null &&
                (ctrl == currentEditingControl || currentEditingControl.isDescendentOfControl(ctrl) || ctrl == currentEditingControl.getLinkedParent(false)))

            {
               MgControlBase parkedControl = ctrl.getForm().getTask().getLastParkedCtrl();
               if (parkedControl != null && parkedControl.InControl && (ctrl == parkedControl || parkedControl.isDescendentOfControl(ctrl)))
               {
                  Events.OnNonParkableLastParkedCtrl(ctrl);
                  ctrl.getForm().getTask().CurrentEditingControl = null;
               }
            }
            ctrl.updatePropertyLogicNesting(PropInterface.PROP_TYPE_VISIBLE, CommandType.PROP_SET_VISIBLE, getValueBoolean(), true);

            // Defect # 137523 : In case of AllowTesting = N, if visibility of text control is set to false, logical control drawn for it,
            // is hidden and actual text control attached to it is moved to control to whom focus is set. But in defect scenario, If visibility
            // set to false for text control on re-compute and focus is moved to some other form instead of  some other control on the same form.
            // The control is still is  visible. The reason is : The logical control is hidden , but the text control attached to it is at same position 
            // & is not hidden. So, control remains visible (in defect scenario it is orphan form). So to fix this problem,
            // add command REFRESH_TMP_EDITOR to refresh it.
            //if (_val != null && _val.Equals("0") && valChanged && ctrl.isTextControl())
            //{
            //   Commands.addAsync(CommandType.REFRESH_TMP_EDITOR, (object)ctrl.getForm(), true);
            //}

         }
      }

      /// <summary>
      /// </summary>
      private void onHorizontalPlacement()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
            Commands.addAsync(CommandType.PROP_HORIZONTAL_PLACEMENT, getObjectByParentObj(), getLine(),
                              getValueBoolean());
      }

      /// <summary>
      /// </summary>
      private void onVerticalPlacement()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
            Commands.addAsync(CommandType.PROP_VERTICAL_PLACEMENT, getObjectByParentObj(), getLine(), getValueBoolean());
      }

      /// <summary>
      /// </summary>
      private void OnControlName()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
         {
            if (Manager.Environment.GetSpecialSwfControlNameProperty())
               Commands.addAsync(CommandType.PROP_SET_CONTROL_NAME, _parentObj, getLine(), GetComputedValue(), 0);
         }
         else
            throw new ApplicationException("in Property.OnControlName()");
      }

      /// <summary>
      /// set hint for edit control property
      /// </summary>
      private void onHint()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
         {
            MgControlBase control = (MgControlBase)getObjectByParentObj();
            string translatedString = Events.Translate(getValue());
            if (!control.IsDateTimePicture() && (!control.IsTableChild || control.IsTableHeaderChild))
               Commands.addAsync(CommandType.SET_PROPERTY, getObjectByParentObj(), getLine(), "placeholder", translatedString);
            }
         else
            throw new ApplicationException("inProperty.onHint()");
      }

      private void onHintColor()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
         {
            MgControlBase control = (MgControlBase)getObjectByParentObj();
            if (!control.IsDateTimePicture() && !control.IsTableChild)
            {
               int colorIndex = getValueInt();
                if (colorIndex > 0 && colorIndex < Manager.GetColorsTable().Count)
                    Commands.addAsync(CommandType.SET_CLASS, getObjectByParentObj(), getLine(), "hintcolor","mgHintColor" + colorIndex);
            }
         }
         else
            throw new ApplicationException("inProperty.onHintColor()");
      }

      /// <summary>
      /// </summary>
      private void onFormName()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_FORM)
            addCommandTypeText(0);
         else
            throw new ApplicationException("in Property.onFormName()");
      }

      /// <summary>
      /// </summary>
      private void addCommandTypeText(int line)
      {
         if (IsDesignerInfoFlagSet(DesignerInfoFlags.IsDesignerValue))
            return;

         String mlsTransValue = Events.Translate(getValue());
         //Fixed bug#:733480 SystemBox = false and title is empty-> .NET hide the title.
         //                  we will set it to ' '
         if (_parentType == GuiConstants.PARENT_TYPE_FORM ||
            (_parentType == GuiConstants.PARENT_TYPE_CONTROL && ((MgControlBase)_parentObj).Type == MgControlType.CTRL_TYPE_COLUMN))
         {
            // #737488: When variable attached to form's text property, spaces are appended 
            // after actual value, equal to picture of variable. Before setting text remove these extra 
            // spaces at the end.
            mlsTransValue = StrUtil.rtrim(mlsTransValue);
            if (_parentType == GuiConstants.PARENT_TYPE_FORM && mlsTransValue.Equals(String.Empty))
               mlsTransValue = " ";
         }
         if (_parentObj is MgControlBase && ((MgControlBase)_parentObj).isTextControl())
            Commands.addAsync(CommandType.SET_VALUE ,_parentObj, line, "", mlsTransValue);
         else
            Commands.addAsync(CommandType.SET_PROPERTY, _parentObj, line, "text", mlsTransValue);
      
      }
      

      /// <summary>
      /// </summary>
      private void onText(int line)
      {
         addCommandTypeText(line);
      }

      /// <summary>
      /// </summary>
      private void onNavigation(bool valChanged)
      {
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
         {
            Property otherProp = null;
            MgControlBase ctrl = null;
            TaskBase task = null;
            bool readOnlyValue = false;
            ctrl = (MgControlBase)_parentObj;
            MgControlBase currentEditingControl = ctrl.getForm().getTask().CurrentEditingControl;
            if (ctrl.isTextOrTreeControl() || ctrl.isRichEditControl())
            {
               task = GetTaskByParentObject();
               // get the other property
               if (_id == PropInterface.PROP_TYPE_ALLOW_PARKING)
                  otherProp = ctrl.getProp(PropInterface.PROP_TYPE_MODIFIABLE);
               else
                  otherProp = ctrl.getProp(PropInterface.PROP_TYPE_ALLOW_PARKING);

               readOnlyValue = !getValueBoolean();
               // if the value of the this property is true and the other property
               // exists then the other property's value determines the value of this
               // property
               if (!readOnlyValue && otherProp != null)
                  readOnlyValue = !otherProp.getValueBoolean();
               // prevent sending unnecessary commands to the browser
               if ((readOnlyValue || task.getMode() != Constants.TASK_MODE_QUERY) &&
                   ctrl.GetCurrReadOnly() != readOnlyValue)
               {
                  ctrl.SetCurrReadOnly(readOnlyValue);

                  // JPN: IME support (enable IME in query mode)
                  if (UtilStrByteMode.isLocaleDefLangDBCS() && !ctrl.isTreeControl() && !ctrl.isMultiline())
                  {
                     if (ctrl.getForm().getTask().checkProp(PropInterface.PROP_TYPE_ALLOW_LOCATE_IN_QUERY, false))
                        return;
                  }

                  if (_id == PropInterface.PROP_TYPE_ALLOW_PARKING && valChanged && _val.Equals("0") && currentEditingControl != null &&
                      (ctrl == currentEditingControl || currentEditingControl.isDescendentOfControl(ctrl) || ctrl == currentEditingControl.getLinkedParent(false)))
                  {
                     MgControlBase parkedControl = ctrl.getForm().getTask().getLastParkedCtrl();
                     if (parkedControl != null && parkedControl.InControl && (ctrl == parkedControl || parkedControl.isDescendentOfControl(ctrl)))
                     {
                        Events.OnNonParkableLastParkedCtrl(ctrl);
                        ctrl.getForm().getTask().CurrentEditingControl = null;
                     }
                  }

                  //Commands.addAsync(CommandType.PROP_SET_READ_ONLY, getObjectByParentObj(), getLine(), readOnlyValue);
                  Commands.addAsync(CommandType.SET_ATTRIBUTE, getObjectByParentObj(), getLine(), readOnlyValue.ToString());
               }
            }
         }
         else
            throw new ApplicationException("in Property.onNavigation()");
      }

      /// <summary>
      /// </summary>
      private void onTooltip()
      {
         String toolTip;

         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
         {
            var mgControl = (MgControlBase)_parentObj;
            // Tooltip expression returns string
            if (_expId == 0 && mgControl.getParent() is MgStatusBar == false)
               toolTip = ((ToolTipHelp)mgControl.getForm().getTask().getHelpItem(Int32.Parse(_val))).tooltipHelpText;
            else
               toolTip = _val;

            // #929889: Remove extra white spaces at the end of tooltip text.
            toolTip = StrUtil.rtrim(toolTip);
            Commands.addAsync(CommandType.SET_PROPERTY, mgControl, getLine(), "tooltip", toolTip);
            Commands.addAsync(CommandType.PROP_SET_TOOLTIP, mgControl, getLine(), toolTip, 0);
         }
         else
            throw new ApplicationException("in Property.onTooltip()");
      }

      private void onPlacement()
      {
         MgRectangle rect = getValueRect();
         if (rect != null)
            Commands.addAsync(CommandType.PROP_SET_PLACEMENT, _parentObj, getLine(), rect.x, rect.y, rect.width,
                              rect.height, false, false);
      }

      /// <summary>
      /// </summary>
      private void onDisplayList(int currLine)
      {
         MgFormBase form = null;
         MgControlBase ctrl = null;
         TaskBase task = null;
         int line = 0;

         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
         {
            ctrl = (MgControlBase)_parentObj;
            form = ctrl.getForm();
            task = GetTaskByParentObject();
            line = currLine == Int32.MinValue
                      ? ctrl.getDisplayLine(false)
                      : currLine;

            // SELECT control. Note we do not support dynamic change of radio buttons range!
            // data control ranges are updated when we set values to them in Control.refreshDispVal()
            if (ctrl.isSelectionCtrl() || ctrl.isTabControl() || ctrl.isRadio() || ctrl.IsDotNetChoiceControl())
            {
               ctrl.refreshAndSetItemsList(line, true);
            }
         }
         else
            throw new ApplicationException("in Property.onDisplayList()");
      }

      /// <summary>
      /// </summary>
      /// <param name = "currLine"></param>
      private void onVisibleLayerList(int currLine)
      {
         MgControlBase ctrl = null;
         int line = 0;

         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
         {
            ctrl = (MgControlBase)_parentObj;

            if (currLine == Int32.MinValue)
               line = ctrl.getDisplayLine(false);
            else
               line = currLine;

            // Always layering should be performed on Displaylist. But,
            // DisplayList and itemList should not be refreshed.
            ctrl.refreshTabForLayerList(line);
         }
         else
            throw new ApplicationException("in Property.onVisibleLayerList()");
      }

      /// <summary>number of visible rows in a combo box</summary>
      private void onSelectionRows()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
            Commands.addAsync(CommandType.PROP_SET_VISIBLE_LINES, getObjectByParentObj(), getLine(), getValueInt());
         else
            throw new ApplicationException("in Property.onSelectionRows()");
      }

      /// <summary>
      /// </summary>
      private void onFormat()
      {
         MgControlBase ctrl = null;
         PIC ctrlPic;

         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
         {
            ctrl = (MgControlBase)_parentObj;
            ctrlPic = ctrl.getPIC();
            ctrl.resetPrevVal();
            switch (ctrl.Type)
            {
               case MgControlType.CTRL_TYPE_BUTTON:
                  // if the button doesn't have field we need to set the format as the text of the control,
                  // otherwise, the field value will set the text on the control
                  // in addition we need to set the text for image button
                  // Also, do not override the text with the format if the control has an expression.
                  // QCR# 938933. When the control is refreshed for the first time, we should show the text as it
                  // is in the Format property - irrespective of whether field or expression is attached to it or not.
                  // The fix is confined to ControlTypes.CTRL_TYPE_BUTTON because only in case of Button we display the value of the Format property.
                  if (ctrl.getField() == null && !ctrl.expressionSetAsData() || ctrl.IsImageButton() || (_parentObj.IsFirstRefreshOfProps() && !Manager.Environment.GetSpecialIgnoreButtonFormat()))
                     addCommandTypeText(getLine());
                  break;

               case MgControlType.CTRL_TYPE_LABEL:
                  if (ctrlPic.getAttr() != StorageAttribute.NUMERIC)
                  {
                     if (Manager.Environment.Language == 'H')
                        Commands.addAsync(CommandType.PROP_SET_RIGHT_TO_LEFT, getObjectByParentObj(),
                                          ctrl.getDisplayLine(false), true);
                  }
                  break;

               case MgControlType.CTRL_TYPE_TEXT:
               case MgControlType.CTRL_TYPE_TREE:
                  // for text controls change the max length
                  if (ctrlPic.getAttr() != StorageAttribute.BLOB &&
                      ctrlPic.getAttr() != StorageAttribute.BLOB_VECTOR)
                  {
                     Commands.addAsync(CommandType.PROP_SET_TEXT_SIZE_LIMIT, getObjectByParentObj(), getLine(),
                                       ctrlPic.getMaskLength());
                  }
                  if (ctrl.Type == MgControlType.CTRL_TYPE_TEXT)
                  {
                     if (ctrlPic.getAttr() != StorageAttribute.NUMERIC)
                     {
                        if (Manager.Environment.Language == 'H')
                        {
                           Commands.addAsync(CommandType.PROP_SET_RIGHT_TO_LEFT, getObjectByParentObj(),
                                             ctrl.getDisplayLine(false), ctrlPic.isHebrew());
                        }
                     }

                     // JPN: IME support
                     UtilImeJpn utilImeJpn = Manager.UtilImeJpn;
                     if (utilImeJpn != null)
                     {
                        int imeMode = UtilImeJpn.IME_FORCE_OFF;

                        if (ctrlPic.isAttrAlpha() || ctrlPic.isAttrUnicode() || ctrlPic.isAttrBlob())
                        {
                           // if TRANSLATOR property is 0, calculate IME mode from the Kanji parameter & AS/400 pictures.
                           Property prop = ctrl.getProp(PropInterface.PROP_TYPE_TRANSLATOR);
                           imeMode = prop.getValueInt();
                           if (imeMode == 0)
                           {
                              // -- if exp is evaluated to 0, it means IME off.
                              if (prop.isExpression())
                                 imeMode = UtilImeJpn.IME_FORCE_OFF;
                              else
                              {
                                 int picImeMode = ctrlPic.getImeMode();
                                 if (picImeMode != -1)
                                    imeMode = picImeMode;
                              }
                           }
                        }
                        if (utilImeJpn.isValid(imeMode))
                           Commands.addAsync(CommandType.PROP_SET_TRANSLATOR, getObjectByParentObj(), getLine(), imeMode);
                     }
                  }
                  break;
            }
         }
         else
            throw new ApplicationException("in Property.onFormat()");
      }

      /// <summary>
      /// </summary>
      private void onLabel(int currLine)
      {
         MgControlBase ctrl = null;
         TaskBase task = null;
         int line = 0;

         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
         {
            // SELECT control. Note we do not support dynamic change of radio buttons range!
            // data control ranges are updated when we set values to them in Control.refreshDispVal()
            ctrl = (MgControlBase)_parentObj;
            task = GetTaskByParentObject();

            if (currLine == Int32.MinValue)
               line = ctrl.getDisplayLine(false);
            else
               line = currLine;

            // on combo box compfor check box the label is the item list property
            if (ctrl.Type == MgControlType.CTRL_TYPE_CHECKBOX)
               addCommandTypeText(line);
            else if (ctrl.isSelectionCtrl() || ctrl.isTabControl() || ctrl.isRadio() || ctrl.IsDotNetChoiceControl())
            {
               ctrl.setRange(getValue());

               ctrl.clearRange(line);
               ctrl.refreshAndSetItemsList(line, true);
               // If ItemList has an expression And it may be modified again, refresh the display value of control.
               // Because it may be possible that display value will change.
               if (_expId > 0)
                  ctrl.ComputeAndRefreshDisplayValue(false);
            }
            else
               throw new ApplicationException("Property.onLabel(), not support control");
         }
         else
            throw new ApplicationException("in Property.onLabel()");
      }

      /// <summary>
      ///   set the minimum size for the height of the form
      /// </summary>
      private void onMinimumHeight()
      {
         MgFormBase form = null;

         if (_parentType == GuiConstants.PARENT_TYPE_FORM || _parentType == GuiConstants.PARENT_TYPE_CONTROL)
         {
            if (_parentType == GuiConstants.PARENT_TYPE_FORM)
               form = getForm();
            else if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
            {
               var control = (MgControlBase)_parentObj;
               form = control.getForm();
            }
            int height = form.uom2pix(getValueInt(), false);
            Commands.addAsync(CommandType.PROP_SET_MIN_HEIGHT, _parentObj, 0, height);
         }
         else
            throw new ApplicationException("in Property.onMinimumHeight()");
      }

      /// <summary>
      ///   set the minimum size for the width of the form
      /// </summary>
      private void onMinimumWidth()
      {
         MgFormBase form = null;

         if (_parentType == GuiConstants.PARENT_TYPE_FORM || _parentType == GuiConstants.PARENT_TYPE_CONTROL)
         {
            if (_parentType == GuiConstants.PARENT_TYPE_FORM)
               form = getForm();
            else if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
            {
               var control = (MgControlBase)_parentObj;
               form = control.getForm();
            }
            int width = form.uom2pix(getValueInt(), true);
            Commands.addAsync(CommandType.PROP_SET_MIN_WIDTH, _parentObj, 0, width);
         }
         else
            throw new ApplicationException("in Property.onMinimumWidth()");
      }

      /// <summary>
      ///   set the default button on the form
      /// </summary>
      private void onDefaultButton()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_FORM || _parentType == GuiConstants.PARENT_TYPE_CONTROL)
         {
            MgFormBase form = _parentObj.getForm();
            MgControlBase defaultButton = form.getCtrlByName(getValue(), MgControlType.CTRL_TYPE_BUTTON);
            int line = defaultButton == null
                          ? 0
                          : defaultButton.getDisplayLine(false);
            Commands.addAsync(CommandType.PROP_SET_DEFAULT_BUTTON, getObjectByParentObj(), defaultButton, 0, line, 0);
         }
         else
            throw new ApplicationException("in Property.onDefaultButton()");
      }

      /// <summary>
      ///   for combo box, visible line in the combo box
      /// </summary>
      private void onVisibleLines()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
         {
            int visibleLines = getValueInt();
            if (visibleLines == 0)
            {
               var ctrl = (MgControlBase)_parentObj;
               String[] itemsList = ctrl.refreshDispRange(true);
               if (itemsList != null)
                  visibleLines = itemsList.Length;
            }
            Commands.addAsync(CommandType.PROP_SET_VISIBLE_LINES, getObjectByParentObj(), getLine(), visibleLines);
         }
         else
            throw new ApplicationException("in Property.onVisibleLines()");
      }

      /// <summary>
      ///   for the radio button the number of the choice column on the control
      /// </summary>
      /// <param name="line">Display line</param>
      private void onChoiceColumn(int line)
      {
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
            Commands.addAsync(CommandType.PROP_SET_LAYOUT_NUM_COLUMN, getObjectByParentObj(), line, getValueInt());
         else
            throw new ApplicationException("in Property.onChoiceColumn()");
      }

      /// <summary>
      ///   all the properties that support in subform, must call this method
      /// </summary>
      /// <returns></returns>
      private Object getObjectByParentObj()
      {
         Object obj = _parentObj;
         if (_parentType == GuiConstants.PARENT_TYPE_FORM)
         {
            var form = (MgFormBase)_parentObj;
            if (form.isSubForm())
            {
               MgControlBase ctrl = form.getSubFormCtrl();
               obj = ctrl;
            }
         }

         return obj;
      }

      /// <summary>
      ///   returns true if property relevant for tree items
      /// </summary>
      /// <param name = "propId">property id</param>
      /// <returns></returns>
      internal static bool isRepeatableInTree(int propId)
      {
         switch (propId)
         {
            case PropInterface.PROP_TYPE_TOOLTIP:
            case PropInterface.PROP_TYPE_ALLOW_PARKING:
            case PropInterface.PROP_TYPE_PARKED_COLLAPSED_IMAGEIDX:
            case PropInterface.PROP_TYPE_EXPANDED_IMAGEIDX:
            case PropInterface.PROP_TYPE_COLLAPSED_IMAGEIDX:
            case PropInterface.PROP_TYPE_PARKED_IMAGEIDX:
            case PropInterface.PROP_TYPE_FORMAT:
               return (true);
         }
         return false;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="propId"></param>
      /// <returns></returns>
      internal static bool isRepeatableInTable(int propId)
      {
         return propId == PropInterface.PROP_TYPE_ROW_BG_COLOR;
      }

      /// <summary>
      ///   get current line
      /// </summary>
      /// <returns></returns>
      private int getLine()
      {
         int line = 0;
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
         {
            var ctrl = (MgControlBase)_parentObj;
            bool displayLineForTreeOrTable = ctrl.isPropertyRepeatable(_id);

            line = ctrl.getDisplayLine(displayLineForTreeOrTable);
         }

         return line;
      }

      /// <summary>
      ///   in the first time we need to skip navigation properties
      /// </summary>
      private bool ShouldSkipNavigationProperties()
      {
         bool skip = false;
         var form = (MgFormBase)_parentObj;

         Debug.Assert((_id == PropInterface.PROP_TYPE_LEFT) || (_id == PropInterface.PROP_TYPE_TOP) ||
                      (_id == PropInterface.PROP_TYPE_WIDTH) || (_id == PropInterface.PROP_TYPE_HEIGHT));


         var windowPosition = form.GetStartupPosition();

         switch (windowPosition)
         {
            // #712347 - when startupPosition is center to *,
            // then left and top properties should not be evaluated.
            case WindowPosition.CenteredToParent:
            case WindowPosition.CenteredToDesktop:
            case WindowPosition.CenteredToMagic:
            case WindowPosition.DefaultLocation:
               skip = ((_id == PropInterface.PROP_TYPE_LEFT) || (_id == PropInterface.PROP_TYPE_TOP));
               break;
            case WindowPosition.DefaultBounds:
               //All left, top, width and height are taken from Windows Default.
               skip = true;
               break;
            case WindowPosition.Customized:
               break;
         }

         if (skip)
         {
            ComputeValue();
            _prevValues[0] = _val;
         }

         return skip;
      }

      /// <summary>
      ///   return original value of property that was computed before execution of expression
      /// </summary>
      /// <returns></returns>
      internal String getOrgValue()
      {
         return _orgValue;
      }

      /// <summary>
      ///   for table control show\hide the lines
      /// </summary>
      private void onShowLines()
      {
         var ctrlTable = (MgControlBase)_parentObj;
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL && ctrlTable.Type == MgControlType.CTRL_TYPE_TREE)
            Commands.addAsync(CommandType.PROP_SET_LINE_VISIBLE, getObjectByParentObj(), 0, getValueBoolean());
         else
            throw new ApplicationException("in Property.onShowLines() the control isn't Table control");
      }

      /// <summary>
      ///   set for all columns controls in the table resizable need to set the resizable for each column
      /// </summary>
      private void onAllowColumnResiz()
      {
         var ctrlTable = (MgControlBase)_parentObj;
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL && ctrlTable.Type == MgControlType.CTRL_TYPE_TABLE)
            Commands.addAsync(CommandType.PROP_SET_RESIZABLE, ctrlTable, 0, getValueBoolean());
         else
            throw new ApplicationException("in Property.onAllowColumnResiz() the control isn't Table control");
      }

      /// <summary>
      ///   set the row height of the table
      /// </summary>
      private void onRowHeight()
      {
         var ctrlTable = (MgControlBase)_parentObj;

         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL && ctrlTable.Type == MgControlType.CTRL_TYPE_TABLE)
         {
            MgFormBase form = ctrlTable.getForm();
            int rowHeight = form.uom2pix(getValueInt(), false);
            Commands.addAsync(CommandType.PROP_SET_ROW_HEIGHT, ctrlTable, 0, rowHeight);
         }
         else
            throw new ApplicationException("in Property.onRowHeight() the control isn't Table control");
      }

      /// <summary>
      ///   set the row height of the table
      /// </summary>
      private void onTitleHeight()
      {
         var ctrlTable = (MgControlBase)_parentObj;

         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL && ctrlTable.Type == MgControlType.CTRL_TYPE_TABLE)
         {
            MgFormBase form = ctrlTable.getForm();
            int titleHeight = form.uom2pix(getValueInt(), false);
            Commands.addAsync(CommandType.PROP_SET_TITLE_HEIGHT, ctrlTable, 0, titleHeight);
         }
         else
            throw new ApplicationException("in Property.onRowHeight() the control isn't Table control");
      }

      /// <summary> set the bottom position interval of the table </summary>
      private void onBottomPositionInterval()
      {
         var control = (MgControlBase)_parentObj;

         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL && control.isTableControl())
            Commands.addAsync(CommandType.PROP_SET_BOTTOM_POSITION_INTERVAL, control, 0, getValueInt());
         else
            throw new ApplicationException("in Property.onBottomPositionInterval() the control isn't Table control");
      }

      /// <summary>
      ///   set for all columns controls in the table movable need to set the movable for each column
      /// </summary>
      private void onAllowReOrder()
      {
         var ctrlTable = (MgControlBase)_parentObj;
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL && ctrlTable.Type == MgControlType.CTRL_TYPE_TABLE)
            Commands.addAsync(CommandType.PROP_SET_ALLOW_REORDER, ctrlTable, 0, getValueBoolean());
         else
            throw new ApplicationException("in Property.onAllowReOrder() the control isn't Table control");
      }

      /// <summary>
      ///   set the sortable for a column
      /// </summary>
      private void onColumnSortable()
      {
         var columnCtrl = (MgControlBase)_parentObj;

         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL && columnCtrl.Type == MgControlType.CTRL_TYPE_COLUMN)
         {
            bool PropValue = getValueBoolean();
            // no sortable for non interactive.
            if (PropValue && !(GetTaskByParentObject().IsInteractive))
               PropValue = false;

            Commands.addAsync(CommandType.PROP_SET_SORTABLE_COLUMN, columnCtrl, 0, PropValue);
         }
         else
            throw new ApplicationException("in Property.onColumnSortable() the control isn't Column control");
      }

      private void onColumnFilterable()
      {
         var columnCtrl = (MgControlBase)_parentObj;

         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL && columnCtrl.Type == MgControlType.CTRL_TYPE_COLUMN)
         {
            bool PropValue = getValueBoolean();
            // no filterable for non interactive.
            if (PropValue && !(GetTaskByParentObject().IsInteractive))
               PropValue = false;

            Commands.addAsync(CommandType.PROP_SET_COLUMN_FILTER, columnCtrl, 0, PropValue);
         }
         else
            throw new ApplicationException("in Property.onColumnFilterable() the control isn't Column control");
      }

      /// <summary>
      ///   set the sortable for a column
      /// </summary>
      private void onColumnPlacement()
      {
         var columnCtrl = (MgControlBase)_parentObj;

         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL && columnCtrl.Type == MgControlType.CTRL_TYPE_COLUMN)
            Commands.addAsync(CommandType.PROP_SET_COLUMN_PLACMENT, columnCtrl, 0, getValueBoolean());
         else
            throw new ApplicationException("in Property.onColumnPlacement() the control isn't Column control");
      }

      /// <summary>
      ///   update size of prevvalues array
      /// </summary>
      /// <param name = "newSize"></param>
      internal void updatePrevValueArray(int newSize)
      {
         if (!ShouldSkipRefresh())
            _prevValues.SetSize(newSize);
      }

      /// <summary>
      /// clear _prevValues array
      /// </summary>
      /// <param name = "newSize"></param>
      internal void clearPrevValueArray()
      {
         _prevValues.Clear();
      }

      /// <summary>
      /// 
      /// </summary>
      internal void ResetPrevValueArray()
      {
         int size = _prevValues.Count;
         _prevValues.Clear();
         _prevValues.SetSize(size);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="idx"></param>
      internal void RemoveAt(int idx)
      {
         if (!ShouldSkipRefresh() && _prevValues.Count > 0)
         {
            Debug.Assert(idx < _prevValues.Count);
            _prevValues.RemoveAt(idx);
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="idx"></param>
      internal void InsertAt(int idx)
      {
         if (!ShouldSkipRefresh() && _prevValues.Count > 0)
         {
            Debug.Assert(idx <= _prevValues.Count);
            _prevValues.Insert(idx, null);
         }
      }

      /// <summary>
      ///   compute and set arrays size
      /// </summary>
      private void setPrevArraySize()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_FORM || _parentType == GuiConstants.PARENT_TYPE_CONTROL)
         {
            int size = 1;
            if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
            {
               var ctrl = (MgControlBase)_parentObj;
               if (ctrl.IsRepeatable)
                  size = ctrl.getForm().getTableItemsCount();
               else if (ctrl.isTreeControl() && isRepeatableInTree(_id))
               {
                  MgTreeBase tree = ctrl.getForm().getMgTree();
                  if (tree != null)
                     size = tree.getSize() + 1; //number of lines in tree + 1, since tree lines start from 1
               }
               else if(ctrl.isTableControl() && isRepeatableInTable(_id))
               {
                  size = ctrl.getForm().getTableItemsCount();
               }
            }
            _prevValues.SetSize(size);
         }
      }

      /// <summary>
      ///   Updates the object's menu property, according to the passed property id pulldown menu.
      /// </summary>
      internal void refreshPulldownMenu()
      {
         int propValue = 0;
         bool parentTypeForm = (_parentType == GuiConstants.PARENT_TYPE_FORM &&
                                _parentType != GuiConstants.PARENT_TYPE_TASK);
         MgMenu mgMenu = null;
         MgControlBase parentControl = null;
         bool pulldownOnSubForm = ((MgFormBase)_parentObj).isSubForm();
         MgFormBase form = (_parentObj is MgFormBase
                               ? (MgFormBase)_parentObj
                               : ((MgControlBase)_parentObj).getForm());
         bool setPulldownMenu = form.IsMDIOrSDIFrame;

         //set PullDownMenu to SDI only.
         if (setPulldownMenu && !pulldownOnSubForm)
         {
            propValue = ((MgFormBase)_parentObj).getPulldownMenuNumber();

            if (parentControl == null || !(parentControl is MgStatusBar))
            {

               // get the matching MenuEntry object. The MenuManager will make sure it already exists.
               TaskBase task = GetTaskByParentObject();
               mgMenu = Manager.GetMenu(task.ContextID, task.getCtlIdx(), propValue, MenuStyle.MENU_STYLE_PULLDOWN, form, true);
               if (mgMenu != null)
                  // create the matching gui command with the object and menuEntry
                  Commands.addAsync(CommandType.PROP_SET_MENU, _parentObj, form, MenuStyle.MENU_STYLE_PULLDOWN, mgMenu, parentTypeForm);
               else
                  Commands.addAsync(CommandType.PROP_RESET_MENU, _parentObj, form, MenuStyle.MENU_STYLE_PULLDOWN, null, parentTypeForm);

               Commands.addAsync(CommandType.UPDATE_MENU_VISIBILITY, form);
            }
         }
      }

      /// <summary>
      ///  This method refreshes the controls who inherit the context menu from the form. It passes on all the
      ///  controls in the MGForm::controlsInheritingContext vector and refreshes their context menu property.
      /// </summary>
      /// <param name="propVal"></param>
      private void RefreshControlsInheritingContext(int propVal)
      {
         if (_parentType == GuiConstants.PARENT_TYPE_FORM)
            ((MgFormBase)_parentObj).refreshContextMenuOnLinkedControls(propVal);
      }

      /// <summary>
      ///   for shell :startup mode(default,maximize, minimize)
      /// </summary>
      private void onStartupMode()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_FORM)
         {
            var startupMode = (StartupMode)getValueInt();
            if (startupMode != StartupMode.Default)
               Commands.addAsync(CommandType.SET_WINDOW_STATE, (_parentObj), 0,
                                 Manager.GetWindowStateByStartupModeStyle(startupMode));
         }
         else
            throw new ApplicationException("in Property.onStartupMode()");
      }

      /// <summary>
      /// set the visible property of statusBar; forces its refresh display and refresh display of the form's height and minimum height properties.
      /// </summary>
      private void onDisplayStatusBar()
      {
         var form = (MgFormBase)_parentObj;
         MgStatusBar statusBar = form.GetStatusBar(false);

         if (statusBar != null)
         {
            statusBar.setProp(PropInterface.PROP_TYPE_VISIBLE, getValue());
            statusBar.getProp(PropInterface.PROP_TYPE_VISIBLE).RefreshDisplay(true);
         }
      }

      /// <summary>
      ///   auto wide property
      /// </summary>
      private void onAutoWide()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
            Commands.addAsync(CommandType.PROP_SET_AUTO_WIDE, getObjectByParentObj(), getLine(), getValueBoolean());
         else
            throw new ApplicationException("in Property.onAutoWide() the control isn't MgControl");
      }

      /// <summary>
      ///   expanded image index property
      /// </summary>
      private void onExpandedImageIdx()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
            Commands.addAsync(CommandType.PROP_SET_EXPANDED_IMAGEIDX, getObjectByParentObj(), getLine(), getValueInt());
         else
            throw new ApplicationException("in Property.onExpandedImageIdx() the control isn't MgControl");
      }

      /// <summary>
      ///   collapsed image index property
      /// </summary>
      private void onCollapsedImageIdx()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
            Commands.addAsync(CommandType.PROP_SET_COLLAPSED_IMAGEIDX, getObjectByParentObj(), getLine(), getValueInt());
         else
            throw new ApplicationException("in Property.onCollapsedImageIdx() the control isn't MgControl");
      }

      /// <summary>
      ///   parked expanded image index property
      /// </summary>
      private void onParkedImageIdx()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
            Commands.addAsync(CommandType.PROP_SET_PARKED_IMAGEIDX, getObjectByParentObj(), getLine(), getValueInt());
         else
            throw new ApplicationException("in Property.onParkedImageIdx() the control isn't MgControl");
      }

      /// <summary>
      ///   parked collapsed image index property
      /// </summary>
      private void onParkedCollapsedImageIdx()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
            Commands.addAsync(CommandType.PROP_SET_PARKED_COLLAPSED_IMAGEIDX, getObjectByParentObj(), getLine(),
                              getValueInt());
         else
            throw new ApplicationException("in Property.onParkedCollapsedImageIdx() the control isn't MgControl");
      }

      /// <summary>
      ///   translator property (JPN: IME support)
      /// </summary>
      private void onTranslator()
      {
         var ctrl = (MgControlBase)_parentObj;

         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL && ctrl.Type == MgControlType.CTRL_TYPE_TEXT)
         {
            UtilImeJpn utilImeJpn = Manager.UtilImeJpn;
            if (utilImeJpn == null)
               return;

            PIC ctrlPic = ctrl.getPIC();
            int imeMode;
            if (ctrlPic.isAttrAlpha() || ctrlPic.isAttrUnicode() || ctrlPic.isAttrBlob())
            {
               imeMode = getValueInt();
               if (imeMode == 0)
               {
                  // -- if exp is evaluated to 0, it means IME off.
                  if (isExpression())
                     imeMode = UtilImeJpn.IME_FORCE_OFF;
                  else
                  {
                     int picImeMode = ctrlPic.getImeMode();
                     if (picImeMode != -1)
                        imeMode = picImeMode;
                  }
               }
            }
            else
               imeMode = UtilImeJpn.IME_FORCE_OFF;

            if (utilImeJpn.isValid(imeMode))
               Commands.addAsync(CommandType.PROP_SET_TRANSLATOR, getObjectByParentObj(), getLine(), imeMode);
         }
      }

      /// <summary>
      ///   Horizontal Alignment property
      /// </summary>
      private void onHorizantalAlignment()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
         {
            bool setHorAligment = true;
            int horAligment = getValueInt();

            if (setHorAligment)
               Commands.addAsync(CommandType.PROP_SET_HORIZANTAL_ALIGNMENT, getObjectByParentObj(), getLine(),
                                 horAligment);
         }
         else
            throw new ApplicationException("in Property.onHorizantalAlignment()");
      }

      /// <summary>
      ///   Horizontal Alignment property
      /// </summary>
      private void onVerticalAlignment()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
         {
            var mgcontrol = ((MgControlBase)_parentObj);
            int verAligment = getValueInt();
            // If the control is lable and multiline is true, vertical align is 
            // always "top" in standard framework and "center" in compact framework.
            // So, to make the behavior common, it should be set to "top" for mobile as well.
            // There is no harm in setting it for standard framework as well, so there is 
            // no need to do it conditionally.
            if (mgcontrol.isLabel() && mgcontrol.isMultiline())
               verAligment = (int)AlignmentTypeVert.Top;
            Commands.addAsync(CommandType.PROP_SET_VERTICAL_ALIGNMENT, getObjectByParentObj(), getLine(),
                              verAligment);
         }
         else
            throw new ApplicationException("in Property.onHorizantalAlignment()");
      }

      /// <summary>
      ///   Radio Button Appearance property
      /// </summary>
      private void onRadioButtonAppearance()
      {
         Boolean failed = true;

         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
         {
            MgControlType ctrlType = ((MgControlBase)_parentObj).Type;
            if (ctrlType == MgControlType.CTRL_TYPE_RADIO)
            {
               failed = false;
               Commands.addAsync(CommandType.PROP_SET_RADIO_BUTTON_APPEARANCE, getObjectByParentObj(), getLine(),
                                 getValueInt());
            }
         }

         if (failed)
            throw new ApplicationException("in Property.onRadioButtonAppearance()");
      }

      /// <summary>
      ///   Multiline property
      /// </summary>
      private void onMultiline()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
            Commands.addAsync(CommandType.PROP_SET_MULTILINE, getObjectByParentObj(), getLine(), getValueBoolean());
         else
            throw new ApplicationException("in Property.onMultiline()");
      }

      /// <summary>
      ///   ThreeStates property
      /// </summary>
      private void onThreeStates()
      {
         Boolean failed = true;
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
         {
            MgControlType ctrlType = ((MgControlBase)_parentObj).Type;
            if (ctrlType == MgControlType.CTRL_TYPE_CHECKBOX)
            {
               failed = false;
               Commands.addAsync(CommandType.PROP_SET_THREE_STATE, getObjectByParentObj(), getLine(),
                                 getValueBoolean());
            }
         }

         if (failed)
            throw new ApplicationException("in Property.onThreeStates()");
      }

      /// <summary>
      ///   Password property
      /// </summary>
      private void onPassword()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
            Commands.addAsync(CommandType.SET_PROPERTY, getObjectByParentObj(), getLine(), "password", getValueBoolean());
         else
            throw new ApplicationException("in Property.onPassword()");
      }

      /// <summary>
      ///   Set Multiline WordWrap Scroll
      /// </summary>
      private void onMultilineWordWrapScroll()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
            Commands.addAsync(CommandType.PROP_SET_MULTILINE_WORDWRAP_SCROLL, getObjectByParentObj(), getLine(), getValueInt());
         else
            throw new ApplicationException("in Property.onMultilineWordWrapScroll()");
      }

      /// <summary>
      ///   Set Multiline Vertical Scroll
      /// </summary>
      private void onMultilineVerticalScroll()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
            Commands.addAsync(CommandType.PROP_SET_MULTILINE_VERTICAL_SCROLL, getObjectByParentObj(), getLine(), getValueBoolean());
         else
            throw new ApplicationException("in Property.onMultilineVerticalScroll()");
      }

      /// <summary>
      ///   Sets AllowCR property of MultiLine Edit Box
      /// </summary>
      private void onMuliLineAllowCR()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
            Commands.addAsync(CommandType.PROP_SET_MULTILINE_ALLOW_CR, getObjectByParentObj(), getLine(), getValueBoolean());
         else
            throw new ApplicationException("in Property.onMuliLineAllowCR()");
      }

      private void onBorderStyle()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
         {
            MgControlType ctrlType = ((MgControlBase)_parentObj).Type;
            switch (ctrlType)
            {
               case MgControlType.CTRL_TYPE_RADIO:
               case MgControlType.CTRL_TYPE_CHECKBOX:
                  Commands.addAsync(CommandType.PROP_SET_BORDER_STYLE, getObjectByParentObj(), getLine(), getValueInt());
                  break;

               default:
                  // all other controls we are not suppoted;
                  break;
            }
         }
      }

      /// <summary>
      ///   style definition: Radio list: 2D & 3D Raised, Check box : 2D & 3D Sunken, ComboBox : 2D & 3D Sunken
      /// </summary>
      private void onStyle3D()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
         {
            MgControlType ctrlType = ((MgControlBase)_parentObj).Type;
            switch (ctrlType)
            {
               case MgControlType.CTRL_TYPE_CHECKBOX:
               case MgControlType.CTRL_TYPE_RADIO:
               case MgControlType.CTRL_TYPE_COMBO:
               case MgControlType.CTRL_TYPE_LINE:
               case MgControlType.CTRL_TYPE_LABEL:
               case MgControlType.CTRL_TYPE_IMAGE:
               case MgControlType.CTRL_TYPE_RICH_EDIT:
               case MgControlType.CTRL_TYPE_GROUP:
                  Commands.addAsync(CommandType.PROP_SET_STYLE_3D, getObjectByParentObj(), getLine(), getValueInt());
                  break;

               default:
                  // all other controls we are not suppoted;
                  break;
            }
         }
         else
            throw new ApplicationException("in Property.onStyle3D()");
      }

      /// <summary>
      ///   Set Checkbox main style
      /// </summary>
      private void onCheckBoxMainStyle()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
            Commands.addAsync(CommandType.PROP_SET_CHECKBOX_MAIN_STYLE, getObjectByParentObj(), getLine(), getValueInt());
         else
            throw new ApplicationException("in Property.onMultilineVerticalScroll()");
      }

      /// <summary>
      ///   Set the Border
      ///   Border definition: the border is enable only for Edit, Table,SubForm,ListBox, Tree
      /// </summary>
      private void onBorder()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
         {
            var ctrl = (MgControlBase)_parentObj;
            MgControlType ctrlType = ctrl.Type;
            switch (ctrlType)
            {
               case MgControlType.CTRL_TYPE_TEXT:
               case MgControlType.CTRL_TYPE_TABLE:
               case MgControlType.CTRL_TYPE_SUBFORM:
               case MgControlType.CTRL_TYPE_LIST:
               case MgControlType.CTRL_TYPE_LABEL:
               case MgControlType.CTRL_TYPE_IMAGE:
               case MgControlType.CTRL_TYPE_TREE:
               case MgControlType.CTRL_TYPE_BROWSER:
              
               case MgControlType.CTRL_TYPE_RICH_TEXT:
               case MgControlType.CTRL_TYPE_SB_LABEL:
                  Commands.addAsync(CommandType.SET_CLASS, getObjectByParentObj(), ctrl.getDisplayLine(false), "showborder", getValueBoolean() ? "has_border" : "hidden_border");
                  break;

               default:
                  throw new ApplicationException("in Property.onBorder()");
            }
         }
      }

      /// <summary>
      ///   onMinBox
      /// </summary>
      private void onMinBox()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_FORM)
            Commands.addAsync(CommandType.PROP_SET_MINBOX, getObjectByParentObj(), getValueBoolean());
         else
            throw new ApplicationException("in Property.onMinBox()");
      }

      /// <summary>
      ///   onMaxBox
      /// </summary>
      private void onMaxBox()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_FORM)
            Commands.addAsync(CommandType.PROP_SET_MAXBOX, getObjectByParentObj(), getValueBoolean());
         else
            throw new ApplicationException("in Property.onMaxBox()");
      }

      /// <summary>
      ///   onSystemMenu
      /// </summary>
      private void onSystemMenu()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_FORM)
            Commands.addAsync(CommandType.PROP_SET_SYSTEM_MENU, getObjectByParentObj(), getValueBoolean());
         else
            throw new ApplicationException("in Property.onSystemMenu()");
      }

      /// <summary>
      ///   onTitleBar
      /// </summary>
      private void onTitleBar()
      {
         bool showTitleBar;
#if !PocketPC
         showTitleBar = _parentObj.getProp(PropInterface.PROP_TYPE_TITLE_BAR).getValueBoolean();
#else
         // On Mobile there's always a title bar, as we don't have full screen mode
         showTitleBar = true;
#endif
         if (_parentType == GuiConstants.PARENT_TYPE_FORM)
            Commands.addAsync(CommandType.PROP_SET_TITLE_BAR, getObjectByParentObj(), showTitleBar);
         else
            throw new ApplicationException("in Property.onTitleBar()");
      }

      /// <summary>
      ///   onStartupPosition
      /// </summary>
      private void onStartupPosition()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_FORM)
         {
            //update the StartupPosition on the form
            var windowPosition = ((MgFormBase)_parentObj).GetStartupPosition();
            Commands.addAsync(CommandType.PROP_SET_STARTUP_POSITION, getObjectByParentObj(), 0, windowPosition);
         }
         else
            throw new ApplicationException("in Property.onSystemMenu()");
      }

      /// <summary>
      ///   onShowFullRow
      /// </summary>
      private void onShowFullRow()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
            Commands.addAsync(CommandType.PROP_SHOW_FULL_ROW, getObjectByParentObj(), getValueBoolean());
         else
            throw new ApplicationException("in Property.onShowFullRow()");
      }

      /// <summary>
      ///   onTopBorder
      /// </summary>
      private void onTopBorder()
      {
         var columnCtrl = (MgControlBase)_parentObj;

         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL && columnCtrl.Type == MgControlType.CTRL_TYPE_COLUMN)
         {
            bool PropValue = getValueBoolean();
            Commands.addAsync(CommandType.PROP_SET_TOP_BORDER, columnCtrl, 0, PropValue);
         }
         else
            throw new ApplicationException("in Property.onColumnSortable() the control isn't Column control");

      }


      /// <summary>
      ///   onRightBorder
      /// </summary>
      private void onRightBorder()
      {
         var columnCtrl = (MgControlBase)_parentObj;

         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL && columnCtrl.Type == MgControlType.CTRL_TYPE_COLUMN)
         {
            bool PropValue = getValueBoolean();

            Commands.addAsync(CommandType.PROP_SET_RIGHT_BORDER, columnCtrl, 0, PropValue);
         }
         else
            throw new ApplicationException("in Property.onColumnSortable() the control isn't Column control");

      }


      /// <summary>
      ///   onShowScrollbar
      /// </summary>
      private void onShowScrollbar()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
         {
            bool PropValue = getValueBoolean();
            Commands.addAsync(CommandType.PROP_SHOW_SCROLLBAR, getObjectByParentObj(), PropValue);
         }
         else
            throw new ApplicationException("in Property.onShowScrollbar()");
      }

      /// <summary>
      ///   onColumnDivider
      /// </summary>
      private void onColumnDivider()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
            Commands.addAsync(CommandType.PROP_COLUMN_DIVIDER, getObjectByParentObj(), getValueBoolean());
         else
            throw new ApplicationException("in Property.onColumnDivider()");
      }

      /// <summary>
      ///   onLineDivider
      /// </summary>
      private void onLineDivider()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
            Commands.addAsync(CommandType.PROP_LINE_DIVIDER, getObjectByParentObj(), getValueBoolean());
         else
            throw new ApplicationException("in Property.onLineDivider()");
      }

      /// <summary>
      ///   onRowHighLightType
      /// </summary>
      private void onRowHighLightType()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
            Commands.addAsync(CommandType.PROP_ROW_HIGHLITING_STYLE, getObjectByParentObj(), 0, getValueInt());
         else
            throw new ApplicationException("in Property.onRowHighLightType()");
      }

      /// <summary>
      ///   onLineWidth
      /// </summary>
      private void onLineWidth()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
         {
            //this property is char 
            String str = getValue();
            if (str != null)
               Commands.addAsync(CommandType.PROP_SET_LINE_WIDTH, getObjectByParentObj(), getLine(), str[0]);
         }
         else
            throw new ApplicationException("in Property.onLineWidth()");
      }

      /// <summary>
      ///   onLineStyle
      /// </summary>
      private void onLineStyle()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
            Commands.addAsync(CommandType.PROP_SET_LINE_STYLE, getObjectByParentObj(), getLine(), getValueInt());
         else
            throw new ApplicationException("in Property.onLineStyle()");
      }

      /// <summary>
      ///   onStaticType
      /// </summary>
      private void onStaticType()
      {
         const int CTRL_STATIC_TYPE_NESW_LINE = 0x0010;
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
         {
            var ctrl = (MgControlBase)_parentObj;
            if (ctrl.isLineControl())
            {
               int val = getValueInt();
               CtrlLineDirection dir;
               if ((val & CTRL_STATIC_TYPE_NESW_LINE) != 0)
                  dir = CtrlLineDirection.Asc;
               else
                  dir = CtrlLineDirection.Des;

               Commands.addAsync(CommandType.PROP_SET_LINE_DIRECTION, getObjectByParentObj(), getLine(), (int)dir);
            }
         }
         else
            throw new ApplicationException("in Property.onStaticType()");
      }

      /// <summary>
      /// on Selection mode. This will set the selction mode of list box to single or multiple.
      /// </summary>
      private void onSelectionMode()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
            Commands.addAsync(CommandType.PROP_SET_SELECTION_MODE, getObjectByParentObj(), getLine(), getValueInt());
         else
            throw new ApplicationException("in Property.onSelectionMode()");
      }

      /// <summary>
      /// on Refresh when Hidden. If this property is changed to 'true' refresh the subform if it is invisible.
      /// </summary>
      private void onRefreshWhenHidden()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
         {
            if (DisplayConvertor.toBoolean(_val))
            {
               MgControlBase ctrl = (MgControlBase)_parentObj;
               Debug.Assert(ctrl.isSubform() || ctrl.isFrameFormControl());
               if (!ctrl.isVisible())
                  ctrl.RefreshSubform();
            }
         }
         else
            throw new ApplicationException("in Property.onRefreshWhenHidden()");
      }

      /// <summary>
      ///   refresh generic property
      /// </summary>
      private void onGenericProperty()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
         {
            Debug.Assert(IsGeneric);

            var ctrl = (MgControlBase)_parentObj;

            //get the property info
            PropertyInfo propInfo = PropInfo;
            if (propInfo != null) //it is possible that the property does not exist, in this case just ignore it
            {
               //convert the display value of the property  to mgValue
               Type type = null;
               String val = _val;
               switch (_dataType)
               {
                  case StorageAttribute.NUMERIC:
                     {
                        val = DisplayConvertor.Instance.disp2mg(val, "", _pic, ctrl.getCompIdx(), BlobType.CONTENT_TYPE_UNKNOWN);
                        type = typeof(int);
                     }
                     break;
                  case StorageAttribute.BOOLEAN:
                     type = typeof(bool);
                     break;
                  case StorageAttribute.UNICODE:
                     type = typeof(String);
                     break;
                  case StorageAttribute.DOTNET:
                     type = propInfo.PropertyType;
                     break;
               }

               //convert the mgValue to dot net data type
               Object DotNetObjectValue = null;
               if (type != null)
               {
                  if (_dataType == StorageAttribute.DOTNET)
                  {
                     int key = BlobType.getKey(val);
                     Object obj = DNManager.getInstance().DNObjectsCollection.GetDNObj(key);
                     DotNetObjectValue = DNConvert.doCast(obj, type);
                  }
                  else
                     DotNetObjectValue = DNConvert.convertMagicToDotNet(val, _dataType, type);
               }

               //set the property value 
               ReflectionServices.SetPropertyValue(propInfo, ctrl, new object[] { }, DotNetObjectValue);
            }
         }
         else
            Debug.Assert(false);
      }

      /// <summary>
      ///   onTrackSelection
      /// </summary>
      private void onHotTrack()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
            Commands.addAsync(CommandType.PROP_HOT_TRACK, getObjectByParentObj(), getValueBoolean());
         else
            throw new ApplicationException("in Property.onTrackSelection()");
      }

      /// <summary>
      ///   onShowButtons
      /// </summary>
      private void onShowButtons()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
            Commands.addAsync(CommandType.PROP_SHOW_BUTTONS, getObjectByParentObj(), getValueBoolean());
         else
            throw new ApplicationException("in Property.onShowButtons()");
      }

      /// <summary>
      ///   onLinesAsRoot
      /// </summary>
      private void onLinesAsRoot()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
            Commands.addAsync(CommandType.PROP_LINES_AT_ROOT, getObjectByParentObj(), getValueBoolean());
         else
            throw new ApplicationException("in Property.onLinesAsRoot()");
      }

      /// <summary>
      ///   onWindowType
      /// </summary>
      private void onWindowType()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_FORM)
         {
            MgFormBase form = getForm();

            //when it is subform the form border style will be None
            if (!form.isSubForm())
            {
               var borderStyle = (BorderType)_parentObj.getProp(PropInterface.PROP_TYPE_BORDER_STYLE).getValueInt();
               Commands.addAsync(CommandType.PROP_SET_FORM_BORDER_STYLE, getObjectByParentObj(), 0, borderStyle);
            }
         }
         else
            throw new ApplicationException("in Property.onWindowType()");
      }

      /// <summary>
      /// </summary>
      private void onRightToLeft()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
         {
            var ctrl = (MgControlBase)_parentObj;
            MgControlType ctrlType = ctrl.Type;
            switch (ctrlType)
            {
               case MgControlType.CTRL_TYPE_TABLE:
                  //the right to left property on Table is handle as a style
                  break;

               case MgControlType.CTRL_TYPE_BUTTON:
               case MgControlType.CTRL_TYPE_COMBO:
               case MgControlType.CTRL_TYPE_LIST:
               case MgControlType.CTRL_TYPE_RADIO:
               case MgControlType.CTRL_TYPE_CHECKBOX:
               case MgControlType.CTRL_TYPE_GROUP:
               case MgControlType.CTRL_TYPE_TAB:
               case MgControlType.CTRL_TYPE_TREE:
               case MgControlType.CTRL_TYPE_RICH_EDIT:
               case MgControlType.CTRL_TYPE_RICH_TEXT:
               case MgControlType.CTRL_TYPE_TEXT:
               case MgControlType.CTRL_TYPE_LABEL:
                  if (!(ctrl.Type == MgControlType.CTRL_TYPE_TEXT && (ctrl.getPIC().getAttr() == StorageAttribute.NUMERIC)))
                     Commands.addAsync(CommandType.PROP_SET_RIGHT_TO_LEFT, getObjectByParentObj(), getLine(), getValueBoolean());
                  break;

               default:
                  throw new ApplicationException("in Property.onRighToLeft()");
            }
         }
         else if (_parentType == GuiConstants.PARENT_TYPE_FORM)
            Commands.addAsync(CommandType.PROP_SET_RIGHT_TO_LEFT, getObjectByParentObj(), 0, getValueBoolean());
      }

      /// <summary>
      /// </summary>
      private void onTabControlSide()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
            Commands.addAsync(CommandType.PROP_TAB_CONTROL_SIDE, getObjectByParentObj(), 0, getValueInt());
         else
            throw new ApplicationException("in Property.onTabControlSide()");
      }

      /// <summary>
      /// 
      /// </summary>
      private void onTabControlTabsWidth()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
            Commands.addAsync(CommandType.PROP_SET_TAB_SIZE_MODE, getObjectByParentObj(), 0, getValueInt());
         else
            throw new ApplicationException("in Property.onTabControlTabsWidth()");
      }

      /// <summary>
      /// saves the value as the studio value, for the right properties
      /// </summary>
      private void SetInitialValueAsStudioValue()
      {
         if ((_parentType == GuiConstants.PARENT_TYPE_CONTROL || _parentType == GuiConstants.PARENT_TYPE_FORM) && 
            StudioValue == null && 
            DesignerPropertiesBuilder.PropertiesUsedByRuntimeDesigner.Contains(_id))
            StudioValue = _val;
      }

      /// <summary>
      /// Set TopBorderMargin property
      /// </summary>
      private void onTopBorderMargin()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL && ((MgControlBase)_parentObj).isGroup())
             Commands.addAsync(CommandType.PROP_SET_TOP_BORDER_MARGIN, getObjectByParentObj(), 0, getValueBoolean());
      }

      /// <summary>
      /// Set FillWidth property
      /// </summary>
      private void onFillWidth()
      {
         Debug.Assert(_parentType == GuiConstants.PARENT_TYPE_CONTROL && ((MgControlBase)_parentObj).isTableControl());

         Commands.addAsync(CommandType.PROP_SET_FILL_WIDTH, getObjectByParentObj(), getValueBoolean());
      }

      /// <summary>
      /// Set MultiColumnDisplay property
      /// </summary>
      private void onMultiColumnDisplay()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
            Commands.addAsync(CommandType.PROP_SET_MULTI_COLUMN_DISPLAY, getObjectByParentObj(), getValueBoolean());
         else
            throw new ApplicationException("in Property.onMultiColumnDisplay()");
      }

      /// <summary>
      /// Set Show Ellipsis property
      /// </summary>
      private void onShowEllipsis()
      {
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
            Commands.addAsync(CommandType.PROP_SET_SHOW_ELLIPSIS, getObjectByParentObj(), getValueBoolean());
         else
            throw new ApplicationException("in Property.onMultiColumnDisplay()");
      }


      /// <summary>
      /// Set TitlePadding property
      /// </summary>
      private void onTitlePadding()
      {
         Debug.Assert(_parentType == GuiConstants.PARENT_TYPE_CONTROL && ((MgControlBase)_parentObj).isTabControl());

         Commands.addAsync(CommandType.PROP_SET_TITLE_PADDING, getObjectByParentObj(), 0, getValueInt());
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="line"></param>
      private void onRowBGColor(int line)
      {
         int colorIndex = getValueInt();
         if (colorIndex > 0)
         {
            MgColor bgColor = Manager.GetColorsTable().getBGColor(colorIndex);
            Commands.addAsync(CommandType.PROP_SET_ROW_BG_COLOR, getObjectByParentObj(), line, bgColor);
         }
      }
#if !PocketPC
      ///<summary>
      ///</summary>
      private void onAllowDrop()
      {
         // Dropping is allowed on controls as well as Form.
         Commands.addAsync(CommandType.PROP_SET_ALLOW_DROP, getObjectByParentObj(), getLine(), getValueBoolean());
      }

      /// <summary>
      /// </summary>
      private void onAllowDrag()
      {
         // Dragging is allowed only on Controls.
         if (_parentType == GuiConstants.PARENT_TYPE_CONTROL)
            Commands.addAsync(CommandType.PROP_SET_ALLOW_DRAG, getObjectByParentObj(), getLine(), getValueBoolean());
      }

      /// <summary>
      /// 
      /// </summary>
      private void onShowInWindowMenu()
      {
         Debug.Assert(_parentType == GuiConstants.PARENT_TYPE_FORM);

         MgFormBase mgForm = (MgFormBase)_parentObj;
         if (getValueBoolean() && mgForm.IsValidWindowTypeForWidowList)
         {
            Manager.MenuManager.WindowList.Add((MgFormBase)_parentObj);
         }
      }
#endif

      /// <summary>
      /// set what is needed on the property, if the control should be using a value from the runtime designer
      /// </summary>
      /// <param name="name"></param>
      public void SetDesignerValue(string name, object value)
      {
         designerInfoFlag |= DesignerInfoFlags.IsDesignerValue;
         if (name.Equals(Constants.WinPropBackColor))
            designerInfoFlag |= DesignerInfoFlags.IsBackColor;
         else if (name.Equals(Constants.WinPropForeColor))
            designerInfoFlag |= DesignerInfoFlags.IsForeColor;
         else if (name.Equals(Constants.WinPropGradientStyle))
            designerInfoFlag |= DesignerInfoFlags.IsGradientStyle;

         if(_id == PropInterface.PROP_TYPE_VISIBLE)
         {
            // if the control is hidden, we need to hide it here, so all calculations which use this property will have the right behavior
            _expId = 0;
            _val = "0";
         }

         if (_id == PropInterface.PROP_TYPE_LAYER)
         {
            MgControlBase parentControl = (MgControlBase)_parentObj;
            parentControl.Layer = (int)value;
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="flag"></param>
      /// <returns></returns>
      bool IsDesignerInfoFlagSet(DesignerInfoFlags flag)
      {
         return (designerInfoFlag & flag) != 0;
      }

      /// <summary>
      /// does the value of this property result from the runtime designer
      /// </summary>
      /// <returns></returns>
      public bool IsDesignerValue()
      {
         return IsDesignerInfoFlagSet(DesignerInfoFlags.IsDesignerValue);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="idx"></param>
      public void RemovePrevValIndexAt(int idx)
      {
         if(_prevValues.Count > idx)
            _prevValues.RemoveAt(idx);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="idx"></param>
      public void InsertPrevValAt(int idx)
      {
         _prevValues.Insert(idx, null);
      }
#if DEBUG
      public static string GetPropertyName(int propTypeId)
      {
         FieldInfo[] fields = typeof(PropInterface).GetFields(BindingFlags.Public | BindingFlags.Static);
         foreach (var fieldInfo in fields)
         {
            if (fieldInfo.GetValue(null).Equals(propTypeId))
               return fieldInfo.Name;
         }
         return "{Field ID " + propTypeId + "}";
      }

      public override string ToString()
      {
         return String.Format("{0} = {1} (exp={2}, studio value={3})", Property.GetPropertyName(_id), _val == null ? "is null" : _val, this._expId, this.StudioValue); ;
      }
#endif
   }
}
