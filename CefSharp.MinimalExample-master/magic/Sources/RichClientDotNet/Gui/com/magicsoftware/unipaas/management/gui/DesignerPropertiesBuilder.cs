using System;
using System.Collections.Generic;
using System.Diagnostics;
using com.magicsoftware.support;
using com.magicsoftware.unipaas.env;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.util;
using Gui.com.magicsoftware.unipaas.management.gui;

namespace com.magicsoftware.unipaas.management.gui
{
   class DesignerPropertiesBuilder
   {
      delegate void BuildPropertyDelegate(MgControlBase control, Dictionary<string, DesignerPropertyInfo> properties);

      internal static List<int> PropertiesUsedByRuntimeDesigner = new List<int>(new int[] 
      { 
         PropInterface.PROP_TYPE_LAYER, PropInterface.PROP_TYPE_LEFT, PropInterface.PROP_TYPE_TOP, PropInterface.PROP_TYPE_WIDTH, PropInterface.PROP_TYPE_HEIGHT, 
         PropInterface.PROP_TYPE_COLOR, PropInterface.PROP_TYPE_FONT, PropInterface.PROP_TYPE_TEXT, PropInterface.PROP_TYPE_LABEL, PropInterface.PROP_TYPE_FORMAT,
         PropInterface.PROP_TYPE_VISIBLE, PropInterface.PROP_TYPE_VISIBLE_LAYERS_LIST,
      });

