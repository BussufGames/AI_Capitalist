/*
 * ----------------------------------------------------------------------------
 * Project: AI Capitalist
 * Author:  Bussuf Senior Dev
 * Date:    2023-10-28
 * ----------------------------------------------------------------------------
 * Description:
 * Calculates revenue generated while the game was closed (The Gap).
 * AI managers produce infinitely. Human managers produce up to 5 cycles max.
 * ----------------------------------------------------------------------------
 * Change Log:
 * 2023-10-28 - Bussuf Senior Dev - Initial implementation with Human/AI cap logic.
 * ----------------------------------------------------------------------------
 */

using AI_Capitalist.Core;
using AI_Capitalist.Data;
using BreakInfinity;
using BussufGames.Core;
using System;
using UnityEngine;

namespace AI_Capitalist.Economy
{
	public class OfflineProgressManager : MonoBehaviour, IService
	{
		[Header("Calibration")]
		[Tooltip("Minimum seconds away to trigger the offline progress calculation.")]
		[SerializeField] private float minimumSecondsForOfflineProgress = 60f;

		// Event for the UI to listen to (Decoupling)
		public event Action<BigDouble, TimeSpan> OnOfflineProgressCalculated;

		private DataManager _dataManager;
		private EconomyManager _economyManager;

		private void Awake()
		{
			if (CoreManager.Instance != null)
			{
				CoreManager.Instance.RegisterService<OfflineProgressManager>(this);
			}
		}

		public void Initialize()
		{
			this.Log("Initializing OfflineProgressManager...");

			_dataManager = CoreManager.Instance.GetService<DataManager>();
			_economyManager = CoreManager.Instance.GetService<EconomyManager>();

			if (_dataManager == null || _economyManager == null)
			{
				this.LogError("Missing dependencies for OfflineProgressManager.");
				return;
			}

			CalculateOfflineProgress();
		}

		private void CalculateOfflineProgress()
		{
			string lastSaveStr = _dataManager.GameData.LastSaveTime;

			// If it's a completely new game, there is no offline progress
			if (string.IsNullOrEmpty(lastSaveStr))
			{
				this.Log("First time playing. No offline progress to calculate.");
				return;
			}

			if (DateTime.TryParse(lastSaveStr, out DateTime lastSaveTime))
			{
				TimeSpan timeAway = DateTime.UtcNow - lastSaveTime;

				if (timeAway.TotalSeconds < minimumSecondsForOfflineProgress)
				{
					this.Log($"Away for {timeAway.TotalSeconds:F0}s. Below threshold ({minimumSecondsForOfflineProgress}s). Ignoring offline progress.");
					return;
				}

				this.Log($"Calculating offline progress for {timeAway.TotalSeconds:F0} seconds...");
				ProcessOfflineEarnings(timeAway.TotalSeconds);
			}
			else
			{
				this.LogError("Failed to parse LastSaveTime.");
			}
		}

		private void ProcessOfflineEarnings(double totalSecondsAway)
		{
			BigDouble totalEarned = BigDouble.Zero;

			foreach (var tierData in _dataManager.GameData.TiersData)
			{
				if (tierData.OwnedUnits <= 0) continue;

				var config = _economyManager.GetTierConfig(tierData.TierID);
				if (config == null) continue;

				BigDouble baseRev = BigDouble.Parse(config.Base_Rev);
				BigDouble revenuePerCycle = baseRev * tierData.OwnedUnits;

				if (tierData.CurrentState == ManagerState.AI)
				{
					// AI calculates infinitely
					double actualCycleTime = config.Cycle_Time / tierData.CurrentAISpeedMulti;
					double cyclesCompleted = totalSecondsAway / actualCycleTime;

					BigDouble tierEarnings = revenuePerCycle * Math.Floor(cyclesCompleted);
					totalEarned += tierEarnings;
				}
				else if (tierData.CurrentState == ManagerState.Human)
				{
					// Human calculates up to 5 shifts max
					BigDouble salaryPerCycle = BigDouble.Parse(config.Base_Human_Salary_Per_Cycle);
					BigDouble currentDebt = BigDouble.Parse(tierData.AccumulatedDebt);

					// How many shifts has the human ALREADY done before we went offline?
					int shiftsAlreadyDone = salaryPerCycle > 0 ? (int)Math.Floor((currentDebt / salaryPerCycle).ToDouble()) : 0;
					int remainingShifts = Mathf.Max(0, 5 - shiftsAlreadyDone);

					if (remainingShifts > 0)
					{
						double actualCycleTime = config.Cycle_Time / tierData.CurrentHumanSpeedMulti;
						double possibleCycles = totalSecondsAway / actualCycleTime;

						// Clamp cycles to whatever remains of their 5-shift limit
						double actualCyclesCompleted = Math.Min(possibleCycles, remainingShifts);
						actualCyclesCompleted = Math.Floor(actualCyclesCompleted);

						if (actualCyclesCompleted > 0)
						{
							BigDouble tierEarnings = revenuePerCycle * actualCyclesCompleted;
							totalEarned += tierEarnings;

							// Update the debt so the human is striking when the game loads
							tierData.AccumulatedDebt = (currentDebt + (salaryPerCycle * actualCyclesCompleted)).ToString();
						}
					}
				}
			}

			if (totalEarned > BigDouble.Zero)
			{
				this.LogSuccess($"Offline progress calculated! Earned: {totalEarned.ToCurrencyString()}");

				// Add money to the bank immediately
				_economyManager.AddIncome(totalEarned);

				// Fire event for the UI to show the "Welcome Back" popup later
				OnOfflineProgressCalculated?.Invoke(totalEarned, TimeSpan.FromSeconds(totalSecondsAway));
			}
			else
			{
				this.Log("Offline calculation resulted in 0 earnings (No managers or on strike).");
			}
		}
	}
}

// ----------------------------------------------------------------------------
// EOF
// ----------------------------------------------------------------------------