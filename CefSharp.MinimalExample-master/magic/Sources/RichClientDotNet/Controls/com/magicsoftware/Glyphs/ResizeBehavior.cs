using System;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Design;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms.Design;
using System.Globalization;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms.Design.Behavior;
using System.Windows.Forms;
using com.magicsoftware.controls.designers;

namespace com.magicsoftware.Glyphs
{
   /// <summary>
   /// Inspired by System.Windows.Forms.Design.Behavior.TableLayoutPanelBehavior
   /// </summary>
   internal abstract class ResizeBehavior : Behavior
   {
      #region fields
      private Point lastMouseLoc;//used to track mouse movement deltas
      private int orgValue; //original value of resized property
      private bool pushedBehavior;//tracks if we've pushed ourself onto the stack 
      protected BehaviorService behaviorService;//used for bounds translation
      private IServiceProvider serviceProvider;//cached to allow our behavior to get services 
      private DesignerTransaction resizeTransaction;//used to make size adjustments within transaction
      private PropertyDescriptor changedProp; //cached property descriptor that refers to the resized property
      private Control control;
      private int delta; //delta changed during move
      private Orientation orientation = Orientation.Horizontal;
      private int lastValidValue;
      protected int previousDelta = 0; // saves the previous delta
      #endregion
      #region properties
      public int Delta
      {
         get { return delta; }
      }


      public bool IsPushedBehavior
      {
         get { return pushedBehavior; }
      }


      /// <summary>
      /// orientation of the resize
      /// </summary>
      public virtual Orientation Orientation
      {
         get { return orientation; }
         set { orientation = value; }
      }
      #endregion

      #region ctor
      ///
      ///     Standard constructor that caches services and clears values. 
      ///     
      internal ResizeBehavior(Control control, IServiceProvider serviceProvider)
      {
         this.control = control;
         this.serviceProvider = serviceProvider;
         behaviorService = serviceProvider.GetService(typeof(BehaviorService)) as BehaviorService;

         if (behaviorService == null)
         {
            Debug.Fail("BehaviorService could not be found!");
            return;
         }
         pushedBehavior = false;
         lastMouseLoc = Point.Empty;
      }
      #endregion

      #region methods
      /// <devdoc>
      ///     Called at the end of a resize operation -this clears state and refreshes the selection 
      /// </devdoc>
      private void FinishResize()
      {
         SetNewValueOnFinishResize(lastValidValue);
         Invalidate();

         pushedBehavior = false;
         behaviorService.PopBehavior(this);
         lastMouseLoc = Point.Empty;

         // fire ComponentChange events so this event is undoable 
         IComponentChangeService cs = serviceProvider.GetService(typeof(IComponentChangeService)) as IComponentChangeService;
         if (cs != null && changedProp != null)
         {
            cs.OnComponentChanged(control, changedProp, null, null);
            changedProp = null;
         }

         //attempt to refresh the selection 
         object selectionManager = serviceProvider.GetService(ReflecionDesignHelper.GetType("System.Windows.Forms.Design.Behavior.SelectionManager", ReflecionDesignHelper.DesignAssembly));
         ReflecionDesignHelper.InvokeMethod(selectionManager, "Refresh");
      }

      /// <devdoc> 
      ///     This method is called when we lose capture during our resize actions. 
      ///     Here, we clear our state and cancel our transaction.
      /// </devdoc> 
      public override void OnLoseCapture(Glyph g, EventArgs e)
      {
         if (pushedBehavior)
         {
            FinishResize();

            // If we still have a transaction, roll it back.
            // 
            if (resizeTransaction != null)
            {
               DesignerTransaction t = resizeTransaction;
               resizeTransaction = null;
               using (t)
               {
                  t.Cancel();
               }
            }
         }
      }


