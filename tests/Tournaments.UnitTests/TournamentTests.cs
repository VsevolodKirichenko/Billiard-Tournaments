using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Tournaments.Api.Mapping;
using Tournaments.Core.Application;
using Tournaments.Core.Domain;
using Tournaments.Core.Events;

namespace Tournaments.UnitTests;

public sealed class TournamentTests
{
    [Fact]
    public void RoundRobinCreatesEveryPairExactlyOnce()
    {
        var players = Enumerable.Range(0, 4).Select(_ => Guid.NewGuid()).ToArray();
        var tournament = Tournament.Create("Cup", "owner", players);
        Assert.Equal(6, tournament.Matches.Count);
        Assert.Equal(6, tournament.Matches.Select(m => (m.HomeId, m.AwayId)).Distinct().Count());
        Assert.Throws<DomainException>(() => Tournament.Create("Cup", "owner", [players[0], players[0]]));
    }

    [Fact]
    public void CorrectingResultReplacesTheWinnerAndRepeatedResultIsANoOp()
    {
        var a = Guid.NewGuid(); var b = Guid.NewGuid();
        var tournament = Tournament.Create("Cup", "owner", [a, b]);
        var match = tournament.Matches.Single();
        tournament.SetResult(match.Id, 8, 2);
        tournament.SetResult(match.Id, 1, 8);
        var standings = Ranking.Calculate(tournament.Matches, new Dictionary<Guid, string> { [a] = "A", [b] = "B" });
        Assert.Equal(b, standings[0].PlayerId);
        Assert.Equal(1, standings[0].Played);
        Assert.Equal(3, tournament.Version);
        Assert.False(tournament.SetResult(match.Id, 1, 8));
        Assert.Equal(3, tournament.Version);
        Assert.Throws<DomainException>(() => tournament.SetResult(match.Id, 2, 2));
    }

    [Fact]
    public async Task ResultCommandPersistsAnOutboxEventOnlyForActualChanges()
    {
        var tournament = Tournament.Create("Cup", "owner", [Guid.NewGuid(), Guid.NewGuid()]);
        var repository = new Mock<ITournamentRepository>();
        repository.Setup(r => r.Get(tournament.Id, It.IsAny<CancellationToken>())).ReturnsAsync(tournament);
        var outbox = new Mock<IOutbox>(); var uow = new Mock<IUnitOfWork>();
        var handler = new SubmitResultHandler(repository.Object, outbox.Object, uow.Object);
        var match = tournament.Matches.Single();
        await handler.Handle(new(tournament.Id, match.Id, 8, 2, 1, new("owner", false)), default);
        await handler.Handle(new(tournament.Id, match.Id, 8, 2, 2, new("owner", false)), default);
        outbox.Verify(o => o.Add(It.Is<MatchResultChanged>(e => e.TournamentVersion == 2)), Times.Once);
        uow.Verify(u => u.Save(It.IsAny<CancellationToken>()), Times.Once);
        await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(new(tournament.Id, match.Id, 3, 8, 1, new("owner", false)), default));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(new(tournament.Id, match.Id, 3, 8, 2, new("other", false)), default));
    }

    [Fact]
    public void TrainingRequiresValidCountersAndUtcTime()
    {
        Assert.Throws<DomainException>(() => TrainingSession.Create(Guid.NewGuid(), "trainer", "Shots", DateTime.UtcNow, 60, 10, 11));
        Assert.Throws<DomainException>(() => TrainingSession.Create(Guid.NewGuid(), "trainer", "Shots", DateTime.Now, 60, 10, 2));
        Assert.Equal(8, TrainingSession.Create(Guid.NewGuid(), "trainer", "Shots", DateTime.UtcNow, 60, 10, 8).SuccessfulAttempts);
    }

    [Fact]
    public void AutoMapperConfigurationIsValid()
    {
        var configuration = new MapperConfiguration(c => c.AddProfile<ApiMappingProfile>(), NullLoggerFactory.Instance);
        configuration.AssertConfigurationIsValid();
    }
}
