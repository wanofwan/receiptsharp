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

namespace ReceiptSharp.Printers
{
    //
    // Plain Text
    //
    class PlainText : Base
    {
        protected class TextData
        {
            public string Data;
            public int Index;
            public int Length;
        }
        protected int Left;
        protected int Position;
        protected int Scale;
        protected List<TextData> Buffer;
        // start printing:
        public override string Open(PrintOption printer)
        {
            Left = 0;
            Position = 0;
            Scale = 1;
            Buffer = new List<TextData>();
            return "";
        }
        // set print area:
        public override string Area(int left, int width, int right)
        {
            Left = left;
            return "";
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
        // print horizontal rule:
        public override string Hr(int width)
        {
            return new string(' ', Left) + new string('-', width);
        }
        // print vertical rules:
        public override string Vr(int[] widths, int height)
        {
            Buffer.Add(new TextData() { Data = "|", Index = Position, Length = 1 });
            foreach (int w in widths)
            {
                Position += w + 1;
                Buffer.Add(new TextData() { Data = "|", Index = Position, Length = 1 });
            }
            return "";
        }
        // start rules:
        public override string VrStart(int[] widths)
        {
            return new string(' ', Left) + widths.Aggregate("+", (a, w) => a + new string('-', w) + "+");
        }
        // stop rules:
        public override string VrStop(int[] widths)
        {
            return new string(' ', Left) + widths.Aggregate("+", (a, w) => a + new string('-', w) + "+");
        }
        // print vertical and horizontal rules:
        public override string VrHr(int[] widths1, int[] widths2, int dl, int dr)
        {
            string r1 = new string(' ', Math.Max(-dl, 0)) + widths1.Aggregate("+", (a, w) => a + new string('-', w) + "+") + new string(' ', Math.Max(dr, 0));
            string r2 = new string(' ', Math.Max(dl, 0)) + widths2.Aggregate("+", (a, w) => a + new string('-', w) + "+") + new string(' ', Math.Max(-dr, 0));
            return new string(' ', Left) + string.Concat(r2.Select((c, i) => VrTable[c][r1[i]]));
        }
        // ruled line composition
        private static readonly Dictionary<char, Dictionary<char, char>> VrTable = new Dictionary<char, Dictionary<char, char>>()
        {
            { ' ', new Dictionary<char, char>() { { ' ', ' ' }, { '+', '+' }, { '-', '-' } } },
            { '+', new Dictionary<char, char>() { { ' ', '+' }, { '+', '+' }, { '-', '+' } } },
            { '-', new Dictionary<char, char>() { { ' ', '-' }, { '+', '+' }, { '-', '-' } } }
        };
        // set line spacing and feed new line:
        public override string VrLf(bool vr)
        {
            return Lf();
        }
        // scale up text:
        public override string Wh(int wh)
        {
            int w = wh < 2 ? wh + 1 : wh - 1;
            Scale = w;
            return "";
        }
        // cancel text decoration:
        public override string Normal()
        {
            Scale = 1;
            return "";
        }
        // print text:
        public override string Text(string text, string encoding)
        {
            string d = ArrayFrom(text, encoding).Aggregate("", (a, c) => a + c + new string(' ', MeasureText(c, encoding) * (Scale - 1)));
            int l = MeasureText(text, encoding) * Scale;
            Buffer.Add(new TextData() { Data = d, Index = Position, Length = l });
            Position += l;
            return "";
        }
        // feed new line:
        public override string Lf()
        {
            string r = "";
            if (Buffer.Count > 0)
            {
                int p = 0;
                r += Buffer.OrderBy(c => c.Index).Aggregate(new string(' ', Left), (a, c) => {
                    string s = a + new string(' ', c.Index - p) + c.Data;
                    p = c.Index + c.Length;
                    return s;
                });
            }
            r += "\n";
            Position = 0;
            Buffer.Clear();
            return r;
        }
    }
}
