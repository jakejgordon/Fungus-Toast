using System;
using System.Collections.Generic;
using System.Text;

namespace FungusToast.Core.Metrics
{
    public interface IMutationPointObserver
    {
        void RecordMutatorPhenotypeMutationPointsEarned(int playerId, int freePointsEarned);
        void RecordHyperadaptiveDriftMutationPointsEarned(int playerId, int freePointsEarned);
        void RecordAdaptiveExpressionBonus(int playerId, int bonus);
    }
}
