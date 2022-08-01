using System;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;

public enum Team
{
    Blue = 0,
    Purple = 1
}

public class AgentSoccer : Agent
{
    // Note that that the detectable tags are different for the blue and purple teams. The order is
    // * ball
    // * own goal
    // * opposing goal
    // * wall
    // * own teammate
    // * opposing player

    public enum Position
    {
        Striker,
        Goalie,
        Generic
    }

    [HideInInspector]
    public Team team;
    float m_KickPower;
    // The coefficient for the reward for colliding with a ball. Set using curriculum.
    float m_BallTouch;
    public Position position;

    const float k_Power = 2000f;
    float m_Existential;
    float m_LateralSpeed;
    float m_ForwardSpeed;

    [HideInInspector]
    public float timePenalty;

    [HideInInspector]
    public Rigidbody agentRb;
    SoccerSettings m_SoccerSettings;
    BehaviorParameters m_BehaviorParameters;
    public Vector3 initialPos;
    public float rotSign;

    EnvironmentParameters m_ResetParams;
    private float ballTouchReward = 0;
    private float opponentSpeed = 0;
    private float opponentExist = 0;
    private bool isCurriculum = false;

    // Opponent Only
    private Collider opponentCollider;
    private MeshRenderer opponentRenderer;

    public override void Initialize()
    {
        SoccerEnvController envController = GetComponentInParent<SoccerEnvController>();
        if (envController != null)
        {
            m_Existential = 1f / envController.MaxEnvironmentSteps;
        }
        else
        {
            m_Existential = 1f / MaxStep;
        }

        m_BehaviorParameters = gameObject.GetComponent<BehaviorParameters>();
        if (m_BehaviorParameters.TeamId == (int)Team.Blue)
        {
            team = Team.Blue;
            initialPos = new Vector3(transform.position.x - 5f, .5f, transform.position.z);
            rotSign = 1f;
        }
        else
        {
            team = Team.Purple;
            initialPos = new Vector3(transform.position.x + 5f, .5f, transform.position.z);
            rotSign = -1f;
        }
        if (position == Position.Goalie)
        {
            m_LateralSpeed = 1.0f;
            m_ForwardSpeed = 1.0f;
        }
        else if (position == Position.Striker)
        {
            m_LateralSpeed = 0.3f;
            m_ForwardSpeed = 1.3f;
        }
        else
        {
            m_LateralSpeed = 0.3f;
            m_ForwardSpeed = 1.0f;
        }
        m_SoccerSettings = FindObjectOfType<SoccerSettings>();
        agentRb = GetComponent<Rigidbody>();
        agentRb.maxAngularVelocity = 500;

        m_ResetParams = Academy.Instance.EnvironmentParameters;
        if (m_ResetParams.GetWithDefault("is_curriculum", 0) == 1.0)
        {
            isCurriculum = true;
        } else {
            isCurriculum = false;
        }

        if (isCurriculum == true)
        {
            if (team == Team.Purple)
            {
                opponentCollider = this.GetComponent<Collider>();
                opponentRenderer = transform.Find("AgentCube_Purple").GetComponent<MeshRenderer>();
            }
        }
    }

