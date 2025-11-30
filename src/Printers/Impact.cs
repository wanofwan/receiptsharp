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
    // ESC/POS Impact
    //
    class Impact : Escpos
    {
        protected class TextData
        {
            public string Data;
            public int Index;
            public int Length;
        }
        protected int Font = 0;
        protected int Style = 0;
        protected int Color = 0;
        protected int Left = 0;
        protected int Right = 0;
        protected int Position = 0;
        protected int Margin = 0;
        protected int MarginRight = 0;
        protected List<TextData> Red = new List<TextData>();
        protected List<TextData> Black = new List<TextData>();
        // start printing: ESC @ GS a n ESC M n (ESC 2) (ESC 3 n) ESC { n
        public override string Open(PrintOption printer)
        {
            Style = Font;
            Color = 0;
            Left = 0;
            Right = 0;
            Position = 0;
            Margin = printer.Margin;
            MarginRight = printer.MarginRight;
            Red.Clear();
            Black.Clear();
            UpsideDown = printer.UpsideDown;
            Spacing = printer.Spacing;
            Cutting = printer.Cutting;
            Gradient = printer.Gradient;
            Gamma = printer.Gamma;
            Threshold = printer.Threshold;
            return $"\u001b@\u001da\u0000\u001bM{(char)Font}{(Spacing ? "\u001b2" : "\u001b3\u0012")}\u001b{{{(char)(UpsideDown ? 1 : 0)}\u001c.";
        }
        // finish printing: GS r n
        public override string Close()
        {
            return $"{(Cutting ? Cut() : "")}\u001dr1";
        }
        // set print area:
        public override string Area(int left, int width, int right)
        {
            Left = Margin + left;
            Right = right + MarginRight;
            return "";
        }
        // set line alignment: ESC a n
        public override string Align(int align)
        {
            return $"\u001ba{(char)align}";
        }
        // set absolute print position:
        public override string Absolute(double position)
        {
            Position = (int)position;
            return "";
        }
        // set relative print position:
        public override string Relative(double position)
        {
            Position += (int)Math.Round(position, MidpointRounding.AwayFromZero);
            return "";
        }
        // print horizontal rule: ESC t n ...
        public override string Hr(int width)
        {
            return $"\u001b!{(char)Font}{new string(' ', Left)}\u001bt\u0001{new string('\u0095', width)}";
        }
        // print vertical rules: ESC ! n ESC t n ...
        public override string Vr(int[] widths, int height)
        {
            string d = $"\u001b!{(char)(Font + (height > 1 ? 16 : 0))}\u001bt\u0001\u0096";
            Black.Add(new TextData() { Data = d, Index = Position, Length = 1 });
            foreach (int w in widths)
            {
                Position += w + 1;
                Black.Add(new TextData() { Data = d, Index = Position, Length = 1 });
            }
            return "";
        }
        // start rules: ESC ! n ESC t n ...
        public override string VrStart(int[] widths)
        {
            string s = widths.Aggregate("\u009c", (a, w) => $"{a}{new string('\u0095', w)}\u0091");
            return $"\u001b!{(char)Font}{new string(' ', Left)}\u001bt\u0001{s.Substring(0, s.Length - 1)}\u009d";
        }
        // stop rules: ESC ! n ESC t n ...
        public override string VrStop(int[] widths)
        {
            string s = widths.Aggregate("\u009e", (a, w) => $"{a}{new string('\u0095', w)}\u0090");
            return $"\u001b!{(char)Font}{new string(' ', Left)}\u001bt\u0001{s.Substring(0, s.Length - 1)}\u009f";
        }
        // print vertical and horizontal rules: ESC ! n ESC t n ...
        public override string VrHr(int[] widths1, int[] widths2, int dl, int dr)
        {
            string s1 = widths1.Aggregate(dl > 0 ? "\u009e" : "\u009a", (a, w) => $"{a}{new string('\u0095', w)}\u0090");
            string r1 = new string(' ', Math.Max(-dl, 0)) + s1.Substring(0, s1.Length - 1) + (dr < 0 ? '\u009f' : '\u009b') + new string(' ', Math.Max(dr, 0));
            string s2 = widths2.Aggregate(dl < 0 ? "\u009c" : "\u0098", (a, w) => $"{a}{new string('\u0095', w)}\u0091");
            string r2 = new string(' ', Math.Max(dl, 0)) + s2.Substring(0, s2.Length - 1) + (dr > 0 ? '\u009d' : '\u0099') + new string(' ', Math.Max(-dr, 0));
            return $"\u001b!{(char)Font}{new string(' ', Left)}\u001bt\u0001{string.Concat(r2.Select((c, i) => VrTable[c][r1[i]]))}";
        }
        // set line spacing and feed new line: (ESC 2) (ESC 3 n)
        public override string VrLf(bool vr)
        {
            return (vr == UpsideDown && Spacing ? "\u001b2" : "\u001b3\u0012") + Lf();
        }
        // cut paper: GS V m n
        public override string Cut()
        {
            return "\u001dVB\u0000";
        }
        // underline text:
        public override string Ul()
        {
            Style += 128;
            return "";
        }
        // emphasize text:
        public override string Em()
        {
            Style += 8;
            return "";
        }
        // invert text:
        public override string Iv()
        {
            Color = 1;
            return "";
        }
        // scale up text:
        public override string Wh(int wh)
        {
            if (wh > 0)
            {
                Style += wh < 3 ? 64 >> wh : 48;
            }
            return "";
        }
        // cancel text decoration:
        public override string Normal()
        {
            Style = Font;
            Color = 0;
            return "";
        }
        // print text:
        public override string Text(string text, string encoding)
        {
            string t = Encode(text, encoding == "multilingual" ? "ascii" : encoding);
            string d = $"\u001b!{(char)Style}{(encoding == "multilingual" ? MultiConv(text) : CodePage[encoding] + Encode(text, encoding))}";
            int l = t.Length * ((Style & 32) != 0 ? 2 : 1);
            if (Color > 0)
            {
                Red.Add(new TextData() { Data = d, Index = Position, Length = l });
            }
            else
            {
                Black.Add(new TextData() { Data = d, Index = Position, Length = l });
            }
            Position += l;
            return "";
        }
        // feed new line: LF
        public override string Lf()
        {
            string r = "";
            if (Red.Count > 0)
            {
                int p = 0;
                Red.Sort((a, b) => a.Index - b.Index);
                r += Red.Aggregate($"\u001br\u0001\u001b!{(char)Font}{new string(' ', Left)}", (a, c) => {
                    string s = $"{a}\u001b!{(char)Font}{new string(' ', c.Index - p)}{c.Data}";
                    p = c.Index + c.Length;
                    return s;
                }) + "\u000d\u001br\u0000";
            }
            if (Black.Count > 0)
            {
                int p = 0;
                Black.Sort((a, b) => a.Index - b.Index);
                r += Black.Aggregate($"\u001b!{(char)Font}{new string(' ', Left)}", (a, c) => {
                    string s = $"{a}\u001b!{(char)Font}{new string(' ', c.Index - p)}{c.Data}";
                    p = c.Index + c.Length;
                    return s;
                });
            }
            r += "\u000a";
            Position = 0;
            Red.Clear();
            Black.Clear();
            return r;
        }
        // insert commands:
        public override string Command(string command)
        {
            return command;
        }
        // print image: ESC * 0 wL wH d1 ... dk ESC J n
        public override string Image(string image)
        {
            string r = "";
            byte[] png = Convert.FromBase64String(image);
            SKBitmap img = SKBitmap.Decode(png);
            byte[] imgdata = img.Bytes;
            int w = img.Width;
            if (w < 1024)
            {
                int[] d = new int[w];
                int j = UpsideDown ? imgdata.Length - 4 : 0;
                for (int y = 0; y < img.Height; y += 8)
                {
                    int[] b = new int[w];
                    int h = Math.Min(8, img.Height - y);
                    for (int p = 0; p < h; p++)
                    {
                        int i = 0, e = 0;
                        for (int x = 0; x < w; x++)
                        {
                            int f = (int)Math.Floor((d[i] + e * 5) / 16 + Math.Pow(((imgdata[j] * .299 + imgdata[j + 1] * .587 + imgdata[j + 2] * .114 - 255) * imgdata[j + 3] + 65525) / 65525, 1 / Gamma) * 255);
                            j += UpsideDown ? -4 : 4;
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
                    r += $"{new string(' ', Left)}\u001b*\u0000{(char)(w & 255)}{(char)(w >> 8 & 255)}{b.Aggregate("", (a, c) => a + (char)c)}{new string(' ', Right)}\u001bJ{(char)(h * 2)}";
                }
            }
            return r;
        }
    }
}
