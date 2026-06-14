namespace SimplexLab.WeaveMaze.TApplication
{
    partial class CustomizedMazeControl
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
            loopFraction = new System.Windows.Forms.NumericUpDown();
            label3 = new System.Windows.Forms.Label();
            crossFraction = new System.Windows.Forms.NumericUpDown();
            label4 = new System.Windows.Forms.Label();
            longPassages = new System.Windows.Forms.CheckBox();
            filename = new System.Windows.Forms.TextBox();
            brower = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)loopFraction).BeginInit();
            ((System.ComponentModel.ISupportInitialize)crossFraction).BeginInit();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(3, 8);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(92, 17);
            label1.TabIndex = 0;
            label1.Text = "Mask File Path";
            // 
            // loopFraction
            // 
            loopFraction.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            loopFraction.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
            loopFraction.Location = new System.Drawing.Point(101, 34);
            loopFraction.Name = "loopFraction";
            loopFraction.Size = new System.Drawing.Size(160, 23);
            loopFraction.TabIndex = 5;
            loopFraction.Value = new decimal(new int[] { 5, 0, 0, 0 });
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(3, 36);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(92, 17);
            label3.TabIndex = 4;
            label3.Text = "Loop";
            // 
            // crossFraction
            // 
            crossFraction.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            crossFraction.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
            crossFraction.Location = new System.Drawing.Point(100, 63);
            crossFraction.Name = "crossFraction";
            crossFraction.Size = new System.Drawing.Size(160, 23);
            crossFraction.TabIndex = 7;
            crossFraction.Value = new decimal(new int[] { 25, 0, 0, 0 });
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new System.Drawing.Point(3, 65);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(91, 17);
            label4.TabIndex = 6;
            label4.Text = "Cross";
            // 
            // longPassages
            // 
            longPassages.AutoSize = true;
            longPassages.Location = new System.Drawing.Point(3, 92);
            longPassages.Name = "longPassages";
            longPassages.Size = new System.Drawing.Size(114, 21);
            longPassages.TabIndex = 9;
            longPassages.Text = "Long Passages";
            longPassages.UseVisualStyleBackColor = true;
            // 
            // filename
            // 
            filename.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            filename.Location = new System.Drawing.Point(100, 5);
            filename.Name = "filename";
            filename.ReadOnly = true;
            filename.Size = new System.Drawing.Size(135, 23);
            filename.TabIndex = 10;
            // 
            // brower
            // 
            brower.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            brower.Location = new System.Drawing.Point(237, 5);
            brower.Name = "brower";
            brower.Size = new System.Drawing.Size(23, 23);
            brower.TabIndex = 11;
            brower.Text = "...";
            brower.UseVisualStyleBackColor = true;
            brower.Click += OnBrowerClickedHandler;
            // 
            // RectangularMazeMaskControl
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(brower);
            Controls.Add(filename);
            Controls.Add(longPassages);
            Controls.Add(crossFraction);
            Controls.Add(label4);
            Controls.Add(loopFraction);
            Controls.Add(label3);
            Controls.Add(label1);
            Name = "RectangularMazeMaskControl";
            Size = new System.Drawing.Size(260, 121);
            ((System.ComponentModel.ISupportInitialize)loopFraction).EndInit();
            ((System.ComponentModel.ISupportInitialize)crossFraction).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown loopFraction;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown crossFraction;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.CheckBox longPassages;
        private System.Windows.Forms.TextBox filename;
        private System.Windows.Forms.Button brower;
    }
}
