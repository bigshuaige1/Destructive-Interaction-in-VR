using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandlerMoveAction : MonoBehaviour
{
    public Transform Handler_lead;
    public Transform Handler_flollow;

    private Vector3 previousPosition_lead;

    private void Start()
    {
        // Record the initial position of Handler_lead in Start
        previousPosition_lead = Handler_lead.position;
    }

    private void Update()
    {
        if(Handler_lead.position != previousPosition_lead)
        {
            HandlerMove();
        }
    }

    public void HandlerMove()
    {
        // Calculate the current displacement increment of Handler_lead
        Vector3 currentPosition_lead = Handler_lead.position;
        Vector3 deltaPosition_lead = currentPosition_lead - previousPosition_lead;

        // Apply the opposite displacement increment to Handler_follow
        Handler_flollow.position += deltaPosition_lead;

        // Update previousPosition_lead to the Handler_lead position of the current frame
        previousPosition_lead = currentPosition_lead;
    }
}
