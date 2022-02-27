using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HilbertCurve
{
    enum LayerType
    {
        FirstLayer,
        SecondLayer,
        OtherLayer,
        LastLayer
    }

    class Program
    {
        static void Main(string[] args)
        {
            const string outputFolder = @"C:\Users\info\source\repos\HilbertCurve";

            // Starting position of the main Hilbert Curve
            const double xStart = 50.0;
            const double yStart = 50.0;

            // Length in mm of one "step" of the Hilbert Curve grid
            const double gridStep = 0.5;
            const double stepsPerSecond = 33.21; // "Steps" here means steps of the Hilbert Curve. Note: if gridStep changes, this will also change
            const double oneMinuteInSteps = 60 * stepsPerSecond; // For convenience, stores the equivalent number of 'steps' for one minute's worth of time

            // Minutes to draw the intro line
            const double introLineMinutes = 1.0;

            // Layer height in mm
            const double layerHeight = 0.2;
            const string layerHeightText = "0.2mm"; // as it will appear in the filename

            // Border padding in mm
            const double borderPadding = 5.0;

            // Total number of layers to print
            const int nLayers = 9;

            // extrusion rate per mm (not sure what the units on the extruder motor are)
            const double extrusionRate = 0.032715;

            // Feed rate for printing (not sure but I think the units on this are millimeters per minute)
            const double printFeedRate = 1200.000;

            const int exponent = 7; // Power of 2 for which we are generating a hilbert curve
            GenerateHilbertCurve(exponent, out int[] xCoords, out int[] yCoords, out int sideLenMain);
            int nPtsMain = xCoords.Length;

            const int exponentIntro = 3; // Power of 2 for which we are generating the "intro" (and "outro") hilbert curve
            GenerateHilbertCurve(exponentIntro, out int[] xCoordsIntro, out int[] yCoordsIntro, out int sideLenIntro);
            int nPtsIntro = xCoordsIntro.Length;

            // Total number of Hilbert curve steps taken
            int totalHilberSteps = nLayers * nPtsMain + // Main pattern in each layer
                2 * nPtsIntro + // Intro and outro in first layer
                2; // Single step before intro and after outro in first layer

            // Extended pattern includes the "intro" and "outro" portions
            int[] xCoordsExtended = new int[nPtsMain + 2 * nPtsIntro];
            int[] yCoordsExtended = new int[nPtsMain + 2 * nPtsIntro];
            for (int idx = 0; idx < nPtsIntro; idx++)
            {
                xCoordsExtended[idx] = xCoordsIntro[nPtsIntro - 1 - idx];
                yCoordsExtended[idx] = -1 - yCoordsIntro[nPtsIntro - 1 - idx];
            }
            for (int idx = 0; idx < nPtsMain; idx++)
            {
                xCoordsExtended[idx + nPtsIntro] = xCoords[idx];
                yCoordsExtended[idx + nPtsIntro] = yCoords[idx];
            }
            for (int idx = 0; idx < nPtsIntro; idx++)
            {
                xCoordsExtended[idx + nPtsIntro + nPtsMain] = sideLenMain - 1 - xCoordsIntro[idx];
                yCoordsExtended[idx + nPtsIntro + nPtsMain] = -1 - yCoordsIntro[idx];
            }

            // Print time for just the print portions that are Hilbert-Curves
            double hilbertPrintTimeInMinutes = totalHilberSteps / oneMinuteInSteps;
            Console.WriteLine("Grid size (mm): " + (sideLenMain * gridStep).ToString());

            #region Determine total print time

            double preDistanceFirstLayer = 0;
            double xMM = Math.Round(xStart + gridStep * (xCoordsExtended[0] + 2), 3); // Align x one "Hilbert Step" past the start
            double yMM = Math.Round(yStart - gridStep * sideLenIntro - borderPadding, 3); // Align y on the bottom border
            double xMMPrev = xMM;
            double yMMPrev = yMM;
            xMM = Math.Round(xStart + gridStep * (sideLenMain + sideLenIntro - 1) + borderPadding, 3); // Move x to the right of the border
            double distanceTraveled = Math.Sqrt(Math.Pow(xMM - xMMPrev, 2) + Math.Pow(yMM - yMMPrev, 2));
            preDistanceFirstLayer += distanceTraveled;
            xMMPrev = xMM;
            yMMPrev = yMM;

            yMM = Math.Round(yStart + gridStep * (sideLenMain + sideLenIntro - 1) + borderPadding, 3); // Move y to the top of the border
            distanceTraveled = Math.Sqrt(Math.Pow(xMM - xMMPrev, 2) + Math.Pow(yMM - yMMPrev, 2));
            preDistanceFirstLayer += distanceTraveled;
            xMMPrev = xMM;
            yMMPrev = yMM;

            xMM = Math.Round(xStart - gridStep * sideLenIntro - borderPadding, 3); // Move x to the left of the border
            distanceTraveled = Math.Sqrt(Math.Pow(xMM - xMMPrev, 2) + Math.Pow(yMM - yMMPrev, 2));
            preDistanceFirstLayer += distanceTraveled;
            xMMPrev = xMM;
            yMMPrev = yMM;

            yMM = Math.Round(yStart - gridStep * sideLenIntro - borderPadding, 3); // Move y to the bottom of the border
            distanceTraveled = Math.Sqrt(Math.Pow(xMM - xMMPrev, 2) + Math.Pow(yMM - yMMPrev, 2));
            preDistanceFirstLayer += distanceTraveled;
            xMMPrev = xMM;
            yMMPrev = yMM;

            xMM = Math.Round(xStart + gridStep * (xCoordsExtended[0] + 1), 3); // Move x to the pattern start
            distanceTraveled = Math.Sqrt(Math.Pow(xMM - xMMPrev, 2) + Math.Pow(yMM - yMMPrev, 2));
            preDistanceFirstLayer += distanceTraveled;
            xMMPrev = xMM;
            yMMPrev = yMM;

            yMM = Math.Round(yStart + gridStep * yCoordsExtended[0], 3); // Move y to the pattern start
            distanceTraveled = Math.Sqrt(Math.Pow(xMM - xMMPrev, 2) + Math.Pow(yMM - yMMPrev, 2));
            preDistanceFirstLayer += distanceTraveled;

            double postDistanceFirstLayer = 0;
            xMM = Math.Round(xStart + gridStep * (xCoordsExtended[xCoordsExtended.Length - 1] - 1), 3);
            xMMPrev = xMM;
            yMMPrev = Math.Round(yStart + gridStep * (yCoordsExtended[yCoordsExtended.Length - 1]), 3);
            yMM = Math.Round(yStart - gridStep * (sideLenIntro - 1) - borderPadding, 3); // Move y to the bottom of the border minus one "Hilbert Step"
            distanceTraveled = Math.Sqrt(Math.Pow(xMM - xMMPrev, 2) + Math.Pow(yMM - yMMPrev, 2));
            postDistanceFirstLayer += distanceTraveled;

            double totalMinutes = hilbertPrintTimeInMinutes + // Print time for the Hilbert Curve
                (preDistanceFirstLayer + postDistanceFirstLayer) / printFeedRate + // Add time to draw border pattern
                2 * introLineMinutes; // add time for each drawing of the intro lines

            int printMinutes = (int)Math.Round(totalMinutes);
            int printHours = 0;
            while (printMinutes >= 60)
            {
                printHours++;
                printMinutes -= 60;
            }

            #endregion Determine total print time

            string outputFilename = "HilbertCurve_" + layerHeightText + "_PLA_MK3S_" + printHours.ToString() + "h" + printMinutes + "m.gcode";

            // Used for doing the 90 degree rotations of the Hilbert Curve from one layer to the next
            int[] cosines = new int[] { 1, 0, -1, 0 };
            int[] sines = new int[] { 0, 1, 0, -1 };

            string outputPath = Path.Combine(outputFolder, outputFilename);
            using (StreamWriter sw = new StreamWriter(outputPath))
            {
                #region Initial G-Code

                sw.WriteLine("; Custom gcode file generated from C# code");
                sw.WriteLine("; filament extrusion speed = " + extrusionRate.ToString());
                sw.WriteLine("");
                sw.WriteLine("M201 X9000 Y9000 Z500 E10000 ; sets maximum accelerations, mm/sec^2");
                sw.WriteLine("M203 X500 Y500 Z12 E120 ; sets maximum feedrates, mm/sec");
                sw.WriteLine("M204 P1500 R1500 T1500 ; sets acceleration (P, T) and retract acceleration (R), mm/sec^2");
                sw.WriteLine("M205 X10.00 Y10.00 Z0.20 E2.50 ; sets the jerk limits, mm/sec");
                sw.WriteLine("M205 S0 T0 ; sets the minimum extruding and travel feed rate, mm/sec");
                sw.WriteLine("M107");
                sw.WriteLine("M115 U3.3.1 ; tell printer latest fw version");
                sw.WriteLine("M201 X1000 Y1000 Z1000 E9000 ; sets maximum accelerations, mm/sec^2");
                sw.WriteLine("M203 X200 Y200 Z12 E120 ; sets maximum feedrates, mm/sec");
                sw.WriteLine("M204 S1250 T1250 ; sets acceleration (S) and retract acceleration (T)");
                sw.WriteLine("M205 X8 Y8 Z0.4 E1.5 ; sets the jerk limits, mm/sec");
                sw.WriteLine("M205 S0 T0 ; sets the minimum extruding and travel feed rate, mm/sec");
                sw.WriteLine("M83  ; extruder relative mode");
                sw.WriteLine("M104 S215 ; set extruder temp");
                sw.WriteLine("M140 S60 ; set bed temp");
                sw.WriteLine("M190 S60 ; wait for bed temp");
                sw.WriteLine("M109 S215 ; wait for extruder temp");
                sw.WriteLine("G28 W ; home all without mesh bed level");
                sw.WriteLine("G80 ; mesh bed leveling");
                sw.WriteLine("G1 Y-3.0 F1000.0 ; go outside print area");
                sw.WriteLine("G92 E0.0");
                sw.WriteLine("G1 X60.0 E9.0  F1000.0 ; intro line");
                sw.WriteLine("M73 Q0 S" + ((int)Math.Round(totalMinutes)).ToString() + " ; updating progress display (0% done, " + ((int)Math.Round(totalMinutes)).ToString() + " minutes remaining)");
                sw.WriteLine("M73 P0 R" + ((int)Math.Round(totalMinutes)).ToString() + " ; updating progress display (0% done, " + ((int)Math.Round(totalMinutes)).ToString() + " minutes remaining)");
                sw.WriteLine("G1 X100.0 E12.5  F1000.0 ; intro line");
                sw.WriteLine("G92 E0.0");
                sw.WriteLine("M221 S95");
                sw.WriteLine("M900 K30; Filament gcode");
                sw.WriteLine("G21 ; set units to millimeters");
                sw.WriteLine("G90 ; use absolute coordinates");
                sw.WriteLine("M83 ; use relative distances for extrusion");
                sw.WriteLine(";BEFORE_LAYER_CHANGE");
                sw.WriteLine("G92 E0.0");
                sw.WriteLine(";0.2");
                sw.WriteLine("");
                sw.WriteLine("");
                sw.WriteLine("G1 E-0.80000 F2100.00000 ; retract filament");
                sw.WriteLine("G1 Z0.600 F10800.000 ; lift tip up");
                sw.WriteLine(";AFTER_LAYER_CHANGE");
                sw.WriteLine(";0.2");
                xMM = Math.Round(xStart + gridStep * (xCoordsExtended[0] + 2), 3); // Align x one "Hilbert Step" past the start
                yMM = Math.Round(yStart - gridStep * sideLenIntro - borderPadding, 3); // Align y on the bottom border
                sw.WriteLine("G1 X" + string.Format("{0,1:F3}", xMM) + " Y" + string.Format("{0,1:F3}", yMM) + " ; go to extrusion start position");
                xMMPrev = xMM;
                yMMPrev = yMM;
                sw.WriteLine("G1 Z" + string.Format("{0,1:F3}", Math.Round(layerHeight, 3)) + " ; bring tip back down to first layer");
                sw.WriteLine("G1 E0.80000 F2100.00000 ; ready filament");
                sw.WriteLine("M204 S1000");
                sw.WriteLine("G1 F" + string.Format("{0,1:F3}", printFeedRate) + " ; restore feed rate to that used for printing (not sure but I think the units on this are millimeters per minute)");
                double minutesElapsed = introLineMinutes;
                int minutesRemaining = (int)Math.Round(totalMinutes - minutesElapsed);
                int pctDone = (int)Math.Round(100 * minutesElapsed / totalMinutes);
                sw.WriteLine("M73 Q" + pctDone.ToString() + " S" + minutesRemaining.ToString() + " ; updating progress display (" + pctDone.ToString() + "% done, " + minutesRemaining.ToString() + " minutes remaining)");
                sw.WriteLine("M73 P" + pctDone.ToString() + " R" + minutesRemaining.ToString() + " ; updating progress display (" + pctDone.ToString() + "% done, " + minutesRemaining.ToString() + " minutes remaining)");
                sw.WriteLine("");

                sw.WriteLine("; Draw border");
                xMM = Math.Round(xStart + gridStep * (sideLenMain + sideLenIntro - 1) + borderPadding, 3); // Move x to the right of the border
                distanceTraveled = Math.Sqrt(Math.Pow(xMM - xMMPrev, 2) + Math.Pow(yMM - yMMPrev, 2));
                sw.WriteLine("G1 X" + string.Format("{0,1:F3}", xMM) + " Y" + string.Format("{0,1:F3}", yMM) + " E" + string.Format("{0,1:F5}", Math.Round(extrusionRate * distanceTraveled, 5)));
                xMMPrev = xMM;
                yMMPrev = yMM;

                yMM = Math.Round(yStart + gridStep * (sideLenMain + sideLenIntro - 1) + borderPadding, 3); // Move y to the top of the border
                distanceTraveled = Math.Sqrt(Math.Pow(xMM - xMMPrev, 2) + Math.Pow(yMM - yMMPrev, 2));
                sw.WriteLine("G1 X" + string.Format("{0,1:F3}", xMM) + " Y" + string.Format("{0,1:F3}", yMM) + " E" + string.Format("{0,1:F5}", Math.Round(extrusionRate * distanceTraveled, 5)));
                xMMPrev = xMM;
                yMMPrev = yMM;

                xMM = Math.Round(xStart - gridStep * sideLenIntro - borderPadding, 3); // Move x to the left of the border
                distanceTraveled = Math.Sqrt(Math.Pow(xMM - xMMPrev, 2) + Math.Pow(yMM - yMMPrev, 2));
                sw.WriteLine("G1 X" + string.Format("{0,1:F3}", xMM) + " Y" + string.Format("{0,1:F3}", yMM) + " E" + string.Format("{0,1:F5}", Math.Round(extrusionRate * distanceTraveled, 5)));
                xMMPrev = xMM;
                yMMPrev = yMM;

                yMM = Math.Round(yStart - gridStep * sideLenIntro - borderPadding, 3); // Move y to the bottom of the border
                distanceTraveled = Math.Sqrt(Math.Pow(xMM - xMMPrev, 2) + Math.Pow(yMM - yMMPrev, 2));
                sw.WriteLine("G1 X" + string.Format("{0,1:F3}", xMM) + " Y" + string.Format("{0,1:F3}", yMM) + " E" + string.Format("{0,1:F5}", Math.Round(extrusionRate * distanceTraveled, 5)));
                xMMPrev = xMM;
                yMMPrev = yMM;

                xMM = Math.Round(xStart + gridStep * (xCoordsExtended[0] + 1), 3); // Move x to the pattern start
                distanceTraveled = Math.Sqrt(Math.Pow(xMM - xMMPrev, 2) + Math.Pow(yMM - yMMPrev, 2));
                sw.WriteLine("G1 X" + string.Format("{0,1:F3}", xMM) + " Y" + string.Format("{0,1:F3}", yMM) + " E" + string.Format("{0,1:F5}", Math.Round(extrusionRate * distanceTraveled, 5)));
                xMMPrev = xMM;
                yMMPrev = yMM;

                yMM = Math.Round(yStart + gridStep * yCoordsExtended[0], 3); // Move y to the pattern start
                distanceTraveled = Math.Sqrt(Math.Pow(xMM - xMMPrev, 2) + Math.Pow(yMM - yMMPrev, 2));
                sw.WriteLine("G1 X" + string.Format("{0,1:F3}", xMM) + " Y" + string.Format("{0,1:F3}", yMM) + " E" + string.Format("{0,1:F5}", Math.Round(extrusionRate * distanceTraveled, 5)));

                // Update progress display
                minutesElapsed = introLineMinutes + preDistanceFirstLayer / printFeedRate;
                minutesRemaining = (int)Math.Round(totalMinutes - minutesElapsed);
                pctDone = (int)Math.Round(100 * minutesElapsed / totalMinutes);
                sw.WriteLine("M73 Q" + pctDone.ToString() + " S" + minutesRemaining.ToString() + " ; updating progress display (" + pctDone.ToString() + "% done, " + minutesRemaining.ToString() + " minutes remaining)");
                sw.WriteLine("M73 P" + pctDone.ToString() + " R" + minutesRemaining.ToString() + " ; updating progress display (" + pctDone.ToString() + "% done, " + minutesRemaining.ToString() + " minutes remaining)");
                sw.WriteLine("; PURGING FINISHED");
                sw.WriteLine("");
                sw.WriteLine("; note: approximate bed center = (125, 105)");
                sw.WriteLine("");
                sw.WriteLine("; Hilbert Curve Pattern");

                #endregion Initial G-Code

                int patternEndX = 0;
                int patternEndY = 0;
                int step = 0;
                minutesRemaining = (int)Math.Round(hilbertPrintTimeInMinutes);
                int percentDone = 0;
                for (int layer = 0; layer < nLayers; layer++)
                {
                    LayerType currentLayerType = layer == 0 ? LayerType.FirstLayer :
                        layer == 1 ? LayerType.SecondLayer :
                        layer == nLayers - 1 ? LayerType.LastLayer :
                        LayerType.OtherLayer;

                    if (currentLayerType == LayerType.SecondLayer)
                    {
                        sw.WriteLine("M106 S255 ; turn on the fan"); // turn on the fan
                    }

                    int nVertices = currentLayerType == LayerType.FirstLayer ?
                        nPtsMain + 2 * nPtsIntro + 2 : // first layer has both the intro and outro pattern, plus the single step at the beginning and end
                        nPtsMain; // otherwise we have neither

                    int xPrev = 0;
                    int yPrev = 0;
                    for (int vIdx = 0; vIdx < nVertices; vIdx++)
                    {
                        bool atStart = vIdx == 0; // are we at the start of the main pattern?
                        bool atEnd = vIdx == nPtsMain - 1; // are we at the end of the main pattern?
                        int xInit = vIdx < nPtsMain ? xCoords[vIdx] : 0; // typical internal layers have neither intro nor outro
                        int yInit = vIdx < nPtsMain ? yCoords[vIdx] : 0;
                        switch (currentLayerType)
                        {
                            case LayerType.FirstLayer: // first layer has both the intro and outro pattern, plus the single step at the beginning and end
                                if (vIdx == 0)
                                {
                                    xInit = xCoordsExtended[0] + 1;
                                    yInit = yCoordsExtended[0];
                                    atStart = false;
                                }
                                else if (vIdx == nVertices - 1)
                                {
                                    xInit = xCoordsExtended[nVertices - 3] - 1;
                                    yInit = yCoordsExtended[nVertices - 3];
                                    atEnd = false;
                                }
                                else
                                {
                                    xInit = xCoordsExtended[vIdx - 1];
                                    yInit = yCoordsExtended[vIdx - 1];
                                    atStart = vIdx == nPtsIntro + 1; // skip intro and first step to determine 'atStart'
                                    atEnd = vIdx == nPtsMain + nPtsIntro; // (+1 - 1) skip intro and first step to determine 'atEnd'
                                }
                                break;
                        }

                        // rotate entire pattern counterclockwise by 90 * layer degrees
                        // center of rotation will be: (sideLenMain - 1)/2 (both coordinates)
                        // rotatedX = x cos(t) - y sin(t)
                        // rotatedY = x sin(t) + y cos(t)
                        int xRel = 2 * xInit - (sideLenMain - 1); // x relative to center (doubled to keep it an integer)
                        int yRel = 2 * yInit - (sideLenMain - 1); // y relative to center (doubled to keep it an integer)
                        int rotatedX = xRel * cosines[layer % 4] - yRel * sines[layer % 4];
                        int rotatedY = xRel * sines[layer % 4] + yRel * cosines[layer % 4];
                        rotatedX = (rotatedX + sideLenMain - 1) / 2;
                        rotatedY = (rotatedY + sideLenMain - 1) / 2;

                        // Sanity check
                        if (atStart && // We're at the beginning of the main pattern
                            (rotatedX != patternEndX || rotatedY != patternEndY)) // we don't line up with the end of the previous main pattern
                        {
                            throw new Exception("Start of next layer did not match end of previous layer!");
                        }

                        // Are we at the first vertex of a new layer?
                        if (vIdx == 0)
                        {
                            // Extrude directly to the next layer (forming one continuous curve), but only for layers after the second layer
                            if (currentLayerType != LayerType.FirstLayer &&
                                currentLayerType != LayerType.SecondLayer)
                            {
                                // Sanity check
                                if (rotatedX != patternEndX || rotatedY != patternEndY) // if we're going to extrude directly up, _always_ check that (x, y) matches previous
                                {
                                    throw new Exception("Start of next layer did not match end of previous layer!");
                                }

                                sw.WriteLine("G1 Z" + string.Format("{0,1:F3}", Math.Round((layer + 1) * layerHeight, 3)) +
                                    " E" + string.Format("{0,1:F5}", Math.Round(extrusionRate * layerHeight, 5)));
                            }
                        }
                        else // For all other layers, extrude 1 step to the next vertex
                        {
                            // Sanity check (make sure we're only making exactly one step)
                            if (Math.Abs(rotatedX - xPrev) + Math.Abs(rotatedY - yPrev) != 1)
                            {
                                throw new Exception("Not making exactly 1 step!");
                            }

                            sw.WriteLine("G1 X" +
                                string.Format("{0,1:F3}", Math.Round(xStart + gridStep * rotatedX, 3)) +
                                " Y" +
                                string.Format("{0,1:F3}", Math.Round(yStart + gridStep * rotatedY, 3)) +
                                " E" +
                                string.Format("{0,1:F5}", Math.Round(extrusionRate * gridStep, 5)));
                        }

                        // Record the pattern end as needed
                        if (atEnd || // record the (x, y) hit at the end of the main pattern
                            (currentLayerType != LayerType.FirstLayer &&  // if we're going to extrude directly up on the next layer,
                            vIdx == nVertices - 1)) //  _always_ record the final (x, y) as the end of this layer, regarless of the state of 'atEnd'
                        {
                            patternEndX = rotatedX;
                            patternEndY = rotatedY;
                        }

                        // Add to the number of steps taken (used for updating the progress display)
                        step++;

                        // Update progress display, if needed
                        minutesElapsed = introLineMinutes + preDistanceFirstLayer / printFeedRate +
                            (currentLayerType == LayerType.FirstLayer ? 0 : introLineMinutes + postDistanceFirstLayer / printFeedRate) +
                            step / oneMinuteInSteps;
                        int nextMinutesRemaining = (int)Math.Round(totalMinutes - minutesElapsed);
                        int nextPercentDone = (int)Math.Round(100 * minutesElapsed / totalMinutes);
                        if (nextMinutesRemaining != minutesRemaining || nextPercentDone != percentDone)
                        {
                            minutesRemaining = nextMinutesRemaining;
                            percentDone = nextPercentDone;
                            sw.WriteLine("M73 Q" + percentDone.ToString() + " S" + minutesRemaining.ToString());
                            sw.WriteLine("M73 P" + percentDone.ToString() + " R" + minutesRemaining.ToString());
                        }

                        // Record the previous position
                        xPrev = rotatedX;
                        yPrev = rotatedY;
                    }

                    if (currentLayerType == LayerType.FirstLayer)
                    {
                        // Go back down to near the border, then raise Z to second layer and retract the filament
                        xMM = Math.Round(xStart + gridStep * (xCoordsExtended[nVertices - 3] - 1), 3);
                        xMMPrev = xMM;
                        yMMPrev = Math.Round(yStart + gridStep * (yCoordsExtended[nVertices - 3]), 3);
                        yMM = Math.Round(yStart - gridStep * (sideLenIntro - 1) - borderPadding, 3); // Move y to the bottom of the border minus one "Hilbert Step"
                        distanceTraveled = Math.Sqrt(Math.Pow(xMM - xMMPrev, 2) + Math.Pow(yMM - yMMPrev, 2));
                        sw.WriteLine("G1 X" + string.Format("{0,1:F3}", xMM) + " Y" + string.Format("{0,1:F3}", yMM) + " E" + string.Format("{0,1:F5}", Math.Round(extrusionRate * distanceTraveled, 5)));
                        xMMPrev = xMM;
                        yMMPrev = yMM;

                        sw.WriteLine("G1 Z" + string.Format("{0,1:F3}", Math.Round(2 * layerHeight, 3)));
                        sw.WriteLine("G1 E-0.80000 F2100.00000; retract filament");

                        // Update progress display
                        minutesElapsed = introLineMinutes + (preDistanceFirstLayer + postDistanceFirstLayer) / printFeedRate + step / oneMinuteInSteps;
                        minutesRemaining = (int)Math.Round(totalMinutes - minutesElapsed);
                        percentDone = (int)Math.Round(100 * minutesElapsed / totalMinutes);
                        sw.WriteLine("M73 Q" + percentDone.ToString() + " S" + minutesRemaining.ToString());
                        sw.WriteLine("M73 P" + percentDone.ToString() + " R" + minutesRemaining.ToString());

                        // Change filament
                        sw.WriteLine("M600");

                        // Repeat the intro line, but offset 3 mm from the first one
                        sw.WriteLine("G28 W ; home all without mesh bed level");
                        sw.WriteLine("G1 Y0.0 F1000.0 ; go outside print area");
                        sw.WriteLine("G1 E0.80000 F2100.00000; ready filament");
                        sw.WriteLine("G92 E0.0");
                        sw.WriteLine("G1 X60.0 E9.0  F1000.0 ; intro line");
                        sw.WriteLine("G1 X100.0 E12.5  F1000.0 ; intro line");
                        sw.WriteLine("G92 E0.0");
                        sw.WriteLine("G1 E-0.80000 F2100.00000 ; retract filament");
                        sw.WriteLine("G1 Z0.800 F10800.000 ; lift tip up");

                        // Go to layer 2 starting position and ready filament
                        int xRel = 2 * xCoords[0] - (sideLenMain - 1); // x relative to center (doubled to keep it an integer)
                        int yRel = 2 * yCoords[0] - (sideLenMain - 1); // y relative to center (doubled to keep it an integer)
                        int rotatedX = xRel * cosines[1] - yRel * sines[1];
                        int rotatedY = xRel * sines[1] + yRel * cosines[1];
                        rotatedX = (rotatedX + sideLenMain - 1) / 2;
                        rotatedY = (rotatedY + sideLenMain - 1) / 2;
                        xMM = Math.Round(xStart + gridStep * rotatedX, 3);
                        yMM = Math.Round(yStart + gridStep * rotatedY, 3);
                        distanceTraveled = Math.Sqrt(Math.Pow(xMM - xMMPrev, 2) + Math.Pow(yMM - yMMPrev, 2));
                        sw.WriteLine("G1 Z" + string.Format("{0,1:F3}", Math.Round(2 * layerHeight, 3)));
                        sw.WriteLine("G1 X" + string.Format("{0,1:F3}", xMM) + " Y" + string.Format("{0,1:F3}", yMM));
                        sw.WriteLine("G1 E0.80000 F2100.00000; ready filament");
                        sw.WriteLine("M204 S1000");
                        sw.WriteLine("G1 F" + string.Format("{0,1:F3}", printFeedRate) + " ; restore feed rate to that used for printing");

                        // Update progress display
                        minutesElapsed = 2 * introLineMinutes + (preDistanceFirstLayer + postDistanceFirstLayer) / printFeedRate + step / oneMinuteInSteps;
                        minutesRemaining = (int)Math.Round(totalMinutes - minutesElapsed);
                        percentDone = (int)Math.Round(100 * minutesElapsed / totalMinutes);
                        sw.WriteLine("M73 Q" + percentDone.ToString() + " S" + minutesRemaining.ToString());
                        sw.WriteLine("M73 P" + percentDone.ToString() + " R" + minutesRemaining.ToString());
                    }
                }

                sw.WriteLine("G1 Z" + string.Format("{0,1:F3}", Math.Round((nLayers + 1) * layerHeight, 3)));
                sw.WriteLine("G1 E-0.80000 F2100.00000 ; retract filament");
                sw.WriteLine("G1 Z" + string.Format("{0,1:F3}", Math.Round((nLayers + 1) * layerHeight, 3) + 10) + " F10800.000 ; lift tip up 10 more mm");
                sw.WriteLine("");
                sw.WriteLine("M73 Q100 S0 ; updating progress display (100% done, 0 minutes remaining)");
                sw.WriteLine("M73 P100 R0 ; updating progress display (100% done, 0 minutes remaining)");
                sw.WriteLine("");
                sw.WriteLine("; Filament-specific end gcode");
                sw.WriteLine("M221 S100");
                sw.WriteLine("M104 S0 ; turn off temperature");
                sw.WriteLine("M140 S0 ; turn off heatbed");
                sw.WriteLine("M107 ; turn off fan");
                sw.WriteLine("G1 Z30.8 ; Move print head up");
                sw.WriteLine("G1 X0 Y200; home X axis");
                sw.WriteLine("M84 ; disable motors");
            }
        }

        static void GenerateHilbertCurve(int exponent, out int[] xCoords, out int[] yCoords, out int sideLen)
        {
            sideLen = 1 << exponent;
            int n = sideLen * sideLen;

            xCoords = new int[n];
            yCoords = new int[n];
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
