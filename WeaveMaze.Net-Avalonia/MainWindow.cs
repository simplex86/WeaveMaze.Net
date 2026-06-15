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
        private WeaveMazeScore mazeScore;

        // Data - Reconstruction page
        private WeaveMazeField? reconField;
        private WeaveMazeSolution reconSolution;
        private WeaveMazeGate[]? reconGates;
        private WeaveMazeScore reconScore;

        // Shared generators
        private WeaveMazeGenerator mazeGenerator = new();
        private WeaveMazeSolutionGenerator solutionGenerator = new();
        private WeaveMazeGateGenerator gateGenerator = new();

        // Controls - Generation page
        private ComboBox shape;
        private RectangularMazeControl rectangularMazeControl;
        private CustomizedMazeControl customizedMazeControl;
        private CircularMazeControl circularMazeControl;
        private Button generation;
        private CheckBox showRoundedCorners;
        private CheckBox showSolution;
        private MazeCanvas canvas;
        private Button save;

        // Score display controls - Generation page
        private TextBlock genPathEfficiencyScore;
        private TextBlock genStructuralComplexityScore;
        private TextBlock genExplorationDepthScore;
        private TextBlock genDecisionDensityScore;
        private TextBlock genDeadEndReasonabilityScore;
        private TextBlock genDeadEndDiversityScore;
        private TextBlock genBranchBalanceScore;
        private TextBlock genSolutionConcealmentScore;
        private TextBlock genTotalScore;

        // Controls - Reconstruction page
        private TextBox reconFileName;
        private Button reconBrowse;
        private ComboBox reconShape;
        private RectangularMazeControl reconRectangularMazeControl;
        private CustomizedMazeControl reconCustomizedMazeControl;
        private CheckBox reconShowRoundedCorners;
        private CheckBox reconShowSolution;
        private MazeCanvas reconCanvas;

        // Score display controls - Reconstruction page
        private TextBlock reconPathEfficiencyScore;
        private TextBlock reconStructuralComplexityScore;
        private TextBlock reconExplorationDepthScore;
        private TextBlock reconDecisionDensityScore;
        private TextBlock reconDeadEndReasonabilityScore;
        private TextBlock reconDeadEndDiversityScore;
        private TextBlock reconBranchBalanceScore;
        private TextBlock reconSolutionConcealmentScore;
        private TextBlock reconTotalScore;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            shape = new ComboBox
            {
                ItemsSource = new[] { "Rectangular", "Circular", "Customized" },
                SelectedIndex = 0,
                HorizontalAlignment = HorizontalAlignment.Stretch,
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

            circularMazeControl = new CircularMazeControl
            {
                IsVisible = false,
            };

            generation = new Button
            {
                Content = "Generate",
                Background = new SolidColorBrush(Color.FromRgb(0x87, 0xCE, 0xEB)),
                FontSize = 19,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Height = 60,
                Margin = new Thickness(2, 8, 2, 2),
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
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Background = new SolidColorBrush(Color.FromRgb(0x87, 0xCE, 0xEB)),
                Width = 80,
                Height = 40,
                Margin = new Thickness(0, 4, 8, 4),
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
                        ColumnDefinitions = ColumnDefinitions.Parse("80,*"),
                        Margin = new Thickness(0, 0, 0, 8),
                        Children =
                        {
                            new TextBlock { Text = "Shape", VerticalAlignment = VerticalAlignment.Center }.WithGridColumn(0),
                            shape.WithGridColumn(1),
                        }
                    },
                    rectangularMazeControl,
                    customizedMazeControl,
                    circularMazeControl,
                }
            };

            // --- Generation page: middle panel (canvas + checkboxes) ---
            var middlePanel = new Grid
            {
                RowDefinitions = RowDefinitions.Parse("*,Auto"),
                Children =
                {
                    canvas,
                    new Grid
                    {
                        ColumnDefinitions = ColumnDefinitions.Parse("*,Auto"),
                        VerticalAlignment = VerticalAlignment.Bottom,
                        Margin = new Thickness(8, 4),
                        Children =
                        {
                            new StackPanel
                            {
                                Orientation = Orientation.Horizontal,
                                Spacing = 24,
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

            // --- Generation page: right evaluation panel ---
            genPathEfficiencyScore = new TextBlock { Text = "0", VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };
            genStructuralComplexityScore = new TextBlock { Text = "0", VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };
            genExplorationDepthScore = new TextBlock { Text = "0", VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };
            genDecisionDensityScore = new TextBlock { Text = "0", VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };
            genDeadEndReasonabilityScore = new TextBlock { Text = "0", VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };
            genDeadEndDiversityScore = new TextBlock { Text = "0", VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };
            genBranchBalanceScore = new TextBlock { Text = "0", VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };
            genSolutionConcealmentScore = new TextBlock { Text = "0", VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };
            genTotalScore = new TextBlock { Text = "0", FontWeight = FontWeight.Bold, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };

            var genRightPanel = new Border
            {
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1, 0, 0, 0),
                Padding = new Thickness(6),
                Child = new StackPanel
                {
                    Children =
                    {
                        new TextBlock { Text = "质量评估报告", FontSize = 15, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 0, 0, 12), HorizontalAlignment = HorizontalAlignment.Center },
                        MakeScoreRow("结构复杂度", genStructuralComplexityScore),
                        MakeScoreRow("探索深度", genExplorationDepthScore),
                        MakeScoreRow("决策密度", genDecisionDensityScore),
                        MakeScoreRow("路径效率", genPathEfficiencyScore),
                        MakeScoreRow("路径合理性", genDeadEndReasonabilityScore),
                        MakeScoreRow("路径多样性", genDeadEndDiversityScore),
                        MakeScoreRow("岔路均衡度", genBranchBalanceScore),
                        MakeScoreRow("解的隐蔽性", genSolutionConcealmentScore),
                        new Border { BorderBrush = Brushes.LightGray, BorderThickness = new Thickness(0, 1, 0, 0), Margin = new Thickness(0, 8) },
                        MakeScoreRow("总得分", genTotalScore),
                    }
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

            // Generation page: left(290) + middle(*) + right(290), with bottom row for checkboxes
            var generationPage = new Grid
            {
                ColumnDefinitions = ColumnDefinitions.Parse("290,*,290"),
                RowDefinitions = RowDefinitions.Parse("*,Auto"),
                Children =
                {
                    leftBorder,
                    middlePanel.WithGridColumn(1),
                    genRightPanel.WithGridColumn(2).WithGridRow(0),
                }
            };

            // --- Reconstruction page ---
            reconFileName = new TextBox
            {
                IsReadOnly = true,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };

            reconBrowse = new Button
            {
                Content = "...",
                Background = new SolidColorBrush(Color.FromRgb(0x87, 0xCE, 0xEB)),
                VerticalAlignment = VerticalAlignment.Stretch,
                Margin = new Thickness(4, 0, 0, 0),
            };
            reconBrowse.Click += OnReconBrowseClickedHandler;

            reconShape = new ComboBox
            {
                ItemsSource = new[] { "Rectangular", "Customized" },
                SelectedIndex = 0,
                HorizontalAlignment = HorizontalAlignment.Stretch,
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
                ColumnDefinitions = ColumnDefinitions.Parse("80,*,Auto"),
                Margin = new Thickness(6, 0, 0, 4),
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
                        ColumnDefinitions = ColumnDefinitions.Parse("80,*"),
                        Margin = new Thickness(0, 0, 0, 8),
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

            // Middle panel (canvas + checkboxes)
            var reconMiddlePanel = new Grid
            {
                RowDefinitions = RowDefinitions.Parse("*,Auto"),
                Children =
                {
                    reconCanvas,
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 24,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Bottom,
                        Margin = new Thickness(8, 4),
                        Children =
                        {
                            reconShowRoundedCorners,
                            reconShowSolution,
                        }
                    }.WithGridRow(1),
                }
            };

            // Right evaluation panel - Reconstruction page
            reconPathEfficiencyScore = new TextBlock { Text = "0", VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };
            reconStructuralComplexityScore = new TextBlock { Text = "0", VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };
            reconExplorationDepthScore = new TextBlock { Text = "0", VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };
            reconDecisionDensityScore = new TextBlock { Text = "0", VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };
            reconDeadEndReasonabilityScore = new TextBlock { Text = "0", VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };
            reconDeadEndDiversityScore = new TextBlock { Text = "0", VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };
            reconBranchBalanceScore = new TextBlock { Text = "0", VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };
            reconSolutionConcealmentScore = new TextBlock { Text = "0", VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };
            reconTotalScore = new TextBlock { Text = "0", FontWeight = FontWeight.Bold, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };

            var reconRightPanel = new Border
            {
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1, 0, 0, 0),
                Padding = new Thickness(6),
                Child = new StackPanel
                {
                    Children =
                    {
                        new TextBlock { Text = "质量评估报告", FontSize = 15, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 0, 0, 12), HorizontalAlignment = HorizontalAlignment.Center },
                        MakeScoreRow("结构复杂度", reconStructuralComplexityScore),
                        MakeScoreRow("探索深度", reconExplorationDepthScore),
                        MakeScoreRow("决策密度", reconDecisionDensityScore),
                        MakeScoreRow("路径效率", reconPathEfficiencyScore),
                        MakeScoreRow("路径合理性", reconDeadEndReasonabilityScore),
                        MakeScoreRow("路径多样性", reconDeadEndDiversityScore),
                        MakeScoreRow("岔路均衡度", reconBranchBalanceScore),
                        MakeScoreRow("解的隐蔽性", reconSolutionConcealmentScore),
                        new Border { BorderBrush = Brushes.LightGray, BorderThickness = new Thickness(0, 1, 0, 0), Margin = new Thickness(0, 8) },
                        MakeScoreRow("总得分", reconTotalScore),
                    }
                }
            };

            // Below top bar: left(290) + middle(*) + right(290)
            var reconContent = new Grid
            {
                ColumnDefinitions = ColumnDefinitions.Parse("290,*,290"),
                RowDefinitions = RowDefinitions.Parse("*,Auto"),
                Children =
                {
                    reconLeftBorder,
                    reconMiddlePanel.WithGridColumn(1),
                    reconRightPanel.WithGridColumn(2).WithGridRow(0).WithGridRowSpan(2),
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
                Padding = new Thickness(2),
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

            Title = "SimplexLab-WeaveMaze v0.9.35";
            Width = 1400;
            Height = 780;
            MinWidth = 1400;
            MinHeight = 780;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Content = tabControl;
        }

        private static Grid MakeScoreRow(string label, TextBlock valueControl)
        {
            return new Grid
            {
                ColumnDefinitions = ColumnDefinitions.Parse("*,Auto"),
                Margin = new Thickness(0, 4),
                Children =
                {
                    new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center },
                    valueControl.WithGridColumn(1),
                }
            };
        }

        private static string FormatScore(double score) => score.ToString("F1");

        private static string FormatTotal(double total) => total.ToString("F1");

        private void UpdateGenScorePanel()
        {
            genPathEfficiencyScore.Text = FormatScore(mazeScore.PathEfficiencyScore);
            genStructuralComplexityScore.Text = FormatScore(mazeScore.StructuralComplexityScore);
            genExplorationDepthScore.Text = FormatScore(mazeScore.ExplorationDepthScore);
            genDecisionDensityScore.Text = FormatScore(mazeScore.DecisionDensityScore);
            genDeadEndReasonabilityScore.Text = FormatScore(mazeScore.DeadEndReasonabilityScore);
            genDeadEndDiversityScore.Text = FormatScore(mazeScore.DeadEndDiversityScore);
            genBranchBalanceScore.Text = FormatScore(mazeScore.BranchBalanceScore);
            genSolutionConcealmentScore.Text = FormatScore(mazeScore.SolutionConcealmentScore);
            genTotalScore.Text = FormatTotal(mazeScore.TotalScore);
        }

        private void UpdateReconScorePanel()
        {
            reconPathEfficiencyScore.Text = FormatScore(reconScore.PathEfficiencyScore);
            reconStructuralComplexityScore.Text = FormatScore(reconScore.StructuralComplexityScore);
            reconExplorationDepthScore.Text = FormatScore(reconScore.ExplorationDepthScore);
            reconDecisionDensityScore.Text = FormatScore(reconScore.DecisionDensityScore);
            reconDeadEndReasonabilityScore.Text = FormatScore(reconScore.DeadEndReasonabilityScore);
            reconDeadEndDiversityScore.Text = FormatScore(reconScore.DeadEndDiversityScore);
            reconBranchBalanceScore.Text = FormatScore(reconScore.BranchBalanceScore);
            reconSolutionConcealmentScore.Text = FormatScore(reconScore.SolutionConcealmentScore);
            reconTotalScore.Text = FormatTotal(reconScore.TotalScore);
        }

        #region Handler

        private void OnMaskChangedHandler(object? sender, SelectionChangedEventArgs e)
        {
            rectangularMazeControl.IsVisible = shape.SelectedIndex == (int)EWeaveMazeShape.Rectangular;
            circularMazeControl.IsVisible = shape.SelectedIndex == (int)EWeaveMazeShape.Circular;
            customizedMazeControl.IsVisible = shape.SelectedIndex == (int)EWeaveMazeShape.Customized;
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
                await EvaluateMaze();
            }
            PostProcess();
        }

        private void PrevProcess()
        {
            shape.IsEnabled = false;
            rectangularMazeControl.IsEnabled = false;
            customizedMazeControl.IsEnabled = false;
            circularMazeControl.IsEnabled = false;
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
                case EWeaveMazeShape.Circular:
                    await GenerateCircularWeaveMaze();
                    break;
            }

            if (mazeField != null)
            {
                mazeSolution = solutionGenerator.Generate(mazeField);
                mazeGates = gateGenerator.Generate(mazeField, mazeSolution);
            }
        }

        private async Task EvaluateMaze()
        {
            if (mazeField != null && mazeGates != null)
            {
                mazeScore = await WeaveMazeScoreEvaluator.EvaluateAsync(mazeField, mazeGates, mazeSolution);
                UpdateGenScorePanel();
            }
        }

        private void PostProcess()
        {
            shape.IsEnabled = true;
            rectangularMazeControl.IsEnabled = true;
            customizedMazeControl.IsEnabled = true;
            circularMazeControl.IsEnabled = true;
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

            // Evaluate reconstruction maze
            if (reconGates != null)
            {
                reconScore = WeaveMazeScoreEvaluator.Evaluate(reconField, reconGates, reconSolution);
                UpdateReconScorePanel();
            }

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

        private async Task GenerateCircularWeaveMaze()
        {
            var r = circularMazeControl.MazeRings;
            var s = circularMazeControl.MazeSectors;
            var loopFrac = circularMazeControl.LoopFraction;
            var crossFrac = circularMazeControl.CrossFraction;
            var longPassages = circularMazeControl.LongPassages;
            var minInnerArcFrac = circularMazeControl.MinInnerArcFrac;

            if (r <= 0) r = CircularWeaveMazeField.DefaultRings;
            if (s <= 0) s = CircularWeaveMazeField.DefaultSectors;

            var field = new CircularWeaveMazeField(r, s, loopFrac, crossFrac, longPassages, minInnerArcFrac);
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

        public static T WithGridRowSpan<T>(this T control, int span) where T : Control
        {
            Grid.SetRowSpan(control, span);
            return control;
        }
    }
}
