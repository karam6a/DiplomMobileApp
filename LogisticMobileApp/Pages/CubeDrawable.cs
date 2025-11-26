// File: Pages/CubeDrawable.cs
using Microsoft.Maui.Graphics;

namespace LogisticMobileApp.Pages;

public class CubeDrawable : IDrawable
{
    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        float size = Math.Min(dirtyRect.Width, dirtyRect.Height) * 0.8f;
        float centerX = dirtyRect.Center.X;
        float offset = size / 2;

        // Тень
        canvas.FillColor = Colors.Black.WithAlpha(0.15f);
        canvas.FillEllipse(centerX - offset + 8, dirtyRect.Center.Y - offset + 8, size, size * 0.4f);

        // Градиентный кубик с 3D-эффектом
        var gradientPaint = new LinearGradientPaint
        {
            StartColor = Color.FromArgb("#7C3AED"),
            EndColor = Color.FromArgb("#3B82F6"),
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 1)
        };

        // Основная грань
        canvas.SetFillPaint(gradientPaint, RectF.FromLTRB(
            centerX - offset, dirtyRect.Center.Y - offset,
            centerX + offset, dirtyRect.Center.Y + offset));

        canvas.FillRoundedRectangle(centerX - offset, dirtyRect.Center.Y - offset, size, size, 20);

        // Светлая грань (сверху-слева)
        canvas.FillColor = Colors.White.WithAlpha(0.25f);
        canvas.FillRoundedRectangle(centerX - offset + 5, dirtyRect.Center.Y - offset + 5, size, size, 20);

        // Тёмная грань (справа-снизу)
        canvas.FillColor = Colors.Black.WithAlpha(0.2f);
        canvas.FillRoundedRectangle(centerX - offset + 15, dirtyRect.Center.Y - offset + 15, size - 10, size - 10, 18);

        // Блик
        canvas.FillColor = Colors.White.WithAlpha(0.6f);
        canvas.FillEllipse(centerX - offset + 20, dirtyRect.Center.Y - offset + 20, 30, 30);
    }
}