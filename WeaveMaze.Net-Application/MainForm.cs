using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SimplexLab.WeaveMaze.TApplication
{
    public partial class MainForm : Form
    {
        private RectangularWeaveMazeField mazeField;
        private WeaveMazeSolution mazeSolution;

        public MainForm()
        {
            InitializeComponent();
        }

        #region Handler

        private void OnGeneratoinClickedHandler(object sender, System.EventArgs e)
        {
            OnGeneratoinClickedHandler();
        }

        private async Task OnGeneratoinClickedHandler()
        {
            PrevProcess();
            {
                await Generate();
            }
            PostProcess();
        }

        private void PrevProcess()
        {
            generation.Enabled = false;
            showSolution.Enabled = false;
        }

        private async Task Generate()
        {
            await GenerateRectangularWeaveMaze();
        }

        private void PostProcess()
        {
            generation.Enabled = true;
            showSolution.Enabled = true;

            canvas.Refresh();
        }

        private void OnCanvasPaintHandler(object sender, PaintEventArgs e)
        {
            DrawRectangularWeaveMaze(e.Graphics);
        }

        private void OnShowSolutionChanged(object sender, System.EventArgs e)
        {
            canvas.Refresh();
        }

        private void OnShowRoundedCornersChanged(object sender, System.EventArgs e)
        {
            canvas.Refresh();
        }

        #endregion

        #region Rectangular

        private async Task GenerateRectangularWeaveMaze()
        {
            var width = rectangularMazeControl.MazeWidth;
            var height = rectangularMazeControl.MazeHeight;
            var loopFrac = rectangularMazeControl.LoopFraction;
            var crossFrac = rectangularMazeControl.CrossFraction;
            var longPassages = rectangularMazeControl.LongPassages;

            if (width <= 0) width = canvas.Width / 30;
            if (height <= 0) height = canvas.Height / 30;

            var generator = new RectangularWeaveMazeGenerator();
            mazeField = await generator.GenerateAsync(width, height, loopFrac, crossFrac, longPassages, null);

            var solutionGenerator = new WeaveMazeSolutionGenerator();
            mazeSolution = solutionGenerator.Generate(mazeField);
        }

        private void DrawRectangularWeaveMaze(Graphics grap)
        {
            var renderer = new RectangularWeaveMazeRenderer();
            renderer.SetSize(canvas.Width, canvas.Height)
                    .SetField(mazeField)
                    .SetSolution(mazeSolution)
                    .SetShowSolution(showSolution.Checked)
                    .SetRoundedCorners(showRoundedCorners.Checked)
                    .Draw(grap);
        }

        #endregion
    }
}
