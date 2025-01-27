using System.Windows.Forms;

namespace Redbox.HAL.Common.GUI.Functions
{
    public static class TextBoxExtensions
    {
        public static int GetInteger(this TextBox tb, string friendlyName, OutputBox box)
        {
            if (string.IsNullOrEmpty(tb.Text))
                return -1;
            int result;
            if (int.TryParse(tb.Text, out result))
                return result;
            box.Write(string.Format("The value '{0}' in box {1} is not an integer.", (object)tb.Text, (object)friendlyName));
            return -1;
        }

        public static void InputNumber(this TextBox tb)
        {
            LocationNumberPad locationNumberPad = new LocationNumberPad();
            if (locationNumberPad.ShowDialog() != DialogResult.OK)
                return;
            tb.Text = locationNumberPad.Number.ToString();
        }
    }
}
