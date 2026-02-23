/*
 * ----------------------------------------------------------------------------
 * Project: AI Capitalist
 * Author:  Bussuf Senior Dev
 * Date:    2023-10-30
 * ----------------------------------------------------------------------------
 * Description:
 * The Core Economy Engine. Holds the Master JSON table in memory.
 * ----------------------------------------------------------------------------
 * Change Log:
 * 2023-10-30 - Bussuf Senior Dev - Added TryGetTierConfig for safe EOC checking.
 * 2023-10-31 - Bussuf Senior Dev - Added SetBalance function for non-destructive dev testing.
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
	public enum BuyMode { x1 = 1, x10 = 10, x100 = 100, Max = 999 }

	public class EconomyManager : MonoBehaviour, IService
	{
		public event Action<BigDouble> OnBalanceChanged;
		public event Action OnBuyModeChanged;

		public BigDouble CurrentBalance { get; private set; }
		public BuyMode CurrentBuyMode { get; private set; } = BuyMode.x1;

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
				this.LogError("DataManager is missing!");
				return;
			}

			LoadMasterTableConfig();
			LoadPlayerBalance();
		}

		public void ToggleBuyMode()
		{
			CurrentBuyMode = CurrentBuyMode switch
			{
				BuyMode.x1 => BuyMode.x10,
				BuyMode.x10 => BuyMode.x100,
				BuyMode.x100 => BuyMode.Max,
				BuyMode.Max => BuyMode.x1,
				_ => BuyMode.x1
			};

			OnBuyModeChanged?.Invoke();
		}

		private void LoadMasterTableConfig()
		{
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
			string savedBalance = _dataManager.GameData.CurrentBalance;
			CurrentBalance = BigDouble.Parse(savedBalance);
			OnBalanceChanged?.Invoke(CurrentBalance);
		}

		// Standard getter (Logs an error if missing)
		public TierStaticData GetTierConfig(int tierID)
		{
			if (_masterTable.TryGetValue(tierID, out var data)) return data;

			this.LogError($"Requested config for unknown Tier ID: {tierID}");
			return null;
		}

		// Safe getter for checking if a tier exists (Used for progression/EOC)
		public bool TryGetTierConfig(int tierID, out TierStaticData data)
		{
			return _masterTable.TryGetValue(tierID, out data);
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

		// DEV TOOL: Non-destructive balance reset
		public void SetBalance(BigDouble newBalance)
		{
			CurrentBalance = newBalance;
			UpdateSaveData();
		}

		public BigDouble GetBuyCostAndAmount(int tierID, int currentOwnedUnits, out int amountToBuy)
		{
			amountToBuy = 0;
			if (!TryGetTierConfig(tierID, out var config)) return BigDouble.Zero;

			BigDouble baseCost = BigDouble.Parse(config.Base_Cost);
			double r = config.Growth_Factor;
			BigDouble a = baseCost * BigDouble.Pow(r, currentOwnedUnits);

			if (CurrentBuyMode == BuyMode.Max)
			{
				if (CurrentBalance < a)
				{
					amountToBuy = 1;
					return a;
				}

				BigDouble rhs = (CurrentBalance * (r - 1.0) / a) + 1.0;
				double n = BigDouble.Log10(rhs) / Math.Log10(r);
				amountToBuy = (int)Math.Floor(n);

				return a * (BigDouble.Pow(r, amountToBuy) - 1.0) / (r - 1.0);
			}
			else
			{
				amountToBuy = (int)CurrentBuyMode;
				return a * (BigDouble.Pow(r, amountToBuy) - 1.0) / (r - 1.0);
			}
		}

		private void UpdateSaveData()
		{
			_dataManager.GameData.CurrentBalance = CurrentBalance.ToString();
			OnBalanceChanged?.Invoke(CurrentBalance);
		}
	}
}

// ----------------------------------------------------------------------------
// EOF
// ----------------------------------------------------------------------------