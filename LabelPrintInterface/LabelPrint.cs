using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Printing;
using System.Xml;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using Gma.QrCodeNet.Encoding;
using System.Net;
using System.IO;
using System.Net.Security;

namespace LabelPrintInterface
{
    public delegate TextItem UpdateTextDelegate(TextItem item);
    public delegate ImageItem UpdateImageDelegate(ImageItem item);
    public delegate void UpdateLabelPrintDelegate(LabelPrint lp);
    public class LabelPrint
    {
        private double m_widthInches;
        private double m_heightInches;
        private int m_widthPixels;
        private int m_heightPixels;
        private int m_widthPaperPixels;
        private int m_heightPaperPixels;
        public List<Page> lstPage = new List<Page>();
        private SheetDefinition sheetDef = null;
        private Graphics gra;
        private Page currentPage = null;
        private BCLabel currentLabel = null;
        private int intPagePrintCounterIndex = 0;
        public int pageCount = 0;
        public int labelCount = 0;


        public LabelPrint(double width = 8.5, double height = 11, int papersizewidth = 850, int papersizeheight = 1100)
        {
            m_widthInches = width;
            m_heightInches = height;
            m_widthPixels = Convert.ToInt32(width * 100);
            m_heightPixels = Convert.ToInt32(height * 100);
            m_widthPaperPixels = papersizewidth;
            m_heightPaperPixels = papersizeheight;
            gra = Graphics.FromImage((Image)new Bitmap(1, 1));
        }

        //gives a complete instruction based on the xml mapping
        public LabelPrint(string xmlFormat, List<List<string>> data, int offsetx=0,int offsety=0, List<Image> lstImg = null)
        {
            if (lstImg == null) { lstImg = new List<Image>(); }
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlFormat);

            XmlNodeList labelTemplate = xmlDoc.SelectNodes("/xml/LabelPrint/LabelTemplate/Item");
            XmlNodeList sheetTemplate = xmlDoc.SelectNodes("/xml/LabelPrint/SheetTemplate/Item");
            //alternative do paper
            decimal? wid1 = (decimal?)GetValueFromNode(xmlDoc.SelectSingleNode("/xml/LabelPrint/Paper/Parameter[@name='width']"), "decimal");
            decimal? hei1 = (decimal?)GetValueFromNode(xmlDoc.SelectSingleNode("/xml/LabelPrint/Paper/Parameter[@name='height']"), "decimal");
            int? prwid1 = (int?)GetValueFromNode(xmlDoc.SelectSingleNode("/xml/LabelPrint/Paper/Parameter[@name='printwidth']"), "int");
            int? prhei1 = (int?)GetValueFromNode(xmlDoc.SelectSingleNode("/xml/LabelPrint/Paper/Parameter[@name='printheight']"), "int");

            if (wid1 != null && hei1 != null && prwid1 != null && prhei1 != null) //if everything is good do basic constructor
            {
                m_widthInches = (double)wid1;
                m_heightInches = (double)hei1;
                m_widthPixels = Convert.ToInt32(wid1 * 100);
                m_heightPixels = Convert.ToInt32(hei1 * 100);
                m_widthPaperPixels = (int)prwid1;
                m_heightPaperPixels = (int)prhei1;
                gra = Graphics.FromImage((Image)new Bitmap(1, 1));
            }
            else
            {
                Exception e = new Exception("XML does not contain adequate information for the constructor");
                throw e;
            }
            int? customLabelWidth = (int?)GetValueFromNode(xmlDoc.SelectSingleNode("/xml/LabelPrint/LabelTemplate"), "int", "width");
            int? customLabelHeight = (int?)GetValueFromNode(xmlDoc.SelectSingleNode("/xml/LabelPrint/LabelTemplate"), "int", "height");


            ////experiment
            List<GraphicItem> lstTempGraphic = new List<GraphicItem>();

            bool success = GetItemTypesFromTemplate(labelTemplate, ref lstTempGraphic);

            //
            //alternative do xml item
            foreach (List<string> row in data)
            {
                if (customLabelWidth != null && customLabelHeight != null) { NewLabel((int)customLabelWidth, (int)customLabelHeight); }
                else { NewLabel(); }
                int colCounter = 0, imgCounter = 0;
                foreach (GraphicItem gItem in lstTempGraphic)  //for each template text or image item
                {
                    if (gItem is ImageItem)
                    {
                        ImageItem item = (ImageItem)gItem;
                        if (item.location == "parameter")  //if the image comes from the image list parameter
                        {
                            Image tmpImage = lstImg.ElementAtOrDefault(imgCounter);
                            if (tmpImage != null)
                            { AddImage(item.tagName, FlipImage(tmpImage, item.flipDegrees), item.positionX, item.positionY, item.itemWidth, item.itemHeight, item.displayMode, item.hasBorder); }
                            imgCounter++;
                        }
                        else if (item.location == "data")  //if it comes from the data source
                        {
                            string strDataItem = row.ElementAtOrDefault(colCounter);
                            Image tmpImage = null;
                            if (item.type == "image") { tmpImage = GetImageFromSource(item.source, strDataItem); }
                            if (item.type == "qr") { tmpImage = GetQRCode(strDataItem); }
                            if (tmpImage != null)
                            { AddImage(item.tagName, FlipImage(tmpImage, item.flipDegrees), item.positionX, item.positionY, item.itemWidth, item.itemHeight, item.displayMode, item.hasBorder); }
                            colCounter++;
                        }
                        else if (item.location == "template")  //in this case the image item should already be populated from the template, just add
                        {
                            if (item.image != null)
                            { AddImage(item.tagName, item.image, item.positionX, item.positionY, item.itemWidth, item.itemHeight, item.displayMode, item.hasBorder); }
                        }
                    }
                    if (gItem is TextItem)
                    {
                        TextItem item = (TextItem)gItem;
                        if (item.location == "template")//then just add the item
                        {
                            AddText(item);
                        }
                        if (item.location == "data")
                        {
                            string strDataItem = row.ElementAtOrDefault(colCounter);
                            if (strDataItem != null)
                            {
                                if (item.type == "bc128") { strDataItem = Get128Code(strDataItem); }
                                if (item.type == "bc39") { strDataItem = "*" + strDataItem + "*"; }
                                item.text = strDataItem;
                            }
                            AddText(item);
                            colCounter++;
                        }
                    }

                }

            }
            int? marginleft = (int?)GetValueFromNode(xmlDoc.SelectSingleNode("/xml/LabelPrint/SheetTemplate/Parameter[@name='marginleft']"), "int");
            int? margintop = (int?)GetValueFromNode(xmlDoc.SelectSingleNode("/xml/LabelPrint/SheetTemplate/Parameter[@name='margintop']"), "int");
            int? lbwidth = (int?)GetValueFromNode(xmlDoc.SelectSingleNode("/xml/LabelPrint/SheetTemplate/Parameter[@name='width']"), "int");
            int? lbheight = (int?)GetValueFromNode(xmlDoc.SelectSingleNode("/xml/LabelPrint/SheetTemplate/Parameter[@name='height']"), "int");
            int? xgap = (int?)GetValueFromNode(xmlDoc.SelectSingleNode("/xml/LabelPrint/SheetTemplate/Parameter[@name='xgap']"), "int");
            int? ygap = (int?)GetValueFromNode(xmlDoc.SelectSingleNode("/xml/LabelPrint/SheetTemplate/Parameter[@name='ygap']"), "int");
            int? rows = (int?)GetValueFromNode(xmlDoc.SelectSingleNode("/xml/LabelPrint/SheetTemplate/Parameter[@name='rows']"), "int");
            int? cols = (int?)GetValueFromNode(xmlDoc.SelectSingleNode("/xml/LabelPrint/SheetTemplate/Parameter[@name='cols']"), "int");
            int? angle = (int?)GetValueFromNode(xmlDoc.SelectSingleNode("/xml/LabelPrint/SheetTemplate/Parameter[@name='angle']"), "int");
            string resizemode = (string)GetValueFromNode(xmlDoc.SelectSingleNode("/xml/LabelPrint/SheetTemplate/Parameter[@name='resizemode']"));
            if (marginleft != null && margintop != null && lbwidth != null && lbheight != null && xgap != null && ygap != null && rows != null && cols != null)
            {
                if (angle == null) { angle = 0; }
                if (resizemode == null) { resizemode = "none"; }
                DefineSheet((int)marginleft+offsetx, (int)margintop+offsety, (int)lbwidth, (int)lbheight, (int)xgap, (int)ygap, (int)cols, (int)rows, (int)angle, resizemode);


            }
        }

