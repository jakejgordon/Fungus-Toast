using UnityEngine;
using FungusToast.Unity.UI.MutationTree;
using FungusToast.Unity.UI.GameLog;

namespace FungusToast.Unity.UI
{
    public class GameUIManager : MonoBehaviour
    {
        [Header("Core UI")]
        [SerializeField] private UI_MutationManager mutationUIManager;
        [SerializeField] private UI_MoldProfilePanel moldProfilePanel;
        [SerializeField] private UI_PlayerBinder playerUIBinder;

        [Header("Sidebars")]
        [SerializeField] private GameObject leftSidebar;
        [SerializeField] private UI_RightSidebar rightSidebar;
        
        [Header("Player Activity Log (Left Sidebar)")]
        [SerializeField] private UI_GameLogPanel playerActivityLogPanel;
        [SerializeField] private GameLogManager playerActivityLogManager;
        
        [Header("Global Events Log (Right Sidebar)")]
        [SerializeField] private UI_GameLogPanel globalEventsLogPanel;
        [SerializeField] private GlobalGameLogManager globalEventsLogManager;

        [Header("End-game")]
        [SerializeField] private UI_EndGamePanel endGamePanel;

        [Header("Phase Transitions")]
        [SerializeField] private UI_PhaseBanner phaseBanner;

        [Header("Phase Tracker")]
        [SerializeField] private UI_PhaseProgressTracker phaseProgressTracker;

        public UI_PhaseProgressTracker PhaseProgressTracker => phaseProgressTracker;

        public UI_MutationManager MutationUIManager => mutationUIManager;
        public UI_MoldProfilePanel MoldProfilePanel => moldProfilePanel;
        public UI_PlayerBinder PlayerUIBinder => playerUIBinder;
        public GameObject LeftSidebar => leftSidebar;
        public UI_RightSidebar RightSidebar => rightSidebar;
        public UI_EndGamePanel EndGamePanel => endGamePanel;
        public UI_PhaseBanner PhaseBanner => phaseBanner;
        
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
                    gameLogRouter = new GameLogRouter(playerActivityLogManager, globalEventsLogManager);
                    
                    // Set the router reference on the player activity log manager for silent mode awareness
                    playerActivityLogManager?.SetGameLogRouter(gameLogRouter);
                }
                return gameLogRouter;
            }
        }
        // Legacy properties for backwards compatibility
        public UI_GameLogPanel GameLogPanel => playerActivityLogPanel;
        public GameLogManager GameLogManager => playerActivityLogManager;
        public UI_GameLogPanel GlobalGameLogPanel => globalEventsLogPanel;
        public GlobalGameLogManager GlobalGameLogManager => globalEventsLogManager;

        // Routing observer for unified event handling
        private GameLogRouter gameLogRouter;
    }
}
