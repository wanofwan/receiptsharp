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
using System.Drawing;
using System.Linq;
using SkiaSharp;

namespace ReceiptSharp.Printers
{
    //
    // ESC/POS Generic
    //
    class Generic : Thermal
    {
        public Generic()
        {
            // image split size
            Split = 2048;
        }
        // start printing: ESC @ GS a n ESC M n ESC SP n FS S n1 n2 (ESC 2) (ESC 3 n) ESC { n FS .
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
            return $"\u001b@\u001da\u0000\u001bM\u0000\u001b \u0000\u001cS\u0000\u0000{(Spacing ? "\u001b2" : "\u001b3\u0000")}\u001b{{{(char)(UpsideDown ? 1 : 0)}\u001c.";
        }
        // finish printing: GS r n
        public override string Close()
        {
            return (Cutting ? Cut() : "") + "\u001dr\u0001";
        }
        // print horizontal rule: FS C n FS . ESC t n ...
        public override string Hr(int width)
        {
            return $"\u001cC\u0000\u001c.\u001bt\u0001{new string('\u0095', width)}";
        }
        // print vertical rules: GS ! n FS C n FS . ESC t n ...
        public override string Vr(int[] widths, int height)
        {
            return widths.Aggregate($"\u001d!{(char)(height - 1)}\u001cC\u0000\u001c.\u001bt\u0001\u0096", (a, w) => $"{a}{Relative(w)}\u0096");
        }
        // start rules: FS C n FS . ESC t n ...
        public override string VrStart(int[] widths)
        {
            string s = widths.Aggregate("\u009c", (a, w) => $"{a}{new string('\u0095', w)}\u0091");
            return $"\u001cC\u0000\u001c.\u001bt\u0001{s.Substring(0, s.Length - 1)}\u009d";
        }
        // stop rules: FS C n FS . ESC t n ...
        public override string VrStop(int[] widths)
        {
            string s = widths.Aggregate("\u009e", (a, w) => $"{a}{new string('\u0095', w)}\u0090");
            return $"\u001cC\u0000\u001c.\u001bt\u0001{s.Substring(0, s.Length - 1)}\u009f";
        }
        // print vertical and horizontal rules: FS C n FS . ESC t n ...
        public override string VrHr(int[] widths1, int[] widths2, int dl, int dr)
        {
            string s1 = widths1.Aggregate(dl > 0 ? "\u009e" : "\u009a", (a, w) => $"{a}{new string('\u0095', w)}\u0090");
            string r1 = $"{new string(' ', Math.Max(-dl, 0))}{s1.Substring(0, s1.Length - 1)}{(dr < 0 ? "\u009f" : "\u009b")}{new string(' ', Math.Max(dr, 0))}";
            string s2 = widths2.Aggregate(dl < 0 ? "\u009c" : "\u0098", (a, w) => $"{a}{new string('\u0095', w)}\u0091");
            string r2 = $"{new string(' ', Math.Max(dl, 0))}{s2.Substring(0, s2.Length - 1)}{(dr > 0 ? "\u009d" : "\u0099")}{new string(' ', Math.Max(-dr, 0))}";
            return $"\u001cC\u0000\u001c.\u001bt\u0001{string.Concat(r2.Select((c, i) => VrTable[c][r1[i]]))}";
        }
        // underline text: ESC - n FS - n
        public override string Ul()
        {
            return "\u001b-\u0002\u001c-\u0002";
        }
        // emphasize text: ESC E n
        public override string Em()
        {
            return "\u001bE\u0001";
        }
        // invert text: GS B n
        public override string Iv()
        {
            return "\u001dB\u0001";
        }
        // scale up text: GS ! n
        public override string Wh(int wh)
        {
            return $"\u001d!{(char)(wh < 3 ? ((wh & 1) << 4 | wh >> 1 & 1) : (wh - 2 << 4 | wh - 2))}";
        }
        // cancel text decoration: ESC - n FS - n ESC E n GS B n GS ! n
        public override string Normal()
        {
            return "\u001b-\u0000\u001c-\u0000\u001bE\u0000\u001dB\u0000\u001d!\u0000";
        }
        // print image: GS v 0 m xL xH yL yH d1 ... dk
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
                int l = w + 7 >> 3;
                r += $"\u001dv0{(char)0}{(char)(l & 255)}{(char)(l >> 8 & 255)}{(char)(h & 255)}{(char)(h >> 8 & 255)}";
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
                    for (int i = 0; i < center; i++)
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
                int l = w + 7 >> 3;
                r += $"\u001dv0{(char)0}{(char)(l & 255)}{(char)(l >> 8 & 255)}{(char)(h & 255)}{(char)(h >> 8 & 255)}";
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
    }
}
