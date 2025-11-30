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
using System.Linq;
using System.Text.RegularExpressions;
using SkiaSharp;

namespace ReceiptSharp.Printers
{
    //
    // ESC/POS Thermal Landscape
    //
    class Thermal90 : Thermal
    {
        protected int Position = 0;
        protected string Content = "";
        protected int Height = 1;
        protected int Feed = 24;
        protected int Cpl = 48;
        protected string Buffer = "";
        // start printing: ESC @ GS a n ESC M n FS ( A pL pH fn m ESC SP n FS S n1 n2 FS . GS P x y ESC L ESC T n
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
            Position = 0;
            Content = "";
            Height = 1;
            Feed = (int)Math.Round(CharWidth * (printer.Spacing ? 2.5 : 2));
            Cpl = printer.Cpl;
            Margin = printer.Margin;
            MarginRight = printer.MarginRight;
            Buffer = "";
            int r = printer.Resolution;
            return $"\u001b@\u001da\u0000\u001bM{(printer.Encoding == "tis620" ? 'a' : '0')}\u001c(A{(char)2}{(char)0}{(char)48}{(char)0}\u001b \u0000\u001cS\u0000\u0000\u001c.\u001dP{(char)r}{(char)r}\u001bL\u001bT{(char)(UpsideDown ? 3 : 1)}";
        }
        // finish printing: ESC W xL xH yL yH dxL dxH dyL dyH FF GS r n
        public override string Close()
        {
            int w = Position;
            int h = Cpl * CharWidth;
            int v = (Margin + Cpl + MarginRight) * CharWidth;
            int m = (UpsideDown ? Margin : MarginRight) * CharWidth;
            return $"\u001bW{(char)0}{(char)0}{(char)0}{(char)0}{(char)(w & 255)}{(char)(w >> 8 & 255)}{(char)(v & 255)}{(char)(v >> 8 & 255)} \u001bW{(char)0}{(char)0}{(char)(m & 255)}{(char)(m >> 8 & 255)}{(char)(w & 255)}{(char)(w >> 8 & 255)}{(char)(h & 255)}{(char)(h >> 8 & 255)}{Buffer}\u000c{(Cutting ? Cut() : "")}\u001dr1";
        }
        // set print area:
        public override string Area(int left, int width, int right)
        {
            Left = left;
            Width = width;
            Right = right;
            return "";
        }
        // set line alignment:
        public override string Align(int align)
        {
            Alignment = align;
            return "";
        }
        // set absolute print position: ESC $ nL nH
        public override string Absolute(double position)
        {
            int p = (int)((Left + position) * CharWidth);
            Content += $"\u001b${(char)(p & 255)}{(char)(p >> 8 & 255)}";
            return "";
        }
        // set relative print position: ESC \ nL nH
        public override string Relative(double position)
        {
            int p = (int)(position * CharWidth);
            Content += $"\u001b\\{(char)(p & 255)}{(char)(p >> 8 & 255)}";
            return "";
        }
        // print horizontal rule: FS C n FS . ESC t n ...
        public override string Hr(int width)
        {
            Content += $"\u001cC0\u001c.\u001bt\u0001{new string('\u0095', width)}";
            return "";
        }
        // print vertical rules: GS ! n FS C n FS . ESC t n ...
        public override string Vr(int[] widths, int height)
        {
            Content += widths.Aggregate($"\u001d!{(char)(height - 1)}\u001cC0\u001c.\u001bt\u0001\u0096", (a, w) =>
            {
                int p = w * CharWidth;
                return $"{a}\u001b\\{(char)(p & 255)}{(char)(p >> 8 & 255)}\u0096";
            });
            return "";
        }
        // start rules: FS C n FS . ESC t n ...
        public override string VrStart(int[] widths)
        {
            string s = widths.Aggregate("\u009c", (a, w) => $"{a}{new string('\u0095', w)}\u0091");
            Content += $"\u001cC0\u001c.\u001bt\u0001{s.Substring(0, s.Length - 1)}\u009d";
            return "";
        }
        // stop rules: FS C n FS . ESC t n ...
        public override string VrStop(int[] widths)
        {
            string s = widths.Aggregate("\u009e", (a, w) => $"{a}{new string('\u0095', w)}\u0090");
            Content += $"\u001cC0\u001c.\u001bt\u0001{s.Substring(0, s.Length - 1)}\u009f";
            return "";
        }
        // print vertical and horizontal rules: FS C n FS . ESC t n ...
        public override string VrHr(int[] widths1, int[] widths2, int dl, int dr)
        {
            string s1 = widths1.Aggregate(dl > 0 ? "\u009e" : "\u009a", (a, w) => $"{a}{new string('\u0095', w)}\u0090");
            string r1 = $"{new string(' ', Math.Max(-dl, 0))}{s1.Substring(0, s1.Length - 1)}{(dr < 0 ? "\u009f" : "\u009b")}{new string(' ', Math.Max(dr, 0))}";
            string s2 = widths2.Aggregate(dl < 0 ? "\u009c" : "\u0098", (a, w) => $"{a}{new string('\u0095', w)}\u0091");
            string r2 = $"{new string(' ', Math.Max(dl, 0))}{s2.Substring(0, s2.Length - 1)}{(dr > 0 ? "\u009d" : "\u0099")}{new string(' ', Math.Max(-dr, 0))}";
            Content += $"\u001cC0\u001c.\u001bt\u0001{string.Concat(r2.Select((c, i) => VrTable[c][r1[i]]))}";
            return "";
        }
        // set line spacing and feed new line:
        public override string VrLf(bool vr)
        {
            Feed = (int)Math.Round(CharWidth * (!vr && Spacing ? 2.5 : 2));
            return Lf();
        }
        // underline text: ESC - n FS - n
        public override string Ul()
        {
            Content += "\u001b-2\u001c-2";
            return "";
        }
        // emphasize text: ESC E n
        public override string Em()
        {
            Content += "\u001bE1";
            return "";
        }
        // invert text: GS B n
        public override string Iv()
        {
            Content += "\u001dB1";
            return "";
        }
        // scale up text: GS ! n
        public override string Wh(int wh)
        {
            Height = Math.Max(Height, wh < 3 ? wh : wh - 1);
            Content += $"\u001d!{(char)(wh < 3 ? (wh & 1) << 4 | wh >> 1 & 1 : wh - 2 << 4 | wh - 2)}";
            return "";
        }
        // cancel text decoration: ESC - n FS - n ESC E n GS B n GS ! n
        public override string Normal()
        {
            Content += "\u001b-0\u001c-0\u001bE0\u001dB0\u001d!\u0000";
            return "";
        }
        // print text:
        public override string Text(string text, string encoding)
        {
            switch (encoding)
            {
                case "multilingual":
                    Content += MultiConv(text);
                    break;
                case "tis620":
                    Content += CodePage[encoding] + ArrayFrom(text, encoding).Aggregate("", (a, c) => $"{a}\u0000{Encode(c, encoding)}");
                    break;
                default:
                    Content += CodePage[encoding] + Encode(text, encoding);
                    break;
            }
            return "";
        }
        // feed new line: GS $ nL nH ESC $ nL nH
        public override string Lf()
        {
            int h = Height * CharWidth * 2;
            int x = Left * CharWidth;
            int y = Position + h * 21 / 24 - 1;
            Buffer += $"\u001d${(char)(y & 255)}{(char)(y >> 8 & 255)}\u001b${(char)(x & 255)}{(char)(x >> 8 & 255)}{Content}";
            Position += Math.Max(h, Feed);
            Height = 1;
            Content = "";
            return "";
        }
        // print image: GS $ nL nH ESC $ nL nH GS 8 L p1 p2 p3 p4 m fn a bx by c xL xH yL yH d1 ... dk
        public override string Image(string image)
        {
            byte[] png = Convert.FromBase64String(image);
            SKBitmap img = SKBitmap.Decode(png);
            byte[] imgdata = img.Bytes;
            int w = img.Width;
            int px = Left * CharWidth + Alignment * (Width * CharWidth - w) / 2;
            int py = Position;
            string r = "";
            int[] d = new int[w];
            int j = 0;
            for (int z = 0; z < img.Height; z += Split)
            {
                int h = Math.Min(Split, img.Height - z);
                int l = (w + 7 >> 3) * h + 10;
                r += $"\u001d${(char)(py + h - 1 & 255)}{(char)(py + h - 1 >> 8 & 255)}\u001b${(char)(px & 255)}{(char)(px >> 8 & 255)}\u001d8L{(char)(l & 255)}{(char)(l >> 8 & 255)}{(char)(l >> 16 & 255)}{(char)(l >> 24 & 255)}{(char)48}{(char)112}{(char)48}{(char)1}{(char)1}{(char)49}{(char)(w & 255)}{(char)(w >> 8 & 255)}{(char)(h & 255)}{(char)(h >> 8 & 255)}";
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
            }
            Buffer += r;
            Position += img.Height;
            return "";
        }
        // print QR Code: GS $ nL nH ESC $ nL nH GS 8 L p1 p2 p3 p4 m fn a bx by c xL xH yL yH d1 ... dk
        public override string Qrcode(SymbolData symbol, string encoding)
        {
            if (symbol.Data.Length > 0)
            {
                byte[,] matrix = QRCodeGenerator.Generate(symbol);
                int size = matrix.GetLength(1);
                int w = size * symbol.Cell;
                int h = w;
                int x = Left * CharWidth + Alignment * (Width * CharWidth - w) / 2;
                int y = Position;
                string r = $"\u001d${(char)(y + h - 1 & 255)}{(char)(y + h - 1 >> 8 & 255)}\u001b${(char)(x & 255)}{(char)(x >> 8 & 255)}";
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
                            if (matrix[i, (int)Math.Floor((double)((j + p) / symbol.Cell)) * 2] == 1)
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
                Buffer += r;
                Position += h;
            }
            return "";
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
    }
}