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
using System.Linq;
using System.Text.RegularExpressions;
using SkiaSharp;

namespace ReceiptSharp.Printers
{
    //
    // ESC/POS Thermal
    //
    class Thermal : Escpos
    {
        protected int Alignment = 0;
        protected int Left = 0;
        protected int Width = 48;
        protected int Right = 0;
        protected int Margin = 0;
        protected int MarginRight = 0;
        // start printing: ESC @ GS a n ESC M n FS ( A pL pH fn m ESC SP n FS S n1 n2 (ESC 2) (ESC 3 n) ESC { n FS .
        public override string Open(PrintOption printer)
        {
            UpsideDown = printer.UpsideDown;
            Spacing = printer.Spacing;
            Cutting = printer.Cutting;
            Gradient = printer.Gradient;
            Gamma = printer.Gamma;
            Threshold = printer.Threshold;
            Alignment = 0;
            Left = 0;
            Width = printer.Cpl;
            Right = 0;
            Margin = printer.Margin;
            MarginRight = printer.MarginRight;
            return $"\u001b@\u001da\u0000\u001bM{(printer.Encoding == "tis620" ? 'a' : '0')}\u001c(A{(char)2}{(char)0}{(char)48}{(char)0}\u001b \u0000\u001cS\u0000\u0000{(Spacing ? "\u001b2" : "\u001b3\u0000")}\u001b{{{(char)(UpsideDown ? 1 : 0)}\u001c.";
        }
        // finish printing: GS r n
        public override string Close()
        {
            return (Cutting ? Cut() : "") + "\u001dr1";
        }
        // set print area: GS L nL nH GS W nL nH
        public override string Area(int left, int width, int right)
        {
            Left = left;
            Width = width;
            Right = right;
            int m = (Margin + left) * CharWidth;
            int w = width * CharWidth;
            return $"\u001dL{(char)(m & 255)}{(char)(m >> 8 & 255)}\u001dW{(char)(w & 255)}{(char)(w >> 8 & 255)}";
        }
        // set line alignment: ESC a n
        public override string Align(int align)
        {
            Alignment = align;
            return $"\u001ba{(char)align}";
        }
        // set absolute print position: ESC $ nL nH
        public override string Absolute(double position)
        {
            int p = (int)(position * CharWidth);
            return $"\u001b${(char)(p & 255)}{(char)(p >> 8 & 255)}";
        }
        // set relative print position: ESC \ nL nH
        public override string Relative(double position)
        {
            int p = (int)(position * CharWidth);
            return $"\u001b\\{(char)(p & 255)}{(char)(p >> 8 & 255)}";
        }
        // print horizontal rule: FS C n FS . ESC t n ...
        public override string Hr(int width)
        {
            return $"\u001cC0\u001c.\u001bt\u0001{new string('\u0095', width)}";
        }
        // print vertical rules: GS ! n FS C n FS . ESC t n ...
        public override string Vr(int[] widths, int height)
        {
            return widths.Aggregate($"\u001d!{(char)(height - 1)}\u001cC0\u001c.\u001bt\u0001\u0096", (a, w) => $"{a}{Relative(w)}\u0096");
        }
        // start rules: FS C n FS . ESC t n ...
        public override string VrStart(int[] widths)
        {
            string s = widths.Aggregate("\u009c", (a, w) => $"{a}{new string('\u0095', w)}\u0091");
            return $"\u001cC0\u001c.\u001bt\u0001{s.Substring(0, s.Length - 1)}\u009d";
        }
        // stop rules: FS C n FS . ESC t n ...
        public override string VrStop(int[] widths)
        {
            string s = widths.Aggregate("\u009e", (a, w) => $"{a}{new string('\u0095', w)}\u0090");
            return $"\u001cC0\u001c.\u001bt\u0001{s.Substring(0, s.Length - 1)}\u009f";
        }
        // print vertical and horizontal rules: FS C n FS . ESC t n ...
        public override string VrHr(int[] widths1, int[] widths2, int dl, int dr)
        {
            string s1 = widths1.Aggregate(dl > 0 ? "\u009e" : "\u009a", (a, w) => $"{a}{new string('\u0095', w)}\u0090");
            string r1 = $"{new string(' ', Math.Max(-dl, 0))}{s1.Substring(0, s1.Length - 1)}{(dr < 0 ? "\u009f" : "\u009b")}{new string(' ', Math.Max(dr, 0))}";
            string s2 = widths2.Aggregate(dl < 0 ? "\u009c" : "\u0098", (a, w) => $"{a}{new string('\u0095', w)}\u0091");
            string r2 = $"{new string(' ', Math.Max(dl, 0))}{s2.Substring(0, s2.Length - 1)}{(dr > 0 ? "\u009d" : "\u0099")}{new string(' ', Math.Max(-dr, 0))}";
            return $"\u001cC0\u001c.\u001bt\u0001{string.Concat(r2.Select((c, i) => VrTable[c][r1[i]]))}";
        }
        // set line spacing and feed new line: (ESC 2) (ESC 3 n)
        public override string VrLf(bool vr)
        {
            return (vr == UpsideDown && Spacing ? "\u001b2" : "\u001b3\u0000") + Lf();
        }
        // cut paper: GS V m n
        public override string Cut()
        {
            return "\u001dVB\u0000";
        }
        // underline text: ESC - n FS - n
        public override string Ul()
        {
            return "\u001b-2\u001c-2";
        }
        // emphasize text: ESC E n
        public override string Em()
        {
            return "\u001bE1";
        }
        // invert text: GS B n
        public override string Iv()
        {
            return "\u001dB1";
        }
        // scale up text: GS ! n
        public override string Wh(int wh)
        {
            return $"\u001d!{(char)(wh < 3 ? (wh & 1) << 4 | wh >> 1 & 1 : wh - 2 << 4 | wh - 2)}";
        }
        // cancel text decoration: ESC - n FS - n ESC E n GS B n GS ! n
        public override string Normal()
        {
            return "\u001b-0\u001c-0\u001bE0\u001dB0\u001d!\u0000";
        }
        // print text:
        public override string Text(string text, string encoding)
        {
            switch (encoding)
            {
                case "multilingual":
                    return MultiConv(text);
                case "tis620":
                    return CodePage[encoding] + ArrayFrom(text, encoding).Aggregate("", (a, c) => $"{a}\u0000{Encode(c, encoding)}");
                default:
                    return CodePage[encoding] + Encode(text, encoding);
            }
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
        protected int Split = 512;
        // print image: GS 8 L p1 p2 p3 p4 m fn a bx by c xL xH yL yH d1 ... dk GS ( L pL pH m fn
        public override string Image(string image)
        {
            string r = UpsideDown ? Area(Right + MarginRight - Margin, Width, Left) + Align(2 - Alignment) : "";
            byte[] png = Convert.FromBase64String(image);
            SKBitmap img = SKBitmap.Decode(png);
            byte[] imgdata = img.Bytes;
            int w = img.Width;
            int[] d = new int[w];
            int j = UpsideDown ? imgdata.Length - 4 : 0;
            for (int z = 0; z < img.Height; z += Split)
            {
                int h = Math.Min(Split, img.Height - z);
                int l = (w + 7 >> 3) * h + 10;
                r += $"\u001d8L{(char)(l & 255)}{(char)(l >> 8 & 255)}{(char)(l >> 16 & 255)}{(char)(l >> 24 & 255)}{(char)48}{(char)112}{(char)48}{(char)1}{(char)1}{(char)49}{(char)(w & 255)}{(char)(w >> 8 & 255)}{(char)(h & 255)}{(char)(h >> 8 & 255)}";
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
                            j += UpsideDown ? -4 : 4;
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
                r += $"\u001d(L{(char)2}{(char)0}{(char)48}{(char)50}";
            }
            return r;
        }
        // print QR Code: GS ( k pL pH cn fn n1 n2 GS ( k pL pH cn fn n GS ( k pL pH cn fn n GS ( k pL pH cn fn m d1 ... dk GS ( k pL pH cn fn m
        public override string Qrcode(SymbolData symbol, string encoding)
        {
            string r = UpsideDown ? Area(Right + MarginRight - Margin, Width, Left) + Align(2 - Alignment) : "";
            if (symbol.Data.Length > 0)
            {
                byte[,] matrix = QRCodeGenerator.Generate(symbol);
                int size = matrix.GetLength(1);
                if (UpsideDown)
                {
                    int center = size / 2;
                    for (int i = 0;  i < center; i++)
                    {
                        for (int j = 0; j < size; j++)
                        {
                            (matrix[i, j], matrix[size - 1 - i, size - 1 - j]) = (matrix[size - 1 - i, size - 1 - j], matrix[i, j]);
                        }
                    }
                    for (int j = 0; j < center; j++)
                    {
                        (matrix[center, j], matrix[center, size - 1 - j]) = (matrix[center, size - 1 - j], matrix[center, j]);
                    }
                }
                int w = size * symbol.Cell;
                int h = w;
                int l = (w + 7 >> 3) * h + 10;
                r += $"\u001d8L{(char)(l & 255)}{(char)(l >> 8 & 255)}{(char)(l >> 16 & 255)}{(char)(l >> 24 & 255)}{(char)48}{(char)112}{(char)48}{(char)1}{(char)1}{(char)49}{(char)(w & 255)}{(char)(w >> 8 & 255)}{(char)(h & 255)}{(char)(h >> 8 & 255)}";
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
                r += $"\u001d(L{(char)2}{(char)0}{(char)48}{(char)50}";
            }
            return r;
        }
        // QR Code error correction level:
        protected Dictionary<string, char> QrLevel = new Dictionary<string, char>()
        {
            { "l", (char)48 }, { "m", (char)49 }, { "q", (char)50 }, { "h", (char)51 }
        };
        // print barcode: GS w n GS h n GS H n GS k m n d1 ... dn
        public override string Barcode(SymbolData symbol, string encoding)
        {
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
            return d.Length > 0 ? $"\u001dw{(char)symbol.Width}\u001dh{(char)symbol.Height}\u001dH{(char)(symbol.Hri ? 2 : 0)}\u001dk{b}{(char)d.Length}{d}" : "";
        }
        // barcode types:
        protected Dictionary<string, char> BarType = new Dictionary<string, char>()
        {
            { "upc", (char)65 }, { "ean", (char)67 }, { "jan", (char)67 }, { "code39", (char)69 }, { "itf", (char)70 }, { "codabar", (char)71 }, { "nw7", (char)71 }, { "code93", (char)72 }, { "code128", (char)73 }
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
        // CODE128 special characters:
        protected Dictionary<string, char> C128 = new Dictionary<string, char>()
        {
            { "special", (char)123 }, { "codea", (char)65 }, { "codeb", (char)66 }, { "codec", (char)67 }, { "shift", (char)83 }
        };
        // generate CODE128 data (minimize symbol width):
        protected string Code128(string data)
        {
            string r = "";
            string s = Regex.IsMatch(data, @"[^\x00-\x7f]") ? "" : data;
            s = Regex.Replace(s, @"{", "{{");
            if (s.Length > 0)
            {
                List<char> d = new List<char>();
                int p = Regex.Match(s, @"[^ -_]").Index;
                if (Regex.IsMatch(s, @"^\d{2}$"))
                {
                    d.Add(C128["special"]);
                    d.Add(C128["codec"]);
                    d.Add((char)int.Parse(s));
                }
                else if (Regex.IsMatch(s, @"^\d{4,}"))
                {
                    Code128C(C128["codec"], s, d);
                }
                else if (p >= 0 && s[p] < 32)
                {
                    Code128A(C128["codea"], s, d);
                }
                else if (s.Length > 0)
                {
                    Code128B(C128["codeb"], s, d);
                }
                else
                {
                    // end
                }
                r = string.Join("", d);
            }
            return r;
        }
        // process CODE128 code set A:
        protected void Code128A(char x, string s, List<char> d)
        {
            if (x != C128["shift"])
            {
                d.Add(C128["special"]);
                d.Add(x);
            }
            s = Regex.Replace(s, @"^((?!\d{4,})[\x00-_])+", m => { m.Value.ToList().ForEach(c => d.Add(c)); return ""; });
            s = Regex.Replace(s, @"^\d(?=(\d\d){2,}(\D|$))", m => { d.Add(m.Value[0]); return ""; });
            string t = s.Length > 0 ? s.Substring(1) : "";
            Match n = Regex.Match(t, @"[^ -_]");
            int p = n.Success ? n.Index : -1;
            if (Regex.IsMatch(s, @"^\d{4,}"))
            {
                Code128C(C128["codec"], s, d);
            }
            else if (p >= 0 && t[p] < 32)
            {
                d.Add(C128["special"]);
                d.Add(C128["shift"]);
                d.Add(s[0]);
                Code128A(C128["shift"], t, d);
            }
            else if (s.Length > 0)
            {
                Code128B(C128["codeb"], s, d);
            }
            else
            {
                // end
            }
        }
        // process CODE128 code set B:
        protected void Code128B(char x, string s, List<char> d)
        {
            if (x != C128["shift"])
            {
                d.Add(C128["special"]);
                d.Add(x);
            }
            s = Regex.Replace(s, @"^((?!\d{4,})[ -\x7f])+", m => { m.Value.ToList().ForEach(c => d.Add(c)); return ""; });
            s = Regex.Replace(s, @"^\d(?=(\d\d){2,}(\D|$))", m => { d.Add(m.Value[0]); return ""; });
            string t = s.Length > 0 ? s.Substring(1) : "";
            Match n = Regex.Match(t, @"[^ -_]");
            int p = n.Success ? n.Index : -1;
            if (Regex.IsMatch(s, @"^\d{4,}"))
            {
                Code128C(C128["codec"], s, d);
            }
            else if (p >= 0 && t[p] > 95)
            {
                d.Add(C128["special"]);
                d.Add(C128["shift"]);
                d.Add(s[0]);
                Code128B(C128["shift"], t, d);
            }
            else if (s.Length > 0)
            {
                Code128A(C128["codea"], s, d);
            }
            else
            {
                // end
            }
        }
        // process CODE128 code set C:
        protected void Code128C(char x, string s, List<char> d)
        {
            if (x != C128["shift"])
            {
                d.Add(C128["special"]);
                d.Add(x);
            }
            s = Regex.Replace(s, @"^\d{4,}", m => Regex.Replace(m.Value, @"\d{2}", c => { d.Add((char)int.Parse(c.Value)); return ""; }));
            Match n = Regex.Match(s, @"[^ -_]");
            int p = n.Success ? n.Index : -1;
            if (p >= 0 && s[p] < 32)
            {
                Code128A(C128["codea"], s, d);
            }
            else if (s.Length > 0)
            {
                Code128B(C128["codeb"], s, d);
            }
            else
            {
                // end
            }
        }
    }
}