using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Zenject;

namespace SkyTecGamesTest
{
	public class GameUI : MonoBehaviour
	{
		[Inject] GamePaused _gamePaused;
		[Inject] GameResumed _gameResumed;

		List<PlayerInfo> _playersInfoList = new List<PlayerInfo>();
		float _scores;
		ScoresManager _scoreManager;
		Player _localPlayer;
		int _localPlayerId { get { return _localPlayer != null ? _localPlayer.PlayerId : 0; } }

		[Header("UI")]
		[SerializeField] string _playerInfoFormat = "{0}. {1} {2}";
		[SerializeField] string _dotsString = "...";
		[SerializeField] Text[] _playersInfo;
		[SerializeField] Button _pauseButton;
		[SerializeField] Button _resumeButton;
		[SerializeField] Button _disconnectButton;
		[SerializeField] TransitionGroup _pauseAnimGroup;
		[SerializeField] Text _scoreText;
		[SerializeField] TransitionalObject _scoreAnim;
		[SerializeField] GameObject _pauseBack;

		[Header("Fonts")]
		[SerializeField] Font _regularFont;
		[SerializeField] Font _boldFont;

		public void OnLocalPlayerIdInstalled(Player localPlayer)
		{
			_localPlayer = localPlayer;

			UpdatePlayersInfo(_playersInfoList, _localPlayerId);
		}

		void HidePlayersInfo()
		{
			foreach (var text in _playersInfo)
			{
				if (text == null || text.gameObject == null) break;
				text.gameObject.SetActive(false);
			}
		}

		void OnScoreAdded(float score)
		{
			_scores += score;
			_scoreText.text = _scores.ToString();
			_scoreAnim.TriggerTransition();
		}

		public void OnPlayerNameChanged(Player player, string name)
		{
			PlayerInfo playerInfo;
			bool found = false;
			int i = 0;
			foreach (var info in _playersInfoList)
			{
				if (info.PlayerId == player.PlayerId)
				{
					found = true;
					break;
				}
				i++;
			}
			if (found)
			{
				playerInfo = _playersInfoList[i];
				playerInfo.Name = name;
				_playersInfoList[i] = playerInfo;
				UpdatePlayersInfo(_playersInfoList, _localPlayerId);
			}
		}

		public void OnPlayerScoreChanged(Player player, int score)
		{
			PlayerInfo playerInfo;
			bool found = false;
			int i = 0;
			foreach (var info in _playersInfoList)
			{
				if (info.PlayerId == player.PlayerId)
				{
					found = true;
					break;
				}
				i++;
			}
			if (found)
			{
				playerInfo = _playersInfoList[i];
				playerInfo.Score = score;
				_playersInfoList[i] = playerInfo;
				UpdatePlayersInfo(_playersInfoList, _localPlayerId);
			}
		}

		public void OnPlayerAdded(Player player)
		{
			_playersInfoList.Add(new PlayerInfo()
			{
				PlayerId = player.PlayerId,
				Name = player.Name,
				Score = player.Score
			});
			UpdatePlayersInfo(_playersInfoList, _localPlayerId);
		}

		public void OnPlayerRemoved(Player player)
		{
			int i = 0;
			bool found = false;
			foreach (var info in _playersInfoList)
			{
				if (info.PlayerId == player.PlayerId)
				{
					found = true;
					break;
				}
				i++;
			}
			if (found)
			{
				_playersInfoList.RemoveAt(i);
				UpdatePlayersInfo(_playersInfoList, _localPlayerId);
			}
		}

		void UpdatePlayersInfo(List<PlayerInfo> playersInfo, int localPlayerId)
		{
			if (localPlayerId == 0) return;
			playersInfo.Sort();

			int dotsIndex = -1;
			int dotsIndex2 = -1;
			int localPlayerPlaceIndex = -1;
			int localPlayerIndex = -1;

			//find local player index in total list
			int i = 0;
			foreach (var playerInfo in playersInfo)
			{
				if (playerInfo.PlayerId == localPlayerId)
				{
					localPlayerIndex = i;
					break;
				}
				i++;
			}

			//find places for dots and local player info
			if (playersInfo.Count > _playersInfo.Length)
			{
				if (localPlayerIndex >= _playerInfoFormat.Length - 1)
				{
					localPlayerPlaceIndex = _playerInfoFormat.Length - 2;
					dotsIndex = localPlayerIndex - 1;
				}
				else
				{
					localPlayerPlaceIndex = localPlayerIndex;
				}

				dotsIndex2 = _playersInfo.Length - 1;
			}
			else
			{
				localPlayerPlaceIndex = localPlayerIndex;
			}

			PlayerInfo info;

			//apply updated info to texts
			//regular fond for other players and dots, bold font for local player
			for(int k = 0, s = Mathf.Min(_playersInfo.Length, playersInfo.Count); k < s; k++)
			{
				if (k == localPlayerPlaceIndex)
				{
					info = playersInfo[localPlayerIndex];
					_playersInfo[k].text = string.Format(_playerInfoFormat, localPlayerIndex + 1, info.Name, info.Score);
					_playersInfo[k].font = _boldFont;
				}
				else if (k == dotsIndex || k == dotsIndex2)
				{
					_playersInfo[k].text = _dotsString;
					_playersInfo[k].font = _regularFont;
				}
				else
				{
					info = playersInfo[k];
					_playersInfo[k].text = string.Format(_playerInfoFormat, k + 1, info.Name, info.Score);
					_playersInfo[k].font = _regularFont;
				}
			}

			//hide all and show which are needed
			HidePlayersInfo();
			for (int j = 0, s = Mathf.Min(_playersInfo.Length, playersInfo.Count); j < s; j++)
			{
				_playersInfo[j].gameObject.SetActive(true);
			}
		}

		public void OnPauseSignal()
		{
			_pauseBack.SetActive(true);
			_pauseAnimGroup.TriggerGroupTransition(true);
		}

		public void OnResumedSignal()
		{
			_pauseBack.SetActive(false);
			_pauseAnimGroup.TriggerGroupFadeOut();
		}

		void OnPause()
		{
			_gamePaused.Fire();
		}

		void OnResume()
		{
			_gameResumed.Fire();
		}

		void OnDisconnect()
		{
			GameManager.Disconnect();
		}

		public void SpawnScoreObject(int score, Vector3 position)
		{
			_scoreManager.SpawnScoreObject(position, score);
		}

		void Awake()
		{
			_pauseButton.onClick.AddListener(OnPause);
			_resumeButton.onClick.AddListener(OnResume);
			_disconnectButton.onClick.AddListener(OnDisconnect);

			_scoreText.text = _scores.ToString();
			_scoreManager = FindObjectOfType<ScoresManager>();
			_scoreManager.ScoreAdded += OnScoreAdded;

			HidePlayersInfo();
		}
	}

	
}