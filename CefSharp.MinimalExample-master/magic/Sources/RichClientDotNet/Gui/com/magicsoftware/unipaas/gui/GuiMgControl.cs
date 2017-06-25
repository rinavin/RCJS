using System;
using System.Diagnostics;
using com.magicsoftware.util;

namespace com.magicsoftware.unipaas.gui
{
   public abstract class GuiMgControl
   {
      public bool ForceRefreshPropertyWidth { get; set; }
      public bool ForceRefreshPropertyHight { get; set; }

      private MgControlType _type = 0;
      public MgControlType Type
      {
         get { return _type; }
         set
         {
            if (_type == 0)
               _type = value;
            else
               Debug.Assert(false);
         }
      }

      public CtrlButtonTypeGui ButtonStyle { get; set; }
      public bool IsRepeatable { get; set; }

      public bool IsTableChild
      {
         get
         {
            GuiMgControl parent = getParent() as GuiMgControl;
            bool parentIsTable = parent != null && parent.isTableControl();

            return parentIsTable && !isColumnControl();
         }
      }

      /// <summary>
      /// Saves control z order
      /// </summary>
      public int ControlZOrder { get; protected set; }

      public bool IsTableHeaderChild
      {
         get
         {
            return !IsRepeatable && IsTableChild;
         }
      }

      public int Layer { get; set; }
      public CheckboxMainStyle CheckBoxMainStyle { get; set; }
      public RbAppearance RadioButtonAppearance { get; set; }
      public bool IsWideControl { get; set; } //if the control is a wide control

      public String Name { get; protected internal set; }

      /// <summary>
      ///   get the name of the Control with the line number for repeatable controls
      /// </summary>
      public String getName(int line)
      {
         if (IsRepeatable)
         {
            if (line > Int32.MinValue)
               return Name + "_" + line;
            // else there is no current row, yet (parsing time for example). Try first row.
            return Name + "_0";
         }

         return Name;
      }

      public GuiMgForm GuiMgForm { get; protected internal set; }

      public abstract Object getParent();

       /// <summary>
       /// return true if this control is a direct child of frame set
       /// </summary>
       /// <returns></returns>
      public bool IsDirectFrameChild
      {
          get
          {
              bool isFrameChild = false;
              if (isSubform())
                  isFrameChild = getParent() != null && getParent() is GuiMgControl && ((GuiMgControl)getParent()).isFrameSet();

              if ((isFrameFormControl() || isFrameChild))
                  isFrameChild = true;

              return isFrameChild;
          }
      }
      /// <summary>
      ///   return true if it is container control
      /// </summary>
      /// <returns>
      /// </returns>
      public bool isContainerControl()
      {
         return (_type == MgControlType.CTRL_TYPE_CONTAINER);
      }

      /// <summary>
      ///   returns true if this control is a static control
      /// </summary>
      public bool isStatic()
      {
         return ((_type == MgControlType.CTRL_TYPE_LABEL || _type == MgControlType.CTRL_TYPE_GROUP ||
                  _type == MgControlType.CTRL_TYPE_TABLE || _type == MgControlType.CTRL_TYPE_COLUMN ||
                  _type == MgControlType.CTRL_TYPE_STATUS_BAR || _type == MgControlType.CTRL_TYPE_SB_IMAGE || 
                  _type == MgControlType.CTRL_TYPE_SB_LABEL || _type == MgControlType.CTRL_TYPE_LINE));
      }

      /// <summary>
      ///   returns true if this control is a status bar control
      /// </summary>
      public bool IsStatusBar()
      {
         return (_type == MgControlType.CTRL_TYPE_STATUS_BAR);
      }

      /// <summary>
      ///   returns true if this control is a pane of the status bar
      /// </summary>
      /// <returns></returns>
      public bool IsStatusPane()
      {
         return (_type == MgControlType.CTRL_TYPE_SB_LABEL || _type == MgControlType.CTRL_TYPE_SB_IMAGE);
      }

      /// <summary>
      ///   return true if it is frame form control
      /// </summary>
      /// <returns>
      /// </returns>
      public bool isFrameFormControl()
      {
         return (_type == MgControlType.CTRL_TYPE_FRAME_FORM);
      }

      /// <summary>
      ///   return true for table control
      /// </summary>
      /// <returns>
      /// </returns>
      public bool isTableControl()
      {
         return (_type == MgControlType.CTRL_TYPE_TABLE);
      }

      /// <summary>
      ///   return true for tab control
      /// </summary>
      /// <returns>
      /// </returns>
      public bool isTabControl()
      {
         return (_type == MgControlType.CTRL_TYPE_TAB);
      }

