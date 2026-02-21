/*
 * ----------------------------------------------------------------------------
 * Project: AI Capitalist
 * Author:  Bussuf Senior Dev
 * Date:    2023-10-28
 * ----------------------------------------------------------------------------
 * Description:
 * The Core Economy Engine. Holds the Master JSON table in memory, 
 * handles BigDouble conversions, and manages the primary game balance.
 * ----------------------------------------------------------------------------
 * Change Log:
 * 2023-10-28 - Bussuf Senior Dev - Initial implementation.
 * ----------------------------------------------------------------------------
 */

using AI_Capitalist.Core;
using AI_Capitalist.Data;
using BreakInfinity;
using BussufGames.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AI_Capitalist.Economy
{
	public class EconomyManager : MonoBehaviour, IService
	{
		// Action to notify UI when balance changes (Decoupling)
		public event Action<BigDouble> OnBalanceChanged;

		public BigDouble CurrentBalance { get; private set; }

		private Dictionary<int, TierStaticData> _masterTable = new Dictionary<int, TierStaticData>();
		private DataManager _dataManager;

		private void Awake()
		{
			if (CoreManager.Instance != null)
			{
				CoreManager.Instance.RegisterService<EconomyManager>(this);
			}
		}

		public void Initialize()
		{
			this.Log("Initializing EconomyManager...");

			_dataManager = CoreManager.Instance.GetService<DataManager>();
			if (_dataManager == null)
			{
				this.LogError("DataManager is missing! Economy cannot initialize properly.");
				return;
			}

			LoadMasterTableConfig();
			LoadPlayerBalance();
		}

		/// <summary>
		/// Loads the economy balancing from a JSON file (currently Resources, later remote).
		/// </summary>
		private void LoadMasterTableConfig()
		{
			// For MVP, we load from a local Resources file named "MasterTable"
			TextAsset jsonFile = Resources.Load<TextAsset>("MasterTable");
			if (jsonFile == null)
			{
				this.LogError("Could not find 'MasterTable.json' in Resources!");
				return;
			}

			MasterEconomyTable table = JsonConvert.DeserializeObject<MasterEconomyTable>(jsonFile.text);
			foreach (var tier in table.Tiers)
			{
				_masterTable[tier.TierID] = tier;
			}

			this.LogSuccess($"Loaded Economy Config with {_masterTable.Count} Tiers.");
		}

		private void LoadPlayerBalance()
		{
			// The PlayerSaveData stores string to prevent JSON loss. 
			// We convert it to BigDouble here.
			string savedBalance = _dataManager.GameData.CurrentBalance;
			CurrentBalance = BigDouble.Parse(savedBalance);
			this.Log($"Player starting balance: {CurrentBalance.ToCurrencyString()}");

			OnBalanceChanged?.Invoke(CurrentBalance);
		}

		public TierStaticData GetTierConfig(int tierID)
		{
			if (_masterTable.TryGetValue(tierID, out var data))
			{
				return data;
			}
			this.LogError($"Requested config for unknown Tier ID: {tierID}");
			return null;
		}

		public void AddIncome(BigDouble amount)
		{
			CurrentBalance += amount;
			UpdateSaveData();
		}

		public bool TrySpend(BigDouble cost)
		{
			if (CurrentBalance >= cost)
			{
				CurrentBalance -= cost;
				UpdateSaveData();
				return true;
			}
			return false;
		}

		/// <summary>
		/// Calculates the cost of upgrading a tier based on the geometric series formula:
		/// Cost = BaseCost * (GrowthFactor ^ CurrentOwnedUnits)
		/// </summary>
		public BigDouble CalculateNextUnitCost(int tierID, int currentOwnedUnits)
		{
			var config = GetTierConfig(tierID);
			if (config == null) return BigDouble.Zero;

			BigDouble baseCost = BigDouble.Parse(config.Base_Cost);
			// cost = base * (growth ^ units)
			return baseCost * BigDouble.Pow(config.Growth_Factor, currentOwnedUnits);
		}

		private void UpdateSaveData()
		{
			// Save string representation back to Data layer
			_dataManager.GameData.CurrentBalance = CurrentBalance.ToString();
			OnBalanceChanged?.Invoke(CurrentBalance);

			// Note: We don't call _dataManager.SaveGame() on EVERY coin picked up 
			// to save performance. Saving should be done periodically or on exit.
		}
	}
}

// ----------------------------------------------------------------------------
// EOF
// ----------------------------------------------------------------------------