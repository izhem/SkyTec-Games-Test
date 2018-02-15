using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Zenject;


namespace SkyTecGamesTest
{
	public class Effect : NetworkBehaviour
	{
		[InjectOptional(Id = "EffectsParent")] Transform _parent;
		bool _wasInjected;

		ParticleSystem __particle;
		ParticleSystem _particle { get { if (__particle == null) __particle = GetComponent<ParticleSystem>(); return __particle; } }

		[Inject(Id = "Bullet")] EffectsPool _bulletpool;
		[Inject(Id = "Destroy")] EffectsPool _destroyPool;

		[SerializeField] bool _bullet;

		[Inject] void Inject() { _wasInjected = true; }

		public override void OnStartClient()
		{
			if (!_wasInjected)
			{
				var context = FindObjectOfType<SceneContext>();
				if (context != null)
				{
					context.Container.Inject(this);
				}
			}

			transform.parent = _parent;

			if (gameObject.activeSelf)
			{
				_particle.Play();
				Invoke("UnspawnClient", 2);
			}

			base.OnStartClient();
		}

		void UnspawnServer()
		{
			if (!isServer) return;
			if (_bullet) _bulletpool.Despawn(this);
			else _destroyPool.Despawn(this);
		}

		void UnspawnClient()
		{
			gameObject.SetActive(false);
			_particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
		}

		[ClientRpc] void RpcSpawn(Vector3 pos)
		{
			transform.position = pos;
			_particle.Play(true);
		}

		[ClientRpc] void RpcUnSpawn()
		{
			gameObject.SetActive(false);
			_particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
		}

		public class EffectsPool : MemoryPool<Vector3, Effect>
		{
			[InjectOptional(Id = "EffectsParent")] Transform _parent;

			protected override void OnDespawned(Effect item)
			{
				item.gameObject.SetActive(false);
				item._particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
				NetworkServer.UnSpawn(item.gameObject);
				item.RpcUnSpawn();
			}

			protected override void OnCreated(Effect item)
			{
				item.gameObject.SetActive(false);

				item.transform.parent = _parent;
			}

			protected override void Reinitialize(Vector3 p1, Effect item)
			{
				item.gameObject.SetActive(true);
				item._particle.Play(true);
				NetworkServer.Spawn(item.gameObject);
				item.RpcSpawn(p1);
				item.Invoke("UnspawnServer", 2f);
			}
		}
	}
}
