/*
 * ----------------------------------------------------------------------------
 * Project: AI Capitalist
 * Author:  Bussuf Senior Dev
 * Date:    2023-10-28
 * ----------------------------------------------------------------------------
 * Description:
 * POCO classes representing the Master Economy Table (Google Sheets).
 * Designed to be deserialized from a remote or local JSON file.
 * ----------------------------------------------------------------------------
 * Change Log:
 * 2023-10-28 - Bussuf Senior Dev - Initial implementation based on GDD Appendix A.
 * ----------------------------------------------------------------------------
 */

using System.Collections.Generic;

namespace AI_Capitalist.Economy
{
	[System.Serializable]
	public class TierStaticData
	{
		public int TierID;
		public string BusinessName;
		public string Base_Cost;
		public double Growth_Factor;
		public string Base_Rev;
		public float Cycle_Time;
		public string Human_Hire_Cost;
		public string Base_Human_Salary_Per_Cycle;
		public string AI_Hire_Cost;
	}

	[System.Serializable]
	public class MasterEconomyTable
	{
		public List<TierStaticData> Tiers;
	}
}

// ----------------------------------------------------------------------------
// EOF
// ----------------------------------------------------------------------------