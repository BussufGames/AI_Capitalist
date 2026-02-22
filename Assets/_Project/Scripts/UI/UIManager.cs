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
 * ----------------------------------------------------------------------------
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using AI_Capitalist.Core;
using AI_Capitalist.Gameplay;
using AI_Capitalist.Economy;
using BussufGames.Core;

namespace AI_Capitalist.UI
{
	public class UIManager : MonoBehaviour, IService
	{
		[Header("Prefabs")]
		[Tooltip("The visual UI Prefab with TierUIView script.")]
		[SerializeField] private GameObject tierUIPrefab;

		[Tooltip("The Manager Popup Prefab.")]
		[SerializeField] private GameObject managerPopupPrefab;

		[Tooltip("The Welcome Back Offline Progress Popup Prefab.")]
		[SerializeField] private GameObject offlinePopupPrefab;

		private Transform _uiContainer;
		private ManagerPopupUI _activeManagerPopup;
		private TierManager _tierManager;
		private Dictionary<int, TierVisualData> _visualDataDict = new Dictionary<int, TierVisualData>();

		private void Awake()
		{
			if (CoreManager.Instance != null)
			{
				CoreManager.Instance.RegisterService<UIManager>(this);
			}
		}

		public void Initialize()
		{
			_tierManager = CoreManager.Instance.GetService<TierManager>();
			if (_tierManager == null || tierUIPrefab == null || managerPopupPrefab == null || offlinePopupPrefab == null)
			{
				this.LogError("UIManager missing critical references (Check Prefabs in Inspector)!");
				return;
			}

			LoadVisualData();
			SceneManager.sceneLoaded += OnSceneLoaded;
		}

		private void OnDestroy()
		{
			SceneManager.sceneLoaded -= OnSceneLoaded;
		}

		private void LoadVisualData()
		{
			TierVisualData[] loadedData = Resources.LoadAll<TierVisualData>("Visuals");
			foreach (var data in loadedData)
			{
				_visualDataDict[data.TierID] = data;
			}
			this.Log($"Loaded {_visualDataDict.Count} TierVisualData objects.");
		}

		private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
		{
			if (scene.buildIndex == 1)
			{
				SpawnUI();
			}
		}

		private void SpawnUI()
		{
			GameObject containerObj = GameObject.Find("UI_Container");
			if (containerObj != null)
			{
				_uiContainer = containerObj.transform;
			}
			else return;

			// 1. Spawn Tier Rows
			foreach (var controller in _tierManager.ActiveTiers)
			{
				GameObject uiObj = Instantiate(tierUIPrefab, _uiContainer);
				uiObj.name = $"UI_{controller.gameObject.name}";

				TierUIView uiView = uiObj.GetComponent<TierUIView>();
				if (uiView != null)
				{
					_visualDataDict.TryGetValue(controller.StaticData.TierID, out TierVisualData visualData);
					uiView.Initialize(controller, visualData);
				}
			}

			Canvas mainCanvas = Object.FindAnyObjectByType<Canvas>();
			if (mainCanvas != null)
			{
				// 2. Spawn Global Manager Popup
				GameObject popupObj = Instantiate(managerPopupPrefab, mainCanvas.transform);
				popupObj.name = "Manager_Popup_UI";
				popupObj.transform.SetAsLastSibling();

				_activeManagerPopup = popupObj.GetComponent<ManagerPopupUI>();
				_activeManagerPopup.gameObject.SetActive(false);

				// 3. Spawn Offline Progress Popup (if applicable)
				var offlineMgr = CoreManager.Instance.GetService<OfflineProgressManager>();
				if (offlineMgr != null && offlineMgr.HasOfflineProgress)
				{
					GameObject offlineObj = Instantiate(offlinePopupPrefab, mainCanvas.transform);
					offlineObj.name = "Offline_Popup_UI";
					offlineObj.transform.SetAsLastSibling(); // Ensure it's on top of everything

					OfflinePopupUI offlineUI = offlineObj.GetComponent<OfflinePopupUI>();
					if (offlineUI != null)
					{
						offlineUI.OpenPopup(offlineMgr.PendingTimeAway, offlineMgr.PendingOfflineEarnings);
					}
				}
			}

			this.LogSuccess("All UIs spawned successfully.");
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