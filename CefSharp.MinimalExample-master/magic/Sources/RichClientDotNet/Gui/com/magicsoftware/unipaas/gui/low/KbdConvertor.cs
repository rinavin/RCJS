using System.Windows.Forms;

namespace com.magicsoftware.unipaas.gui.low
{

    /// <summary> Converts SWT keyboard code to a magic one
    /// 
    /// </summary>
    /// <author>  rinav
    /// 
    /// </author>
    internal class KbdConvertor
    {
       /// <summary> returns true if a keycode is a only a modifier
       /// 
       /// </summary>
       /// <param name="keyCode">
       /// </param>
       /// <returns>
       /// </returns>
       internal static bool isModifier(Keys keyCode)
       {
          // Alt = (Keys.RButton | Keys.ShiftKey)
          return (keyCode == (Keys.RButton | Keys.ShiftKey) || keyCode == Keys.ControlKey || keyCode == Keys.ShiftKey);
       }

    }
}