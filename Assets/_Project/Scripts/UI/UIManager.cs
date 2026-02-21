/*
 * ----------------------------------------------------------------------------
 * Project: AI Capitalist
 * Author:  Bussuf Senior Dev
 * Date:    2023-10-28
 * ----------------------------------------------------------------------------
 * Description:
 * Generates the UI Prefabs for each Tier and links them to the Logic Controllers.
 * Waits for the Main Scene to load before searching for the UI_Container.
 * ----------------------------------------------------------------------------
 * Change Log:
 * 2023-10-28 - Bussuf Senior Dev - Initial implementation.
 * 2023-10-29 - Bussuf Senior Dev - Added SceneLoaded event to dynamically find 
 * UI_Container after transitioning to Main Scene.
 * ----------------------------------------------------------------------------
 */

using AI_Capitalist.Core;
using AI_Capitalist.Gameplay;
using BussufGames.Core;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AI_Capitalist.UI
{
	public class UIManager : MonoBehaviour, IService
	{
		[Header("Prefabs")]
		[Tooltip("The visual UI Prefab with TierUIView script.")]
		[SerializeField] private GameObject tierUIPrefab;

		private Transform _uiContainer;
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
			this.Log("Initializing UIManager...");

			_tierManager = CoreManager.Instance.GetService<TierManager>();
			if (_tierManager == null)
			{
				this.LogError("TierManager not found. UI cannot initialize.");
				return;
			}

			if (tierUIPrefab == null)
			{
				this.LogError("TierUIPrefab is missing in UIManager!");
				return;
			}

			LoadVisualData();

			// Subscribe to the scene loaded event so we spawn UI ONLY when the canvas actually exists
			SceneManager.sceneLoaded += OnSceneLoaded;
			this.Log("UIManager ready. Waiting for Main Scene to load...");
		}

		private void OnDestroy()
		{
			SceneManager.sceneLoaded -= OnSceneLoaded;
		}

		private void LoadVisualData()
		{
			// Load all ScriptableObjects from a Resources folder named "Visuals"
			TierVisualData[] loadedData = Resources.LoadAll<TierVisualData>("Visuals");
			foreach (var data in loadedData)
			{
				_visualDataDict[data.TierID] = data;
			}
			this.Log($"Loaded {_visualDataDict.Count} TierVisualData objects.");
		}

		private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
		{
			// Assuming Scene 1 is the Main Game Scene
			if (scene.buildIndex == 1)
			{
				this.Log("Main Scene loaded. Spawning UI elements...");
				SpawnUI();
			}
		}

		private void SpawnUI()
		{
			// Dynamically find the container in the newly loaded scene
			GameObject containerObj = GameObject.Find("UI_Container");
			if (containerObj != null)
			{
				_uiContainer = containerObj.transform;
			}
			else
			{
				this.LogError("CRITICAL: Could not find 'UI_Container' in the scene! Make sure the object is named exactly 'UI_Container'.");
				return;
			}

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
				else
				{
					this.LogError("TierUIPrefab is missing the TierUIView script!");
				}
			}

			this.LogSuccess("All Tier UIs spawned and connected to logic.");
		}
	}
}

// ----------------------------------------------------------------------------
// EOF
// ----------------------------------------------------------------------------