      /// <summary>
      ///   return true for line control
      /// </summary>
      /// <returns>
      /// </returns>
      public bool isLineControl()
      {
         return (_type == MgControlType.CTRL_TYPE_LINE);
      }

      /// <summary>
      ///   return true for dotnet control
      /// </summary>
      /// <returns></returns>
      public bool IsDotNetControl()
      {
         return (_type == MgControlType.CTRL_TYPE_DOTNET);
      }

      /// <summary>
      ///   return tree for tree control
      /// </summary>
      /// <returns>
      /// </returns>
      public bool isTreeControl()
      {
         return (_type == MgControlType.CTRL_TYPE_TREE);
      }

      /// <summary>
      ///   return tree for tree and text control
      /// </summary>
      /// <returns>
      /// </returns>
      public bool isTextOrTreeControl()
      {
         return (_type == MgControlType.CTRL_TYPE_TREE || _type == MgControlType.CTRL_TYPE_TEXT);
      }

      /// <summary>
      ///   return true for rich edit control
      /// </summary>
      /// <returns></returns>
      public bool isRichEditControl()
      {
         return (_type == MgControlType.CTRL_TYPE_RICH_EDIT);
      }

      /// <summary>
      ///   return true for image control
      /// </summary>
      /// <returns></returns>
      public bool isImageControl()
      {
         return (_type == MgControlType.CTRL_TYPE_IMAGE);
      }

      /// <summary>
      ///   returns true if this control is a sub form control
      /// </summary>
      public bool isSubform()
      {
         return (_type == MgControlType.CTRL_TYPE_SUBFORM);
      }

      /// <summary>
      ///   returns true if this control is a sub form control
      /// </summary>
      public bool isRadio()
      {
         return (_type == MgControlType.CTRL_TYPE_RADIO);
      }

      /// <summary>
      ///   returns true if this control is a ComboBox control
      /// </summary>
      public bool isComboBox()
      {
         return (_type == MgControlType.CTRL_TYPE_COMBO);
      }

      /// <summary>
      ///   returns true if this control is a ListBox control
      /// </summary>
      public bool isListBox()
      {
         return (_type == MgControlType.CTRL_TYPE_LIST);
      }

      /// <summary>
      ///   returns true if this control is a isButton control
      /// </summary>
      public bool isButton()
      {
         return (_type == MgControlType.CTRL_TYPE_BUTTON);
      }

      /// <summary>
      ///   returns true if this control is a label control
      /// </summary>
      public bool isLabel()
      {
         return (_type == MgControlType.CTRL_TYPE_LABEL);
      }

      /// <summary>
      ///   returns true if this control is a Group control
      /// </summary>
      public bool isGroup()
      {
         return (_type == MgControlType.CTRL_TYPE_GROUP);
      }

      /// <summary>
      ///   returns true if this control is a frame control
      /// </summary>
      public bool isFrameSet()
      {
         return (_type == MgControlType.CTRL_TYPE_FRAME_SET);
      }

      public bool isRichText()
      {
         return (_type == MgControlType.CTRL_TYPE_RICH_TEXT);
      }

      /// <summary>
      ///   returns true if the control is either combo or list
      /// </summary>
      /// <returns>
      /// </returns>
      public bool isSelectionCtrl()
      {
         return (_type == MgControlType.CTRL_TYPE_COMBO || _type == MgControlType.CTRL_TYPE_LIST);
      }

      /// <summary>
      ///   Returns true if the control is text.
      /// </summary>
      /// <returns>
      /// </returns>
      public bool isTextControl()
      {
         return (_type == MgControlType.CTRL_TYPE_TEXT);
      }

      /// <summary>
      ///   return true for browser control
      /// </summary>
      /// <returns></returns>
      public bool isBrowserControl()
      {
         return _type == MgControlType.CTRL_TYPE_BROWSER;
      }

      public bool isCheckBox()
      {
         return (_type == MgControlType.CTRL_TYPE_CHECKBOX);
      }

      public bool isColumnControl()
      {
         return (_type == MgControlType.CTRL_TYPE_COLUMN);
      }

      /// <summary>
      ///   Returns true if the control is a choice control: CTRL_TYPE_TAB, CTRL_TYPE_COMBO, CTRL_TYPE_LIST,
      ///   CTRL_TYPE_RADIO
      /// </summary>
      /// <returns></returns>
      public bool isChoiceControl()
      {
         bool val = false;
         switch (_type)
         {
            case MgControlType.CTRL_TYPE_TAB:
            case MgControlType.CTRL_TYPE_COMBO:
            case MgControlType.CTRL_TYPE_LIST:
            case MgControlType.CTRL_TYPE_RADIO:
               val = true;
               break;
         }
         return val;
      }

