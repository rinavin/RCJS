using System;
using System.Collections.Generic;
using com.magicsoftware.util;
using com.magicsoftware.unipaas.gui;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.unipaas.management.tasks;

namespace com.magicsoftware.unipaas.management.events
{
   /// <summary>
   /// functionality required by the GUI namespace from the EventsManager class.
   /// </summary>
   public interface IEventsManager
   {
      /// <summary></summary>
      /// <param name="ctrl"></param>
      /// <param name="DisplayLine"></param>
      /// <param name="code"></param>
      void addInternalEvent(MgControlBase ctrl, int DisplayLine, int code);

      /// <summary></summary>
      /// <param name="form"></param>
      /// <param name="ctrl"></param>
      /// <param name="modifier"></param>
      /// <param name="keyCode"></param>
      /// <param name="start"></param>
      /// <param name="end"></param>
      /// <param name="text"></param>
      /// <param name="im"></param>
      /// <param name="isActChar"></param>
      /// <param name="suggestedValue"></param>
      /// <param name="code"></param>
      void AddKeyboardEvent(MgFormBase form, MgControlBase ctrl, Modifiers modifier, int keyCode,
                            int start, int end, string text, ImeParam im,
                            bool isActChar, string suggestedValue, int code);

      /// <summary></summary>
      /// <param name="ctrl"></param>
      /// <param name="code"></param>
      /// <param name="priority"></param>
      void addInternalEvent(MgControlBase ctrl, int code, Priority priority);

      /// <summary> Add internal event that was triggered by GUI queue to the queue </summary>
      /// <param name="ctrl"></param>
      /// <param name="code"></param>
      /// <param name="line"></param>
      void addGuiTriggeredEvent(MgControlBase ctrl, int code, int line);

      /// <summary> Add internal event that was triggered by GUI queue to the queue </summary>
      /// <param name="ctrl"></param>
      /// <param name="code"></param>
      /// <param name="line"></param>
      void addGuiTriggeredEvent(MgControlBase ctrl, int code, int line, Modifiers modifier);

      /// <summary> Add internal event that was triggered by GUI queue to the queue </summary>
      /// <param name="ctrl"></param>
      /// <param name="code"></param>
      /// <param name="line"></param>
      /// <param name="dotNetArgs"></param>
      /// <param name="onMultiMark">true if mutimark continues</param>
      void addGuiTriggeredEvent(MgControlBase ctrl, int code, int line, Object[] dotNetArgs, bool onMultiMark);

      /// <summary> Add internal event that was triggered by GUI queue to the queue </summary>
      /// <param name="ctrl"></param>
      /// <param name="code"></param>
      /// <param name="line"></param>
      /// <param name="dotNetArgs"></param>
      /// <param name="raisedBy"></param>
      /// <param name="onMultiMark">true if mutimark continues</param>
      void addGuiTriggeredEvent(MgControlBase ctrl, int code, int line, Object[] dotNetArgs, RaisedBy raisedBy, bool onMultiMark);

      /// <summary> Add internal event that was triggered by GUI queue to the queue </summary>
      /// <param name="task"></param>
      /// <param name="code"></param>
      void addGuiTriggeredEvent(ITask task, int code);

      /// <summary>
      /// Add internal event that was triggered by GUI queue to the queue
      /// </summary>
      /// <param name="task"></param>
      /// <param name="code"></param>
      /// <param name="onMultiMark"> true if mutimark continues</param>
      void addGuiTriggeredEvent(ITask task, int code, bool onMultiMark);

      
      /// <summary> Add internal event that was triggered by GUI queue to the queue </summary>
      /// <param name="task"></param>
      /// <param name="code"></param>
      /// <param name="raisedBy"></param>
      void addGuiTriggeredEvent(ITask task, int code, RaisedBy raisedBy);

      /// <summary> handle Column Click event on Column </summary>
      /// <param name="columnCtrl"></param>
      /// <param name="direction"></param>
      /// <param name="columnHeader"></param>
      void AddColumnClickEvent(MgControlBase columnCtrl, int direction, String columnHeader);

      /// <summary>
      /// handle column filter event on column
      /// </summary>
      /// <param name="columnCtrl"></param>
      /// <param name="columnHeader"></param>
      /// <param name="index"></param>
      /// <param name="x"></param>
      /// <param name="y"></param>
      void AddColumnFilterEvent(MgControlBase columnCtrl, String columnHeader, int x, int y, int width, int height);
   }
}
