using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;

namespace SkyTecGamesTest
{
	public class ScoresManager : MonoBehaviour
	{
		Stack<ScoreObject> _inactiveScoreObjects = new Stack<ScoreObject>();
		int _activeObjects = 0;

		[SerializeField] GameObject _scoreObjectPrefab;
		[SerializeField] int _initialPoolSize;
		[SerializeField] bool _allowTestByTKey;

		public event Action<float> ScoreAdded;

		void Awake()
		{
			for(int i = 0; i < _initialPoolSize; i++)
			{
				_inactiveScoreObjects.Push(InstantiateScoreObject());
			}
		}

		ScoreObject InstantiateScoreObject()
		{
			var go = GameObject.Instantiate(_scoreObjectPrefab, transform);
			var obj = go.GetComponent<ScoreObject>();
			obj.AnimFinished += OnScoreObjectAnimFinished;
			go.SetActive(false);
			return obj;
		}

		void OnScoreObjectAnimFinished(ScoreObject scoreObj)
		{
			_inactiveScoreObjects.Push(scoreObj);
			scoreObj.gameObject.SetActive(false);
			_activeObjects--;

			if (ScoreAdded != null)
			{
				ScoreAdded(scoreObj.Score);
			}
		}

		public void SpawnScoreObject(Vector3 position, float score)
		{
			ScoreObject scoreObj;
			if (_inactiveScoreObjects.Count > 0)
			{
				scoreObj = _inactiveScoreObjects.Pop();
			}
			else
			{
				scoreObj = InstantiateScoreObject();
			}

			scoreObj.Show(position, score);

			_activeObjects++;
		}

		void Update()
		{
			if (_allowTestByTKey && Input.GetKeyDown(KeyCode.T))
			{
				SpawnScoreObject(new Vector3(UnityEngine.Random.Range(-900, 900), UnityEngine.Random.Range(-600, 300), 0),
					UnityEngine.Random.Range(1, 10));
			}
		}
	}
}