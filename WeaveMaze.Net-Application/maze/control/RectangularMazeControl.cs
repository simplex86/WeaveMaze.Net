using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SimplexLab.WeaveMaze.TApplication
{
    public partial class RectangularMazeControl : UserControl
    {
        public RectangularMazeControl()
        {
            InitializeComponent();
        }

        public int MazeWidth => (int)width.Value;
        public int MazeHeight => (int)height.Value;
    }
}
