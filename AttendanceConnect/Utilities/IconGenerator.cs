using System.Drawing;

namespace AttendanceConnect.Utilities
{
    public static class IconGenerator
    {
        public static Icon CreateNormalIcon()
        {
            var bitmap = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.White);
                g.FillEllipse(new SolidBrush(Color.Green), 4, 4, 24, 24);
                g.DrawEllipse(Pens.DarkGreen, 4, 4, 24, 24);
            }
            return Icon.FromHandle(bitmap.GetHicon());
        }

        public static Icon CreateErrorIcon()
        {
            var bitmap = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.White);
                g.FillEllipse(new SolidBrush(Color.Red), 4, 4, 24, 24);
                g.DrawEllipse(Pens.DarkRed, 4, 4, 24, 24);

                // Draw exclamation mark
                var font = new Font("Arial", 16, FontStyle.Bold);
                g.DrawString("!", font, Brushes.White, 10, 6);
            }
            return Icon.FromHandle(bitmap.GetHicon());
        }
    }
}
