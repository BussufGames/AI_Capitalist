/*
 * ----------------------------------------------------------------------------
 * Project: AI Capitalist
 * Author:  Bussuf Senior Dev
 * Date:    2023-10-29
 * ----------------------------------------------------------------------------
 * Description:
 * The core logical controller for a single business tier. 
 * Handles the timer loop, manager states, debt calculation, and hiring.
 * ----------------------------------------------------------------------------
 * Change Log:
 * 2023-10-28 - Bussuf Senior Dev - Initial implementation.
 * 2023-10-29 - Bussuf Senior Dev - Added Global Milestones & Multi-buy logic.
 * 2023-10-29 - Bussuf Senior Dev - Converted ManualClick to time-based progress.
 * 2023-10-29 - Bussuf Senior Dev - Added HireHuman, HireAI, and PayDebt logic.
 * 2023-10-29 - Bussuf Senior Dev - Linked IsWorkingManually to persistent DynamicData.
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

		// Proxy to the persistent saved data
		public bool IsWorkingManually => DynamicData != null && DynamicData.IsWorkingManually;

		private EconomyManager _economyManager;
		private DataManager _dataManager;

		public void Initialize(TierDynamicData dynamicData, TierStaticData staticData)
		{
			DynamicData = dynamicData;
			StaticData = staticData;

			_economyManager = CoreManager.Instance.GetService<EconomyManager>();
			_dataManager = CoreManager.Instance.GetService<DataManager>();

			IsInitialized = true;
			OnDataChanged?.Invoke();
		}

		private void Update()
		{
			if (!IsInitialized || DynamicData.OwnedUnits <= 0) return;

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

			float actualCycleTime = StaticData.Cycle_Time / speedMultiplier;
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
			BigDouble baseRev = BigDouble.Parse(StaticData.Base_Rev);
			BigDouble earned = baseRev * DynamicData.OwnedUnits * GetMilestoneMultiplier();

			_economyManager.AddIncome(earned);

			if (isHuman)
			{
				BigDouble salary = BigDouble.Parse(StaticData.Base_Human_Salary_Per_Cycle);
				BigDouble currentDebt = BigDouble.Parse(DynamicData.AccumulatedDebt);
				DynamicData.AccumulatedDebt = (currentDebt + salary).ToString();
			}
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
			float speedMultiplier = 1f;
			if (DynamicData.CurrentState == ManagerState.AI) speedMultiplier = DynamicData.CurrentAISpeedMulti;
			else if (DynamicData.CurrentState == ManagerState.Human) speedMultiplier = DynamicData.CurrentHumanSpeedMulti;

			float actualCycleTime = StaticData.Cycle_Time / speedMultiplier;
			return Mathf.Clamp01(DynamicData.CurrentCycleProgress / actualCycleTime);
		}

		// --- PUBLIC ACTIONS ---

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

		// --- MANAGERS LOGIC ---

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
	}
}

// ----------------------------------------------------------------------------
// EOF
// ----------------------------------------------------------------------------