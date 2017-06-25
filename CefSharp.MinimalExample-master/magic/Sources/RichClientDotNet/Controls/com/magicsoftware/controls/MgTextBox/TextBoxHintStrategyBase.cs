using System;
using System.Drawing;

namespace com.magicsoftware.controls
{
   public abstract class TextBoxHintStrategyBase
   {
      /// <summary>
      /// Base strategy for TextBox type
      /// </summary>

      protected MgTextBox mgTextBox;


      #region properties
      /// <summary>
      /// Hint Foreground color
      /// </summary>
      public virtual Color HintFgColor { get; set; }
      /// <summary>
      /// Hint Text
      /// </summary>
      public virtual string HintText { get; set; }

      /// <summary>
      /// returns if Hint is shown
      /// </summary>
      public virtual bool IsHintEnabled { get { return false; } }

      /// <summary>
      /// Checks if hint text has value
      /// </summary>
      /// <returns></returns>
      public virtual bool IsHintTextHasValue { get { return false; } }

      #endregion properties

      /// <summary>
      /// Constructor
      /// </summary>
      /// <param name="mgTextBox"></param>
      public TextBoxHintStrategyBase(MgTextBox mgTextBox)
      {
         this.mgTextBox = mgTextBox;
      }

      #region virtual methods
      /// <summary>
      /// Checks if hint watermark should be enabled or disabled
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="args"></param>
      public virtual void UpdateHintStatus()
      {
      }

      internal virtual void UnregisterEvents()
      {
      }

      /// <summary>
      /// Enable hint watermark
      /// </summary>
      public virtual void EnableHint()
      {
      }

      /// <summary>
      /// Disable hint watermark
      /// </summary>
      public virtual void DisbaleHint()
      {
      }
      #endregion virtual methods

   }
}
