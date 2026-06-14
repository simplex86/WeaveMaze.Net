using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace SimplexLab.WeaveMaze.TApplication
{
    public partial class CircularMazeControl : UserControl
    {
        private NumericUpDown rings;
        private NumericUpDown sectors;
        private NumericUpDown loopFraction;
        private NumericUpDown crossFraction;
        private CheckBox longPassages;

        public CircularMazeControl()
        {
            InitializeComponent();
        }

        public CircularMazeControl(bool enabled) : this()
        {
            IsEnabled = enabled;
        }

        public int MazeRings => (int)(rings.Value ?? 0);
        public int MazeSectors => (int)(sectors.Value ?? 0);
        public double LoopFraction => (double)(loopFraction.Value ?? 0) / 100.0;
        public double CrossFraction => (double)(crossFraction.Value ?? 0) / 100.0;
        public bool LongPassages => longPassages.IsChecked == true;

        public void SetReconstructionValues(int r, int s, double loopFrac, double crossFrac, bool longPass)
        {
            rings.Value = r;
            sectors.Value = s;
            loopFraction.Value = (decimal)(loopFrac * 100);
            crossFraction.Value = (decimal)(crossFrac * 100);
            longPassages.IsChecked = longPass;
        }

        private void InitializeComponent()
        {
            rings = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 200,
                Value = 0,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };

            sectors = new NumericUpDown
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
                            new TextBlock { Text = "Rings", VerticalAlignment = VerticalAlignment.Center }.WithGridColumn(0),
                            rings.WithGridColumn(1),
                        }
                    },
                    new Grid
                    {
                        ColumnDefinitions = ColumnDefinitions.Parse("80,*"),
                        Children =
                        {
                            new TextBlock { Text = "Sectors", VerticalAlignment = VerticalAlignment.Center }.WithGridColumn(0),
                            sectors.WithGridColumn(1),
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
