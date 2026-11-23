using HydroColor.Models;

namespace HydroColor.Services
{
    public class BayerPatternDemosaic
    {
        public ColorChannelData<UInt16[,]> Bayer2RGB(UInt16[,] imageData, int ImageHeight, int ImageWidth, BayerFilterType bayerFilterType)
        {
            int[] Roffset = new int[2];
            int[] G1offset = new int[2];
            int[] G2offset = new int[2];
            int[] Boffset = new int[2];
            switch (bayerFilterType)
            {
                case BayerFilterType.BGGR:
                    /* B G B G
                    /  G R G R
                    /  B G B G 
                    /  G R G R
                    */
                    Roffset = new int[] { 1, 1 };
                    G1offset = new int[] { 0, 1 };
                    G2offset = new int[] { 1, 0 };
                    Boffset = new int[] { 0, 0 };

                    break;
                case BayerFilterType.GBRG:
                    /* G B G B
                    /  R G R G
                    /  G B G B 
                    /  R G R G
                    */
                    Roffset = new int[] { 1, 0 };
                    G1offset = new int[] { 0, 0 };
                    G2offset = new int[] { 1, 1 };
                    Boffset = new int[] { 0, 1 };

                    break;
                case BayerFilterType.GRBG:
                    /* G R G R
                    /  B G B G
                    /  G R G R 
                    /  B G B G
                    */
                    Roffset = new int[] { 0, 1 };
                    G1offset = new int[] { 0, 0 };
                    G2offset = new int[] { 1, 1 };
                    Boffset = new int[] { 1, 0 };
                    break;
                case BayerFilterType.RGGB:
                    /* R G R G
                    /  G B G B
                    /  R G R G 
                    /  G B G B
                    */
                    Roffset = new int[] { 0, 0 };
                    G1offset = new int[] { 0, 1 };
                    G2offset = new int[] { 1, 0 };
                    Boffset = new int[] { 1, 1 };
                    break;
                case BayerFilterType.Unknown:
                    break;
            }

            ColorChannelData<UInt16[,]> RGBImages = new ColorChannelData<UInt16[,]>();

            RGBImages.Red = new UInt16[ImageHeight / 2, ImageWidth / 2];
            RGBImages.Green = new UInt16[ImageHeight / 2, ImageWidth / 2];
            RGBImages.Blue = new UInt16[ImageHeight / 2, ImageWidth / 2];

            /* Writing out raw image data for troubleshooting
            UInt16[,] G1Image = new UInt16[ImageHeight / 2, ImageWidth / 2];
            UInt16[,] G2Image = new UInt16[ImageHeight / 2, ImageWidth / 2];
            */

            int pixelRow = 0;
            int pixelCol = 0;
            UInt16 G1;
            UInt16 G2;

            // This creates one RGB pixel for each 2x2 pixel region of the bayer image
            for (int row = 0; row < ImageHeight; row += 2)
            {
                for (int col = 0; col < ImageWidth; col += 2)
                {
                    RGBImages.Red[pixelRow, pixelCol] = imageData[row + Roffset[0], col + Roffset[1]];
                    G1 = imageData[row + G1offset[0], col + G1offset[1]];
                    G2 = imageData[row + G2offset[0], col + G2offset[1]];
                    RGBImages.Blue[pixelRow, pixelCol] = imageData[row + Boffset[0], col + Boffset[1]];

                    // take average of the two green pixels
                    RGBImages.Green[pixelRow, pixelCol] = (UInt16)(((float)G1 + (float)G2) / 2);

                    /* Writing out raw image data for troubleshooting
                    G1Image[pixelRow, pixelCol] = imageData[row + G1offset[0], col + G1offset[1]];
                    G2Image[pixelRow, pixelCol] = imageData[row + G2offset[0], col + G2offset[1]];
                    */

                    pixelCol += 1;
                }
                pixelCol = 0;
                pixelRow += 1;
            }

            /* Writing out raw image data for troubleshooting
            FileReaderWriter FilerWriter = new();

            FilerWriter.WriteBinaryArray2File("ImageDataR.bin", RGBImages.Red);
            FilerWriter.WriteBinaryArray2File("ImageDataG1.bin", G1Image);
            FilerWriter.WriteBinaryArray2File("ImageDataG2.bin", G2Image);
            FilerWriter.WriteBinaryArray2File("ImageDataB.bin", RGBImages.Blue);
            */

            return RGBImages;
        }

