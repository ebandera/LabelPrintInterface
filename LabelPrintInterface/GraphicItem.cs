using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabelPrintInterface
{
    public class GraphicItem
    {
        public string tagName;
        
        public int positionX;
        public int positionY;
        public int itemWidth;
        public int itemHeight;
        public bool hasBorder;
        public int lbposx;
        public int lbposy;
        public string type;//to support xml node reading
        public string location;//to support xml node reading
        public string value;//to support xml node reading
        public GraphicItem(string tag, int posX, int posY, int width, int height, bool brdr)
        {
            tagName = tag;
            positionX = posX;
            positionY = posY;
            lbposx = posX;
            lbposy = posY;
            itemWidth = width;
            itemHeight = height;
            hasBorder = brdr;
          
        
        }
        public GraphicItem()
        {

        }
    }
}
