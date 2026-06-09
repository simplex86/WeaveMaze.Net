using System.Drawing;
using System.IO;
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
            masktype.SelectedIndex = 0;
        }

        #region Handler

        private void OnMaskChangedHandler(object sender, System.EventArgs e)
        {
            rectangularMazeControl.Visible = masktype.SelectedIndex == 0;
            rectangularMazeMaskControl.Visible = masktype.SelectedIndex == 1;
        }

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
            masktype.Enabled = false;
            rectangularMazeControl.Enabled = false;
            rectangularMazeMaskControl.Enabled = false;
            generation.Enabled = false;
            showRoundedCorners.Enabled = false;
            showSolution.Enabled = false;
        }

        private async Task Generate()
        {
            await GenerateRectangularWeaveMaze();
        }

        private void PostProcess()
        {
            masktype.Enabled = true;
            rectangularMazeControl.Enabled = true;
            rectangularMazeMaskControl.Enabled = true;
            generation.Enabled = true;
            showRoundedCorners.Enabled = true;
            showSolution.Enabled = true;

            canvas.Refresh();
        }

        private void OnCanvasPaintHandler(object sender, PaintEventArgs e)
        {
            DrawRectangularWeaveMaze(e.Graphics);
            DrawRectangularWeaveMazeSolution(e.Graphics);
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
            var generator = new RectangularWeaveMazeGenerator();

            if (masktype.SelectedIndex == 0)
            {
                var width = rectangularMazeControl.MazeWidth;
                var height = rectangularMazeControl.MazeHeight;
                var loopFrac = rectangularMazeControl.LoopFraction;
                var crossFrac = rectangularMazeControl.CrossFraction;
                var longPassages = rectangularMazeControl.LongPassages;

                if (width  <= 0) width  = canvas.Width  / 30;
                if (height <= 0) height = canvas.Height / 30;

                mazeField = await generator.GenerateAsync(width, height, loopFrac, crossFrac, longPassages);
            }
            else if (masktype.SelectedIndex == 1)
            {
                var filename = rectangularMazeMaskControl.FileName;
                var loopFrac = rectangularMazeMaskControl.LoopFraction;
                var crossFrac = rectangularMazeMaskControl.CrossFraction;
                var longPassages = rectangularMazeMaskControl.LongPassages;

                if (string.IsNullOrEmpty(filename) || !File.Exists(filename))
                {
                    var width = canvas.Width / 30;
                    var height = canvas.Height / 30;

                    mazeField = await generator.GenerateAsync(width, height, loopFrac, crossFrac, longPassages);
                }
                else
                {
                    var mask = RectangularWeaveMazeMaskLoader.Load(filename);
                    mazeField = await generator.GenerateAsync(mask, loopFrac, crossFrac, longPassages);
                }
            }

            var solutionGenerator = new WeaveMazeSolutionGenerator();
            mazeSolution = solutionGenerator.Generate(mazeField);
        }

        private void DrawRectangularWeaveMaze(Graphics grap)
        {
            var renderer = new RectangularWeaveMazeRenderer();
            renderer.SetSize(canvas.Width, canvas.Height)
                    .SetField(mazeField)
                    .SetRoundedCorners(showRoundedCorners.Checked)
                    .Draw(grap);
        }

        private void DrawRectangularWeaveMazeSolution(Graphics grap)
        {
            if (!showSolution.Checked) return;

            var renderer = new RectangularWeaveMazeSolutionRenderer();
            renderer.SetSize(canvas.Width, canvas.Height)
                    .SetField(mazeField)
                    .SetSolution(mazeSolution)
                    .SetRoundedCorners(showRoundedCorners.Checked)
                    .Draw(grap);
        }

        #endregion
    }
}
