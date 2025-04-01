using System.Linq;
using UnityEngine;

public class ClothFollowHandler : MonoBehaviour
{
    public Cloth cloth;
    public Transform handler;
    public float followFactor;
    public float liftDistance;
    public float correctFactor;

    public GameObject leftGrabber;
    public GameObject rightGrabber;
    public Collider leftHandler;
    public Collider rightHandler;

    private Mesh clothMesh;
    private Vector3[] initialVertices;
    private Vector3[] vertices;
    private Vector3 initialHandlerPosition;
    private Vector3 initialVertexPosition;
    private int topVerticesNumber;

    private bool isLeftGrabbed = false;
    private bool isRightGrabbed = false;
    private bool isGrabbed = false;

    private void Start()
    {
        // Get the vertex position of the current fabric
        clothMesh = cloth.GetComponent<MeshFilter>().mesh;
        if (clothMesh == null)
        {
            Debug.LogError("Target does not have a MeshFilter component.");
            return;
        }
        initialVertices = clothMesh.vertices;
        vertices = (Vector3[])initialVertices.Clone();

        // Record initial position
        initialHandlerPosition = handler.position;
        initialVertexPosition = vertices[0];

        // Get the number of the top vertices
        float maxZPosition = initialVertices.Max(v => v.z);
        topVerticesNumber = initialVertices.Count(v => v.z == maxZPosition);
    }

    private void UpdateVertexLimits()
    {
        // Updating vertex limits
        ClothSkinningCoefficient[] coefficients = cloth.coefficients;

        // Initialize coefficients
        for (int i = 0; i < coefficients.Length; i++)
        {
            coefficients[i].maxDistance = 0f;
        }

        // Set the coefficient of the first vertex of the second row
        coefficients[topVerticesNumber].maxDistance = 0.00005f;

        // Setting the coefficient of other vertices
        for (int i = 0; i < topVerticesNumber; i++)
        {
            for (int j = i; j < topVerticesNumber; j++)
            {
                coefficients[topVerticesNumber * (i + 1) + (j + 1)].maxDistance = 0.0001f;
            }
        }

        // Update coefficients
        cloth.coefficients = coefficients;
    }

    private void Update()
    {
        //Detect Collision
        Collider leftGrabberCollider = leftGrabber.GetComponent<Collider>();
        Collider rightGrabberCollider = rightGrabber.GetComponent<Collider>();

        if (!isGrabbed)
        {
            if (leftGrabberCollider.bounds.Intersects(leftHandler.bounds))
            {
                isLeftGrabbed = true;
            }

            if (rightGrabberCollider.bounds.Intersects(rightHandler.bounds))
            {
                isRightGrabbed = true;
            }

            if (isLeftGrabbed && isRightGrabbed)
            {
                isGrabbed = true;
                UpdateVertexLimits();
            }
        }

        // Get the current position of the handle
        Vector3 targetPosition = handler.position - initialHandlerPosition;

        // Update top of the vertex position
        for (int i = 0; i < topVerticesNumber; i++)
        {
            Vector3 vertex = initialVertices[i];
            vertices[i] = new Vector3(vertex.x + targetPosition.x * followFactor, vertex.y - targetPosition.z * followFactor, vertex.z + targetPosition.y * followFactor);
        }

        if (Mathf.Abs(vertices[0].y - initialVertexPosition.y) < liftDistance)
        {
            for (int i = topVerticesNumber; i < initialVertices.Length; i++)
            {
                Vector3 vertex = initialVertices[i];
                vertices[i] = new Vector3(vertex.x, vertex.y + targetPosition.z * followFactor * correctFactor, vertex.z);
            }
        }

        // Update coefficients if needed
        ClothSkinningCoefficient[] coefficients = cloth.coefficients;

        // Update mesh with new vertex positions
        clothMesh.vertices = vertices;
        clothMesh.RecalculateBounds();
        clothMesh.RecalculateNormals();

        SkinnedMeshRenderer skinnedMeshRenderer = cloth.GetComponent<SkinnedMeshRenderer>();
        if (skinnedMeshRenderer != null)
        {
            skinnedMeshRenderer.sharedMesh = clothMesh;
        }
        else
        {
            Debug.LogError("Target does not have a SkinnedMeshRenderer component.");
        }
    }
}






