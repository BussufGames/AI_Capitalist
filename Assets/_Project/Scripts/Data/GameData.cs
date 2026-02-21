/*
 * ----------------------------------------------------------------------------
 * Project: AI Capitalist
 * Author:  Bussuf Senior Dev
 * Date:    2023-10-28
 * ----------------------------------------------------------------------------
 * Description:
 * Contains the pure data structures for the player's save file.
 * These are strictly data containers (POCOs) with no game logic, 
 * ready to be serialized to JSON via Newtonsoft.
 * ----------------------------------------------------------------------------
 * Change Log:
 * 2023-10-28 - Bussuf Senior Dev - Initial implementation based on TDD Part 1.
 * ----------------------------------------------------------------------------
 */

using System.Collections.Generic;

namespace AI_Capitalist.Data
{
	public enum ManagerState
	{
		None = 0,
		Human = 1,
		AI = 2
	}

	[System.Serializable]
	public class TierDynamicData
	{
		public int TierID;
		public int OwnedUnits;
		public ManagerState CurrentState;

		// Stored as string to support BigDouble in the Economy Engine
		public string AccumulatedDebt;

		public float CurrentCycleProgress;
		public float CurrentHumanSpeedMulti;
		public float CurrentAISpeedMulti;

		public TierDynamicData(int id)
		{
			TierID = id;
			OwnedUnits = 0;
			CurrentState = ManagerState.None;
			AccumulatedDebt = "0";
			CurrentCycleProgress = 0f;
			CurrentHumanSpeedMulti = 1.0f;
			CurrentAISpeedMulti = 2.0f; // AI is natively 2x faster
		}
	}

	[System.Serializable]
	public class PlayerSaveData
	{
		public string LastSaveTime;

		// Stored as string to support BigDouble
		public string CurrentBalance;
		public int PrestigeLevel;
		public List<TierDynamicData> TiersData;

		public PlayerSaveData()
		{
			LastSaveTime = string.Empty;
			CurrentBalance = "0";
			PrestigeLevel = 0;
			TiersData = new List<TierDynamicData>();
		}
	}
}

// ----------------------------------------------------------------------------
// EOF
// ----------------------------------------------------------------------------