using System;
using System.Collections.Generic;
using com.magicsoftware.util;
using com.magicsoftware.unipaas.util;
using util.com.magicsoftware.util;
using com.magicsoftware.util.Xml;

namespace com.magicsoftware.unipaas.management.gui
{
   /// <summary> /// Base class for all the help types supported in magic./// </summary>
   public abstract class MagicHelp
   {
      public abstract HelpType GetHelpType();
   }

   /// <summary> /// This class will contain the details required to draw the internal help window./// </summary>
   public class InternalHelp : MagicHelp
   {
      public override HelpType GetHelpType()
      {
         return HelpType.Internal;
      }

      public string val {get;set;}
      public string Name { get; set; }

      public int FrameX { get; set; }
      public int FrameY { get; set; }
      public int FrameDx { get; set; }
      public int FrameDy { get; set; }
      public int SizedX { get; set; }
      public int SizedY { get; set; }
      public int FactorX { get; set; }
      public int FactorY { get; set; }
      public int Borderstyle { get; set; }
      public int TitleBar { get; set; }
      public int SystemMenu { get; set; }
      public int FontTableIndex { get; set; }
   }

   /// <summary> /// This class will contain the details required to show prompt help./// </summary>
   public class PromptpHelp : MagicHelp
   {
      public override HelpType GetHelpType()
      {
         return HelpType.Prompt;
      }

      public string PromptHelpText { get; set; }
   }

   /// <summary> /// This class will contain the details required to show tool tip help./// </summary>
   public class ToolTipHelp : MagicHelp
   {
      internal string tooltipHelpText;

      public override HelpType GetHelpType()
      {
         return HelpType.Tooltip;
      }
   }

   /// <summary> /// This class will contain the details required to show URL help./// </summary>
   public class URLHelp:MagicHelp
   {
      public string urlHelpText;

      public override HelpType GetHelpType()
      {
         return HelpType.URL;
      }
   }

   /// <summary> /// This class will contain the details required to show windows help./// </summary>
   public class WindowsHelp : MagicHelp
   {
      public override HelpType GetHelpType()
      {
         return HelpType.Windows;
      }
      public string FilePath { get; set; }
      public HelpCommand HelpCommand { get; set; }
      public string HelpKey { get; set; }
   }

   /// <summary>
   ///   data for <helptable> <helpitem value = "string"> ... </helptable>
   /// </summary>
   public class Helps
   {
      private readonly List<MagicHelp> _helps; // of String

      /// <summary>
      ///   CTOR
      /// </summary>
      public Helps()
      {
         _helps = new List<MagicHelp>();
      }

      /// <summary>
      ///   To parse the input string and fill data field (helps) in the class.
      /// </summary>
      public void fillData()
      {
         XmlParser parser = Manager.GetCurrentRuntimeContext().Parser;
         while (initInnerObjects(parser.getNextTag()))
         {
         }
      }

      /// <summary>
      ///   To initial inner object
      /// </summary>
      /// <param name = "foundTagName">of found/current tag</param>
      private bool initInnerObjects(String foundTagName)
      {
         XmlParser parser = Manager.GetCurrentRuntimeContext().Parser;
         if (foundTagName == null)
            return false;

         if (foundTagName.Equals(XMLConstants.MG_TAG_HELPTABLE))
         {
            parser.setCurrIndex(parser.getXMLdata().IndexOf(XMLConstants.TAG_CLOSE, parser.getCurrIndex()) +
                                   1); //end of outer tad and its ">"
         }
         else if (foundTagName.Equals(XMLConstants.MG_TAG_HELPITEM))
         {
            int endContext = parser.getXMLdata().IndexOf(XMLConstants.TAG_TERM, parser.getCurrIndex());
            if (endContext != -1 && endContext < parser.getXMLdata().Length)
            {
               //last position of its tag
               String tag = parser.getXMLsubstring(endContext);
               parser.add2CurrIndex(tag.IndexOf(XMLConstants.MG_TAG_HELPITEM) +
                                       XMLConstants.MG_TAG_HELPITEM.Length);

               List<string> tokensVector = XmlParser.getTokens(parser.getXMLsubstring(endContext), XMLConstants.XML_ATTR_DELIM);
               fillHelpItem(tokensVector);
               parser.setCurrIndex(endContext + XMLConstants.TAG_TERM.Length); //to delete "/>" too
               return true;
            }
         }
         else if (foundTagName.Equals('/' + XMLConstants.MG_TAG_HELPTABLE))
         {
            parser.setCurrIndex2EndOfTag();
            return false;
         }
         else
         {
            Events.WriteExceptionToLog("in Command.FillData() out of string bounds");
            return false;
         }
         return true;
      }

