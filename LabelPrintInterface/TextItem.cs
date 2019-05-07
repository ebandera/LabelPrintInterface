using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace LabelPrintInterface
{
    public class TextItem : GraphicItem, ICloneable
    {
        //public string tagName;
        public string text;
        public Font font;
        public FontStyle style;
        //public int positionX;
        //public int positionY;
        //public int itemWidth;
        //public int itemHeight;
        public string textAlignH;
        public string textAlignV;
        //public bool hasBorder;
        public bool hasFill;
        //some properties, not initally assigned, but help for positioning
        public int stringWidth;
        public int stringHeight;
        public int vOffset;
        public int hOffset;
        public bool blnMultiline = false;
        //public int lbposx;
        //public int lbposy;
        //  public int labelIndex;

        private TextItem():base()
        {

        }
        public TextItem(string tag, string txt, string fnt, int sz, string sty, int posX, int posY, int width, int height, string txtAlignH, string txtAlignV, bool brdr, bool fil):base(tag,posX,posY,width, height,brdr)
        {
            //apply values (except for font - handled second)
           // tagName = tag;
            text = txt;
           // positionX = posX;
           // positionY = posY;
           // lbposx = posX;
            //lbposy = posY;
            //itemWidth = width;
           // itemHeight = height;
            textAlignH = txtAlignH;
            textAlignV = txtAlignV;
           // hasBorder = brdr;
            hasFill = fil;
            //get font data to build a Font property with the fontFamily,Size, and style
            switch (sty)
            {
                case "bold":
                    style = FontStyle.Bold;
                    break;
                case "italic":
                    style = FontStyle.Italic;
                    break;
                case "underline":
                    style = FontStyle.Underline;
                    break;
                case "strikeout":
                    style = FontStyle.Strikeout;
                    break;
                case "regular":
                    style = FontStyle.Regular;
                    break;
                case "bolditalic":
                    style = FontStyle.Bold | FontStyle.Italic;
                    break;
                case "boldunderline":
                    style = FontStyle.Bold | FontStyle.Underline;
                    break;
                default:
                    style = FontStyle.Regular;
                    break;
            }
            font = new Font(fnt, sz, style);
            //  labelIndex = lblIndex;

        }
        public TextItem(string tag, string txt, string fnt, int sz, string sty, int posX, int posY, Graphics gra):base(tag,posX,posY,0,0,false)
        {
            //apply values (except for font - handled second)
           // tagName = tag;
            text = txt;
          //  positionX = posX;
           // positionY = posY;
           // lbposx = posX;
          //  lbposy = posY;
            textAlignH = "left";
            textAlignV = "top";
           // hasBorder = false;
            hasFill = false;
            //get font data to build a Font property with the fontFamily,Size, and style
            switch (sty)
            {
                case "bold":
                    style = FontStyle.Bold;
                    break;
                case "italic":
                    style = FontStyle.Italic;
                    break;
                case "underline":
                    style = FontStyle.Underline;
                    break;
                case "strikeout":
                    style = FontStyle.Strikeout;
                    break;
                case "regular":
                    style = FontStyle.Regular;
                    break;
                case "bolditalic":
                    style = FontStyle.Bold | FontStyle.Italic;
                    break;
                case "boldunderline":
                    style = FontStyle.Bold | FontStyle.Underline;
                    break;
                default:
                    style = FontStyle.Regular;
                    break;
            }
            font = new Font(fnt, sz, style);
            GetWidthAndHeight(gra);
            // labelIndex = lblIndex;
        }

        public void CleanPositioning(System.Drawing.Printing.PrintPageEventArgs e1)
        {

            try
            {
                SizeF textSize = e1.Graphics.MeasureString(text, font);
                stringWidth = Convert.ToInt32(textSize.Width) + 5;  //Last char gettin cut off too often
                stringHeight = Convert.ToInt32(textSize.Height);
                int tempVoffset;
                bool blnVPosNumeric = int.TryParse(textAlignV, out tempVoffset);
                int tempHoffset;
                bool blnHPosNumeric = int.TryParse(textAlignH, out tempHoffset);
                if (textAlignV == "bottom")
                {
                    vOffset = itemHeight - stringHeight;
                }
                else if (textAlignV == "center")
                {
                    vOffset = (itemHeight - stringHeight) / 2;
                }
                else if (blnVPosNumeric)//vertical Positon == topx
                {
                    vOffset = tempVoffset;
                }
                else
                {
                    vOffset = 0;
                }

                if (textAlignH == "right")
                {
                    hOffset = itemWidth - stringWidth;
                    if (hOffset < 0) { hOffset = 0; }
                }
                else if (textAlignH == "center")
                {
                    hOffset = (itemWidth - stringWidth) / 2;
                    if (hOffset < 0) { hOffset = 0; }
                }
                else if (blnHPosNumeric)//vertical Positon == top
                {
                    hOffset = tempHoffset;
                }
                else //horizontal position==left
                {
                    hOffset = 0;
                }

            }
            catch
            {
                stringWidth = 0;
                stringHeight = 0;
            }
        }
        public void CleanPositioning(Graphics gra)
        {

            try
            {
                SizeF textSize = gra.MeasureString(text, font);
                stringWidth = Convert.ToInt32(textSize.Width) + 5;  //Last char gettin cut off too often
                stringHeight = Convert.ToInt32(textSize.Height);
                int tempVoffset;
                bool blnVPosNumeric = int.TryParse(textAlignV, out tempVoffset);
                int tempHoffset;
                bool blnHPosNumeric = int.TryParse(textAlignH, out tempHoffset);
                if (textAlignV == "bottom")
                {
                    vOffset = itemHeight - stringHeight;
                }
                else if (textAlignV == "center")
                {
                    vOffset = (itemHeight - stringHeight) / 2;
                }
                else if (blnVPosNumeric)//vertical Positon == top
                {
                    vOffset = tempVoffset;
                }
                else
                {
                    vOffset = 0;
                }

                if (textAlignH == "right")
                {
                    hOffset = itemWidth - stringWidth;
                    if (hOffset < 0) { hOffset = 0; }
                }
                else if (textAlignH == "center")
                {
                    hOffset = (itemWidth - stringWidth) / 2;
                    if (hOffset < 0) { hOffset = 0; }
                }
                else if (blnHPosNumeric)//vertical Positon == top
                {
                    hOffset = tempHoffset;
                }
                else //horizontal position==left
                {
                    hOffset = 0;
                }

            }
            catch
            {
                stringWidth = 0;
                stringHeight = 0;
            }

        }
        private void GetWidthAndHeight(Graphics gra)
        {
            try
            {
                SizeF textSize = gra.MeasureString(text, font);
                stringWidth = Convert.ToInt32(textSize.Width) + 5;  //Last char gettin cut off too often
                stringHeight = Convert.ToInt32(textSize.Height);
                itemWidth = stringWidth;
                itemHeight = stringHeight;



                vOffset = 0;
                hOffset = 0;

            }
            catch
            {
                stringWidth = 0;
                stringHeight = 0;
            }
        }
        public object Clone()
        {
            TextItem clone = new TextItem();
            clone.tagName = tagName;
            clone.text = text;
            clone.font = font;
            clone.style = style;
            clone.positionX = positionX;
            clone.positionY = positionY;
            clone.lbposx = lbposx;
            clone.lbposy = lbposy;
            clone.itemWidth = itemWidth;
            clone.itemHeight = itemHeight;
            clone.textAlignH = textAlignH;
            clone.textAlignV = textAlignV;
            clone.hasBorder = hasBorder;
            clone.hasFill = hasFill;
            clone.stringWidth = stringWidth;
            clone.stringHeight = stringHeight;
            clone.vOffset = vOffset;
            clone.hOffset = hOffset;
            clone.blnMultiline = blnMultiline;
            //  clone.labelIndex = labelIndex;
            return clone;

        }
    }
}
