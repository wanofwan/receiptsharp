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
    // SII Landscape
    //
    class Sii90 : Thermal90
    {
        public Sii90()
        {
            // image split size
            Split = 1662;
            // QR Code error correction level:
            QrLevel = new Dictionary<string, char>()
            {
                { "l", (char)76 }, { "m", (char)77 }, { "q", (char)81 }, { "h", (char)72 }
            };
            // CODE93 special characters:
            C128 = new Dictionary<string, char>()
            {
                { "special", (char)123 }, { "codea", (char)65 }, { "codeb", (char)66 }, { "codec", (char)67 }, { "shift", (char)83 }
            };
        }
        // start printing: ESC @ GS a n ESC M n ESC SP n FS S n1 n2 FS . GS P x y ESC L ESC T n
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
            return $"\u001b@\u001da\u0000\u001bM0\u001b \u0000\u001cS\u0000\u0000\u001c.\u001dP{(char)r}{(char)r}\u001bL\u001bT{(char)(UpsideDown ? 3 : 1)}";
        }
        // finish printing: ESC W xL xH yL yH dxL dxH dyL dyH ESC $ nL nH FF DC2 q n
        public override string Close()
        {
            int w = Position;
            int h = Cpl * CharWidth;
            int v = (Margin + Cpl + MarginRight) * CharWidth;
            int m = (UpsideDown ? Margin : MarginRight) * CharWidth;
            return $"\u001bW{(char)0}{(char)0}{(char)0}{(char)0}{(char)(w & 255)}{(char)(w >> 8 & 255)}{(char)(v & 255)}{(char)(v >> 8 & 255)} \u001bW{(char)0}{(char)0}{(char)(m & 255)}{(char)(m >> 8 & 255)}{(char)(w & 255)}{(char)(w >> 8 & 255)}{(char)(h & 255)}{(char)(h >> 8 & 255)}{Buffer}\u000c{(Cutting ? Cut() : "")}\u0012q\u0000";
        }
        // feed new line: GS $ nL nH ESC $ nL nH
        public override string Lf()
        {
            int h = Height * CharWidth * 2;
            int x = Left * CharWidth;
            int y = Position + h;
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
                r += $"\u001d${(char)(py + h & 255)}{(char)(py + h >> 8 & 255)}\u001b${(char)(px & 255)}{(char)(px >> 8 & 255)}\u001d8L{(char)(l & 255)}{(char)(l >> 8 & 255)}{(char)(l >> 16 & 255)}{(char)(l >> 24 & 255)}{(char)48}{(char)112}{(char)48}{(char)1}{(char)1}{(char)49}{(char)(w & 255)}{(char)(w >> 8 & 255)}{(char)(h & 255)}{(char)(h >> 8 & 255)}";
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
                string r = $"\u001d${(char)(y + h & 255)}{(char)(y + h >> 8 & 255)}\u001b${(char)(x & 255)}{(char)(x >> 8 & 255)}";
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
                int w = bar.Length + symbol.Width * (Regex.IsMatch(symbol.Type, @"upc|[ej]an") ? (symbol.Data.Length < 9 ? 14 : 18) : 20);
                int l = symbol.Height;
                int h = l + (symbol.Hri ? CharWidth * 2 + 4 : 0);
                int x = Left * CharWidth + Alignment * (Width * CharWidth - w) / 2;
                int y = Position;
                string r = $"\u001d${(char)(y + l & 255)}{(char)(y + l >> 8 & 255)}\u001b${(char)(x & 255)}{(char)(x >> 8 & 255)}";
                string d = Encode(symbol.Data, encoding == "multilingual" ? "ascii" : encoding);
                char b = BarType[symbol.Type];
                if (Regex.IsMatch(symbol.Type, @"upc|[ej]an") && symbol.Data.Length < 9)
                {
                    b++;
                }
                if (b == BarType["upc"] + 1)
                {
                    d = Upce(d);
                }
                else if (b == BarType["codabar"])
                {
                    d = Codabar(d);
                }
                else if (b == BarType["code93"])
                {
                    d = Code93(d);
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
        // generate Codabar data:
        protected string Codabar(string data)
        {
            return data.ToUpper();
        }
        // CODE93 special characters:
        protected string[] C93Escape = new string[]
        {
            "cU", "dA", "dB", "dC", "dD", "dE", "dF", "dG", "dH", "dI", "dJ", "dK", "dL", "dM", "dN", "dO",
            "dP", "dQ", "dR", "dS", "dT", "dU", "dV", "dW", "dX", "dY", "dZ", "cA", "cB", "cC", "cD", "cE",
            " ", "sA", "sB", "sC", "$", "%", "sF", "sG", "sH", "sI", "sJ", "+", "sL", "-", ".", "/",
            "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "sZ", "cF", "cG", "cH", "cI", "cJ",
            "cV", "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O",
            "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z", "cK", "cL", "cM", "cN", "cO",
            "cW", "pA", "pB", "pC", "pD", "pE", "pF", "pG", "pH", "pI", "pJ", "pK", "pL", "pM", "pN", "pO",
            "pP", "pQ", "pR", "pS", "pT", "pU", "pV", "pW", "pX", "pY", "pZ", "cP", "cQ", "cR", "cS", "cT"
        };
        protected Dictionary<char, char> C93Code = new Dictionary<char, char>
        {
            { '0', (char)0 }, { '1', (char)1 }, { '2', (char)2 }, { '3', (char)3 }, { '4', (char)4 },
            { '5', (char)5 }, { '6', (char)6 }, { '7', (char)7 }, { '8', (char)8 }, { '9', (char)9 },
            { 'A', (char)10 }, { 'B', (char)11 }, { 'C', (char)12 }, { 'D', (char)13 }, { 'E', (char)14 },
            { 'F', (char)15 }, { 'G', (char)16 }, { 'H', (char)17 }, { 'I', (char)18 }, { 'J', (char)19 },
            { 'K', (char)20 }, { 'L', (char)21 }, { 'M', (char)22 }, { 'N', (char)23 }, { 'O', (char)24 },
            { 'P', (char)25 }, { 'Q', (char)26 }, { 'R', (char)27 }, { 'S', (char)28 }, { 'T', (char)29 },
            { 'U', (char)30 }, { 'V', (char)31 }, { 'W', (char)32 }, { 'X', (char)33 }, { 'Y', (char)34 },
            { 'Z', (char)35 }, { '-', (char)36 }, { '.', (char)37 }, { ' ', (char)38 }, { '$', (char)39 },
            { '/', (char)40 }, { '+', (char)41 }, { '%', (char)42 }, { 'd', (char)43 }, { 'c', (char)44 },
            { 's', (char)45 }, { 'p', (char)46 }
        };
        protected char C93Start = (char)47;
        protected char C93Stop = (char)48;
        // generate CODE93 data (minimize symbol width):
        protected string Code93(string data)
        {
            string r = "";
            string s = Regex.IsMatch(data, @"[^\x00-\x7f]") ? "" : data;
            if (s.Length > 0)
            {
                List<char> d = s.Aggregate("", (a, c) => a + C93Escape[c]).Select(c => C93Code[c]).ToList();
                d.Add(C93Stop);
                r = string.Join("", d);
            }
            return r;
        }
        // CODE128 special characters:
        protected new Dictionary<string, char> C128 = new Dictionary<string, char>()
        {
            { "starta", (char)103 }, { "startb", (char)104 }, { "startc", (char)105 },
            { "atob", (char)100 }, { "atoc", (char)99 }, { "btoa", (char)101 },
            { "btoc", (char)99 }, { "ctoa", (char)101 }, { "ctob", (char)100 },
            { "shift", (char)98 }, { "stop", (char)105 }
        };
        // generate CODE128 data (minimize symbol width):
        protected new string Code128(string data)
        {
            string r = "";
            string s = Regex.IsMatch(data, @"[^\x00-\x7f]") ? "" : data;
            if (s.Length > 0)
            {
                List<char> d = new List<char>();
                int p = Regex.Match(s, @"[^ -_]").Index;
                if (Regex.IsMatch(s, @"^\d{2}$"))
                {
                    d.Add(C128["startc"]);
                    d.Add((char)int.Parse(s));
                }
                else if (Regex.IsMatch(s, @"^\d{4,}"))
                {
                    Code128C(C128["startc"], s, d);
                }
                else if (p >= 0 && s[p] < 32)
                {
                    Code128A(C128["starta"], s, d);
                }
                else if (s.Length > 0)
                {
                    Code128B(C128["startb"], s, d);
                }
                else
                {
                    // end
                }
                d.Add(C128["stop"]);
                r = string.Join("", d);
            }
            return r;
        }
        // process CODE128 code set A:
        protected new void Code128A(char x, string s, List<char> d)
        {
            if (x != C128["shift"])
            {
                d.Add(x);
            }
            s = Regex.Replace(s, @"^((?!\d{4,})[\x00-_])+", m => { m.Value.ToList().ForEach(c => d.Add((char)((c + 64) % 96))); return ""; });
            s = Regex.Replace(s, @"^\d(?=(\d\d){2,}(\D|$))", m => { d.Add((char)((m.Value[0] + 64) % 96)); return ""; });
            string t = s.Length > 0 ? s.Substring(1) : "";
            Match n = Regex.Match(t, @"[^ -_]");
            int p = n.Success ? n.Index : -1;
            if (Regex.IsMatch(s, @"^\d{4,}"))
            {
                Code128C(C128["atoc"], s, d);
            }
            else if (p >= 0 && t[p] < 32)
            {
                d.Add(C128["shift"]);
                d.Add((char)(s[0] - 32));
                Code128A(C128["shift"], t, d);
            }
            else if (s.Length > 0)
            {
                Code128B(C128["atob"], s, d);
            }
            else
            {
                // end
            }
        }
        // process CODE128 code set B:
        protected new void Code128B(char x, string s, List<char> d)
        {
            if (x != C128["shift"])
            {
                d.Add(x);
            }
            s = Regex.Replace(s, @"^((?!\d{4,})[ -\x7f])+", m => { m.Value.ToList().ForEach(c => d.Add((char)(c - 32))); return ""; });
            s = Regex.Replace(s, @"^\d(?=(\d\d){2,}(\D|$))", m => { d.Add((char)(m.Value[0] - 32)); return ""; });
            string t = s.Length > 0 ? s.Substring(1) : "";
            Match n = Regex.Match(t, @"[^ -_]");
            int p = n.Success ? n.Index : -1;
            if (Regex.IsMatch(s, @"^\d{4,}"))
            {
                Code128C(C128["btoc"], s, d);
            }
            else if (p >= 0 && t[p] > 95)
            {
                d.Add(C128["shift"]);
                d.Add((char)(s[0] + 64));
                Code128B(C128["shift"], t, d);
            }
            else if (s.Length > 0)
            {
                Code128A(C128["btoa"], s, d);
            }
            else
            {
                // end
            }
        }
        // process CODE128 code set C:
        protected new void Code128C(char x, string s, List<char> d)
        {
            if (x != C128["shift"])
            {
                d.Add(x);
            }
            s = Regex.Replace(s, @"^\d{4,}", m => Regex.Replace(m.Value, @"\d{2}", c => { d.Add((char)int.Parse(c.Value)); return ""; }));
            Match n = Regex.Match(s, @"[^ -_]");
            int p = n.Success ? n.Index : -1;
            if (p >= 0 && s[p] < 32)
            {
                Code128A(C128["ctoa"], s, d);
            }
            else if (s.Length > 0)
            {
                Code128B(C128["ctob"], s, d);
            }
            else
            {
                // end
            }
        }
    }
}
