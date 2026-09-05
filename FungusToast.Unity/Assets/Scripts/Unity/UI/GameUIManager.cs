using UnityEngine;
using UnityEngine.UI;
using FungusToast.Core.Board;
using FungusToast.Unity.UI.MutationTree;
using FungusToast.Unity.UI.GameLog;

namespace FungusToast.Unity.UI
{
    public class GameUIManager : MonoBehaviour
    {
        // The activity log is deliberately the sidebar's pressure-release area.
        // Gameplay controls above it must retain their complete footprint.
        private const float HumanActivityLogMinHeight = 0f;
        private const float GlobalActivityLogMinHeight = 180f;
        private const float SpendPointsRowHeight = 82f;
        private const string SpendPointsRowName = "UI_SpendPointsRow";

        [Header("Core UI")]
        [SerializeField] private UI_MutationManager mutationUIManager;
        [SerializeField] private UI_PlayerBinder playerUIBinder;

        [Header("Sidebars")]
        [SerializeField] private GameObject leftSidebar;
        private UI_RightSidebar rightSidebar;
        private UI_MoldProfileRoot moldProfileRoot;
        
        // Player Activity Log (Left Sidebar)
        private UI_GameLogPanel playerActivityLogPanel;
        private GameLogManager playerActivityLogManager;

        // Global Events Log (Right Sidebar)
        private UI_GameLogPanel globalEventsLogPanel;
        private GlobalGameLogManager globalEventsLogManager;

        private UI_LoadingScreen loadingScreen;

        private UI_EndGamePanel endGamePanel;

        [Header("Pause Menu")]
        [SerializeField] private UI_PauseMenuPanel pauseMenuPanel;
        [SerializeField] private Sprite pauseMenuButtonIcon;
        [SerializeField] private Sprite nextTrackButtonIcon;
        [SerializeField] private Sprite nextTrackMenuButtonIcon;

        [Header("Phase Transitions")]
        [SerializeField] private UI_PhaseBanner phaseBanner;

        [Header("Phase Tracker")]
        [SerializeField] private UI_PhaseProgressTracker phaseProgressTracker;

            private void Awake()
            {
                ResolveOwnedReferences();
                ApplySidebarLogLayoutBehavior();
            }

            // Idempotent. Also called explicitly, first, from GameManager.BootstrapServices() —
            // Unity doesn't guarantee this Awake() runs before GameManager's, and
            // BootstrapServices() reads GameLogRouter synchronously, so relying on
            // this Awake() alone let GameLogRouter get built (and cached) with null
            // managers when GameManager's Awake ran first.
            public void ResolveOwnedReferences()
            {
                // Must run before RegisterGlobalEventsLog below, which reads rightSidebar
                // to find its child panel.
                RegisterRightSidebar(FindAnyObjectByType<UI_RightSidebar>(FindObjectsInactive.Include));
                RegisterMoldProfileRoot(FindAnyObjectByType<UI_MoldProfileRoot>(FindObjectsInactive.Include));

                RegisterPlayerActivityLog(
                    leftSidebar != null ? leftSidebar.GetComponentInChildren<UI_GameLogPanel>(true) : null,
                    GetComponentInChildren<GameLogManager>(true));
                RegisterGlobalEventsLog(
                    rightSidebar != null ? rightSidebar.GetComponentInChildren<UI_GameLogPanel>(true) : null,
                    GetComponentInChildren<GlobalGameLogManager>(true));
                RegisterLoadingScreen(FindAnyObjectByType<UI_LoadingScreen>(FindObjectsInactive.Include));
                RegisterEndGamePanel(FindAnyObjectByType<UI_EndGamePanel>(FindObjectsInactive.Include));
            }

        public UI_PhaseProgressTracker PhaseProgressTracker => phaseProgressTracker;

