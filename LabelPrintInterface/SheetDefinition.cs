using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace LabelPrintInterface
{
    public class SheetDefinition:PageDefinition
    {
        //public int marginLeft = 0;
        //public int marginTop = 0;
        //public int availableLabelWidth = 0;
        //public int availableLabelHeight = 0;
        //public int xGapBetweenLabels = 0;
        //public int yGapBetweenLabels = 0;
        //public int colsPerRow = 0;
        //public int totalRows = 0;
        //public int clockwiseDegreeShift = 0;
        //private int finalXOffset;
        //private int finalYOffset;
       // private int totalLabelsPerSheet;
        //public string resizeType;
      //  public GridLayout grid;
       // private List<Point> lstPoint = new List<Point>();
        //public GridLayout gridLayout;
        public SheetDefinition(int marginleft, int margintop, int availablelabelwidth, int availablelabelheight, int xgapbetweenlabels, int ygapbetweenlabels, int colsperrow, int totalrows, int clockwisedegreeshift, string resizemode):base(marginleft,margintop,availablelabelwidth,availablelabelheight,xgapbetweenlabels,ygapbetweenlabels,colsperrow,totalrows,clockwisedegreeshift,resizemode)
        {
            
           
        }
        public SheetDefinition(int totalLabels):base(totalLabels)
        {
           
        }

        //public void GridOutPosition(GridLayout gl, int startPageIndex,int endPageIndex, int startGridIndex=0)
        //{
        //    int gi = startGridIndex;
        //    gridLayout = gl;
        //    for(int i=startPageIndex;i<=endPageIndex;i++)
        //    {
        //       Point pt= gl.GetPositionForGridIndex(gi);
        //       SetIndexPosition(i, pt.X, pt.Y);
        //       gi++;
        //    }

        //}
        /// <summary>
        /// Will Apply positioning and pagination for the sheeting process based on the sheet parameters
        /// </summary>
        /// <param name="lp"></param>
        /// <returns></returns>
        public void ResetSheets(LabelPrint lp,Boolean paginate = true)
        {
            UpdateLabelPrintDelegate sheetingDelegate = new UpdateLabelPrintDelegate(ApplyOffsets);
            lp.UpdateLP(sheetingDelegate);
            if (paginate)
            {
                UpdateLabelPrintDelegate paginationDelegate = new UpdateLabelPrintDelegate(Paginate);
                lp.UpdateLP(paginationDelegate);
            }

        }

        public void ApplyOffsets(LabelPrint lp)
        {
            foreach (Page pg in lp.lstPage)
            {
                foreach (BCLabel lb in pg.lstLabel)
                {
                    int pageIndex = lb.index % totalLabelsPerSheet;
                    lb.positionX = lstPoint[pageIndex].X;
                    lb.positionY = lstPoint[pageIndex].Y;
                    PageDefinition currentPageDef = this;
                    if (pg.pgDefinition != null) { currentPageDef = pg.pgDefinition; }
                    GridLayout currentGrid = gridLayout;
                    if(pg.pgDefinition != null && pg.pgDefinition.Grid != null) { currentGrid = pg.pgDefinition.Grid; }
                    if (currentGrid != null)
                    {//the distinction between label width and template label width is for rotating and resizing
                        lb.templatewidth = currentGrid.availableLabelWidth;
                        lb.templateheight = currentGrid.availableLabelHeight;
                        if (lb.width == null) { lb.width = currentGrid.availableLabelWidth; }
                        if (lb.height == null) { lb.height = currentGrid.availableLabelHeight; }
                    }
                    foreach (TextItem item in lb.lstTxt)
                    {
                        item.positionX += currentPageDef.lstPoint[pageIndex].X;
                        item.positionY += currentPageDef.lstPoint[pageIndex].Y;
                    }
                    foreach (ImageItem item in lb.lstImg)
                    {
                        item.positionX += currentPageDef.lstPoint[pageIndex].X;
                        item.positionY += currentPageDef.lstPoint[pageIndex].Y;
                    }
                }

            }
        }
        public void Paginate(LabelPrint lp)
        {
            // int intLabelCount = 0;
            //make deep copy of all the items on page 1, and clear out the first page of all text and image items
            List<BCLabel> lstLabel = new List<BCLabel>();

            foreach (BCLabel lb in lp.lstPage[0].lstLabel)
            {
                lstLabel.Add((BCLabel)lb.Clone());
                lb.lstTxt.Clear();
                lb.lstImg.Clear();
            }

            //determine the number of pages
            int intNumberOfPages = (int)Math.Ceiling(lp.labelCount / (decimal)totalLabelsPerSheet);
            //first add the labels to the page
            lp.SetBlankLabels(lp.labelCount, totalLabelsPerSheet);
            //for each item, add it to the right page and label
            foreach (BCLabel lb in lstLabel)
            {
                int pageNumber = (int)Math.Ceiling((lb.index + 1) / (decimal)totalLabelsPerSheet);
                int pageIndex = lb.index % totalLabelsPerSheet; //normalized for multiple pages  
                lp.lstPage[pageNumber - 1].lstLabel[pageIndex] = (BCLabel)lb.Clone();
            }                                             // lp.lstPage[pageNumber - 1].lstLabel[pageIndex].positionX = lb.positionX;


        }
        //private Point CalculateFinalOffsetByIndex(int index)
        //{
        //    int pageIndex = index % totalLabelsPerSheet; //normalized for multiple pages
        //                                                 // if (pageIndex == 0) { pageIndex = totalLabelsPerSheet; }
        //                                                 //if first row
        //    int columnIndex = (pageIndex) % colsPerRow;//0 based
        //    finalXOffset = marginLeft + columnIndex * (availableLabelWidth + xGapBetweenLabels);
        //    //second find out what row
        //    int rowIndex = Convert.ToInt32(Math.Floor(pageIndex / (decimal)colsPerRow));
        //    finalYOffset = marginTop + rowIndex * (availableLabelHeight + yGapBetweenLabels);
        //    return new Point(finalXOffset, finalYOffset);
        //}
        //public Point GetPositionForLabelIndex(int i)
        //{
        //    int pageIndex = i % totalLabelsPerSheet;
        //    return gridLayout.lstPoint[pageIndex];
        //}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="i"></param>
        //public void SetPageIndexCount(int i)
        //{
        //    int intCurrentIndexes = lstPoint.Count;
        //    for (int j = intCurrentIndexes; j < i; j++)
        //    {
        //        lstPoint.Add(new Point(0, 0));
        //    }
        //}
        /// <summary>
        /// Sets the position of the label index manually
        /// </summary>
        /// <param name="i">Index of the label on the sheet</param>
        /// <param name="xPos">X position of this label</param>
        /// <param name="yPos">Y position of this label</param>
        //public void SetIndexPosition(int i, int xPos, int yPos)
        //{
        //    try
        //    {
        //        if (i >= lstPoint.Count)//fill in the blanks if the index provided doesn't exist
        //        {
        //            SetPageIndexCount(i + 1);
        //        }
        //        lstPoint[i] = new Point(xPos, yPos);
        //    }
        //    catch
        //    {
        //        throw;
        //    }
           

        //}
    }
}
