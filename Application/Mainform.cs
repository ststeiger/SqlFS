using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            Hide();
            Visible = false;
        }

        private void menuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            if (e.ClickedItem.Text.Equals("Exit"))
                Application.Exit();
            if (e.ClickedItem.Text.Equals("Change"))
            {
                Credentials c = new Credentials();
                c.ShowDialog();
                
            }

        }


    }
}