        // Converts the black level values from the raw image metadata (the DNG 'BlackLevel' entry)
        // into one black level for each color channel.
        //
        // The DNG format stores the black level as a small repeating tile, in reading order
        // (row by row, starting at the top-left of the image). Each value therefore belongs to a
        // POSITION in the tile, not to a color:
        //
        //   one value                 the same black level for every pixel
        //   four values (2x2 tile)    [0] top-left   [1] top-right
        //                             [2] bottom-left [3] bottom-right
        //
        // A 2x2 tile lines up with the 2x2 Bayer pattern, so the color at each position is given by
        // the Bayer pattern. The positions used here must match those used in Bayer2RGB above.
        //
        // Any other layout with differing values cannot be mapped to colors, so the average is used
        // for all three channels.
        public static ColorChannelData<double> GetChannelBlackLevels(IReadOnlyList<double> blackLevelValues, BayerFilterType bayerFilterType)
        {
            if (blackLevelValues == null || blackLevelValues.Count == 0)
            {
                throw new ArgumentException("The image metadata does not contain a black level.");
            }

            ColorChannelData<double> BlackLevel = new();

            bool allValuesEqual = blackLevelValues.All(value => value == blackLevelValues[0]);

            if (!allValuesEqual && blackLevelValues.Count == 4 && bayerFilterType != BayerFilterType.Unknown)
            {
                double topLeft = blackLevelValues[0];
                double topRight = blackLevelValues[1];
                double bottomLeft = blackLevelValues[2];
                double bottomRight = blackLevelValues[3];

                switch (bayerFilterType)
                {
                    case BayerFilterType.BGGR:
                        /* B G
                        /  G R
                        */
                        BlackLevel.Blue = topLeft;
                        BlackLevel.Green = (topRight + bottomLeft) / 2;
                        BlackLevel.Red = bottomRight;
                        break;
                    case BayerFilterType.GBRG:
                        /* G B
                        /  R G
                        */
                        BlackLevel.Green = (topLeft + bottomRight) / 2;
                        BlackLevel.Blue = topRight;
                        BlackLevel.Red = bottomLeft;
                        break;
                    case BayerFilterType.GRBG:
                        /* G R
                        /  B G
                        */
                        BlackLevel.Green = (topLeft + bottomRight) / 2;
                        BlackLevel.Red = topRight;
                        BlackLevel.Blue = bottomLeft;
                        break;
                    case BayerFilterType.RGGB:
                        /* R G
                        /  G B
                        */
                        BlackLevel.Red = topLeft;
                        BlackLevel.Green = (topRight + bottomLeft) / 2;
                        BlackLevel.Blue = bottomRight;
                        break;
                }
            }
            else
            {
                // A single value, a list of equal values, or a layout that cannot be mapped to colors
                double average = blackLevelValues.Average();
                BlackLevel.Red = average;
                BlackLevel.Green = average;
                BlackLevel.Blue = average;
            }

            return BlackLevel;
        }

        // Converts a raw camera image into the median pixel value of each color channel.
        // Used for every measurement image and by the sensitivity test, so both are processed the same way.
        public static ColorChannelData<double> GetMedianPixelValues(HydroColorRawImageData rawImage)
        {
            BayerPatternDemosaic BPD = new BayerPatternDemosaic();

            // Convert Byte array to Uint16 values (and reshape into 2d array).
            UInt16[,] reshapedImageData = new UInt16[rawImage.ImageHeight, rawImage.RowStride / 2];
            Buffer.BlockCopy(rawImage.RawImagePixelData, 0, reshapedImageData, 0, rawImage.ImageHeight * rawImage.RowStride);

            // Demosaic Bayer filter pattern
            ColorChannelData<UInt16[,]> RGBImages = BPD.Bayer2RGB(reshapedImageData, rawImage.ImageHeight, rawImage.ImageWidth, rawImage.BayerFilterPattern);

            // Only use the center of the image. Take a square at the center 1/4 the size of the smallest dimension.
            int minDimension = Math.Min(RGBImages.Red.GetLength(0), RGBImages.Red.GetLength(1));

            ColorChannelData<double> medians = new();
            medians.Red = BPD.GetMedian(RGBImages.Red, minDimension / 4);
            medians.Green = BPD.GetMedian(RGBImages.Green, minDimension / 4);
            medians.Blue = BPD.GetMedian(RGBImages.Blue, minDimension / 4);
            return medians;
        }

        public UInt16 GetMedian(UInt16[,] Data, int PixleSquareSize)
        {
            int startRow = Data.GetLength(0) / 2 - PixleSquareSize / 2;
            int endRow = Data.GetLength(0) / 2 + PixleSquareSize / 2;
            int startCol = Data.GetLength(1) / 2 - PixleSquareSize / 2;
            int endCol = Data.GetLength(1) / 2 + PixleSquareSize / 2;

            List<UInt16> pixels = new();

            for (int row = startRow; row <= endRow; row++)
            {
                for (int col = startCol; col <= endCol; col++)
                {
                    pixels.Add(Data[row, col]);
                }
            }
            return MedianOfList(pixels);
        }

        private UInt16 MedianOfList(List<UInt16> numbers)
        {
            if (numbers == null || numbers.Count == 0)
            {
                return 0;
            }

            numbers = numbers.OrderBy(n => n).ToList();

            var halfIndex = numbers.Count() / 2;

            if (numbers.Count() % 2 == 0)
            {
                return (UInt16)(((float)numbers[halfIndex] + (float)numbers[halfIndex - 1]) / 2.0);
            }
            else
            {
                return numbers[halfIndex];
            }
        }
    }
}
