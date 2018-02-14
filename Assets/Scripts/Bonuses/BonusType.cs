using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkyTecGamesTest
{
	[CreateAssetMenu(menuName = "Game/BonusType")]
	public class BonusType : ScriptableObject
	{
		[SerializeField] Sprite _sprite;
		[SerializeField] float _speed;
		[SerializeField] float _spawnWeight;

		public Sprite Sprite { get { return _sprite; } }
		public float Speed { get { return _speed; } }
		public float SpawnWeight { get { return _spawnWeight; } }
	}
}
