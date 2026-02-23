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
 * 2023-10-29 - Bussuf Senior Dev - Added Task.Delay to fix Scene Load collision.
 * 2023-10-31 - Bussuf Senior Dev - Expanded cheat buttons (+1K, +1B, +1T, 0 Funds).
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
		[Header("Money Cheats")]
		[SerializeField] private Button add1KButton;
		[SerializeField] private Button add1MButton; // Keep for existing inspector links if any
		[SerializeField] private Button add1BButton;
		[SerializeField] private Button add1TButton;
		[SerializeField] private Button zeroFundsButton;

		[Header("Save Management")]
		[SerializeField] private Button resetSaveButton;

		private void Start()
		{
			if (add1KButton != null) add1KButton.onClick.AddListener(() => GiveMoneyCheat(1000));
			if (add1MButton != null) add1MButton.onClick.AddListener(() => GiveMoneyCheat(1000000));
			if (add1BButton != null) add1BButton.onClick.AddListener(() => GiveMoneyCheat(1000000000));
			if (add1TButton != null) add1TButton.onClick.AddListener(() => GiveMoneyCheat(1000000000000));

			if (zeroFundsButton != null) zeroFundsButton.onClick.AddListener(ZeroFundsCheat);
			if (resetSaveButton != null) resetSaveButton.onClick.AddListener(ResetSaveCheat);
		}

		private void GiveMoneyCheat(double amount)
		{
			var economy = CoreManager.Instance.GetService<EconomyManager>();
			if (economy != null)
			{
				economy.AddIncome(new BigDouble(amount));
				this.LogWarning($"CHEAT USED: +${amount:N0} added to balance.");
			}
		}

		private void ZeroFundsCheat()
		{
			var economy = CoreManager.Instance.GetService<EconomyManager>();
			if (economy != null)
			{
				economy.SetBalance(BigDouble.Zero);
				this.LogWarning("CHEAT USED: Balance reset to $0.");
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

			// 4. Wait for Unity to finish destroying the CoreManager at the end of the frame
			await System.Threading.Tasks.Task.Delay(150);

			// 5. Reload Bootstrap Scene
			SceneManager.LoadScene(0);
		}
	}
}

#endif // UNITY_EDITOR || DEVELOPMENT_BUILD

// ----------------------------------------------------------------------------
// EOF
// ----------------------------------------------------------------------------