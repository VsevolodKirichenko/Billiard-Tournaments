using AutoMapper;
using Tournaments.Api.Contracts;
using Tournaments.Core.Domain;

namespace Tournaments.Api.Mapping;

public sealed class ApiMappingProfile : Profile
{
    public ApiMappingProfile()
    {
        CreateMap<Player, PlayerResponse>();
        CreateMap<Match, MatchResponse>();
        CreateMap<Tournament, TournamentResponse>();
        CreateMap<TrainingSession, TrainingResponse>();
    }
}
