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
    // Star MBCS Chinese Korean
    //
    class StarMbcs2 : Star
    {
        // print horizontal rule: - ...
        public override string Hr(int width)
        {
            return new string('-', width);
        }
        // print vertical rules: ESC i n1 n2 | ...
        public override string Vr(int[] widths, int height)
        {
            return widths.Aggregate($"\u001bi{(char)(height - 1)}{(char)0}|", (a, w) => $"{a}{Relative(w)}|");
        }
        // start rules: + - ...
        public override string VrStart(int[] widths)
        {
            return widths.Aggregate("+", (a, w) => $"{a}{new string('-', w)}+");
        }
        // stop rules: + - ...
        public override string VrStop(int[] widths)
        {
            return widths.Aggregate("+", (a, w) => $"{a}{new string('-', w)}+");
        }
        // print vertical and horizontal rules: + - ...
        public override string VrHr(int[] widths1, int[] widths2, int dl, int dr)
        {
            string r1 = $"{new string(' ', Math.Max(-dl, 0))}{widths1.Aggregate("+", (a, w) => $"{a}{new string('-', w)}+")}{new string(' ', Math.Max(dr, 0))}";
            string r2 = $"{new string(' ', Math.Max(dl, 0))}{widths2.Aggregate("+", (a, w) => $"{a}{new string('-', w)}+")}{new string(' ', Math.Max(-dr, 0))}";
            return $"{string.Concat(r2.Select((c, i) => VrTable[c][r1[i]]))}";
        }
        // ruled line composition
        protected Dictionary<char, Dictionary<char, char>> VrTable = new Dictionary<char, Dictionary<char, char>>()
        {
            { ' ', new Dictionary<char, char> { { ' ', ' ' }, { '+', '+' }, { '-', '-' } } },
            { '+', new Dictionary<char, char> { { ' ', '+' }, { '+', '+' }, { '-', '+' } } },
            { '-', new Dictionary<char, char> { { ' ', '-' }, { '+', '+' }, { '-', '-' } } }
        };
    }
}
