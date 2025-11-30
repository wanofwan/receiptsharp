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
using SkiaSharp;

namespace ReceiptSharp.Printers
{
    //
    // Star Graphic Mode
    //
    class StarGraphic : Star
    {
        protected int Alignment = 0;
        protected int Left = 0;
        protected int Width = 48;
        protected int Right = 0;
        // start printing: ESC RS a n ESC * r A ESC * r P n NUL (ESC * r E n NUL)
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
            Margin = (printer.UpsideDown ? printer.MarginRight : printer.Margin) * CharWidth;
            return $"\u001b\u001ea\u0000\u001b*rA\u001b*rP0\u0000{(Cutting ? "" : "\u001b*rE1\u0000")}";
        }
        // finish printing: ESC * r B ESC ACK SOH
        public override string Close()
        {
            return "\u001b*rB\u001b\u0006\u0001";
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
        // cut paper: ESC FF NUL
        public override string Cut()
        {
            return "\u001b\u000c\u0000";
        }
        // feed new line: ESC * r Y n NUL
        public override string Lf()
        {
            return $"\u001b*rY{(char)Math.Round(CharWidth * (Spacing ? 2.5 : 2))}\u0000";
        }
        // insert commands:
        public override string Command(string command)
        {
            return command;
        }
        // print image: b n1 n2 data
        public override string Image(string image)
        {
            string r = "";
            byte[] png = Convert.FromBase64String(image);
            SKBitmap img = SKBitmap.Decode(png);
            byte[] imgdata = img.Bytes;
            int w = img.Width;
            int[] d = new int[w];
            int m = Margin + Math.Max((UpsideDown ? Right : Left) * CharWidth + (Width * CharWidth - w) * (UpsideDown ? 2 - Alignment : Alignment) >> 1, 0);
            int l = m + w + 7 >> 3;
            int j = UpsideDown ? imgdata.Length - 4 : 0;
            for (int y = 0; y < img.Height; y++)
            {
                int i = 0, e = 0;
                r += $"b{(char)(l & 255)}{(char)(l >> 8 & 255)}";
                for (int x = 0; x < m + w; x += 8)
                {
                    int b = 0;
                    int q = Math.Min(m + w - x, 8);
                    for (int p = 0; p < q; p++)
                    {
                        if (m <= x + p)
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
                    }
                    r += (char)b;
                }
            }
            return r;
        }
    }
}
