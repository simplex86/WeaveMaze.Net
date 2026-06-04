using System.Windows.Forms;

namespace SimplexLab.WeaveMaze.TApplication
{
    public partial class MainForm : Form
    {
        private RectangularWeaveMazeField mazeField;

        public MainForm()
        {
            InitializeComponent();
        }

        private void OnGeneratoinClickedHandler(object sender, System.EventArgs e)
        {
            var width = rectangularMazeControl.MazeWidth;
            var height = rectangularMazeControl.MazeHeight;

            var generator = new RectangularWeaveMazeGenerator();
            mazeField = generator.Generate(width, height, 0.5, 0.5, false, null);

            canvas.Refresh();
        }

        private void OnCanvasPaintHandler(object sender, PaintEventArgs e)
        {
            var renderer = new RectangularMazeRenderer();
            renderer.SetSize(canvas.Width, canvas.Height)
                    .SetField(mazeField)
                    .Draw(e.Graphics);
        }
    }
}
