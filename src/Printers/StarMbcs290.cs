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
    // Star MBCS Chinese Korean Landscape
    //
    class StarMbcs290 : Star90
    {
        // print horizontal rule: - ...
        public override string Hr(int width)
        {
            Content += new string('-', width);
            return "";
        }
        // print vertical rules: ESC i n1 n2 | ...
        public override string Vr(int[] widths, int height)
        {
            Content += widths.Aggregate($"\u001bi{(char)(height - 1)}{(char)0}|", (a, w) =>
            {
                int p = w * CharWidth;
                return $"{a}\u001b\u001dR{(char)(p & 255)}{(char)(p >> 8 & 255)}|";
            });
            return "";
        }
        // start rules: + - ...
        public override string VrStart(int[] widths)
        {
            Content += widths.Aggregate("+", (a, w) => $"{a}{new string('-', w)}+");
            return "";
        }
        // stop rules: + - ...
        public override string VrStop(int[] widths)
        {
            Content += widths.Aggregate("+", (a, w) => $"{a}{new string('-', w)}+");
            return "";
        }
        // print vertical and horizontal rules: + - ...
        public override string VrHr(int[] widths1, int[] widths2, int dl, int dr)
        {
            string r1 = $"{new string(' ', Math.Max(-dl, 0))}{widths1.Aggregate("+", (a, w) => $"{a}{new string('-', w)}+")}{new string(' ', Math.Max(dr, 0))}";
            string r2 = $"{new string(' ', Math.Max(dl, 0))}{widths2.Aggregate("+", (a, w) => $"{a}{new string('-', w)}+")}{new string(' ', Math.Max(-dr, 0))}";
            Content += $"{string.Concat(r2.Select((c, i) => VrTable[c][r1[i]]))}";
            return "";
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
