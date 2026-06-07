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
            rectangularMazeControl = new RectangularMazeControl();
            generation = new Button();
            showRoundedCorners = new CheckBox();
            showSolution = new CheckBox();
            canvas = new PictureBox();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)canvas).BeginInit();
            SuspendLayout();
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.FixedPanel = FixedPanel.Panel1;
            splitContainer1.Location = new Point(0, 0);
            splitContainer1.Margin = new Padding(3, 2, 3, 2);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(rectangularMazeControl);
            splitContainer1.Panel1.Controls.Add(generation);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(showRoundedCorners);
            splitContainer1.Panel2.Controls.Add(showSolution);
            splitContainer1.Panel2.Controls.Add(canvas);
            splitContainer1.Size = new Size(964, 620);
            splitContainer1.SplitterDistance = 292;
            splitContainer1.TabIndex = 0;
            // 
            // rectangularMazeControl
            // 
            rectangularMazeControl.Location = new Point(4, 4);
            rectangularMazeControl.Margin = new Padding(5);
            rectangularMazeControl.Name = "rectangularMazeControl";
            rectangularMazeControl.Size = new Size(284, 265);
            rectangularMazeControl.TabIndex = 2;
            // 
            // generation
            // 
            generation.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            generation.BackColor = Color.LightSkyBlue;
            generation.Font = new Font("Microsoft YaHei UI", 16F, FontStyle.Bold, GraphicsUnit.Point, 134);
            generation.Location = new Point(3, 533);
            generation.Margin = new Padding(3, 2, 3, 2);
            generation.Name = "generation";
            generation.Size = new Size(287, 85);
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
            showRoundedCorners.Location = new Point(0, 592);
            showRoundedCorners.Margin = new Padding(4);
            showRoundedCorners.Name = "showRoundedCorners";
            showRoundedCorners.Size = new Size(222, 24);
            showRoundedCorners.TabIndex = 2;
            showRoundedCorners.Text = "Show as Rounded Corners";
            showRoundedCorners.UseVisualStyleBackColor = true;
            showRoundedCorners.CheckedChanged += OnShowRoundedCornersChanged;
            // 
            // showSolution
            // 
            showSolution.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            showSolution.AutoSize = true;
            showSolution.Location = new Point(269, 592);
            showSolution.Margin = new Padding(4);
            showSolution.Name = "showSolution";
            showSolution.Size = new Size(164, 24);
            showSolution.TabIndex = 1;
            showSolution.Text = "Show the Solution";
            showSolution.UseVisualStyleBackColor = true;
            showSolution.CheckedChanged += OnShowSolutionChanged;
            // 
            // canvas
            // 
            canvas.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            canvas.BackColor = Color.White;
            canvas.Location = new Point(3, 4);
            canvas.Margin = new Padding(3, 2, 3, 2);
            canvas.Name = "canvas";
            canvas.Size = new Size(664, 582);
            canvas.TabIndex = 0;
            canvas.TabStop = false;
            canvas.Paint += OnCanvasPaintHandler;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(9F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(964, 620);
            Controls.Add(splitContainer1);
            Margin = new Padding(3, 2, 3, 2);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Weave Maze Generator v0.1.8";
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
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
    }
}
