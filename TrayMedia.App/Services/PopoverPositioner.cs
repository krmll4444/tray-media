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
        return Place(trayBounds, windowSize, screen.Bounds, screen.WorkingArea);
    }

    /// <summary>
    /// Pure layout helper — used by unit tests without depending on live Screen APIs.
    /// </summary>
    public static Point Place(
        Rectangle trayBounds,
        Size windowSize,
        Rectangle displayBounds,
        Rectangle workArea)
    {
        const int gap = 4;
        var width = windowSize.Width;
        var height = windowSize.Height;
        var trayValid = trayBounds.Width > 0 && trayBounds.Height > 0;
        var side = DetectTaskbarSide(displayBounds, workArea);

        int x;
        int y;

        if (!trayValid)
        {
            x = workArea.Right - width - 8;
            y = workArea.Bottom - height - 8;
        }
        else if (side == "top")
        {
            x = trayBounds.X + trayBounds.Width / 2 - width / 2;
            y = workArea.Top + gap;
        }
        else if (side == "left")
        {
            x = workArea.Left + gap;
            y = trayBounds.Y + trayBounds.Height / 2 - height / 2;
        }
        else if (side == "right")
        {
            x = workArea.Right - width - gap;
            y = trayBounds.Y + trayBounds.Height / 2 - height / 2;
        }
        else
        {
            x = trayBounds.X + trayBounds.Width / 2 - width / 2;
            y = Math.Min(trayBounds.Y - height - gap, workArea.Bottom - height - gap);
        }

        x = Clamp(x, workArea.Left + 4, workArea.Right - width - 4);
        y = Clamp(y, workArea.Top + 4, workArea.Bottom - height - 4);
        return new Point(x, y);
    }

    public static string DetectTaskbarSide(Rectangle displayBounds, Rectangle workArea)
    {
        if (workArea.Y > displayBounds.Y + 2) return "top";
        if (workArea.X > displayBounds.X + 2) return "left";
        if (workArea.Right < displayBounds.Right - 2) return "right";
        if (workArea.Bottom < displayBounds.Bottom - 2) return "bottom";
        return "bottom";
    }

    public static int Clamp(int value, int min, int max) => Math.Min(Math.Max(value, min), max);
}
