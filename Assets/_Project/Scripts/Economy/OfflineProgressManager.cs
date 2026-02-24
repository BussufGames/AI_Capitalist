/*
 * ----------------------------------------------------------------------------
 * Project: AI Capitalist
 * Author:  Bussuf Senior Dev
 * Date:    2023-10-29
 * ----------------------------------------------------------------------------
 * Change Log:
 * 2023-10-29 - Bussuf Senior Dev - Added pending properties for Offline UI Popup.
 * 2026-02-24 - Bussuf Senior Dev - Added OnApplicationPause to support backgrounding on mobile.
 * 2026-02-24 - Bussuf Senior Dev - Added OnOfflineProgressCalculated event for dynamic UI spawning.
 * ----------------------------------------------------------------------------
 */

using System;
using UnityEngine;
using BreakInfinity;
using AI_Capitalist.Core;
using AI_Capitalist.Data;
using BussufGames.Core;

namespace AI_Capitalist.Economy
{
	public class OfflineProgressManager : MonoBehaviour, IService
	{
		[Tooltip("Minimum seconds away before we show the Welcome Back UI popup.")]
		[SerializeField] private float minimumSecondsForPopup = 60f;

		private static readonly int[] MILESTONES = { 10, 25, 50, 100, 200, 300, 400, 500, 1000 };

		public BigDouble PendingOfflineEarnings { get; private set; } = BigDouble.Zero;
		public TimeSpan PendingTimeAway { get; private set; } = TimeSpan.Zero;
		public bool HasOfflineProgress => PendingOfflineEarnings > BigDouble.Zero;

		// NEW: Event to notify the UIManager to spawn the popup if the app resumed from background
		public event Action<TimeSpan, BigDouble> OnOfflineProgressCalculated;

		private EconomyManager _economyManager;
		private DataManager _dataManager;

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

			_economyManager = CoreManager.Instance.GetService<EconomyManager>();
			_dataManager = CoreManager.Instance.GetService<DataManager>();

			if (_economyManager == null || _dataManager == null) return;

			CalculateOfflineProgress();
		}

		// FIX: Catch app resume from background (mobile behavior)
		private void OnApplicationPause(bool pauseStatus)
		{
			// If pauseStatus is false, the app just came BACK to the foreground!
			if (!pauseStatus && _economyManager != null && _dataManager != null)
			{
				CalculateOfflineProgress();
			}
		}

		private void CalculateOfflineProgress()
		{
			var gameData = _dataManager.GameData;
			if (string.IsNullOrEmpty(gameData.LastSaveTime)) return;

			DateTime lastSaveTime;
			if (!DateTime.TryParse(gameData.LastSaveTime, null, System.Globalization.DateTimeStyles.RoundtripKind, out lastSaveTime))
			{
				return;
			}

			lastSaveTime = lastSaveTime.ToUniversalTime();
			TimeSpan timeAway = DateTime.UtcNow - lastSaveTime;

			BigDouble totalOfflineEarnings = BigDouble.Zero;
			double globalPrestigeMulti = _economyManager.GetGlobalPrestigeMultiplier();

			foreach (var tier in gameData.TiersData)
			{
				if (tier.OwnedUnits <= 0) continue;

				var staticConfig = _economyManager.GetTierConfig(tier.TierID);
				if (staticConfig == null) continue;

				BigDouble cycleRev = BigDouble.Parse(staticConfig.Base_Rev) * tier.OwnedUnits * GetMilestoneMultiplier(tier.OwnedUnits) * globalPrestigeMulti;

				if (tier.CurrentState == ManagerState.None)
				{
					if (tier.IsWorkingManually)
					{
						float actualCycleTime = staticConfig.Cycle_Time;
						tier.CurrentCycleProgress += (float)timeAway.TotalSeconds;

						if (tier.CurrentCycleProgress >= actualCycleTime)
						{
							totalOfflineEarnings += cycleRev;
							tier.CurrentCycleProgress = 0f;
							tier.IsWorkingManually = false;
						}
					}
				}
				else if (tier.CurrentState == ManagerState.AI)
				{
					float actualCycleTime = staticConfig.Cycle_Time / tier.CurrentAISpeedMulti;
					double cyclesCompleted = timeAway.TotalSeconds / actualCycleTime;
					totalOfflineEarnings += cycleRev * cyclesCompleted;
				}
				else if (tier.CurrentState == ManagerState.Human)
				{
					float actualCycleTime = staticConfig.Cycle_Time / tier.CurrentHumanSpeedMulti;
					double potentialCycles = timeAway.TotalSeconds / actualCycleTime;

					BigDouble salary = BigDouble.Parse(staticConfig.Base_Human_Salary_Per_Cycle);
					BigDouble currentDebt = BigDouble.Parse(tier.AccumulatedDebt);
					int missedPayments = salary > BigDouble.Zero ? (int)Math.Floor((currentDebt / salary).ToDouble()) : 0;

					int maxCyclesAllowed = Mathf.Max(0, 5 - missedPayments);

					if (maxCyclesAllowed > 0)
					{
						double actualCyclesWorked = Math.Min(potentialCycles, maxCyclesAllowed);
						totalOfflineEarnings += cycleRev * actualCyclesWorked;
						tier.AccumulatedDebt = (currentDebt + (salary * actualCyclesWorked)).ToString();
					}
				}
			}

			if (totalOfflineEarnings > BigDouble.Zero)
			{
				_economyManager.AddIncome(totalOfflineEarnings);

				if (timeAway.TotalSeconds >= minimumSecondsForPopup)
				{
					PendingOfflineEarnings = totalOfflineEarnings;
					PendingTimeAway = timeAway;
					this.LogSuccess($"<color=yellow>OFFLINE PROGRESS UI SPAWNED: Earned {totalOfflineEarnings.ToCurrencyString()}</color>");

					// FIX: Notify listeners (UIManager) to pop the welcome back screen dynamically
					OnOfflineProgressCalculated?.Invoke(timeAway, totalOfflineEarnings);
				}
				else
				{
					this.Log($"Silent offline calculation. Earned {totalOfflineEarnings.ToCurrencyString()} in {(int)timeAway.TotalSeconds}s.");
				}
			}

			// Update DataManager's timestamp to NOW so it doesn't double-count later
			_dataManager.SaveGame();
		}

		public void AcknowledgeOfflineProgress()
		{
			PendingOfflineEarnings = BigDouble.Zero;
			PendingTimeAway = TimeSpan.Zero;
		}

		private int GetMilestoneMultiplier(int units)
		{
			int multi = 1;
			for (int i = 0; i < MILESTONES.Length; i++)
			{
				if (units >= MILESTONES[i]) multi *= 2;
				else break;
			}
			return multi;
		}
	}
}

// ----------------------------------------------------------------------------
// EOF
// ----------------------------------------------------------------------------