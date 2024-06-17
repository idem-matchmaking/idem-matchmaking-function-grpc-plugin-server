// Copyright (c) 2022 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Grpc.Core;
using Google.Protobuf.WellKnownTypes;

using AccelByte.MatchmakingV2.MatchFunction;
using IdemUtils;
using static IdemUtils.IdemAPI;

namespace AccelByte.PluginArch.Demo.Server.Services
{
    public class MatchFunctionService : MatchFunction.MatchFunctionBase
    {
        private readonly ILogger<MatchFunctionService> _Logger;

        private List<Ticket> _UnmatchedTickets = new List<Ticket>();

        private AuthenticationResult _idemAuthRes;
        private async Task CheckIdemAuth()
        {
            if (_idemAuthRes == null)
                _idemAuthRes = await IdemAPI.Authorize("idem-10238", "$N4Nl2WL72dpy");
        }

        const int ONE_V_ONE_Game = 2;

        private async Task AddIdemNewPlayersInTickets()
        {
            await CheckIdemAuth();
            List<Ticket.Types.PlayerData> players = new List<Ticket.Types.PlayerData>();
            var idemPlayers = new List<IdemAPI.Player>();

            for (int i = 0; i < _UnmatchedTickets.Count; i++)
            {
                players.AddRange(_UnmatchedTickets[i].Players);
            }

            List<string> playerIds = players.Select(p => p.PlayerId).ToList();
            foreach (var id in playerIds)
                idemPlayers.Add(new IdemAPI.Player { playerId = id, servers = new List<string> { "main" } });

            var response = await IdemAPI.AddPlayer(_idemAuthRes.IdToken, new AddPlayerPayload
            {
                gameId = "1v1",
                partyName = "party1",
                players = idemPlayers
            });
        }

        private async Task<IEnumerable<Match>> GetIdemNewMatches()
        {
            await CheckIdemAuth();
            var payload = await IdemAPI.GetMatches(_idemAuthRes.IdToken, new GameIDPayload { gameId = "1v1" });
            List<Match> matches = new List<Match>();    
            foreach(var idemMatches in payload.matches)
            {
                Match match = new Match();
                match.RegionPreferences.Add("any");
                match.Tickets.AddRange(_UnmatchedTickets);
                foreach (var idemTeam in idemMatches.teams)
                    match.Teams.Add(idemTeam.ToMatchTypeTeam());

                matches.Add(match); 
            }    
            return matches;
        }


        private async void IdemMatch(IServerStreamWriter<MatchResponse> responseStream)
        {
            await CheckIdemAuth();
            await AddIdemNewPlayersInTickets();

            var newMatches = await GetIdemNewMatches();
            await PushMatches(responseStream, newMatches);
        }

        private async Task PushMatches(IServerStreamWriter<MatchResponse> responseStream, IEnumerable<Match> matches)
        {
            foreach (var match in matches)
                await responseStream.WriteAsync(new MatchResponse()
                {
                    Match = match
                });
            _UnmatchedTickets.Clear();
        }

        public MatchFunctionService(ILogger<MatchFunctionService> logger)
        {
            _Logger = logger;
        }

        public override Task<StatCodesResponse> GetStatCodes(GetStatCodesRequest request, ServerCallContext context)
        {
            _Logger.LogInformation("Received GetStatCodes request.");
            try
            {
                StatCodesResponse response = new StatCodesResponse();

                return Task.FromResult(response);
            }
            catch (Exception x)
            {
                _Logger.LogError("Cannot deserialize json rules. " + x.Message);
                throw;
            }            
        }

        public override Task<ValidateTicketResponse> ValidateTicket(ValidateTicketRequest request, ServerCallContext context)
        {
            _Logger.LogInformation("Received ValidateTicket request.");
            ValidateTicketResponse response = new ValidateTicketResponse()
            {
                ValidTicket = true
            };

            return Task.FromResult(response);
        }

        public override Task<EnrichTicketResponse> EnrichTicket(EnrichTicketRequest request, ServerCallContext context)
        {
            _Logger.LogInformation("Received EnrichTicket request.");

            Ticket ticket = request.Ticket;
            if (ticket.TicketAttributes.Fields.Count <= 0)
                ticket.TicketAttributes.Fields.Add("enrichedNumber", Value.ForNumber(20));

            EnrichTicketResponse response = new EnrichTicketResponse() { Ticket = ticket };
            return Task.FromResult(response);
        }

        public override async Task MakeMatches(IAsyncStreamReader<MakeMatchesRequest> requestStream, IServerStreamWriter<MatchResponse> responseStream, ServerCallContext context)
        {
            while (await requestStream.MoveNext())
            {
                MakeMatchesRequest request = requestStream.Current;

                if (request.Ticket != null)
                {
                    _Logger.LogInformation("Received ticket");
                    _UnmatchedTickets.Add(request.Ticket);
                    if (_UnmatchedTickets.Count == ONE_V_ONE_Game)
                    {
                        IdemMatch(responseStream);
                    }

                    _Logger.LogInformation("Unmatched tickets size : " + _UnmatchedTickets.Count.ToString());
                }
            }

            //complete
            _Logger.LogInformation("On completed. Unmatched tickets size: " + _UnmatchedTickets.Count.ToString());
            if (_UnmatchedTickets.Count >= ONE_V_ONE_Game)
            {
                 IdemMatch(responseStream);
            }
        }

        public override async Task BackfillMatches(IAsyncStreamReader<BackfillMakeMatchesRequest> requestStream, IServerStreamWriter<BackfillResponse> responseStream, ServerCallContext context)
        {
            while (await requestStream.MoveNext())
            {
                await responseStream.WriteAsync(new BackfillResponse()
                {
                    BackfillProposal = new BackfillProposal()
                });

            }
        }
    }
}

public static class IdemAPIExt
{
    public static Match.Types.Team ToMatchTypeTeam(this IdemAPI.TeamData teamData)
    {
        Match.Types.Team team = new Match.Types.Team();
        team.UserIds.AddRange(teamData.players.Select(p => p.playerId));
        return team;
    } 
}