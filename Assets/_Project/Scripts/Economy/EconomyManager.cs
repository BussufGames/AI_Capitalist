/*
 * ----------------------------------------------------------------------------
 * Project: AI Capitalist
 * Author:  Bussuf Senior Dev
 * Date:    2023-10-31
 * ----------------------------------------------------------------------------
 * Description:
 * The Core Economy Engine. Holds the Master JSON table in memory.
 * ----------------------------------------------------------------------------
 * Change Log:
 * 2023-10-30 - Bussuf Senior Dev - Added TryGetTierConfig.
 * 2023-10-31 - Bussuf Senior Dev - Added LifetimeEarnings tracking and GlobalPrestigeMultiplier.
 * ----------------------------------------------------------------------------
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using BreakInfinity;
using AI_Capitalist.Core;
using AI_Capitalist.Data;

namespace AI_Capitalist.Economy
{
	public enum BuyMode { x1 = 1, x10 = 10, x100 = 100, Max = 999 }

	public class EconomyManager : MonoBehaviour, IService
	{
		public event Action<BigDouble> OnBalanceChanged;
		public event Action OnBuyModeChanged;

		public BigDouble CurrentBalance { get; private set; }
		public BigDouble LifetimeEarnings { get; private set; }
		public BigDouble PrestigeTokens { get; private set; }
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
			_dataManager = CoreManager.Instance.GetService<DataManager>();
			if (_dataManager == null) return;

			LoadMasterTableConfig();
			LoadPlayerEconomyData();
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
			if (jsonFile != null)
			{
				MasterEconomyTable table = JsonConvert.DeserializeObject<MasterEconomyTable>(jsonFile.text);
				foreach (var tier in table.Tiers)
				{
					_masterTable[tier.TierID] = tier;
				}
			}
		}

		private void LoadPlayerEconomyData()
		{
			CurrentBalance = BigDouble.Parse(_dataManager.GameData.CurrentBalance);
			LifetimeEarnings = BigDouble.Parse(_dataManager.GameData.LifetimeEarnings);
			PrestigeTokens = BigDouble.Parse(_dataManager.GameData.PrestigeTokens);
			OnBalanceChanged?.Invoke(CurrentBalance);
		}

		// --- PRESTIGE MATH ---
		// Each token gives +10% bonus to all profits. So 10 tokens = 100% bonus (x2 multiplier)
		public double GetGlobalPrestigeMultiplier()
		{
			return 1.0 + (PrestigeTokens.ToDouble() * 0.1);
		}

		public bool TryGetTierConfig(int tierID, out TierStaticData data)
		{
			return _masterTable.TryGetValue(tierID, out data);
		}

		public TierStaticData GetTierConfig(int tierID)
		{
			if (_masterTable.TryGetValue(tierID, out var data)) return data;
			return null;
		}

		public void AddIncome(BigDouble amount)
		{
			CurrentBalance += amount;
			LifetimeEarnings += amount; // Track lifetime!
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
			_dataManager.GameData.LifetimeEarnings = LifetimeEarnings.ToString();
			OnBalanceChanged?.Invoke(CurrentBalance);
		}
	}
}

// ----------------------------------------------------------------------------
// EOF
// ----------------------------------------------------------------------------