/*
 * ----------------------------------------------------------------------------
 * Project: AI Capitalist
 * Author:  Bussuf Senior Dev
 * Date:    2023-10-29
 * ----------------------------------------------------------------------------
 * Description:
 * Development tools for testing the game loop.
 * Wrapped in preprocessor directives so it DOES NOT compile in Release builds.
 * ----------------------------------------------------------------------------
 * Change Log:
 * 2023-10-29 - Bussuf Senior Dev - Initial implementation.
 * 2023-10-29 - Bussuf Senior Dev - Fixed ResetSave logic to destroy CoreManager safely.
 * ----------------------------------------------------------------------------
 */

#if UNITY_EDITOR || DEVELOPMENT_BUILD

using AI_Capitalist.Core;
using AI_Capitalist.Data;
using AI_Capitalist.Economy;
using BreakInfinity;
using BussufGames.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AI_Capitalist.DevTools
{
	public class DevCheatMenu : MonoBehaviour
	{
		[Header("Cheat Buttons")]
		[SerializeField] private Button addMoneyButton;
		[SerializeField] private Button resetSaveButton;

		[SerializeField] private BigDouble startingCash = 50;

		private void Start()
		{
			CoreManager.Instance.GetService<EconomyManager>().AddIncome(startingCash);
			if (addMoneyButton != null)
				addMoneyButton.onClick.AddListener(GiveMoneyCheat);

			if (resetSaveButton != null)
				resetSaveButton.onClick.AddListener(ResetSaveCheat);
		}

		private void GiveMoneyCheat()
		{
			var economy = CoreManager.Instance.GetService<EconomyManager>();
			if (economy != null)
			{
				// Give 1 Million Dollars
				economy.AddIncome(new BigDouble(1000000));
				this.LogWarning("CHEAT USED: +$1,000,000 added to balance.");
			}
		}

		private void ResetSaveCheat()
		{
			this.LogWarning("CHEAT USED: Erasing local save data and rebooting...");

			// Delete Unity PlayerPrefs (Local Save)
			PlayerPrefs.DeleteAll();
			PlayerPrefs.Save();

			// Destroy the persistent CoreManager so it restarts fresh in Scene 0
			if (CoreManager.Instance != null)
			{
				Destroy(CoreManager.Instance.gameObject);
			}

			// Reload Bootstrap Scene
			SceneManager.LoadScene(0);
		}
	}
}

#endif // UNITY_EDITOR || DEVELOPMENT_BUILD

// ----------------------------------------------------------------------------
// EOF
// ----------------------------------------------------------------------------