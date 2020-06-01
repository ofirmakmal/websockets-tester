using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Globalization;
using System.Runtime.Loader;
using WebSocketSharp.Server;
using WebSocketSharp;
using System.Threading;

namespace websocketstester
{
    public class EchoWebSocketsServer : WebSocketBehavior
    {
        int counter = 0;
        protected override void OnOpen()
        {
            Console.WriteLine("Connection opened.");
        }

        protected override void OnError(ErrorEventArgs e)
        {
            Console.WriteLine("Connection error:" + e.Exception.ToString());
        }

        protected override void OnClose(CloseEventArgs e)
        {
            Console.WriteLine("Connection closed.");
        }

        protected override async void OnMessage(MessageEventArgs e)
        {
            string[] arr = e.Data.Split(',');
            long ticks = long.Parse(arr[0]);
            int messageSize = int.Parse(arr[1]);
            string message = $"{ticks},{arr[2]}";
            message = message.PadRight(messageSize, 'R');
            message = message.Substring(0, messageSize);
            Send(message);

            //Send(e.Data);
            Console.WriteLine($"Message recieved in {TimeSpan.FromTicks(DateTime.Now.Ticks - ticks).TotalMilliseconds} ms, Payload size: {e.Data.Length}, Sent back: {message.Length}, count: {counter}");
            Interlocked.Increment(ref counter);
        }
    }
}
