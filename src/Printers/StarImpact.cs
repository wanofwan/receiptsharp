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
using SkiaSharp;

namespace ReceiptSharp.Printers
{
    //
    // Star Mode on dot impact printers
    //
    class StarImpact : StarSbcs
    {
        protected int Font = 0;
        // start printing: ESC @ ESC RS a n (ESC M) (ESC P) (ESC :) ESC SP n ESC s n1 n2 (ESC z n) (ESC 0) (SI) (DC2)
        public override string Open(PrintOption printer)
        {
            UpsideDown = printer.UpsideDown;
            Spacing = printer.Spacing;
            Cutting = printer.Cutting;
            Gradient = printer.Gradient;
            Gamma = printer.Gamma;
            Threshold = printer.Threshold;
            Margin = printer.Margin;
            var f = new [] { 'M', 'P', ':' };
            return $"\u001b@\u001b\u001ea\u0000\u001b{f[Font]}\u001b \u0000\u001bs\u0000\u0000{(Spacing ? "\u001bz\u0001" : "\u001b0")}{(UpsideDown ? "\u000f" : "\u0012")}";
        }
        // finish printing: ESC GS ETX s n1 n2 EOT
        public override string Close()
        {
            return $"{(Cutting ? Cut() : "")}\u001b\u001d\u0003\u0001\u0000\u0000\u0004";
        }
        // scale up text: ESC W n ESC h n
        public override string Wh(int wh)
        {
            return $"\u001bW{(char)(wh < 3 ? wh & 1 : 1)}\u001bh{(char)(wh < 3 ? wh >> 1 & 1 : 1)}";
        }
        // cancel text decoration: ESC - n ESC F ESC 5 ESC W n ESC h n
        public override string Normal()
        {
            return $"\u001b-\u0000\u001bF\u001b5\u001bW{(char)0}\u001bh{(char)0}";
        }
        // print image: ESC 0 ESC K n NUL d1 ... dn LF (ESC z n) (ESC 0)
        public override string Image(string image)
        {
            byte[] png = Convert.FromBase64String(image);
            SKBitmap img = SKBitmap.Decode(png);
            byte[] imgdata = img.Bytes;
            int w = Math.Min(img.Width, 255);
            int[] d = new int[w];
            List<string> s = new List<string>();
            for (int y = 0; y < img.Height; y += 8)
            {
                int[] b = new int[w];
                int h = Math.Min(8, img.Height - y);
                for (int p = 0; p < h; p++)
                {
                    int i = 0, e = 0;
                    int j = (y + p) * img.Width * 4;
                    for (int x = 0; x < w; x++)
                    {
                        int f = (int)Math.Floor((d[i] + e * 5) / 16 + Math.Pow(((imgdata[j] * .299 + imgdata[j + 1] * .587 + imgdata[j + 2] * .114 - 255) * imgdata[j + 3] + 65525) / 65525, 1 / Gamma) * 255);
                        j += 4;
                        if (Gradient)
                        {
                            d[i] = e * 3;
                            if (f < Threshold)
                            {
                                e = f;
                                if (UpsideDown)
                                {
                                    b[w - x - 1] |= 1 << p;
                                }
                                else
                                {
                                    b[x] |= 128 >> p;
                                }
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
                                if (UpsideDown)
                                {
                                    b[w - x - 1] |= 1 << p;
                                }
                                else
                                {
                                    b[x] |= 128 >> p;
                                }
                            }
                        }
                    }
                }
                s.Add($"\u001bK{(char)w}\u0000{b.Aggregate("", (a, c) => a + (char)c)}\u000a");
            }
            if (UpsideDown)
            {
                s.Reverse();
            }
            return $"\u001b0{string.Join("", s.ToArray())}{(Spacing ? "\u001bz\u0001" : "\u001b0")}";
        }
        // print QR Code:
        public override string Qrcode(SymbolData symbol, string encoding)
        {
            return "";
        }
        // print barcode:
        public override string Barcode(SymbolData symbol, string encoding)
        {
            return "";
        }
    }
}
