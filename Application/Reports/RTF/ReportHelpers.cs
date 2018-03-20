﻿using CoreSampleAnnotation.AnnotationPlane.LayerBoundaries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreSampleAnnotation.Reports.RTF
{
    public class PropertyDescription
    {
        public string Name { get; private set; }
        public string[] Values { get; private set; }
        public string Comment { get; private set; }

        public PropertyDescription(string name, string[] values, string comment = "") {
            Name = name;
            Values = values;
            Comment = comment;
        }
    }

    public class LayerDescrition {
        public PropertyDescription[] Properties { get; private set; }
        public LayerDescrition(PropertyDescription[] properties) {
            Properties = properties;
        }
    }


    public class LayerBoundary {
        public int OrderNumber { get; private set; }

        public double Depth { get; private set; }

        public int Rank { get; private set; }
        
        public LayerBoundary(int orderNumber, double depth, int rank) {
            OrderNumber = orderNumber;
            Depth = depth;
            Rank = rank;
        }
    }

    public static class ReportHelpers
    {
        private static int LeftColWidth = 7000;
        private static int RightcolWidth = 3000;

        /// <summary>
        /// Generates the row depicting extraction interval
        /// </summary>
        /// <param name="upperDepth">in meters (positive)</param>
        /// <param name="lowerDepth">in meters (positive)</param>
        /// <param name="extractedLength">in meters (positive)</param>
        /// <returns></returns>
        public static ReportRow GetIntervalRow(double upperDepth, double lowerDepth, double extractedLength) {
            return new ReportRow(new TextCell[] {
                        new TextCell(string.Format("Интервал {0:0.#}-{1:0.##}/{2:0.##} м (присутствует {3:0.##} м)",upperDepth,lowerDepth,lowerDepth-upperDepth,extractedLength),LeftColWidth,isBold: true),
                        new TextCell("",RightcolWidth)
            });
        }

        /// <summary>
        /// Generates the row depicting group of layers with specific rank
        /// </summary>
        /// <param name="length">group total length (in meters)</param>
        /// <returns></returns>
        public static ReportRow GetRankDescrRow(int orderNumber, string rankName, double length) {
            return new ReportRow(new TextCell[] {
                        new TextCell(string.Format("{0} {1} ({2:0.## м})",orderNumber,rankName,length),LeftColWidth, isBold: true),
                        new TextCell("",RightcolWidth)
            });
        }

        /// <summary>
        /// Generates row deorcting specific layer
        /// </summary>        
        /// <param name="length">in meters</param>        
        /// <returns></returns>
        public static ReportRow GetLayerDescrRow(int orderNum, double length, LayerDescrition description) {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("{0} слой ({1:0.##} м)", orderNum, length);
            foreach (PropertyDescription property in description.Properties)
            {
                if (property.Values == null)
                    continue;
                sb.AppendFormat(". {0} - {1}", property.Name, string.Join(", ", property.Values.Select( v => v.ToLower())));
                if (!string.IsNullOrEmpty(property.Comment)) {
                    sb.AppendFormat(". {0}", property.Comment);
                }                
            }
            sb.Append(".");

            return new ReportRow(new TextCell[] {
                        new TextCell(sb.ToString(),LeftColWidth),
                        new TextCell("",RightcolWidth)
                    }
                );

        }

        /// <summary>
        /// Forms a table by filling it with supplied data
        /// </summary>
        /// <param name="intervals"></param>
        /// <param name="boundaries">including the "outer boundaries" of bounding layer</param>    
        /// <param name="layers"></param>
        /// <param name="isDepthIncreases">true if depth increases (other paarameter arrays are sorted so the depth increases)</param>
        /// <returns></returns>
        public static ReportTable GenerateTableContents(
        Intervals.BoreIntervalVM[] intervals,
        LayerBoundary[] boundaries,
        LayerDescrition[] layers,
        string[] rankNames,
        bool isDepthIncreases)
        {
            //asserting intervals
            //lower - upper bounds order
            //intersection check
            List<KeyValuePair<double, int>> intBounds = new List<KeyValuePair<double, int>>();
            for (int i = 0; i < intervals.Length; i++)
            {
                Intervals.BoreIntervalVM interval = intervals[i];
                if (interval.LowerDepth < interval.UpperDepth)
                    throw new ArgumentException("The upper bound is lower than lower bound");
                intBounds.Add(new KeyValuePair<double, int>(interval.UpperDepth, 1)); // +1 opens the interval
                intBounds.Add(new KeyValuePair<double, int>(interval.LowerDepth, -1)); // -1 closes the interval
            }
            // detecting intervals with touching bounds; removing these bounds
            double[] keys = intBounds.Select(kvp => kvp.Key).ToArray();            
            foreach (int key in keys)
            {
                int curKeyValsSum = intBounds.Where(kvp => kvp.Key == key).Sum(kvp => kvp.Value);
                if (curKeyValsSum == 0) {
                    intBounds = intBounds.Where(kvp => kvp.Key != key).ToList();
                }
            }

            int[] events = intBounds.OrderBy(kvp => kvp.Key).Select(kvp => kvp.Value).ToArray();
            int sum = 0;
            for (int i = 0; i < events.Length; i++)
            {
                sum += events[i];
                if (Math.Abs(sum) > 1)
                    throw new InvalidOperationException("You've passed intersecting intervals");
            }


            List<ReportRow> rows = new List<ReportRow>();

            if (boundaries.Length - 1 != layers.Length)
                throw new ArgumentException("Layers count must be exactly one more than boundary count");

            ReportRow header = new ReportRow(
                new TextCell[] {
                new TextCell("Описание керна сверху вниз. Интервал / выход керна в м", LeftColWidth, horizontalAlignement:TextAlignement.Centered, isBold:true),
                new TextCell("№ образца/место отбора от начала керна, м", RightcolWidth, horizontalAlignement: TextAlignement.Centered, isBold: true)
                });

            rows.Add(header);
            
            //for now works with increasing depths order
            if (isDepthIncreases)
            {
                intervals = intervals.OrderBy(i => i.UpperDepth).ToArray();
                for (int i = 0; i < boundaries.Length - 1; i++)
                    if (boundaries[i+1].Depth < boundaries[i].Depth)
                        throw new NotSupportedException("Boundary depths must increase");

                int reportedInIndex = -1;
                int inIndex = 0;
                int laIndex = 0;                
                int layerOrderNum = 1;               
                while (inIndex < intervals.Length && laIndex < layers.Length) {
                    if (reportedInIndex != inIndex) {
                        //adding row depicting interval start
                        rows.Add(GetIntervalRow(intervals[inIndex].UpperDepth, intervals[inIndex].LowerDepth, intervals[inIndex].ExtractedLength));
                        reportedInIndex = inIndex;
                    }
                    LayerBoundary upperLabound = boundaries[laIndex];
                    double curLaUpper = upperLabound.Depth;
                    double curLaLower = boundaries[laIndex + 1].Depth; //boundraies array always contains one more element than layers
                    double curIntUpper = intervals[inIndex].UpperDepth;
                    double curIntLower = intervals[inIndex].UpperDepth + intervals[inIndex].ExtractedLength;
                    bool doInInc = false;
                    bool doLaInc = false;
                    bool skipReporting = false;

                    //analysing the relative position of the interval and the layer
                    if (curIntLower >= curLaLower)
                    {
                        //the interval includes the end of the layer or interval is entirly below the layer
                        if (curIntUpper > curLaLower)
                        {
                            //the entire interal is below the layer
                            skipReporting = true; //the layer is not reported
                        }
                        else
                        {
                            if (curIntUpper > curLaUpper)
                            {
                                //coersing layer upper bound to match interval
                                curLaUpper = curIntUpper;
                            }
                        }
                        doLaInc = true;
                    }
                    else {
                        //the layer includes the end of the interval
                        curLaLower = curIntLower; //coersing layer lower bound to match interval
                        if (curIntUpper > curLaUpper) {
                            //coersing layer upper bound to match interval
                            curLaUpper = curIntUpper;
                        }

                        doInInc = true;
                    }

                    if (!skipReporting)
                    {
                        if (upperLabound.Rank > 0)
                        {
                            //we need to add rank row
                            for (int rank = upperLabound.Rank; rank > 0; rank--)
                            {
                                double length = 0.0;
                                //calculating total length. starting from cur bound till the next bound of the same rank or higher
                                for (int i = laIndex + 1; i < boundaries.Length; i++)
                                {
                                    int curRank = boundaries[i].Rank;
                                    length += boundaries[i].Depth - boundaries[i - 1].Depth;
                                    if (curRank >= rank)
                                        break;
                                }
                                int curOrder = 1;
                                if (rank == upperLabound.Rank) //the outer (highest) rank is kept. All smaller ranks reset to 1
                                    curOrder = upperLabound.OrderNumber;
                                length = Math.Min(length, curIntLower - upperLabound.Depth);
                                rows.Add(GetRankDescrRow(curOrder, rankNames[rank], length));
                            }
                            layerOrderNum = 1; //ranks higher than 0 reset the numbering of layers
                        }
                        rows.Add(GetLayerDescrRow(layerOrderNum++, curLaLower - curLaUpper, layers[laIndex]));
                    }
                    if(doLaInc)
                        laIndex++;
                    if (doInInc)
                        inIndex++;
                }                
            }

            return new ReportTable(rows.ToArray());
        }
    }
}
