using UnityEngine;
using FungusToast.UI;

namespace FungusToast.Game
{
    public class GameUIManager : MonoBehaviour
    {
        [SerializeField] private UI_MutationManager mutationUIManager;
        [SerializeField] private UI_MoldProfilePanel moldProfilePanel;
        [SerializeField] private UI_PlayerBinder playerUIBinder;

        public UI_MutationManager MutationUIManager => mutationUIManager;
        public UI_MoldProfilePanel MoldProfilePanel => moldProfilePanel;
        public UI_PlayerBinder PlayerUIBinder => playerUIBinder;
    }
}
