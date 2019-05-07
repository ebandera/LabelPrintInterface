using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabelPrintInterface
{
    public class BCLabel : ICloneable
    {
        public BCLabel(int i, int? lbwidth = null, int? lbheight = null, int? tpwidth = null, int? tpheight = null, int pX = 0, int pY = 0)
        {
            index = i;
            width = lbwidth;
            height = lbheight;
            templatewidth = tpwidth;
            templateheight = tpheight;
            positionX = pX;
            positionY = pY;

        }
        private BCLabel()
        {

        }

        public List<ImageItem> lstImg = new List<ImageItem>();
        public List<TextItem> lstTxt = new List<TextItem>();
        public int index;
        public int? height;
        public int? width;
        public int? templateheight;
        public int? templatewidth;
        public int positionX;
        public int positionY;

        public object Clone()
        {
            BCLabel clone = new BCLabel();
            clone.index = index;
            clone.height = height;
            clone.width = width;
            clone.height = height;
            clone.templatewidth = templatewidth;
            clone.templateheight = templateheight;
            clone.positionX = positionX;
            clone.positionY = positionY;

            foreach (TextItem ti in lstTxt)
            {
                clone.lstTxt.Add((TextItem)ti.Clone());
                // if (ti.labelIndex > intLabelCount) { intLabelCount = ti.labelIndex; }//dont like
            }
            foreach (ImageItem ii in lstImg)
            {
                clone.lstImg.Add((ImageItem)ii.Clone());
                // if (ii.labelIndex > intLabelCount) { intLabelCount = ii.labelIndex; }//dont like
            }
            return clone;
        }
    }
}
