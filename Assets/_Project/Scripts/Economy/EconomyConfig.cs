/*
 * ----------------------------------------------------------------------------
 * Project: AI Capitalist
 * Author:  Bussuf Senior Dev
 * Date:    2026-02-28
 * ----------------------------------------------------------------------------
 * Description:
 * Defines the static data structures loaded from the MasterTable.json file.
 * ----------------------------------------------------------------------------
 * Change Log:
 * 2023-10-28 - Bussuf Senior Dev - Initial implementation.
 * 2023-10-30 - Bussuf Senior Dev - Added Unlock_Cost for dynamic progression.
 * 2026-02-28 - Bussuf Senior Dev - Added UpgradeStaticData and Upgrades list to MasterEconomyTable.
 * ----------------------------------------------------------------------------
 */

using System;
using System.Collections.Generic;

namespace AI_Capitalist.Economy
{
	#region Tiers Config
	[Serializable]
	public class TierStaticData
	{
		public int TierID;
		public string BusinessName;
		public string Unlock_Cost;
		public string Base_Cost;
		public double Growth_Factor;
		public string Base_Rev;
		public float Cycle_Time;
		public string Human_Hire_Cost;
		public string Base_Human_Salary_Per_Cycle;
		public string AI_Hire_Cost;
	}
	#endregion

	#region Upgrades Config
	[Serializable]
	public class UpgradeStaticData
	{
		public string UpgradeID;      // e.g., "upg_t1_speed_1"
		public string Name;           // e.g., "Faster Typing"
		public string Description;    // e.g., "Tier 1 Speed x2"
		public string Cost;           // e.g., "150"
		public int TargetTierID;      // 0 means GLOBAL (affects all tiers), otherwise specific TierID
		public float Multiplier;      // e.g., 2.0
		public string UpgradeType;    // "Revenue" or "Speed"
	}
	#endregion

	#region Master Table Wrapper
	[Serializable]
	public class MasterEconomyTable
	{
		public List<TierStaticData> Tiers;
		public List<UpgradeStaticData> Upgrades; // NEW: Added Upgrades list mapping
	}
	#endregion
}

// ----------------------------------------------------------------------------
// EOF
// ----------------------------------------------------------------------------