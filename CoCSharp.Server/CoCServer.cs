﻿using CoCSharp.Logic;
using CoCSharp.Networking;
using CoCSharp.Server.Handlers;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace CoCSharp.Server
{
    public class CoCServer
    {
        public CoCServer()
        {
            _listener = new Socket(SocketType.Stream, ProtocolType.Tcp);
            _acceptPool = new SocketAsyncEventArgsPool(100);

            Clients = new List<CoCRemoteClient>();
            MessageHandlers = new Dictionary<ushort, MessageHandler>();

            LoginMessageHandlers.RegisterLoginMessageHandlers(this);
            InGameMessageHandlers.RegisterInGameMessageHandlers(this);
        }

        public Dictionary<ushort, MessageHandler> MessageHandlers { get; private set; }
        public List<CoCRemoteClient> Clients { get; private set; }

        private readonly Socket _listener;
        private readonly SocketAsyncEventArgsPool _acceptPool;

        public void Start()
        {
            _listener.Bind(new IPEndPoint(IPAddress.Any, 9339));
            _listener.Listen(100);
            Console.WriteLine("CoC#.Server listening on *:9339...");
            StartAccept();
        }

        public void RegisterMessageHandler(Message message, MessageHandler handler)
        {
            MessageHandlers.Add(message.ID, handler);
        }

        private void StartAccept()
        {
            var acceptArgs = (SocketAsyncEventArgs)null;
            if (_acceptPool.Count > 1)
            {
                try
                {
                    acceptArgs = _acceptPool.Pop();
                    acceptArgs.Completed += AcceptOperationCompleted;
                }
                catch
                {
                    acceptArgs = CreateNewAcceptArgs();
                }
            }
            else
            {
                acceptArgs = CreateNewAcceptArgs();
            }

            if (!_listener.AcceptAsync(acceptArgs))
                ProcessAccept(acceptArgs);
        }

        private SocketAsyncEventArgs CreateNewAcceptArgs()
        {
            var args = new SocketAsyncEventArgs();
            args.Completed += AcceptOperationCompleted;
            return args;
        }

        private void ProcessAccept(SocketAsyncEventArgs args)
        {
            args.Completed -= AcceptOperationCompleted;
            if (args.SocketError != SocketError.Success)
            {
                StartAccept(); // start accept asap
                ProcessBadAccept(args);
            }

            StartAccept(); // start accept asap

            Console.WriteLine("Accepted new connection: {0}", args.AcceptSocket.RemoteEndPoint);
            Clients.Add(new CoCRemoteClient(this, args.AcceptSocket));
            args.AcceptSocket = null;
            _acceptPool.Push(args);
        }

        private void ProcessBadAccept(SocketAsyncEventArgs args)
        {
            args.AcceptSocket.Close();
            _acceptPool.Push(args);
        }

        private void AcceptOperationCompleted(object sender, SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
        }
    }
}