using System;
using System.Drawing;
using System.ComponentModel;
using Controls.com.magicsoftware.support;


#if PocketPC
using ContentAlignment = OpenNETCF.Drawing.ContentAlignment2;
#else
using System.ComponentModel.Design.Serialization;
#endif

namespace com.magicsoftware.controls
{

#if !PocketPC
   ///class represent table column and it's properties
   [ DesignTimeVisible(false), ToolboxItem(false)]
#endif
   public class TableColumn : Component, IFontOrientation, IFontDescriptionProperty
   {
      #region Delegates

      public delegate void SectionTrackHandler(object sender,
                                               EventArgs ea);

      #endregion

      private readonly HeaderSection _headerSection;

      internal HeaderSection HeaderSection
      {
         get { return _headerSection; }
      }

      public int HeaderSectionIndex
      {
         get { return HeaderSection.Index; }
      }

      private TableControl _tableControl;

      public TableControl TableControl
      {
         get { return _tableControl; }
         set { _tableControl = value; }
      }
      private bool _isDisposed;


       public TableColumn()
       {
          _headerSection = new HeaderSection("", 40);
          TopBorder = true;
          RightBorder = true;
       }
      

      internal bool IsDisposed
      {
         get { return _isDisposed; }
      }

      public int Width
      {
         get { return _tableControl.GetColumnWidthFromSectionWidth(Index); }
         set
         {
           
            if (_tableControl != null)
            {
               _headerSection.Width = _tableControl.GetHeaderSectionWidthByColumnWidth(Index, value);
               _tableControl.PerformLayout(_tableControl, "Width");
#if !PocketPC
               _tableControl.OnComponentChanged();
#endif
            }
         }
      }


      public bool TopBorder
      {
         get { return _headerSection.TopBorder; }
         set
         {
            _headerSection.TopBorder = value;
         }
      }

      public bool RightBorder
      {
         get { return _headerSection.RightBorder; }
         set
         {
            _headerSection.RightBorder = value;
         }
      }

      public String Text
      {
         get { return _headerSection.Text; }
         set { _headerSection.Text = value; }
      }

      public Font Font
      {
         get { return _headerSection.Font; }
         set { _headerSection.Font = value; }
      }

      public FontDescription FontDescription
      {
         get { return _headerSection.FontDescription; }
         set { _headerSection.FontDescription = value; }
      }

      public int FontOrientation
      {
         get { return _headerSection.FontOrientation; }
         set { _headerSection.FontOrientation = value; }
      }

      public Color FgColor
      {
         get { return _headerSection.Color; }
         set { _headerSection.Color = value; }
      }

      Color _bgColor = Color.White;
      public Color BgColor
      {
         get { return _bgColor; }
         set { _bgColor = value; }
      }

      public ContentAlignment ContentAlignment
      {
         get { return _headerSection.ContentAlignment; }
         set { _headerSection.ContentAlignment = value; }
      }

      public Object Tag { get; set; }

      public int Index
      {
         get { return _headerSection.Index; }
      }

      public HeaderSectionSortMarks SortMark
      {
         get { return _headerSection.SortMark; }
         set { _headerSection.SortMark = value; }
      }

      public bool AllowFilter
      {
         get { return _headerSection.AllowFilter; }
         set { _headerSection.AllowFilter = value; }
      }

      public event SectionTrackHandler AfterTrackHandler; //sent after column drag
      internal event SectionTrackHandler MoveHandler; //sent if column changed its position
      public event SectionTrackHandler ClickHandler; //sent if column header clicked
      public event SectionTrackHandler ClickFilterHandler; //sent if column header clicked
      internal event SectionTrackHandler DisposeHandler; //sent if column header is disposed

      internal void OnColumnClick()
      {
         if (ClickHandler != null)
            ClickHandler(this, null);
      }

      /// <summary>
      /// Handler for column filter click event
      /// </summary>
      /// <param name="ea"></param>
      internal void OnFilterClick(HeaderSectionEventArgs ea)
      {
         if (ClickFilterHandler != null)
            ClickFilterHandler(this, ea);
      }

      /// <summary>
      /// column track
      /// </summary>
      internal void onAfterColumnTrack()
      {
         var ea = new EventArgs();

         if (AfterTrackHandler != null)
         {
            Delegate[] aHandlers = AfterTrackHandler.GetInvocationList();

            foreach (SectionTrackHandler handler in aHandlers)
            {
               try
               {
                  handler(this, ea);
               }
               catch (Exception)
               {
               }
            }
         }
      }

      /// <summary>
      /// column move
      /// </summary>
      internal void Move()
      {
         var ea = new EventArgs();

         if (MoveHandler != null)
         {
            Delegate[] aHandlers = MoveHandler.GetInvocationList();

            foreach (SectionTrackHandler handler in aHandlers)
            {
               try
               {
                  handler(this, ea);
               }
               catch (Exception)
               {
               }
            }
         }
      }

      /// <summary>
      /// Dispose Column
      /// </summary>
      public new void Dispose()
      {
         AfterTrackHandler = null;
         MoveHandler = null;
         if (DisposeHandler != null)
            DisposeHandler(this, new EventArgs());
         DisposeHandler = null;
         _isDisposed = true;
         _tableControl.RemoveColumnAt(Index);
         _headerSection.Dispose();
      }

      public string Name
      {
         get
         {
            return ToString();
         }
      }
      public override string ToString()
      {
         return "Column #" + (Index + 1) + ": " + Text;
      }
   }


}