      /// <summary>
      /// which properties should be passed, according to control type
      /// </summary>
      static Dictionary<MgControlType, List<int>> controlPropsDictionary = new Dictionary<MgControlType, List<int>>() 
      {
         // PropInterface.PROP_TYPE_NAME is line of property value "Text" so it must be after PropInterface.PROP_TYPE_TEXT\PROP_TYPE_LABEL\PROP_TYPE_FORMAT
         { MgControlType.CTRL_TYPE_TEXT, new List<int>(new int[]       {PropInterface.PROP_TYPE_VISIBLE, PropInterface.PROP_TYPE_LAYER, PropInterface.PROP_TYPE_LEFT, PropInterface.PROP_TYPE_TOP, PropInterface.PROP_TYPE_WIDTH, PropInterface.PROP_TYPE_HEIGHT ,PropInterface.PROP_TYPE_NAME, PropInterface.PROP_TYPE_COLOR, PropInterface.PROP_TYPE_FONT})},
         { MgControlType.CTRL_TYPE_BUTTON, new List<int>(new int[]     {PropInterface.PROP_TYPE_VISIBLE, PropInterface.PROP_TYPE_LAYER, PropInterface.PROP_TYPE_LEFT, PropInterface.PROP_TYPE_TOP, PropInterface.PROP_TYPE_WIDTH, PropInterface.PROP_TYPE_HEIGHT ,PropInterface.PROP_TYPE_FORMAT, PropInterface.PROP_TYPE_NAME,  PropInterface.PROP_TYPE_COLOR, PropInterface.PROP_TYPE_FONT, PropInterface.PROP_TYPE_GRADIENT_STYLE})},
         { MgControlType.CTRL_TYPE_CHECKBOX, new List<int>(new int[]   {PropInterface.PROP_TYPE_VISIBLE, PropInterface.PROP_TYPE_LAYER, PropInterface.PROP_TYPE_LEFT, PropInterface.PROP_TYPE_TOP, PropInterface.PROP_TYPE_WIDTH, PropInterface.PROP_TYPE_HEIGHT ,PropInterface.PROP_TYPE_LABEL, PropInterface.PROP_TYPE_NAME,  PropInterface.PROP_TYPE_COLOR, PropInterface.PROP_TYPE_FONT})},
         { MgControlType.CTRL_TYPE_GROUP, new List<int>(new int[]      {PropInterface.PROP_TYPE_VISIBLE, PropInterface.PROP_TYPE_LAYER, PropInterface.PROP_TYPE_LEFT, PropInterface.PROP_TYPE_TOP, PropInterface.PROP_TYPE_WIDTH, PropInterface.PROP_TYPE_HEIGHT ,PropInterface.PROP_TYPE_TEXT, PropInterface.PROP_TYPE_NAME,  PropInterface.PROP_TYPE_COLOR, PropInterface.PROP_TYPE_FONT, PropInterface.PROP_TYPE_GRADIENT_STYLE})},
         { MgControlType.CTRL_TYPE_LABEL, new List<int>(new int[]      {PropInterface.PROP_TYPE_VISIBLE, PropInterface.PROP_TYPE_LAYER, PropInterface.PROP_TYPE_LEFT, PropInterface.PROP_TYPE_TOP, PropInterface.PROP_TYPE_WIDTH, PropInterface.PROP_TYPE_HEIGHT ,PropInterface.PROP_TYPE_TEXT, PropInterface.PROP_TYPE_NAME,  PropInterface.PROP_TYPE_COLOR, PropInterface.PROP_TYPE_FONT, PropInterface.PROP_TYPE_GRADIENT_STYLE})},
         { MgControlType.CTRL_TYPE_COMBO, new List<int>(new int[]      {PropInterface.PROP_TYPE_VISIBLE, PropInterface.PROP_TYPE_LAYER, PropInterface.PROP_TYPE_LEFT, PropInterface.PROP_TYPE_TOP, PropInterface.PROP_TYPE_WIDTH, PropInterface.PROP_TYPE_HEIGHT ,PropInterface.PROP_TYPE_NAME, PropInterface.PROP_TYPE_COLOR, PropInterface.PROP_TYPE_FONT})},
         { MgControlType.CTRL_TYPE_LIST, new List<int>(new int[]       {PropInterface.PROP_TYPE_VISIBLE, PropInterface.PROP_TYPE_LAYER, PropInterface.PROP_TYPE_LEFT, PropInterface.PROP_TYPE_TOP, PropInterface.PROP_TYPE_WIDTH, PropInterface.PROP_TYPE_HEIGHT ,PropInterface.PROP_TYPE_NAME, PropInterface.PROP_TYPE_COLOR, PropInterface.PROP_TYPE_FONT})},
         { MgControlType.CTRL_TYPE_RADIO, new List<int>(new int[]      {PropInterface.PROP_TYPE_VISIBLE, PropInterface.PROP_TYPE_LAYER, PropInterface.PROP_TYPE_LEFT, PropInterface.PROP_TYPE_TOP, PropInterface.PROP_TYPE_WIDTH, PropInterface.PROP_TYPE_HEIGHT ,PropInterface.PROP_TYPE_NAME, PropInterface.PROP_TYPE_COLOR, PropInterface.PROP_TYPE_FONT})},
         { MgControlType.CTRL_TYPE_RICH_TEXT, new List<int>(new int[]  {PropInterface.PROP_TYPE_VISIBLE, PropInterface.PROP_TYPE_LAYER, PropInterface.PROP_TYPE_LEFT, PropInterface.PROP_TYPE_TOP, PropInterface.PROP_TYPE_WIDTH, PropInterface.PROP_TYPE_HEIGHT ,PropInterface.PROP_TYPE_NAME, PropInterface.PROP_TYPE_COLOR, PropInterface.PROP_TYPE_FONT})},
         { MgControlType.CTRL_TYPE_RICH_EDIT, new List<int>(new int[]  {PropInterface.PROP_TYPE_VISIBLE, PropInterface.PROP_TYPE_LAYER, PropInterface.PROP_TYPE_LEFT, PropInterface.PROP_TYPE_TOP, PropInterface.PROP_TYPE_WIDTH, PropInterface.PROP_TYPE_HEIGHT ,PropInterface.PROP_TYPE_NAME, PropInterface.PROP_TYPE_COLOR, PropInterface.PROP_TYPE_FONT})},
         { MgControlType.CTRL_TYPE_TAB, new List<int>(new int[]        {PropInterface.PROP_TYPE_VISIBLE, PropInterface.PROP_TYPE_LAYER, PropInterface.PROP_TYPE_LEFT, PropInterface.PROP_TYPE_TOP, PropInterface.PROP_TYPE_WIDTH, PropInterface.PROP_TYPE_HEIGHT ,PropInterface.PROP_TYPE_NAME, PropInterface.PROP_TYPE_COLOR, PropInterface.PROP_TYPE_FONT, PropInterface.PROP_TYPE_GRADIENT_STYLE, PropInterface.PROP_TYPE_VISIBLE_LAYERS_LIST})},
         { MgControlType.CTRL_TYPE_TABLE, new List<int>(new int[]      {PropInterface.PROP_TYPE_LAYER, PropInterface.PROP_TYPE_LEFT, PropInterface.PROP_TYPE_TOP, PropInterface.PROP_TYPE_WIDTH, PropInterface.PROP_TYPE_HEIGHT ,PropInterface.PROP_TYPE_NAME })},
         { MgControlType.CTRL_TYPE_DOTNET, new List<int>(new int[]     {PropInterface.PROP_TYPE_VISIBLE, PropInterface.PROP_TYPE_LAYER, PropInterface.PROP_TYPE_LEFT, PropInterface.PROP_TYPE_TOP, PropInterface.PROP_TYPE_WIDTH, PropInterface.PROP_TYPE_HEIGHT ,PropInterface.PROP_TYPE_NAME })},
         { MgControlType.CTRL_TYPE_BROWSER, new List<int>(new int[]    {PropInterface.PROP_TYPE_VISIBLE, PropInterface.PROP_TYPE_LAYER, PropInterface.PROP_TYPE_LEFT, PropInterface.PROP_TYPE_TOP, PropInterface.PROP_TYPE_WIDTH, PropInterface.PROP_TYPE_HEIGHT ,PropInterface.PROP_TYPE_NAME })},
         { MgControlType.CTRL_TYPE_IMAGE, new List<int>(new int[]      {PropInterface.PROP_TYPE_VISIBLE, PropInterface.PROP_TYPE_LAYER, PropInterface.PROP_TYPE_LEFT, PropInterface.PROP_TYPE_TOP, PropInterface.PROP_TYPE_WIDTH, PropInterface.PROP_TYPE_HEIGHT ,PropInterface.PROP_TYPE_NAME })},
         { MgControlType.CTRL_TYPE_TREE, new List<int>(new int[]       {PropInterface.PROP_TYPE_LAYER, PropInterface.PROP_TYPE_LEFT, PropInterface.PROP_TYPE_TOP, PropInterface.PROP_TYPE_WIDTH, PropInterface.PROP_TYPE_HEIGHT ,PropInterface.PROP_TYPE_NAME })},
         { MgControlType.CTRL_TYPE_LINE, new List<int>(new int[]       {PropInterface.PROP_TYPE_LAYER, PropInterface.PROP_TYPE_LEFT, PropInterface.PROP_TYPE_TOP, PropInterface.PROP_TYPE_WIDTH, PropInterface.PROP_TYPE_HEIGHT ,PropInterface.PROP_TYPE_NAME })},
         { MgControlType.CTRL_TYPE_CONTAINER, new List<int>(new int[]  {PropInterface.PROP_TYPE_LAYER, PropInterface.PROP_TYPE_FORM_NAME })},
         { MgControlType.CTRL_TYPE_SUBFORM, new List<int>(new int[]    {PropInterface.PROP_TYPE_VISIBLE, PropInterface.PROP_TYPE_LAYER, PropInterface.PROP_TYPE_LEFT, PropInterface.PROP_TYPE_TOP, PropInterface.PROP_TYPE_WIDTH, PropInterface.PROP_TYPE_HEIGHT ,PropInterface.PROP_TYPE_NAME, PropInterface.PROP_TYPE_FORM_NAME })}
      };

