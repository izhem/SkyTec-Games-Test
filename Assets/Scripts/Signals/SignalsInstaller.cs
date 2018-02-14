using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Zenject;

namespace SkyTecGamesTest
{
	public class PlayerEnteredMatch : Signal<PlayerEnteredMatch, Player> { }
	public class PlayerLeavedMatch : Signal<PlayerLeavedMatch, Player> { }
	public class PlayerScoreChanged : Signal<PlayerScoreChanged, Player, int> { }
	public class LocalPlayerInstalled : Signal<LocalPlayerInstalled, Player> { }
	public class PlayerNameChanged : Signal<PlayerNameChanged, Player, string> { }
	public class GameSpeedChanged : Signal<GameSpeedChanged, float> { }

	public class ScoreObjectSpawned : Signal<ScoreObjectSpawned, int, Vector3> { }

	public class GamePaused : Signal<GamePaused> { }
	public class GameResumed : Signal<GameResumed> { }
	public class GamePausedFromServer : Signal<GamePausedFromServer> { }
	public class GameResumedFromServer : Signal<GameResumedFromServer> { }

	public class PlayerGunsPowerChanged : Signal<PlayerGunsPowerChanged, Player, int> { }
	public class PlayerFireRateModificatorChanged : Signal<PlayerFireRateModificatorChanged, Player, float> { }
	public class PlayerGunsCountChanged : Signal<PlayerGunsCountChanged, Player, int> { }
	public class PlayerHasShieldChanged : Signal<PlayerHasShieldChanged, Player, bool> { }
	public class PlayerScoreModificatorChanged : Signal<PlayerScoreModificatorChanged, Player, float> { }


	public class SignalsInstaller : MonoInstaller<SignalsInstaller>
	{
		public override void InstallBindings()
		{
			Container.DeclareSignal<PlayerEnteredMatch>();
			Container.DeclareSignal<PlayerLeavedMatch>();
			Container.DeclareSignal<PlayerScoreChanged>();
			Container.DeclareSignal<LocalPlayerInstalled>();
			Container.DeclareSignal<PlayerNameChanged>();
			Container.DeclareSignal<GameSpeedChanged>();

			Container.DeclareSignal<ScoreObjectSpawned>();

			Container.DeclareSignal<GamePaused>();
			Container.DeclareSignal<GameResumed>();
			Container.DeclareSignal<GamePausedFromServer>();
			Container.DeclareSignal<GameResumedFromServer>();

			Container.DeclareSignal<PlayerGunsPowerChanged>();
			Container.DeclareSignal<PlayerFireRateModificatorChanged>();
			Container.DeclareSignal<PlayerGunsCountChanged>();
			Container.DeclareSignal<PlayerHasShieldChanged>();
			Container.DeclareSignal<PlayerScoreModificatorChanged>();
		}
	}
}
