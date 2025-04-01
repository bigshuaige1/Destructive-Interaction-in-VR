using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.IMGUI.Controls.PrimitiveBoundsHandle;

public class SliderController : MonoBehaviour
{
    public Transform _tool;

    public void MoveTool(float y)
    {
        var pos = _tool.position;
        pos.y = y;
        _tool.position = pos;
    }
}