      /// <summary>
      ///   Fill member element helps by <helpitem> tag
      /// </summary>
      /// <param name = "tokensVector">attribute/value/...attribute/value/ vector</param>
      private void fillHelpItem(List<String> tokensVector)
      {
         //Since the first attribute is type of help. Extract this attribute to check type.
         int j = 0;
         string attribute = (tokensVector[j]);
         string valueStr = (tokensVector[j + 1]);
         //Get and check the type of the help(Window\Internal\Prompt).
         string hlpType = XmlParser.unescape(valueStr);

         switch (hlpType)
         {
            case XMLConstants.MG_ATTR_HLP_TYP_TOOLTIP:
               ToolTipHelp tooltipHelp = new ToolTipHelp();
               for (j = 2; j < tokensVector.Count; j += 2)
               {
                  attribute = (tokensVector[j]);
                  valueStr = (tokensVector[j + 1]);
                  
                  if (attribute.Equals(XMLConstants.MG_ATTR_VALUE))
                  {
                     //#932086: Special characters in helps text are escaped while serializing,
                     // so unescape the escaped characters.
                     tooltipHelp.tooltipHelpText = XmlParser.unescape(valueStr);
                  }
                  else
                     Events.WriteExceptionToLog(
                        string.Format("There is no such tag in <helptable><helpitem ..>.Insert case to HelpTable.FillHelpItem for {0}", attribute));
               }
               _helps.Add(tooltipHelp);
               break;

            case XMLConstants.MG_ATTR_HLP_TYP_PROMPT:
               PromptpHelp promptHelp = new PromptpHelp();
               for (j = 2; j < tokensVector.Count; j += 2)
               {
                  attribute = (tokensVector[j]);
                  valueStr = (tokensVector[j + 1]);

                  if (attribute.Equals(XMLConstants.MG_ATTR_VALUE))
                  {
                     //#932086: Special characters in helps text are escaped while serializing,
                     // so unescape the escaped characters.
                     promptHelp.PromptHelpText = XmlParser.unescape(valueStr);
                  }
                  else
                     Events.WriteExceptionToLog(
                        string.Format("There is no such tag in <helptable><helpitem ..>.Insert case to HelpTable.FillHelpItem for {0}", attribute));
               }
               _helps.Add(promptHelp);
               break;

            case XMLConstants.MG_ATTR_HLP_TYP_URL:
               URLHelp urlHelp = new URLHelp();
               for (j = 2; j < tokensVector.Count; j += 2)
               {
                  attribute = (tokensVector[j]);
                  valueStr = (tokensVector[j + 1]);

                  if (attribute.Equals(XMLConstants.MG_ATTR_VALUE))
                  {
                     urlHelp.urlHelpText = valueStr;
                  }
                  else
                     Events.WriteExceptionToLog(
                        string.Format("There is no such tag in <helptable><helpitem ..>.Insert case to HelpTable.FillHelpItem for {0}", attribute));
               }
               _helps.Add(urlHelp);
               break;

            case XMLConstants.MG_ATTR_HLP_TYP_INTERNAL:
               InternalHelp internalHelpWindowDetails = new InternalHelp();
               //Init Internal Help Window Details.
               for (j = 2; j < tokensVector.Count; j += 2)
               {
                  attribute = (tokensVector[j]);
                  valueStr = (tokensVector[j + 1]);

                  if (attribute.Equals(XMLConstants.MG_ATTR_VALUE))
                  {
                     internalHelpWindowDetails.val = XmlParser.unescape(valueStr);
                  }
                  else if (attribute.Equals(XMLConstants.MG_ATTR_INTERNAL_HELP_NAME))
                  {
                     internalHelpWindowDetails.Name = XmlParser.unescape(valueStr);
                  }
                  else if (attribute.Equals(XMLConstants.MG_ATTR_INTERNAL_HELP_FRAMEX))
                  {
                     internalHelpWindowDetails.FrameX = Convert.ToInt32(XmlParser.unescape(valueStr));
                  }
                  else if (attribute.Equals(XMLConstants.MG_ATTR_INTERNAL_HELP_FRAMEY))
                  {
                     internalHelpWindowDetails.FrameY = Convert.ToInt32(XmlParser.unescape(valueStr));
                  }
                  else if (attribute.Equals(XMLConstants.MG_ATTR_INTERNAL_HELP_FRAMEDX))
                  {
                     internalHelpWindowDetails.FrameDx = Convert.ToInt32(XmlParser.unescape(valueStr));
                  }
                  else if (attribute.Equals(XMLConstants.MG_ATTR_INTERNAL_HELP_FRAMEDY))
                  {
                     internalHelpWindowDetails.FrameDy = Convert.ToInt32(XmlParser.unescape(valueStr));
                  }
                  else if (attribute.Equals(XMLConstants.MG_ATTR_INTERNAL_HELP_SIZEDX))
                  {
                     internalHelpWindowDetails.SizedX = Convert.ToInt32(XmlParser.unescape(valueStr));
                  }
                  else if (attribute.Equals(XMLConstants.MG_ATTR_INTERNAL_HELP_SIZEDY))
                  {
                     internalHelpWindowDetails.SizedY = Convert.ToInt32(XmlParser.unescape(valueStr));
                  }
                  else if (attribute.Equals(XMLConstants.MG_ATTR_INTERNAL_HELP_FACTORX))
                  {
                     internalHelpWindowDetails.FactorX = Convert.ToInt32(XmlParser.unescape(valueStr));
                  }
                  else if (attribute.Equals(XMLConstants.MG_ATTR_INTERNAL_HELP_FACTORY))
                  {
                     internalHelpWindowDetails.FactorY = Convert.ToInt32(XmlParser.unescape(valueStr));
                  }
                  else if (attribute.Equals(XMLConstants.MG_ATTR_INTERNAL_HELP_BORDERSTYLE))
                  {
                     internalHelpWindowDetails.Borderstyle = Convert.ToInt32(XmlParser.unescape(valueStr));
                  }
                  else if (attribute.Equals(XMLConstants.MG_ATTR_INTERNAL_TITLE_BAR))
                  {
                     internalHelpWindowDetails.TitleBar = Convert.ToInt32(XmlParser.unescape(valueStr));
                  }
                  else if (attribute.Equals(XMLConstants.MG_ATTR_INTERNAL_HELP_SYSTEM_MENU))
                  {
                     internalHelpWindowDetails.SystemMenu = Convert.ToInt32(XmlParser.unescape(valueStr));
                  }
                  else if (attribute.Equals(XMLConstants.MG_ATTR_INTERNAL_HELP_FONT_TABLE_INDEX))
                  {
                     internalHelpWindowDetails.FontTableIndex = Convert.ToInt32(XmlParser.unescape(valueStr));
                  }
                  else
                     Events.WriteExceptionToLog(
                        string.Format("There is no such tag in <helptable><helpitem ..>.Insert case to HelpTable.FillHelpItem for {0}", attribute));
               }

               //Add help object to collection.
               _helps.Add(internalHelpWindowDetails);
               break;

            case XMLConstants.MG_ATTR_HLP_TYP_WINDOWS:
               WindowsHelp wndHelpDetails = new WindowsHelp();

               //Init Windows Help Details.
               for (j = 2; j < tokensVector.Count; j += 2)
               {
                  attribute = (tokensVector[j]);
                  valueStr = (tokensVector[j + 1]);

                  if (attribute.Equals(XMLConstants.MG_ATTR_WINDOWS_HELP_FILE))
                  {
                     wndHelpDetails.FilePath = valueStr;
                  }
                  else if (attribute.Equals(XMLConstants.MG_ATTR_WINDOWS_HELP_COMMAND))
                  {
                     wndHelpDetails.HelpCommand = (HelpCommand)Convert.ToInt32(valueStr);
                  }
                  else if (attribute.Equals(XMLConstants.MG_ATTR_WINDOWS_HELP_KEY))
                  {
                     wndHelpDetails.HelpKey = XmlParser.unescape(valueStr);
                  }
                  else
                     Events.WriteExceptionToLog(
                        string.Format("There is no such tag in <helptable><helpitem ..>.Insert case to HelpTable.FillHelpItem for {0}", attribute));
               }

               //Add help object to collection.
               _helps.Add(wndHelpDetails);
               break;
         }
      }

      /// <summary>
      ///   get help item
      /// </summary>
      /// <param name = "idx">index of help item</param>
      public MagicHelp getHelp(int idx)
      {
         if (idx < 0 || idx >= _helps.Count)
            return null;
         return _helps[idx];
      }
   }
}
