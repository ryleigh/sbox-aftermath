using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace aftermath
{
	public class FloaterText
	{
		public Vector3 Position { get; set; }
		public string Text { get; set; }
		public Color Color { get; set; }

		// public float ElapsedLifetime { get; set; }
		// public float Lifetime { get; set; }

		public Vector3 Velocity { get; set; }

		public bool ShouldRemove { get; set; }
		public float Opacity { get; set; }

		public FloaterText(Vector3 pos, string text, Color color)
		{
			Position = pos;
			Text = text;
			Color = color;
			// Lifetime = lifetime;
			// ElapsedLifetime = 0f;

			Velocity = new Vector3( Rand.Float( -20f, 20f ), Rand.Float( -20f, 20f ), Rand.Float( 400f, 450f ) );
			Opacity = 1f;
		}

		public void Update( float dt )
		{
			DebugOverlay.Text( Position, Text, Color.WithAlpha( Opacity ), 0f, float.MaxValue);

			if ( Position.z < 0f )
			{
				Opacity -= 5f * dt;
				if ( Opacity <= 0f )
					ShouldRemove = true;
			}
			else
			{
				Position += Velocity * dt;
				Velocity = new Vector3( Velocity.x * 0.95f, Velocity.y * 0.95f, Velocity.z - 350f * dt );
			}
		}
	}
}
