/*
 * ----------------------------------------------------------------------------
 * Project: AI Capitalist
 * Author:  Bussuf Senior Dev
 * Date:    2023-10-28
 * ----------------------------------------------------------------------------
 * Description:
 * A purely visual data container for a Tier. 
 * Maps the TierID to its graphical representation (Sprite, Display Name).
 * ----------------------------------------------------------------------------
 * Change Log:
 * 2023-10-28 - Bussuf Senior Dev - Initial implementation.
 * 2023-10-29 - Bussuf Senior Dev - Added state sprites for the action button.
 * ----------------------------------------------------------------------------
 */

using UnityEngine;

namespace AI_Capitalist.UI
{
	[CreateAssetMenu(fileName = "TierVisualData_", menuName = "AI Capitalist/Tier Visual Data")]
	public class TierVisualData : ScriptableObject
	{
		[Tooltip("Must match the TierID in the JSON Master Table.")]
		public int TierID;

		[Tooltip("The nice display name for the UI.")]
		public string DisplayName;

		[Tooltip("The icon representing the business.")]
		public Sprite TierIcon;

		[Header("State Button Sprites")]
		public Sprite StateManual;
		public Sprite StateHumanWorking;
		public Sprite StateHumanStrike;
		public Sprite StateAIRunning;
	}
}

// ----------------------------------------------------------------------------
// EOF
// ----------------------------------------------------------------------------