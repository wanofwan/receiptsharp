/*
Copyright 2025 Open Foodservice System Consortium

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

// QR Code is a registered trademark of DENSO WAVE INCORPORATED.

using System.Collections.Generic;
using System.Text;

namespace ReceiptSharp.Printers
{
    //
    // multilingual conversion table (cp437, cp852, cp858, cp866, cp1252)
    //
    class Command : Base
    {
        //
        // iconv
        //
        protected static string Encode(string content, string encoding)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Encoding enc;
            switch (encoding)
            {
                case "cp932":
                case "shiftjis":
                    enc = Encoding.GetEncoding(932);
                    break;
                case "cp936":
                case "gb18030":
                    enc = Encoding.GetEncoding(936);
                    break;
                case "cp949":
                case "ksc5601":
                    enc = Encoding.GetEncoding(949);
                    break;
                case "cp950":
                case "big5":
                    enc = Encoding.GetEncoding(950);
                    break;
                case "tis620":
                    enc = Encoding.GetEncoding(874);
                    break;
                default:
                    enc = Encoding.ASCII;
                    break;
            }
            return Encoding.GetEncoding("ISO-8859-1").GetString(enc.GetBytes(content));
        }
        //
        // multilingual conversion table (cp437, cp852, cp858, cp866, cp1252)
        //
        protected static readonly Dictionary<char, string> MultiTable = new Dictionary<char, string>();
        protected static readonly Dictionary<char, string> MultiPage = new Dictionary<char, string>()
        {
            { '\u0000', "ÇüéâäàåçêëèïîìÄÅÉæÆôöòûùÿÖÜ¢£¥₧ƒáíóúñÑªº¿⌐¬½¼¡«»░▒▓│┤╡╢╖╕╣║╗╝╜╛┐└┴┬├─┼╞╟╚╔╩╦╠═╬╧╨╤╥╙╘╒╓╫╪┘┌█▄▌▐▀αßΓπΣσµτΦΘΩδ∞φε∩≡±≥≤⌠⌡÷≈°∙·√ⁿ²■ " },
            { '\u0010', "€�‚ƒ„…†‡ˆ‰Š‹Œ�Ž��‘’“”•–—˜™š›œ�žŸ ¡¢£¤¥¦§¨©ª«¬­®¯°±²³´µ¶·¸¹º»¼½¾¿ÀÁÂÃÄÅÆÇÈÉÊËÌÍÎÏÐÑÒÓÔÕÖ×ØÙÚÛÜÝÞßàáâãäåæçèéêëìíîïðñòóôõö÷øùúûüýþÿ" },
            { '\u0011', "АБВГДЕЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдежзийклмноп░▒▓│┤╡╢╖╕╣║╗╝╜╛┐└┴┬├─┼╞╟╚╔╩╦╠═╬╧╨╤╥╙╘╒╓╫╪┘┌█▄▌▐▀рстуфхцчшщъыьэюяЁёЄєЇїЎў°∙·√№¤■ " },
            { '\u0012', "ÇüéâäůćçłëŐőîŹÄĆÉĹĺôöĽľŚśÖÜŤťŁ×čáíóúĄąŽžĘę¬źČş«»░▒▓│┤ÁÂĚŞ╣║╗╝Żż┐└┴┬├─┼Ăă╚╔╩╦╠═╬¤đĐĎËďŇÍÎě┘┌█▄ŢŮ▀ÓßÔŃńňŠšŔÚŕŰýÝţ´­˝˛ˇ˘§÷¸°¨˙űŘř■ " },
            { '\u0013', "ÇüéâäàåçêëèïîìÄÅÉæÆôöòûùÿÖÜø£Ø×ƒáíóúñÑªº¿®¬½¼¡«»░▒▓│┤ÁÂÀ©╣║╗╝¢¥┐└┴┬├─┼ãÃ╚╔╩╦╠═╬¤ðÐÊËÈ€ÍÎÏ┘┌█▄¦Ì▀ÓßÔÒõÕµþÞÚÛÙýÝ¯´­±‗¾¶§÷¸°¨·¹³²■ " }
        };
        protected Dictionary<char, char> StarPage = new Dictionary<char, char>()
        {
            { '\u0000', '\u0001' }, { '\u0010', '\u0020' }, { '\u0011', '\u000a' }, { '\u0012', '\u0005' }, { '\u0013', '\u0004' }
        };
        static Command()
        {
            foreach (char p in MultiPage.Keys)
            {
                string s = MultiPage[p];
                for (int i = 0; i < 128; i++)
                {
                    char c = s[i];
                    if (!MultiTable.ContainsKey(c))
                    {
                        MultiTable[c] = new string(new char[] { p, (char)(i + 128) });
                    }
                }
            }
        }
    }
}