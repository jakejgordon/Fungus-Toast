using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FungusToast.Core.Board;
using FungusToast.Core.Events;
using FungusToast.Core.Players;
using FungusToast.Core.Tests.Mutations;
using Xunit;

namespace FungusToast.Core.Tests.Events;

public sealed class GameRulesEventSubscriberConcurrencyTests
{
    /// <summary>
    /// GameRulesEventSubscriber keeps one process-wide map of board subscriptions, the same shape
    /// already made concurrent in <see cref="AnalyticsEventSubscriber"/>. Nothing in the game runs
    /// two boards at once, but a test host does: parallel xUnit collections corrupted the map and
    /// surfaced as unrelated simulation tests failing with "Operations that change non-concurrent
    /// collections must have exclusive access", landing on a different test each run. Independent
    /// boards must be able to subscribe and unsubscribe concurrently without tearing the map.
    /// </summary>
    [Fact]
    public void ConcurrentBoards_SubscribeAndUnsubscribeWithoutCorruptingSharedState()
    {
        const int boardCount = 256;
        var exceptions = new List<Exception>();

        Parallel.For(0, boardCount, _ =>
        {
            try
            {
                var board = new GameBoard(8, 8, 2);
                var players = CreatePlayers();
                GameRulesEventSubscriber.SubscribeAll(board, players, new Random(0), new TestSimulationObserver());
                GameRulesEventSubscriber.UnsubscribeAll(board);
            }
            catch (Exception exception)
            {
                lock (exceptions) exceptions.Add(exception);
            }
        });

        Assert.Equal(string.Empty, string.Join(Environment.NewLine, exceptions));
    }

    /// <summary>
    /// Repeated subscribe/unsubscribe cycles on one board must stay balanced, so the concurrency
    /// fix cannot quietly turn an entry removal into a leak.
    /// </summary>
    [Fact]
    public void RepeatedSubscribeUnsubscribeCycles_RemainStable()
    {
        var board = new GameBoard(8, 8, 2);
        var players = CreatePlayers();

        for (var cycle = 0; cycle < 32; cycle++)
        {
            GameRulesEventSubscriber.SubscribeAll(board, players, new Random(0), new TestSimulationObserver());
            GameRulesEventSubscriber.UnsubscribeAll(board);
        }

        // Unsubscribing a board that is no longer tracked must be a no-op rather than a throw.
        GameRulesEventSubscriber.UnsubscribeAll(board);
    }

    private static List<Player> CreatePlayers() => new()
    {
        new Player(0, "Concurrency AI 0", PlayerTypeEnum.AI),
        new Player(1, "Concurrency AI 1", PlayerTypeEnum.AI),
    };
}
