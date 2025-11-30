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

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ReceiptSharp
{
    public static class BarcodeGenerator
    {
        // CODE128 patterns:
        private static class C128
        {
            public static string[] Element = new string[]
            {
                "212222", "222122", "222221", "121223", "121322", "131222", "122213", "122312", "132212", "221213", "221312", "231212",
                "112232", "122132", "122231", "113222", "123122", "123221", "223211", "221132", "221231", "213212", "223112", "312131",
                "311222", "321122", "321221", "312212", "322112", "322211", "212123", "212321", "232121", "111323", "131123", "131321",
                "112313", "132113", "132311", "211313", "231113", "231311", "112133", "112331", "132131", "113123", "113321", "133121",
                "313121", "211331", "231131", "213113", "213311", "213131", "311123", "311321", "331121", "312113", "312311", "332111",
                "314111", "221411", "431111", "111224", "111422", "121124", "121421", "141122", "141221", "112214", "112412", "122114",
                "122411", "142112", "142211", "241211", "221114", "413111", "241112", "134111", "111242", "121142", "121241", "114212",
                "124112", "124211", "411212", "421112", "421211", "212141", "214121", "412121", "111143", "111341", "131141", "114113",
                "114311", "411113", "411311", "113141", "114131", "311141", "411131", "211412", "211214", "211232", "2331112"
            };
            public static int StartA = 103;
            public static int StartB = 104;
            public static int StartC = 105;
            public static int AtoB = 100;
            public static int AtoC = 99;
            public static int BtoA = 101;
            public static int BtoC = 99;
            public static int CtoA = 101;
            public static int CtoB = 100;
            public static int Shift = 98;
            public static int Stop = 106;
        }
        // generate CODE128 data (minimize symbol width):
        private static BarcodeForm Code128(SymbolData symbol)
        {
            BarcodeForm r = new BarcodeForm();
            string s = Regex.IsMatch(symbol.Data, @"[^\x00-\x7f]") ? "" : symbol.Data;
            if (s.Length > 0)
            {
                // generate HRI
                r.Hri = symbol.Hri;
                r.Text = Regex.Replace(s, @"[\x00- \x7f]", " ");
                // minimize symbol width
                List<int> d = new List<int>();
                Match n = Regex.Match(s, @"[^ -_]");
                int p = n.Success ? n.Index : -1;
                if (Regex.IsMatch(s, @"^\d{2}$"))
                {
                    d.Add(C128.StartC);
                    d.Add(int.Parse(s));
                }
                else if (Regex.IsMatch(s, @"^\d{4,}"))
                {
                    Code128C(C128.StartC, s, d);
                }
                else if (p >= 0 && s[p] < 32)
                {
                    Code128A(C128.StartA, s, d);
                }
                else if (s.Length > 0)
                {
                    Code128B(C128.StartB, s, d);
                }
                else
                {
                    // end
                }
                // calculate check digit and append stop character
                int i = 1;
                d.Add(d.Aggregate((a, c) => a + c * i++) % 103);
                d.Add(C128.Stop);
                // generate bars and spaces
                string q = symbol.QuietZone ? "a" : "0";
                string m = d.Aggregate(q, (a, c) => a + C128.Element[c]) + q;
                r.Widths = m.Select(c => (c - (c < 'a' ? '0' : 'W')) * symbol.Width).ToArray();
                r.Length = symbol.Width * (d.Count * 11 + (symbol.QuietZone ? 22 : 2));
                r.Height = symbol.Height;
            }
            return r;
        }
        // process CODE128 code set A:
        private static void Code128A(int x, string s, List<int> d)
        {
            if (x != C128.Shift)
            {
                d.Add(x);
            }
            s = Regex.Replace(s, @"^((?!\d{4,})[\x00-_])+", m => { m.Value.ToList().ForEach(c => d.Add((c + 64) % 96)); return ""; });
            s = Regex.Replace(s, @"^\d(?=(\d\d){2,}(\D|$))", m => { d.Add((m.Value[0] + 64) % 96); return ""; });
            string t = s.Length > 0 ? s.Substring(1) : "";
            Match n = Regex.Match(t, @"[^ -_]");
            int p = n.Success ? n.Index : -1;
            if (Regex.IsMatch(s, @"^\d{4,}"))
            {
                Code128C(C128.AtoC, s, d);
            }
            else if (p >= 0 && t[p] < 32)
            {
                d.Add(C128.Shift);
                d.Add(s[0] - 32);
                Code128A(C128.Shift, t, d);
            }
            else if (s.Length > 0)
            {
                Code128B(C128.AtoB, s, d);
            }
            else
            {
                // end
            }
        }
        // process CODE128 code set B:
        private static void Code128B(int x, string s, List<int> d)
        {
            if (x != C128.Shift)
            {
                d.Add(x);
            }
            s = Regex.Replace(s, @"^((?!\d{4,})[ -\x7f])+", m => { m.Value.ToList().ForEach(c => d.Add(c - 32)); return ""; });
            s = Regex.Replace(s, @"^\d(?=(\d\d){2,}(\D|$))", m => { d.Add(m.Value[0] - 32); return ""; });
            string t = s.Length > 0 ? s.Substring(1) : "";
            Match n = Regex.Match(t, @"[^ -_]");
            int p = n.Success ? n.Index : -1;
            if (Regex.IsMatch(s, @"^\d{4,}"))
            {
                Code128C(C128.BtoC, s, d);
            }
            else if (p >= 0 && t[p] > 95)
            {
                d.Add(C128.Shift);
                d.Add(s[0] + 64);
                Code128B(C128.Shift, t, d);
            }
            else if (s.Length > 0)
            {
                Code128A(C128.BtoA, s, d);
            }
            else
            {
                // end
            }
        }
        // process CODE128 code set C:
        private static void Code128C(int x, string s, List<int> d)
        {
            if (x != C128.Shift)
            {
                d.Add(x);
            }
            s = Regex.Replace(s, @"^\d{4,}", m => Regex.Replace(m.Value, @"\d{2}", c => { d.Add(int.Parse(c.Value)); return ""; }));
            Match n = Regex.Match(s, @"[^ -_]");
            int p = n.Success ? n.Index : -1;
            if (p >= 0 && s[p] < 32)
            {
                Code128A(C128.CtoA, s, d);
            }
            else if (s.Length > 0)
            {
                Code128B(C128.CtoB, s, d);
            }
            else
            {
                // end
            }
        }
        // CODE93 patterns:
        private static class C93
        {
            public static string[] Escape = new string[]
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
            public static Dictionary<char, int> Code = new Dictionary<char, int>()
            {
                { '0', 0 }, { '1', 1 }, { '2', 2 }, { '3', 3 }, { '4', 4 }, { '5', 5 }, { '6', 6 }, { '7', 7 },
                { '8', 8 }, { '9', 9 }, { 'A', 10 }, { 'B', 11 }, { 'C', 12 }, { 'D', 13 }, { 'E', 14 }, { 'F', 15 },
                { 'G', 16 }, { 'H', 17 }, { 'I', 18 }, { 'J', 19 }, { 'K', 20 }, { 'L', 21 }, { 'M', 22 }, { 'N', 23 },
                { 'O', 24 }, { 'P', 25 }, { 'Q', 26 }, { 'R', 27 }, { 'S', 28 }, { 'T', 29 }, { 'U', 30 }, { 'V', 31 },
                { 'W', 32 }, { 'X', 33 }, { 'Y', 34 }, { 'Z', 35 }, { '-', 36 }, { '.', 37 }, { ' ', 38 }, { '$', 39 },
                { '/', 40 }, { '+', 41 }, { '%', 42 }, { 'd', 43 }, { 'c', 44 }, { 's', 45 }, { 'p', 46 }
            };
            public static string[] Element = new string[]
            {
                "131112", "111213", "111312", "111411", "121113", "121212", "121311", "111114", "131211", "141111",
                "211113", "211212", "211311", "221112", "221211", "231111", "112113", "112212", "112311", "122112",
                "132111", "111123", "111222", "111321", "121122", "131121", "212112", "212211", "211122", "211221",
                "221121", "222111", "112122", "112221", "122121", "123111", "121131", "311112", "311211", "321111",
                "112131", "113121", "211131", "121221", "312111", "311121", "122211", "111141", "1111411"
            };
            public static int Start = 47;
            public static int Stop = 48;
        }
        // generate CODE93 data:
        private static BarcodeForm Code93(SymbolData symbol)
        {
            BarcodeForm r = new BarcodeForm();
            string s = Regex.IsMatch(symbol.Data, @"[^\x00-\x7f]") ? "" : symbol.Data;
            if (s.Length > 0)
            {
                // generate HRI
                r.Hri = symbol.Hri;
                r.Text = Regex.Replace(s, @"[\x00- \x7f]", " ");
                // calculate check digit
                var d = s.Aggregate("", (a, c) => a + C93.Escape[c]).Select(c => C93.Code[c]).ToList();
                int i = d.Count - 1;
                d.Add(d.Aggregate(0, (a, c) => a + c * (i-- % 20 + 1)) % 47);
                i = d.Count - 1;
                d.Add(d.Aggregate(0, (a, c) => a + c * (i-- % 15 + 1)) % 47);
                // append start character and stop character
                d.Insert(0, C93.Start);
                d.Add(C93.Stop);
                // generate bars and spaces
                string q = symbol.QuietZone ? "a" : "0";
                string m = d.Aggregate(q, (a, c) => a + C93.Element[c]) + q;
                r.Widths = m.Select(c => (c - (c < 'a' ? '0' : 'W')) * symbol.Width).ToArray();
                r.Length = symbol.Width * (d.Count * 9 + (symbol.QuietZone ? 21 : 1));
                r.Height = symbol.Height;
            }
            return r;
        }
        // Codabar(NW-7) patterns:
        private static readonly Dictionary<char, string> Nw7 = new Dictionary<char, string>()
        {
            { '0', "2222255" }, { '1', "2222552" }, { '2', "2225225" }, { '3', "5522222" }, { '4', "2252252" },
            { '5', "5222252" }, { '6', "2522225" }, { '7', "2522522" }, { '8', "2552222" }, { '9', "5225222" },
            { '-', "2225522" }, { '$', "2255222" }, { ':', "5222525" }, { '/', "5252225" }, { '.', "5252522" },
            { '+', "2252525" }, { 'A', "2255252" }, { 'B', "2525225" }, { 'C', "2225255" }, { 'D', "2225552" }
        };
        // generate Codabar(NW-7) data:
        private static BarcodeForm Codabar(SymbolData symbol)
        {
            BarcodeForm r = new BarcodeForm();
            string s = Regex.IsMatch(symbol.Data, @"^[A-Da-d][0-9\-$:/.+]+[A-Da-d]$") ? symbol.Data : "";
            if (s.Length > 0)
            {
                // generate HRI
                r.Hri = symbol.Hri;
                r.Text = s;
                // generate bars and spaces
                string q = symbol.QuietZone ? "a" : "0";
                string m = s.ToUpper().Aggregate(q, (a, c) => a + Nw7[c] + "2");
                m = m.Substring(0, m.Length - 1) + q;
                r.Widths = m.Select(c => (c - (c < 'a' ? '0' : 'W')) * symbol.Width + 1 >> 1).ToArray();
                var w = new[] { 25, 39, 50, 3, 5, 6 };
                r.Length = s.Length * w[symbol.Width - 2] - Regex.Matches(s, @"[\d\-$]").Count * w[symbol.Width + 1] + symbol.Width * (symbol.QuietZone ? 19 : -1);
                r.Height = symbol.Height;
            }
            return r;
        }
        // Interleaved 2 of 5 patterns:
        private static class I25
        {
            public static string[] Element = new string[]
            {
                "22552", "52225", "25225", "55222", "22525", "52522", "25522", "22255", "52252", "25252"
            };
            public static string Start = "2222";
            public static string Stop = "522";
        }
        // generate Interleaved 2 of 5 data:
        private static BarcodeForm Itf(SymbolData symbol)
        {
            BarcodeForm r = new BarcodeForm();
            string s = Regex.IsMatch(symbol.Data, @"^(\d{2})+$") ? symbol.Data : "";
            if (s.Length > 0)
            {
                // generate HRI
                r.Hri = symbol.Hri;
                r.Text = s;
                // generate bars and spaces
                int[] d = s.Select(c => c - '0').ToArray();
                string q = symbol.QuietZone ? "a" : "0";
                string m = q + I25.Start;
                int i = 0;
                while (i < d.Length)
                {
                    string bar = I25.Element[d[i++]];
                    string space = I25.Element[d[i++]];
                    int j = 0;
                    m += bar.Aggregate("", (a, c) => a + c + space[j++]);
                }
                m += I25.Stop + q;
                r.Widths = m.Select(c => (c - (c < 'a' ? '0' : 'W')) * symbol.Width + 1 >> 1).ToArray();
                var w = new[] { 16, 25, 32, 17, 26, 34 };
                r.Length = s.Length * w[symbol.Width - 2] + w[symbol.Width + 1] + symbol.Width * (symbol.QuietZone ? 20 : 0);
                r.Height = symbol.Height;
            }
            return r;
        }
        // CODE39 patterns:
        private static readonly Dictionary<char, string> C39 = new Dictionary<char, string>()
        {
            { '0', "222552522" }, { '1', "522522225" }, { '2', "225522225" }, { '3', "525522222" }, { '4', "222552225" },
            { '5', "522552222" }, { '6', "225552222" }, { '7', "222522525" }, { '8', "522522522" }, { '9', "225522522" },
            { 'A', "522225225" }, { 'B', "225225225" }, { 'C', "525225222" }, { 'D', "222255225" }, { 'E', "522255222" },
            { 'F', "225255222" }, { 'G', "222225525" }, { 'H', "522225522" }, { 'I', "225225522" }, { 'J', "222255522" },
            { 'K', "522222255" }, { 'L', "225222255" }, { 'M', "525222252" }, { 'N', "222252255" }, { 'O', "522252252" },
            { 'P', "225252252" }, { 'Q', "222222555" }, { 'R', "522222552" }, { 'S', "225222552" }, { 'T', "222252552" },
            { 'U', "552222225" }, { 'V', "255222225" }, { 'W', "555222222" }, { 'X', "252252225" }, { 'Y', "552252222" },
            { 'Z', "255252222" }, { '-', "252222525" }, { '.', "552222522" }, { ' ', "255222522" }, { '$', "252525222" },
            { '/', "252522252" }, { '+', "252225252" }, { '%', "222525252" }, { '*', "252252522" }
        };
        // generate CODE39 data:
        private static BarcodeForm Code39(SymbolData symbol)
        {
            BarcodeForm r = new BarcodeForm();
            string s = Regex.IsMatch(symbol.Data, @"^\*?[0-9A-Z\-. $/+%]+\*?$") ? symbol.Data : "";
            if (s.Length > 0)
            {
                // append start character and stop character
                s = Regex.Replace(s, @"^\*?([^*]+)\*?$", "*$1*");
                // generate HRI
                r.Hri = symbol.Hri;
                r.Text = s;
                // generate bars and spaces
                string q = symbol.QuietZone ? "a" : "0";
                string m = s.Aggregate(q, (a, c) => a + C39[c] + "2");
                m = m.Substring(0, m.Length - 1) + q;
                r.Widths = m.Select(c => (c - (c < 'a' ? '0' : 'W')) * symbol.Width + 1 >> 1).ToArray();
                var w = new[] { 29, 45, 58 };
                r.Length = s.Length * w[symbol.Width - 2] + symbol.Width * (symbol.QuietZone ? 19 : -1);
                r.Height = symbol.Height;
            }
            return r;
        }
        // UPC/EAN/JAN patterns:
        private static readonly Dictionary<char, string[]> Ean = new Dictionary<char, string[]>()
        {
            { 'A', new string[] { "3211", "2221", "2122", "1411", "1132", "1231", "1114", "1312", "1213", "3112" } },
            { 'B', new string[] { "1123", "1222", "2212", "1141", "2311", "1321", "4111", "2131", "3121", "2113" } },
            { 'C', new string[] { "3211", "2221", "2122", "1411", "1132", "1231", "1114", "1312", "1213", "3112" } },
            { 'G', new string[] { "111", "11111", "111111", "11", "112" } },
            { 'P', new string[] { "AAAAAA", "AABABB", "AABBAB", "AABBBA", "ABAABB", "ABBAAB", "ABBBAA", "ABABAB", "ABABBA", "ABBABA" } },
            { 'E', new string[] { "BBBAAA", "BBABAA", "BBAABA", "BBAAAB", "BABBAA", "BAABBA", "BAAABB", "BABABA", "BABAAB", "BAABAB" } }
        };
        // generate UPC-A data:
        private static BarcodeForm Upca(SymbolData symbol)
        {
            SymbolData s = symbol.Clone();
            s.Data = "0" + symbol.Data;
            BarcodeForm r = Ean13(s);
            if (r.Text != null)
            {
                r.Text = r.Text.Substring(1);
            }
            return r;
        }
        // generate UPC-E data:
        private static BarcodeForm Upce(SymbolData symbol)
        {
            BarcodeForm r = new BarcodeForm();
            var d = (Regex.IsMatch(symbol.Data, @"^0\d{6,7}$") ? symbol.Data : "").Select(c => c - '0').ToList();
            if (d.Count > 0)
            {
                // calculate check digit
                d = d.Take(7).ToList();
                d.Add((10 - Upcetoa(d).Select((c, i) => c * (3 - (i % 2) * 2)).Sum() % 10) % 10);
                // generate HRI
                r.Hri = symbol.Hri;
                r.Text = string.Join("", d);
                // generate bars and spaces
                string q = symbol.QuietZone ? "7" : "0";
                string m = q + Ean['G'][0];
                for (int i = 1; i < 7; i++) m += Ean[Ean['E'][d[7]][i - 1]][d[i]];
                m += Ean['G'][2] + q;
                r.Widths = m.Select(c => (c - (c < 'a' ? '0' : 'W')) * symbol.Width).ToArray();
                r.Length = symbol.Width * (symbol.QuietZone ? 65 : 51);
                r.Height = symbol.Height;
            }
            return r;
        }
        // convert UPC-E to UPC-A:
        private static List<int> Upcetoa(List<int> e)
        {
            var a = e.Take(3).ToList();
            switch (e[6])
            {
                case 0:
                case 1:
                case 2:
                    a.AddRange(new[] { e[6], 0, 0, 0, 0, e[3], e[4], e[5] });
                    break;
                case 3:
                    a.AddRange(new[] { e[3], 0, 0, 0, 0, 0, e[4], e[5] });
                    break;
                case 4:
                    a.AddRange(new[] { e[3], e[4], 0, 0, 0, 0, 0, e[5] });
                    break;
                default:
                    a.AddRange(new[] { e[3], e[4], e[5], 0, 0, 0, 0, e[6] });
                    break;
            }
            a.Add(e[7]);
            return a;
        }
        // generate EAN-13(JAN-13) data:
        private static BarcodeForm Ean13(SymbolData symbol)
        {
            BarcodeForm r = new BarcodeForm();
            var d = (Regex.IsMatch(symbol.Data, @"^\d{12,13}$") ? symbol.Data : "").Select(c => c - '0').ToList();
            if (d.Count > 0)
            {
                // calculate check digit
                d = d.Take(12).ToList();
                d.Add((10 - d.Select((c, i) => c * ((i % 2) * 2 + 1)).Sum() % 10) % 10);
                // generate HRI
                r.Hri = symbol.Hri;
                r.Text = string.Join("", d);
                // generate bars and spaces
                string m = (symbol.QuietZone ? "b" : "0") + Ean['G'][0];
                for (int i = 1; i < 7; i++) m += Ean[Ean['P'][d[0]][i - 1]][d[i]];
                m += Ean['G'][1];
                for (int i = 7; i < 13; i++) m += Ean['C'][d[i]];
                m += Ean['G'][0] + (symbol.QuietZone ? '7' : '0');
                r.Widths = m.Select(c => (c - (c < 'a' ? '0' : 'W')) * symbol.Width).ToArray();
                r.Length = symbol.Width * (symbol.QuietZone ? 113 : 95);
                r.Height = symbol.Height;
            }
            return r;
        }
        // generate EAN-8(JAN-8) data:
        private static BarcodeForm Ean8(SymbolData symbol)
        {
            BarcodeForm r = new BarcodeForm();
            var d = (Regex.IsMatch(symbol.Data, @"^\d{7,8}$") ? symbol.Data : "").Select(c => c - '0').ToList();
            if (d.Count > 0)
            {
                // calculate check digit
                d = d.Take(7).ToList();
                d.Add((10 - d.Select((c, i) => c * (3 - (i % 2) * 2)).Sum() % 10) % 10);
                // generate HRI
                r.Hri = symbol.Hri;
                r.Text = string.Join("", d);
                // generate bars and spacesd
                string q = symbol.QuietZone ? "7" : "0";
                string m = q + Ean['G'][0];
                for (int i = 0; i < 4; i++) m += Ean['A'][d[i]];
                m += Ean['G'][1];
                for (int i = 4; i < 8; i++) m += Ean['C'][d[i]];
                m += Ean['G'][0] + q;
                r.Widths = m.Select(c => (c - (c < 'a' ? '0' : 'W')) * symbol.Width).ToArray();
                r.Length = symbol.Width * (symbol.QuietZone ? 81 : 67);
                r.Height = symbol.Height;
            }
            return r;
        }

        /**
         * Generate barcode.
         * @param {object} symbol barcode information (data, type, width, height, hri, quietZone)
         * @returns {object} barcode form
         */
        public static BarcodeForm Generate(SymbolData symbol)
        {
            BarcodeForm r = new BarcodeForm() { Hri = false, Text = "", Widths = new int[0], Length = 0, Height = 0 };
            switch (symbol.Type)
            {
                case "upc":
                    r = symbol.Data.Length < 9 ? Upce(symbol) : Upca(symbol);
                    break;
                case "ean":
                case "jan":
                    r = symbol.Data.Length < 9 ? Ean8(symbol) : Ean13(symbol);
                    break;
                case "code39":
                    r = Code39(symbol);
                    break;
                case "itf":
                    r = Itf(symbol);
                    break;
                case "codabar":
                case "nw7":
                    r = Codabar(symbol);
                    break;
                case "code93":
                    r = Code93(symbol);
                    break;
                case "code128":
                    r = Code128(symbol);
                    break;
                default:
                    break;
            }
            return r;
        }
    }
}
