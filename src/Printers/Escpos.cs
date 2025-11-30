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

namespace ReceiptSharp.Printers
{
    //
    // ESC/POS Common
    //
    class Escpos : Command
    {
        // printer configuration
        protected bool UpsideDown = false;
        protected bool Spacing = false;
        protected bool Cutting = true;
        protected bool Gradient = false;
        protected double Gamma = 1.8;
        protected int Threshold = 128;
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
        // codepages: (ESC t n) (FS &) (FS C n) (ESC R n)
        protected Dictionary<string, string> CodePage = new Dictionary<string, string>()
        {
            { "cp437", "\u001bt\u0000" }, { "cp852", "\u001bt\u0012" }, { "cp858", "\u001bt\u0013" }, { "cp860", "\u001bt\u0003" },
            { "cp863", "\u001bt\u0004" }, { "cp865", "\u001bt\u0005" }, { "cp866", "\u001bt\u0011" }, { "cp1252", "\u001bt\u0010" },
            { "cp932", "\u001bt\u0001\u001cC1\u001bR\u0008" }, { "cp936", "\u001bt\u0000\u001c&" },
            { "cp949", "\u001bt\u0000\u001c&\u001bR\u000d" }, { "cp950", "\u001bt\u0000\u001c&" },
            { "shiftjis", "\u001bt\u0001\u001cC1\u001bR\u0008" }, { "gb18030", "\u001bt\u0000\u001c&" },
            { "ksc5601", "\u001bt\u0000\u001c&\u001bR\u000d" }, { "big5", "\u001bt\u0000\u001c&" }, { "tis620", "\u001bt\u0015" }
        };
        // convert to multiple codepage characters: (ESC t n)
        protected string MultiConv(string text)
        {
            string r = "";
            char p = '\u0100';
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c > '\u007f')
                {
                    if (MultiTable.ContainsKey(c))
                    {
                        string d = MultiTable[c];
                        char q = d[0];
                        if (p == q)
                        {
                            r += d[1];
                        }
                        else
                        {
                            r += "\u001bt" + d;
                            p = q;
                        }
                    }
                    else
                    {
                        r += '?';
                    }
                }
                else
                {
                    r += c;
                }
            }
            return r;
        }
    }
}
