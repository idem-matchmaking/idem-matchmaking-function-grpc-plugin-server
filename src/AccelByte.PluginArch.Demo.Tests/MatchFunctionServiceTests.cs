// Copyright (c) 2022 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using NUnit.Framework;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using AccelByte.MatchmakingV2.MatchFunction;
using AccelByte.PluginArch.Demo.Server.Services;
using AccelByte.PluginArch.Demo.Server.Model;

using AccelByte.Sdk.Core.Util;

namespace AccelByte.PluginArch.Demo.Tests
{
    [TestFixture]
    public class MatchFunctionServiceTests
    {
        private ILogger<MatchFunctionService> _MMS_Logger;

        public MatchFunctionServiceTests()
        {
            ILoggerFactory loggerFactory = new NullLoggerFactory();
            _MMS_Logger = loggerFactory.CreateLogger<MatchFunctionService>();
        }


        [Test]
        public async Task GetStatCodesTest()
        {
            var service = new MatchFunctionService(_MMS_Logger);
            var response = await service.GetStatCodes(new GetStatCodesRequest(), new UnitTestCallContext());

            Assert.IsNotNull(response);
        }

        [Test]
        public async Task ValidateTicketTest()
        {
            var service = new MatchFunctionService(_MMS_Logger);
            var response = await service.ValidateTicket(new ValidateTicketRequest(), new UnitTestCallContext());

            Assert.IsTrue(response.ValidTicket);
        }

        [Test]
        public async Task MakeMatchesTest()
        {
            string player1Id = Helper.GenerateRandomId(8);
            string player2Id = Helper.GenerateRandomId(8);

            var service = new MatchFunctionService(_MMS_Logger);

            var context = new UnitTestCallContext();
            var requestStream = new TestAsyncStreamReader<MakeMatchesRequest>(context);
            var responseStream = new TestServerStreamWriter<MatchResponse>(context);

            using var call = service.MakeMatches(requestStream, responseStream, context);

            //Send Rule and 1st Player Id
            MakeMatchesRequest request = new MakeMatchesRequest();
            request.Parameters = new MakeMatchesRequest.Types.MakeMatchesParameters();
            request.Parameters.Rules = new Rules()
            {
                Json = JsonSerializer.Serialize(new RuleObject()
                {
                    ShipCountMin = 2,
                    ShipCountMax = 2
                })
            };

            request.Ticket = new Ticket();
            request.Ticket.Players.Add(new Ticket.Types.PlayerData()
            {
                PlayerId = player1Id
            });

            requestStream.AddMessage(request);

            //Send 2nd Player Id
            request = new MakeMatchesRequest();
            request.Ticket = new Ticket();
            request.Ticket.Players.Add(new Ticket.Types.PlayerData()
            {
                PlayerId = player2Id
            });

            requestStream.AddMessage(request);

            //Read response stream
            var response = await responseStream.ReadNextAsync();
            Assert.IsNotNull(response);
            if (response != null)
            {
                Assert.AreEqual(1, response.Match.Teams.Count);
                Assert.AreEqual(2, response.Match.Teams[0].UserIds.Count);

                Assert.AreEqual(player1Id, response.Match.Teams[0].UserIds[0]);
                Assert.AreEqual(player2Id, response.Match.Teams[0].UserIds[1]);
            }

            requestStream.Complete();
            await call;
            responseStream.Complete();

            Assert.Null(await responseStream.ReadNextAsync());
        }
    }
}
