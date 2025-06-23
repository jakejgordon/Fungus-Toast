using FungusToast.Core.Board;
using FungusToast.Core.Mycovariants;
using FungusToast.Core.Players;
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
            // Jetting Mycelium: prompt/select cell, then direction, then call processor
            if (IsJettingMycelium(mycovariant.Id))
            {
                yield return StartCoroutine(HandleJettingMycelium(player, mycovariant, onComplete));
            }
            // Plasmid Bounty: instant
            else if (mycovariant.Id == MycovariantIds.PlasmidBountyId)
            {
                HandlePlasmidBounty(player);
                onComplete?.Invoke();
            }
            // Add more cases as needed
            else
            {
                onComplete?.Invoke();
            }
        }

        /// <summary>
        /// Handles the async UI and effect for Jetting Mycelium (any direction).
        /// </summary>
        private IEnumerator HandleJettingMycelium(Player player, Mycovariant picked, Action onComplete)
        {
            var direction = DirectionFromMycovariantId(picked.Id); // Use your direction helper!

            if (player.PlayerType == PlayerTypeEnum.AI)
            {
                var livingCells = GameManager.Instance.Board.GetAllCellsOwnedBy(player.PlayerId)
                    .Where(c => c.IsAlive)
                    .ToList();
                FungalCell cell = livingCells.Count > 0
                    ? livingCells[UnityEngine.Random.Range(0, livingCells.Count)]
                    : null;

                if (cell != null)
                {
                    var playerMyco = player.PlayerMycovariants
                        .FirstOrDefault(pm => pm.MycovariantId == picked.Id);

                    MycovariantEffectProcessor.ResolveJettingMycelium(
                        playerMyco,
                        player,
                        GameManager.Instance.Board,
                        cell.TileId,
                        direction
                    );
                }
                yield return new WaitForSeconds(0.6f);
                onComplete?.Invoke();
            }
            else
            {
                // HIDE THE DRAFT PANEL as soon as coroutine starts!
                draftPanel.SetActive(false);

                bool done = false;
                FungalCell selectedCell = null;

                TileSelectionController.Instance.PromptSelectLivingCell(
                    player.PlayerId,
                    (cell) => {
                        selectedCell = cell;
                        done = true;
                    },
                    () => {
                        done = true;
                    }
                );

                while (!done) yield return null;

                if (selectedCell != null)
                {
                    var playerMyco = player.PlayerMycovariants
                        .FirstOrDefault(pm => pm.MycovariantId == picked.Id);

                    MycovariantEffectProcessor.ResolveJettingMycelium(
                        playerMyco,
                        player,
                        GameManager.Instance.Board,
                        selectedCell.TileId,
                        direction
                    );
                }
                onComplete?.Invoke();
            }
        }



        private void HandlePlasmidBounty(Player player)
        {
            player.AddMutationPoints(15);
            GameManager.Instance.GameUI.MoldProfilePanel?.PulseMutationPoints();
            /*
            GameManager.Instance.GameUI.RightSidebar?.AddLogEntry(
                $"{player.PlayerName} gained 15 mutation points from Plasmid Bounty!"
            );
            */
        }

        /// <summary>
        /// AI picks a random living cell (improve this logic if needed).
        /// </summary>
        private FungalCell PickBestLivingCellForJetting(Player player)
        {
            var livingCells = GameManager.Instance.Board.GetAllCellsOwnedBy(player.PlayerId)
                .Where(c => c.IsAlive)
                .ToList();
            if (livingCells.Count == 0) return null;
            return livingCells[UnityEngine.Random.Range(0, livingCells.Count)];
        }

        /// <summary>
        /// Returns the correct CardinalDirection for a Jetting Mycelium mycovariant ID.
        /// </summary>
        private CardinalDirection DirectionFromMycovariantId(int id)
        {
            if (id == MycovariantIds.JettingMyceliumNorthId) return CardinalDirection.North;
            if (id == MycovariantIds.JettingMyceliumEastId) return CardinalDirection.East;
            if (id == MycovariantIds.JettingMyceliumSouthId) return CardinalDirection.South;
            if (id == MycovariantIds.JettingMyceliumWestId) return CardinalDirection.West;
            throw new ArgumentException("Invalid Jetting Mycelium ID");
        }
    }
}