            private void ApplySidebarLogLayoutBehavior()
            {
                if (leftSidebar != null)
                {
                    var sidebarLayout = leftSidebar.GetComponent<VerticalLayoutGroup>();
                    if (sidebarLayout != null)
                    {
                        sidebarLayout.childControlHeight = true;
                        sidebarLayout.childForceExpandHeight = false;
                    }

                    ApplyFixedSpendPointsLayout(leftSidebar.transform);
                }

                if (playerActivityLogPanel == null)
                {
                        return;
                }

                    ApplyFlexibleLogLayout(playerActivityLogPanel, HumanActivityLogMinHeight);
                    ApplyFlexibleLogLayout(globalEventsLogPanel, GlobalActivityLogMinHeight);
            }

        private static void ApplyFixedSpendPointsLayout(Transform sidebarTransform)
        {
            var spendPointsRow = sidebarTransform.Find(SpendPointsRowName);
            if (spendPointsRow == null)
            {
                return;
            }

            var layoutElement = spendPointsRow.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = spendPointsRow.gameObject.AddComponent<LayoutElement>();
            }

            layoutElement.minHeight = SpendPointsRowHeight;
            layoutElement.preferredHeight = SpendPointsRowHeight;
            layoutElement.flexibleHeight = 0f;
        }

        private static void ApplyFlexibleLogLayout(UI_GameLogPanel logPanel, float minimumHeight)
        {
            if (logPanel == null)
            {
                return;
            }

            var layoutElement = logPanel.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = logPanel.gameObject.AddComponent<LayoutElement>();
            }

