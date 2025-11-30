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
    // Star Landscape
    //
    class Star90 : Star
    {
        protected int Alignment = 0;
        protected int Width = 48;
        protected int Left = 0;
        protected int Position = 0;
        protected string Content = "";
        protected int Height = 1;
        protected int Feed = 24;
        protected int Cpl = 48;
        protected int MarginRight = 0;
        protected string Buffer = "";
        // start printing: ESC @ ESC RS a n (ESC RS R n) ESC RS F n ESC SP n ESC s n1 n2 ESC GS P 0 ESC GS P 2 n
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
            Position = 0;
            Content = "";
            Height = 1;
            Feed = (int)Math.Round(CharWidth * (printer.Spacing ? 2.5 : 2));
            Cpl = printer.Cpl;
            Margin = printer.Margin;
            MarginRight = printer.MarginRight;
            Buffer = "";
            return $"\u001b@\u001b\u001ea\u0000{(printer.Encoding == "tis620" ? "\u001b\u001eR\u0001" : "")}\u001b\u001eF\u0000\u001b 0\u001bs00\u001b\u001dP0\u001b\u001dP2{(char)(UpsideDown ? 3 : 1)}";
        }
        // finish printing: ESC GS P 3 xL xH yL yH dxL dxH dyL dyH ESC GS P 7 ESC GS ETX s n1 n2
        public override string Close()
        {
            int w = Position + 24;
            int h = Cpl * CharWidth;
            int v = (Margin + Cpl + MarginRight) * CharWidth;
            int m = (UpsideDown ? Margin : MarginRight) * CharWidth;
            return $"\u001b\u001dP3{(char)0}{(char)0}{(char)0}{(char)0}{(char)(w & 255)}{(char)(w >> 8 & 255)}{(char)(v & 255)}{(char)(v >> 8 & 255)} \u001b\u001dP3{(char)0}{(char)0}{(char)(m & 255)}{(char)(m >> 8 & 255)}{(char)(w & 255)}{(char)(w >> 8 & 255)}{(char)(h & 255)}{(char)(h >> 8 & 255)}{Buffer}\u001b\u001dP7{(Cutting ? Cut() : "")}\u001b\u001d\u0003\u0001\u0000\u0000";
        }
        // set print area:
        public override string Area(int left, int width, int right)
        {
            Left = left;
            Width = width;
            return "";
        }
        // set line alignment:
        public override string Align(int align)
        {
            Alignment = align;
            return "";
        }
        // set absolute print position: ESC GS A n1 n2
        public override string Absolute(double position)
        {
            int p = (int)((Left + position) * CharWidth);
            Content += $"\u001b\u001dA{(char)(p & 255)}{(char)(p >> 8 & 255)}";
            return "";
        }
        // set relative print position: ESC GS R n1 n2
        public override string Relative(double position)
        {
            int p = (int)(position * CharWidth);
            Content += $"\u001b\u001dR{(char)(p & 255)}{(char)(p >> 8 & 255)}";
            return "";
        }
        // set line spacing and feed new line:
        public override string VrLf(bool vr)
        {
            Feed = (int)Math.Round(CharWidth * (!vr && Spacing ? 2.5 : 2));
            return Lf();
        }
        // underline text: ESC - n
        public override string Ul()
        {
            Content += "\u001b-1";
            return "";
        }
        // emphasize text: ESC E
        public override string Em()
        {
            Content += "\u001bE";
            return "";
        }
        // invert text: ESC 4
        public override string Iv()
        {
            Content += "\u001b4";
            return "";
        }
        // scale up text: ESC i n1 n2
        public override string Wh(int wh)
        {
            Height = Math.Max(Height, wh < 3 ? wh : wh - 1);
            Content += $"\u001bi{(wh < 3 ? $"{(char)(wh >> 1 & 1)}{(char)(wh & 1)}" : $"{(char)(wh - 2)}{(char)(wh - 2)}")}";
            return "";
        }
        // cancel text decoration: ESC - n ESC F ESC 5 ESC i n1 n2
        public override string Normal()
        {
            Content += $"\u001b-0\u001bF\u001b5\u001bi{(char)0}{(char)0}";
            return "";
        }
        // print text:
        public override string Text(string text, string encoding)
        {
            Content += encoding == "multilingual" ? MultiConv(text) : CodePage[encoding] + Encode(text, encoding);
            return "";
        }
        // feed new line: ESC GS P 4 nL nH ESC GS A n1 n2
        public override string Lf()
        {
            int h = Height * CharWidth * 2;
            int x = Left * CharWidth;
            int y = Position + h * 20 / 24;
            Buffer += $"\u001b\u001dP4{(char)(y & 255)}{(char)(y >> 8 & 255)}\u001b\u001dA{(char)(x & 255)}{(char)(x >> 8 & 255)}{Content}";
            Position += Math.Max(h, Feed);
            Height = 1;
            Content = "";
            return "";
        }
        // print image: ESC GS P 4 nL nH ESC GS A n1 n2 ESC k n1 n2 d1 ... dk
        public override string Image(string image)
        {
            byte[] png = Convert.FromBase64String(image);
            SKBitmap img = SKBitmap.Decode(png);
            byte[] imgdata = img.Bytes;
            int w = img.Width;
            int h = img.Height;
            int px = Left * CharWidth + Alignment * (Width * CharWidth - w) / 2;
            int py = Position + CharWidth * 40 / 24;
            int[] d = new int[w];
            int l = w + 7 >> 3;
            string r = $"\u001b0\u001b\u001dP4{(char)(py & 255)}{(char)(py >> 8 & 255)}";
            int j = 0;
            for (int y = 0; y < h; y += 24)
            {
                r += $"\u001b\u001dA{(char)(px & 255)}{(char)(px >> 8 & 255)}\u001bk{(char)(l & 255)}{(char)(l >> 8 & 255)}";
                for (int z = 0; z < 24; z++)
                {
                    if (y + z < h)
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
                    else
                    {
                        r += new string('\u0000', l);
                    }
                }
                r += "\u000a";
            }
            r += Spacing ? "\u001bz1" : "\u001b0";
            Buffer += r;
            Position += h;
            return "";
        }
        // print QR Code: ESC GS P 4 nL nH ESC GS A n1 n2 ESC k n1 n2 d1 ... dk
        public override string Qrcode(SymbolData symbol, string encoding)
        {
            if (symbol.Data.Length > 0)
            {
                byte[,] matrix = QRCodeGenerator.Generate(symbol);
                int size = matrix.GetLength(1);
                int w = size * symbol.Cell;
                int h = w;
                int x = Left * CharWidth + Alignment * (Width * CharWidth - w) / 2;
                int y = Position + CharWidth * 40 / 24;
                int l = w + 7 >> 3;
                string r = $"\u001b0\u001b\u001dP4{(char)(y & 255)}{(char)(y >> 8 & 255)}";
                List<string> s = new List<string>();
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
                        s.Add(d);
                    }
                }
                while (s.Count % 24 != 0)
                {
                    string d = new string('\u0000', l);
                    s.Add(d);
                }
                for (int k = 0; k < s.Count; k += 24)
                {
                    var a = s.GetRange(k, 24);
                    r += $"\u001b\u001dA{(char)(x & 255)}{(char)(x >> 8 & 255)}\u001bk{(char)(l & 255)}{(char)(l >> 8 & 255)}{string.Join("", a.ToArray())}\u000a";
                }
                r += Spacing ? "\u001bz1" : "\u001b0";
                Buffer += r;
                Position += h;
            }
            return "";
        }
        // print barcode: ESC GS P 4 nL nH ESC GS A n1 n2 ESC b n1 n2 n3 n4 d1 ... dk RS
        public override string Barcode(SymbolData symbol, string encoding)
        {
            BarcodeForm bar = BarcodeGenerator.Generate(symbol);
            if (bar.Length > 0)
            {
                int w = bar.Length;
                switch (symbol.Type)
                {
                    case "code39":
                        w += symbol.Width;
                        break;
                    case "itf":
                        w += bar.Widths.Aggregate(0, (a, c) => (c == 8 ? a + 1 : a));
                        break;
                    default:
                        break;
                }
                int x = Left * CharWidth + Alignment * (Width * CharWidth - w) / 2;
                int y = Position + symbol.Height;
                int h = symbol.Height + (symbol.Hri ? CharWidth * 2 + 2 : 0);
                string r = $"\u001b\u001dP4{(char)(y & 255)}{(char)(y >> 8 & 255)}\u001b\u001dA{(char)(x & 255)}{(char)(x >> 8 & 255)}";
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
                var t = new [] { 49, 56, 50 };
                int u = symbol.Type == "itf" ? t[symbol.Width - 2] : symbol.Width + (Regex.IsMatch(symbol.Type, @"^(code39|codabar|nw7)$") ? 50 : 47);
                r += $"\u001bb{b}{(char)(symbol.Hri ? 50 : 49)}{(char)u}{(char)symbol.Height}{d}\u001e";
                Buffer += r;
                Position += h;
            }
            return "";
        }
    }
}
