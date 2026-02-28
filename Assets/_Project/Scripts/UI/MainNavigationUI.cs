/*
 * ----------------------------------------------------------------------------
 * Project: AI Capitalist
 * Author:  Bussuf Senior Dev
 * Date:    2026-02-28
 * ----------------------------------------------------------------------------
 * Description:
 * Manages the Hamburger Menu and navigates between full-screen UI pages.
 * ----------------------------------------------------------------------------
 */
using UnityEngine;
using UnityEngine.UI;

namespace AI_Capitalist.UI
{
	public class MainNavigationUI : MonoBehaviour
	{
		[Header("Panels")]
		[Tooltip("הפאנל הקטן שנפתח מההמבורגר ומכיל את כפתורי הניווט")]
		[SerializeField] private GameObject navigationMenuPanel;
		[Tooltip("דפי המסך המלא")]
		[SerializeField] private GameObject upgradesPage;
		[SerializeField] private GameObject prestigePage;

		[Header("Hamburger Controls")]
		[SerializeField] private Button btnHamburgerToggle; // הכפתור בפוטר

		[Header("Navigation Menu Buttons (Inside Menu)")]
		[SerializeField] private Button btnGoToUpgrades;
		[SerializeField] private Button btnGoToPrestige;

		[Header("Full Page Close Buttons")]
		[SerializeField] private Button btnCloseUpgrades;
		[SerializeField] private Button btnClosePrestige;

		private void Awake()
		{
			// Hamburger Toggle
			if (btnHamburgerToggle != null)
				btnHamburgerToggle.onClick.AddListener(ToggleNavigationMenu);

			// Menu Buttons -> Open Pages
			if (btnGoToUpgrades != null) btnGoToUpgrades.onClick.AddListener(OpenUpgradesPage);
			if (btnGoToPrestige != null) btnGoToPrestige.onClick.AddListener(OpenPrestigePage);

			// Page Close Buttons -> Close themselves
			if (btnCloseUpgrades != null) btnCloseUpgrades.onClick.AddListener(() => upgradesPage.SetActive(false));
			if (btnClosePrestige != null) btnClosePrestige.onClick.AddListener(() => prestigePage.SetActive(false));
		}

		private void Start()
		{
			// ודא שהכל סגור בהתחלה
			if (navigationMenuPanel != null) navigationMenuPanel.SetActive(false);
			if (upgradesPage != null) upgradesPage.SetActive(false);
			if (prestigePage != null) prestigePage.SetActive(false);
		}

		private void ToggleNavigationMenu()
		{
			if (navigationMenuPanel != null)
			{
				bool isCurrentlyOpen = navigationMenuPanel.activeSelf;
				navigationMenuPanel.SetActive(!isCurrentlyOpen); // פותח אם סגור, סוגר אם פתוח
			}
		}

		public void OpenUpgradesPage()
		{
			if (navigationMenuPanel != null) navigationMenuPanel.SetActive(false); // סגור את התפריט
			upgradesPage.SetActive(true);
			prestigePage.SetActive(false);
		}

		public void OpenPrestigePage()
		{
			if (navigationMenuPanel != null) navigationMenuPanel.SetActive(false); // סגור את התפריט
			upgradesPage.SetActive(false);
			prestigePage.SetActive(true);
		}
	}
}
// ----------------------------------------------------------------------------
// EOF
// ----------------------------------------------------------------------------