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

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SkiaSharp;

namespace ReceiptSharp.Printers
{
    //
    // StarPRNT Common
    //
    class Star : Command
    {
        // printer configuration
        protected bool UpsideDown = false;
        protected bool Spacing = false;
        protected bool Cutting = true;
        protected bool Gradient = false;
        protected double Gamma = 1.8;
        protected int Threshold = 128;
        protected int Margin = 0;
        // start printing: ESC @ ESC RS a n (ESC RS R n) ESC RS F n ESC SP n ESC s n1 n2 (ESC z n) (ESC 0) (SI) (DC2)
        public override string Open(PrintOption printer)
        {
            UpsideDown = printer.UpsideDown;
            Spacing = printer.Spacing;
            Cutting = printer.Cutting;
            Gradient = printer.Gradient;
            Gamma = printer.Gamma;
            Threshold = printer.Threshold;
            Margin = printer.Margin;
            return $"\u001b@\u001b\u001ea\u0000{(printer.Encoding == "tis620" ? "\u001b\u001eR\u0001" : "")}\u001b\u001eF\u0000\u001b 0\u001bs00{(Spacing ? "\u001bz1" : "\u001b0")}{(UpsideDown ? "\u000f" : "\u0012")}";
        }
        // finish printing: ESC GS ETX s n1 n2
        public override string Close()
        {
            return (Cutting ? Cut() : "") + "\u001b\u001d\u0003\u0001\u0000\u0000";
        }
        // set print area: ESC l n ESC Q n
        public override string Area(int left, int width, int right)
        {
            return $"\u001bl{(char)0}\u001bQ{(char)(Margin + left + width + right)}\u001bl{(char)(Margin + left)}\u001bQ{(char)(Margin + left + width)}";
        }
        // set line alignment: ESC GS a n
        public override string Align(int align)
        {
            return $"\u001b\u001da{(char)align}";
        }
        // set absolute print position: ESC GS A n1 n2
        public override string Absolute(double position)
        {
            int p = (int)(position * CharWidth);
            return $"\u001b\u001dA{(char)(p & 255)}{(char)(p >> 8 & 255)}";
        }
        // set relative print position: ESC GS R n1 n2
        public override string Relative(double position)
        {
            int p = (int)(position * CharWidth);
            return $"\u001b\u001dR{(char)(p & 255)}{(char)(p >> 8 & 255)}";
        }
        // set line spacing and feed new line: (ESC z n) (ESC 0)
        public override string VrLf(bool vr)
        {
            return $"{(UpsideDown ? Lf() : "")}{(vr == UpsideDown && Spacing ? "\u001bz1" : "\u001b0")}{(UpsideDown ? "" : Lf())}";
        }
        // cut paper: ESC d n
        public override string Cut()
        {
            return "\u001bd3";
        }
        // underline text: ESC - n
        public override string Ul()
        {
            return "\u001b-1";
        }
        // emphasize text: ESC E
        public override string Em()
        {
            return "\u001bE";
        }
        // invert text: ESC 4
        public override string Iv()
        {
            return "\u001b4";
        }
        // scale up text: ESC i n1 n2
        public override string Wh(int wh)
        {
            return $"\u001bi{(wh < 3 ? $"{(char)(wh >> 1 & 1)}{(char)(wh & 1)}" : $"{(char)(wh - 2)}{(char)(wh - 2)}")}";
        }
        // cancel text decoration: ESC - n ESC F ESC 5 ESC i n1 n2
        public override string Normal()
        {
            return $"\u001b-0\u001bF\u001b5\u001bi{(char)0}{(char)0}";
        }
        // print text:
        public override string Text(string text, string encoding)
        {
            return encoding == "multilingual" ? MultiConv(text) : CodePage[encoding] + Encode(text, encoding);
        }
        // codepages: (ESC GS t n) (ESC $ n) (ESC R n)
        protected Dictionary<string, string> CodePage = new Dictionary<string, string>()
        {
            { "cp437", "\u001b\u001dt\u0001" }, { "cp852", "\u001b\u001dt\u0005" }, { "cp858", "\u001b\u001dt\u0004" }, { "cp860", "\u001b\u001dt\u0006" },
            { "cp863", "\u001b\u001dt\u0008" }, { "cp865", "\u001b\u001dt\u0009" }, { "cp866", "\u001b\u001dt\u000a" }, { "cp1252", "\u001b\u001dt\u0020" },
            { "cp932", "\u001b$1\u001bR8" }, { "cp936", "" },
            { "cp949", "\u001bRD" }, { "cp950", "" },
            { "shiftjis", "\u001b$1\u001bR8" }, { "gb18030", "" },
            { "ksc5601", "\u001bRD" }, { "big5", "" }, { "tis620", "\u001b\u001dt\u0061" }
        };
        // convert to multiple codepage characters: (ESC GS t n)
        protected string MultiConv(string text)
        {
            string r = "";
            char p = '\u0100';
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c > '\u007f')
                {
                    if (MultiTable.ContainsKey(c))
                    {
                        string d = MultiTable[c];
                        char q = d[0];
                        if (p == q)
                        {
                            r += d[1];
                        }
                        else
                        {
                            r += "\u001b\u001dt" + StarPage[q] + d[1];
                            p = q;
                        }
                    }
                    else
                    {
                        r += '?';
                    }
                }
                else
                {
                    r += c;
                }
            }
            return r;
        }
        // feed new line: LF
        public override string Lf()
        {
            return "\u000a";
        }
        // insert commands:
        public override string Command(string command)
        {
            return command;
        }
        // image split size
        protected int Split = 2400;
        // print image: ESC GS S m xL xH yL yH n [d11 d12 ... d1k]
        public override string Image(string image)
        {
            byte[] png = Convert.FromBase64String(image);
            SKBitmap img = SKBitmap.Decode(png);
            byte[] imgdata = img.Bytes;
            int w = img.Width;
            int[] d = new int[w];
            int l = w + 7 >> 3;
            List<string> s = new List<string>();
            int j = 0;
            for (int z = 0; z < img.Height; z += Split)
            {
                int h = Math.Min(Split, img.Height - z);
                string r = $"\u001b\x001dS{(char)1}{(char)(l & 255)}{(char)(l >> 8 & 255)}{(char)(h & 255)}{(char)(h >> 8 & 255)}{(char)0}";
                for (int y = 0; y < h; y++)
                {
                    int i = 0, e = 0;
                    for (int x = 0; x < w; x += 8)
                    {
                        int b = 0;
                        int q = Math.Min(w - x, 8);
                        for (int p = 0; p < q; p++)
                        {
                            int f = (int)Math.Floor((d[i] + e * 5) / 16 + Math.Pow(((imgdata[j] * .299 + imgdata[j + 1] * .587 + imgdata[j + 2] * .114 - 255) * imgdata[j + 3] + 65525) / 65525, 1 / Gamma) * 255);
                            j += 4;
                            if (Gradient)
                            {
                                d[i] = e * 3;
                                if (f < Threshold)
                                {
                                    b |= 128 >> p;
                                    e = f;
                                }
                                else
                                {
                                    e = f - 255;
                                }
                                if (i > 0)
                                {
                                    d[i - 1] += e;
                                }
                                d[i++] += e * 7;
                            }
                            else
                            {
                                if (f < Threshold)
                                {
                                    b |= 128 >> p;
                                }
                            }
                        }
                        r += (char)b;
                    }
                }
                s.Add(r);
            }
            if (UpsideDown)
            {
                s.Reverse();
            }
            return string.Join("", s.ToArray());
        }
        // print QR Code: ESC GS y S 0 n ESC GS y S 1 n ESC GS y S 2 n ESC GS y D 1 m nL nH d1 d2 ... dk ESC GS y P
        public override string Qrcode(SymbolData symbol, string encoding)
        {
            string r = "";
            if (symbol.Data.Length > 0)
            {
                byte[,] matrix = QRCodeGenerator.Generate(symbol);
                int size = matrix.GetLength(1);
                int w = size * symbol.Cell;
                int h = w;
                int l = w + 7 >> 3;
                r += $"\u001b\u001dS{(char)1}{(char)(l & 255)}{(char)(l >> 8 & 255)}{(char)(h & 255)}{(char)(h >> 8 & 255)}{(char)0}";
                for (int i = 0; i < size; i++)
                {
                    string d = "";
                    for (int j = 0; j < w; j += 8)
                    {
                        int b = 0;
                        int q = Math.Min(w - j, 8);
                        for (int p = 0; p < q; p++)
                        {
                            if (matrix[i, (int)Math.Floor((double)((j + p) / symbol.Cell))] == 1)
                            {
                                b |= 128 >> p;
                            }
                        }
                        d += (char)b;
                    }
                    for (int k = 0; k < symbol.Cell; k++)
                    {
                        r += d;
                    }
                }
            }
            return r;
        }
        // QR Code error correction levels:
        protected Dictionary<string, char> QrLevel = new Dictionary<string, char>()
        {
            { "l", (char)0 }, { "m", (char)1 }, { "q", (char)2 }, { "h", (char)3 }
        };
        // print barcode: ESC b n1 n2 n3 n4 d1 ... dk RS
        public override string Barcode(SymbolData symbol, string encoding)
        {
            string d = Encode(symbol.Data, encoding == "multilingual" ? "ascii" : encoding);
            char b = BarType[symbol.Type];
            if (Regex.IsMatch(symbol.Type, @"upc|[ej]an") && symbol.Data.Length < 9)
            {
                b--;
            }
            if (b == BarType["upc"] - 1)
            {
                d = Upce(d);
            }
            else if (b == BarType["code128"])
            {
                d = Code128(d);
            }
            else
            {
                // nothing to do
            }
            var t = new[] { 49, 56, 50 };
            int u = symbol.Type == "itf" ? t[symbol.Width - 2] : symbol.Width + (Regex.IsMatch(symbol.Type, @"^(code39|codabar|nw7)$") ? 50 : 47);
            return d.Length > 0 ? $"\u001bb{b}{(char)(symbol.Hri ? 50 : 49)}{(char)u}{(char)symbol.Height}{d}\u001e" : "";
        }
        // barcode types:
        protected Dictionary<string, char> BarType = new Dictionary<string, char>()
        {
            { "upc", (char)49 }, { "ean", (char)51 }, { "jan", (char)51 }, { "code39", (char)52 }, { "itf", (char)53 }, { "codabar", (char)56 }, { "nw7", (char)56 }, { "code93", (char)55 }, { "code128", (char)54 }
        };
        // generate UPC-E data (convert UPC-E to UPC-A):
        protected string Upce(string data)
        {
            string r = "";
            string s = Regex.IsMatch(data, @"^0\d{6,7}$") ? data : "";
            if (s.Length > 0)
            {
                r += s.Substring(0, 3);
                switch (s[6])
                {
                    case '0':
                    case '1':
                    case '2':
                        r += s[6] + "0000" + s[3] + s[4] + s[5];
                        break;
                    case '3':
                        r += s[3] + "00000" + s[4] + s[5];
                        break;
                    case '4':
                        r += s[3] + s[4] + "00000" + s[5];
                        break;
                    default:
                        r += s[3] + s[4] + s[5] + "0000" + s[6];
                        break;
                }
            }
            return r;
        }
        // generate CODE128 data:
        protected string Code128(string data)
        {
            string s = Regex.IsMatch(data, @"[^\x00-\x7f]") ? "" : data;
            s = s.Replace(@"%", "%0");
            s = Regex.Replace(s, @"[\x00-\x1f]", m => $"%'{(char)(m.Value[0] + 64)}");
            s = s.Replace(@"\u007f", "%5");
            return s;
        }
    }
}
