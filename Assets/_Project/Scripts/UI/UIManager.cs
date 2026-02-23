/*
 * ----------------------------------------------------------------------------
 * Project: AI Capitalist
 * Author:  Bussuf Senior Dev
 * Date:    2023-10-29
 * ----------------------------------------------------------------------------
 * Description:
 * Generates the UI Prefabs for each Tier and links them to the Logic Controllers.
 * Manages the Singleton ManagerPopupUI and dynamically spawns OfflinePopupUI.
 * ----------------------------------------------------------------------------
 * Change Log:
 * 2023-10-28 - Bussuf Senior Dev - Initial implementation.
 * 2023-10-29 - Bussuf Senior Dev - Added SceneLoaded event to dynamically find container.
 * 2023-10-29 - Bussuf Senior Dev - Added Manager Popup instantiation.
 * 2023-10-29 - Bussuf Senior Dev - Updated OpenManagerPopup to pass VisualData.
 * 2023-10-29 - Bussuf Senior Dev - Added instantiation of OfflinePopupUI on load.
 * 2026-02-23 - Bussuf Senior Dev - Added Next tier unlock animation with DOTWeen.
 * 2023-10-30 - Bussuf Senior Dev - Fixed EOC (End of Content) bug by safely 
 * checking if next tier exists before spawning unlock panel.
 * ----------------------------------------------------------------------------
 */

using AI_Capitalist.Core;
using AI_Capitalist.Data;
using AI_Capitalist.Economy;
using AI_Capitalist.Gameplay;
using BussufGames.Core;
using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AI_Capitalist.UI
{
	public class UIManager : MonoBehaviour, IService
	{
		[Header("Prefabs")]
		[SerializeField] private GameObject tierUIPrefab;
		[SerializeField] private GameObject managerPopupPrefab;
		[SerializeField] private GameObject offlinePopupPrefab;
		[SerializeField] private GameObject unlockPanelPrefab;

		private Transform _uiContainer;
		private ManagerPopupUI _activeManagerPopup;
		private UnlockPanelUI _currentUnlockPanel;

		private TierManager _tierManager;
		private EconomyManager _economyManager;
		private Dictionary<int, TierVisualData> _visualDataDict = new Dictionary<int, TierVisualData>();

		private void Awake()
		{
			if (CoreManager.Instance != null)
				CoreManager.Instance.RegisterService<UIManager>(this);
		}

		public void Initialize()
		{
			_tierManager = CoreManager.Instance.GetService<TierManager>();
			_economyManager = CoreManager.Instance.GetService<EconomyManager>();

			if (_tierManager == null || tierUIPrefab == null || unlockPanelPrefab == null)
			{
				this.LogError("UIManager missing critical references!");
				return;
			}

			LoadVisualData();

			SceneManager.sceneLoaded += OnSceneLoaded;
			_tierManager.OnTierUnlocked += HandleNewTierUnlocked;
		}

		private void OnDestroy()
		{
			SceneManager.sceneLoaded -= OnSceneLoaded;
			if (_tierManager != null) _tierManager.OnTierUnlocked -= HandleNewTierUnlocked;
		}

		private void LoadVisualData()
		{
			TierVisualData[] loadedData = Resources.LoadAll<TierVisualData>("Visuals");
			foreach (var data in loadedData)
				_visualDataDict[data.TierID] = data;
		}

		private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
		{
			if (scene.buildIndex == 1) SpawnUI();
		}

		private void SpawnUI()
		{
			GameObject containerObj = GameObject.Find("UI_Container");
			if (containerObj != null) _uiContainer = containerObj.transform;
			else return;

			foreach (var controller in _tierManager.ActiveTiers)
			{
				SpawnSingleTierUI(controller, false);
			}

			SpawnUnlockPanelForNextTier();

			Canvas mainCanvas = Object.FindAnyObjectByType<Canvas>();
			if (mainCanvas != null)
			{
				GameObject popupObj = Instantiate(managerPopupPrefab, mainCanvas.transform);
				popupObj.name = "Manager_Popup_UI";
				popupObj.transform.SetAsLastSibling();
				_activeManagerPopup = popupObj.GetComponent<ManagerPopupUI>();
				_activeManagerPopup.gameObject.SetActive(false);

				var offlineMgr = CoreManager.Instance.GetService<OfflineProgressManager>();
				if (offlineMgr != null && offlineMgr.HasOfflineProgress)
				{
					GameObject offlineObj = Instantiate(offlinePopupPrefab, mainCanvas.transform);
					offlineObj.transform.SetAsLastSibling();
					offlineObj.GetComponent<OfflinePopupUI>()?.OpenPopup(offlineMgr.PendingTimeAway, offlineMgr.PendingOfflineEarnings);
				}
			}
		}

		private void SpawnSingleTierUI(TierController controller, bool playPopAnimation)
		{
			GameObject uiObj = Instantiate(tierUIPrefab, _uiContainer);
			uiObj.name = $"UI_{controller.gameObject.name}";
			uiObj.transform.SetAsLastSibling();

			TierUIView uiView = uiObj.GetComponent<TierUIView>();
			if (uiView != null)
			{
				_visualDataDict.TryGetValue(controller.StaticData.TierID, out TierVisualData visualData);
				uiView.Initialize(controller, visualData);
			}

			if (playPopAnimation)
			{
				uiObj.transform.localScale = Vector3.zero;
				uiObj.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack);
			}
		}

		private void SpawnUnlockPanelForNextTier()
		{
			var dataManager = CoreManager.Instance.GetService<DataManager>();
			if (dataManager == null || _economyManager == null) return;

			int nextTierID = dataManager.GameData.HighestUnlockedTier + 1;

			// FIX: Use TryGetTierConfig to gracefully handle End Of Content (EOC)
			if (_economyManager.TryGetTierConfig(nextTierID, out TierStaticData nextConfig))
			{
				GameObject unlockObj = Instantiate(unlockPanelPrefab, _uiContainer);
				unlockObj.name = $"UnlockPanel_Tier_{nextTierID}";
				unlockObj.transform.SetAsLastSibling();

				_currentUnlockPanel = unlockObj.GetComponent<UnlockPanelUI>();
				_currentUnlockPanel.Initialize(nextConfig);
			}
			else
			{
				this.Log($"Tier {nextTierID} does not exist in MasterTable. End of content reached.");
			}
		}

		private void HandleNewTierUnlocked(TierController newController)
		{
			if (_currentUnlockPanel != null)
			{
				_currentUnlockPanel.transform.DOScale(0f, 0.3f).SetEase(Ease.InBack).OnComplete(() =>
				{
					Destroy(_currentUnlockPanel.gameObject);
					_currentUnlockPanel = null;

					SpawnSingleTierUI(newController, true);

					// Will gracefully do nothing if it's the last tier
					SpawnUnlockPanelForNextTier();
				});
			}
		}

		public void OpenManagerPopup(TierController controller)
		{
			if (_activeManagerPopup != null)
			{
				_visualDataDict.TryGetValue(controller.StaticData.TierID, out TierVisualData visualData);
				_activeManagerPopup.OpenPopup(controller, visualData);
			}
		}
	}
}

// ----------------------------------------------------------------------------
// EOF
// ----------------------------------------------------------------------------