    public void MoveAgent(ActionSegment<int> act)
    {
        if (isCurriculum == true)
        {
             // Opponent Exist
            if (team == Team.Purple && opponentExist == 0)
            {
                return;
            }
        }
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;

        m_KickPower = 0f;

        var forwardAxis = act[0];
        var rightAxis = act[1];
        var rotateAxis = act[2];

        switch (forwardAxis)
        {
            case 1:
                dirToGo = transform.forward * m_ForwardSpeed;
                m_KickPower = 1f;
                break;
            case 2:
                dirToGo = transform.forward * -m_ForwardSpeed;
                break;
        }

        switch (rightAxis)
        {
            case 1:
                dirToGo = transform.right * m_LateralSpeed;
                break;
            case 2:
                dirToGo = transform.right * -m_LateralSpeed;
                break;
        }

        switch (rotateAxis)
        {
            case 1:
                rotateDir = transform.up * -1f;
                break;
            case 2:
                rotateDir = transform.up * 1f;
                break;
        }

        transform.Rotate(rotateDir, Time.deltaTime * 100f);
        if (isCurriculum == true)
        {
            if (team == Team.Blue)
            {
                agentRb.AddForce(dirToGo * m_SoccerSettings.blueAgentRunSpeed,
                ForceMode.VelocityChange);
            } else {
                agentRb.AddForce(dirToGo * opponentSpeed,
                ForceMode.VelocityChange);
            }
        } else {
            agentRb.AddForce(dirToGo * m_SoccerSettings.agentRunSpeed,ForceMode.VelocityChange);
        }
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        if (position == Position.Goalie)
        {
            // Existential bonus for Goalies.
            AddReward(m_Existential);
        }
        else if (position == Position.Striker)
        {
            // Existential penalty for Strikers
            AddReward(-m_Existential);
        }

        MoveAgent(actionBuffers.DiscreteActions);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        //forward
        if (Input.GetKey(KeyCode.W))
        {
            discreteActionsOut[0] = 1;
        }
        if (Input.GetKey(KeyCode.S))
        {
            discreteActionsOut[0] = 2;
        }
        //rotate
        if (Input.GetKey(KeyCode.A))
        {
            discreteActionsOut[2] = 1;
        }
        if (Input.GetKey(KeyCode.D))
        {
            discreteActionsOut[2] = 2;
        }
        //right
        if (Input.GetKey(KeyCode.E))
        {
            discreteActionsOut[1] = 1;
        }
        if (Input.GetKey(KeyCode.Q))
        {
            discreteActionsOut[1] = 2;
        }
    }
    /// <summary>
    /// Used to provide a "kick" to the ball.
    /// </summary>
    void OnCollisionEnter(Collision c)
    {
        var force = k_Power * m_KickPower;
        if (position == Position.Goalie)
        {
            force = k_Power;
        }
        if (c.gameObject.CompareTag("ball"))
        {
            if (isCurriculum == true)
            {
                if (team == Team.Blue && ballTouchReward > 0)
                {
                    AddReward(ballTouchReward);
                    // AddReward(.2f * m_BallTouch);
                }
            } else {
                AddReward(.2f * m_BallTouch);
            }
            
            var dir = c.contacts[0].point - transform.position;
            dir = dir.normalized;
            c.gameObject.GetComponent<Rigidbody>().AddForce(dir * force);
        }
    }

    public override void OnEpisodeBegin()
    {
        if (isCurriculum == true)
        {
            timePenalty = 0;
            ballTouchReward = m_ResetParams.GetWithDefault("ball_touch_reward", 0);
            m_BallTouch = m_ResetParams.GetWithDefault("ball_touch", 0);
            opponentSpeed = m_ResetParams.GetWithDefault("opponent_speed", m_SoccerSettings.purpleAgentRunSpeed);

            // Deactivate opponent
            if (team == Team.Purple)
            {
                opponentExist = m_ResetParams.GetWithDefault("opponent_exist", 1);
                setOpponentActive(opponentExist == 1);
            }


            if (team == Team.Purple)
            {
                transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            }
            else
            {
                transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            }

            Vector3 startPosition = new Vector3(initialPos.x, initialPos.y, initialPos.z + UnityEngine.Random.Range(-1.0f, 1.0f));
            transform.position = startPosition;

            agentRb.velocity = Vector3.zero;
            agentRb.angularVelocity = Vector3.zero;
            // SetResetParameters();
        } else {
            m_BallTouch = m_ResetParams.GetWithDefault("ball_touch", 0);
        }
    }

    private void setOpponentActive(bool isActive)
    {
        if (team == Team.Purple)
        {
            opponentCollider.enabled = isActive;
            opponentRenderer.enabled = isActive;
        }
    }
}