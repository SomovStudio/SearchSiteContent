using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SearchSiteContent
{
    public partial class FormInputBox : Form
    {
        public FormInputBox()
        {
            InitializeComponent();
        }

        public FormSearchSiteContent Parent;

        private void FormInputBox_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            if(Parent != null)
            {
                Parent.toolStripTextBoxPath.Text = textBox1.Text;
                Close();
            }
        }
    }
}
