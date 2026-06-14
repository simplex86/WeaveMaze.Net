namespace SimplexLab.WeaveMaze.TApplication
{
    partial class RectangularMazeControl
    {
        /// <summary> 
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region 组件设计器生成的代码

        /// <summary> 
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            label1 = new System.Windows.Forms.Label();
            width = new System.Windows.Forms.NumericUpDown();
            height = new System.Windows.Forms.NumericUpDown();
            label2 = new System.Windows.Forms.Label();
            loopFraction = new System.Windows.Forms.NumericUpDown();
            label3 = new System.Windows.Forms.Label();
            crossFraction = new System.Windows.Forms.NumericUpDown();
            label4 = new System.Windows.Forms.Label();
            longPassages = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)width).BeginInit();
            ((System.ComponentModel.ISupportInitialize)height).BeginInit();
            ((System.ComponentModel.ISupportInitialize)loopFraction).BeginInit();
            ((System.ComponentModel.ISupportInitialize)crossFraction).BeginInit();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(3, 5);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(42, 17);
            label1.TabIndex = 0;
            label1.Text = "Width";
            // 
            // width
            // 
            width.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            width.Location = new System.Drawing.Point(100, 3);
            width.Name = "width";
            width.Size = new System.Drawing.Size(160, 23);
            width.TabIndex = 1;
            // 
            // height
            // 
            height.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            height.Location = new System.Drawing.Point(100, 32);
            height.Name = "height";
            height.Size = new System.Drawing.Size(160, 23);
            height.TabIndex = 3;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(3, 34);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(46, 17);
            label2.TabIndex = 2;
            label2.Text = "Height";
            // 
            // loopFraction
            // 
            loopFraction.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            loopFraction.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
            loopFraction.Location = new System.Drawing.Point(100, 61);
            loopFraction.Name = "loopFraction";
            loopFraction.Size = new System.Drawing.Size(160, 23);
            loopFraction.TabIndex = 5;
            loopFraction.Value = new decimal(new int[] { 5, 0, 0, 0 });
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(3, 63);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(92, 17);
            label3.TabIndex = 4;
            label3.Text = "Loop";
            // 
            // crossFraction
            // 
            crossFraction.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            crossFraction.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
            crossFraction.Location = new System.Drawing.Point(100, 90);
            crossFraction.Name = "crossFraction";
            crossFraction.Size = new System.Drawing.Size(160, 23);
            crossFraction.TabIndex = 7;
            crossFraction.Value = new decimal(new int[] { 25, 0, 0, 0 });
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new System.Drawing.Point(3, 92);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(91, 17);
            label4.TabIndex = 6;
            label4.Text = "Cross";
            // 
            // longPassages
            // 
            longPassages.AutoSize = true;
            longPassages.Location = new System.Drawing.Point(3, 119);
            longPassages.Name = "longPassages";
            longPassages.Size = new System.Drawing.Size(114, 21);
            longPassages.TabIndex = 9;
            longPassages.Text = "Long Passages";
            longPassages.UseVisualStyleBackColor = true;
            // 
            // RectangularMazeControl
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(longPassages);
            Controls.Add(crossFraction);
            Controls.Add(label4);
            Controls.Add(loopFraction);
            Controls.Add(label3);
            Controls.Add(height);
            Controls.Add(label2);
            Controls.Add(width);
            Controls.Add(label1);
            Name = "RectangularMazeControl";
            Size = new System.Drawing.Size(260, 150);
            ((System.ComponentModel.ISupportInitialize)width).EndInit();
            ((System.ComponentModel.ISupportInitialize)height).EndInit();
            ((System.ComponentModel.ISupportInitialize)loopFraction).EndInit();
            ((System.ComponentModel.ISupportInitialize)crossFraction).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown width;
        private System.Windows.Forms.NumericUpDown height;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown loopFraction;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown crossFraction;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.CheckBox longPassages;
    }
}
