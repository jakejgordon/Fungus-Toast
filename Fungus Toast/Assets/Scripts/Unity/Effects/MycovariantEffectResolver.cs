using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Mycovariants;
using FungusToast.Core.Players;
using FungusToast.Unity.Grid;
using FungusToast.Unity.UI;
using FungusToast.Unity.UI.MycovariantDraft;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace FungusToast.Unity.Effects
{
    public class MycovariantEffectResolver : MonoBehaviour
    {
        public static MycovariantEffectResolver Instance { get; private set; }

        [SerializeField] private GameObject draftPanel;
        [SerializeField] private GridVisualizer gridVisualizer; // Add this in inspector if not already present

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(this.gameObject);
            else
                Instance = this;
        }

        /// <summary>
        /// Returns true if the given mycovariant ID is any Jetting Mycelium (all directions).
        /// </summary>
        public static bool IsJettingMycelium(int id) =>
            id == MycovariantIds.JettingMyceliumNorthId ||
            id == MycovariantIds.JettingMyceliumEastId ||
            id == MycovariantIds.JettingMyceliumSouthId ||
            id == MycovariantIds.JettingMyceliumWestId;

        public IEnumerator ResolveEffect(
            Player player,
            Mycovariant mycovariant,
            PlayerMycovariant playerMyco,
            Action onComplete)
        {
            // Note: AutoMarkTriggered mycovariants are automatically marked as triggered 
            // when added to the player in Player.AddMycovariant()
            // Manual marking is only needed for active mycovariants that require user interaction
            if (playerMyco != null && !mycovariant.AutoMarkTriggered)
            {
                playerMyco.MarkTriggered();
            }

            if (IsJettingMycelium(mycovariant.Id))
            {
                yield return StartCoroutine(
                    MycovariantEffectHelpers.HandleJettingMycelium(
                        player,
                        mycovariant,
                        onComplete,
                        draftPanel,
                        gridVisualizer
                    )
                );
            }
            else if (mycovariant.Id == MycovariantIds.PlasmidBountyId ||
                     mycovariant.Id == MycovariantIds.PlasmidBountyIIId ||
                     mycovariant.Id == MycovariantIds.PlasmidBountyIIIId)
            {
                // Plasmid Bounty effects are now handled by the core ApplyEffect
                // Just handle the UI feedback
                HandlePlasmidBountyUI(player);
                onComplete?.Invoke();
            }
            else if (mycovariant.Id == MycovariantIds.MycelialBastionIId ||
                     mycovariant.Id == MycovariantIds.MycelialBastionIIId ||
                     mycovariant.Id == MycovariantIds.MycelialBastionIIIId)
            {
                yield return StartCoroutine(
                    MycovariantEffectHelpers.HandleMycelialBastion(
                        player,
                        mycovariant,
                        onComplete,
                        draftPanel,
                        gridVisualizer
                    )
                );
            }
            else if (mycovariant.Id == MycovariantIds.SurgicalInoculationId)
            {
                yield return StartCoroutine(
                    MycovariantEffectHelpers.HandleSurgicalInoculation(
                        player,
                        mycovariant,
                        onComplete,
                        draftPanel,
                        gridVisualizer
                    )
                );
            }
            else if (mycovariant.Id == MycovariantIds.BallistosporeDischargeIId ||
                     mycovariant.Id == MycovariantIds.BallistosporeDischargeIIId ||
                     mycovariant.Id == MycovariantIds.BallistosporeDischargeIIIId)
            {
                yield return StartCoroutine(
                    MycovariantEffectHelpers.HandleBallistosporeDischarge(
                        player,
                        mycovariant,
                        onComplete,
                        draftPanel,
                        gridVisualizer
                    )
                );
            }
            // Add more cases as needed
            else
            {
                onComplete?.Invoke();
            }
        }

        private void HandlePlasmidBountyUI(Player player)
        {
            // Only handle UI feedback - the core effect is applied by ApplyEffect
            // Only pulse if the panel is active and enabled
            var panel = GameManager.Instance.GameUI.MoldProfilePanel;
            if (panel != null && panel.gameObject.activeInHierarchy && panel.enabled)
            {
                panel.PulseMutationPoints();
            }
        }

        /// <summary>
        /// Returns the correct CardinalDirection for a Jetting Mycelium mycovariant ID.
        /// </summary>
        public static CardinalDirection DirectionFromMycovariantId(int id)
        {
            if (id == MycovariantIds.JettingMyceliumNorthId) return CardinalDirection.North;
            if (id == MycovariantIds.JettingMyceliumEastId) return CardinalDirection.East;
            if (id == MycovariantIds.JettingMyceliumSouthId) return CardinalDirection.South;
            if (id == MycovariantIds.JettingMyceliumWestId) return CardinalDirection.West;
            throw new ArgumentException("Invalid Jetting Mycelium ID");
        }
    }
}
