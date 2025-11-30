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
using System.Net.Sockets;
using System.Threading.Tasks;

namespace ReceiptSharp.Connections
{
    class Network : IConnection
    {
        private string Destination;

        private TcpClient Client;
        private NetworkStream Stream;
        private bool Opened;

        // events
        public event EventHandler Connected;
        public event EventHandler<byte[]> DataReceived;
        public event EventHandler DataSent;
        public event EventHandler<Exception> ErrorOccurred;
        public event EventHandler Disconnected;

        public Network(string destination)
        {
            Destination = destination;
        }

        // open port
        public void Connect()
        {
            try
            {
                Client = new TcpClient();
                Client.Connect(Destination, 9100);
                Stream = Client.GetStream();
                if (Client.Available > 0)
                {
                    byte[] buffer = new byte[Client.Available];
                    Stream.Read(buffer, 0, buffer.Length);
                }
                Opened = true;
                _ = DataReceiveAsync();
                Connected?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception)
            {
                Disconnected?.Invoke(this, EventArgs.Empty);
            }
        }

        private async Task DataReceiveAsync()
        {
            byte[] buffer = new byte[1024];
            try
            {
                while (Opened)
                {
                    int bytesToRead = await Stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesToRead == 0)
                    {
                        break;
                    }
                    byte[] data = new byte[bytesToRead];
                    Array.Copy(buffer, data, bytesToRead);
                    DataReceived?.Invoke(this, data);
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
                try
                {
                    Stream.Write(data, 0, data.Length);
                }
                catch (Exception ex)
                {
                    ErrorOccurred?.Invoke(this, ex);
                    return;
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
                    Stream?.Close();
                    Client?.Close();
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
