using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabelPrintInterface
{
    public class Page
    {
        public Page(int pgIndx)
        {
            isLast = true;
            index = pgIndx;
        }

        // public List<ImageItem> lstImg = new List<ImageItem>();
        // public List<TextItem> lstTxt = new List<TextItem>();
        public List<BCLabel> lstLabel = new List<BCLabel>();
        private int index;
        public bool isLast;
        public PageDefinition pgDefinition;
        public int pageNumber { get { return index + 1; } }
        public int pageIndex { get { return index; } }
    }
}
