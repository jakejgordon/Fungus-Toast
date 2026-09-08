using System.Collections.Generic;

namespace FungusToast.Core.Mycovariants
{
    /// <summary>
    /// Mycovariant ids produced by filtering whole categories. The order is repository
    /// declaration order, which carries no notion of strength — so it must not be consumed as a
    /// ranking. <see cref="FungusToast.Core.AI.ParameterizedSpendingStrategy"/> detects this type
    /// and treats the ids as one equally-preferred set, drafting the strongest member on offer.
    ///
    /// A plain <see cref="List{T}"/> means the opposite: the author ranked those ids deliberately
    /// and position is respected.
    /// </summary>
    public sealed class CategoryDerivedMycovariantIds : List<int>
    {
        public CategoryDerivedMycovariantIds(IEnumerable<int> mycovariantIds)
            : base(mycovariantIds)
        {
        }
    }
}