      static Dictionary<MgControlType, String> DefaultConrtolNameDictionary = new Dictionary<MgControlType, String>() 
      {
         // PropInterface.PROP_TYPE_NAME must be after PropInterface.PROP_TYPE_TEXT
         { MgControlType.CTRL_TYPE_TEXT, "Text"},
         { MgControlType.CTRL_TYPE_BUTTON, "Button"},
         { MgControlType.CTRL_TYPE_CHECKBOX,"Check Box"},
         { MgControlType.CTRL_TYPE_GROUP, "Group"},
         { MgControlType.CTRL_TYPE_LABEL, "Label"},
         { MgControlType.CTRL_TYPE_COMBO, "Combo Box"},
         { MgControlType.CTRL_TYPE_LIST, "List Box"},
         { MgControlType.CTRL_TYPE_RADIO, "Radio Button"},
         { MgControlType.CTRL_TYPE_RICH_TEXT,"Rich Text"},
         { MgControlType.CTRL_TYPE_RICH_EDIT, "Edit"},
         { MgControlType.CTRL_TYPE_TAB, "Tab"},
         { MgControlType.CTRL_TYPE_TABLE, "Table"},
         { MgControlType.CTRL_TYPE_DOTNET, ".Net"},
         { MgControlType.CTRL_TYPE_BROWSER, "Browser"},
         { MgControlType.CTRL_TYPE_IMAGE, "Image"},
         { MgControlType.CTRL_TYPE_TREE, "Tree"},
         { MgControlType.CTRL_TYPE_LINE, "Line"},
         { MgControlType.CTRL_TYPE_SUBFORM, "Subform"}
      };
      /// <summary>
      /// build the runtime designer property info, according to magic property type
      /// </summary>
      static Dictionary<int, BuildPropertyDelegate> propBuilderDictionary = new Dictionary<int, BuildPropertyDelegate>()
      {
         { PropInterface.PROP_TYPE_LAYER, BuildLayerProperty},
         { PropInterface.PROP_TYPE_LEFT, BuildLeftProperty},
         { PropInterface.PROP_TYPE_TOP, BuildTopProperty},
         { PropInterface.PROP_TYPE_WIDTH, BuildWidthProperty},
         { PropInterface.PROP_TYPE_HEIGHT, BuildHeightProperty},
         { PropInterface.PROP_TYPE_COLOR, BuildColorsProperties},
         { PropInterface.PROP_TYPE_FONT, BuildFontProperty},
         { PropInterface.PROP_TYPE_TEXT, BuildTextProperty},
         { PropInterface.PROP_TYPE_LABEL, BuildTextPropertyFromLabel},
         { PropInterface.PROP_TYPE_FORMAT, BuildTextPropertyFromFormat},
         { PropInterface.PROP_TYPE_NAME, BuildNameProperty},
         { PropInterface.PROP_TYPE_FORM_NAME, BuildFormNameProperty},
         { PropInterface.PROP_TYPE_VISIBLE, BuildVisibleProperty},
         { PropInterface.PROP_TYPE_GRADIENT_STYLE, BuildGradientStyleProperty},
         { PropInterface.PROP_TYPE_VISIBLE_LAYERS_LIST, BuildVisibleLayersListProperty},
      };

