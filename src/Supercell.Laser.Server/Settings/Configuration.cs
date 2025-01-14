﻿namespace Supercell.Laser.Server.Settings
{
    using Newtonsoft.Json;

    public class Configuration
    {
        public static Configuration Instance;

        [JsonProperty("udp_host")] public readonly string UdpHost;
        [JsonProperty("udp_port")] public readonly int UdpPort;

        [JsonProperty("database_username")] public readonly string DatabaseUsername;
        [JsonProperty("database_password")] public readonly string DatabasePassword;
        [JsonProperty("database_name")] public readonly string DatabaseName;
        [JsonProperty("IsMaintence")] public readonly bool IsMaintance;
        [JsonProperty("MaintenceTimer")] public readonly DateTime MaintenceTimer;
        [JsonProperty("TrophySeasonTimer")] public DateTime TrophySeasonTimer;
        [JsonProperty("BrawlPassSeasonTimer")] public DateTime BrawlPassSeasonTimer;


        public static Configuration LoadFromFile(string filename)
        {
            return JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(filename));
        }
    }
}
