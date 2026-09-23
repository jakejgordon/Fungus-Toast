using System.Linq;
using FungusToast.Core.Mycovariants;
using Xunit;

namespace FungusToast.Core.Tests.Mycovariants
{
    /// <summary>
    /// The draft card badges a Mycovariant as Passive or One-time straight from
    /// <see cref="Mycovariant.Type"/>, so that flag has to keep agreeing with the cadence the
    /// description opens with. These tests fail if new content drifts apart from the badge.
    /// </summary>
    public class MycovariantTypeBadgeTests
    {
        private const string OneTimeCadence = "One-time on draft:";

        [Fact]
        public void Non_passive_mycovariants_open_with_the_one_time_cadence()
        {
            var offenders = MycovariantRepository.All
                .Where(m => m.Type != MycovariantType.Passive)
                .Where(m => !m.Description.StartsWith(OneTimeCadence))
                .Select(m => m.Name)
                .ToList();

            Assert.Empty(offenders);
        }

        [Fact]
        public void Passive_mycovariants_describe_a_lasting_effect()
        {
            // Enduring Toxaphores is the one deliberate hybrid: it resolves an immediate extension
            // on draft and then keeps extending new toxins for the rest of the game. It is badged
            // Passive because the lasting half is what the draft decision turns on.
            var offenders = MycovariantRepository.All
                .Where(m => m.Type == MycovariantType.Passive)
                .Where(m => m.Name != "Enduring Toxaphores")
                .Where(m => m.Description.StartsWith(OneTimeCadence))
                .Select(m => m.Name)
                .ToList();

            Assert.Empty(offenders);
        }

        [Fact]
        public void Enduring_toxaphores_is_still_the_only_hybrid()
        {
            var hybrids = MycovariantRepository.All
                .Where(m => m.Type == MycovariantType.Passive)
                .Where(m => m.Description.StartsWith(OneTimeCadence))
                .Select(m => m.Name)
                .ToList();

            Assert.Equal(new[] { "Enduring Toxaphores" }, hybrids);
        }
    }
}
