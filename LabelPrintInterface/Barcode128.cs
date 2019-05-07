using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabelPrintInterface
{
    public class Barcode128
    {
        private int GetCode3Char(string inputStr)
        {
            //this should only happen when 2 numberical characters in a row are detected
            int stringVal, asciiOffset, highAsciiOffset;
            asciiOffset = 32;
            highAsciiOffset = 18;
            stringVal = Convert.ToInt32(inputStr);
            if (stringVal == 0) { return 96 + asciiOffset; }  //if there are two 0s in a row
            else if (stringVal > 0 && stringVal < 95) { return stringVal + asciiOffset; }
            else { return stringVal + asciiOffset + highAsciiOffset; }
        }
        private int AnsiToUnicodeString(int inInt)
        {
            switch (inInt)
            {
                case 128:
                    return 0x20AC;
                case 32:
                    return 0x20AC;

                case 145:
                    return 0x2018;

                case 146:
                    return 0x2019;

                case 147:
                    return 0x201C;

                case 148:
                    return 0x201D;

                case 149:
                    return 0x2022;

                case 150:
                    return 0x2013;

                case 151:
                    return 0x2014;

                case 152:
                    return 0x2DC;

                case 153:
                    return 0x2122;

                case 154:
                    return 0x161;

                case 155:
                    return 0x203A;

                case 156:
                    return 0x153;

                default:
                    return Convert.ToChar(inInt);




            }


        }

        private int UnicodeToAnsiValue(int inInt)
        {
            switch (inInt)
            {
                case 0x20AC:
                    return 32;
                case 0x2018:
                    return 145;
                case 0x2019:
                    return 146;
                case 0x201C:
                    return 147;
                case 0x201D:
                    return 148;
                case 0x2022:
                    return 149;
                case 0x2013:
                    return 150;
                case 0x2014:
                    return 151;
                case 0x2DC:
                    return 152;
                case 0x2122:
                    return 153;
                case 0x161:
                    return 154;
                case 0x203A:
                    return 155;
                case 0x153:
                    return 156;
                default:
                    return inInt;
            }


        }

        public static string ToEncryptedForm(string dataString)
        {
            Barcode128 bc = new Barcode128();
            string strWorking;
            int intStringLength, intCurrentChar, currentVariant, vB, vC, highAscii, offset;
            char chFirst, chSecond;
            string encodedString, holderString;

            offset = 32;
            highAscii = 18;
            currentVariant = 0;
            intCurrentChar = 1;
            vB = 1;
            vC = 2;
            strWorking = dataString;
            intStringLength = strWorking.Length;
            encodedString = "";

            if (intStringLength == 0) { return ""; } //if there's nothing, return nothing

            for (int i = 0; i < intStringLength; i++)
            {
                if (intCurrentChar >= intStringLength) // if the character is the last one in the string
                {
                    if (intCurrentChar > intStringLength) { break; }// if there are no characters left exit
                    chFirst = strWorking[intCurrentChar - 1];
                    if (currentVariant == 0) { encodedString = Convert.ToChar(bc.AnsiToUnicodeString(104 + offset + highAscii)).ToString(); currentVariant = vB; }
                    if (currentVariant == vC) { encodedString += Convert.ToChar(bc.AnsiToUnicodeString(100 + offset + highAscii)).ToString(); currentVariant = vB; }
                    encodedString += chFirst;
                    break;
                }
                else
                {
                    chFirst = strWorking[intCurrentChar - 1];  //gets the next 2 consecutive characters
                    chSecond = strWorking[intCurrentChar];
                    if (bc.IsNumber(chFirst) && bc.IsNumber(chSecond))    //if the next 2 are numbers //this is for variation C
                    {
                        holderString = chFirst.ToString() + chSecond.ToString();
                        //if it is the first char
                        if (currentVariant == 0) { encodedString = Convert.ToChar(bc.AnsiToUnicodeString(105 + offset + highAscii)).ToString(); currentVariant = vC; }
                        //if this is in the middle
                        if (currentVariant == vB) { encodedString += Convert.ToChar(bc.AnsiToUnicodeString(99 + offset + highAscii)).ToString(); currentVariant = vC; }
                        encodedString += Convert.ToChar(bc.AnsiToUnicodeString(bc.GetCode3Char(holderString))).ToString();
                        intCurrentChar += 2;
                    }
                    else
                    {
                        chFirst = strWorking[intCurrentChar - 1];
                        if (currentVariant == 0) { encodedString += Convert.ToChar(bc.AnsiToUnicodeString(104 + offset + highAscii)).ToString(); currentVariant = vB; }
                        if (currentVariant == vC) { encodedString += Convert.ToChar(bc.AnsiToUnicodeString(100 + offset + highAscii)).ToString(); currentVariant = vB; }
                        encodedString += chFirst;
                        intCurrentChar = intCurrentChar + 1;
                    }
                }
            }
            encodedString = encodedString.Replace(" ", Convert.ToChar(bc.AnsiToUnicodeString(128)).ToString());
            encodedString = encodedString + Convert.ToChar(bc.GetCheckDigit((encodedString))).ToString();
            encodedString = encodedString + Convert.ToChar(bc.AnsiToUnicodeString(106 + offset + highAscii)).ToString();
            // encodedString = encodedString.Replace("'", "''");
            return encodedString;
        }


        private int GetCheckDigit(string data)
        {
            int total, highAscii, offset;
            char chFirst;
            offset = 32;
            highAscii = 18;
            total = GetCharValue(data[0]); //Gets the value of the first one
            for (int i = 1; i < data.Length; i++) //Gets the value of the rest
            {
                chFirst = data[i];
                total += i * GetCharValue(chFirst);
            }
            total = total % 103;
            if (total == 0) { return AnsiToUnicodeString(128); }
            else if (total + offset > 126) { return AnsiToUnicodeString(total + offset + highAscii); }
            else { return AnsiToUnicodeString(total + offset); }


        }

        private int GetCharValue(char chMyChar)
        {
            int highAscii, offset, charValue, retVal;
            // charValue = BitConverter.ToInt16(BitConverter.GetBytes(chMyChar),0);
            charValue = UnicodeToAnsiValue(BitConverter.ToInt16(BitConverter.GetBytes(chMyChar), 0));
            offset = 32;
            highAscii = 18;
            if (charValue > 144) { retVal = charValue - offset - highAscii; }
            else { retVal = charValue - offset; }
            if (charValue == 128) { retVal = 0; }
            return retVal;
        }
        private Boolean IsNumber(char testString)
        {
            Boolean blnIsNumber;
            double num;
            blnIsNumber = double.TryParse(testString.ToString(), out num);
            return blnIsNumber;

        }

    }
}

