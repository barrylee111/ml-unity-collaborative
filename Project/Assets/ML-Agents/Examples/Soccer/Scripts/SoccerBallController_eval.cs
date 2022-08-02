// using UnityEngine;

// public class SoccerBallController : MonoBehaviour
// {
//     public GameObject area;
//     [HideInInspector]
//     public SoccerEnvController envController;
//     public string purpleGoalTag; //will be used to check if collided with purple goal
//     public string blueGoalTag; //will be used to check if collided with blue goal

//     void Start()
//     {
//         Debug.Log("PlayerLog: Ball position is: " + transform.position);
//         envController = area.GetComponent<SoccerEnvController>();
//     }

    
//     void OnCollisionEnter(Collision col)
//     {
//         Debug.Log("PlayerLog: Ball position is: " + transform.position);
//         if (col.gameObject.CompareTag(purpleGoalTag)) //ball touched purple goal
//         {
//             Debug.Log("PlayerLog: Blue Team Scored!");
//             envController.GoalTouched(Team.Blue);
//         }
//         if (col.gameObject.CompareTag(blueGoalTag)) //ball touched blue goal
//         {
//             Debug.Log("PlayerLog: Purple Team Scored!");
//             envController.GoalTouched(Team.Purple);
//         }
//     }
// }