      /// <summary>
      /// This method will cache off our glyph, set the proper selection and
      //      push this behavior onto the stack. 
      /// </summary>
      /// <param name="g"></param>
      /// <param name="button"></param>
      /// <param name="mouseLoc"></param>
      /// <returns></returns>
      public override bool OnMouseDown(Glyph g, MouseButtons button, Point mouseLoc)
      {

         //we only care about the right mouse button for resizing 
         if (button == MouseButtons.Left && g is ResizeGlyph)
         {
            lastMouseLoc = mouseLoc;
            IComponentChangeService cs = serviceProvider.GetService(typeof(IComponentChangeService)) as IComponentChangeService;
            if (cs != null)
            {
               changedProp = GetChangedProp();
               orgValue = lastValidValue = GetOrgValue();
               if (changedProp != null)
               {
                  {
                     IDesignerHost host = serviceProvider.GetService(typeof(IDesignerHost)) as IDesignerHost;
                     if (host != null)
                        resizeTransaction = CreateResizeTransaction(host);

                     try
                     {
                        cs.OnComponentChanging(control, changedProp);
                     }
                     catch (CheckoutException checkoutException)
                     {
                        if (CheckoutException.Canceled.Equals(checkoutException))
                        {
                           if ((resizeTransaction != null) && (!resizeTransaction.Canceled))
                           {
                              resizeTransaction.Cancel();
                           }
                        }
                        throw;
                     }
                  }
               }
            }

            //push this resizebehavior
            behaviorService.PushCaptureBehavior(this);
            pushedBehavior = true;
         }

         return false;
      }




      /// <summary>
      ///  If we have pushed ourselves onto the stack then use our propertyDescriptor to 
      ///   make realtime adjustments to the table's rows or columns. 
      /// </summary>
      /// <param name="g"></param>
      /// <param name="button"></param>
      /// <param name="mouseLoc"></param>
      /// <returns></returns>
      public override bool OnMouseMove(Glyph g, MouseButtons button, Point mouseLoc)
      {
         if (pushedBehavior)
         {
            previousDelta = delta;
            if (Orientation == Orientation.Horizontal)
               delta = mouseLoc.Y - lastMouseLoc.Y;
            else
               delta = mouseLoc.X - lastMouseLoc.X;

            if (delta != 0)
            {
               if (CanResize(orgValue + delta))
               {
                  SetNewValueOnDrag(orgValue + delta);
                  Invalidate();
                  lastValidValue = orgValue + delta;
               }
            }
         }
         return false;
      }

      protected virtual void Invalidate()
      {
         Point location = behaviorService.MapAdornerWindowPoint(this.control.Handle, this.control.DisplayRectangle.Location);
         Rectangle clientAreaRectangle = new Rectangle(location, this.control.DisplayRectangle.Size);
         this.behaviorService.Invalidate(clientAreaRectangle);
      }

      /// <summary>
      ///  Here, we clear our cached state and commit the transaction.
      /// </summary>
      /// <param name="g"></param>
      /// <param name="button"></param>
      /// <returns></returns>
      public override bool OnMouseUp(Glyph g, MouseButtons button)
      {
         if (pushedBehavior)
         {
            FinishResize();
            delta = 0;
            //commit transaction 
            if (resizeTransaction != null)
            {
               DesignerTransaction t = resizeTransaction;
               resizeTransaction = null;
               using (t)
               {
                  t.Commit();
               }
               changedProp = null;
            }
            Invalidate();

         }
         return false;
      }
      #endregion

      #region abstractmethods

      /// <summary>
      /// returns true if newSize is legal value
      /// </summary>
      /// <param name="newSize"></param>
      /// <returns></returns>
      protected abstract bool CanResize(int newSize);

      /// <summary>
      /// set new value during drag
      /// </summary>
      /// <param name="value"></param>
      protected abstract void SetNewValueOnDrag(int value);

      /// <summary>
      /// get original value of resized property
      /// </summary>
      /// <returns></returns>
      protected abstract int GetOrgValue();

      /// <summary>
      /// get property descriptor of changed property
      /// </summary>
      /// <returns></returns>
      protected abstract PropertyDescriptor GetChangedProp();

      /// <summary>
      /// set new value on finish resize
      /// </summary>
      /// <param name="value"></param>
      protected virtual void SetNewValueOnFinishResize(int value)
      {
      }

      protected virtual DesignerTransaction CreateResizeTransaction(IDesignerHost host)
      {
         return host.CreateTransaction(control.Site.Name);
      }
      #endregion
   }

}



