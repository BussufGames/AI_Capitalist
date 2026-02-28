/*
 * ----------------------------------------------------------------------------
 * Project: AI Capitalist
 * Author:  Bussuf Senior Dev
 * Date:    2026-02-28
 * ----------------------------------------------------------------------------
 * Description:
 * Core service managing Upgrade logic, purchasing, and applying multipliers.
 * ----------------------------------------------------------------------------
 * Change Log:
 * 2026-02-28 - Bussuf Senior Dev - Initial implementation.
 * ----------------------------------------------------------------------------
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using BreakInfinity;
using AI_Capitalist.Core;
using AI_Capitalist.Data;
using AI_Capitalist.Economy;
using BussufGames.Core;

namespace AI_Capitalist.Gameplay
{
	public class UpgradesManager : MonoBehaviour, IService
	{
		#region Events & Properties
		public event Action OnUpgradesChanged;
		#endregion

		#region Fields
		private DataManager _dataManager;
		private EconomyManager _economyManager;
		private Dictionary<string, UpgradeStaticData> _allUpgrades;
		#endregion

		#region Initialization
		private void Awake()
		{
			if (CoreManager.Instance != null)
				CoreManager.Instance.RegisterService<UpgradesManager>(this);
		}

		public void Initialize()
		{
			this.Log("Initializing UpgradesManager...");
			_dataManager = CoreManager.Instance.GetService<DataManager>();
			_economyManager = CoreManager.Instance.GetService<EconomyManager>();

			if (_economyManager != null)
			{
				_allUpgrades = _economyManager.GetAllUpgrades();
				this.LogSuccess($"UpgradesManager loaded {_allUpgrades.Count} static upgrades.");
			}
		}
		#endregion

		#region Validation & Purchasing
		public bool IsUpgradePurchased(string upgradeID)
		{
			if (_dataManager?.GameData == null) return false;
			return _dataManager.GameData.PurchasedUpgrades.Contains(upgradeID);
		}

		public bool CanAfford(UpgradeStaticData upgrade)
		{
			if (IsUpgradePurchased(upgrade.UpgradeID)) return false;
			BigDouble cost = BigDouble.Parse(upgrade.Cost);
			return _economyManager.CurrentBalance >= cost;
		}

		public void BuyUpgrade(string upgradeID)
		{
			if (IsUpgradePurchased(upgradeID)) return;
			if (_allUpgrades == null || !_allUpgrades.TryGetValue(upgradeID, out var upgrade)) return;

			BigDouble cost = BigDouble.Parse(upgrade.Cost);
			if (_economyManager.TrySpend(cost))
			{
				_dataManager.GameData.PurchasedUpgrades.Add(upgradeID);
				_dataManager.SaveGame();

				this.LogSuccess($"Purchased upgrade: {upgrade.Name} ({upgrade.UpgradeType} x{upgrade.Multiplier})");
				OnUpgradesChanged?.Invoke();
			}
		}
		#endregion

		#region Multiplier Calculations
		public double GetRevenueMultiplier(int tierID)
		{
			double multi = 1.0;
			if (_dataManager?.GameData == null || _allUpgrades == null) return multi;

			foreach (var upgradeID in _dataManager.GameData.PurchasedUpgrades)
			{
				if (_allUpgrades.TryGetValue(upgradeID, out var upg))
				{
					if (upg.UpgradeType == "Revenue" && (upg.TargetTierID == 0 || upg.TargetTierID == tierID))
					{
						multi *= upg.Multiplier;
					}
				}
			}
			return multi;
		}

		public double GetSpeedMultiplier(int tierID)
		{
			double multi = 1.0;
			if (_dataManager?.GameData == null || _allUpgrades == null) return multi;

			foreach (var upgradeID in _dataManager.GameData.PurchasedUpgrades)
			{
				if (_allUpgrades.TryGetValue(upgradeID, out var upg))
				{
					if (upg.UpgradeType == "Speed" && (upg.TargetTierID == 0 || upg.TargetTierID == tierID))
					{
						multi *= upg.Multiplier;
					}
				}
			}
			return multi;
		}
		#endregion

		#region UI Helpers
		public UpgradeStaticData GetCheapestAvailableUpgrade()
		{
			if (_allUpgrades == null) return null;

			UpgradeStaticData cheapest = null;
			BigDouble minCost = BigDouble.PositiveInfinity;

			foreach (var upg in _allUpgrades.Values)
			{
				if (IsUpgradePurchased(upg.UpgradeID)) continue;

				// Don't suggest upgrades for businesses the player hasn't unlocked yet
				if (upg.TargetTierID != 0 && upg.TargetTierID > _dataManager.GameData.HighestUnlockedTier) continue;

				BigDouble cost = BigDouble.Parse(upg.Cost);
				if (cost < minCost)
				{
					minCost = cost;
					cheapest = upg;
				}
			}
			return cheapest;
		}
		#endregion
	}
}

// ----------------------------------------------------------------------------
// EOF
// ----------------------------------------------------------------------------