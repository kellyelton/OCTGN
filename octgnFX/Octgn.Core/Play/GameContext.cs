using Octgn.Data;
using Octgn.Online.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Octgn.Core.Play
{
    public class GameContext
    {
        public GameSettings Settings { get; set; }
        public ClientSocket Client { get; set; }
        public HostedGame Game { get; set; }
    }
}
