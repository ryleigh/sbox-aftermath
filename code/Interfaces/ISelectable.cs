using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace aftermath
{
	public interface ISelectable
	{
		public Player Player { get; }
		public Vector2 Position2D { get; }
		public bool IsSelected { get; }
		// public bool CanMultiSelect { get; }
		public void Select();
		public void Deselect();
		public void Assign( Player player );
		public bool IsLocalPlayers { get; }
	}
}
