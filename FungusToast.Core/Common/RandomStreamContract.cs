using System;
using System.Security.Cryptography;
using System.Text;

namespace FungusToast.Core.Common
{
    /// <summary>
    /// Defines deterministic, purpose-scoped random streams for a game.
    /// Gameplay uses the historical base-seed stream. AI decisions use streams
    /// derived from stable decision identity so their draw counts cannot shift
    /// subsequent gameplay randomness.
    /// </summary>
    public sealed class RandomStreamContract
    {
        public const string Version = "fungus-toast.random-streams.v1";

        private readonly int baseSeed;

        public RandomStreamContract(int baseSeed)
        {
            this.baseSeed = baseSeed;
            Gameplay = new Random(baseSeed);
        }

        public Random Gameplay { get; }

        public Random CreateAiDecisionRandom(
            int playerId,
            int round,
            string decisionKind,
            int occurrence = 0)
        {
            if (string.IsNullOrWhiteSpace(decisionKind))
            {
                throw new ArgumentException("Decision kind is required.", nameof(decisionKind));
            }

            string identity = string.Join(
                "|",
                Version,
                baseSeed.ToString(System.Globalization.CultureInfo.InvariantCulture),
                playerId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                round.ToString(System.Globalization.CultureInfo.InvariantCulture),
                decisionKind.Trim(),
                occurrence.ToString(System.Globalization.CultureInfo.InvariantCulture));

            using var sha256 = SHA256.Create();
            byte[] digest = sha256.ComputeHash(Encoding.UTF8.GetBytes(identity));
            int derivedSeed = digest[0]
                | (digest[1] << 8)
                | (digest[2] << 16)
                | (digest[3] << 24);
            return new Random(derivedSeed);
        }
    }
}