      /// <summary>
      /// build command 
      /// </summary>
      /// <param name="control"></param>
      /// <returns></returns>
      public static Dictionary<string, DesignerPropertyInfo> build(MgControlBase control)
      {
         Dictionary<string, DesignerPropertyInfo> properties = null;
         if (controlPropsDictionary.ContainsKey(control.Type))
         {
            properties = new Dictionary<string, DesignerPropertyInfo>();
            foreach (var item in controlPropsDictionary[control.Type])
            {
               propBuilderDictionary[item](control, properties);
            }
         }
         return properties;
      }

      static bool AddNavigationProperties(MgControlBase control)
      {
         bool isNavigationEnable = true;
         if (control.IsFrame() || control.isFrameFormControl())
         {
            isNavigationEnable = false;
         }

         return isNavigationEnable;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="control"></param>
      /// <param name="properties"></param>
      /// <param name="value"></param>
      private static void CreateCoordinateDesignerPropertyInfo(String keyName, MgControlBase control, Dictionary<string, DesignerPropertyInfo> properties, int value)
      {
         DesignerPropertyInfo DesignerPropertyInfo = new DesignerPropertyInfo();
         DesignerPropertyInfo.Value = value;

         if (control.Type != MgControlType.CTRL_TYPE_LINE)
            DesignerPropertyInfo.VisibleInPropertyGrid = (control.Type == MgControlType.CTRL_TYPE_LINE ? false : true);

         if (control.isComboBox() && !control.isOwnerDrawComboBox() && (keyName == "Height"))
            DesignerPropertyInfo.VisibleInPropertyGrid = false;
           

         properties.Add(keyName, DesignerPropertyInfo);
      }


      /// <summary>
      /// Left
      /// </summary>
      /// <param name="control"></param>
      /// <param name="properties"></param>
      static void BuildLeftProperty(MgControlBase control, Dictionary<string, DesignerPropertyInfo> properties)
      {
         if (AddNavigationProperties(control))
         {
            int value = control.getProp(PropInterface.PROP_TYPE_LEFT).CalcLeftValue(control);
            String keyName = (control.Type == MgControlType.CTRL_TYPE_LINE ? Constants.WinPropX1 : Constants.WinPropLeft);

            CreateCoordinateDesignerPropertyInfo(keyName, control, properties, value);
         }
      }

      /// <summary>
      /// Top
      /// </summary>
      /// <param name="control"></param>
      /// <param name="properties"></param>
      static void BuildTopProperty(MgControlBase control, Dictionary<string, DesignerPropertyInfo> properties)
      {
         if (AddNavigationProperties(control))
         {
            int value = control.getProp(PropInterface.PROP_TYPE_TOP).CalcTopValue(control);
            String keyName = (control.Type == MgControlType.CTRL_TYPE_LINE ? Constants.WinPropY1 : Constants.WinPropTop);
            CreateCoordinateDesignerPropertyInfo(keyName, control, properties, value);
         }
      }

      /// <summary>
      /// Width
      /// </summary>
      /// <param name="control"></param>
      /// <param name="properties"></param>
      static void BuildWidthProperty(MgControlBase control, Dictionary<string, DesignerPropertyInfo> properties)
      {
         if (AddNavigationProperties(control))
         {
            int value = -1;
            if (control.Type == MgControlType.CTRL_TYPE_SUBFORM && 
               control.getProp(PropInterface.PROP_TYPE_AUTO_FIT).getValueInt() == (int)AutoFit.AsCalledForm &&
               control.GetSubformMgForm() != null)
            {
               MgFormBase subformForm = control.GetSubformMgForm();
               value = subformForm.getProp(PropInterface.PROP_TYPE_WIDTH).CalcWidthValue(control);
            }
            else
            {
               value = control.getProp(PropInterface.PROP_TYPE_WIDTH).CalcWidthValue(control);
            }
            String keyName = (control.Type == MgControlType.CTRL_TYPE_LINE ? Constants.WinPropX2 : Constants.WinPropWidth);
            CreateCoordinateDesignerPropertyInfo(keyName, control, properties, value);
        }
      }

      /// <summary>
      /// Height
      /// </summary>
      /// <param name="control"></param>
      /// <param name="properties"></param>
      static void BuildHeightProperty(MgControlBase control, Dictionary<string, DesignerPropertyInfo> properties)
      {
         if (AddNavigationProperties(control))
         {
            int value;
            if (control.Type == MgControlType.CTRL_TYPE_SUBFORM && 
               control.getProp(PropInterface.PROP_TYPE_AUTO_FIT).getValueInt() == (int)AutoFit.AsCalledForm &&
               control.GetSubformMgForm() != null)
            {
               MgFormBase subformForm = control.GetSubformMgForm();
               value = subformForm.getProp(PropInterface.PROP_TYPE_HEIGHT).CalcHeightValue(control);
            }
            else
            {
               value = control.getProp(PropInterface.PROP_TYPE_HEIGHT).CalcHeightValue(control);
            }

            String keyName = (control.Type == MgControlType.CTRL_TYPE_LINE ? Constants.WinPropY2 : Constants.WinPropHeight);
            CreateCoordinateDesignerPropertyInfo(keyName, control, properties, value);
         }
      }

      /// <summary>
      /// Colors
      /// </summary>
      /// <param name="control"></param>
      /// <param name="properties"></param>
      static void BuildColorsProperties(MgControlBase control, Dictionary<string, DesignerPropertyInfo> properties)
      {
         bool isDefaultvalue = false;
         int value = GetRuntimeValueAsInt(control, PropInterface.PROP_TYPE_COLOR, ref isDefaultvalue);
         if (value > 0)
         {
            MgColor color = isDefaultvalue ? null : Manager.GetColorsTable().getBGColor(value);
            properties.Add(Constants.WinPropBackColor, new DesignerPropertyInfo() { VisibleInPropertyGrid = true, Value = color, IsDefaultValue = isDefaultvalue });

            if (control.Type == MgControlType.CTRL_TYPE_TEXT || control.Type == MgControlType.CTRL_TYPE_TABLE)
               properties.Add(Constants.WinPropIsTransparent, new DesignerPropertyInfo() { VisibleInPropertyGrid = false, Value = color, IsDefaultValue = isDefaultvalue });

            color = isDefaultvalue ? null : Manager.GetColorsTable().getFGColor(value);
            properties.Add(Constants.WinPropForeColor, new DesignerPropertyInfo() { VisibleInPropertyGrid = true, Value = color, IsDefaultValue = isDefaultvalue });
         }
         else
         {
            // fixed defect #:132036, 132037
            // for button control set default color that we set on MgButton.cs , BtnFace\WindowText convert from ControlUtils.cs method MgColor2Color()
            if (control.Type == MgControlType.CTRL_TYPE_BUTTON)
            {
               MgColor colorBG = new MgColor() { IsSystemColor = true, SystemColor = MagicSystemColor.BtnFace };
               properties.Add(Constants.WinPropBackColor, new DesignerPropertyInfo() { VisibleInPropertyGrid = true, Value = colorBG, IsDefaultValue = isDefaultvalue });

               MgColor colorFG = new MgColor() { IsSystemColor = true, SystemColor = MagicSystemColor.WindowText };
               properties.Add(Constants.WinPropForeColor, new DesignerPropertyInfo() { VisibleInPropertyGrid = true, Value = colorFG, IsDefaultValue = isDefaultvalue });
            }

         }
      }

      /// <summary>
      /// Font
      /// </summary>
      /// <param name="control"></param>
      /// <param name="properties"></param>
      static void BuildFontProperty(MgControlBase control, Dictionary<string, DesignerPropertyInfo> properties)
      {
         bool isDefaultvalue = false;
         int value = GetRuntimeValueAsInt(control, PropInterface.PROP_TYPE_FONT, ref isDefaultvalue);
         MgFont mgFont = isDefaultvalue ? null : Manager.GetFontsTable().getFont(value);

         properties.Add(Constants.WinPropFont, new DesignerPropertyInfo() { VisibleInPropertyGrid = true, Value = mgFont, IsDefaultValue = isDefaultvalue });
      }

      /// <summary>
      /// Text
      /// </summary>
      /// <param name="control"></param>
      /// <param name="properties"></param>
      static void BuildTextProperty(MgControlBase control, Dictionary<string, DesignerPropertyInfo> properties)
      {
         BuildTextProperty(control, properties, PropInterface.PROP_TYPE_TEXT);
      }

      /// <summary>
      /// CalculatControlName
      /// </summary>
      /// <param name="properties"></param>
      /// <param name="control"></param>
      /// <returns></returns>
      static String CalculatControlName(Dictionary<string, DesignerPropertyInfo> properties, MgControlBase control)
      {
         string name = String.Empty;

         //1. If the control has a Text property (see above) then the original studio text will be seen.
         if (properties.ContainsKey(Constants.WinPropText))
         {
            DesignerPropertyInfo TextPropertyInfo = properties[Constants.WinPropText];
            if (TextPropertyInfo != null)
               name = TextPropertyInfo.Value as String;

            if (name != null)
               name = name.Trim();
         }

         //2. If not or if the text is blank, then the control name will be seen.
         if (String.IsNullOrEmpty(name))
         {
            name = control.Name;

            //3. If the name is blank then the variable display(Display name property) name will be seen. (TODO take it from FieldDef)
            if (String.IsNullOrEmpty(name))
            {
               Field fld = control.getField();
               if (fld != null && !String.IsNullOrEmpty(fld.VarDisplayName))
                  name = fld.VarDisplayName;

               if (name != null)
                  name = name.Trim();
            }

            ////4. If the display name is blank then the variable name will be seen.
            if (String.IsNullOrEmpty(name))
            {
               Field fld = control.getField();
               if (fld != null)
                  name = fld.getVarName();

               if (name != null)
                  name = name.Trim();

               // 5. If there is no variable name (such as for a Group control) then the word ‘Group’ will be seen.
               if (String.IsNullOrEmpty(name))
               {
                  name = control.Type.ToString();
                  //remove CTRL_TYPE_ from magic type 
                  name = name.Remove(0, "CTRL_TYPE_".Length);
                  //move all string to lower
                  name = name.ToLower();
                  //move first char to upper
                  String FirstChar = char.ToUpper(name[0]).ToString();
                  name = name.Remove(0, 1);
                  name = name.Insert(0, FirstChar);
                  name = name.Trim();
               }
            }
         }
         return name;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="control"></param>
      /// <param name="properties"></param>
      static void BuildNameProperty(MgControlBase control, Dictionary<string, DesignerPropertyInfo> properties)
      {
         String value = CalculatControlName(properties, control);
         properties.Add(Constants.WinPropName, new DesignerPropertyInfo() { VisibleInPropertyGrid = true, Value = value, IsDefaultValue = false, IsNativeProperty = false });
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="control"></param>
      /// <param name="properties"></param>
      static void BuildFormNameProperty(MgControlBase control, Dictionary<string, DesignerPropertyInfo> properties)
      {
         MgFormBase form = control.GetSubformMgForm();
         if (form == null)
            form = control.getForm();
         String controlsPersistencyPath = EnvControlsPersistencyPath.GetInstance().GetFullControlsPersistencyFileName(form);
         properties.Add(Constants.ConfigurationFilePropertyName, new DesignerPropertyInfo() { VisibleInPropertyGrid = true, Value = controlsPersistencyPath, IsNativeProperty = false });
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="control"></param>
      /// <param name="properties"></param>
      static void BuildVisibleProperty(MgControlBase control, Dictionary<string, DesignerPropertyInfo> properties)
      {

         bool isDefaultvalue = false;
         bool value = true;
         bool runtimeValue = true;
         // value is always "true", unless defined otherwise in previous runtime designer executions
         if (control.PropertyExists(PropInterface.PROP_TYPE_VISIBLE))
         {
            Property prop = control.getProp(PropInterface.PROP_TYPE_VISIBLE);
            value = !prop.IsDesignerValue();
            runtimeValue = GetRuntimeValueAsBool(control, PropInterface.PROP_TYPE_VISIBLE, ref isDefaultvalue);
         }        
         properties.Add(Constants.WinPropVisible, new DesignerPropertyInfo() { VisibleInPropertyGrid = false, Value = value, IsDefaultValue = false, IsNativeProperty = false , RuntimeValue = runtimeValue });
      }

      /// <summary>
      /// Label
      /// </summary>
      /// <param name="control"></param>
      /// <param name="properties"></param>
      static void BuildTextPropertyFromLabel(MgControlBase control, Dictionary<string, DesignerPropertyInfo> properties)
      {
         BuildTextProperty(control, properties, PropInterface.PROP_TYPE_LABEL);
      }

      /// <summary>
      /// Format
      /// </summary>
      /// <param name="control"></param>
      /// <param name="properties"></param>
      static void BuildTextPropertyFromFormat(MgControlBase control, Dictionary<string, DesignerPropertyInfo> properties)
      {
         if (control.getField() == null && !control.expressionSetAsData() || control.IsImageButton())
            BuildTextProperty(control, properties, PropInterface.PROP_TYPE_FORMAT);
         else
            properties.Add(Constants.WinPropText, new DesignerPropertyInfo() { VisibleInPropertyGrid = true, Value = control.Value, IsDefaultValue = false });
      }

      /// <summary>
      /// build the text property from the requested magic property
      /// </summary>
      /// <param name="control"></param>
      /// <param name="properties"></param>
      /// <param name="propId"></param>
      static void BuildTextProperty(MgControlBase control, Dictionary<string, DesignerPropertyInfo> properties, int propId)
      {
         bool isDefaultValue = false;
         string value = GetRuntimeValueAsString(control, propId, ref isDefaultValue);

         if (value != null)
            value = Events.Translate(value);
         properties.Add(Constants.WinPropText, new DesignerPropertyInfo() { VisibleInPropertyGrid = true, Value = value, IsDefaultValue = isDefaultValue });
      }

      /// <summary>
      /// Layer
      /// </summary>
      /// <param name="control"></param>
      /// <param name="properties"></param>
      static void BuildLayerProperty(MgControlBase control, Dictionary<string, DesignerPropertyInfo> properties)
      {
         bool isDefaultvalue = false;
         int value = GetRuntimeValueAsInt(control, PropInterface.PROP_TYPE_LAYER, ref isDefaultvalue);
         properties.Add(Constants.WinPropLayer, new DesignerPropertyInfo()
         {
            VisibleInPropertyGrid = false,
            Value = value,
            IsDefaultValue = isDefaultvalue,
            IsNativeProperty = false,
            RuntimeValue = control.getLayer()
         });
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="control"></param>
      /// <param name="properties"></param>
      static void BuildGradientStyleProperty(MgControlBase control, Dictionary<string, DesignerPropertyInfo> properties)
      {
         bool isDefaultvalue = false;
         GradientStyle value = (GradientStyle)GetRuntimeValueAsInt(control, PropInterface.PROP_TYPE_GRADIENT_STYLE, ref isDefaultvalue);
         properties.Add(Constants.WinPropGradientStyle, new DesignerPropertyInfo()
         {
            VisibleInPropertyGrid = false,
            Value = value,
            IsDefaultValue = isDefaultvalue,
            IsNativeProperty = true,
         });
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="control"></param>
      /// <param name="properties"></param>
      static void BuildVisibleLayersListProperty(MgControlBase control, Dictionary<string, DesignerPropertyInfo> properties)
      {
         MgArrayList value = control.GetTabControlChoiceLayerList();

         properties.Add(Constants.WinPropVisibleLayerList, new DesignerPropertyInfo()
         {
            VisibleInPropertyGrid = false,
            Value = value,
            IsDefaultValue = false,
            IsNativeProperty = false,
         });
      }


      /// <summary>
      /// </summary>
      /// <param name="control"></param>
      /// <param name="propId"></param>
      /// <param name="isDefaultValue"></param>
      /// <returns></returns>
      private static int GetRuntimeValueAsInt(MgControlBase control, int propId, ref bool isDefaultValue)
      {
         string s = GetRuntimeValueAsString(control, propId, ref isDefaultValue);

         return isDefaultValue ? 0 : Int32.Parse(s);
      }

      /// <summary>
      /// get the value for the specified property. If the property does not exist, get default value
      /// without creating the property object
      /// </summary>
      /// <param name="control"></param>
      /// <param name="propId"></param>
      /// <param name="isDefaultValue"></param>
      /// <returns></returns>
      private static string GetRuntimeValueAsString(MgControlBase control, int propId, ref bool isDefaultValue)
      {
         Property prop = null;
         if (!control.PropertyExists(propId))
         {
            isDefaultValue = true;
         }
         else
            prop = control.getProp(propId);

         return prop != null ? prop.getValue() : string.Empty;
      }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="control"></param>
        /// <param name="propId"></param>
        /// <param name="isDefaultValue"></param>
        /// <returns></returns>
        private static bool GetRuntimeValueAsBool(MgControlBase control, int propId, ref bool isDefaultValue)
        {
            int i = Int32.Parse(GetRuntimeValueAsString(control, propId, ref isDefaultValue));
            return i != 0;
        }

        /// <summary>
        /// get the magic property Id which is affected by the window property.
        /// This function is not activated with the other functions when building the properties for the runtime designer. It is used
        /// when unserializing values and seting them on magic properties.
        /// </summary>
        /// <param name="controlType"></param>
        /// <param name="winPropName"></param>
        /// <returns></returns>
        static public int GetMagicPropId(MgControlType controlType, string winPropName)
      {
         switch (winPropName)
         {
            case Constants.WinPropLeft:
            case Constants.WinPropX1:
               return PropInterface.PROP_TYPE_LEFT;

            case Constants.WinPropTop:
            case Constants.WinPropY1:
               return PropInterface.PROP_TYPE_TOP;

            case Constants.WinPropWidth:
            case Constants.WinPropX2:
               return PropInterface.PROP_TYPE_WIDTH;

            case Constants.WinPropHeight:
            case Constants.WinPropY2:
               return PropInterface.PROP_TYPE_HEIGHT;

            case Constants.WinPropBackColor:
            case Constants.WinPropForeColor:
               return PropInterface.PROP_TYPE_COLOR;

            case Constants.WinPropFont:
               return PropInterface.PROP_TYPE_FONT;

            case Constants.WinPropText:
               if (controlPropsDictionary[controlType].Contains(PropInterface.PROP_TYPE_FORMAT))
                  return PropInterface.PROP_TYPE_FORMAT;
               if (controlPropsDictionary[controlType].Contains(PropInterface.PROP_TYPE_LABEL))
                  return PropInterface.PROP_TYPE_LABEL;
               return PropInterface.PROP_TYPE_TEXT;

            case Constants.WinPropLayer:
               return PropInterface.PROP_TYPE_LAYER;

            case Constants.WinPropVisible:
               return PropInterface.PROP_TYPE_VISIBLE;

            case Constants.WinPropGradientStyle:
               return PropInterface.PROP_TYPE_GRADIENT_STYLE;
         }

         if (!winPropName.EndsWith(Constants.TabOrderPropertyTermination))
            Debug.Assert(false, "unhandled property " + winPropName);

         return 0;
      }
   }
}
