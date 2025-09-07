using FungusToast.Core.Mutations;

namespace FungusToast.Unity.UI.MutationTree
{
    public class MutationLayoutMetadata
    {
        public int Row;
        public int Column;

        public MutationLayoutMetadata(int column, int row, MutationCategory category)
        {
            Column = column;
            Row = row;
            Category = category;
        }

        public MutationCategory Category; // Add this too
    }
}
