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
        /// <param name="livingCellChange">Change in living cell count (can be negative)</param>
        /// <param name="deadCellChange">Change in dead cell count (can be negative)</param>
        /// <param name="toxinChange">Change in toxin count (can be negative)</param>
        /// <param name="livingCells">Current living cell count</param>
        /// <param name="deadCells">Current dead cell count</param>
        /// <param name="toxinCells">Current toxin cell count</param>
        /// <param name="occupancyPercent">Current board occupancy percentage</param>
        /// <param name="isPlayerSpecific">If true, uses player-specific language; if false, uses global language</param>
        /// <returns>Formatted round summary string</returns>
        public static string FormatRoundSummary(
            int roundNumber,
            int livingCellChange,
            int deadCellChange,
            int toxinChange,
            int livingCells,
            int deadCells,
            int toxinCells,
            float occupancyPercent,
            bool isPlayerSpecific = false)
        {
            var summaryParts = new List<string>();
            
            // Add living cell changes
            if (livingCellChange != 0)
            {
                string livingText = livingCellChange > 0 
                    ? $"Added {livingCellChange} living cell{(livingCellChange == 1 ? "" : "s")}"
                    : $"Lost {Math.Abs(livingCellChange)} living cell{(Math.Abs(livingCellChange) == 1 ? "" : "s")}";
                summaryParts.Add(livingText);
            }
            
            // Add dead cell changes
            if (deadCellChange != 0)
            {
                string deadText = deadCellChange > 0 
                    ? $"added {deadCellChange} dead cell{(deadCellChange == 1 ? "" : "s")}"
                    : $"removed {Math.Abs(deadCellChange)} dead cell{(Math.Abs(deadCellChange) == 1 ? "" : "s")}";
                summaryParts.Add(deadText);
            }
            
            // Add toxin changes
            if (toxinChange != 0)
            {
                string toxinText = toxinChange > 0 
                    ? $"added {toxinChange} toxin{(toxinChange == 1 ? "" : "s")}"
                    : $"removed {Math.Abs(toxinChange)} toxin{(Math.Abs(toxinChange) == 1 ? "" : "s")}";
                summaryParts.Add(toxinText);
            }
            
            // Create the changes summary
            string changes = summaryParts.Any() ? string.Join(", ", summaryParts) : "no net changes";
            
            // Create the final summary
            if (isPlayerSpecific)
            {
                // Player-specific format: simple delta summary
                return $"Round {roundNumber} summary: {changes}";
            }
            else
            {
                // Global format: includes current board state
                return $"Round {roundNumber} summary: {changes}, " +
                       $"board now {occupancyPercent:F1}% occupied " +
                       $"({livingCells} living, {deadCells} dead, {toxinCells} toxin{(toxinCells == 1 ? "" : "s")})";
            }
        }
    }
}