      public bool isContainer()
      {
         switch (_type)
         {
            case MgControlType.CTRL_TYPE_TAB:
            case MgControlType.CTRL_TYPE_TABLE:
            case MgControlType.CTRL_TYPE_GROUP:
            case MgControlType.CTRL_TYPE_CONTAINER:
               return true;
            default:
               return false;
         }
      }

      /// <summary>
      ///   returns true if this control is support gradient
      /// </summary>
      internal bool SupportGradient()
      {
         return (isButton() || isLabel() || isTabControl() || isGroup() || IsCheckBoxMainStyleButton() ||
                 IsRadioAppearanceButton());
      }

      /// <summary>
      ///   return TRUE if the control is button and his style is text on image
      /// </summary>
      /// <returns></returns>
      public bool IsImageButton()
      {
         bool IsImageOnButton = false;

         if (isButton())
            IsImageOnButton = (ButtonStyle == CtrlButtonTypeGui.Image);

         return IsImageOnButton;
      }


      /// <summary>
      ///   return TRUE if the control is button and his style is  push button
      /// </summary>
      /// <returns></returns>
      public bool IsButtonPushButton()
      {
         bool isButtonPushButton = false;

         if (isButton())
            isButtonPushButton = (ButtonStyle == CtrlButtonTypeGui.Push);

         return isButtonPushButton;
      }

      /// <summary>
      ///   return TRUE if the control is button and his style is Hyper Text
      /// </summary>
      /// <returns></returns>
      public bool IsHyperTextButton()
      {
         bool IsHyperText = false;

         if (isButton())
            IsHyperText = (ButtonStyle == CtrlButtonTypeGui.Hypertext);

         return IsHyperText;
      }

      /// <summary>
      ///   returns TRUE if the  main style of a check box is Button
      /// </summary>
      /// <returns></returns>
      public bool IsCheckBoxMainStyleButton()
      {
         bool isCheckBoxAppearanceButton = false;

         if (isCheckBox())
            isCheckBoxAppearanceButton = (CheckBoxMainStyle == CheckboxMainStyle.Button);

         return isCheckBoxAppearanceButton;
      }

      /// <summary>
      ///   reurn TRUE if the  Appearance of a radio button is Button
      /// </summary>
      /// <returns></returns>
      public bool IsRadioAppearanceButton()
      {
         bool isRadioAppearanceButton = false;

         if (isRadio())
            isRadioAppearanceButton = (RadioButtonAppearance == RbAppearance.Button);

         return isRadioAppearanceButton;
      }

      /// <summary>
      ///   returns the command type for creating the control
      /// </summary>
      /// <returns>: CommandType
      /// </returns>
      public CommandType getCreateCommandType()
      {
         CommandType commandType = CommandType.CREATE_EDIT;
         switch (_type)
         {
            case MgControlType.CTRL_TYPE_TREE:
               commandType = CommandType.CREATE_TREE;
               break;

            case MgControlType.CTRL_TYPE_BUTTON:
               commandType = CommandType.CREATE_BUTTON;
               break;

            case MgControlType.CTRL_TYPE_CHECKBOX:
               commandType = CommandType.CREATE_CHECK_BOX;
               break;

            case MgControlType.CTRL_TYPE_RADIO:
               commandType = CommandType.CREATE_RADIO_CONTAINER;
               break;

            case MgControlType.CTRL_TYPE_COMBO:
               commandType = CommandType.CREATE_COMBO_BOX;
               break;

            case MgControlType.CTRL_TYPE_LIST:
               commandType = CommandType.CREATE_LIST_BOX;
               break;

            case MgControlType.CTRL_TYPE_TEXT: // edit
               commandType = CommandType.CREATE_EDIT;
               break;

            case MgControlType.CTRL_TYPE_TABLE:
               commandType = CommandType.CREATE_TABLE;
               break;

            case MgControlType.CTRL_TYPE_COLUMN:
               commandType = CommandType.CREATE_COLUMN;
               break;

            case MgControlType.CTRL_TYPE_IMAGE:
               commandType = CommandType.CREATE_IMAGE;
               break;

            case MgControlType.CTRL_TYPE_SUBFORM:
               commandType = CommandType.CREATE_SUB_FORM;
               break;

            case MgControlType.CTRL_TYPE_FRAME_SET:
               commandType = CommandType.CREATE_FRAME_SET;
               break;

            case MgControlType.CTRL_TYPE_LABEL:
               commandType = CommandType.CREATE_LABEL;
               break;

            case MgControlType.CTRL_TYPE_TAB:
               commandType = CommandType.CREATE_TAB;
               break;

            case MgControlType.CTRL_TYPE_GROUP:
               commandType = CommandType.CREATE_GROUP;
               break;

            case MgControlType.CTRL_TYPE_CONTAINER:
               commandType = CommandType.CREATE_CONTAINER;
               break;

            case MgControlType.CTRL_TYPE_FRAME_FORM:
               commandType = CommandType.CREATE_FRAME_FORM;
               break;

            case MgControlType.CTRL_TYPE_BROWSER:
               commandType = CommandType.CREATE_BROWSER;
               break;

            case MgControlType.CTRL_TYPE_STATUS_BAR:
               commandType = CommandType.CREATE_STATUS_BAR;
               break;

            case MgControlType.CTRL_TYPE_SB_LABEL:
               commandType = CommandType.CREATE_SB_LABEL;
               break;

            case MgControlType.CTRL_TYPE_SB_IMAGE:
               commandType = CommandType.CREATE_SB_IMAGE;
               break;

            case MgControlType.CTRL_TYPE_RICH_EDIT:
               commandType = CommandType.CREATE_RICH_EDIT;
               break;

            case MgControlType.CTRL_TYPE_RICH_TEXT:
               commandType = CommandType.CREATE_RICH_TEXT;
               break;

            case MgControlType.CTRL_TYPE_LINE:
               commandType = CommandType.CREATE_LINE;
               break;

            case MgControlType.CTRL_TYPE_DOTNET:
               commandType = CommandType.CREATE_DOTNET;
               break;

            default:
               Debug.Assert(false);
               break;
         }
         return commandType;
      }

