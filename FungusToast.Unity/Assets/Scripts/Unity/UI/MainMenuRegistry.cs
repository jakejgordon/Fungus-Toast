using UnityEngine;

namespace FungusToast.Unity.UI
{
    /// <summary>
    /// Runtime lookup for the three top-level main-menu panel controllers, replacing
    /// the <c>[SerializeField]</c> cross-references <see cref="FungusToast.Unity.GameManager"/>
    /// and <see cref="FungusToast.Unity.Services.GameTransitionService"/> used to hold
    /// directly. Each controller registers itself in <c>Awake</c> and clears itself in
    /// <c>OnDestroy</c>. See
    /// <see href="../../../../FungusToast.Core/docs/UNITY_CODE_FIRST_MIGRATION.md">
    /// UNITY_CODE_FIRST_MIGRATION.md</see>.
    /// </summary>
    public static class MainMenuRegistry
    {
        public static Campaign.UI_ModeSelectPanelController ModeSelectPanel { get; set; }

        public static GameStart.UI_StartGamePanel StartGamePanel { get; set; }

        public static Campaign.UI_CampaignPanelController CampaignPanel { get; set; }

        /// <summary>
        /// Removes a scene-authored child a panel used to depend on, now that the
        /// panel builds that content itself. While the legacy child is still
        /// authored in the scene, destroying it here keeps the freshly built copy
        /// from rendering twice; once the scene child is gone (after
        /// UNITY_CODE_FIRST_MIGRATION.md's Phase D), this is a no-op.
        /// </summary>
        public static void DestroyLegacyChildIfPresent(Transform root, string childName)
        {
            if (root == null)
            {
                return;
            }

            Transform legacyChild = root.Find(childName);
            if (legacyChild == null)
            {
                return;
            }

            legacyChild.gameObject.SetActive(false);
            if (Application.isPlaying)
            {
                Object.Destroy(legacyChild.gameObject);
            }
            else
            {
                Object.DestroyImmediate(legacyChild.gameObject);
            }
        }
    }
}
