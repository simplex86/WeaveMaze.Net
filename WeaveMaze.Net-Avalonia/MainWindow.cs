using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace SimplexLab.WeaveMaze.TApplication
{
    public partial class MainWindow : Window
    {
        private WeaveMazeField? mazeField;
        private WeaveMazeGenerator mazeGenerator = new();
        private WeaveMazeSolution mazeSolution;
        private WeaveMazeSolutionGenerator solutionGenerator = new();
        private WeaveMazeGateGenerator gateGenerator = new();
        private WeaveMazeGate[]? mazeGates;

        // Controls
        private ComboBox shape;
        private RectangularMazeControl rectangularMazeControl;
        private CustomizedMazeControl customizedMazeControl;
        private Button generation;
        private CheckBox showRoundedCorners;
        private CheckBox showSolution;
        private MazeCanvas canvas;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            shape = new ComboBox
            {
                ItemsSource = new[] { "Rectangular", "Customized" },
                SelectedIndex = 0,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(5, 0, 0, 0),
            };
            shape.SelectionChanged += OnMaskChangedHandler;

            rectangularMazeControl = new RectangularMazeControl
            {
                IsVisible = true,
            };

            customizedMazeControl = new CustomizedMazeControl
            {
                IsVisible = false,
            };

            generation = new Button
            {
                Content = "Generate",
                Background = Brushes.LightSkyBlue,
                FontWeight = FontWeight.Bold,
                FontSize = 16,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Height = 70,
                Margin = new Thickness(3, 0, 3, 3),
            };
            generation.Click += OnGeneratoinClickedHandler;

            showRoundedCorners = new CheckBox
            {
                Content = "Show as Rounded Corners",
                IsChecked = true,
            };
            showRoundedCorners.IsCheckedChanged += (s, e) => canvas.InvalidateVisual();

            showSolution = new CheckBox
            {
                Content = "Show the Solution",
                IsChecked = false,
            };
            showSolution.IsCheckedChanged += (s, e) => canvas.InvalidateVisual();

            canvas = new MazeCanvas
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
            };
            canvas.RenderRequested += OnCanvasRender;

            // Left panel
            var leftPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 6,
                Margin = new Thickness(0),
                Children =
                {
                    new Grid
                    {
                        ColumnDefinitions = ColumnDefinitions.Parse("95,*"),
                        Children =
                        {
                            new TextBlock { Text = "Shape", VerticalAlignment = VerticalAlignment.Center }.WithGridColumn(0),
                            shape.WithGridColumn(1),
                        }
                    },
                    rectangularMazeControl,
                    customizedMazeControl,
                }
            };

            // Right panel
            var rightPanel = new Grid
            {
                RowDefinitions = RowDefinitions.Parse("*,Auto"),
                Children =
                {
                    canvas,
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 16,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Bottom,
                        Margin = new Thickness(4),
                        Children =
                        {
                            showRoundedCorners,
                            showSolution,
                        }
                    }.WithGridRow(1),
                }
            };

            // Generate button at bottom of left panel
            var leftWithButton = new Grid
            {
                RowDefinitions = RowDefinitions.Parse("*,Auto"),
                Children =
                {
                    new ScrollViewer
                    {
                        Content = leftPanel,
                    }.WithGridRow(0),
                    generation.WithGridRow(1),
                }
            };

            // Left panel wrapped in Border with right edge separator
            var leftBorder = new Border
            {
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(0, 0, 1, 0),
                Padding = new Thickness(6),
                Child = leftWithButton,
            };

            // Main layout: fixed left + flexible right
            var mainGrid = new Grid
            {
                ColumnDefinitions = ColumnDefinitions.Parse("300,*"),
                Children =
                {
                    leftBorder,
                    rightPanel.WithGridColumn(1),
                }
            };

            Title = "Weave Maze Generator v0.5.21";
            Width = 1200;
            Height = 800;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Content = mainGrid;
        }

        #region Handler

        private void OnMaskChangedHandler(object? sender, SelectionChangedEventArgs e)
        {
            rectangularMazeControl.IsVisible = shape.SelectedIndex == 0;
            customizedMazeControl.IsVisible = shape.SelectedIndex == 1;
        }

        private async void OnGeneratoinClickedHandler(object? sender, RoutedEventArgs e)
        {
            await OnGeneratoinClickedHandler();
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
            shape.IsEnabled = false;
            rectangularMazeControl.IsEnabled = false;
            customizedMazeControl.IsEnabled = false;
            generation.IsEnabled = false;
            showRoundedCorners.IsEnabled = false;
            showSolution.IsEnabled = false;
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

            if (mazeField != null)
            {
                mazeSolution = solutionGenerator.Generate(mazeField);
                mazeGates = gateGenerator.Generate(mazeField, mazeSolution);
            }
        }

        private void PostProcess()
        {
            shape.IsEnabled = true;
            rectangularMazeControl.IsEnabled = true;
            customizedMazeControl.IsEnabled = true;
            generation.IsEnabled = true;
            showRoundedCorners.IsEnabled = true;
            showSolution.IsEnabled = true;

            canvas.InvalidateVisual();
        }

        private void OnCanvasRender(DrawingContext context)
        {
            if (mazeField != null)
            {
                DrawWeaveMaze(context);
                DrawWeaveMazeSolution(context);
            }
        }

        #endregion

        #region Rectangular

        private async Task GenerateRectangularWeaveMaze()
        {
            var w = rectangularMazeControl.MazeWidth;
            var h = rectangularMazeControl.MazeHeight;
            var loopFrac = rectangularMazeControl.LoopFraction;
            var crossFrac = rectangularMazeControl.CrossFraction;
            var longPassages = rectangularMazeControl.LongPassages;

            if (w <= 0) w = (int)canvas.Bounds.Width / 30;
            if (h <= 0) h = (int)canvas.Bounds.Height / 30;

            var field = new RectangularWeaveMazeField(w, h, loopFrac, crossFrac, longPassages);
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

        private void DrawWeaveMaze(DrawingContext context)
        {
            using var gc = new GraphicsContext(context);
            var renderer = new WeaveMazeRenderer();
            renderer.SetSize((int)canvas.Bounds.Width, (int)canvas.Bounds.Height)
                    .SetField(mazeField!)
                    .SetGates(mazeGates)
                    .SetRoundedCorners(showRoundedCorners.IsChecked == true)
                    .Draw(gc);
        }

        private void DrawWeaveMazeSolution(DrawingContext context)
        {
            if (showSolution.IsChecked != true) return;

            using var gc = new GraphicsContext(context);
            var renderer = new WeaveMazeSolutionRenderer();
            renderer.SetSize((int)canvas.Bounds.Width, (int)canvas.Bounds.Height)
                    .SetField(mazeField!)
                    .SetSolution(mazeSolution)
                    .SetGates(mazeGates)
                    .SetRoundedCorners(showRoundedCorners.IsChecked == true)
                    .Draw(gc);
        }

        #endregion
    }

    /// <summary>
    /// 自定义画布控件，支持自定义渲染
    /// </summary>
    internal class MazeCanvas : Control
    {
        public event Action<DrawingContext>? RenderRequested;

        public override void Render(DrawingContext context)
        {
            base.Render(context);
            // 绘制白色背景
            context.DrawRectangle(Brushes.White, null, new Rect(0, 0, Bounds.Width, Bounds.Height));
            RenderRequested?.Invoke(context);
        }
    }

    /// <summary>
    /// Grid 行列扩展辅助
    /// </summary>
    internal static class ControlExtensions
    {
        public static T WithGridRow<T>(this T control, int row) where T : Control
        {
            Grid.SetRow(control, row);
            return control;
        }

        public static T WithGridColumn<T>(this T control, int column) where T : Control
        {
            Grid.SetColumn(control, column);
            return control;
        }
    }
}
