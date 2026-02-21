/*
 * ----------------------------------------------------------------------------
 * Project: AI Capitalist
 * Author:  Bussuf Senior Dev
 * Date:    2023-10-28
 * ----------------------------------------------------------------------------
 * Description:
 * Base interface for all core services/managers in the game.
 * Ensures every manager has an Initialize method for controlled startup.
 * ----------------------------------------------------------------------------
 * Change Log:
 * 2023-10-28 - Bussuf Senior Dev - Initial implementation.
 * ----------------------------------------------------------------------------
 */

namespace AI_Capitalist.Core
{
	public interface IService
	{
		/// <summary>
		/// Called by the CoreManager during the Bootstrap phase.
		/// </summary>
		void Initialize();
	}
}

// ----------------------------------------------------------------------------
// EOF
// ----------------------------------------------------------------------------