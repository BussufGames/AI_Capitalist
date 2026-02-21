/*
 * ----------------------------------------------------------------------------
 * Project: AI Capitalist
 * Author:  Bussuf Senior Dev
 * Date:    2023-10-28
 * ----------------------------------------------------------------------------
 * Description:
 * The core logical controller for a single business tier. 
 * Handles the timer loop (Update), manager states, debt calculation, 
 * and notifies listeners when progress or data changes.
 * ----------------------------------------------------------------------------
 * Change Log:
 * 2023-10-28 - Bussuf Senior Dev - Initial implementation.
 * 2023-10-29 - Bussuf Senior Dev - Added Global Milestones logic (x2 revenue).
 * 2023-10-29 - Bussuf Senior Dev - Updated Buy logic to support multi-buy.
 * ----------------------------------------------------------------------------
 */

using AI_Capitalist.Core;
using AI_Capitalist.Data;
using AI_Capitalist.Economy;
using BreakInfinity;
using BussufGames.Core;
using System;
using UnityEngine;

namespace AI_Capitalist.Gameplay
{
	public class TierController : MonoBehaviour
	{
		// Global milestones array for MVP
		private static readonly int[] MILESTONES = { 10, 25, 50, 100, 200, 300, 400, 500, 1000 };

		public event Action<float> OnProgressUpdated;
		public event Action OnDataChanged;

		public TierDynamicData DynamicData { get; private set; }
		public TierStaticData StaticData { get; private set; }

		private EconomyManager _economyManager;
		private DataManager _dataManager;
		public bool IsInitialized { get; private set; }

		public void Initialize(TierDynamicData dynamicData, TierStaticData staticData)
		{
			DynamicData = dynamicData;
			StaticData = staticData;

			_economyManager = CoreManager.Instance.GetService<EconomyManager>();
			_dataManager = CoreManager.Instance.GetService<DataManager>();

			IsInitialized = true;
			this.Log($"Tier {StaticData.TierID} ({StaticData.BusinessName}) Logic Initialized.");

			OnDataChanged?.Invoke();
		}

		private void Update()
		{
			if (!IsInitialized || DynamicData.OwnedUnits <= 0) return;

			if (DynamicData.CurrentState == ManagerState.AI)
			{
				ProcessWorkingState(DynamicData.CurrentAISpeedMulti, false);
			}
			else if (DynamicData.CurrentState == ManagerState.Human)
			{
				ProcessWorkingState(DynamicData.CurrentHumanSpeedMulti, true);
			}
		}

		private void ProcessWorkingState(float speedMultiplier, bool isHuman)
		{
			if (isHuman && IsHumanOnStrike()) return;

			float actualCycleTime = StaticData.Cycle_Time / speedMultiplier;
			DynamicData.CurrentCycleProgress += Time.deltaTime;

			OnProgressUpdated?.Invoke(Mathf.Clamp01(DynamicData.CurrentCycleProgress / actualCycleTime));

			if (DynamicData.CurrentCycleProgress >= actualCycleTime)
			{
				CompleteCycle(isHuman);
				DynamicData.CurrentCycleProgress -= actualCycleTime;
			}
		}

		private void CompleteCycle(bool isHuman)
		{
			BigDouble baseRev = BigDouble.Parse(StaticData.Base_Rev);
			// Apply Milestone Multipliers
			BigDouble earned = baseRev * DynamicData.OwnedUnits * GetMilestoneMultiplier();

			_economyManager.AddIncome(earned);

			if (isHuman)
			{
				BigDouble salary = BigDouble.Parse(StaticData.Base_Human_Salary_Per_Cycle);
				BigDouble currentDebt = BigDouble.Parse(DynamicData.AccumulatedDebt);
				DynamicData.AccumulatedDebt = (currentDebt + salary).ToString();
			}

			OnDataChanged?.Invoke();
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

		// --- MILESTONE LOGIC ---

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

			if (lastMilestone == nextMilestone) return 1f; // Reached max milestone

			float currentProgress = DynamicData.OwnedUnits - lastMilestone;
			float requiredForNext = nextMilestone - lastMilestone;
			return Mathf.Clamp01(currentProgress / requiredForNext);
		}

		// --- PUBLIC ACTIONS ---

		public void ManualClick()
		{
			if (DynamicData.CurrentState != ManagerState.None) return;
			CompleteCycle(false);
			_dataManager.SaveGame();
		}

		public void BuyUnits()
		{
			BigDouble cost = _economyManager.GetBuyCostAndAmount(StaticData.TierID, DynamicData.OwnedUnits, out int amountToBuy);

			if (amountToBuy > 0 && _economyManager.TrySpend(cost))
			{
				DynamicData.OwnedUnits += amountToBuy;
				_dataManager.SaveGame();
				OnDataChanged?.Invoke();
				this.LogSuccess($"Bought {amountToBuy} units for {StaticData.BusinessName}. Total: {DynamicData.OwnedUnits}");
			}
		}
	}
}

// ----------------------------------------------------------------------------
// EOF
// ----------------------------------------------------------------------------