/*
 * ----------------------------------------------------------------------------
 * Project: AI Capitalist
 * Author:  Bussuf Senior Dev
 * Date:    2023-10-28
 * ----------------------------------------------------------------------------
 * Description:
 * The Service Locator and main entry point of the application.
 * Resides in the Bootstrap scene, registers all IServices, initializes them, 
 * and transitions to the Main scene.
 * ----------------------------------------------------------------------------
 * Change Log:
 * 2023-10-28 - Bussuf Senior Dev - Initial implementation.
 * ----------------------------------------------------------------------------
 */

using BussufGames.Core;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AI_Capitalist.Core
{
	// Ensure this script runs before almost anything else
	[DefaultExecutionOrder(-100)]
	public class CoreManager : MonoBehaviour
	{
		public static CoreManager Instance { get; private set; }

		// The "Phonebook" of all our managers
		private readonly Dictionary<Type, IService> _services = new Dictionary<Type, IService>();

		private void Awake()
		{
			if (Instance != null && Instance != this)
			{
				this.LogWarning("Duplicate CoreManager detected. Destroying...");
				Destroy(gameObject);
				return;
			}

			Instance = this;
			DontDestroyOnLoad(gameObject);
			this.Log("CoreManager awoken and set to DontDestroyOnLoad.");
		}

		/// <summary>
		/// Registers a service into the locator.
		/// </summary>
		public void RegisterService<T>(T service) where T : IService
		{
			var type = typeof(T);
			if (!_services.ContainsKey(type))
			{
				_services.Add(type, service);
				this.Log($"Service registered: {type.Name}");
			}
			else
			{
				this.LogError($"Service of type {type.Name} is already registered!");
			}
		}

		/// <summary>
		/// Retrieves a service from the locator.
		/// </summary>
		public T GetService<T>() where T : class, IService
		{
			var type = typeof(T);
			if (_services.TryGetValue(type, out var service))
			{
				return service as T;
			}

			this.LogError($"Requested Service of type {type.Name} was not found! Did you register it?");
			return null;
		}

		/// <summary>
		/// Bootstraps the game. Called via a UI Button or a Bootstrapper script 
		/// once all essential managers are loaded in the Init scene.
		/// </summary>
		public void StartGameSequence()
		{
			this.Log("Starting Game Sequence...");

			// 1. Initialize all registered services
			foreach (var service in _services.Values)
			{
				service.Initialize();
			}

			this.LogSuccess("All services initialized successfully.");

			// 2. Load the Main Game Scene (Assuming Scene 1 is Main)
			this.Log("Loading Main Scene...");
			SceneManager.LoadScene(1);
		}
	}
}

// ----------------------------------------------------------------------------
// EOF
// ----------------------------------------------------------------------------