      /// <summary>
      ///   Function returns the controltype name when control type character is passed to it.
      /// </summary>
      /// <returns> Control type name</returns>
      internal string getControlTypeName()
      {
         string controlTypeName = null;
         switch (_type)
         {
            case MgControlType.CTRL_TYPE_BROWSER:
               controlTypeName = "Browser";
               break;
            case MgControlType.CTRL_TYPE_BUTTON:
               controlTypeName = "Button";
               break;
            case MgControlType.CTRL_TYPE_CHECKBOX:
               controlTypeName = "Checkbox";
               break;
            case MgControlType.CTRL_TYPE_COLUMN:
               controlTypeName = "Column";
               break;
            case MgControlType.CTRL_TYPE_COMBO:
               controlTypeName = "Combo";
               break;
            case MgControlType.CTRL_TYPE_CONTAINER:
               controlTypeName = "Container";
               break;
            case MgControlType.CTRL_TYPE_DOTNET:
               controlTypeName = "Dotnet";
               break;
            case MgControlType.CTRL_TYPE_FRAME_FORM:
               controlTypeName = "Frame form";
               break;
            case MgControlType.CTRL_TYPE_FRAME_SET:
               controlTypeName = "Frame set";
               break;
            case MgControlType.CTRL_TYPE_GROUP:
               controlTypeName = "Groupbox";
               break;
            case MgControlType.CTRL_TYPE_IMAGE:
               controlTypeName = "Image";
               break;
            case MgControlType.CTRL_TYPE_LABEL:
               controlTypeName = "Label";
               break;
            case MgControlType.CTRL_TYPE_LINE:
               controlTypeName = "Line";
               break;
            case MgControlType.CTRL_TYPE_LIST:
               controlTypeName = "List";
               break;
            case MgControlType.CTRL_TYPE_RADIO:
               controlTypeName = "Radio";
               break;
            case MgControlType.CTRL_TYPE_RICH_EDIT:
               controlTypeName = "Rich edit";
               break;
            case MgControlType.CTRL_TYPE_RICH_TEXT:
               controlTypeName = "Rich text";
               break;
            case MgControlType.CTRL_TYPE_SB_IMAGE:
               controlTypeName = "Status bar image";
               break;
            case MgControlType.CTRL_TYPE_SB_LABEL:
               controlTypeName = "Status bar label";
               break;
            case MgControlType.CTRL_TYPE_STATUS_BAR:
               controlTypeName = "Status bar";
               break;
            case MgControlType.CTRL_TYPE_SUBFORM:
               controlTypeName = "Subform";
               break;
            case MgControlType.CTRL_TYPE_TAB:
               controlTypeName = "Tab";
               break;
            case MgControlType.CTRL_TYPE_TABLE:
               controlTypeName = "Table";
               break;
            case MgControlType.CTRL_TYPE_TEXT:
               controlTypeName = "Text";
               break;
            case MgControlType.CTRL_TYPE_TREE:
               controlTypeName = "Tree";
               break;
         }

         return controlTypeName;
      }
   }
}
