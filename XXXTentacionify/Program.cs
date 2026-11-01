using System.Buffers.Binary;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection.PortableExecutable;
using System.Runtime.Serialization.Formatters;
using static System.Net.Mime.MediaTypeNames;
internal class Program
{
    static byte changeByte(byte color, byte[] data, ref long bit, long required)
    {
        int value = 0;
        for (int i = 0; i < 2; i++)
        {
            value <<= 1;

            if (bit < required)
            {
                int byteIndex = (int)(bit / 8);
                int bitIndex = 7 - (int)(bit % 8);

                value |= (data[byteIndex] >> bitIndex) & 1;
                bit++;
            }
        }
        return (byte)((color & 0xFC) | value);
    }

    static byte[] recoverBytes(Bitmap image, int count, ref long bit)
    {
        byte[] result = new byte[count];
        for (int i = 0; i < count; i++)
        {
            byte value = 0;

            for (int j = 0; j < 8; j+= 2)
            {
                long pixel = bit / (6);
                int channel = (int)((bit / 2) % 3);

                int x = (int)(pixel % image.Width);
                int y = (int)(pixel / image.Width);

                Color color = image.GetPixel(x, y);

                byte color2 = channel == 0 ? color.R : channel == 1 ? color.G : color.B;

                value = (byte)((value << 2) | (color2 & 0x03));

                bit += 2;
            }

            result[i] = value;
        }
        return result;
    }

    private static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Please drag a file onto the app next time or launch it with \"extract\" as argument. Click any key to exit.");
            Console.ReadKey();
            return;
        }
        else
        {
            if (args[0].ToLower().EndsWith("done.png"))
            {
                using Bitmap image = new Bitmap("done.png");
                long bit = 0;

                byte[] header = recoverBytes(image, 4, ref bit);
                int length = BitConverter.ToInt32(header, 0);

                long capacity = (long)image.Width * image.Height * 6 / 8;

                if (length < 0 || length > capacity - 4)
                    throw new Exception("Invalid embedded file size!");

                byte[] payload = recoverBytes(image, length, ref bit);
                File.WriteAllBytes("output", payload);
            }
            else
            {
                byte[] payload = File.ReadAllBytes(Path.GetFullPath(args[0]));
                byte[] data    = new byte[payload.Length + 4];

                BitConverter.GetBytes(payload.Length).CopyTo(data, 0);
                Buffer.BlockCopy(payload, 0, data, 4, payload.Length);

                using Bitmap image = new Bitmap(Path.Combine(AppContext.BaseDirectory, "xxxtentacion.jpg"));

                long capacity = (long)image.Width * image.Height * 6 / 8;
                long required = (long)data.Length * 8;

                if (data.Length > capacity)
                {
                    Console.WriteLine("The file is too large to fit in the image. Click any key to exit.");
                    Console.ReadKey();
                    return;
                }

                long bit = 0;

                for (int y = 0; y < image.Height && bit < required; y++)
                {
                    for (int x = 0; x < image.Width && bit < required; x++)
                    {
                        Color color = image.GetPixel(x, y);

                        byte r = changeByte(color.R, data, ref bit, required);
                        byte g = changeByte(color.G, data, ref bit, required);
                        byte b = changeByte(color.B, data, ref bit, required);

                        image.SetPixel(x, y, Color.FromArgb(color.A, r, g, b));
                    }
                }
                if (File.Exists("done.png"))
                    File.Delete("done.png");
                image.Save("done.png", ImageFormat.Png);
            }
        }
    }
}