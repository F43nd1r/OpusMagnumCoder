using System;
using System.Windows.Forms;

namespace OpusMagnumCoder
{
    public sealed partial class TextDialog : Form
    {
        public TextDialog(string initialValue, string title, string positiveButtonText)
        {
            InitializeComponent();
            textBox1.Text = initialValue;
            Text = title;
            button1.Text = positiveButtonText;
        }

        public string UserInput => textBox1.Text;

        private void button1_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}