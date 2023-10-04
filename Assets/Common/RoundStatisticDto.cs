using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Common
{
    /// <summary>
    ///     Payload for the stats event
    /// </summary>
    [Serializable]
    public class RoundStatisticDto
    {
        /// <summary>
        ///     Type of the simulated environment
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public enum EnvironmentTypeEnum
        {
            GWO,
            SMA
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum WhoWonEnum
        {
            DefenderWon,
            AttackerWon
        }

        /// <summary>
        ///     Type of the simulation
        /// </summary>
        public EnvironmentTypeEnum? EnvironmentType;

        /// <summary>
        ///     Setting for maximum playable number of rounds
        /// </summary>
        public uint? MaxRounds;

        /// <summary>
        ///     Number of rounds actually played
        /// </summary>
        public uint? PlayedRounds;

        /// <summary>
        ///     Who won this simulation
        /// </summary>
        public WhoWonEnum? WhoWon;

        public float? LineOfSight;
        public uint? NrAttackers;
        public uint? NrDefenders;
        public float? MaxSpeed;
        public float? AreaSideLength;
        public float? TargetHitRadius;
    }
}