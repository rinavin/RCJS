using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.richclient.util;
using com.magicsoftware.unipaas.management.exp;
using com.magicsoftware.util;
using System.Text.RegularExpressions;

namespace com.magicsoftware.richclient.rt
{
   internal class ArgumentsList
   {
      private List<Argument> _list;

      // String for subform arguments
      // Only for offline. It's relevant for call with destination 
      internal String RefreshOnString { get; set; }

      /// <summary>
      ///   CTOR
      /// </summary>
      internal ArgumentsList()
      {
      }

      /// <summary>
      ///   CTOR that creates a VALUE argument list using a given argument list
      /// </summary>
      internal ArgumentsList(ArgumentsList srcArgs)
         : this()
      {
         if (srcArgs == null)
            _list = null;
         else
         {
            _list = new List<Argument>();

            for (int i = 0;
                 i < srcArgs.getSize();
                 i++)
               _list.Add(new Argument(srcArgs.getArg(i)));
         }
      }

      /// <summary>
      ///   CTOR that creates a VALUE argument list using a given expression value list
      /// </summary>
      internal ArgumentsList(GuiExpressionEvaluator.ExpVal[] Exp_params)
         : this()
      {
         int argCnt = Exp_params.Length;

         if (argCnt == 0)
            _list = null;
         else
         {
            _list = new List<Argument>();

            for (int i = 0;
                 i < argCnt;
                 i++)
               _list.Add(new Argument(Exp_params[i]));
         }
      }

      /// <summary>
      /// CTOR - create an argument list with one specified argument
      /// </summary>
      /// <param name="argument"></param>
      public ArgumentsList(Argument argument)
      {
         Add(argument);
      }

      /// <summary>
      ///   fill the argument list
      /// </summary>
      /// <param name = "valueStr">string in the format of: "[F:str,str]|[E:str]$..."</param>
      /// <param name = "srcTask">the source task for parsing expression arguments</param>
      internal void fillList(String valueStr, Task srcTask)
      {
         String[] sTok = StrUtil.tokenize(valueStr, "$");
         Argument arg;
         int i;

         int size = sTok.Length;
         _list = new List<Argument>();

         for (i = 0;
              i < size;
              i++)
         {
            arg = new Argument();
            arg.fillData(sTok[i], srcTask);
            _list.Add(arg);
         }
      }

      /// <summary>
      ///   builds argument list from the event parameters
      /// </summary>
      /// <param name = "size">   - argument number </param>
      /// <param name = "attrs">  - string of attributes</param>
      /// <param name = "vals">   - value strings</param>
      /// <param name = "nulls">  - arguments nulls</param>
      internal void buildListFromParams(int size, String attrs, String[] vals, String nulls)
      {
         Argument arg;
         int i;

         _list = new List<Argument>();

         for (i = 0;
              i < size;
              i++)
         {
            arg = new Argument();
            arg.fillDataByParams((StorageAttribute)attrs[i], vals[i], (nulls[i] != '0'));
            _list.Add(arg);
         }
      }

      /// <summary>
      ///   build the XML string for the arguments list
      /// </summary>
      /// <param name = "message">the XML string to append the arguments to</param>
      internal void buildXML(StringBuilder message)
      {
         int i;

         for (i = 0;
              i < _list.Count;
              i++)
         {
            if (i > 0)
               message.Append('$');
            _list[i].buildXML(message);
         }
      }

      /// <summary>
      ///   returns the size of the list
      /// </summary>
      internal int getSize()
      {
         return null == _list
                   ? 0
                   : _list.Count;
      }

      /// <summary>
      ///   returns an argument item from the list by its index
      /// </summary>
      /// <param name = "idx">the index of the requested argument</param>
      internal Argument getArg(int idx)
      {
         if (idx < 0 || idx >= _list.Count)
            return null;

         return _list[idx];
      }

      /// <summary>
      ///   get Value of Argument
      /// </summary>
      /// <param name = "idx">the index of the requested argument</param>
      /// <param name = "expType">type of expected type of evaluation, for expression only</param>
      /// <param name = "expSize">size of expected string from evaluation, for expression only</param>
      /// <returns> value of evaluated Argument</returns>
      internal String getArgValue(int idx, StorageAttribute expType, int expSize)
      {
         Argument arg = getArg(idx);

         if (arg == null)
            return null;
         return arg.getValue(expType, expSize);
      }

      /// <returns> translation of the arguments content into Magic URL style arguments</returns>
      internal String toURL(bool makePrintable)
      {
         int i;
         var htmlArgs = new StringBuilder();

         for (i = 0;
              i < getSize();
              i++)
         {
            if (i > 0)
               htmlArgs.Append(ConstInterface.REQ_ARG_COMMA);
            _list[i].toURL(htmlArgs, makePrintable);
         }

         return htmlArgs.ToString();
      }

      /// <summary>
      ///   This method fills the argument data from the mainProgVar strings
      /// </summary>
      /// <param name = "mainProgVars">- a vector of strings of main program variables</param>
      /// <param name = "ctlIdx"></param>
      internal void fillListByMainProgVars(List<String> mainProgVars, int ctlIdx)
      {
         if (mainProgVars == null)
            _list = null;
         else
         {
            _list = new List<Argument>();
            Task mainProgTask = (Task)MGDataCollection.Instance.GetMainProgByCtlIdx(ctlIdx);
            for (int i = 0;
                 i < mainProgVars.Count;
                 i++)
            {
               var arg = new Argument();
               arg.fillDataByMainProgVars(mainProgVars[i], mainProgTask);
               _list.Add(arg);
            }
         }
      }

      /// <summary>
      /// fill the arguments list from a string
      /// </summary>
      /// <param name="argsString"></param>
      internal void FillListFromString(String argsString)
      {
         String[] st = Regex.Split(argsString, "(?<!\\\\),", RegexOptions.IgnorePatternWhitespace);
         _list = new List<Argument>();

         for (int i = 0; i < st.Length; i++)
         {
            Argument argument = new Argument();
            argument.FillFromString(st[i]);
            _list.Add(argument);
         }
      }

      /// <summary>
      /// add an argument to the list
      /// </summary>
      /// <param name="argument"></param>
      internal void Add(Argument argument)
      {
         if (_list == null)
            _list = new List<Argument>();

         _list.Add(argument);
      }
   }
}