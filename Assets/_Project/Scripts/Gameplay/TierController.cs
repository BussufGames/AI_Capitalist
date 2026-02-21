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
		// Events for the UI layer to listen to (Decoupling)
		public event Action<float> OnProgressUpdated; // Passes a normalized value (0.0 to 1.0)
		public event Action OnDataChanged;            // Fired when stats/units/states change

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
			// Do not process if uninitialized or if the player owns 0 units
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
			if (isHuman && IsHumanOnStrike())
			{
				return; // The human is striking! Zzz... No progress.
			}

			float actualCycleTime = StaticData.Cycle_Time / speedMultiplier;
			DynamicData.CurrentCycleProgress += Time.deltaTime;

			// Notify UI about the progress bar movement
			OnProgressUpdated?.Invoke(Mathf.Clamp01(DynamicData.CurrentCycleProgress / actualCycleTime));

			if (DynamicData.CurrentCycleProgress >= actualCycleTime)
			{
				CompleteCycle(isHuman);
				// Reset progress (keep remainder for precision)
				DynamicData.CurrentCycleProgress -= actualCycleTime;
			}
		}

		private void CompleteCycle(bool isHuman)
		{
			BigDouble baseRev = BigDouble.Parse(StaticData.Base_Rev);
			BigDouble earned = baseRev * DynamicData.OwnedUnits;

			_economyManager.AddIncome(earned);

			if (isHuman)
			{
				BigDouble salary = BigDouble.Parse(StaticData.Base_Human_Salary_Per_Cycle);
				BigDouble currentDebt = BigDouble.Parse(DynamicData.AccumulatedDebt);

				// Add salary to debt. The player hasn't lost money yet, just owes it.
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

			// If the human missed 5 payments, they strike.
			return missedPayments >= 5;
		}

		// --- PUBLIC ACTIONS (Called by UI or Input later) ---

		public void ManualClick()
		{
			// Can only manually click if NO manager is present
			if (DynamicData.CurrentState != ManagerState.None) return;

			float actualCycleTime = StaticData.Cycle_Time;
			// For MVP manual clicking is instant. If you want a progress bar for manual, we adapt here.
			// Let's make it instant for the basic clicker feel.
			CompleteCycle(false);
			_dataManager.SaveGame(); // Save on action
		}

		public void BuyUnit()
		{
			BigDouble cost = _economyManager.CalculateNextUnitCost(StaticData.TierID, DynamicData.OwnedUnits);
			if (_economyManager.TrySpend(cost))
			{
				DynamicData.OwnedUnits++;
				_dataManager.SaveGame();
				OnDataChanged?.Invoke();
				this.LogSuccess($"Bought unit for {StaticData.BusinessName}. Total: {DynamicData.OwnedUnits}");
			}
		}
	}
}

// ----------------------------------------------------------------------------
// EOF
// ----------------------------------------------------------------------------