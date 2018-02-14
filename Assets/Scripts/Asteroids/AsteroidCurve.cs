using UnityEngine;
using System.Collections.Generic;

namespace SkyTecGamesTest
{
	public struct AsteroidCurve
	{
		public Vector2 P1, P2, P3;

		public Vector2 Evaluate(float t)
		{
			t = Mathf.Clamp01(t);
			return (1 - t) * (1 - t) * P1 + 2 * t * (1 - t) * P2 + t * t * P3;
		}

		public Queue<Vector2> GetPath(float deltaT)
		{
			var queue = new Queue<Vector2>();
			float t = deltaT;
			Vector2 last = P1;
			queue.Enqueue(P1);
			while (t < 1)
			{
				last = Evaluate(t);
				queue.Enqueue(last);
				t += deltaT;
			}
			if (last != P3)
			{
				queue.Enqueue(P3);
			}
			return queue;
		}
	}
}
