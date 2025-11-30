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
using System.IO.Ports;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ReceiptSharp.Connections
{
    class Serial : IConnection
    {
        private string Destination;
        private SerialPort Port;
        private bool Opened;

        // events
        public event EventHandler Connected;
        public event EventHandler<byte[]> DataReceived;
        public event EventHandler DataSent;
        public event EventHandler<Exception> ErrorOccurred;
        public event EventHandler Disconnected;

        public Serial(string destination)
        {
            Destination = destination;
        }

        private static Dictionary<string, Parity> ParityMap = new Dictionary<string, Parity>
        {
            { "n", Parity.None }, { "e", Parity.Even }, { "o", Parity.Odd }
        };
        private static Dictionary<string, StopBits> StopBitsMap = new Dictionary<string, StopBits>
        {
            { "1", StopBits.One }, { "2", StopBits.Two }
        };
        private static Dictionary<string, Handshake> HandshakeMap = new Dictionary<string, Handshake>
        {
            { "n", Handshake.None }, { "r", Handshake.RequestToSend }, { "x", Handshake.XOnXOff }, { "", Handshake.None }
        };

        // open port
        public void Connect()
        {
            try
            {
                Match match = Regex.Match(Destination, @"^([^:]*)(:((?:24|48|96|192|384|576|1152)00),?([neo]),?([78]),?([12]),?([nrx]?)$)?", RegexOptions.IgnoreCase);
                string portName = match.Groups[1].Value;
                int baudRate = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 115200;                
                Parity parity = ParityMap[match.Groups[4].Success ? match.Groups[4].Value.ToLower() : "n"];
                int dataBits = match.Groups[5].Success ? int.Parse(match.Groups[5].Value) : 8;
                StopBits stopBits = StopBitsMap[match.Groups[6].Success ? match.Groups[6].Value : "1"];
                Handshake handshake = HandshakeMap[match.Groups[7].Success ? match.Groups[7].Value.ToLower() : "n"]; 
                Port = new SerialPort(portName, baudRate, parity, dataBits, stopBits);
                Port.WriteTimeout = 3000;
                Port.Handshake = handshake;
                for (int i = 0; i < 3; i++)
                {
                    try
                    {
                        Port.Open();
                        break;
                    }
                    catch (Exception e)
                    {
                        if (i == 2)
                        {
                            throw e;
                        }
                    }
                }
                Opened = true;
                Port.DiscardInBuffer();
                Port.DataReceived += Port_DataReceived;
                Connected?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception)
            {
                Disconnected?.Invoke(this, EventArgs.Empty);
            }
        }

        private void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                int bytesToRead = Port.BytesToRead;
                if (bytesToRead > 0)
                {
                    byte[] buffer = new byte[bytesToRead];
                    Port.Read(buffer, 0, bytesToRead);
                    DataReceived?.Invoke(this, buffer);
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, ex);
            }
        }

        // write data
        public void Send(byte[] data)
        {
            if (Opened)
            {
                if (Port.Handshake == Handshake.RequestToSend)
                {
                    for (int i = 0; i < data.Length; i += 1024)
                    {
                        int count = Math.Min(1024, data.Length - i);
                        while (Opened)
                        {
                            try
                            {
                                if (Port.CtsHolding)
                                {
                                    Port.Write(data, i, count);
                                    break;
                                }
                                else
                                {
                                    Task.Delay(100).Wait();
                                }
                            }
                            catch (Exception ex)
                            {
                                ErrorOccurred?.Invoke(this, ex);
                                return;
                            }
                        }
                    }
                }
                else
                {
                    try
                    {
                        Port.Write(data, 0, data.Length);
                    }
                    catch (Exception ex)
                    {
                        ErrorOccurred?.Invoke(this, ex);
                        return;
                    }
                }
                DataSent?.Invoke(this, EventArgs.Empty);
            }
        }

        // close port
        public void Disconnect()
        {
            if (Opened)
            {
                try
                {
                    Port.Close();
                    Opened = false;
                    Disconnected?.Invoke(this, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    ErrorOccurred?.Invoke(this, ex);
                }
            }
        }
    }
}
