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

namespace ReceiptSharp.Printers
{
    class PrintOption
    {
        public bool AsImage { get; set; }
        public bool Landscape { get; set; }
        public int Resolution { get; set; }
        public int Cpl { get; set; }
        public string Encoding { get; set; }
        public bool Gradient { get; set; }
        public double Gamma { get; set; }
        public int Threshold { get; set; }
        public bool UpsideDown { get; set; }
        public bool Spacing { get; set; }
        public bool Cutting { get; set; }
        public int Margin { get; set; }
        public int MarginRight { get; set; }
        public IPrinter Command { get; set; }
        public string Type { get; set; }
        public PrintOption Clone()
        {
            return (PrintOption)MemberwiseClone();
        }
    }
}
