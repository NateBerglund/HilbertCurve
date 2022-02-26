using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HilbertCurve
{
    class Program
    {
        static void Main(string[] args)
        {
            const int exponent = 7; // Power of 2 for which we are generating a hilbert curve
            const int sideLen = 1 << exponent;
            const int n = sideLen * sideLen;

            int[] xCoords = new int[n];
            int[] yCoords = new int[n];
            for (int d = 0; d < n; d++)
            {
                d2xy(n, d, ref xCoords[d], ref yCoords[d]);
            }
            if (exponent % 2 == 1)
            {
                for (int d = 0; d < n; d++)
                {
                    //Swap x and y
                    int t = xCoords[d];
                    xCoords[d] = yCoords[d];
                    yCoords[d] = t;
                }
            }

            const double extrusionRate = 0.032715;
            const double gridStep = 0.5;
            const double stepsPerSecond = 33.88; // Note: if gridStep changes, this will also change
            const double layerHeight = 0.2;
            Console.WriteLine("Grid size (mm): " + (sideLen * gridStep).ToString());
            const double xStart = 50.0;
            const double yStart = 50.0;

            const int nLayers = 9;
            Console.WriteLine("Print time (minutes): " + (nLayers * n / (60 * stepsPerSecond)).ToString());
            int[] cosines = new int[] { 1, 0, -1, 0 };
            int[] sines = new int[] { 0, 1, 0, -1 };

            string outputPath = @"C:\Users\info\source\repos\HilbertCurve\HilbertCurveGCode.txt";
            using (StreamWriter sw = new StreamWriter(outputPath))
            {
                int layerEndX = 0;
                int layerEndY = 0;
                int step = 0;
                int minutesRemaining = (int)Math.Round(nLayers * n / (60 * stepsPerSecond));
                int percentDone = 0;
                for (int layer = 0; layer < nLayers; layer++)
                {
                    if (layer == 1)
                    {
                        sw.WriteLine("M106 S255 ; turn on the fan"); // turn on the fan
                    }
                    for (int d = 0; d < n; d++)
                    {
                        // rotate entire pattern counterclockwise by 90 * layer degrees
                        // center of rotation will be: (sideLen - 1)/2 (both coordinates)
                        // rotatedX = x cos(t) - y sin(t)
                        // rotatedY = x sin(t) + y cos(t)
                        int xRel = 2 * xCoords[d] - (sideLen - 1); // x relative to center (doubled to keep it an integer)
                        int yRel = 2 * yCoords[d] - (sideLen - 1); // y relative to center (doubled to keep it an integer)
                        int rotatedX = xRel * cosines[layer % 4] - yRel * sines[layer % 4];
                        int rotatedY = xRel * sines[layer % 4] + yRel * cosines[layer % 4];
                        rotatedX = (rotatedX + sideLen - 1) / 2;
                        rotatedY = (rotatedY + sideLen - 1) / 2;

                        if (d == 0)
                        {
                            if (rotatedX != layerEndX || rotatedY != layerEndY)
                            {
                                throw new Exception("Start of next layer did not match end of previous layer!");
                            }
                            if (layer > 0)
                            {
                                sw.WriteLine("G1 Z" + string.Format("{0,1:F3}", Math.Round((layer + 1) * layerHeight, 3)) +
                                    " E" + string.Format("{0,1:F5}", Math.Round(extrusionRate * layerHeight, 5)));
                            }
                        }
                        else
                        {
                            sw.WriteLine("G1 X" +
                                string.Format("{0,1:F3}", Math.Round(xStart + gridStep * rotatedX, 3)) +
                                " Y" +
                                string.Format("{0,1:F3}", Math.Round(yStart + gridStep * rotatedY, 3)) +
                                " E" +
                                string.Format("{0,1:F5}", Math.Round(extrusionRate * gridStep, 5)));
                        }

                        if (d == n - 1)
                        {
                            layerEndX = rotatedX;
                            layerEndY = rotatedY;
                        }

                        step++;

                        int nextMinutesRemaining = (int)Math.Round((nLayers * n - step) / (60 * stepsPerSecond));
                        int nextPercentDone = (100 * step) / (nLayers * n - 1);
                        if (nextMinutesRemaining != minutesRemaining || nextPercentDone != percentDone)
                        {
                            minutesRemaining = nextMinutesRemaining;
                            percentDone = nextPercentDone;
                            sw.WriteLine("M73 Q" + percentDone.ToString() + " S" + minutesRemaining.ToString());
                            sw.WriteLine("M73 P" + percentDone.ToString() + " R" + minutesRemaining.ToString());
                        }
                    }
                }
            }
        }

        // See https://en.wikipedia.org/wiki/Hilbert_curve

        //convert (x,y) to d
        static int xy2d(int n, int x, int y)
        {
            int rx, ry, s, d = 0;
            for (s = n / 2; s > 0; s /= 2)
            {
                rx = (x & s) > 0 ? 1 : 0;
                ry = (y & s) > 0 ? 1 : 0;
                d += s * s * ((3 * rx) ^ ry);
                rot(n, ref x, ref y, rx, ry);
            }
            return d;
        }

        //convert d to (x,y)
        static void d2xy(int n, int d, ref int x, ref int y)
        {
            int rx, ry, s, t = d;
            x = 0;
            y = 0;
            for (s = 1; s < n; s *= 2)
            {
                rx = 1 & (t / 2);
                ry = 1 & (t ^ rx);
                rot(s, ref x, ref y, rx, ry);
                x += s * rx;
                y += s * ry;
                t /= 4;
            }
        }

        //rotate/flip a quadrant appropriately
        static void rot(int n, ref int x, ref int y, int rx, int ry)
        {
            if (ry == 0)
            {
                if (rx == 1)
                {
                    x = n - 1 - x;
                    y = n - 1 - y;
                }

                //Swap x and y
                int t = x;
                x = y;
                y = t;
            }
        }
    }
}
