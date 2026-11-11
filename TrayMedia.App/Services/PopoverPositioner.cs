using System.Drawing;
using System.Windows.Forms;

namespace TrayMedia.Services;

public static class PopoverPositioner
{
    public static Point Place(Rectangle trayBounds, Size windowSize)
    {
        var point = new Point(
            trayBounds.X + trayBounds.Width / 2,
            trayBounds.Y + trayBounds.Height / 2);

        var screen = Screen.FromPoint(point);
        var work = screen.WorkingArea;
        var bounds = screen.Bounds;
        const int gap = 4;
        var width = windowSize.Width;
        var height = windowSize.Height;
        var trayValid = trayBounds.Width > 0 && trayBounds.Height > 0;
        var side = DetectTaskbarSide(bounds, work);

        int x;
        int y;

        if (!trayValid)
        {
            x = work.Right - width - 8;
            y = work.Bottom - height - 8;
        }
        else if (side == "top")
        {
            x = trayBounds.X + trayBounds.Width / 2 - width / 2;
            y = work.Top + gap;
        }
        else if (side == "left")
        {
            x = work.Left + gap;
            y = trayBounds.Y + trayBounds.Height / 2 - height / 2;
        }
        else if (side == "right")
        {
            x = work.Right - width - gap;
            y = trayBounds.Y + trayBounds.Height / 2 - height / 2;
        }
        else
        {
            x = trayBounds.X + trayBounds.Width / 2 - width / 2;
            y = Math.Min(trayBounds.Y - height - gap, work.Bottom - height - gap);
        }

        x = Clamp(x, work.Left + 4, work.Right - width - 4);
        y = Clamp(y, work.Top + 4, work.Bottom - height - 4);
        return new Point(x, y);
    }

    private static string DetectTaskbarSide(Rectangle displayBounds, Rectangle workArea)
    {
        if (workArea.Y > displayBounds.Y + 2) return "top";
        if (workArea.X > displayBounds.X + 2) return "left";
        if (workArea.Right < displayBounds.Right - 2) return "right";
        if (workArea.Bottom < displayBounds.Bottom - 2) return "bottom";
        return "bottom";
    }

    private static int Clamp(int value, int min, int max) => Math.Min(Math.Max(value, min), max);
}
