using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SkyTecGamesTest
{
	[CreateAssetMenu(menuName = "Game/AsteroidType")]
	public class AsteroidType : ScriptableObject
	{
		[Serializable]
		public class SpriteColliderPair
		{
			public Sprite Sprite;
			public int ColliderId;
		}

		[Header("Base")]
		[SerializeField] float _spawnWeight;
		[MinMaxRange(0, 100)]
		[SerializeField] MinMaxRange _speedRange;
		[MinMaxRange(-100, 100)]
		[SerializeField] MinMaxRange _rotationRange;
		[SerializeField] int _bounty;
		[SerializeField] int _hp;
		[SerializeField] SpriteColliderPair[] _spriteColliderPairs;

		[Header("Splinters")]
		[SerializeField] bool _hasSplinters;
		[SerializeField] MinMaxRange _splinterCountRange;
		[SerializeField] AsteroidType[] _splintersTypes;

		public float SpawnWeight { get { return _spawnWeight; } }
		public int Bounty { get { return _bounty; } }
		public int HP { get { return _hp; } }
		public bool HasSplinters { get { return _hasSplinters; } }

		public List<AsteroidType> GetSplintersTypes()
		{
			return new List<AsteroidType>(_splintersTypes);
		}
		
		public float GetRandomBaseSpeed()
		{
			return _speedRange.GetRandomValue();
		}

		public float GetRandomRotation()
		{
			return _rotationRange.GetRandomValue();
		}

		public AsteroidType GetRandomSpliterType()
		{
			if (_splintersTypes.Length == 0) return null;
			if (_splintersTypes.Length == 1) return _splintersTypes[0];

			return _splintersTypes[UnityEngine.Random.Range(0, _splintersTypes.Length)];
		}

		public int GetRandomSplintersCount()
		{
			return UnityEngine.Random.Range((int)_splinterCountRange.rangeStart, (int)_splinterCountRange.rangeEnd + 1);
		}

		public Sprite GetRandomSprite()
		{
			if (_spriteColliderPairs.Length == 0) return null;
			if (_spriteColliderPairs.Length == 1) return _spriteColliderPairs[0].Sprite;

			return _spriteColliderPairs[UnityEngine.Random.Range(0, _spriteColliderPairs.Length)].Sprite;
		}


		public int SetColliderForSprite(Sprite sprite, Asteroid asteroid)
		{
			var colliders = asteroid.GetComponentsInChildren<AsteroidCollider>(true);
			int id = 0;
			foreach(var pair in _spriteColliderPairs)
			{
				if (pair.Sprite == sprite)
				{
					id = pair.ColliderId;
					break;
				}
			}

			foreach(var collider in colliders)
			{
				collider.gameObject.SetActive(collider.Id == id);
			}
			return id;
		}
	}
}