using System;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;

namespace WebUIMonitor
{
    /// <summary>
    /// 支持真正改变颜色的进度条（解决标准ProgressBar颜色问题）
    /// </summary>
    public class ColoredProgressBar : ProgressBar
    {
        private Color _barColor = Color.Green;

        public ColoredProgressBar()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color BarColor
        {
            get { return _barColor; }
            set 
            { 
                if (_barColor != value)
                {
                    _barColor = value;
                    Invalidate();
                    Refresh();  // 强制立即重绘
                }
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Rectangle rect = ClientRectangle;
            
            // 绘制背景
            e.Graphics.FillRectangle(new SolidBrush(SystemColors.Control), rect);
            
            // 绘制进度条
            if (Maximum > 0)
            {
                int barWidth = (int)((double)Value / Maximum * rect.Width);
                Rectangle barRect = new Rectangle(rect.X, rect.Y, barWidth, rect.Height);
                e.Graphics.FillRectangle(new SolidBrush(_barColor), barRect);
            }
            
            // 绘制边框
            e.Graphics.DrawRectangle(SystemPens.ControlDark, rect.X, rect.Y, rect.Width - 1, rect.Height - 1);
        }
    }
}
