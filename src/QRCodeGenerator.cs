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
using System.Text;

namespace ReceiptSharp
{
    public static class QRCodeGenerator
    {
        // Reed-Solomon codes
        private static class RS
        {
            private static byte[] GfExp = new byte[512];
            private static byte[] GfLog = new byte[256];

            static RS()
            {
                int p = 1;
                for (int i = 0; i < 255; i++)
                {
                    GfExp[i] = (byte)p;
                    GfLog[p] = (byte)i;
                    p <<= 1;
                    if ((p & 0x100) != 0)
                    {
                        p ^= 0x11d;
                    }
                }
                for (int i = 255; i < 512; i++)
                {
                    GfExp[i] = GfExp[i - 255];
                }
            }
            private static byte GfMul(byte a, byte b)
            {
                return a == 0 || b == 0 ? (byte)0 : GfExp[GfLog[a] + GfLog[b]];
            }
            private static byte[] MulPoly(byte[] p1, byte[] p2)
            {
                byte[] r = new byte[p1.Length + p2.Length - 1];
                for (int i = 0; i < p1.Length; i++)
                {
                    for (int j = 0; j < p2.Length; j++)
                    {
                        r[i + j] ^= GfMul(p1[i], p2[j]);
                    }
                }
                return r;
            }
            private static byte[] GenPoly(int eclen)
            {
                byte[] gx = new byte[] { 1 };
                for (int i = 0; i < eclen; i++)
                {
                    gx = MulPoly(gx, new byte[] { 1, GfExp[i] });
                }
                return gx;
            }
            public static byte[] Encode(byte[] data, int eclen)
            {
                byte[] gx = GenPoly(eclen);
                byte[] mx = new byte[data.Length + eclen];
                Array.Copy(data, mx, data.Length);
                for (int i = 0; i < data.Length; i++)
                {
                    byte m = mx[i];
                    if (m != 0)
                    {
                        for (int j = 0; j < gx.Length; j++)
                        {
                            mx[i + j] ^= GfMul(m, gx[j]);
                        }
                    }
                }
                Array.Copy(data, mx, data.Length);
                return mx;
            }
        }
        // encoding
        private static Dictionary<string, int[,]> RsTable = new Dictionary<string, int[,]>
        {
            {
                "l", new int[,]
                {
                    { 0, 0, 0, 0 }, { 1, 0, 26, 19 }, { 1, 0, 44, 34 }, { 1, 0, 70, 55 }, { 1, 0, 100, 80 },
                    { 1, 0, 134, 108 }, { 2, 0, 86, 68 }, { 2, 0, 98, 78 }, { 2, 0, 121, 97 },
                    { 2, 0, 146, 116 }, { 2, 2, 86, 68 }, { 4, 0, 101, 81 }, { 2, 2, 116, 92 },
                    { 4, 0, 133, 107 }, { 3, 1, 145, 115 }, { 5, 1, 109, 87 }, { 5, 1, 122, 98 },
                    { 1, 5, 135, 107 }, { 5, 1, 150, 120 }, { 3, 4, 141, 113 }, { 3, 5, 135, 107 },
                    { 4, 4, 144, 116 }, { 2, 7, 139, 111 }, { 4, 5, 151, 121 }, { 6, 4, 147, 117 },
                    { 8, 4, 132, 106 }, { 10, 2, 142, 114 }, { 8, 4, 152, 122 }, { 3, 10, 147, 117 },
                    { 7, 7, 146, 116 }, { 5, 10, 145, 115 }, { 13, 3, 145, 115 }, { 17, 0, 145, 115 },
                    { 17, 1, 145, 115 }, { 13, 6, 145, 115 }, { 12, 7, 151, 121 }, { 6, 14, 151, 121 },
                    { 17, 4, 152, 122 }, { 4, 18, 152, 122 }, { 20, 4, 147, 117 }, { 19, 6, 148, 118 }
                }
            },
            {
                "m", new int[,]
                {
                    { 0, 0, 0, 0 }, { 1, 0, 26, 16 }, { 1, 0, 44, 28 }, { 1, 0, 70, 44 }, { 2, 0, 50, 32 },
                    { 2, 0, 67, 43 }, { 4, 0, 43, 27 }, { 4, 0, 49, 31 }, { 2, 2, 60, 38 },
                    { 3, 2, 58, 36 }, { 4, 1, 69, 43 }, { 1, 4, 80, 50 }, { 6, 2, 58, 36 },
                    { 8, 1, 59, 37 }, { 4, 5, 64, 40 }, { 5, 5, 65, 41 }, { 7, 3, 73, 45 },
                    { 10, 1, 74, 46 }, { 9, 4, 69, 43 }, { 3, 11, 70, 44 }, { 3, 13, 67, 41 },
                    { 17, 0, 68, 42 }, { 17, 0, 74, 46 }, { 4, 14, 75, 47 }, { 6, 14, 73, 45 },
                    { 8, 13, 75, 47 }, { 19, 4, 74, 46 }, { 22, 3, 73, 45 }, { 3, 23, 73, 45 },
                    { 21, 7, 73, 45 }, { 19, 10, 75, 47 }, { 2, 29, 74, 46 }, { 10, 23, 74, 46 },
                    { 14, 21, 74, 46 }, { 14, 23, 74, 46 }, { 12, 26, 75, 47 }, { 6, 34, 75, 47 },
                    { 29, 14, 74, 46 }, { 13, 32, 74, 46 }, { 40, 7, 75, 47 }, { 18, 31, 75, 47 }
                }
            },
            {
                "q", new int[,]
                {
                    { 0, 0, 0, 0 }, { 1, 0, 26, 13 }, { 1, 0, 44, 22 }, { 2, 0, 35, 17 }, { 2, 0, 50, 24 },
                    { 2, 2, 33, 15 }, { 4, 0, 43, 19 }, { 2, 4, 32, 14 }, { 4, 2, 40, 18 },
                    { 4, 4, 36, 16 }, { 6, 2, 43, 19 }, { 4, 4, 50, 22 }, { 4, 6, 46, 20 },
                    { 8, 4, 44, 20 }, { 11, 5, 36, 16 }, { 5, 7, 54, 24 }, { 15, 2, 43, 19 },
                    { 1, 15, 50, 22 }, { 17, 1, 50, 22 }, { 17, 4, 47, 21 }, { 15, 5, 54, 24 },
                    { 17, 6, 50, 22 }, { 7, 16, 54, 24 }, { 11, 14, 54, 24 }, { 11, 16, 54, 24 },
                    { 7, 22, 54, 24 }, { 28, 6, 50, 22 }, { 8, 26, 53, 23 }, { 4, 31, 54, 24 },
                    { 1, 37, 53, 23 }, { 15, 25, 54, 24 }, { 42, 1, 54, 24 }, { 10, 35, 54, 24 },
                    { 29, 19, 54, 24 }, { 44, 7, 54, 24 }, { 39, 14, 54, 24 }, { 46, 10, 54, 24 },
                    { 49, 10, 54, 24 }, { 48, 14, 54, 24 }, { 43, 22, 54, 24 }, { 34, 34, 54, 24 }
                }
            },
            {
                "h",  new int[,]
                {
                    { 0, 0, 0, 0 }, { 1, 0, 26, 9 }, { 1, 0, 44, 16 }, { 2, 0, 35, 13 }, { 4, 0, 25, 9 },
                    { 2, 2, 33, 11 }, { 4, 0, 43, 15 }, { 4, 1, 39, 13 }, { 4, 2, 40, 14 },
                    { 4, 4, 36, 12 }, { 6, 2, 43, 15 }, { 3, 8, 36, 12 }, { 7, 4, 42, 14 },
                    { 12, 4, 33, 11 }, { 11, 5, 36, 12 }, { 11, 7, 36, 12 }, { 3, 13, 45, 15 },
                    { 2, 17, 42, 14 }, { 2, 19, 42, 14 }, { 9, 16, 39, 13 }, { 15, 10, 43, 15 },
                    { 19, 6, 46, 16 }, { 34, 0, 37, 13 }, { 16, 14, 45, 15 }, { 30, 2, 46, 16 },
                    { 22, 13, 45, 15 }, { 33, 4, 46, 16 }, { 12, 28, 45, 15 }, { 11, 31, 45, 15 },
                    { 19, 26, 45, 15 }, { 23, 25, 45, 15 }, { 23, 28, 45, 15 }, { 19, 35, 45, 15 },
                    { 11, 46, 45, 15 }, { 59, 1, 46, 16 }, { 22, 41, 45, 15 }, { 2, 64, 45, 15 },
                    { 24, 46, 45, 15 }, { 42, 32, 45, 15 }, { 10, 67, 45, 15 }, { 20, 61, 45, 15 }
                }
            }
        };
        static int GetCapacity(string level, int version)
        {
            int[,] t = RsTable[level];
            return t[version, 0] * t[version, 3] + t[version, 1] * (t[version, 3] + 1) - (version < 10 ? 2 : 3);
        }
        static int SelectVersion(byte[] data, string level)
        {
            int t = data.Length;
            int l = 1, r = 40;
            while (l <= r)
            {
                int m = (l + r) / 2;
                int c = GetCapacity(level, m);
                if (c == t)
                {
                    return m;
                }
                else if (c < t)
                {
                    l = m + 1;
                }
                else {
                    r = m - 1;
                }
            }
            return l;
        }
        private static byte[] CreateData(byte[] utf8, string level, int version)
        {
            byte mode = 4;
            int len = utf8.Length;
            List<byte> d = new List<byte>() { mode };
            if (version >= 10)
            {
                d.Add((byte)(len >> 8));
            }
            d.Add((byte)len);
            d.AddRange(utf8);
            byte[] data = new byte[GetCapacity(level, version) + d.Count - len];
            Array.Copy(d.Select((c, i) => (byte)(c << 4 | (i + 1 < d.Count ? d[i + 1] : 0) >> 4)).ToArray(), data, d.Count);
            for (int i = d.Count; i < data.Length; i++)
            {
                data[i] = (byte)((i - d.Count) % 2 != 0 ? 0x11 : 0xec);
            }
            List<byte[]> block = new List<byte[]>();
            int[,] t = RsTable[level];
            int x = t[version, 0], n = t[version, 2], k = t[version, 3];
            int a = 0;
            for (int i = 0; i < 2; i++)
            {
                int m = k + i;
                for (int j = 0; j < t[version, i]; j++)
                {
                    byte[] b = new byte[m];
                    Array.Copy(data, a, b, 0, m);
                    block.Add(RS.Encode(b, n - k));
                    a += m;
                }
            }
            List<byte> msg = new List<byte>();
            for (int i = 0; i < n; i++)
            {
                if (i == k)
                {
                    for (int j = x; j < block.Count; j++)
                    {
                        msg.Add(block[j][k]);
                    }
                }
                for (int j = 0; j < block.Count; j++)
                {
                    msg.Add(block[j][i < k || j < x ? i : i + 1]);
                }
            }
            return msg.ToArray();
        }
        // finder patterns
        private static byte[,] Finder = {
            { 1, 1, 1, 1, 1, 1, 1, 0 },
            { 1, 0, 0, 0, 0, 0, 1, 0 },
            { 1, 0, 1, 1, 1, 0, 1, 0 },
            { 1, 0, 1, 1, 1, 0, 1, 0 },
            { 1, 0, 1, 1, 1, 0, 1, 0 },
            { 1, 0, 0, 0, 0, 0, 1, 0 },
            { 1, 1, 1, 1, 1, 1, 1, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 0 }
        };
        private static void DrawFinder(byte[,] matrix)
        {
            int size = matrix.GetLength(0);
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    matrix[i, j] = Finder[i, j];
                    matrix[i, size - 1 - j] = Finder[i, j];
                    matrix[size - 1 - i, j] = Finder[i, j];
                }
            }
        }
        // alignment patterns
        private static byte[,] Align = {
            { 1, 1, 1, 1, 1 },
            { 1, 0, 0, 0, 1 },
            { 1, 0, 1, 0, 1 },
            { 1, 0, 0, 0, 1 },
            { 1, 1, 1, 1, 1 }
        };
        private static int[][] AlignPos = new int[][]
        {
            Array.Empty<int>(), Array.Empty<int>(), new int[] { 6, 18 }, new int[] { 6, 22 },
            new int[] { 6, 26 }, new int[] { 6, 30 }, new int[] { 6, 34 },
            new int[] { 6, 22, 38 }, new int[] { 6, 24, 42 }, new int[] { 6, 26, 46 },
            new int[] { 6, 28, 50 }, new int[] { 6, 30, 54 }, new int[] { 6, 32, 58 }, new int[] { 6, 34, 62 },
            new int[] { 6, 26, 46, 66 }, new int[] { 6, 26, 48, 70 }, new int[] { 6, 26, 50, 74 },
            new int[] { 6, 30, 54, 78 }, new int[] { 6, 30, 56, 82 }, new int[] { 6, 30, 58, 86 }, new int[] { 6, 34, 62, 90 },
            new int[] { 6, 28, 50, 72, 94 }, new int[] { 6, 26, 50, 74, 98 },
            new int[] { 6, 30, 54, 78, 102 }, new int[] { 6, 28, 54, 80, 106 },
            new int[] { 6, 32, 58, 84, 110 }, new int[] { 6, 30, 58, 86, 114 }, new int[] { 6, 34, 62, 90, 118 },
            new int[] { 6, 26, 50, 74, 98, 122 }, new int[] { 6, 30, 54, 78, 102, 126 },
            new int[] { 6, 26, 52, 78, 104, 130 }, new int[] { 6, 30, 56, 82, 108, 134 },
            new int[] { 6, 34, 60, 86, 112, 138 }, new int[] { 6, 30, 58, 86, 114, 142 }, new int[] { 6, 34, 62, 90, 118, 146 },
            new int[] { 6, 30, 54, 78, 102, 126, 150 }, new int[] { 6, 24, 50, 76, 102, 128, 154 },
            new int[] { 6, 28, 54, 80, 106, 132, 158 }, new int[] { 6, 32, 58, 84, 110, 136, 162 },
            new int[] { 6, 26, 54, 82, 110, 138, 166 }, new int[] { 6, 30, 58, 86, 114, 142, 170 }
        };
        private static void DrawAlign(byte[,] matrix, int version)
        {
            int[] p = AlignPos[version];
            for (int r = 0; r < p.Length; r++)
            {
                for (int c = 0; c < p.Length; c++)
                {
                    int row = p[r];
                    int col = p[c];
                    if (matrix[row, col] == 255)
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            for (int j = 0; j < 5; j++)
                            {
                                matrix[row - 2 + i, col - 2 + j] = Align[i, j];
                            }
                        }
                    }
                }
            }
        }
        // timing patterns
        private static void DrawTiming(byte[,] matrix)
        {
            int size = matrix.GetLength(0);
            for (int i = 8; i < size - 8; i++)
            {
                matrix[i, 6] = matrix[6, i] = (byte)(1 - i % 2);
            }
        }
        // format information
        private static int[] Bch15m = new int[]
        {
            0x5412, 0x5125, 0x5e7c, 0x5b4b, 0x45f9, 0x40ce, 0x4f97, 0x4aa0,
            0x77c4, 0x72f3, 0x7daa, 0x789d, 0x662f, 0x6318, 0x6c41, 0x6976,
            0x1689, 0x13be, 0x1ce7, 0x19d0, 0x0762, 0x0255, 0x0d0c, 0x083b,
            0x355f, 0x3068, 0x3f31, 0x3a06, 0x24b4, 0x2183, 0x2eda, 0x2bed
        };
        private static Dictionary<string, int> EcLevel = new Dictionary<string, int>
        {
            { "l", 1 }, { "m", 0 }, { "q", 3 }, { "h", 2 }
        };
        private static void DrawFormat(byte[,] matrix, string level, int mask)
        {
            int size = matrix.GetLength(0);
            int d = Bch15m[EcLevel[level] << 3 | mask];
            int r = 0;
            int c = size - 1;
            for (int i = 0; i < 15; i++)
            {
                matrix[r, 8] = matrix[8, c] = (byte)(d >> i & 1);
                r += i == 5 ? 2 : i == 7 ? size - 15 : 1;
                c -= i == 7 ? size - 15 : i == 8 ? 2 : 1;
            }
            matrix[size - 8, 8] = 1;
        }
        // version information
        private static int[] bch18 = new int[]
        {
            0, 0, 0, 0, 0, 0, 0, 0x07c94, 0x085bc, 0x09a99, 0x0a4d3,
            0x0bbf6, 0x0c762, 0x0d847, 0x0e60d, 0x0f928,
            0x10b78, 0x1145d, 0x12a17, 0x13532, 0x149a6,
            0x15683, 0x168c9, 0x177ec, 0x18ec4, 0x191e1,
            0x1afab, 0x1b08e, 0x1cc1a, 0x1d33f, 0x1ed75,
            0x1f250, 0x209d5, 0x216f0, 0x228ba, 0x2379f,
            0x24b0b, 0x2542e, 0x26a64, 0x27541, 0x28c69
        };
        private static void DrawVersion(byte[,] matrix, int version)
        {
            if (version >= 7)
            {
                int size = matrix.GetLength(0);
                int i = 0;
                for (int r = 0; r < 6; r++)
                {
                    for (int c = size - 11; c < size - 8; c++)
                    {
                        matrix[r, c] = matrix[c, r] = (byte)(bch18[version] >> i++ & 1);
                    }
                }
            }
        }
        // data and error correction codewords
        private static Func<int, int, int>[] MaskFn = new Func<int, int, int>[]
        {
            (i, j) => (i + j) % 2 == 0 ? 1 : 0,
            (i, j) => i % 2 == 0 ? 1 : 0,
            (i, j) => j % 3 == 0 ? 1 : 0,
            (i, j) => (i + j) % 3 == 0 ? 1 : 0,
            (i, j) => (i / 2 + j / 3) % 2 == 0 ? 1 : 0,
            (i, j) => (i * j) % 2 + (i * j) % 3 == 0 ? 1 : 0,
            (i, j) => ((i * j) % 2 + (i * j) % 3) % 2 == 0 ? 1 : 0,
            (i, j) => ((i * j) % 3 + (i + j) % 2) % 2 == 0 ? 1 : 0
        };
        private static IEnumerable<int> BitGen(byte[] cword)
        {
            foreach (byte w in cword)
            {
                for (int i = 7; i >= 0; i--)
                {
                    yield return w >> i & 1;
                }
            }
        }
        private static void DrawData(byte[,] matrix, byte[] data, int mask)
        {
            int size = matrix.GetLength(0);
            var f = MaskFn[mask];
            var g = BitGen(data).GetEnumerator();
            int a = -1, c = size - 1, r = size - 1;
            while (c > 0)
            {
                for (int i = c; i > c - 2; i--)
                {
                    if (matrix[r, i] == 255)
                    {
                        matrix[r, i] = (byte)((g.MoveNext() ? g.Current : 0) ^ f(r, i));
                    }
                }
                r += a;
                if (r < 0 || r > size - 1)
                {
                    a = -a;
                    c -= c == 8 ? 3 : 2;
                    r += a;
                }
            }
        }
        // matrix
        private static byte[,] CreateMatrix(byte[] data, string level, int version, int mask)
        {
            int size = version * 4 + 17;
            byte[,] matrix = new byte[size, size];
            for (int r = 0; r < size; r++)
            {
                for (int c = 0; c < size; c++)
                {
                    matrix[r, c] = 255;
                }
            }
            DrawFinder(matrix);
            DrawAlign(matrix, version);
            DrawTiming(matrix);
            DrawFormat(matrix, level, mask);
            DrawVersion(matrix, version);
            DrawData(matrix, data, mask);
            return matrix;
        }
        // masking
        private static int EvaluateMask(byte[,] m)
        {
            int size = m.GetLength(0);
            int score = 0;
            byte[] p = new byte[] { 1, 1, 3, 1, 1 };
            for (int r = 0; r < size; r++)
            {
                List<int> h = new List<int>(), v = new List<int>();
                int a = 0, b = 0, x = 0, y = 0;
                for (int c = 0; c < size; c++)
                {
                    if (x != m[r, c])
                    {
                        h.Add(a);
                        x = 1 - x;
                        a = 1;
                    }
                    else {
                        a++;
                    }
                    if (y != m[c, r])
                    {
                        v.Add(b);
                        y = 1 - y;
                        b = 1;
                    }
                    else {
                        b++;
                    }
                }
                h.Add(a);
                if (x != 0)
                {
                    h.Add(0);
                }
                v.Add(b);
                if (y != 0)
                {
                    v.Add(0);
                }
                // rule 1
                score += h.Aggregate(0, (s, c) => s + (c >= 5 ? c - 2 : 0));
                score += v.Aggregate(0, (s, c) => s + (c >= 5 ? c - 2 : 0));
                // rule 3
                for (int i = 1; i < h.Count - 5; i += 2)
                {
                    bool match = true;
                    for (int j = 0; j < p.Length; j++)
                    {
                        if (p[j] != h[i + j])
                        {
                            match = false;
                            break;
                        }
                    }
                    if (match && (i == 1 || h[i - 1] >= 4 || i == h.Count - 6 || h[i + 5] >= 4))
                    {
                        score += 40;
                    }
                }
                for (int i = 1; i < v.Count - 5; i += 2)
                {
                    bool match = true;
                    for (int j = 0; j < p.Length; j++)
                    {
                        if (p[j] != v[i + j])
                        {
                            match = false;
                            break;
                        }
                    }
                    if (match && (i == 1 || v[i - 1] >= 4 || i == v.Count - 6 || v[i + 5] >= 4))
                    {
                        score += 40;
                    }
                }
            }
            // rule 2
            for (int r = 1; r < size; r++)
            {
                for (int c = 1; c < size; c++)
                {
                    byte z = m[r, c];
                    if (z == m[r, c - 1] && z == m[r - 1, c] && z == m[r - 1, c - 1])
                    {
                        score += 3;
                    }
                }
            }
            // rule 4
            int d = m.Cast<byte>().Sum(s => s);
            score += (int)Math.Ceiling(Math.Abs((decimal)d * 100 / (size * size) - 50) / 5) * 10 - 10;
            return score;
        }
        private static int SelectMask(byte[] data, string level, int version)
        {
            int[] score = new int[8];
            for (int i = 0; i < 8; i++)
            {
                score[i] = EvaluateMask(CreateMatrix(data, level, version, i));
            }
            int a = 0;
            for (int i = 1; i < score.Length; i++)
            {
                if (score[i] < score[a])
                {
                    a = i;
                }
            }
            return a;
        }
        public static byte[,] Generate(SymbolData symbol)
        {
            string level = symbol.Level;
            byte[] utf8 = new UTF8Encoding(false).GetBytes(symbol.Data).Take(GetCapacity(level, 40)).ToArray();
            int version = SelectVersion(utf8, level);
            byte[] data = CreateData(utf8, level, version);
            int mask = SelectMask(data, level, version);
            byte[,] matrix = CreateMatrix(data, level, version, mask);
            if (symbol.QuietZone)
            {
                int size = matrix.GetLength(0);
                byte[,] m = new byte[size + 8, size + 8];
                for (int i = 0; i < size; i++)
                {
                    for (int j = 0; i < size; j++)
                    {
                        m[i + 4, j + 4] = matrix[i, j];
                    }
                }
                return m;
            }
            else
            {
                return matrix;
            }
        }
    }
}
