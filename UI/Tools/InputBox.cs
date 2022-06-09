using System;
using System.Windows.Forms;

namespace Nyerguds.Util.UI
{
    /// <summary>
    /// Shows a prompt in a dialog box using the static method Show().
    /// </summary>
    public partial class InputBox : Form
    {
        InputBox()
        {
            this.InitializeComponent();
        }

        void buttonCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        void buttonOK_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Displays a prompt in a dialog box, waits for the user to input text or click a button.
        /// </summary>
        /// <param name="prompt">String expression displayed as the message in the dialog box.</param>
        /// <param name="title">String expression displayed in the title bar of the dialog box.</param>
        /// <param name="defaultText">String expression displayed in the text box as the default response.</param>
        /// <param name="xpos">Numeric expression that specifies the distance of the left edge of the dialog box from the left edge of the screen.</param>
        /// <param name="ypos">Numeric expression that specifies the distance of the upper edge of the dialog box from the top of the screen.</param>
        /// <param name="startPosition">Form start position.</param>
        /// <returns>A string which is null if the user pressed Cancel.</returns>
        static string Show(string prompt, string title, string defaultText, int xpos, int ypos, FormStartPosition startPosition)
        {
            using var form = new InputBox();
            form.labelPrompt.Text = prompt;
            form.Text = title;
            form.textBoxText.Text = defaultText;
            if (startPosition == FormStartPosition.Manual && xpos >= 0 && ypos >= 0)
            {
                form.Left = xpos;
                form.Top = ypos;
            }
            else
                form.StartPosition = startPosition;
            var result = form.ShowDialog();
            if (result != DialogResult.OK)
                return null;
            return form.textBoxText.Text;
        }

        /// <summary>
        /// Displays a prompt in a dialog box, waits for the user to input text or click a button.
        /// </summary>
        /// <param name="prompt">String expression displayed as the message in the dialog box.</param>
        /// <param name="title">String expression displayed in the title bar of the dialog box.</param>
        /// <param name="defaultText">String expression displayed in the text box as the default response.</param>
        /// <param name="xpos">Numeric expression that specifies the distance of the left edge of the dialog box from the left edge of the screen.</param>
        /// <param name="ypos">Numeric expression that specifies the distance of the upper edge of the dialog box from the top of the screen.</param>
        /// <returns>A string which is null if the user pressed Cancel.</returns>
        public static string Show(string prompt, string title, string defaultText, int xpos, int ypos)
        {
            return Show(prompt, title, defaultText, xpos, ypos, FormStartPosition.Manual);
        }

        /// <summary>
        /// Displays a prompt in a dialog box, waits for the user to input text or click a button.
        /// </summary>
        /// <param name="prompt">String expression displayed as the message in the dialog box.</param>
        /// <param name="title">String expression displayed in the title bar of the dialog box.</param>
        /// <param name="defaultText">String expression displayed in the text box as the default response.</param>
        /// <returns>A string which is null if the user pressed Cancel.</returns>
        public static string Show(string prompt, string title, string defaultText)
        {
            return Show(prompt, title, defaultText, -1, -1, FormStartPosition.CenterScreen);
        }

        /// <summary>
        /// Displays a prompt in a dialog box, waits for the user to input text or click a button.
        /// </summary>
        /// <param name="prompt">String expression displayed as the message in the dialog box.</param>
        /// <param name="title">String expression displayed in the title bar of the dialog box.</param>
        /// <param name="defaultText">String expression displayed in the text box as the default response.</param>
        /// <param name="startPosition">Form start position.</param>
        /// <returns>A string which is null if the user pressed Cancel.</returns>
        public static string Show(string prompt, string title, string defaultText, FormStartPosition startPosition)
        {
            return Show(prompt, title, defaultText, -1, -1, startPosition);
        }
    }
}
