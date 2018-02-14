using System;
using UnityEngine;
using UnityEngine.UI;

namespace SkyTecGamesTest
{
	public class ScoreObject : MonoBehaviour
	{
		[SerializeField] Text _text;
		[SerializeField] string _format = "+{0}";
		[SerializeField] TransitionalObject _animObject;

		public float Score { get; private set; }

		public event Action<ScoreObject> AnimFinished;

		public void Show(Vector3 position, float score)
		{
			Score = score;
			_text.text = string.Format(_format, score);
			transform.position = position;
			gameObject.SetActive(true);
			_animObject.TriggerTransition();
		}

		public void OnAnimFinished()
		{
			if (AnimFinished != null)
			{
				AnimFinished(this);
			}
		}
	}
}