        private Image GetImageFromSource(string source, string path)
        {
            try
            {
                if (source == "filesystem")
                {
                    return Image.FromFile(path);
                }
                else if (source == "web")
                {
                    WebClient wc = new WebClient();
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                    ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(delegate { return true; });
                    byte[] bytes = wc.DownloadData(path);
                    MemoryStream ms = new MemoryStream(bytes);
                    return Image.FromStream(ms);
                }
                else
                {
                    return null;
                }
            }
            catch { return null; }
        }

        private Boolean GetItemTypesFromTemplate(XmlNodeList lstNode, ref List<GraphicItem> lstGraphic)
        {
            try
            {
                foreach (XmlNode item in lstNode)  //for each template text or image item
                {
                    string type = (string)GetValueFromNode(item, "string", "type");
                    string tagname = (string)GetValueFromNode(item, "string", "tagname");
                    int? positionx = (int?)GetValueFromNode(item.SelectSingleNode("Parameter[@name='positionx']"), "int");
                    int? positiony = (int?)GetValueFromNode(item.SelectSingleNode("Parameter[@name='positiony']"), "int");
                    int? width = (int?)GetValueFromNode(item.SelectSingleNode("Parameter[@name='width']"), "int");
                    int? height = (int?)GetValueFromNode(item.SelectSingleNode("Parameter[@name='height']"), "int");
                    bool border = (bool)GetValueFromNode(item.SelectSingleNode("Parameter[@name='border']"), "bool");
                    string location = (string)GetValueFromNode(item, "string", "location");
                    string value = (string)GetValueFromNode(item, "string", "value");
                    if (type == "image" && tagname != null && positionx != null && positiony != null && width != null && height != null)
                    {
                        string displayMode = (string)GetValueFromNode(item.SelectSingleNode("Parameter[@name='displaymode']"));
                        string source = (string)GetValueFromNode(item, "string", "source");
                        int? flip = (int?)GetValueFromNode(item, "int", "flip");

                        if (location == "template")  //then get the image content from the xml so we don't have to download it repeatedly
                        {

                            Image tmpImage = GetImageFromSource(source, value);
                            if (tmpImage != null)
                            {
                                ImageItem tmpImageItem = new ImageItem(tagname, tmpImage, (int)positionx, (int)positiony, (int)width, (int)height, displayMode, border);
                                tmpImageItem.location = location;
                                tmpImageItem.source = source;
                                tmpImageItem.type = type;
                                if (flip != null) { tmpImageItem.image = FlipImage(tmpImageItem.image,(int)flip); } 
                                //lstGraphic.Add(new ImageItem(tagname, tmpImage, (int)positionx, (int)positiony, (int)width, (int)height, displayMode, border));
                                lstGraphic.Add(tmpImageItem);
                            }
                        }
                        else  //leave the image null so that we can populate it later based on the data or the parameter
                        {
                            ImageItem tmpImageItem = new ImageItem(tagname, null, (int)positionx, (int)positiony, (int)width, (int)height, displayMode, border);
                            tmpImageItem.location = location;
                            tmpImageItem.source = source;
                            tmpImageItem.type = type;
                            if (flip != null) { tmpImageItem.flipDegrees = (int)flip; }
                            lstGraphic.Add(tmpImageItem);

                        }

                    }
                    else  //if not an image either qr or text
                    {

                        string fontfamily = (string)GetValueFromNode(item.SelectSingleNode("Parameter[@name='fontfamily']"));
                        int? fontsize = (int?)GetValueFromNode(item.SelectSingleNode("Parameter[@name='fontsize']"), "int");
                        string fontstyle = (string)GetValueFromNode(item.SelectSingleNode("Parameter[@name='fontstyle']"));

                        string valign = (string)GetValueFromNode(item.SelectSingleNode("Parameter[@name='valign']"));
                        string halign = (string)GetValueFromNode(item.SelectSingleNode("Parameter[@name='halign']"));

                        bool multiline = (bool)GetValueFromNode(item.SelectSingleNode("Parameter[@name='multiline']"), "bool"); 
                        bool fill = (bool)GetValueFromNode(item.SelectSingleNode("Parameter[@name='fill']"), "bool");


                        if ((type == "text" || type == "bc128" || type == "bc39") && tagname != null && fontfamily != null && fontsize != null && positionx != null && positiony != null & width != null && height != null)
                        {
                            if (valign == null) { valign = "top"; }
                            if (halign == null) { halign = "left"; }
                            if (location == "template")
                            {
                                if (value == null) { value = ""; }
                                TextItem tmpTextItem = new TextItem(tagname, value, fontfamily, (int)fontsize, fontstyle, (int)positionx, (int)positiony, (int)width, (int)height, halign, valign, border, fill);
                                tmpTextItem.type = type;
                                tmpTextItem.location = location;
                                tmpTextItem.blnMultiline = multiline;
                                lstGraphic.Add(tmpTextItem);

                            }
                            else  //it's from the data record or the location is not specified default to datasource
                            {
                                TextItem tmpTextItem = new TextItem(tagname, "", fontfamily, (int)fontsize, fontstyle, (int)positionx, (int)positiony, (int)width, (int)height, halign, valign, border, fill);
                                tmpTextItem.type = type;
                                tmpTextItem.location = "data";
                                tmpTextItem.blnMultiline = multiline;
                                lstGraphic.Add(tmpTextItem);
                            }
                            // AddText(tagname, strDataItem, fontfamily, (int)fontsize, fontstyle, (int)positionx, (int)positiony, (int)width, (int)height, halign, valign, border, fill);
                        }
                        if (type == "qr" && tagname != null && positionx != null && positiony != null && width != null && height != null)
                        {
                            ImageItem tmpImageItem;
                            if (location == "template" && value != null)  //this supports static qr codes from Template
                            {
                                tmpImageItem = new ImageItem(tagname, GetQRCode(value), (int)positionx, (int)positiony, (int)width, (int)height, "qr", border);

                            }
                            else
                            {
                                tmpImageItem = new ImageItem(tagname, null, (int)positionx, (int)positiony, (int)width, (int)height, "qr", border);

                            }
                            if (location == null) { tmpImageItem.location = "data"; }
                            else { tmpImageItem.location = location; }
                            tmpImageItem.type = type;
                            lstGraphic.Add(tmpImageItem);

                        }
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }


        }
        private object GetValueFromNode(XmlNode node, string datatype = "string", string attributename = null)
        {
            if (node == null)
            {
                if (datatype == "bool")
                {
                    return false;
                }
                else
                {
                    return null;
                }

            }
            bool blnFail = false;
            string strValue = "";
            object objOutput = null;

            if (attributename == null)//we're looking for the inner text of the node
            {
                strValue = node.InnerText;
            }
            else //find specified attribute value
            {
                if (node.Attributes[attributename] != null)
                {
                    strValue = node.Attributes[attributename].Value;
                }
                else
                {
                    blnFail = true;
                }
            }

            //next try to convert this to the correct data type
            switch (datatype)
            {
                case "string":
                    objOutput = strValue.Trim();
                    break;
                case "int":
                    int tempint = 0;
                    blnFail = !Int32.TryParse(strValue.Trim(), out tempint);
                    objOutput = tempint;
                    break;
                case "decimal":
                    decimal tempdec = 0;
                    blnFail = !Decimal.TryParse(strValue.Trim(), out tempdec);
                    objOutput = tempdec;
                    break;
                case "float":
                    float tempfloat = 0.0F;
                    blnFail = !float.TryParse(strValue.Trim(), out tempfloat);
                    objOutput = tempfloat;
                    break;
                case "bool":
                    bool tempbln = false;
                    if (strValue.Trim().ToUpper() == "TRUE") { tempbln = true; }
                    else { tempbln = false; }
                    objOutput = tempbln;
                    break;
                default:
                    blnFail = true;
                    break;
            }


            if (blnFail == true)
            {
                return null;
            }
            else
            {
                return objOutput;
            }
        }
        public string Print(bool withPrintDialog = false, PrintDocument p = null)
        {
            bool isPrintDocumentDefined;
            if (p == null)
            {
                p = new PrintDocument();
                isPrintDocumentDefined = false;

            }
            else
            {
                isPrintDocumentDefined = true;
            }
            string strDefaultPrinter = p.PrinterSettings.PrinterName;
            if (m_widthPaperPixels != 850 || m_heightPaperPixels != 1100)
            {
                p.DefaultPageSettings.PaperSize = new PaperSize("New Custom Paper", m_widthPaperPixels, m_heightPaperPixels);
            }
            PrintPageEventHandler eh = new PrintPageEventHandler(AddAllGraphics);
            p.PrintPage += eh;
            if (withPrintDialog == false)  //no dialog
            {
                try
                {
                    if (p.PrinterSettings.PrintRange == PrintRange.SomePages)//this handles the range if one is assigned
                    {
                        if (p.PrinterSettings.ToPage >= p.PrinterSettings.FromPage && p.PrinterSettings.ToPage > 0 && p.PrinterSettings.FromPage <= pageCount)
                        {//this is to validate the given range
                            if (p.PrinterSettings.FromPage == 0) { p.PrinterSettings.FromPage = 1; }//fix the from page to be a minimum of 1 if pages are specified
                            intPagePrintCounterIndex = p.PrinterSettings.FromPage - 1;

                        }
                        else
                        {
                            Exception e = new Exception("Error in Print Range. Please Try Again.");
                            throw e;
                        }
                    }
                    else
                    {
                        intPagePrintCounterIndex = 0;
                    }

                    p.Print();
                }
                catch (Exception)
                {
                    throw;
                }

            }
            else  //with dialog
            {
                PrintDialog pd = new PrintDialog();
                //set defaults for the dialog and the form if the print document was already defined
                pd.AllowSomePages = true;
                if (isPrintDocumentDefined == true)
                {
                    pd.PrinterSettings.PrinterName = p.PrinterSettings.PrinterName;
                    pd.PrinterSettings.PrintRange = p.PrinterSettings.PrintRange;
                    pd.PrinterSettings.ToPage = p.PrinterSettings.ToPage;
                    pd.PrinterSettings.FromPage = p.PrinterSettings.FromPage;

                }
                if (pd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        p.PrinterSettings.PrinterName = pd.PrinterSettings.PrinterName;
                        if (pd.PrinterSettings.PrintRange == PrintRange.SomePages)
                        { //this handles the range
                            p.PrinterSettings.PrintRange = PrintRange.SomePages;
                            p.PrinterSettings.FromPage = pd.PrinterSettings.FromPage;
                            p.PrinterSettings.ToPage = pd.PrinterSettings.ToPage;
                            if (p.PrinterSettings.ToPage >= p.PrinterSettings.FromPage && p.PrinterSettings.ToPage > 0 && p.PrinterSettings.FromPage <= pageCount)
                            {
                                if (p.PrinterSettings.FromPage == 0) { p.PrinterSettings.FromPage = 1; }//fix the from page to be a minimum of 1 if pages are specified
                                intPagePrintCounterIndex = p.PrinterSettings.FromPage - 1;
                                strDefaultPrinter = p.PrinterSettings.PrinterName; //in case they switched the printer in the dialog
                            }
                            else
                            {
                                Exception e = new Exception("Error in Print Range. Please Try Again.");
                                throw e;
                            }
                        }
                        else
                        {
                            intPagePrintCounterIndex = 0;
                        }
                        p.PrinterSettings.Copies = pd.PrinterSettings.Copies;
                        p.DefaultPageSettings.PaperSource = pd.PrinterSettings.DefaultPageSettings.PaperSource;
                        p.Print();
                        return pd.PrinterSettings.PrinterName;
                    }
                    catch (Exception) { throw; }
                }


            }

            return strDefaultPrinter;  //returns the used printer name (helps if they use print dialog)



        }

        /// <summary>
        /// Adds Text to the label in the specified location for the current page
        /// </summary>
        /// <param name="tagName">The tagName to be used to identify the text element easily</param>
        /// <param name="text">The text to be displayed</param>
        /// <param name="fontFamily">The font name to be used "Times New Roman", "Arial"</param>
        /// <param name="fontSize">The size of the font to be used</param>
        /// <param name="fontStyle">The style of the font "regular","bold","italic","underline","strikeout","bolditalic","boldunderline"</param>
        /// <param name="positionX">The number of pixels from the left to the left edge of this text section</param>
        /// <param name="positionY">The number of pixels fromt he top to the top edge of this text section</param>
        /// <param name="itemWidth">The width of the text section (this will be bordered or filled optionally)</param>
        /// <param name="itemHeight">The height of the text section (this will be bordered or filled optionally</param>
        /// <param name="textAlignH">Horizontal Position "left","center","right" or an integer value pixels from left</param>
        /// <param name="textAlignV">Vertical Position "top","center","bottom" or an integer value pixels from top</param>
        /// <param name="hasBorder">Is there a border around this text section</param>
        /// <param name="hasFill">Is there a light gray fill around this text section</param>
        public PrintDocument GetPrintDocument(string strPrinterName, bool allPages = true, int fromPage = 0, int toPage = 0)
        {
            PrintDocument p = GetPrintDocument();
            if (isPrinterNameValid(strPrinterName))
            { p.PrinterSettings.PrinterName = strPrinterName; }
            if (allPages == true)
            {
                p.PrinterSettings.PrintRange = PrintRange.AllPages;
            }
            else
            {
                p.PrinterSettings.PrintRange = PrintRange.SomePages;
                p.PrinterSettings.FromPage = fromPage;
                p.PrinterSettings.ToPage = toPage;
            }
            return p;

        }
        public PrintDocument GetPrintDocument()
        {
            return new PrintDocument();
        }
        private bool isPrinterNameValid(string printerName)
        {
            bool isValid = false;
            foreach (string pName in PrinterSettings.InstalledPrinters)
            {
                if (printerName == pName)
                {
                    isValid = true;
                }
            }
            return isValid;
        }
        public TextItem AddText(string tagName, string text, string fontFamily, int fontSize, string fontStyle, int positionX, int positionY, int itemWidth, int itemHeight, string textAlignH, string textAlignV, bool hasBorder, bool hasFill)
        {
            if (currentPage == null)
            {
                NewPage();
            }
            if (currentLabel == null)
            {
                NewLabel();
            }
            if (positionX < m_widthPixels && positionY < m_heightPixels)
            {
              //  if (positionX + itemWidth > m_widthPixels) { itemWidth = m_widthPixels - positionX; }
               // if (positionY + itemHeight > m_heightPixels) { itemHeight = m_heightPixels - positionY; }
                TextItem txt = new TextItem(tagName, text, fontFamily, fontSize, fontStyle, positionX, positionY, itemWidth, itemHeight, textAlignH, textAlignV, hasBorder, hasFill);
                //---new support
                //currentPage.lstTxt.Add(txt)
                currentLabel.lstTxt.Add(txt);
                return txt;
            }
            else
            {
                return null;
            }

        }
        public TextItem AddText(string tagName, string text, string fontFamily, int fontSize, string fontStyle, int positionX, int positionY)
        {
            if (currentPage == null)
            {
                NewPage();
            }
            if (currentLabel == null)
            {
                NewLabel();
            }
            if (positionX < m_widthPixels && positionY < m_heightPixels)
            {
                TextItem txt = new TextItem(tagName, text, fontFamily, fontSize, fontStyle, positionX, positionY, gra);
                currentLabel.lstTxt.Add(txt);
                return txt;
            }
            else
            {
                return null;
            }

        }

        public TextItem AddText(TextItem ti)
        {
            if (currentPage == null)
            {
                NewPage();
            }
            if (currentLabel == null)
            {
                NewLabel();
            }
            if (ti.positionX < m_widthPixels && ti.positionY < m_heightPixels)
            {
               // if (ti.positionX + ti.itemWidth > m_widthPixels) { ti.itemWidth = m_widthPixels - ti.positionX; }
               // if (ti.positionY + ti.itemHeight > m_heightPixels) { ti.itemHeight = m_heightPixels - ti.positionY; }

                currentLabel.lstTxt.Add((TextItem)ti.Clone());
                return ti;
            }
            else
            {
                return null;
            }

        }
        public SheetDefinition DefineSheet(int marginLeft, int marginTop, int availableLabelWidth, int availableLabelHeight, int xGapBetweenLabels, int yGapBetweenLabels, int colsPerRow, int totalRows, int clockwiseDegreeShift = 0, string resizeMode = "none")
        {
            sheetDef = new SheetDefinition(marginLeft, marginTop, availableLabelWidth, availableLabelHeight, xGapBetweenLabels, yGapBetweenLabels, colsPerRow, totalRows, clockwiseDegreeShift, resizeMode);
            sheetDef.ResetSheets(this);
            return sheetDef;
        }
        public void ResetSheets()
        {
            sheetDef.ResetSheets(this);
        }
            
        public PageDefinition DefinePage(int marginLeft, int marginTop, int availableLabelWidth, int availableLabelHeight, int xGapBetweenLabels, int yGapBetweenLabels, int colsPerRow, int totalRows, int clockwiseDegreeShift = 0, string resizeMode = "none")
        {
            PageDefinition temp = new PageDefinition(marginLeft, marginTop, availableLabelWidth, availableLabelHeight, xGapBetweenLabels, yGapBetweenLabels, colsPerRow, totalRows, clockwiseDegreeShift, resizeMode);
            currentPage.pgDefinition = temp;
            return temp;
           
        }
        public SheetDefinition DefineCustomSheet(int labelsPerSheet)
        {
            sheetDef = new SheetDefinition(labelsPerSheet);
            return sheetDef;

        }
        public PageDefinition DefineCustomPage(int labelsPerPage)
        {
            PageDefinition temp = new LabelPrintInterface.PageDefinition(labelsPerPage);
            currentPage.pgDefinition = temp;
            return temp;
        }

        public void NewPage()
        {
            foreach (Page pg in lstPage)
            {
                pg.isLast = false;//all the other pages are not the last page anymore

            }
            Page pgNewPage = new Page(lstPage.Count);
            lstPage.Add(pgNewPage);
            currentPage = pgNewPage;
            currentLabel = null;
            pageCount += 1;

        }
        public void NewLabel()
        {
            if (currentPage == null) { NewPage(); }
            int? width = null;
            int? height = null;
            int? templatewidth = null;
            int? templateheight = null;
            int posX = 0;
            int posY = 0;
            int intLbCount = currentPage.lstLabel.Count;
            //if (sheetDef != null) {
            //    width = sheetDef.availableLabelWidth;
            //    height = sheetDef.availableLabelHeight;
            //    templatewidth = width;
            //    templateheight = height;
            //    posX = sheetDef.GetPositionForLabelIndex(intLbCount).X;
            //    posY = sheetDef.GetPositionForLabelIndex(intLbCount).Y;
            //}

            BCLabel lb = new BCLabel(intLbCount, width, height, templatewidth, templateheight, posX, posY);
            currentPage.lstLabel.Add(lb);
            //shallow reference
            currentLabel = lb;
            labelCount += 1;
        }
        public void NewLabel(int width, int height)
        {
            if (currentPage == null) { NewPage(); }
            int? wid = width;
            int? hei = height;
            int? templatewidth = null;
            int? templateheight = null;
            int posX = 0;
            int posY = 0;
            int intLbCount = currentPage.lstLabel.Count;
            //if (sheetDef != null)
            //{
            //    templatewidth = sheetDef.availableLabelWidth;
            //    templateheight = sheetDef.availableLabelHeight;
            //    posX = sheetDef.GetPositionForLabelIndex(intLbCount).X;
            //    posY = sheetDef.GetPositionForLabelIndex(intLbCount).Y;
            //}

            BCLabel lb = new BCLabel(intLbCount, width, height, templatewidth, templateheight, posX, posY);
            currentPage.lstLabel.Add(lb);
            //shallow reference
            currentLabel = lb;
            labelCount += 1;
        }
        public void NewLabel(int index, int width, int height)
        {
            if (currentPage == null) { NewPage(); }
            int? wid = width;
            int? hei = height;
            int? templatewidth = null;
            int? templateheight = null;
            int posX = 0;
            int posY = 0;
            int intLbCount = currentPage.lstLabel.Count;
            //if (sheetDef != null)
            //{
            //    templatewidth = sheetDef.availableLabelWidth;
            //    templateheight = sheetDef.availableLabelHeight;
            //    posX = sheetDef.GetPositionForLabelIndex(intLbCount).X;
            //    posY = sheetDef.GetPositionForLabelIndex(intLbCount).Y;
            //}

            BCLabel lb = new BCLabel(index, width, height, templatewidth, templateheight, posX, posY);
            currentPage.lstLabel.Add(lb);
            //shallow reference
            currentLabel = lb;
            labelCount += 1;
        }
        public void SetPageCount(int pages)
        {
            while (lstPage.Count <= pages)//revert this back to less than
            {
                NewPage();
            }
        }
        public void SetCurrentPage(int pageIndex, bool withCreate = false)
        {
            try
            {
                if (lstPage.Count <= pageIndex && withCreate == true) //if the requested page is out of index
                {
                    SetPageCount(pageIndex);//change this to be index +1
                }
                currentPage = lstPage[pageIndex];
            }
            catch { throw; }

        }

        public void SetBlankLabels(int blankLabelCount, int labelsPerPage)
        {
            int tempCounter = 0;
            labelCount = 0;  //reset the counter to 0 since I will make all new blanks
            int pages = (int)Math.Ceiling(blankLabelCount / (decimal)labelsPerPage);
            for (int i = 0; i < pages; i++)
            {
                SetCurrentPage(i, true);
                currentPage.lstLabel.Clear();
                for (int j = 0; j < labelsPerPage; j++)//make this not give a full sheet of labels for every page maybe
                {
                    if (tempCounter < blankLabelCount)//make sure no additional labels are maded that arent needed
                    {
                        NewLabel();
                    }
                    tempCounter++;
                }
            }

        }
        /// <summary>
        /// Adds an image to the label in a specified location
        /// </summary>
        /// <param name="tagName">The tagName to be used to identify the image element easily</param>
        /// <param name="image">The Image that will be pasted onto the label</param>
        /// <param name="positionX">The number of pixels from the left of the label to the component that holds the image - point represents left edge </param>
        /// <param name="positionY">The number of pixels from the top of the label to component that holds the image - point represents upper edge</param>
        /// <param name="itemWidth">The Width of the section that this image will fit into</param>
        /// <param name="itemHeight">The Height of the section that this image will fit into</param>
        /// <param name="displayMode">The mode of the image in relation to the section, possibilities are "contains","stretch", "zoom", and "qr" ,blank will not fit image to area, but will just place it full-size in location</param>

        /// <param name="hasBorder">Should the image have a border</param>
        public void AddImage(string tagName, Image image, int positionX, int positionY, int itemWidth, int itemHeight, string displayMode, bool hasBorder, bool bypassPositionValidation=false)
        {
            if (currentPage == null)
            {
                NewPage();
            }
            if (currentLabel == null)
            {
                NewLabel();
            }
            if (positionX < m_widthPixels && positionY < m_heightPixels)
            {
                if (positionX + itemWidth > m_widthPixels) { itemWidth = m_widthPixels - positionX; }
                if (positionY + itemHeight > m_heightPixels) { itemHeight = m_heightPixels - positionY; }
                //---new stuff
                // currentPage.lstImg.Add(new ImageItem(tagName, image, positionX, positionY, itemWidth, itemHeight, displayMode, hasBorder,currentLabelIndex));
                currentLabel.lstImg.Add(new ImageItem(tagName, image, positionX, positionY, itemWidth, itemHeight, displayMode, hasBorder));
            }
            else if (bypassPositionValidation==true)
            {
                currentLabel.lstImg.Add(new ImageItem(tagName, image, positionX, positionY, itemWidth, itemHeight, displayMode, hasBorder));
            }
        }
        /// <summary>
        /// This method is intended to update templates and will run the callback method for all pages accessing the updatable text object by tag name
        /// </summary>
        /// <param name="tagName">Tag name of the text item to be updated</param>
        /// <param name="utd">Callback method to manipulate the text item with</param>

        public void UpdateText(string tagName, UpdateTextDelegate utd)
        {
            TextItem ti = null;
            foreach (Page pg in lstPage)
            {
                foreach (BCLabel lb in pg.lstLabel)
                {
                    foreach (TextItem item in lb.lstTxt)
                    {
                        if (item.tagName == tagName)
                        {
                            ti = item;
                        }
                    }
                    if (ti != null)
                    {
                        ti = utd(ti);
                    }
                }

            }

        }

        public void UpdateLP(UpdateLabelPrintDelegate labelPaginationDelegate)
        {
            labelPaginationDelegate(this);
        }

        /// <summary>
        /// This method is intended to update templates and will run the callback method for all pages accessing the updatable image object by tag name
        /// </summary>
        /// <param name="tagName">Tag name of the image item to be updated</param>
        /// <param name="uid">Callback method to manipulate the image item with</param>
        public void UpdateImage(string tagName, UpdateImageDelegate uid)
        {
            ImageItem ii = null;
            foreach (Page pg in lstPage)
            {
                foreach (BCLabel lb in pg.lstLabel)
                {
                    foreach (ImageItem item in lb.lstImg)
                    {
                        if (item.tagName == tagName)
                        {
                            ii = item;
                        }
                    }
                    if (ii != null)
                    {
                        ii = uid(ii);
                    }
                }

            }

        }

        private Point DoTranslation(int angleDegrees, BCLabel lb, Graphics gra, float scale)
        {
            //int xTranslation = 0;
            //int yTranslation = 0; 
            //double midX = lb.positionX + (double)lb.width / 2;
            //double midY = lb.positionY + (double)lb.height / 2;
            //double newMidX = lb.positionX + (double)lb.templatewidth / 2;
            //double newMidY = lb.positionY + (double)lb.templateheight / 2;
            //double angleRadians = angleDegrees * (Math.PI / 180);
            //double radiansInternalAngle= Math.Atan(midX / midY);
            //double newAngleRadians = angleRadians + radiansInternalAngle;
            //double hypotenuse = Math.Sqrt((Math.Pow(midX,2)) + (Math.Pow(midY,2)));
            //double newX = Math.Sin(newAngleRadians) * hypotenuse;
            //double newY = Math.Cos(newAngleRadians) * hypotenuse;

            double midX = lb.positionX + (double)lb.width / 2;
            double midY = lb.positionY + (double)lb.height / 2;
            double newMidX = lb.positionX + (double)lb.templatewidth / 2;
            double newMidY = lb.positionY + (double)lb.templateheight / 2;
            double radiansInternalAngle = Math.Atan(newMidX / newMidY);
            double angleRadians = angleDegrees * (Math.PI / 180);
            double newAngleRadians = angleRadians + radiansInternalAngle;
            double hypotenuse = Math.Sqrt((Math.Pow(newMidX, 2)) + (Math.Pow(newMidY, 2)));
            double newX = Math.Sin(newAngleRadians) * hypotenuse;
            double newY = Math.Cos(newAngleRadians) * hypotenuse;

            //modify newX and newY as adverse to the scale
            double inverseScale = 1 / (double)scale;
            newX *= inverseScale;
            newY *= inverseScale;
            //good we have the new coordinates, but now we have to zero out existing position
            //but keep in mind that we are translating from center to center
            newX -= midX;
            newY -= midY;

            int x = Convert.ToInt32(Math.Round(newX, 0));
            int y = Convert.ToInt32(Math.Round(newY, 0));

            //xTranslation -= lb.positionX;
            //xTranslation -= (lb.positionY + (int)lb.height);
            //yTranslation -= lb.positionY;
            //yTranslation += lb.positionX;
            //gra.TranslateTransform(xTranslation, yTranslation);
            //return new Point(xTranslation, yTranslation);

            gra.TranslateTransform(x, y);
            return new Point(x, y);
        }
        private void UndoTranslation(Point pt, Graphics gra)
        {
            gra.TranslateTransform(-pt.X, -pt.Y);
        }

        private float CalculateResizeScale(BCLabel lb, string resizetype, int angleDegrees)
        {
            int actualLabelWidth = (int)lb.width;
            int actualLabelHeight = (int)lb.height;
            int finalLabelWidth = (int)lb.templatewidth;
            int finalLabelHeight = (int)lb.templateheight;
            double angleRadians = angleDegrees * (Math.PI / 180);
            double scale = 1;
            //get practical width and height after degree shift
            double width = Math.Abs((actualLabelWidth * Math.Cos(angleRadians)) + (actualLabelHeight * Math.Sin(angleRadians)));
            double height = Math.Abs((actualLabelWidth * Math.Sin(angleRadians)) + (actualLabelHeight * Math.Cos(angleRadians)));
            //find ratio width/height for label
            double ratioOriginal = width / height;
            //find ration width/height for template
            double ratioTemplate = (double)finalLabelWidth / (double)finalLabelHeight;
            bool blnMatchHeight;
            //this is for "contains
            if (ratioOriginal >= ratioTemplate) { blnMatchHeight = false; }
            else { blnMatchHeight = true; }
            if (resizetype=="zoom")//only other option is zoom
            {
                blnMatchHeight = !blnMatchHeight;

            }
            if (blnMatchHeight == true)
            {
                scale = finalLabelHeight / height;
            }
            else
            {
                scale = finalLabelWidth / width;
            }

            return (float)scale;


        }
        private void AddAllGraphics(object sender1, PrintPageEventArgs e1)
        {
            PrintDocument pd = (PrintDocument)sender1;
            int from = pd.PrinterSettings.FromPage;
            int to = pd.PrinterSettings.ToPage;
            PrintRange pr = pd.PrinterSettings.PrintRange;




            string rsz = "none";
            //rotation stuff
            int rotateAngle = 0;
            currentPage = lstPage[intPagePrintCounterIndex];
            PageDefinition currentDefinition = sheetDef;//if sheetDef is applied, it is the default
            if (currentPage.pgDefinition!=null)
            { currentDefinition = currentPage.pgDefinition; }

            if (currentDefinition != null)
            {
                if (currentDefinition.Grid != null)
                { 
                    rotateAngle = currentDefinition.Grid.clockwiseDegreeShift;
                    rsz = currentDefinition.Grid.resizeType;
                }
                else
                {
                    rotateAngle = 0;
                    rsz = "none";
                }
                if (rotateAngle != 0) { e1.Graphics.RotateTransform(rotateAngle); }


            }
           
            bool blnScale = false;
            float scale = 1.0F;
            foreach (BCLabel lb in currentPage.lstLabel)
            {
                if (rsz != "none" || blnScale == true)
                {
                    scale = CalculateResizeScale(lb, rsz, rotateAngle);
                    e1.Graphics.PageScale = scale;
                    blnScale = true;
                }
                //Do Transition calculation (needed if rotation occurrs)
                Point pt = new Point();
                if (rotateAngle != 0 || blnScale==true)
                {
                    e1.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                    pt = DoTranslation(rotateAngle, lb, e1.Graphics, e1.Graphics.PageScale);
                }





                foreach (TextItem item in lb.lstTxt)
                {
                    item.CleanPositioning(e1); //To calculate text sizes and offsets
                                               // int totalAvailableWidth = m_widthPixels - item.positionX; // for the section
                                               // int totalAvailableHeight = m_heightPixels - item.positionY;
                    int labelWidth = m_widthPixels;//first assuming the width and the height are full page
                    int labelHeight = m_heightPixels;
                    int totalAvailableWidth = labelWidth - item.positionX; // for the section
                    int totalAvailableHeight = labelHeight - item.positionY;
                    if (lb.width != null)// if specific label widths and heights are defined, then override
                    {
                        labelWidth = (int)lb.width;
                        totalAvailableWidth = (int)lb.width - item.lbposx; // for the section
                    }
                    if (lb.height != null)
                    {
                        labelHeight = (int)lb.height;
                        totalAvailableHeight = (int)lb.height - item.lbposy;
                    }

                    //make sure item width fits within printable area
                    int adjustedItemWidth = item.itemWidth;
                    if (totalAvailableWidth < item.itemWidth) { adjustedItemWidth = totalAvailableWidth; }
                    //make sure text with all it's offsets fit in printable area
                    int textPlacementLeft = item.positionX + item.hOffset;
                    int textAvailableWidth = m_widthPixels - textPlacementLeft;
                    // int adjustedTextWidth = item.stringWidth;
                    //if (item.stringWidth + textPlacementLeft > item.positionX + item.itemWidth)
                    //{
                    //    adjustedTextWidth = item.positionX + item.itemWidth - textPlacementLeft;
                    //    int multiplier = (item.stringWidth / textAvailableWidth);//desn't work right for sheets
                    //    if (item.textAlignV == "bottom") { item.vOffset -= item.stringHeight * multiplier; }//if there are 2 lines 2X height
                    //    if (item.textAlignV == "center") { item.vOffset -= Convert.ToInt32(item.stringHeight * (.5) * multiplier); }//if there are 2 lines 2X height
                    //}
                    //new code to fix barcode error 
                    //if (item.font.FontFamily.Name == "Code128bWin" & item.stringWidth > adjustedItemWidth)
                    //{
                    //    item.font = new Font("Times New Roman", 12, FontStyle.Regular);
                    //    item.text = "Barcode Error";

                    //}

                    if (item.hasFill == true)
                    {
                        // e1.Graphics.FillRectangle(Brushes.LightGray, item.positionX, item.positionY, adjustedItemWidth, item.itemHeight);
                        e1.Graphics.FillRectangle(Brushes.Gainsboro, item.positionX, item.positionY, adjustedItemWidth, item.itemHeight);
                    }
                    if (item.hasBorder == true)
                    {
                        //  e1.Graphics.DrawRectangle(Pens.Black, item.positionX, item.positionY, adjustedItemWidth, item.itemHeight);
                        e1.Graphics.DrawRectangle(Pens.Black, item.positionX, item.positionY, adjustedItemWidth, item.itemHeight);
                    }
                    //  e1.Graphics.DrawString(item.text, item.font, new SolidBrush(Color.Black), new RectangleF(item.positionX + item.hOffset, item.positionY + item.vOffset, adjustedTextWidth, totalAvailableHeight));
                    StringFormat fmt = new StringFormat(StringFormatFlags.LineLimit | StringFormatFlags.NoWrap, 1003) { Trimming = StringTrimming.None };
                    if(item.blnMultiline==true)
                    {
                        fmt = new StringFormat(StringFormatFlags.LineLimit, 1003) { Trimming = StringTrimming.None };
                    }
                   

                    e1.Graphics.DrawString(item.text, item.font, new SolidBrush(Color.Black), new RectangleF(item.positionX + item.hOffset, item.positionY + item.vOffset, adjustedItemWidth, totalAvailableHeight), fmt);


                }
                foreach (ImageItem item in lb.lstImg)
                {
                    int imgWidth = item.image.Width;
                    int imgHeight = item.image.Height;

                    if (item.displayMode == "zoom")
                    {
                        if ((double)imgWidth / (double)imgHeight > (double)item.itemWidth / (double)item.itemHeight) //if the image is wider than the space
                        {
                            //then clip the extra width //destination rect, src rectangle, Unit No 16
                            //first find the multiplier for the height
                            double dblMultiplier = (double)item.itemHeight / (double)imgHeight;
                            //then find out what the width would have been
                            double expandedWidth = imgWidth * dblMultiplier;
                            //then find out how much to cut
                            double widthToCutFromExpanded = expandedWidth - item.itemWidth;
                            int widthToCutFromOriginal = Convert.ToInt32(widthToCutFromExpanded / dblMultiplier);


                            e1.Graphics.DrawImage(item.image, new Rectangle(item.positionX, item.positionY, item.itemWidth, item.itemHeight), new Rectangle(widthToCutFromOriginal / 2, 0, imgWidth - widthToCutFromOriginal, imgHeight), GraphicsUnit.Pixel);
                        }
                        else //the image is taller or equal to the space
                        {
                            //then clip the extra height
                            double dblMultiplier = (double)item.itemWidth / (double)imgWidth;
                            double expandedHeight = imgHeight * dblMultiplier;
                            double heightToCutFromExpanded = expandedHeight - item.itemHeight;
                            int heightToCutFromOriginal = Convert.ToInt32(heightToCutFromExpanded / dblMultiplier);
                            e1.Graphics.DrawImage(item.image, new Rectangle(item.positionX, item.positionY, item.itemWidth, item.itemHeight), new Rectangle(0, heightToCutFromOriginal / 2, imgWidth, imgHeight - heightToCutFromOriginal), GraphicsUnit.Pixel);
                        }

                    }
                    else if (item.displayMode == "stretch")
                    {
                        e1.Graphics.DrawImage(item.image, new Rectangle(item.positionX, item.positionY, item.itemWidth, item.itemHeight));
                    }
                    else if (item.displayMode == "contains")
                    {
                        if ((double)imgWidth / (double)imgHeight > (double)item.itemWidth / (double)item.itemHeight) //if the image is wider than the space
                        {
                            //then the image width is proportionally shrunk to fit the item.width
                            int newImageWidth = item.itemWidth;
                            int newImageHeight = Convert.ToInt32(imgHeight * ((double)item.itemWidth / (double)imgWidth));
                            int verticalOffset = Convert.ToInt32(((double)(item.itemHeight - newImageHeight)) / 2);
                            e1.Graphics.DrawImage(item.image, new Rectangle(item.positionX, item.positionY + verticalOffset, newImageWidth, newImageHeight));

                        }
                        else //the image is taller or equal to the space
                        {
                            int newImageWidth = Convert.ToInt32(imgWidth * ((double)item.itemHeight / (double)imgHeight));
                            int newImageHeight = item.itemHeight; // imgHeight * (item.width / imgWidth);
                            int horizontalOffset = Convert.ToInt32(((double)(item.itemWidth - newImageWidth)) / 2);
                            e1.Graphics.DrawImage(item.image, new Rectangle(item.positionX + horizontalOffset, item.positionY, newImageWidth, newImageHeight));

                        }
                    }
                    else if (item.displayMode == "qr")
                    {
                        int tempWidth = Convert.ToInt32(item.itemWidth * scale);
                        int tempHeight = Convert.ToInt32(item.itemHeight * scale);
                        e1.Graphics.DrawImage(ResizeImage(item.image, tempWidth, tempHeight), item.positionX, item.positionY);
                    }
                    else
                    {
                        e1.Graphics.DrawImage(item.image, item.positionX, item.positionY);
                    }
                    if (item.hasBorder == true)  //add border afterwards
                    {
                        e1.Graphics.DrawRectangle(Pens.Black, item.positionX, item.positionY, item.itemWidth, item.itemHeight);
                    }
                }
                //undo Translation
                if (rotateAngle != 0) { UndoTranslation(pt, e1.Graphics); }

            }
            //if it's not the last page and either all pages are printed or it is in range
            if (currentPage.isLast == false && (intPagePrintCounterIndex < (to - 1) || pr == PrintRange.AllPages))//&& (intPagePrintCounterIndex<(to-1)||printAllPages==true)
            {
                e1.HasMorePages = true;
                intPagePrintCounterIndex += 1;
            }




        }
        /// <summary>
        /// Will produce an Image with the QR code. Often used with LabelPrint.AddImage
        /// </summary>
        /// <param name="text">The text to be encoded</param>
        /// <returns></returns>
        public Image GetQRCode(string text)
        {
            QrEncoder encoder = new QrEncoder();
            QrCode code = encoder.Encode(text);
            Bitmap bmp = new Bitmap(code.Matrix.Width, code.Matrix.Height);
            for (int x = 0; x < code.Matrix.Width; x++)
            {
                for (int y = 0; y < code.Matrix.Height; y++)
                {
                    if (code.Matrix.InternalArray[x, y])
                    {
                        bmp.SetPixel(x, y, Color.Black);
                    }
                    else
                    {
                        bmp.SetPixel(x, y, Color.White);
                    }
                }
            }
            return (Image)bmp;

        }
        public Image GetQRCode(List<Byte> lstByte)
        {
            QrEncoder encoder = new QrEncoder(ErrorCorrectionLevel.M);
            QrCode code = encoder.Encode(lstByte);
            Bitmap bmp = new Bitmap(code.Matrix.Width, code.Matrix.Height);
            for (int x = 0; x < code.Matrix.Width; x++)
            {
                for (int y = 0; y < code.Matrix.Height; y++)
                {
                    if (x > 19)
                    {
                        int stophere = 1;
                    }
                    if (code.Matrix.InternalArray[x, y])
                    {
                        bmp.SetPixel(x, y, Color.Black);
                    }
                    else
                    {
                        bmp.SetPixel(x, y, Color.White);
                    }
                }
            }
            return (Image)bmp;

        }

        /// <summary>
        /// Will produce a string with the encoded barcode 128 information. Often used with LabelPrint.AddText. Note that this method depends on font Code128Win to display correctly
        /// </summary>
        /// <param name="text">The text to be encoded</param>
        /// <returns></returns>
        public string Get128Code(string text)
        {

            return Barcode128.ToEncryptedForm(text);
        }


        private Image ResizeImage(Image bmp, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(bmp.HorizontalResolution, bmp.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {

                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                graphics.SmoothingMode = SmoothingMode.None;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(bmp, destRect, 0, 0, bmp.Width, bmp.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return (Image)destImage;
        }
        public List<Byte> GetBytesFromBinaryString(string binary)
        {
            List<Byte> lstByte = new List<Byte>();
            for (int i = 0; i < binary.Length; i += 8)
            {
                String t = binary.Substring(i, 8);
                lstByte.Add(Convert.ToByte(t, 2));
            }
            return lstByte;
        }
        public bool DoesFontExist(string fontname)
        {
            FontValidator fnt = new FontValidator();
            return fnt.IsFontInstalled(fontname);
        }
        public Image FlipImage(Image img,int degrees)
        {
            switch(degrees)
            {
                case 90:
                    img.RotateFlip(RotateFlipType.Rotate90FlipNone);
                    break;
                case 180:
                    img.RotateFlip(RotateFlipType.Rotate180FlipNone);
                    break;
                case 270:
                    img.RotateFlip(RotateFlipType.Rotate270FlipNone);
                    break;
                default:
                    break;

            }
            return img;

        }
        public List<Image> FlipImage(List<Image> lstImg, int degrees)
        {
            foreach(Image img in lstImg)
            {
                switch (degrees)
                {
                    case 90:
                        img.RotateFlip(RotateFlipType.Rotate90FlipNone);
                        break;
                    case 180:
                        img.RotateFlip(RotateFlipType.Rotate180FlipNone);
                        break;
                    case 270:
                        img.RotateFlip(RotateFlipType.Rotate270FlipNone);
                        break;
                    default:
                        break;

                }
            }
            return lstImg;
        }
    }
}
