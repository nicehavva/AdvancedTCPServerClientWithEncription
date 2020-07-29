using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Nicehavva.AdvancedTCP.Client.Helpers
{
    public class RemoteDesktop
    {
        public static MemoryStream CaptureScreenToMemoryStream(int quality)
        {
            // ************ All Screen ************
            //Bitmap bmp = new Bitmap(SystemInformation.VirtualScreen.Width,
            //                   SystemInformation.VirtualScreen.Height,
            //                   PixelFormat.Format32bppArgb);
            //Graphics screenGraph = Graphics.FromImage(bmp);
            //screenGraph.CopyFromScreen(SystemInformation.VirtualScreen.X,
            //                           SystemInformation.VirtualScreen.Y,
            //                           0,
            //                           0,
            //                           SystemInformation.VirtualScreen.Size,
            //                           CopyPixelOperation.SourceCopy);
            //screenGraph.Dispose();

            // ************ Primary Screen ************

            Bitmap bmp = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
            Graphics g = Graphics.FromImage(bmp);
            g.CopyFromScreen(new Point(0, 0), new Point(0, 0), bmp.Size);
            g.Dispose();


            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
            ImageCodecInfo ici = null;

            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.MimeType == "image/jpeg")
                    ici = codec;
            }

            var ep = new EncoderParameters();
            ep.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, (long)quality);

            MemoryStream ms = new MemoryStream();
            bmp.Save(ms, ici, ep);
            ms.Position = 0;
            bmp.Dispose();

            return ms;
        }
    }
}
