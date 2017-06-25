using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using System.Windows.Forms.Design.Behavior;
using com.magicsoftware.Glyphs;
using com.magicsoftware.util.notifyCollection;
using com.magicsoftware.controls.designers;
using com.magicsoftware.controls.utils;

namespace com.magicsoftware.controls
{
   /// <summary>
   /// designer for table control
   /// </summary>
   public class TablePanelDesigner : ParentControlDesigner
   {
      #region fields

      private TableControl tableControl;

      private DesignerVerbCollection designerVerbCollection;
      private DesignerVerb removeVerb;
      private DesignerVerb selectAttachedControlsVerb;

      DragDropHandler dragDropHandler;
      CanParentProvider canParentProvider;

      #endregion fields

      public override void Initialize(System.ComponentModel.IComponent component)
      {
         base.Initialize(component);

         // #297589. Do not show the grids on table control.
         BindingFlags bindingAttrs = BindingFlags.Instance | BindingFlags.NonPublic;
         Type classType = typeof(ParentControlDesigner);
         MemberInfo[] memberinfos = classType.GetMember("DrawGrid", bindingAttrs);
         PropertyInfo propInfo = memberinfos[0] as PropertyInfo;
         propInfo.SetValue(this, false, null);

         designerVerbCollection = new DesignerVerbCollection();
         // Verb to add buttons
         DesignerVerb addVerb = new DesignerVerb(Controls.Properties.Resources.AddColumn_s, new EventHandler(OnAddColumn));
         removeVerb = new DesignerVerb(Controls.Properties.Resources.RemoveColumn_s, new EventHandler(OnRemoveColumn));
         designerVerbCollection.Add(addVerb);
         designerVerbCollection.Add(removeVerb);
         selectAttachedControlsVerb = new DesignerVerb(Controls.Properties.Resources.SelectAttachedControls_s, new EventHandler(OnSelectAttachedControls));
         designerVerbCollection.Add(selectAttachedControlsVerb);
         SetVerbStatus();

         ((Control)Component).ControlAdded += new ControlEventHandler(TableControlDesigner_ControlAdded);
         ((Control)Component).ParentChanged += new EventHandler(TablePanelDesigner_ParentChanged);
      
         dragDropHandler = new TablePanelDragDropHandler((Control)component, this.BehaviorService, (IDesignerHost)this.GetService(typeof(IDesignerHost)));
      }

      void TablePanelDesigner_ParentChanged(object sender, EventArgs e)
      {
         canParentProvider = new CanParentProvider(this);
      }

      #region Overriden members

      public override System.ComponentModel.Design.DesignerVerbCollection Verbs
      {
         get { return designerVerbCollection; }
      }

      public override bool ParticipatesWithSnapLines
      {
         get
         {
            return false;
         }
      }

      protected override bool AllowControlLasso
      {
         get
         {
            return false;
         }
      }

      #endregion

      #region event handlers

      private void TableControlDesigner_ControlAdded(object sender, ControlEventArgs e)
      {
         // Record instance of control we're designing
         if (e.Control is TableControl)
         {
            tableControl = ((Control)Component).Controls[0] as TableControl;
            tableControl.CollectionChanged += new NotifyCollectionChangedEventHandler(tableControl_CollectionChanged);
            SetVerbStatus();
         }
      }

      /// <summary>
      /// Handle table control collection changed.
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void tableControl_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
      {
         SetVerbStatus();
      }

      #endregion

      #region designer actions
      /// <summary>
      /// Sets the status of Remove Designer Verb.
      /// </summary>
      private void SetVerbStatus()
      {
         removeVerb.Enabled = tableControl != null ? tableControl.ColumnCount > 0 : false;

         if (tableControl != null)
         {
            IDesigner designer = tableControl.GetDesignerHost().GetDesigner(tableControl);
            selectAttachedControlsVerb.Enabled = ((TableControlDesigner)designer).AssociatedComponents.Count > 0;
         }
      }

      /// <summary>
      /// Add Column verb
      /// the funtionality is in the table control so that it couls be overriten by children classes
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void OnAddColumn(object sender, System.EventArgs e)
      {
         tableControl.OnDesignerAddColumn(sender, e);
         tableControl.showColumn(tableControl.Columns[tableControl.ColumnCount - 1]);
      }

      /// <summary>
      /// remove column verb
      /// the funtionality is in the table control so that it couls be overriten by children classes
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void OnRemoveColumn(object sender, System.EventArgs e)
      {
         tableControl.OnDesignerRemoveColumn(sender, e);
         object selected = tableControl.ColumnCount > 0 ? (object)tableControl.Columns[tableControl.ColumnCount - 1] : tableControl;

         ISelectionService selectionService = (ISelectionService)(Component.Site.GetService(typeof(ISelectionService)));

         selectionService.SetSelectedComponents(new object[] { selected });
         RefreshSmartTag();
      }

      /// <summary>
      /// Select attached controls verb
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void OnSelectAttachedControls(object sender, System.EventArgs e)
      {
         SelectAttachedControls();
      }

