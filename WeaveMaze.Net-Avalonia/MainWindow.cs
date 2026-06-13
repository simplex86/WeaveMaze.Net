using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;

namespace SimplexLab.WeaveMaze.TApplication
{
    public partial class MainWindow : Window
    {
        // Data - Generation page
        private WeaveMazeField? mazeField;
        private WeaveMazeSolution mazeSolution;
        private WeaveMazeGate[]? mazeGates;

        // Data - Reconstruction page
        private WeaveMazeField? reconField;
        private WeaveMazeSolution reconSolution;
        private WeaveMazeGate[]? reconGates;

        // Shared generators
        private WeaveMazeGenerator mazeGenerator = new();
        private WeaveMazeSolutionGenerator solutionGenerator = new();
        private WeaveMazeGateGenerator gateGenerator = new();

        // Controls - Generation page
        private ComboBox shape;
        private RectangularMazeControl rectangularMazeControl;
        private CustomizedMazeControl customizedMazeControl;
        private Button generation;
        private CheckBox showRoundedCorners;
        private CheckBox showSolution;
        private MazeCanvas canvas;
        private Button save;

        // Controls - Reconstruction page
        private TextBox reconFileName;
        private Button reconBrowse;
        private ComboBox reconShape;
        private RectangularMazeControl reconRectangularMazeControl;
        private CustomizedMazeControl reconCustomizedMazeControl;
        private CheckBox reconShowRoundedCorners;
        private CheckBox reconShowSolution;
        private MazeCanvas reconCanvas;

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

            save = new Button
            {
                Content = "Save",
                VerticalAlignment = VerticalAlignment.Center,
                Padding = new Thickness(16, 8),
                Background = Brushes.DodgerBlue,
                Foreground = Brushes.White,
            };
            save.Click += OnSaveClickedHandler;

            // --- Generation page: left panel ---
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

            // --- Generation page: right panel ---
            var rightPanel = new Grid
            {
                RowDefinitions = RowDefinitions.Parse("*,Auto"),
                Children =
                {
                    canvas,
                    new Grid
                    {
                        ColumnDefinitions = ColumnDefinitions.Parse("*,Auto"),
                        VerticalAlignment = VerticalAlignment.Bottom,
                        Margin = new Thickness(4),
                        Children =
                        {
                            new StackPanel
                            {
                                Orientation = Orientation.Horizontal,
                                Spacing = 16,
                                Children =
                                {
                                    showRoundedCorners,
                                    showSolution,
                                }
                            },
                            save.WithGridColumn(1),
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

            // Generation page: fixed left + flexible right
            var generationPage = new Grid
            {
                ColumnDefinitions = ColumnDefinitions.Parse("300,*"),
                Children =
                {
                    leftBorder,
                    rightPanel.WithGridColumn(1),
                }
            };

            // --- Reconstruction page ---
            reconFileName = new TextBox
            {
                IsReadOnly = true,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(5, 0, 0, 0),
            };

            reconBrowse = new Button
            {
                Content = "...",
                Width = 36,
                Height = 36,
                Background = Brushes.DodgerBlue,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            reconBrowse.Click += OnReconBrowseClickedHandler;

            reconShape = new ComboBox
            {
                ItemsSource = new[] { "Rectangular", "Customized" },
                SelectedIndex = 0,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(5, 0, 0, 0),
                IsEnabled = false,
            };

            reconRectangularMazeControl = new RectangularMazeControl
            {
                IsVisible = true,
                IsEnabled = false,
            };

            reconCustomizedMazeControl = new CustomizedMazeControl(false)
            {
                IsVisible = false,
                IsEnabled = false,
            };

            reconShowRoundedCorners = new CheckBox
            {
                Content = "Show as Rounded Corners",
                IsChecked = true,
            };
            reconShowRoundedCorners.IsCheckedChanged += (s, e) => reconCanvas.InvalidateVisual();

            reconShowSolution = new CheckBox
            {
                Content = "Show the Solution",
                IsChecked = false,
            };
            reconShowSolution.IsCheckedChanged += (s, e) => reconCanvas.InvalidateVisual();

            reconCanvas = new MazeCanvas
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
            };
            reconCanvas.RenderRequested += OnReconCanvasRender;

            // Top: file selector
            var reconTopBar = new Grid
            {
                ColumnDefinitions = ColumnDefinitions.Parse("95,*,Auto"),
                Margin = new Thickness(4),
                Children =
                {
                    new TextBlock { Text = "Selection", VerticalAlignment = VerticalAlignment.Center }.WithGridColumn(0),
                    reconFileName.WithGridColumn(1),
                    reconBrowse.WithGridColumn(2),
                }
            };

            // Left panel (read-only parameters)
            var reconLeftPanel = new StackPanel
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
                            reconShape.WithGridColumn(1),
                        }
                    },
                    reconRectangularMazeControl,
                    reconCustomizedMazeControl,
                }
            };

