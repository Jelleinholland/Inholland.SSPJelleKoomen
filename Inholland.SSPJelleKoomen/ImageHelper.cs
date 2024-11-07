using System;
using System.IO;
using Microsoft.Extensions.Logging;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Inholland.SSPJelleKoomen;

public class ImageHelper
{
    public static Stream AddTextToImage(Stream imageStream,
        params (string text, (float x, float y) position, int fontSize, string colorHex)[] texts)
    {
        var memoryStream = new MemoryStream();

        // Load the image from the input stream
        var image = Image.Load(imageStream);

        // Perform the drawing operations on the image
        image.Mutate(img =>
        {
            var textGraphicsOptions = new TextGraphicsOptions()
            {
                TextOptions =
                {
                    WrapTextWidth = image.Width - 10 // Prevent text from overflowing
                }
            };

            // Add each text entry
            foreach (var (text, (x, y), fontSize, colorHex) in texts)
            {
                var font = SystemFonts.CreateFont("Verdana", fontSize);
                var color = Rgba32.ParseHex(colorHex);

                // Draw the text on the image
                img.DrawText(textGraphicsOptions, text, font, color, new PointF(x, y));
            }
        });

        // Save the processed image as PNG to the memory stream
        image.SaveAsPng(memoryStream);

        // Reset the position of the stream for reading after saving the image
        memoryStream.Position = 0;

        return memoryStream;
    }
}

