/*
 * ----------------------------------------------------------------------------
 * Project: AI Capitalist
 * Author:  Bussuf Senior Dev
 * Date:    2026-02-28
 * ----------------------------------------------------------------------------
 * Description:
 * ScriptableObject holding the unique visual data for a specific tier.
 * Cleaned up: Global manager icons moved to UIManager.
 * ----------------------------------------------------------------------------
 */

using UnityEngine;

namespace AI_Capitalist.Data
{
	[CreateAssetMenu(fileName = "TierVisualData_", menuName = "AI Capitalist/Tier Visual Data")]
	public class TierVisualData : ScriptableObject
	{
		[Header("Basic Info")]
		public int TierID;
		public string DisplayName;

		[Header("Unique Icons")]
		public Sprite TierIcon;
	}
}

// ----------------------------------------------------------------------------
// EOF
// ----------------------------------------------------------------------------