/*
 * ----------------------------------------------------------------------------
 * Project: AI Capitalist
 * Author:  Bussuf Senior Dev
 * Date:    2026-02-28
 * ----------------------------------------------------------------------------
 * Description:
 * The core logical controller for a single business tier.
 * Handles the timer loop, manager states, debt calculation, and hiring.
 * ----------------------------------------------------------------------------
 * Change Log:
 * 2023-10-31 - Bussuf Senior Dev - Connected Prestige Multiplier to CompleteCycle.
 * 2026-02-28 - Bussuf Senior Dev - Added UpgradesManager integration.
 * 2026-02-28 - Bussuf Senior Dev - Added Overdrive (Flow) logic for AI Managers.
 * ----------------------------------------------------------------------------
 */

using System;
using UnityEngine;
using BreakInfinity;
using AI_Capitalist.Data;
using AI_Capitalist.Economy;
using AI_Capitalist.Core;

namespace AI_Capitalist.Gameplay
{
	public class TierController : MonoBehaviour
	{
		private static readonly int[] MILESTONES = { 10, 25, 50, 100, 200, 300, 400, 500, 1000 };

		public event Action<float> OnProgressUpdated;
		public event Action OnDataChanged;

		public TierDynamicData DynamicData { get; private set; }
		public TierStaticData StaticData { get; private set; }
		public bool IsInitialized { get; private set; }

		public bool IsWorkingManually => DynamicData != null && DynamicData.IsWorkingManually;

		private EconomyManager _economyManager;
		private DataManager _dataManager;
		private UpgradesManager _upgradesManager;

		// --- OVERDRIVE SETTINGS ---
		private const float OVERDRIVE_THRESHOLD = 0.2f; // Global Threshold for flow mode

		public bool IsOverdriveActive =>
			DynamicData != null &&
			DynamicData.CurrentState == ManagerState.AI &&
			GetCurrentCycleTime() <= OVERDRIVE_THRESHOLD;

		public void Initialize(TierDynamicData dynamicData, TierStaticData staticData)
		{
			DynamicData = dynamicData;
			StaticData = staticData;

			_economyManager = CoreManager.Instance.GetService<EconomyManager>();
			_dataManager = CoreManager.Instance.GetService<DataManager>();
			_upgradesManager = CoreManager.Instance.GetService<UpgradesManager>();

			if (_upgradesManager != null)
			{
				_upgradesManager.OnUpgradesChanged += HandleUpgradesChanged;
			}

			IsInitialized = true;
			OnDataChanged?.Invoke();
		}

		private void OnDestroy()
		{
			if (_upgradesManager != null)
			{
				_upgradesManager.OnUpgradesChanged -= HandleUpgradesChanged;
			}
		}

		private void Update()
		{
			if (!IsInitialized || DynamicData.OwnedUnits <= 0) return;

			// 1. Overdrive Mode (Continuous Flow)
			if (IsOverdriveActive)
			{
				DynamicData.CurrentCycleProgress = 1f; // Lock visually at 100%
				OnProgressUpdated?.Invoke(DynamicData.CurrentCycleProgress);

				BigDouble revPerSec = GetRevenuePerCycle() / GetCurrentCycleTime();
				BigDouble frameRev = revPerSec * Time.deltaTime;

				_economyManager.AddIncome(frameRev);
				return; // Skip normal chunk progress
			}

			// 2. Normal Mode (Chunk Progress)
			if (DynamicData.CurrentState == ManagerState.AI)
			{
				ProcessWorkingState(DynamicData.CurrentAISpeedMulti, false, false);
			}
			else if (DynamicData.CurrentState == ManagerState.Human)
			{
				ProcessWorkingState(DynamicData.CurrentHumanSpeedMulti, true, false);
			}
			else if (IsWorkingManually)
			{
				ProcessWorkingState(1.0f, false, true);
			}
		}

		private void ProcessWorkingState(float speedMultiplier, bool isHuman, bool isManual)
		{
			if (isHuman && IsHumanOnStrike()) return;

			float actualCycleTime = GetCurrentCycleTime(speedMultiplier);
			DynamicData.CurrentCycleProgress += Time.deltaTime;

			OnProgressUpdated?.Invoke(GetNormalizedProgress());

			if (DynamicData.CurrentCycleProgress >= actualCycleTime)
			{
				CompleteCycle(isHuman);

				if (isManual)
				{
					DynamicData.CurrentCycleProgress = 0f;
					DynamicData.IsWorkingManually = false;
				}
				else
				{
					DynamicData.CurrentCycleProgress -= actualCycleTime;
				}

				OnProgressUpdated?.Invoke(GetNormalizedProgress());
				OnDataChanged?.Invoke();
			}
		}

		private void CompleteCycle(bool isHuman)
		{
			BigDouble earned = GetRevenuePerCycle();
			_economyManager.AddIncome(earned);

			if (isHuman)
			{
				BigDouble salary = BigDouble.Parse(StaticData.Base_Human_Salary_Per_Cycle);
				BigDouble currentDebt = BigDouble.Parse(DynamicData.AccumulatedDebt);
				DynamicData.AccumulatedDebt = (currentDebt + salary).ToString();
			}
		}

		// --- NEW REVENUE CALCULATION (Includes Upgrades) ---
		public BigDouble GetRevenuePerCycle()
		{
			BigDouble baseRev = BigDouble.Parse(StaticData.Base_Rev);
			double prestigeBonus = _economyManager != null ? _economyManager.GetGlobalPrestigeMultiplier() : 1.0;
			double upgradesBonus = _upgradesManager != null ? _upgradesManager.GetRevenueMultiplier(StaticData.TierID) : 1.0;

			return baseRev * DynamicData.OwnedUnits * GetMilestoneMultiplier() * prestigeBonus * upgradesBonus;
		}

