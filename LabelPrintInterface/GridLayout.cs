using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace LabelPrintInterface
{
    public class GridLayout
    {
        public int marginLeft = 0;
        public int marginTop = 0;
        public int availableLabelWidth = 0;
        public int availableLabelHeight = 0;
        public int xGapBetweenLabels = 0;
        public int yGapBetweenLabels = 0;
        public int colsPerRow = 0;
        public int totalRows = 0;
      
        private int finalXOffset;
        private int finalYOffset;
        private int totalLabelsPerSheet;

        public string resizeType;
        public int clockwiseDegreeShift = 0;

        public List<Point> lstPoint = new List<Point>();
        public GridLayout(int marginleft, int margintop, int availablelabelwidth, int availablelabelheight, int xgapbetweenlabels, int ygapbetweenlabels, int colsperrow, int totalrows, int clockwisedegreeshift = 0, string resizemode="none")
        {
            marginLeft = marginleft;
            marginTop = margintop;
            availableLabelWidth = availablelabelwidth;
            availableLabelHeight = availablelabelheight;
            xGapBetweenLabels = xgapbetweenlabels;
            yGapBetweenLabels = ygapbetweenlabels;
            colsPerRow = colsperrow;
            totalRows = totalrows;
          
            totalLabelsPerSheet = totalRows * colsPerRow;

            clockwiseDegreeShift = clockwisedegreeshift;
           
            resizeType = resizemode;
            for (int i = 0; i < totalLabelsPerSheet; i++)
            {

                lstPoint.Add(CalculateFinalOffsetByIndex(i));
            }
        }
        private Point CalculateFinalOffsetByIndex(int index)
        {
            int pageIndex = index % totalLabelsPerSheet; //normalized for multiple pages
                                                         // if (pageIndex == 0) { pageIndex = totalLabelsPerSheet; }
                                                         //if first row
            int columnIndex = (pageIndex) % colsPerRow;//0 based
            finalXOffset = marginLeft + columnIndex * (availableLabelWidth + xGapBetweenLabels);
            //second find out what row
            int rowIndex = Convert.ToInt32(Math.Floor(pageIndex / (decimal)colsPerRow));
            finalYOffset = marginTop + rowIndex * (availableLabelHeight + yGapBetweenLabels);
            return new Point(finalXOffset, finalYOffset);
        }
        public Point GetPositionForGridIndex(int i)
        {
            int pageIndex = i % totalLabelsPerSheet;
            return lstPoint[pageIndex];
        }
    }
}
