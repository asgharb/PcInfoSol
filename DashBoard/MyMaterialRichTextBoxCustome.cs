using System;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace DashBoard
{
    public class MyMaterialRichTextBoxCustome : UserControl
    {
        private RichTextBox box = new RichTextBox();
        private bool isFocused = false;

        public MyMaterialRichTextBoxCustome()
        {
            // Base control
            this.DoubleBuffered = true;
            this.Padding = new Padding(8);
            this.BackColor = Color.White;
            this.ForeColor = Color.Black;

            // RichTextBox
            box.BorderStyle = BorderStyle.None;
            box.BackColor = Color.White;
            box.ForeColor = Color.Black;
            box.Font = new Font("Segoe UI", 10f);
            box.Dock = DockStyle.Fill;

            box.GotFocus += (s, e) => { isFocused = true; this.Invalidate(); };
            box.LostFocus += (s, e) => { isFocused = false; this.Invalidate(); };

            Controls.Add(box);

            this.Height = 120;
        }

        public override string Text
        {
            get => box.Text;
            set => box.Text = value;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int radius = 1;

            // Rounded rectangle background
            using (GraphicsPath path = RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), radius))
            {
                using (SolidBrush brush = new SolidBrush(Color.White))
                {
                    g.FillPath(brush, path);
                }

                // Soft border
                using (Pen pen = new Pen(Color.LightGray, 1))
                {
                    g.DrawPath(pen, path);
                }
            }

            // Underline Focus Animation
            Color focusColor = isFocused ? Color.FromArgb(33, 150, 243) : Color.LightGray; // Material Blue
            int lineWidth = isFocused ? 3 : 2;

            using (Pen underline = new Pen(focusColor, lineWidth))
            {
                g.DrawLine(underline, 10, Height - 8, Width - 10, Height - 8);
            }
        }

        private GraphicsPath RoundedRect(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = radius * 2;

            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();

            return path;
        }
    }
}
