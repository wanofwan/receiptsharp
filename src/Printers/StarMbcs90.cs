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
    // Star MBCS Japanese Landscape
    //
    class StarMbcs90 : Star90
    {
        // print horizontal rule: ESC $ n ...
        public override string Hr(int width)
        {
            Content += $"\u001b$0{new string('\u0095', width)}";
            return "";
        }
        // print vertical rules: ESC i n1 n2 ESC $ n ...
        public override string Vr(int[] widths, int height)
        {
            Content += widths.Aggregate($"\u001bi{(char)(height - 1)}{(char)0}\u001b$0\u0096", (a, w) =>
            {
                int p = w * CharWidth;
                return $"{a}\u001b\u001dR{(char)(p & 255)}{(char)(p >> 8 & 255)}\u0096";
            });
            return "";
        }
        // start rules: ESC $ n ...
        public override string VrStart(int[] widths)
        {
            string s = widths.Aggregate("\u009c", (a, w) => $"{a}{new string('\u0095', w)}\u0091");
            Content += $"\u001b$0{s.Substring(0, s.Length - 1)}\u009d";
            return "";
        }
        // stop rules: ESC $ n ...
        public override string VrStop(int[] widths)
        {
            string s = widths.Aggregate("\u009e", (a, w) => $"{a}{new string('\u0095', w)}\u0090");
            Content += $"\u001b$0{s.Substring(0, s.Length - 1)}\u009f";
            return "";
        }
        // print vertical and horizontal rules: ESC $ n ...
        public override string VrHr(int[] widths1, int[] widths2, int dl, int dr)
        {
            string s1 = widths1.Aggregate(dl > 0 ? "\u009e" : "\u009a", (a, w) => $"{a}{new string('\u0095', w)}\u0090");
            string r1 = $"{new string(' ', Math.Max(-dl, 0))}{s1.Substring(0, s1.Length - 1)}{(dr < 0 ? "\u009f" : "\u009b")}{new string(' ', Math.Max(dr, 0))}";
            string s2 = widths2.Aggregate(dl < 0 ? "\u009c" : "\u0098", (a, w) => $"{a}{new string('\u0095', w)}\u0091");
            string r2 = $"{new string(' ', Math.Max(dl, 0))}{s2.Substring(0, s2.Length - 1)}{(dr > 0 ? "\u009d" : "\u0099")}{new string(' ', Math.Max(-dr, 0))}";
            Content += $"\u001b$0{string.Concat(r2.Select((c, i) => VrTable[c][r1[i]]))}";
            return "";
        }
        // ruled line composition
        protected Dictionary<char, Dictionary<char, char>> VrTable = new Dictionary<char, Dictionary<char, char>>()
        {
            { ' ',      new Dictionary<char, char> { { ' ', ' '      }, { '\u0090', '\u0090' }, { '\u0095', '\u0095' }, { '\u009a', '\u009a' }, { '\u009b', '\u009b' }, { '\u009e', '\u009e' }, { '\u009f', '\u009f' } } },
            { '\u0091', new Dictionary<char, char> { { ' ', '\u0091' }, { '\u0090', '\u008f' }, { '\u0095', '\u0091' }, { '\u009a', '\u008f' }, { '\u009b', '\u008f' }, { '\u009e', '\u008f' }, { '\u009f', '\u008f' } } },
            { '\u0095', new Dictionary<char, char> { { ' ', '\u0095' }, { '\u0090', '\u0090' }, { '\u0095', '\u0095' }, { '\u009a', '\u0090' }, { '\u009b', '\u0090' }, { '\u009e', '\u0090' }, { '\u009f', '\u0090' } } },
            { '\u0098', new Dictionary<char, char> { { ' ', '\u0098' }, { '\u0090', '\u008f' }, { '\u0095', '\u0091' }, { '\u009a', '\u0093' }, { '\u009b', '\u008f' }, { '\u009e', '\u0093' }, { '\u009f', '\u008f' } } },
            { '\u0099', new Dictionary<char, char> { { ' ', '\u0099' }, { '\u0090', '\u008f' }, { '\u0095', '\u0091' }, { '\u009a', '\u008f' }, { '\u009b', '\u0092' }, { '\u009e', '\u008f' }, { '\u009f', '\u0092' } } },
            { '\u009c', new Dictionary<char, char> { { ' ', '\u009c' }, { '\u0090', '\u008f' }, { '\u0095', '\u0091' }, { '\u009a', '\u0093' }, { '\u009b', '\u008f' }, { '\u009e', '\u0093' }, { '\u009f', '\u008f' } } },
            { '\u009d', new Dictionary<char, char> { { ' ', '\u009d' }, { '\u0090', '\u008f' }, { '\u0095', '\u0091' }, { '\u009a', '\u008f' }, { '\u009b', '\u0092' }, { '\u009e', '\u008f' }, { '\u009f', '\u0092' } } }
        };
    }
}
