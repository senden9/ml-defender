using System;
using UnityEngine;

namespace Common
{
    public class StatsEventSystem : MonoBehaviour
    {
        /// <summary>
        ///     Current instance, used for singleton definition
        /// </summary>
        public static StatsEventSystem current = null;

        private void Awake()
        {
            if (current is not null)
            {
                Debug.LogWarning(
                    "Attention, this is not the first instance of this stats event singleton. Self destruction started");
                Destroy(gameObject);
            }

            current = this;
        }

        /// <summary>
        ///     Emitted when simulation round finished. Contains results of that one simulation.
        /// </summary>
        public event Action<RoundStatisticDto> RoundFinished;

        /// <summary>
        ///     Published when emitter knows how many rounds should be played in total.
        /// </summary>
        public event Action<int> CalculatedTotalRounds;

        public virtual void OnRoundFinished(RoundStatisticDto obj)
        {
            RoundFinished?.Invoke(obj);
        }

        public virtual void OnCalculatedTotalRounds(int totalRounds)
        {
            CalculatedTotalRounds?.Invoke(totalRounds);
        }
    }
}