/*
    PSG9080_ARB arbitrary wave read/write utility
    Copyright (C) 2021  qrp73
    https://github.com/qrp73/PSG9080_ARB

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;

namespace PSG9080_ARB
{
    class PSG9080 : IDisposable
    {
        private SerialPort _serial = new SerialPort();

        public PSG9080(string portName)
        {
            _serial.PortName = portName;
            _serial.BaudRate = 115200;
            _serial.Parity = Parity.None;
            _serial.DataBits = 8;
            _serial.StopBits = StopBits.One;
            _serial.ReadTimeout = 3000;
            _serial.Open();
        }

        public void Dispose()
        {
            if (_serial != null)
            {
                _serial.BaseStream.Flush();
                _serial.Dispose();
                _serial = null;
            }
        }

        static readonly string CRLF = Encoding.ASCII.GetString(new byte[] { 0x0d, 0x0a });

        public byte ReadBootStatus()
        {
            var data = new byte[1];
            _serial.Read(data, 0, data.Length);
            return data[0];
        }

        public void WriteBoot(byte[] data)
        {
            _serial.Write(data, 0, data.Length);
            _serial.BaseStream.Flush();
        }

        public void Write(string text)
        {
            var data = Encoding.ASCII.GetBytes(text);
            _serial.Write(data, 0, data.Length);
            _serial.BaseStream.Flush();
        }

        public string Read()
        {
            var length = _serial.BytesToRead;
            var data = new byte[length];
            _serial.Read(data, 0, data.Length);
            return Encoding.ASCII.GetString(data);
        }

        public void WriteLine(string text)
        {
            Write(text + CRLF);
        }

        public string ReadLine()
        {
            var list = new List<byte>();
            while (list.Count < 2 || (list[list.Count - 2] != 0x0d && list[list.Count - 1] != 0x0a))
            {
                var buffer = new byte[1];
                if (_serial.Read(buffer, 0, 1) != 1)
                    throw new Exception("ERROR");
                list.Add(buffer[0]);
            }
            list.RemoveAt(list.Count - 1);
            list.RemoveAt(list.Count - 1);
            return Encoding.ASCII.GetString(list.ToArray());
        }

        public string ReadLineCallback(byte separator, int doneCount, Action<int> callback)
        {
            var list = new List<byte>();
            int separatorCounter = 0;
            callback(0);
            while (list.Count < 2 || (list[list.Count - 2] != 0x0d && list[list.Count - 1] != 0x0a))
            {
                var buffer = new byte[1];
                if (_serial.Read(buffer, 0, 1) != 1)
                    throw new Exception("ERROR");
                list.Add(buffer[0]);
                if (buffer[0] == separator)
                    separatorCounter++;
                if (_serial.BytesToRead == 0)
                    callback(separatorCounter * 100 / doneCount);
            }
            callback(100);
            list.RemoveAt(list.Count - 1);
            list.RemoveAt(list.Count - 1);
            return Encoding.ASCII.GetString(list.ToArray());
        }

        public void WriteLineCallback(string text, Action<int> callback)
        {
            callback(0);
            var rq = text + CRLF;
            Write(rq);
            var length = rq.Length;
            for (; _serial.BytesToWrite > 0; )
            {
                Thread.Sleep(100);
                callback(_serial.BytesToWrite * 100 / length);
            }
            callback(100);
        }


        public string[] ReadLineArray(int count)
        {
            var list = new List<string>();
            while (list.Count < count)
            {
                list.Add(ReadLine());
            }
            return list.ToArray();
        }

        public Dictionary<string, string> GetAll()
        {
            WriteLine(":r00=90.");
            var lines = ReadLineArray(91);
            var items = new Dictionary<string, string>();
            foreach (var line in lines)
            {
                if (!line.StartsWith(":r"))
                    throw new Exception("ERROR");
                var index1 = line.IndexOf('=');
                if (index1 < 1)
                    throw new Exception("ERROR");
                var key = line.Substring(2, index1 - 2);

                var index2 = line.IndexOf('.');
                if (index2 < 0)
                    throw new Exception("ERROR");
                var val = line.Substring(index1 + 1, index2 - (index1 + 1));
                if (items.ContainsKey(key))
                    throw new Exception("ERROR");
                items[key] = val;
            }
            return items;
        }

        public string Execute(string command)
        {
            WriteLine(command);
            return ReadLine();
        }
    }
}

