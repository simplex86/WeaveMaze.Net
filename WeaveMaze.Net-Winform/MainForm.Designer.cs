using System;
using System.Drawing;
using System.Windows.Forms;

namespace SimplexLab.WeaveMaze.TApplication
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            splitContainer1 = new SplitContainer();
            shape = new ComboBox();
            label1 = new Label();
            flowLayoutPanel1 = new FlowLayoutPanel();
            rectangularMazeControl = new RectangularMazeControl();
            customizedMazeControl = new CustomizedMazeControl();
            generation = new Button();
            showRoundedCorners = new CheckBox();
            showSolution = new CheckBox();
            canvas = new PictureBox();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            flowLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)canvas).BeginInit();
            SuspendLayout();
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.FixedPanel = FixedPanel.Panel1;
            splitContainer1.Location = new Point(0, 0);
            splitContainer1.Margin = new Padding(5, 1, 5, 1);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(shape);
            splitContainer1.Panel1.Controls.Add(label1);
            splitContainer1.Panel1.Controls.Add(flowLayoutPanel1);
            splitContainer1.Panel1.Controls.Add(generation);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(showRoundedCorners);
            splitContainer1.Panel2.Controls.Add(showSolution);
            splitContainer1.Panel2.Controls.Add(canvas);
            splitContainer1.Size = new Size(1232, 792);
            splitContainer1.SplitterDistance = 357;
            splitContainer1.SplitterWidth = 5;
            splitContainer1.TabIndex = 0;
            // 
            // shape
            // 
            shape.DropDownStyle = ComboBoxStyle.DropDownList;
            shape.FormattingEnabled = true;
            shape.Items.AddRange(new object[] { "Rectangular", "Customized" });
            shape.Location = new Point(163, 4);
            shape.Margin = new Padding(5, 4, 5, 4);
            shape.Name = "shape";
            shape.Size = new Size(188, 32);
            shape.TabIndex = 5;
            shape.SelectedIndexChanged += OnMaskChangedHandler;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(11, 8);
            label1.Margin = new Padding(5, 0, 5, 0);
            label1.Name = "label1";
            label1.Size = new Size(63, 24);
            label1.TabIndex = 4;
            label1.Text = "Shape";
            // 
            // flowLayoutPanel1
            // 
            flowLayoutPanel1.Controls.Add(rectangularMazeControl);
            flowLayoutPanel1.Controls.Add(customizedMazeControl);
            flowLayoutPanel1.Location = new Point(0, 37);
            flowLayoutPanel1.Margin = new Padding(5, 4, 5, 4);
            flowLayoutPanel1.Name = "flowLayoutPanel1";
            flowLayoutPanel1.Size = new Size(357, 607);
            flowLayoutPanel1.TabIndex = 3;
            // 
            // rectangularMazeControl
            // 
            rectangularMazeControl.Location = new Point(6, 6);
            rectangularMazeControl.Margin = new Padding(6, 6, 6, 6);
            rectangularMazeControl.Name = "rectangularMazeControl";
            rectangularMazeControl.Size = new Size(347, 219);
            rectangularMazeControl.TabIndex = 2;
            // 
            // rectangularMazeMaskControl
            // 
            customizedMazeControl.Location = new Point(8, 237);
            customizedMazeControl.Margin = new Padding(8, 6, 8, 6);
            customizedMazeControl.Name = "rectangularMazeMaskControl";
            customizedMazeControl.Size = new Size(352, 171);
            customizedMazeControl.TabIndex = 3;
            // 
            // generation
            // 
            generation.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            generation.BackColor = Color.LightSkyBlue;
            generation.Font = new Font("Microsoft YaHei UI", 16F, FontStyle.Bold, GraphicsUnit.Point, 134);
            generation.Location = new Point(5, 688);
            generation.Margin = new Padding(5, 1, 5, 1);
            generation.Name = "generation";
            generation.Size = new Size(350, 102);
            generation.TabIndex = 0;
            generation.Text = "Generate";
            generation.UseVisualStyleBackColor = false;
            generation.Click += OnGeneratoinClickedHandler;
            // 
            // showRoundedCorners
            // 
            showRoundedCorners.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            showRoundedCorners.AutoSize = true;
            showRoundedCorners.Checked = true;
            showRoundedCorners.CheckState = CheckState.Checked;
            showRoundedCorners.Location = new Point(0, 760);
            showRoundedCorners.Margin = new Padding(5, 6, 5, 6);
            showRoundedCorners.Name = "showRoundedCorners";
            showRoundedCorners.Size = new Size(260, 28);
            showRoundedCorners.TabIndex = 2;
            showRoundedCorners.Text = "Show as Rounded Corners";
            showRoundedCorners.UseVisualStyleBackColor = true;
            showRoundedCorners.CheckedChanged += OnShowRoundedCornersChanged;
            // 
            // showSolution
            // 
            showSolution.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            showSolution.AutoSize = true;
            showSolution.Location = new Point(328, 760);
            showSolution.Margin = new Padding(5, 6, 5, 6);
            showSolution.Name = "showSolution";
            showSolution.Size = new Size(191, 28);
            showSolution.TabIndex = 1;
            showSolution.Text = "Show the Solution";
            showSolution.UseVisualStyleBackColor = true;
            showSolution.CheckedChanged += OnShowSolutionChanged;
            // 
            // canvas
            // 
            canvas.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            canvas.BackColor = Color.White;
            canvas.Location = new Point(5, 6);
            canvas.Margin = new Padding(5, 1, 5, 1);
            canvas.Name = "canvas";
            canvas.Size = new Size(865, 745);
            canvas.TabIndex = 0;
            canvas.TabStop = false;
            canvas.Paint += OnCanvasPaintHandler;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(11F, 24F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1232, 792);
            Controls.Add(splitContainer1);
            Margin = new Padding(5, 1, 5, 1);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Weave Maze Generator v0.4.16";
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel1.PerformLayout();
            splitContainer1.Panel2.ResumeLayout(false);
            splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            flowLayoutPanel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)canvas).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private SplitContainer splitContainer1;
        private Button generation;
        private PictureBox canvas;
        private RectangularMazeControl rectangularMazeControl;
        private CheckBox showSolution;
        private CheckBox showRoundedCorners;
        private FlowLayoutPanel flowLayoutPanel1;
        private ComboBox shape;
        private Label label1;
        private CustomizedMazeControl customizedMazeControl;
    }
}
