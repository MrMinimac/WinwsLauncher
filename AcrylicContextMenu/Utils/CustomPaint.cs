using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace AcrylicViews.Utils
{
    internal static class CustomPaint
    {
        public static void DrawRoundedRect (Graphics g, Size controlSize, Color color, Padding margin, int cornerRadius)
        {
            try
            {
                g.FillPath(new SolidBrush(color),
                    CustomPaint.RoundedRect(cornerRadius, margin, controlSize));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OnPaint: Error drawing background: {ex.Message}");
            }
        }

        private static GraphicsPath RoundedRect(
            int radius,
            Padding margins, // (0,0,0,0) - (left,top,right,bottom)
            Size controlSize)
        {
            Rectangle bounds = new Rectangle(
                (margins.Left + margins.Right) / 2,
                (margins.Top + margins.Bottom) / 2,
                controlSize.Width - (margins.Left + margins.Right),
                controlSize.Height - (margins.Top + margins.Bottom));

            int diameter = radius * 2;
            Size size = new Size(diameter, diameter);
            Rectangle arc = new Rectangle(bounds.Location, size);
            GraphicsPath path = new GraphicsPath();

            if (radius == 0)
            {
                path.AddRectangle(bounds);
                return path;
            }

            // top left arc  
            path.AddArc(arc, 180, 90);

            // top right arc  
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);

            // bottom right arc  
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);

            // bottom left arc 
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            return path;
        }


        public static void DrawCheckMark(Graphics g, Color color, float size, Padding margin, Size controlSize)
        {
            try
            {
                using (Pen pen = new Pen(color, 2))
                {
                    var checkPoints = CustomPaint.CheckMark(size, margin, controlSize);
                    if (checkPoints != null && checkPoints.Length >= 2)
                    {
                        g.DrawLines(pen, checkPoints);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OnPaint: Error drawing checkmark: {ex.Message}");
            }
        }

        private static Point[] CheckMark(float size, Padding margin, Size controlSize)
        {
            int point1_x = (int)(0 * size) + margin.Left / 3 + 2;
            int point1_y = (int)(5 * size) + controlSize.Height / 3;

            int point2_x = (int)(4 * size) + margin.Left / 3 + 2;
            int point2_y = (int)(9 * size) + controlSize.Height / 3;

            int point3_x = (int)(12 * size) + margin.Left / 3 + 2;
            int point3_y = (int)(0 * size) + controlSize.Height / 3;


            // Точки галочки — можно подкорректировать под размер
            Point[] checkPoints = new Point[]
            {
                    new Point(point1_x, point1_y),
                    new Point(point2_x, point2_y),
                    new Point(point3_x, point3_y)
            };

            return checkPoints;
        }

        public static void DrawArrow(Graphics g, Color color, float Size, Padding margin, Size controlSize)
        {
            try
            {
                var arrowPoints = CustomPaint.Arrow(Size, margin, controlSize);
                if (arrowPoints != null && arrowPoints.Length >= 3)
                {
                    g.FillPolygon(new SolidBrush(color), arrowPoints);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OnPaint: Error drawing arrow: {ex.Message}");
            }
        }

        private static Point[] Arrow(float size, Padding margin, Size controlSize)
        {
            // Высота стрелки в пикселях
            int arrowHeight = (int)(8 * size);

            // Ширина стрелки в пикселях
            int arrowWidth = (int)(5 * size);

            int x = controlSize.Width - arrowWidth - margin.Right + margin.Left;

            // Вычисляем вертикальный отступ, чтобы стрелка была по центру
            int offsetY = ((controlSize.Height - arrowHeight) / 2) + margin.Top - margin.Bottom;

            int point1_x = x + (int)(0 * size);
            int point1_y = offsetY + (int)(0 * size);

            int point2_x = x + arrowWidth;
            int point2_y = offsetY + (int)(4 * size);

            int point3_x = x + (int)(0 * size);
            int point3_y = offsetY + arrowHeight;

            return new Point[]
            {
                new Point(point1_x, point1_y),   // верхняя точка
                new Point(point2_x, point2_y),   // правая точка
                new Point(point3_x, point3_y)    // нижняя точка
            };
        }

        public static void DrawText(Graphics g, string text, Font font, Color foreColor, Padding margin, Size controlSize)
        {
            try
            {
                using (Brush brush = new SolidBrush(foreColor))
                {
                    

                    // Измеряем размер текста
                    SizeF textSize = g.MeasureString(text, font);

                    // Вычисляем координаты, чтобы текст оказался строго по центру
                    float x = margin.Left;
                    float y = (controlSize.Height - textSize.Height) / 2;

                    // Учитываем TextMargins.Top/Bottom, если нужно
                    y = Math.Max(y, margin.Top);

                    GraphicsPath path = new GraphicsPath();
                    path.AddString(text, font.FontFamily, (int)font.Style, font.SizeInPoints * g.DpiY / 72, new PointF(x, y), StringFormat.GenericDefault);

                    // Проверяем, что текст помещается в область
                    if (x + textSize.Width <= controlSize.Width - margin.Right)
                    {
                        g.DrawPath(new Pen(new SolidBrush(Color.FromArgb(100, foreColor)), 0.05f), path);
                        g.FillPath(brush, path);
                    }

                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OnPaint: Error drawing text: {ex.Message}");
            }
        }
    }
}
