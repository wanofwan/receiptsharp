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
    // Star Line Mode MBCS Japanese
    //
    class StarLineMbcs : StarMbcs
    {
        // finish printing: ESC GS ETX s n1 n2 EOT
        public override string Close()
        {
            return (Cutting ? Cut() : "") + "\u001b\u001d\u0003\u0001\u0000\u0000\u0004";
        }
        // print image: ESC k n1 n2 d1 ... dk
        public override string Image(string image)
        {
            byte[] png = Convert.FromBase64String(image);
            SKBitmap img = SKBitmap.Decode(png);
            byte[] imgdata = img.Bytes;
            int w = img.Width;
            int h = img.Height;
            int[] d = new int[w];
            int l = w + 7 >> 3;
            List<string> s = new List<string>();
            int j = 0;
            for (int y = 0; y < img.Height; y += 24)
            {
                string r = $"\u001bk{(char)(l & 255)}{(char)(l >> 8 & 255)}";
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
                s.Add(r + "\u000a");
            }
            if (UpsideDown)
            {
                s.Reverse();
            }
            return $"\u001b0{string.Join("", s.ToArray())}{(Spacing ? "\u001bz1" : "\u001b0")}";
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
                int l = w + 7 >> 3;
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
                if (UpsideDown)
                {
                    s.Reverse();
                }
                r += "\u001b0";
                for (int k = 0; k < s.Count; k += 24)
                {
                    var a = s.GetRange(k, 24);
                    if (UpsideDown)
                    {
                        a.Reverse();
                    }
                    r += $"\u001bk{(char)(l & 255)}{(char)(l >> 8 & 255)}{string.Join("", a.ToArray())}\u000a";
                }
                r += Spacing ? "\u001bz1" : "\u001b0";
            }
            return r;
        }
    }
}
