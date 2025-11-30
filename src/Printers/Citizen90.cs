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

using System.Text.RegularExpressions;

namespace ReceiptSharp.Printers
{
    //
    // Citizen Landscape
    //
    class Citizen90 : Thermal90
    {
        public Citizen90()
        {
            // image split size
            Split = 1662;
        }
        // print barcode: GS $ nL nH ESC $ nL nH GS w n GS h n GS H n GS k m n d1 ... dn
        public override string Barcode(SymbolData symbol, string encoding)
        {
            BarcodeForm bar = BarcodeGenerator.Generate(symbol);
            if (bar.Length > 0)
            {
                int w = bar.Length;
                int l = symbol.Height;
                int h = l + (symbol.Hri ? CharWidth * 2 + 2 : 0);
                int x = Left * CharWidth + Alignment * (Width * CharWidth - w) / 2;
                int y = Position;
                string r = $"\u001d${(char)(y + l - 1 & 255)}{(char)(y + l - 1 >> 8 & 255)}\u001b${(char)(x & 255)}{(char)(x >> 8 & 255)}";
                string d = Encode(symbol.Data, encoding == "multilingual" ? "ascii" : encoding);
                char b = BarType[symbol.Type];
                if (Regex.IsMatch(symbol.Type, @"upc|[ej]an") && symbol.Data.Length < 9)
                {
                    b++;
                }
                if (b == BarType["ean"])
                {
                    d = d.Substring(0, 12);
                }
                else if (b == BarType["upc"])
                {
                    d = d.Substring(0, 11);
                }
                else if (b == BarType["ean"] + 1)
                {
                    d = d.Substring(0, 7);
                }
                else if (b == BarType["upc"] + 1)
                {
                    d = Upce(d);
                }
                else if (b == BarType["codabar"])
                {
                    d = Codabar(d);
                }
                else if (b == BarType["code128"])
                {
                    d = Code128(d);
                }
                else
                {
                    // nothing to do
                }
                if (d.Length > 255)
                {
                    d = d.Substring(0, 255);
                }
                r += d.Length > 0 ? $"\u001dw{(char)symbol.Width}\u001dh{(char)symbol.Height}\u001dH{(char)(symbol.Hri ? 2 : 0)}\u001dk{b}{(char)d.Length}{d}" : "";
                Buffer += r;
                Position += h;
            }
            return "";
        }

        // generate Codabar data:
        protected string Codabar(string data)
        {
            return data.ToUpper();
        }
    }
}
