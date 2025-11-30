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
using SkiaSharp;

namespace ReceiptSharp.Printers
{
    //
    // Fujitsu Isotec
    //
    class Fit : Thermal
    {
        public Fit()
        {
            // image split size
            Split = 1662;
        }
        // print image: GS 8 L p1 p2 p3 p4 m fn a bx by c xL xH yL yH d1 ... dk GS ( L pL pH m fn
        public override string Image(string image)
        {
            byte[] png = Convert.FromBase64String(image);
            SKBitmap img = SKBitmap.Decode(png);
            byte[] imgdata = img.Bytes;
            int w = img.Width;
            int[] d = new int[w];
            List<string> s = new List<string>();
            int j = 0;
            for (int z = 0; z < img.Height; z += Split)
            {
                int h = Math.Min(Split, img.Height - z);
                int l = (w + 7 >> 3) * h + 10;
                string r = $"\u001d8L{(char)(l & 255)}{(char)(l >> 8 & 255)}{(char)(l >> 16 & 255)}{(char)(l >> 24 & 255)}{(char)48}{(char)112}{(char)48}{(char)1}{(char)1}{(char)49}{(char)(w & 255)}{(char)(w >> 8 & 255)}{(char)(h & 255)}{(char)(h >> 8 & 255)}";
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
                    r += $"\u001d(L{(char)2}{(char)0}{(char)48}{(char)50}";
                    s.Add(r);
                }
                if (UpsideDown)
                {
                    s.Reverse();
                }
            }
            return (UpsideDown && Alignment == 2 ? Area(Right, Width, Left) : "") + string.Join("", s.ToArray());
        }
        // print QR Code: GS ( k pL pH cn fn n1 n2 GS ( k pL pH cn fn n GS ( k pL pH cn fn n GS ( k pL pH cn fn m d1 ... dk GS ( k pL pH cn fn m
        public override string Qrcode(SymbolData symbol, string encoding)
        {
            string r = UpsideDown && Alignment == 2 ? Area(Right, Width, Left)  : "";
            if (symbol.Data.Length > 0)
            {
                byte[,] matrix = QRCodeGenerator.Generate(symbol);
                int size = matrix.GetLength(1);
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
    }
}
