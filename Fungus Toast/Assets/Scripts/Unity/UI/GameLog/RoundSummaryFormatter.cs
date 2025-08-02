using System;
using System.Collections.Generic;
using System.Linq;

namespace FungusToast.Unity.UI.GameLog
{
    /// <summary>
    /// Shared utility for generating consistent round summary messages
    /// </summary>
    public static class RoundSummaryFormatter
    {
        /// <summary>
        /// Creates a formatted round summary message with consistent structure
        /// </summary>
        /// <param name="roundNumber">The round number</param>
        /// <param name="cellsGrown">Number of cells grown (can be negative for net loss)</param>
        /// <param name="cellsDied">Number of cells that died during the round</param>
        /// <param name="toxinChange">Change in toxin count (can be negative)</param>
        /// <param name="deadCellChange">Change in dead cell count (can be negative)</param>
        /// <param name="livingCells">Current living cell count</param>
        /// <param name="deadCells">Current dead cell count</param>
        /// <param name="toxinCells">Current toxin cell count</param>
        /// <param name="occupancyPercent">Current board occupancy percentage</param>
        /// <param name="isPlayerSpecific">If true, uses player-specific language; if false, uses global language</param>
        /// <returns>Formatted round summary string</returns>
        public static string FormatRoundSummary(
            int roundNumber,
            int cellsGrown,
            int cellsDied,
            int toxinChange,
            int deadCellChange,
            int livingCells,
            int deadCells,
            int toxinCells,
            float occupancyPercent,
            bool isPlayerSpecific = false)
        {
            var summaryParts = new List<string>();
            
            // Add cells grown/lost
            if (cellsGrown != 0)
            {
                string growthText = isPlayerSpecific 
                    ? (cellsGrown > 0 ? $"Grew {cellsGrown} new cell{(cellsGrown == 1 ? "" : "s")}" : $"Lost {Math.Abs(cellsGrown)} cell{(Math.Abs(cellsGrown) == 1 ? "" : "s")}")
                    : $"{Math.Abs(cellsGrown)} cell{(Math.Abs(cellsGrown) == 1 ? "" : "s")} {(cellsGrown > 0 ? "grown" : "lost")}";
                summaryParts.Add(growthText);
            }
            
            // Add cells died
            if (cellsDied > 0)
            {
                string deathText = isPlayerSpecific 
                    ? $"{cellsDied} cell{(cellsDied == 1 ? "" : "s")} died"
                    : $"{cellsDied} cell{(cellsDied == 1 ? "" : "s")} died";
                summaryParts.Add(deathText);
            }
            
            // Add toxin changes
            if (toxinChange != 0)
            {
                string toxinText = isPlayerSpecific 
                    ? (toxinChange > 0 ? $"Dropped {toxinChange} toxin{(toxinChange == 1 ? "" : "s")}" : $"Removed {Math.Abs(toxinChange)} toxin{(Math.Abs(toxinChange) == 1 ? "" : "s")}")
                    : $"{Math.Abs(toxinChange)} toxin{(Math.Abs(toxinChange) == 1 ? "" : "s")} {(toxinChange > 0 ? "added" : "removed")}";
                summaryParts.Add(toxinText);
            }
            
            // Create the changes summary
            string changes = summaryParts.Any() ? string.Join(", ", summaryParts) : "no net changes";
            
            // Create the final summary
            if (isPlayerSpecific)
            {
                // Player-specific format: show changes and optionally dead cell count if significant
                string playerSummary = $"Round {roundNumber} summary: {changes}";
                if (deadCells > 0)
                {
                    playerSummary += $", {deadCells} dead cell{(deadCells == 1 ? "" : "s")} total";
                }
                return playerSummary;
            }
            else
            {
                // Global format: includes board state
                return $"Round {roundNumber} summary: {changes}, " +
                       $"board now {occupancyPercent:F1}% occupied " +
                       $"({livingCells} living, {deadCells} dead, {toxinCells} toxin{(toxinCells == 1 ? "" : "s")})";
            }
        }
    }
}