/*
 * ----------------------------------------------------------------------------
 * Project: AI Capitalist
 * Author:  Bussuf Senior Dev
 * Date:    2023-10-29
 * ----------------------------------------------------------------------------
 * Description:
 * Development tools for testing the game loop.
 * Properly wipes local/cloud saves and safely reboots the application.
 * ----------------------------------------------------------------------------
 * Change Log:
 * 2023-10-29 - Bussuf Senior Dev - Initial implementation.
 * 2023-10-29 - Bussuf Senior Dev - Fixed ResetSave logic to destroy CoreManager safely.
 * 2023-10-29 - Bussuf Senior Dev - Reset save also reset save on the cloud now.
 * 2023-10-29 - Bussuf Senior Dev - Added Task.Delay to fix Scene Load collision.
 * ----------------------------------------------------------------------------
 */

#if UNITY_EDITOR || DEVELOPMENT_BUILD

using AI_Capitalist.Core;
using AI_Capitalist.Economy;
using AI_Capitalist.Services;
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

		private void Start()
		{

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
				economy.AddIncome(new BigDouble(1000000));
				this.LogWarning("CHEAT USED: +$1,000,000 added to balance.");
			}
		}

		private async void ResetSaveCheat()
		{
			this.LogWarning("CHEAT USED: Erasing local and cloud save data...");

			// 1. Wipe Local Save
			PlayerPrefs.DeleteAll();
			PlayerPrefs.Save();

			// 2. Wipe Cloud Save (Sends empty JSON)
			var ugs = CoreManager.Instance.GetService<UGSManager>();
			if (ugs != null && ugs.IsAuthenticated)
			{
				await ugs.SaveCloudDataAsync("ai_cap_save_v1", "{}");
			}

			// 3. Destroy persistent objects to start totally fresh
			if (CoreManager.Instance != null)
			{
				Destroy(CoreManager.Instance.gameObject);
			}

			// CRITICAL FIX: Wait for Unity to finish destroying the CoreManager at the end of the frame
			// before we load Scene 0. This prevents the "Duplicate CoreManager" collision bug.
			await System.Threading.Tasks.Task.Delay(150);

			// 4. Reload Bootstrap Scene
			SceneManager.LoadScene(0);
		}
	}
}

#endif // UNITY_EDITOR || DEVELOPMENT_BUILD

// ----------------------------------------------------------------------------
// EOF
// ----------------------------------------------------------------------------