      /// <summary>
      /// Select the attached controls of Table
      /// <summary>
      /// Select the attached controls of Table
      /// </summary>
      private void SelectAttachedControls()
      {
         IDesigner designer = tableControl.GetDesignerHost().GetDesigner(tableControl);

         ISelectionService service = (ISelectionService)this.GetService(typeof(ISelectionService));
         foreach (Component item in service.GetSelectedComponents())
            if (item is Component)
               service.SetSelectedComponents(new object[] { item }, SelectionTypes.Remove);

         if (designer is TableControlDesigner)
         {
            ((TableControlDesigner)designer).SelectAttachedControls();
         }
      }

      #endregion
      /// <summary>
      /// Refresh the smart tag added of component.
      /// </summary>
      private void RefreshSmartTag()
      {
         DesignerActionUIService actionUIService = (DesignerActionUIService)GetService(typeof(DesignerActionUIService));

         if (actionUIService != null)
         {
            actionUIService.Refresh(Component);
         }
      }

      #region glyphs for resing row and header
      public const int RESIZE_GLYPH_SIZE = 6;

      private TableRowHeightResizeBehavior rowHeightResizeBehavior;
      /// <summary>
      /// behavior for resize of row height
      /// </summary>
      private TableRowHeightResizeBehavior RowHeightResizeBehavior
      {
         get
         {
            if (rowHeightResizeBehavior == null)
               rowHeightResizeBehavior = new TableRowHeightResizeBehavior(this.tableControl, base.Component.Site);
            return rowHeightResizeBehavior;
         }
      }

      private TableTitleHeightResizeBehavior titleHeightResizeBehavior;
      /// <summary>
      /// behavior for resize of title height
      /// </summary>
      private TableTitleHeightResizeBehavior TitleHeightResizeBehavior
      {
         get
         {
            if (titleHeightResizeBehavior == null)
               titleHeightResizeBehavior = new TableTitleHeightResizeBehavior(this.tableControl, base.Component.Site);
            return titleHeightResizeBehavior;
         }
      }

      //<summary>
      //get glyhs of the designer
      //</summary>
      //<param name="selectionType"></param>
      //<returns></returns>
      public override GlyphCollection GetGlyphs(GlyphSelectionType selectionType)
      {
         GlyphCollection glyphs = base.GetGlyphs(selectionType);
         PropertyDescriptor descriptor = TypeDescriptor.GetProperties(base.Component)["Locked"];
         bool isLocked = (descriptor != null) ? ((bool)descriptor.GetValue(base.Component)) : false;

         if (selectionType != GlyphSelectionType.NotSelected && !isLocked && !tableControl.IsDragging) //selectionType == GlyphSelectionType.SelectedPrimary
         {
            Point location = base.BehaviorService.MapAdornerWindowPoint(this.tableControl.Handle, this.tableControl.DisplayRectangle.Location);
            Rectangle clientAreaRectangle = new Rectangle(location, this.tableControl.DisplayRectangle.Size);
            glyphs.Add(GetVerticalResizeGlyph(clientAreaRectangle, tableControl.TitleHeight + tableControl.RowHeight, RowHeightResizeBehavior));
            glyphs.Add(GetVerticalResizeGlyph(clientAreaRectangle, tableControl.TitleHeight, TitleHeightResizeBehavior));
         }
         return glyphs;
      }

      /// <summary>
      /// add glyph for vertical resize action
      /// </summary>
      /// <param name="glyphs"></param>
      /// <param name="clientAreaRectangle"></param>
      /// <param name="height"></param>
      /// <param name="behavior"></param>
      private ResizeGlyph GetVerticalResizeGlyph(Rectangle clientAreaRectangle, int height, Behavior behavior)
      {
         Rectangle glyphRectangle = new Rectangle(clientAreaRectangle.Left, clientAreaRectangle.Top + height - RESIZE_GLYPH_SIZE / 2, clientAreaRectangle.Width, RESIZE_GLYPH_SIZE);
         ResizeGlyph glyph = new ResizeGlyph(glyphRectangle, Cursors.HSplit, behavior);
         return glyph;
      }

      #endregion


      #region drag drop related

      protected override void OnMouseDragBegin(int x, int y)
      {
         if (canParentProvider.CanDropFromSelectedToolboxItem())
            base.OnMouseDragBegin(x, y);
      }

      protected override void OnDragEnter(DragEventArgs de)
      {
         if (canParentProvider.CanEnterDrag(de))
            base.OnDragEnter(de);
      }


      /// <summary>
      /// handle the drag drop of other controls on this Group control
      /// </summary>
      /// <param name="de"></param>
      protected override void OnDragDrop(DragEventArgs de)
      {
         dragDropHandler.BeforeDragDrop();
         bool isDroppedFromToolbox = Utils.GetIsDroppedFromToolbox(this);
         base.OnDragDrop(de);
         dragDropHandler.AfterDragDrop(de, isDroppedFromToolbox);
      }

      #endregion


      protected override void Dispose(bool disposing)
      {
         ((Control)Component).ControlAdded -= new ControlEventHandler(TableControlDesigner_ControlAdded);
         ((Control)Component).ParentChanged -= TablePanelDesigner_ParentChanged;
         base.Dispose(disposing);
      }
   }
}