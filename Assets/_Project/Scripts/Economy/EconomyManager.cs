/*
 * ----------------------------------------------------------------------------
 * Project: AI Capitalist
 * Author:  Bussuf Senior Dev
 * Date:    2023-10-28
 * ----------------------------------------------------------------------------
 * Description:
 * The Core Economy Engine. Holds the Master JSON table in memory, 
 * handles BigDouble conversions, and manages the primary game balance.
 * Includes Geometric Series math for multi-purchases (x1, x10, x100, MAX).
 * ----------------------------------------------------------------------------
 * Change Log:
 * 2023-10-28 - Bussuf Senior Dev - Initial implementation.
 * 2023-10-29 - Bussuf Senior Dev - Added BuyMode (x1,x10,x100,Max) & Geometric series cost math.
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
				this.LogError("DataManager is missing! Economy cannot initialize properly.");
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

			this.Log($"Buy Mode changed to: {CurrentBuyMode}");
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
		/// Calculates the cost and actual amount of units to buy based on the current BuyMode.
		/// Uses the Geometric Series formula for rapid O(1) calculation.
		/// </summary>
		public BigDouble GetBuyCostAndAmount(int tierID, int currentOwnedUnits, out int amountToBuy)
		{
			amountToBuy = 0;
			var config = GetTierConfig(tierID);
			if (config == null) return BigDouble.Zero;

			BigDouble baseCost = BigDouble.Parse(config.Base_Cost);
			double r = config.Growth_Factor;
			BigDouble a = baseCost * BigDouble.Pow(r, currentOwnedUnits); // Cost of the immediate next unit

			if (CurrentBuyMode == BuyMode.Max)
			{
				if (CurrentBalance < a)
				{
					amountToBuy = 1; // Show cost of 1 even if can't afford
					return a;
				}

				// Max formula: n = floor( log_r( (Balance * (r-1) / a) + 1 ) )
				BigDouble rhs = (CurrentBalance * (r - 1.0) / a) + 1.0;
				double n = BigDouble.Log10(rhs) / Math.Log10(r);
				amountToBuy = (int)Math.Floor(n);

				// Return total cost for 'amountToBuy' units
				return a * (BigDouble.Pow(r, amountToBuy) - 1.0) / (r - 1.0);
			}
			else
			{
				amountToBuy = (int)CurrentBuyMode;
				// Sum formula: a * (r^n - 1) / (r - 1)
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