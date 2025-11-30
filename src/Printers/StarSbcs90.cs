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
    // Star SBCS Landscape
    //
    class StarSbcs90 : Star90
    {
        // print horizontal rule: ESC GS t n ...
        public override string Hr(int width)
        {
            Content += $"\u001b\u001dt\u0001{new string('\u00c4', width)}";
            return "";
        }
        // print vertical rules: ESC i n1 n2 ESC GS t n ...
        public override string Vr(int[] widths, int height)
        {
            Content += widths.Aggregate($"\u001bi{(char)(height - 1)}{(char)0}\u001b\u001dt\u0001\u00b3", (a, w) =>
            {
                int p = w * CharWidth;
                return $"{a}\u001b\u001dR{(char)(p & 255)}{(char)(p >> 8 & 255)}\u00b3";
            });
            return "";
        }
        // start rules: ESC GS t n ...
        public override string VrStart(int[] widths)
        {
            string s = widths.Aggregate("\u00da", (a, w) => $"{a}{new string('\u00c4', w)}\u00c2");
            Content += $"\u001b\u001dt\u0001{s.Substring(0, s.Length - 1)}\u00bf";
            return "";
        }
        // stop rules: FS C n FS . ESC t n ...
        public override string VrStop(int[] widths)
        {
            string s = widths.Aggregate("\u00c0", (a, w) => $"{a}{new string('\u00c4', w)}\u00c1");
            Content += $"\u001b\u001dt\u0001{s.Substring(0, s.Length - 1)}\u00d9";
            return "";
        }
        // print vertical and horizontal rules: ESC GS t n ...
        public override string VrHr(int[] widths1, int[] widths2, int dl, int dr)
        {
            string s1 = widths1.Aggregate("\u00c0", (a, w) => $"{a}{new string('\u00c4', w)}\u00c1");
            string r1 = $"{new string(' ', Math.Max(-dl, 0))}{s1.Substring(0, s1.Length - 1)}\u00d9{new string(' ', Math.Max(dr, 0))}";
            string s2 = widths2.Aggregate("\u00da", (a, w) => $"{a}{new string('\u00c4', w)}\u00c2");
            string r2 = $"{new string(' ', Math.Max(dl, 0))}{s2.Substring(0, s2.Length - 1)}\u00bf{new string(' ', Math.Max(-dr, 0))}";
            Content += $"\u001b\u001dt\u0001{string.Concat(r2.Select((c, i) => VrTable[c][r1[i]]))}";
            return "";
        }
        // ruled line composition
        protected Dictionary<char, Dictionary<char, char>> VrTable = new Dictionary<char, Dictionary<char, char>>()
        {
            { ' ',      new Dictionary<char, char> { { ' ', ' '      }, { '\u00c0', '\u00c0' }, { '\u00c1', '\u00c1' }, { '\u00c4', '\u00c4' }, { '\u00d9', '\u00d9' } } },
            { '\u00bf', new Dictionary<char, char> { { ' ', '\u00bf' }, { '\u00c0', '\u00c5' }, { '\u00c1', '\u00c5' }, { '\u00c4', '\u00c2' }, { '\u00d9', '\u00b4' } } },
            { '\u00c2', new Dictionary<char, char> { { ' ', '\u00c2' }, { '\u00c0', '\u00c5' }, { '\u00c1', '\u00c5' }, { '\u00c4', '\u00c2' }, { '\u00d9', '\u00c5' } } },
            { '\u00c4', new Dictionary<char, char> { { ' ', '\u00c4' }, { '\u00c0', '\u00c1' }, { '\u00c1', '\u00c1' }, { '\u00c4', '\u00c4' }, { '\u00d9', '\u00c1' } } },
            { '\u00da', new Dictionary<char, char> { { ' ', '\u00da' }, { '\u00c0', '\u00c3' }, { '\u00c1', '\u00c5' }, { '\u00c4', '\u00c2' }, { '\u00d9', '\u00c5' } } }
        };
    }
}
