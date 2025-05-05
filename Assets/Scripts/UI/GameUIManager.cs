using UnityEngine;
using FungusToast.UI;
using FungusToast.UI.MutationTree;

namespace FungusToast.Game
{
    public class GameUIManager : MonoBehaviour
    {
        [SerializeField] private UI_MutationManager mutationUIManager;
        [SerializeField] private UI_MoldProfilePanel moldProfilePanel;
        [SerializeField] private UI_PlayerBinder playerUIBinder;
        [SerializeField] private UI_RightSidebar rightSidebar;

        public UI_MutationManager MutationUIManager => mutationUIManager;
        public UI_MoldProfilePanel MoldProfilePanel => moldProfilePanel;
        public UI_PlayerBinder PlayerUIBinder => playerUIBinder;
        public UI_RightSidebar RightSidebar => rightSidebar;
    }
}
