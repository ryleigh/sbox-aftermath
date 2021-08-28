using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace aftermath
{
	public class PersonComponent
	{
		public Person Person { get; set; }
		public virtual void Init() {}
		public virtual void Update( float dt ) {}
	}
}
