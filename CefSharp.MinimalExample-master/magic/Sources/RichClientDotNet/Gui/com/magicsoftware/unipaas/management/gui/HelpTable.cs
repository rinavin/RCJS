using System;
using System.Collections.Generic;
using com.magicsoftware.util;
using com.magicsoftware.unipaas.util;

namespace com.magicsoftware.unipaas.management.gui
{
   /// <summary> /// Help types supported./// </summary>
   public enum HelpType
   {
    HLP_TYP_TOOLTIP   = 'T',
    HLP_TYP_PROMPT    = 'P',
    HLP_TYP_URL       = 'U',
    HLP_TYP_INTERNAL  = 'I',
    HLP_TYP_WINDOWS   = 'W'
   }
   
   /// <summary> ///Help commands used in windows help./// </summary>
   public enum HelpComand
   {
      HLP_COMMAND_CONTEXT = 1,
      HLP_COMMAND_CONTENTS = 2,
      HLP_COMMAND_SETCONTENTS = 3,
      HLP_COMMAND_CONTEXTPOPUP = 4,
      HLP_COMMAND_KEY = 5,
      HLP_COMMAND_COMMAND = 6,
      HLP_COMMAND_FORCEFILE = 7,
      HLP_COMMAND_HELPONHELP = 8,
      HLP_COMMAND_QUIT = 9
   }

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
         return HelpType.HLP_TYP_INTERNAL;
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
      public int WinStyle { get; set; }
      public int FontTableIndex { get; set; }
   }

   /// <summary> /// This class will contain the details required to show prompt help./// </summary>
   public class PromptpHelp : MagicHelp
   {
      public override HelpType GetHelpType()
      {
         return HelpType.HLP_TYP_PROMPT;
      }

      public string PromptHelpText { get; set; }
   }

   /// <summary> /// This class will contain the details required to show tool tip help./// </summary>
   public class ToolTipHelp : MagicHelp
   {
      internal string tooltipHelpText;

      public override HelpType GetHelpType()
      {
         return HelpType.HLP_TYP_TOOLTIP;
      }
   }

   /// <summary> /// This class will contain the details required to show URL help./// </summary>
   public class URLHelp:MagicHelp
   {
      public string urlHelpText;

      public override HelpType GetHelpType()
      {
         return HelpType.HLP_TYP_URL;
      }
   }

   /// <summary> /// This class will contain the details required to show windows help./// </summary>
   public class WindowsHelp : MagicHelp
   {
      public override HelpType GetHelpType()
      {
         return HelpType.HLP_TYP_WINDOWS;
      }
      public string FilePath { get; set; }
      public HelpComand HelpCommand { get; set; }
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
         while (initInnerObjects(Manager.Parser.getNextTag()))
         {
         }
      }

      /// <summary>
      ///   To initial inner object
      /// </summary>
      /// <param name = "foundTagName">of found/current tag</param>
      private bool initInnerObjects(String foundTagName)
      {
         if (foundTagName == null)
            return false;

         if (foundTagName.Equals(XMLConstants.MG_TAG_HELPTABLE))
         {
            Manager.Parser.setCurrIndex(Manager.Parser.getXMLdata().IndexOf(XMLConstants.TAG_CLOSE, Manager.Parser.getCurrIndex()) +
                                   1); //end of outer tad and its ">"
         }
         else if (foundTagName.Equals(XMLConstants.MG_TAG_HELPITEM))
         {
            int endContext = Manager.Parser.getXMLdata().IndexOf(XMLConstants.TAG_TERM, Manager.Parser.getCurrIndex());
            if (endContext != -1 && endContext < Manager.Parser.getXMLdata().Length)
            {
               //last position of its tag
               String tag = Manager.Parser.getXMLsubstring(endContext);
               Manager.Parser.add2CurrIndex(tag.IndexOf(XMLConstants.MG_TAG_HELPITEM) +
                                       XMLConstants.MG_TAG_HELPITEM.Length);

               List<string> tokensVector = XMLparser.getTokens(Manager.Parser.getXMLsubstring(endContext), XMLConstants.XML_ATTR_DELIM);
               fillHelpItem(tokensVector);
               Manager.Parser.setCurrIndex(endContext + XMLConstants.TAG_TERM.Length); //to delete "/>" too
               return true;
            }
         }
         else if (foundTagName.Equals('/' + XMLConstants.MG_TAG_HELPTABLE))
         {
            Manager.Parser.setCurrIndex2EndOfTag();
            return false;
         }
         else
         {
            Events.WriteErrorToLog("in Command.FillData() out of string bounds");
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
         string hlpType = XMLparser.unescape(valueStr);

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
                     tooltipHelp.tooltipHelpText = valueStr;
                  }
                  else
                     Events.WriteErrorToLog(
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
                     promptHelp.PromptHelpText = valueStr;
                  }
                  else
                     Events.WriteErrorToLog(
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
                     Events.WriteErrorToLog(
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
                     internalHelpWindowDetails.val = XMLparser.unescape(valueStr);
                  }
                  else if (attribute.Equals(XMLConstants.MG_ATTR_INTERNAL_HELP_NAME))
                  {
                     internalHelpWindowDetails.Name = valueStr;
                  }
                  else if (attribute.Equals(XMLConstants.MG_ATTR_INTERNAL_HELP_FRAMEX))
                  {
                     internalHelpWindowDetails.FrameX = Convert.ToInt32(XMLparser.unescape(valueStr));
                  }
                  else if (attribute.Equals(XMLConstants.MG_ATTR_INTERNAL_HELP_FRAMEY))
                  {
                     internalHelpWindowDetails.FrameY = Convert.ToInt32(XMLparser.unescape(valueStr));
                  }
                  else if (attribute.Equals(XMLConstants.MG_ATTR_INTERNAL_HELP_FRAMEDX))
                  {
                     internalHelpWindowDetails.FrameDx = Convert.ToInt32(XMLparser.unescape(valueStr));
                  }
                  else if (attribute.Equals(XMLConstants.MG_ATTR_INTERNAL_HELP_FRAMEDY))
                  {
                     internalHelpWindowDetails.FrameDy = Convert.ToInt32(XMLparser.unescape(valueStr));
                  }
                  else if (attribute.Equals(XMLConstants.MG_ATTR_INTERNAL_HELP_SIZEDX))
                  {
                     internalHelpWindowDetails.SizedX = Convert.ToInt32(XMLparser.unescape(valueStr));
                  }
                  else if (attribute.Equals(XMLConstants.MG_ATTR_INTERNAL_HELP_SIZEDY))
                  {
                     internalHelpWindowDetails.SizedY = Convert.ToInt32(XMLparser.unescape(valueStr));
                  }
                  else if (attribute.Equals(XMLConstants.MG_ATTR_INTERNAL_HELP_FACTORX))
                  {
                     internalHelpWindowDetails.FactorX = Convert.ToInt32(XMLparser.unescape(valueStr));
                  }
                  else if (attribute.Equals(XMLConstants.MG_ATTR_INTERNAL_HELP_FACTORY))
                  {
                     internalHelpWindowDetails.FactorY = Convert.ToInt32(XMLparser.unescape(valueStr));
                  }
                  else if (attribute.Equals(XMLConstants.MG_ATTR_INTERNAL_HELP_BORDERSTYLE))
                  {
                     internalHelpWindowDetails.Borderstyle = Convert.ToInt32(XMLparser.unescape(valueStr));
                  }
                  else if (attribute.Equals(XMLConstants.MG_ATTR_INTERNAL_HELP_WINSTYLE))
                  {
                     internalHelpWindowDetails.WinStyle = Convert.ToInt32(XMLparser.unescape(valueStr));
                  }
                  else if (attribute.Equals(XMLConstants.MG_ATTR_INTERNAL_HELP_FONT_TABLE_INDEX))
                  {
                     internalHelpWindowDetails.FontTableIndex = Convert.ToInt32(XMLparser.unescape(valueStr));
                  }
                  else
                     Events.WriteErrorToLog(
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
                     wndHelpDetails.HelpCommand = (HelpComand)Convert.ToInt32(valueStr);
                  }
                  else if (attribute.Equals(XMLConstants.MG_ATTR_WINDOWS_HELP_KEY))
                  {
                     wndHelpDetails.HelpKey = XMLparser.unescape(valueStr);
                  }
                  else
                     Events.WriteErrorToLog(
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