		// --- NEW SPEED CALCULATION (Includes Upgrades) ---
		public float GetCurrentCycleTime(float managerSpeedMulti = 1f)
		{
			// If calling from outside without specifying manager speed, figure it out based on state
			if (managerSpeedMulti == 1f)
			{
				if (DynamicData.CurrentState == ManagerState.Human) managerSpeedMulti = DynamicData.CurrentHumanSpeedMulti;
				else if (DynamicData.CurrentState == ManagerState.AI) managerSpeedMulti = DynamicData.CurrentAISpeedMulti;
			}

			float baseTime = StaticData.Cycle_Time;
			double upgradesSpeedMulti = _upgradesManager != null ? _upgradesManager.GetSpeedMultiplier(StaticData.TierID) : 1.0;

			float finalTime = (baseTime / managerSpeedMulti) / (float)upgradesSpeedMulti;

			return Mathf.Max(0.01f, finalTime); // Absolute safety clamp to prevent division by zero
		}

		public bool IsHumanOnStrike()
		{
			if (DynamicData.CurrentState != ManagerState.Human) return false;

			BigDouble salary = BigDouble.Parse(StaticData.Base_Human_Salary_Per_Cycle);
			if (salary <= BigDouble.Zero) return false;

			BigDouble currentDebt = BigDouble.Parse(DynamicData.AccumulatedDebt);
			int missedPayments = (int)Math.Floor((currentDebt / salary).ToDouble());

			return missedPayments >= 5;
		}

		public int GetMilestoneMultiplier()
		{
			int multi = 1;
			for (int i = 0; i < MILESTONES.Length; i++)
			{
				if (DynamicData.OwnedUnits >= MILESTONES[i]) multi *= 2;
				else break;
			}
			return multi;
		}

		public float GetMilestoneProgress()
		{
			int lastMilestone = 0;
			int nextMilestone = MILESTONES[0];
			for (int i = 0; i < MILESTONES.Length; i++)
			{
				if (DynamicData.OwnedUnits >= MILESTONES[i])
				{
					lastMilestone = MILESTONES[i];
					nextMilestone = (i + 1 < MILESTONES.Length) ? MILESTONES[i + 1] : MILESTONES[i];
				}
				else
				{
					break;
				}
			}

			if (lastMilestone == nextMilestone) return 1f;
			float currentProgress = DynamicData.OwnedUnits - lastMilestone;
			float requiredForNext = nextMilestone - lastMilestone;
			return Mathf.Clamp01(currentProgress / requiredForNext);
		}

		public float GetNormalizedProgress()
		{
			if (IsOverdriveActive) return 1f; // Lock visually full in overdrive
			float actualCycleTime = GetCurrentCycleTime();
			return Mathf.Clamp01(DynamicData.CurrentCycleProgress / actualCycleTime);
		}

		public void ManualClick()
		{
			if (DynamicData.CurrentState != ManagerState.None || IsWorkingManually) return;

			DynamicData.IsWorkingManually = true;
			_dataManager.SaveGame();
			OnDataChanged?.Invoke();
		}

		public void BuyUnits()
		{
			BigDouble cost = _economyManager.GetBuyCostAndAmount(StaticData.TierID, DynamicData.OwnedUnits, out int amountToBuy);
			if (amountToBuy > 0 && _economyManager.TrySpend(cost))
			{
				DynamicData.OwnedUnits += amountToBuy;
				_dataManager.SaveGame();
				OnDataChanged?.Invoke();
			}
		}

		public void HireHumanManager()
		{
			BigDouble cost = BigDouble.Parse(StaticData.Human_Hire_Cost);
			if (DynamicData.CurrentState == ManagerState.None && _economyManager.TrySpend(cost))
			{
				DynamicData.CurrentState = ManagerState.Human;
				_dataManager.SaveGame();
				OnDataChanged?.Invoke();
			}
		}

		public void HireAIManager()
		{
			BigDouble baseAiCost = BigDouble.Parse(StaticData.AI_Hire_Cost);
			BigDouble currentDebt = BigDouble.Parse(DynamicData.AccumulatedDebt);
			BigDouble totalCost = baseAiCost + currentDebt;
			if (DynamicData.CurrentState != ManagerState.AI && _economyManager.TrySpend(totalCost))
			{
				DynamicData.CurrentState = ManagerState.AI;
				DynamicData.AccumulatedDebt = "0";
				DynamicData.IsWorkingManually = false;

				_dataManager.SaveGame();
				OnDataChanged?.Invoke();
			}
		}

		public void PayHumanDebt()
		{
			BigDouble currentDebt = BigDouble.Parse(DynamicData.AccumulatedDebt);
			if (currentDebt > 0 && _economyManager.TrySpend(currentDebt))
			{
				DynamicData.AccumulatedDebt = "0";
				_dataManager.SaveGame();
				OnDataChanged?.Invoke();
			}
		}

		private void HandleUpgradesChanged()
		{
			OnDataChanged?.Invoke(); // Refresh UI if an upgrade affected this tier
		}

		// --- DEV TOOLS / QA ONLY ---
		public void ForceStrike()
		{
			if (DynamicData.CurrentState != ManagerState.Human) return;

			BigDouble salary = BigDouble.Parse(StaticData.Base_Human_Salary_Per_Cycle);
			BigDouble strikeDebt = salary * 5; // 5 missed payments triggers a strike

			DynamicData.AccumulatedDebt = strikeDebt.ToString();
			_dataManager.SaveGame();
			OnDataChanged?.Invoke();
		}
	}
}

// ----------------------------------------------------------------------------
// EOF
// ----------------------------------------------------------------------------