using System;

namespace HelloKinectWPF20170705
{
    /// <summary>
    /// 对RGB32位像素进行处理
    /// </summary>
    internal class PixelDisposal
    {
        /// <summary>
        /// 反转颜色
        /// </summary>
        /// <param name="pixelData">像素数据byte数组</param>
        /// <param name="bytePerPixel">每个像素所占的字节数</param>
        public static void InvertedColor(byte[] pixelData, int bytePerPixel)
        {
            for (int i = 0; i < pixelData.Length; i += bytePerPixel)
            {
                pixelData[i] = (byte)~pixelData[i];
                pixelData[i + 1] = (byte)~pixelData[i + 1];
                pixelData[i + 2] = (byte)~pixelData[i + 2];
            }
        }

        public static void ApocalypticZombie(byte[] pixelData, int bytePerPixel)
        {
            for (int i = 0; i < pixelData.Length; i += bytePerPixel)
            {
                pixelData[i] = pixelData[i + 1];
                pixelData[i + 1] = pixelData[i];
                pixelData[i + 2] = (byte)~pixelData[i + 2];
            }
        }

        public static void GrayScale(byte[] pixelData, int bytePerPixel)
        {
            for (int i = 0; i < pixelData.Length; i += bytePerPixel)
            {
                byte gray = Math.Max(pixelData[i], pixelData[i + 1]);
                gray = Math.Max(gray, pixelData[i + 2]);
                pixelData[i] = gray;
                pixelData[i + 1] = gray;
                pixelData[i + 2] = gray;
            }
        }

        public static void GrainyBlackAndWhiteMovie(byte[] pixelData, int bytePerPixel)
        {
            for (int i = 0; i < pixelData.Length; i += bytePerPixel)
            {
                byte gray = Math.Min(pixelData[i], pixelData[i + 1]);
                gray = Math.Min(gray, pixelData[i + 2]);
                pixelData[i] = gray;
                pixelData[i + 1] = gray;
                pixelData[i + 2] = gray;
            }
        }

        public static void WashedOutColor(byte[] pixelData, int bytePerPixel)
        {
            for (int i = 0; i < pixelData.Length; i += bytePerPixel)
            {
                double gray = (pixelData[i] * 0.11) + (pixelData[i + 1] * 0.59) + (pixelData[i + 2] * 0.3);
                double desaturation = 0.75;
                pixelData[i] = (byte)(pixelData[i] + desaturation * (gray - pixelData[i]));
                pixelData[i + 1] = (byte)(pixelData[i + 1] + desaturation * (gray - pixelData[i + 1]));
                pixelData[i + 2] = (byte)(pixelData[i + 2] + desaturation * (gray - pixelData[i + 2]));
            }
        }

        public static void HighSaturation(byte[] pixelData, int bytePerPixel)
        {
            for (int i = 0; i < pixelData.Length; i += bytePerPixel)
            {
                if (pixelData[i] < 0x33 || pixelData[i] > 0xE5)
                {
                    pixelData[i] = 0x00;
                }
                else
                {
                    pixelData[i] = 0Xff;
                }
                if (pixelData[i + 1] < 0x33 || pixelData[i + 1] > 0xE5)
                {
                    pixelData[i + 1] = 0x00;
                }
                else
                {
                    pixelData[i + 1] = 0Xff;
                }
                if (pixelData[i + 2] < 0x33 || pixelData[i + 2] > 0xE5)
                {
                    pixelData[i + 2] = 0x00;
                }
                else
                {
                    pixelData[i + 1] = 0Xff;
                }
            }
        }
    }
}