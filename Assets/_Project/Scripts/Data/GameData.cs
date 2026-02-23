/*
 * ----------------------------------------------------------------------------
 * Project: AI Capitalist
 * Author:  Bussuf Senior Dev
 * Date:    2023-10-29
 * ----------------------------------------------------------------------------
 * Description:
 * Holds the Data Models for the player's save file.
 * ----------------------------------------------------------------------------
 * Change Log:
 * 2023-10-28 - Bussuf Senior Dev - Initial implementation.
 * 2023-10-29 - Bussuf Senior Dev - Added JsonProperty attributes to fix deserialization.
 * 2023-10-29 - Bussuf Senior Dev - Added IsWorkingManually to persistent TierDynamicData.
 * 2023-10-30 - Bussuf Senior Dev - Added HighestUnlockedTier.
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
		[JsonProperty]
		public string LastSaveTime { get; set; }

		[JsonProperty]
		public string CurrentBalance { get; set; } = "0";

		// NEW: Tracks how many tiers the player has bought access to.
		[JsonProperty]
		public int HighestUnlockedTier { get; set; } = 1;

		[JsonProperty]
		public List<TierDynamicData> TiersData { get; set; } = new List<TierDynamicData>();
	}

	[Serializable]
	public class TierDynamicData
	{
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

		public TierDynamicData(int tierID)
		{
			TierID = tierID;
		}

		public TierDynamicData() { }
	}
}

// ----------------------------------------------------------------------------
// EOF
// ----------------------------------------------------------------------------