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
