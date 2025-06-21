using UnityEngine;
using FungusToast.Core.Mycovariants;

namespace Assets.Scripts.Unity.UI.MycovariantDraft
{
    public static class MycovariantArtRepository
    {
        /// <summary>
        /// Always returns the same dummy/default sprite for all mycovariants.
        /// </summary>
        public static Sprite GetIcon(MycovariantType type)
        {
            // Always returns the default placeholder sprite.
            // Replace this path with the actual one if you add your own asset.
            return Resources.Load<Sprite>("Icons/Mycovariant_Default");
        }
    }
}
