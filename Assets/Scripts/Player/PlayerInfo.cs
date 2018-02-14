using System;

namespace SkyTecGamesTest
{
	public struct PlayerInfo : IComparable<PlayerInfo>
	{
		public int PlayerId;
		public string Name;
		public float Score;

		public int CompareTo(PlayerInfo other)
		{
			return Math.Sign(other.Score - Score);
		}
	}
}
