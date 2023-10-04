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

        public event Action<RoundStatisticDto> RoundFinished;

        public virtual void OnRoundFinished(RoundStatisticDto obj)
        {
            RoundFinished?.Invoke(obj);
        }
    }
}