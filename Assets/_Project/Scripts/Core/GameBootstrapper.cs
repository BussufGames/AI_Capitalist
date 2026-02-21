using UnityEngine;
namespace AI_Capitalist.Core
{
	public class GameBootstrapper : MonoBehaviour
	{
		private void Start()
		{
			// Give managers a frame to register, then boot the game.
			CoreManager.Instance.StartGameSequence();
		}
	}
}