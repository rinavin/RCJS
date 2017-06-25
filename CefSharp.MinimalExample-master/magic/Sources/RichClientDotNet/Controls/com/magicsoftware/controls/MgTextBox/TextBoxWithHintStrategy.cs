using System;
using System.Drawing;

namespace com.magicsoftware.controls
{
   public class TextBoxWithHintStrategy : TextBoxHintStrategyBase
   {
      /// <summary>
      /// Strategy for Hint textbox
      /// </summary>
      protected bool hintTextEnabled = false;


      #region properties
      public override bool IsHintEnabled
      {
         get
         {
            return hintTextEnabled;
         }
      }

      private Color hintFgColor = SystemColors.GrayText;
      public override Color HintFgColor
      {
         get { return hintFgColor; }
         set
         {
            if (value == Color.Empty)
               hintFgColor = SystemColors.GrayText;
            else
               hintFgColor = value;
            mgTextBox.Refresh();
         }
      }

      private string hintText;
      public override string HintText
      {
         get { return hintText; }
         set
         {
            hintText = value;
            mgTextBox.Refresh();
         }
      }
      #endregion properties

      /// <summary>
      /// Constructor
      /// </summary>
      /// <param name="mgTextBox"></param>
      public TextBoxWithHintStrategy(MgTextBox mgTextBox)
         : base(mgTextBox)
      {
         RegisterEvents();
      }

      #region events

      private void OnReadOnlyChanged(object sender, EventArgs e)
      {
         UpdateHintStatus();
      }

      private void OnClick(object sender, EventArgs args)
      {
         DisbaleHint();
      }

      void RegisterEvents()
      {
         mgTextBox.TextChanged += TextChanged;
         mgTextBox.LostFocus += AddRemoveHint;
         mgTextBox.GotFocus += AddRemoveHint;
         mgTextBox.Click += OnClick;
         mgTextBox.ReadOnlyChanged += OnReadOnlyChanged;
      }

      internal override void UnregisterEvents()
      {
         mgTextBox.TextChanged -= TextChanged;
         mgTextBox.LostFocus -= AddRemoveHint;
         mgTextBox.GotFocus -= AddRemoveHint;
         mgTextBox.Click -= OnClick;
         mgTextBox.ReadOnlyChanged -= OnReadOnlyChanged;
      }

      #endregion events

      public override bool IsHintTextHasValue
      {
         get
         {
            return HintText != null && HintText.Trim() != "";
         }
      }

      private void TextChanged(object sender, EventArgs args)
      {
         if (hintTextEnabled)
            DisbaleHint();
      }

      private void AddRemoveHint(object sender, EventArgs args)
      {
         UpdateHintStatus();
      }

      public override void UpdateHintStatus()
      {
         if (string.IsNullOrEmpty(mgTextBox.Text) && !mgTextBox.ReadOnly && IsHintTextHasValue)
            EnableHint();
         else
            DisbaleHint();
      }

      public override void EnableHint()
      {
         if (!hintTextEnabled)
         {
            mgTextBox.SetStyle(true);
            hintTextEnabled = true;
            mgTextBox.Invalidate(true);
         }
      }

      public override void DisbaleHint()
      {
         if (hintTextEnabled)
         {
            hintTextEnabled = false;
            mgTextBox.SetStyle(false);
            mgTextBox.Invalidate(true);
         }
      }
   }
}