            layoutElement.minHeight = minimumHeight;
            layoutElement.preferredHeight = -1f;
            layoutElement.flexibleHeight = 1f;
        }

        // ── Core accessors ──
        public UI_MutationManager MutationUIManager => mutationUIManager;
        public UI_PlayerBinder PlayerUIBinder => playerUIBinder;
        public GameObject LeftSidebar => leftSidebar;
        public UI_RightSidebar RightSidebar => rightSidebar;
        public UI_LoadingScreen LoadingScreen => loadingScreen;
        public UI_EndGamePanel EndGamePanel => endGamePanel;
        public UI_PauseMenuPanel PauseMenuPanel => pauseMenuPanel;
        public Sprite PauseMenuButtonIcon => pauseMenuButtonIcon;
        public Sprite NextTrackButtonIcon => nextTrackButtonIcon;
        public Sprite NextTrackMenuButtonIcon => nextTrackMenuButtonIcon != null ? nextTrackMenuButtonIcon : nextTrackButtonIcon;
        public UI_PhaseBanner PhaseBanner => phaseBanner;
        public UI_MoldProfileRoot MoldProfileRoot => moldProfileRoot;

        // ── Board accessor ──
        // Set by GameManager after board creation so UI components can access it
        // without reaching into GameManager.Instance.
        public GameBoard Board { get; private set; }

        /// <summary>
        /// Called by GameManager after board creation. Allows UI components to
        /// reference the board via GameUIManager instead of GameManager.Instance.
        /// </summary>
        public void SetBoard(GameBoard board) => Board = board;
        public void ClearBoard() => Board = null;
        
        // Player Activity Log (Left Sidebar)
        public UI_GameLogPanel PlayerActivityLogPanel => playerActivityLogPanel;
        public GameLogManager PlayerActivityLogManager => playerActivityLogManager;
        
        // Global Events Log (Right Sidebar)  
        public UI_GameLogPanel GlobalEventsLogPanel => globalEventsLogPanel;
        public GlobalGameLogManager GlobalEventsLogManager => globalEventsLogManager;

        // Unified logging interface
        public GameLogRouter GameLogRouter 
        {
            get
            {
                if (gameLogRouter == null)
                {
                    gameLogRouter = new GameLogRouter(playerActivityLogManager, globalEventsLogManager, MutationTreeToastPresenter);

                    // Set the router reference on the player activity log manager for silent mode awareness
                    playerActivityLogManager?.SetGameLogRouter(gameLogRouter);
                    playerActivityLogManager?.SetMutationPointBonusPopupPresenter(MutationPointBonusPopupPresenter);
                }
                return gameLogRouter;
            }
        }
        // Legacy properties for backwards compatibility
        public UI_GameLogPanel GameLogPanel => playerActivityLogPanel;
        public GameLogManager GameLogManager => playerActivityLogManager;
        public UI_GameLogPanel GlobalGameLogPanel => globalEventsLogPanel;
        public GlobalGameLogManager GlobalGameLogManager => globalEventsLogManager;
        public UI_MutationTreeToastPresenter MutationTreeToastPresenter
        {
            get
            {
                if (mutationTreeToastPresenter == null)
                {
                    mutationTreeToastPresenter = GetComponent<UI_MutationTreeToastPresenter>();
                    if (mutationTreeToastPresenter == null)
                    {
                        mutationTreeToastPresenter = gameObject.AddComponent<UI_MutationTreeToastPresenter>();
                    }

                    mutationTreeToastPresenter.Initialize(mutationUIManager);
                }

                return mutationTreeToastPresenter;
            }
        }

        public UI_MutationPointBonusPopupPresenter MutationPointBonusPopupPresenter
        {
            get
            {
                if (mutationPointBonusPopupPresenter == null)
                {
                    mutationPointBonusPopupPresenter = GetComponent<UI_MutationPointBonusPopupPresenter>();
                    if (mutationPointBonusPopupPresenter == null)
                    {
                        mutationPointBonusPopupPresenter = gameObject.AddComponent<UI_MutationPointBonusPopupPresenter>();
                    }

                    mutationPointBonusPopupPresenter.Initialize(
                        mutationUIManager,
                        mutationUIManager != null ? mutationUIManager.MutationPointBonusPopClip : null,
                        mutationUIManager != null ? mutationUIManager.MutationPointBonusPopVolume : 1f);
                }

                return mutationPointBonusPopupPresenter;
            }
        }

        public void RegisterPauseMenuPanel(UI_PauseMenuPanel panel) => pauseMenuPanel = panel;

        public void RegisterLoadingScreen(UI_LoadingScreen panel)
        {
            if (panel == null)
            {
                Debug.LogError("[GameUIManager] No UI_LoadingScreen found in the scene.");
            }

            loadingScreen = panel;
        }

        public void RegisterPlayerActivityLog(UI_GameLogPanel panel, GameLogManager manager)
        {
            if (panel == null)
            {
                Debug.LogError("[GameUIManager] No UI_GameLogPanel found under leftSidebar for the player activity log.");
            }

            if (manager == null)
            {
                Debug.LogError("[GameUIManager] No GameLogManager found for the player activity log.");
            }

            playerActivityLogPanel = panel;
            playerActivityLogManager = manager;
        }

        public void RegisterGlobalEventsLog(UI_GameLogPanel panel, GlobalGameLogManager manager)
        {
            if (panel == null)
            {
                Debug.LogError("[GameUIManager] No UI_GameLogPanel found under rightSidebar for the global events log.");
            }

            if (manager == null)
            {
                Debug.LogError("[GameUIManager] No GlobalGameLogManager found for the global events log.");
            }

            globalEventsLogPanel = panel;
            globalEventsLogManager = manager;
        }

        public void RegisterEndGamePanel(UI_EndGamePanel panel)
        {
            if (panel == null)
            {
                Debug.LogError("[GameUIManager] No UI_EndGamePanel found in the scene.");
            }

            endGamePanel = panel;
        }

        public void RegisterRightSidebar(UI_RightSidebar sidebar)
        {
            if (sidebar == null)
            {
                Debug.LogError("[GameUIManager] No UI_RightSidebar found in the scene.");
            }

            rightSidebar = sidebar;
        }

        public void RegisterMoldProfileRoot(UI_MoldProfileRoot root)
        {
            if (root == null)
            {
                Debug.LogError("[GameUIManager] No UI_MoldProfileRoot found in the scene.");
            }

            moldProfileRoot = root;
        }

        // Routing observer for unified event handling
        private GameLogRouter gameLogRouter;
        private UI_MutationTreeToastPresenter mutationTreeToastPresenter;
        private UI_MutationPointBonusPopupPresenter mutationPointBonusPopupPresenter;
    }
}
