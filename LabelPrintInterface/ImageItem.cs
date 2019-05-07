using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace LabelPrintInterface
{
    public class ImageItem:GraphicItem,ICloneable
    {
        public ImageItem(string tag, System.Drawing.Image im, int posX, int posY, int width, int height, string md, bool brdr):base(tag,posX,posY,width,height,brdr)
        {
           // tagName = tag;
            image = im;
          //  itemWidth = width;
          //  itemHeight = height;
            displayMode = md;
            
          //  positionX = posX;
          //  positionY = posY;
          //  lbposx = posX;
          //  lbposy = posY;

          //  border = brdr;

        }
        private ImageItem():base()
        {

        }
       // public string tagName;
        public Image image;
       // public int itemWidth;
       // public int itemHeight;
        public string displayMode;  //stretch, contain, zoom
        public string source;  //web or filesystem
        public int flipDegrees = 0;
       // public int positionX;
       // public int positionY;
       // public int lbposx;
       // public int lbposy;
       // public bool border;
        //  public int labelIndex;  //moving to 0 based
        public object Clone()
        {
            ImageItem clone = new ImageItem();
            clone.tagName = tagName;
            clone.image = image;
            clone.itemWidth = itemWidth;
            clone.itemHeight = itemHeight;
            clone.displayMode = displayMode;
            clone.positionX = positionX;
            clone.positionY = positionY;
            clone.lbposx = lbposx;
            clone.lbposy = lbposy;
            clone.hasBorder = hasBorder;
            return clone;

        }
    }
}
