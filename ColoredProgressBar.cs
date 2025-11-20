using System;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;

namespace WebUIMonitor
{
    /// <summary>
    /// 支持动态颜色变化的自定义进度条（解决标准ProgressBar在Windows Vista+无法改变颜色的问题）
    /// </summary>
    public class ColoredProgressBar : ProgressBar
    {
        private Color _barColor = Color.Green;

        public ColoredProgressBar()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        /// <summary>获取或设置进度条填充颜色</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color BarColor
        {
            get => _barColor;
            set
            {
                if (_barColor != value)
                {
                    _barColor = value;
                    Invalidate();
                }
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e) => e.Graphics.Clear(BackColor);

        protected override void OnPaint(PaintEventArgs e)
        {
            var rect = ClientRectangle;
            
            // 绘制背景
            e.Graphics.FillRectangle(new SolidBrush(SystemColors.Control), rect);
            
            // 绘制进度条填充
            if (Maximum > 0)
            {
                int barWidth = (int)((double)Value / Maximum * rect.Width);
                e.Graphics.FillRectangle(new SolidBrush(_barColor), rect.X, rect.Y, barWidth, rect.Height);
            }
            
            // 绘制边框
            e.Graphics.DrawRectangle(SystemPens.ControlDark, rect.X, rect.Y, rect.Width - 1, rect.Height - 1);
        }
    }
}
