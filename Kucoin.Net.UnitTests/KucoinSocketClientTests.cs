﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CryptoExchange.Net;
using CryptoExchange.Net.Objects;
using Kucoin.Net.Objects;
using Kucoin.Net.Objects.Sockets;
using Kucoin.Net.UnitTests.TestImplementations;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Kucoin.Net.UnitTests
{
    [TestFixture]
    public class KucoinSocketClientTests
    {
        [Test]
        public void Subscribe_Should_SucceedIfAckResponse()
        {
            // arrange
            var socket = new TestSocket();
            socket.CanConnect = true;
            var client = TestHelpers.CreateSocketClient(socket);

            // act
            var subTask = client.SubscribeToTickerUpdatesAsync("test", test => { });
            socket.InvokeMessage($"{{\"type\": \"ack\", \"id\":\"{BaseClient.LastId}\"}}");
            var subResult = subTask.Result;

            // assert
            Assert.IsTrue(subResult.Success);
        }

        [Test]
        public void Subscribe_Should_FailIfNotAckResponse()
        {
            // arrange
            var socket = new TestSocket();
            socket.CanConnect = true;
            var client = TestHelpers.CreateSocketClient(socket);

            // act
            var subTask = client.SubscribeToTickerUpdatesAsync("test", test => { });
            socket.InvokeMessage($"{{\"type\": \"error\", \"id\":\"{BaseClient.LastId}\", \"data\": \"TestError\", \"code\": \"1234\"}}");
            var subResult = subTask.Result;

            // assert
            Assert.IsFalse(subResult.Success);
            Assert.IsTrue(subResult.Error.Code == 1234);
            Assert.IsTrue(subResult.Error.Message == "TestError");
        }
        
        [Test]
        public void UpdateTick_Should_TriggerAction()
        {
            // arrange
            var socket = new TestSocket();
            socket.CanConnect = true;
            var client = TestHelpers.CreateSocketClient(socket);
            KucoinStreamTick result = null;

            // act
            var subTask = client.SubscribeToTickerUpdatesAsync("test", test => result = test);
            socket.InvokeMessage($"{{\"type\": \"ack\", \"id\":\"{BaseClient.LastId}\"}}");
            var subResult = subTask.Result;

            var expected = TestHelpers.CreateObjectWithTestParameters<KucoinStreamTick>();
            var update = new KucoinUpdateMessage<KucoinStreamTick>()
            {
                Type = "message",
                Subject = "trade.ticker",
                Topic = "/market/ticker:test",
                Data = expected
            };
            socket.InvokeMessage(JsonConvert.SerializeObject(update));

            // assert
            Assert.IsTrue(subResult.Success);
            Assert.IsTrue(TestHelpers.AreEqual(result, expected, "Symbol"));
        }

        [Test]
        public void UpdateSnapshot_Should_TriggerAction()
        {
            // arrange
            var socket = new TestSocket();
            socket.CanConnect = true;
            var client = TestHelpers.CreateSocketClient(socket);
            KucoinStreamSnapshot result = null;

            // act
            var subTask = client.SubscribeToSnapshotUpdatesAsync("test", test => result = test);
            socket.InvokeMessage($"{{\"type\": \"ack\", \"id\":\"{BaseClient.LastId}\"}}");
            var subResult = subTask.Result;

            var expected = TestHelpers.CreateObjectWithTestParameters<KucoinStreamSnapshot>();
            var update = new KucoinUpdateMessage<KucoinStreamSnapshotWrapper>()
            {
                Type = "message",
                Subject = "trade.ticker",
                Topic = "/market/snapshot:test",
                Data = new KucoinStreamSnapshotWrapper()
                {
                    Data = expected,
                    Sequence = 1
                }
            };
            socket.InvokeMessage(JsonConvert.SerializeObject(update));

            // assert
            Assert.IsTrue(subResult.Success);
            Assert.IsTrue(TestHelpers.AreEqual(result, expected));
        }
    }
}