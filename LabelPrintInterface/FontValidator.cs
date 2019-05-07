using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Drawing.Text;

namespace LabelPrintInterface
{
    public class FontValidator
    {
        public bool IsFontInstalled(string fontName)
        {
            bool blnFontIsInstalled = false;
            var fontsCollection = new System.Drawing.Text.InstalledFontCollection();
            foreach (var fontFamily in fontsCollection.Families)
            {
                if (fontFamily.Name == fontName) { blnFontIsInstalled = true; }
            }
            return blnFontIsInstalled;
        }
        public bool CopyToDesktop(string fontName)
        {
            try
            {

                string fontFile = fontName + ".ttf";
                if (File.Exists(fontFile))
                {
                    string fullPath = Path.GetFullPath(fontFile);
                    string destPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\" + fontFile;
                    File.Copy(fullPath, destPath);
                }
                return true;
            }
            catch
            {
                return false;
            }

        }
    }
}
