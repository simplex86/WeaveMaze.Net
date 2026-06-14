using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace SimplexLab.WeaveMaze.TApplication
{
    public partial class RectangularMazeControl : UserControl
    {
        private NumericUpDown width;
        private NumericUpDown height;
        private NumericUpDown loopFraction;
        private NumericUpDown crossFraction;
        private CheckBox longPassages;

        public RectangularMazeControl()
        {
            InitializeComponent();
        }

        public int MazeWidth => (int)(width.Value ?? 0);
        public int MazeHeight => (int)(height.Value ?? 0);
        public double LoopFraction => (double)(loopFraction.Value ?? 0) / 100.0;
        public double CrossFraction => (double)(crossFraction.Value ?? 0) / 100.0;
        public bool LongPassages => longPassages.IsChecked == true;

        public void SetReconstructionValues(int w, int h, double loopFrac, double crossFrac, bool longPass)
        {
            width.Value = w;
            height.Value = h;
            loopFraction.Value = (decimal)(loopFrac * 100);
            crossFraction.Value = (decimal)(crossFrac * 100);
            longPassages.IsChecked = longPass;
        }

        private void InitializeComponent()
        {
            width = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 200,
                Value = 0,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };

            height = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 200,
                Value = 0,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };

            loopFraction = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 100,
                Value = 5,
                Increment = 1,
                FormatString = "0",
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };

            crossFraction = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 100,
                Value = 25,
                Increment = 1,
                FormatString = "0",
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };

            longPassages = new CheckBox
            {
                Content = "Long Passages",
            };

            var panel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 6,
                Children =
                {
                    new Grid
                    {
                        ColumnDefinitions = ColumnDefinitions.Parse("80,*"),
                        Children =
                        {
                            new TextBlock { Text = "Width", VerticalAlignment = VerticalAlignment.Center }.WithGridColumn(0),
                            width.WithGridColumn(1),
                        }
                    },
                    new Grid
                    {
                        ColumnDefinitions = ColumnDefinitions.Parse("80,*"),
                        Children =
                        {
                            new TextBlock { Text = "Height", VerticalAlignment = VerticalAlignment.Center }.WithGridColumn(0),
                            height.WithGridColumn(1),
                        }
                    },
                    new Grid
                    {
                        ColumnDefinitions = ColumnDefinitions.Parse("80,*"),
                        Children =
                        {
                            new TextBlock { Text = "Loop", VerticalAlignment = VerticalAlignment.Center }.WithGridColumn(0),
                            loopFraction.WithGridColumn(1),
                        }
                    },
                    new Grid
                    {
                        ColumnDefinitions = ColumnDefinitions.Parse("80,*"),
                        Children =
                        {
                            new TextBlock { Text = "Cross", VerticalAlignment = VerticalAlignment.Center }.WithGridColumn(0),
                            crossFraction.WithGridColumn(1),
                        }
                    },
                    longPassages,
                }
            };

            Content = panel;
        }
    }
}
