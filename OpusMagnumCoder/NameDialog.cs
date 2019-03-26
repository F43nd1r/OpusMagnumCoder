using System;
using System.Windows.Forms;

namespace OpusMagnumCoder
{
    public partial class NameDialog : Form
    {
        public NameDialog(string initialValue)
        {
            InitializeComponent();
            textBox1.Text = initialValue;
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