/*
 * ----------------------------------------------------------------------------
 * Project: AI Capitalist
 * Author:  Bussuf Senior Dev
 * Date:    2026-02-28
 * ----------------------------------------------------------------------------
 * Description:
 * Holds the Data Models for the player's save file.
 * ----------------------------------------------------------------------------
 * Change Log:
 * 2023-10-28 - Bussuf Senior Dev - Initial implementation.
 * 2023-10-31 - Bussuf Senior Dev - Added LifetimeEarnings and PrestigeTokens.
 * 2026-02-28 - Bussuf Senior Dev - Added PurchasedUpgrades list. Applied #region tags.
 * ----------------------------------------------------------------------------
 */

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace AI_Capitalist.Data
{
	public enum ManagerState { None, Human, AI }

	[Serializable]
	public class PlayerSaveData
	{
		#region General Data
		[JsonProperty]
		public string LastSaveTime { get; set; }

		[JsonProperty]
		public string CurrentBalance { get; set; } = "0";

		[JsonProperty]
		public int HighestUnlockedTier { get; set; } = 1;
		#endregion

		#region Prestige System Data
		[JsonProperty]
		public string LifetimeEarnings { get; set; } = "0"; // Used to calculate available prestige rewards

		[JsonProperty]
		public string PrestigeTokens { get; set; } = "0"; // The premium currency kept after reset
		#endregion

		#region Collections
		[JsonProperty]
		public List<TierDynamicData> TiersData { get; set; } = new List<TierDynamicData>();

		// NEW: Tracks which upgrades the player has purchased by ID
		[JsonProperty]
		public List<string> PurchasedUpgrades { get; set; } = new List<string>();
		#endregion
	}

	[Serializable]
	public class TierDynamicData
	{
		#region Tier State
		[JsonProperty]
		public int TierID { get; set; }

		[JsonProperty]
		public int OwnedUnits { get; set; } = 0;

		[JsonProperty]
		public ManagerState CurrentState { get; set; } = ManagerState.None;

		[JsonProperty]
		public bool IsWorkingManually { get; set; } = false;

		[JsonProperty]
		public float CurrentCycleProgress { get; set; } = 0f;

		[JsonProperty]
		public string AccumulatedDebt { get; set; } = "0";

		[JsonProperty]
		public float CurrentHumanSpeedMulti { get; set; } = 1.0f;

		[JsonProperty]
		public float CurrentAISpeedMulti { get; set; } = 2.0f;
		#endregion

		#region Constructors
		public TierDynamicData(int tierID)
		{
			TierID = tierID;
		}

		public TierDynamicData() { }
		#endregion
	}
}

// ----------------------------------------------------------------------------
// EOF
// ----------------------------------------------------------------------------