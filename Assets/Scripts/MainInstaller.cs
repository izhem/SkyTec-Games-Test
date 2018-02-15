using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace SkyTecGamesTest
{
	public class MainInstaller : MonoInstaller<MainInstaller>
	{
		[Header("Components")]
		[SerializeField] GameUI _gameUI;
		[SerializeField] Gameplay _gameplay;
		[SerializeField] Background _background;

		[Header("Asteroids")]
		[SerializeField] GameObject _asteroidPrefab;
		[SerializeField] Transform _asteroidsParent;
		[SerializeField] int _initialAsteroidsPoolSize = 10;
		[SerializeField] float _wayDelta = 0.05f;

		[Header("Bullets")]
		[SerializeField] GameObject _bulletPrefab;
		[SerializeField] Transform _bulletsParent;
		[SerializeField] int _initialBulletsPoolSize = 20;

		[Header("Bonuses")]
		[SerializeField] GameObject _bonusPrefab;
		[SerializeField] Transform _bonusesParent;
		[SerializeField] int _initialBonusesPoolSize = 20;

		[Header("Effects")]
		[SerializeField] GameObject _bulletEffectPrefab;
		[SerializeField] GameObject _destroyEffectPrefab;
		[SerializeField] Transform _effectsParent;
		[SerializeField] int _initialEffectsPoolSize = 20;

		public override void InstallBindings()
		{
			Container.UnbindAll();

			//Bind Player factory
			Container.BindFactory<GameObject, int, Player, Player.PlayerFactory>()
				.FromFactory<Player.PlayerCustomFactory>();

			//Bind SpaceShip factory
			Container.BindFactory<GameObject, Player, SpaceShip, SpaceShip.SpaceShipFactory>()
				.FromFactory<SpaceShip.SpaceShipCustomFactory>();

			//Bind Asteroid pool
			Container.BindMemoryPool<Asteroid, Asteroid.AsteroidsPool, Asteroid.AsteroidsPool>()
				.WithInitialSize(_initialAsteroidsPoolSize)
				.ExpandByDoubling()
				.FromComponentInNewPrefab(_asteroidPrefab);

			//Bind wayDelta for asteroids movement through the AsteroidCurve
			Container.Bind<float>().WithId("WayDelta").FromInstance(_wayDelta).AsSingle();

			//Bind parent for Asteroids
			Container.Bind<Transform>().WithId("AsteroidsParent").FromInstance(_asteroidsParent).AsCached();

			//Bind Bullets pool
			Container.BindMemoryPool<Bullet, Bullet.BulletsPool, Bullet.BulletsPool>()
				.WithInitialSize(_initialBulletsPoolSize)
				.ExpandByDoubling()
				.FromComponentInNewPrefab(_bulletPrefab);

			//Bind parent for Bullets
			Container.Bind<Transform>().WithId("BulletsParent").FromInstance(_bulletsParent).AsCached();

			//Bind Bullets pool
			Container.BindMemoryPool<Effect, Effect.EffectsPool, Effect.EffectsPool>()
				.WithInitialSize(_initialEffectsPoolSize)
				.ExpandByDoubling()
				.WithId("Bullet")
				.FromComponentInNewPrefab(_bulletEffectPrefab);

			Container.BindMemoryPool<Effect, Effect.EffectsPool, Effect.EffectsPool>()
				.WithInitialSize(_initialEffectsPoolSize)
				.ExpandByDoubling()
				.WithId("Destroy")
				.FromComponentInNewPrefab(_destroyEffectPrefab);

			//Bind parent for Bullets
			Container.Bind<Transform>().WithId("EffectsParent").FromInstance(_effectsParent).AsCached();

			//Bind Bonuses pool
			Container.BindMemoryPool<Bonus, Bonus.BonusesPool, Bonus.BonusesPool>()
				.WithInitialSize(_initialBonusesPoolSize)
				.ExpandByDoubling()
				.FromComponentInNewPrefab(_bonusPrefab);

			//Bind parent for Bonuses
			Container.Bind<Transform>().WithId("BonusesParent").FromInstance(_bonusesParent).AsCached();

			Container.Bind<Gameplay>().FromInstance(_gameplay).AsSingle();

			//Bind Background signals
			Container.BindSignal<float, GameSpeedChanged>().To(_background.OnGameSpeedChanged);

			//Bind GameUI Signals
			Container.BindSignal<int, Vector3, ScoreObjectSpawned>().
				To(_gameUI.SpawnScoreObject);

			Container.BindSignal<Player, LocalPlayerInstalled>().
				To(_gameUI.OnLocalPlayerIdInstalled);

			Container.BindSignal<Player, int, PlayerScoreChanged>().
				To(_gameUI.OnPlayerScoreChanged);

			Container.BindSignal<Player, PlayerEnteredMatch>().
				To(_gameUI.OnPlayerAdded);

			Container.BindSignal<Player, PlayerLeavedMatch>().
				To(_gameUI.OnPlayerRemoved);

			Container.BindSignal<Player, string, PlayerNameChanged>().
				To(_gameUI.OnPlayerNameChanged);

			Container.BindSignal<GamePausedFromServer>().To(_gameUI.OnPauseSignal);
			Container.BindSignal<GameResumedFromServer>().To(_gameUI.OnResumedSignal);

			Container.BindSignal<GamePaused>().To(_gameplay.OnGamePaused);
			Container.BindSignal<GameResumed>().To(_gameplay.OnGameResumed);
			Container.BindSignal<GamePausedFromServer>().To(_gameplay.OnGamePaused);
			Container.BindSignal<GameResumedFromServer>().To(_gameplay.OnGameResumed);
		}
	}
}
