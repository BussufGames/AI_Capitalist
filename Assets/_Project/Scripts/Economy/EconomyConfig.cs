/*
 * ----------------------------------------------------------------------------
 * Project: AI Capitalist
 * Author:  Bussuf Senior Dev
 * Date:    2023-10-28
 * ----------------------------------------------------------------------------
 * Change Log:
 * 2023-10-28 - Bussuf Senior Dev - Initial implementation.
 * 2023-10-30 - Bussuf Senior Dev - Added Unlock_Cost for dynamic progression.
 * ----------------------------------------------------------------------------
 */

using System;
using System.Collections.Generic;

namespace AI_Capitalist.Economy
{
	[Serializable]
	public class TierStaticData
	{
		public int TierID;
		public string BusinessName;
		public string Unlock_Cost; // NEW: The cost to reveal this tier
		public string Base_Cost;
		public double Growth_Factor;
		public string Base_Rev;
		public float Cycle_Time;
		public string Human_Hire_Cost;
		public string Base_Human_Salary_Per_Cycle;
		public string AI_Hire_Cost;
	}

	[Serializable]
	public class MasterEconomyTable
	{
		public List<TierStaticData> Tiers;
	}
}

// ----------------------------------------------------------------------------
// EOF
// ----------------------------------------------------------------------------