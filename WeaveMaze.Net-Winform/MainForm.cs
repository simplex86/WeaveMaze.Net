using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SimplexLab.WeaveMaze.TApplication
{
    public partial class MainForm : Form
    {
        private WeaveMazeField mazeField;
        private WeaveMazeGenerator mazeGenerator = new WeaveMazeGenerator();
        private WeaveMazeSolution mazeSolution;
        private WeaveMazeSolutionGenerator solutionGenerator = new WeaveMazeSolutionGenerator();
        private WeaveMazeGateGenerator gateGenerator = new WeaveMazeGateGenerator();
        private WeaveMazeGate[] mazeGates;

        public MainForm()
        {
            InitializeComponent();
            shape.SelectedIndex = (int)EWeaveMazeShape.Rectangular;
        }

        #region Handler

        private void OnMaskChangedHandler(object sender, System.EventArgs e)
        {
            rectangularMazeControl.Visible = shape.SelectedIndex == 0;
            customizedMazeControl.Visible = shape.SelectedIndex == 1;
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
            shape.Enabled = false;
            rectangularMazeControl.Enabled = false;
            customizedMazeControl.Enabled = false;
            generation.Enabled = false;
            showRoundedCorners.Enabled = false;
            showSolution.Enabled = false;
        }

        private async Task Generate()
        {
            switch ((EWeaveMazeShape)shape.SelectedIndex)
            {
                case EWeaveMazeShape.Rectangular:
                    await GenerateRectangularWeaveMaze();
                    break;
                case EWeaveMazeShape.Customized:
                    await GenerateCustomizedWeaveMaze();
                    break;
            }

            mazeSolution = solutionGenerator.Generate(mazeField);
            mazeGates = gateGenerator.Generate(mazeField, mazeSolution);
        }

        private void PostProcess()
        {
            shape.Enabled = true;
            rectangularMazeControl.Enabled = true;
            customizedMazeControl.Enabled = true;
            generation.Enabled = true;
            showRoundedCorners.Enabled = true;
            showSolution.Enabled = true;

            canvas.Refresh();
        }

        private void OnCanvasPaintHandler(object sender, PaintEventArgs e)
        {
            if (mazeField != null)
            {
                DrawWeaveMaze(e.Graphics);
                DrawWeaveMazeSolution(e.Graphics);
            }
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

            var field = new RectangularWeaveMazeField(width, height, loopFrac, crossFrac, longPassages);
            mazeField = await mazeGenerator.GenerateAsync(field);
        }

        private async Task GenerateCustomizedWeaveMaze()
        {
            var filename = customizedMazeControl.FileName;
            if (string.IsNullOrEmpty(filename) || !File.Exists(filename))
            {
                return;
            }

            var loopFrac = customizedMazeControl.LoopFraction;
            var crossFrac = customizedMazeControl.CrossFraction;
            var longPassages = customizedMazeControl.LongPassages;

            var mask = CustomizedWeaveMazeMaskLoader.Load(filename);
            var field = new CustomizedWeaveMazeField(mask, loopFrac, crossFrac, longPassages);

            mazeField = await mazeGenerator.GenerateAsync(field);
        }


        private void DrawWeaveMaze(Graphics grap)
        {
            using var gc = new GraphicsContext(grap);
            var renderer = new WeaveMazeRenderer();
            renderer.SetSize(canvas.Width, canvas.Height)
                    .SetField(mazeField)
                    .SetGates(mazeGates)
                    .SetRoundedCorners(showRoundedCorners.Checked)
                    .Draw(gc);
        }

        private void DrawWeaveMazeSolution(Graphics grap)
        {
            if (!showSolution.Checked) return;

            using var gc = new GraphicsContext(grap);
            var renderer = new WeaveMazeSolutionRenderer();
            renderer.SetSize(canvas.Width, canvas.Height)
                    .SetField(mazeField)
                    .SetSolution(mazeSolution)
                    .SetGates(mazeGates)
                    .SetRoundedCorners(showRoundedCorners.Checked)
                    .Draw(gc);
        }

        #endregion
    }
}
