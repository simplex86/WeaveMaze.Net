using System;
using System.Windows.Forms;

namespace SimplexLab.WeaveMaze.TApplication
{
    public partial class CustomizedMazeControl : UserControl
    {
        public CustomizedMazeControl()
        {
            InitializeComponent();
        }

        public string FileName { get; private set; }
        public double LoopFraction => (double)loopFraction.Value / 100.0;
        public double CrossFraction => (double)crossFraction.Value / 100.0;
        public int Samples => (int)samples.Value;
        public bool LongPassages => longPassages.Checked;

        private void OnBrowerClickedHandler(object sender, EventArgs e)
        {
            var dialog = new OpenFileDialog()
            {
                Filter = "(*.bmp)|*.bmp|(*.png)|*.png|(*.jpg)|*.jpg",
                FilterIndex = 2,
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                FileName = dialog.FileName;
                filename.Text = FileName;
            }
        }
    }
}
