﻿// Copyright 2017, Google Inc.
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are
// met:
//
//     * Redistributions of source code must retain the above copyright
// notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above
// copyright notice, this list of conditions and the following disclaimer
// in the documentation and/or other materials provided with the
// distribution.
//     * Neither the name of Google Inc. nor the names of its
// contributors may be used to endorse or promote products derived from
// this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
// A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
// OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
// SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
// LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
// THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

using Com.Example.Grpc.Chat;
using Grpc.Core;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using static System.Console;

namespace GreeterServer
{
    public class ChatServiceImpl : ChatService.ChatServiceBase
    {
        private static long _id = 0;
        private static ConcurrentDictionary<long, IServerStreamWriter<ChatMessageFromServer>> s_responseStreams 
            = new ConcurrentDictionary<long, IServerStreamWriter<ChatMessageFromServer>>();

        public override async Task chat(
            IAsyncStreamReader<ChatMessage> requestStream,
            IServerStreamWriter<ChatMessageFromServer> responseStream,
            ServerCallContext context)
        {
            long id = ++_id;
            WriteLine($"New streaming request [{_id}]:");
            s_responseStreams.TryAdd(id, responseStream);
            try
            {
                while (await requestStream.MoveNext(CancellationToken.None))
                {
                    // Get the client message from the request stream
                    var messageFromClient = requestStream.Current;
                    WriteLine($"Received new request [{_id}]: {messageFromClient.From}");

                    // Create a server message that wraps the client message
                    var message = new ChatMessageFromServer
                    {
                        Message = messageFromClient
                    };

                    foreach (var streamKvp in s_responseStreams)
                    {
                        try
                        {
                            // Send to connected clients
                            await streamKvp.Value.WriteAsync(message);
                        }
                        catch (RpcException ex)
                        {
                            // TODO: remove the key from list.
                            WriteLine($"Write error [{streamKvp.Key}]: {ex}");
                        }
                    }
                }
            }
            finally
            {
                WriteLine($"Exit streaming request [{_id}]:");
                IServerStreamWriter<ChatMessageFromServer> dummy;
                s_responseStreams.TryRemove(id, out dummy);
            }
        }
    }
}