            // Left panel wrapped in Border with right edge separator
            var reconLeftBorder = new Border
            {
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(0, 0, 1, 0),
                Padding = new Thickness(6),
                Child = new ScrollViewer
                {
                    Content = reconLeftPanel,
                },
            };

            // Right panel (canvas + checkboxes)
            var reconRightPanel = new Grid
            {
                RowDefinitions = RowDefinitions.Parse("*,Auto"),
                Children =
                {
                    reconCanvas,
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 16,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Bottom,
                        Margin = new Thickness(4),
                        Children =
                        {
                            reconShowRoundedCorners,
                            reconShowSolution,
                        }
                    }.WithGridRow(1),
                }
            };

            // Below top bar: fixed left + flexible right
            var reconContent = new Grid
            {
                ColumnDefinitions = ColumnDefinitions.Parse("300,*"),
                Children =
                {
                    reconLeftBorder,
                    reconRightPanel.WithGridColumn(1),
                }
            };

            // Reconstruction page layout
            var reconstructionPage = new Grid
            {
                RowDefinitions = RowDefinitions.Parse("Auto,*"),
                Children =
                {
                    reconTopBar.WithGridRow(0),
                    reconContent.WithGridRow(1),
                }
            };

            // --- TabControl ---
            var tabControl = new TabControl
            {
                Items =
                {
                    new TabItem
                    {
                        Header = "Generation",
                        Content = generationPage,
                    },
                    new TabItem
                    {
                        Header = "Reconstruction",
                        Content = reconstructionPage,
                    },
                }
            };

            Title = "SimplexLab-WeaveMaze v0.8.28";
            Width = 1200;
            Height = 800;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Content = tabControl;
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

        private async void OnSaveClickedHandler(object? sender, RoutedEventArgs e)
        {
            if (mazeField == null) return;

            var storageProvider = TopLevel.GetTopLevel(this)?.StorageProvider;
            if (storageProvider == null) return;

            var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save Maze",
                DefaultExtension = "waze",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("Weave Maze")
                    {
                        Patterns = new[] { "*.waze" }
                    }
                }
            });

            if (file == null) return;

            var gates = mazeGates ?? Array.Empty<WeaveMazeGate>();
            WeaveMazeWriter.Write(mazeField, gates, mazeSolution, file.Path.LocalPath);
        }

        private async void OnReconBrowseClickedHandler(object? sender, RoutedEventArgs e)
        {
            var storageProvider = TopLevel.GetTopLevel(this)?.StorageProvider;
            if (storageProvider == null) return;

            var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open Maze File",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Weave Maze")
                    {
                        Patterns = new[] { "*.waze" }
                    }
                }
            });

            if (files.Count == 0) return;

            var filePath = files[0].Path.LocalPath;
            reconFileName.Text = filePath;

            var field = WeaveMazeReader.ReadField(filePath);
            var gates = WeaveMazeReader.ReadGates(filePath);

            reconField = field;
            reconGates = gates;
            reconSolution = solutionGenerator.Generate(reconField);

            // Update left panel to reflect loaded maze parameters
            bool isCustomized = field is CustomizedWeaveMazeField;
            reconShape.SelectedIndex = isCustomized ? 1 : 0;
            reconRectangularMazeControl.IsVisible = !isCustomized;
            reconCustomizedMazeControl.IsVisible = isCustomized;

            if (isCustomized)
            {
                reconCustomizedMazeControl.SetReconstructionValues(field.Width, field.Height, field.LoopFrac, field.CrossFrac, field.LongPassages);
            }
            else
            {
                reconRectangularMazeControl.SetReconstructionValues(field.Width, field.Height, field.LoopFrac, field.CrossFrac, field.LongPassages);
            }

            reconCanvas.InvalidateVisual();
        }

        private void OnReconCanvasRender(DrawingContext context)
        {
            if (reconField == null) return;

            using var gc = new GraphicsContext(context);
            var renderer = new WeaveMazeRenderer();
            renderer.SetSize((int)reconCanvas.Bounds.Width, (int)reconCanvas.Bounds.Height)
                    .SetField(reconField)
                    .SetGates(reconGates)
                    .SetRoundedCorners(reconShowRoundedCorners.IsChecked == true)
                    .Draw(gc);

            if (reconShowSolution.IsChecked == true)
            {
                using var gc2 = new GraphicsContext(context);
                var solRenderer = new WeaveMazeSolutionRenderer();
                solRenderer.SetSize((int)reconCanvas.Bounds.Width, (int)reconCanvas.Bounds.Height)
                           .SetField(reconField)
                           .SetSolution(reconSolution)
                           .SetGates(reconGates)
                           .SetRoundedCorners(reconShowRoundedCorners.IsChecked == true)
                           .Draw(gc2);
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
