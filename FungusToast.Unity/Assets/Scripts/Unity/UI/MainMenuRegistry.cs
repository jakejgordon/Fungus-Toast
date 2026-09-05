using UnityEngine;

namespace FungusToast.Unity.UI
{
    /// <summary>
    /// Runtime lookup for the three top-level main-menu panel controllers, replacing
    /// the <c>[SerializeField]</c> cross-references <see cref="FungusToast.Unity.GameManager"/>
    /// and <see cref="FungusToast.Unity.Services.GameTransitionService"/> used to hold
    /// directly. Each controller registers itself in <c>Awake</c>, but the Solo
    /// Game and Campaign panels start inactive in the scene — Unity does not call
    /// <c>Awake</c> on an inactive GameObject — so the getters also fall back to a
    /// scene scan (including inactive objects) the first time they are read before
    /// that panel has ever been activated. See
    /// <see href="../../../../FungusToast.Core/docs/UNITY_CODE_FIRST_MIGRATION.md">
    /// UNITY_CODE_FIRST_MIGRATION.md</see>.
    /// </summary>
    public static class MainMenuRegistry
    {
        private static Campaign.UI_ModeSelectPanelController modeSelectPanel;
        private static GameStart.UI_StartGamePanel startGamePanel;
        private static Campaign.UI_CampaignPanelController campaignPanel;

        public static Campaign.UI_ModeSelectPanelController ModeSelectPanel
        {
            get => modeSelectPanel ??= Object.FindAnyObjectByType<Campaign.UI_ModeSelectPanelController>(FindObjectsInactive.Include);
            set => modeSelectPanel = value;
        }

        public static GameStart.UI_StartGamePanel StartGamePanel
        {
            get => startGamePanel ??= Object.FindAnyObjectByType<GameStart.UI_StartGamePanel>(FindObjectsInactive.Include);
            set => startGamePanel = value;
        }

        public static Campaign.UI_CampaignPanelController CampaignPanel
        {
            get => campaignPanel ??= Object.FindAnyObjectByType<Campaign.UI_CampaignPanelController>(FindObjectsInactive.Include);
            set => campaignPanel = value;
        }

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
