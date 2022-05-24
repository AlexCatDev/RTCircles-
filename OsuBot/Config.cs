using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsuBot
{
    public class Config
    {
        public string OSU_API_SECRET { get; set; } = "";
        public string DISCORD_BOT_SECRET { get; set; } = "";

        public ulong BOTOWNER_ID { get; set; } = 0;
    }
}
