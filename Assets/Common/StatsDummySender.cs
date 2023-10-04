using System.Collections;
using Common;
using UnityEngine;

/// <summary>
///     Test-Emitter of Stats data.
///     Used only for testing and debugging of application parts.
/// </summary>
public class StatsDummySender : MonoBehaviour
{
    public float waitSeconds = 2f;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(Emitter());
    }

    private IEnumerator Emitter()
    {
        while (true)
        {
            RoundStatisticDto obj = new RoundStatisticDto();
            obj.WhoWon = RoundStatisticDto.WhoWonEnum.AttackerWon;
            obj.MaxRounds = 4000;

            //dummy non-null
            /*
            obj.NrAttackers = 1;
            obj.EnvironmentType = RoundStatisticDto.EnvironmentTypeEnum.GWO;
            obj.MaxSpeed = 0f;
            obj.PlayedRounds = 2;
            obj.TargetHitRadius = -1f;
            obj.LineOfSight = 2f;
            obj.NrDefenders = 0;
            obj.AreaSideLength = 23334;
            */

            StatsEventSystem.current.OnRoundFinished(obj);
            yield return new WaitForSeconds(waitSeconds);
        }
    }
}