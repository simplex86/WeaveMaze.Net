using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SimplexLab.WeaveMaze.TApplication
{
    public partial class RectangularMazeMaskControl : UserControl
    {
        public RectangularMazeMaskControl()
        {
            InitializeComponent();
        }

        public string FileName { get; private set; }
        public double LoopFraction => (double)loopFraction.Value / 100.0;
        public double CrossFraction => (double)crossFraction.Value / 100.0;
        public bool LongPassages => longPassages.Checked;

        private void OnBrowerClickedHandler(object sender, EventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "(*.bmp)|*.bmp|(*.png)|*.png|(*.jpg)|*.jpg";
            dialog.FilterIndex = 2;

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                FileName = dialog.FileName;
                filename.Text = FileName;
            }
        }
    }
}
