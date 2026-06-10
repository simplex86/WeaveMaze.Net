using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Platform.Storage;

namespace SimplexLab.WeaveMaze.TApplication
{
    public partial class RectangularMazeMaskControl : UserControl
    {
        private NumericUpDown loopFraction;
        private NumericUpDown crossFraction;
        private CheckBox longPassages;
        private TextBox filename;
        private Button brower;

        public RectangularMazeMaskControl()
        {
            InitializeComponent();
        }

        public string? FileName { get; private set; }
        public double LoopFraction => (double)(loopFraction.Value ?? 0) / 100.0;
        public double CrossFraction => (double)(crossFraction.Value ?? 0) / 100.0;
        public bool LongPassages => longPassages.IsChecked == true;

        private void InitializeComponent()
        {
            loopFraction = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 100,
                Value = 5,
                Increment = 1,
                FormatString = "0",
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(5, 0, 0, 0),
            };

            crossFraction = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 100,
                Value = 25,
                Increment = 1,
                FormatString = "0",
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(5, 0, 0, 0),
            };

            longPassages = new CheckBox
            {
                Content = "Long Passages",
            };

            filename = new TextBox
            {
                IsReadOnly = true,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(5, 0, 0, 0),
            };

            brower = new Button
            {
                Content = "...",
                Width = 23,
                VerticalAlignment = VerticalAlignment.Stretch,
            };
            brower.Click += OnBrowerClickedHandler;

            var panel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 6,
                Children =
                {
                    new Grid
                    {
                        ColumnDefinitions = ColumnDefinitions.Parse("95,*,Auto"),
                        Children =
                        {
                            new TextBlock { Text = "Mask Path", VerticalAlignment = VerticalAlignment.Center }.WithGridColumn(0),
                            filename.WithGridColumn(1),
                            brower.WithGridColumn(2),
                        }
                    },
                    new Grid
                    {
                        ColumnDefinitions = ColumnDefinitions.Parse("95,*"),
                        Children =
                        {
                            new TextBlock { Text = "Loop  Fraction", VerticalAlignment = VerticalAlignment.Center }.WithGridColumn(0),
                            loopFraction.WithGridColumn(1),
                        }
                    },
                    new Grid
                    {
                        ColumnDefinitions = ColumnDefinitions.Parse("95,*"),
                        Children =
                        {
                            new TextBlock { Text = "Cross Fraction", VerticalAlignment = VerticalAlignment.Center }.WithGridColumn(0),
                            crossFraction.WithGridColumn(1),
                        }
                    },
                    longPassages,
                }
            };

            Content = panel;
        }

        private async void OnBrowerClickedHandler(object? sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var storageProvider = topLevel.StorageProvider;
            var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select Mask File",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Image Files")
                    {
                        Patterns = new[] { "*.bmp", "*.png", "*.jpg" }
                    }
                }
            });

            if (files.Count > 0)
            {
                FileName = files[0].Path.LocalPath;
                filename.Text = FileName;
            }
        }
    }
}
