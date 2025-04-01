using BNG;
using EzySlice;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Windows;

namespace BNG
{

    public class ChopControl : GrabbableEvents
    {
        [Header("Chopper Grabbing")]
        public bool CanGrabChopper = false;

        [HideInInspector]
        public Grabber chopGrabber;

        public Transform chopper;
        public Transform startChopPoint;
        public Transform endChopPoint;
        public Transform handler;
        public Material crossSectionMaterial;
        public LayerMask choppableLayer;
        public float sizeFactor = 0.001f;
        public float velocityFactor = 1.2f;
        public float xFollowFactor = 0.5f;
        public float cutForce;

        public bool isOver;
        public bool isCollide;

        public AudioSource chopperAudio;

        private GameObject target;
        private GameObject leftHull;
        private GameObject fissureChild;
        private GameObject fissure;
        private GameObject rightHull;
        private GameObject leftPartHull;

        private Transform fissureTransform;
        private Vector3 initialFissureScale;
        private Vector3 initialChopperPosition;
        private float targetBottomY;
        private bool isHitted = false;
        private bool isChopping = true;

        private float _lastChoppingHapticTime;
        private float _lastChopPercent;
        private float ChopPercent;
        private float _lastChopHaptic;

        // Used for chopping haptics
        List<ChopDefinition> chopDefs;

        public void Start()
        {
            isOver = false;
            isCollide = false;

            // Define a few haptic positions
            chopDefs = new List<ChopDefinition>() {
                { new ChopDefinition() { ChopPercentage = 10f, HapticAmplitude = 0.1f, HapticFrequency = 0.1f } },
                { new ChopDefinition() { ChopPercentage = 20f, HapticAmplitude = 0.1f, HapticFrequency = 0.1f } },
                { new ChopDefinition() { ChopPercentage = 30f, HapticAmplitude = 0.1f, HapticFrequency = 0.1f } },
                { new ChopDefinition() { ChopPercentage = 40f, HapticAmplitude = 0.1f, HapticFrequency = 0.1f } },
                { new ChopDefinition() { ChopPercentage = 50f, HapticAmplitude = 0.1f, HapticFrequency = 0.2f } },
                { new ChopDefinition() { ChopPercentage = 60f, HapticAmplitude = 0.1f, HapticFrequency = 0.3f } },
                { new ChopDefinition() { ChopPercentage = 70f, HapticAmplitude = 0.1f, HapticFrequency = 0.5f } },
                { new ChopDefinition() { ChopPercentage = 80f, HapticAmplitude = 0.1f, HapticFrequency = 0.7f } },
                { new ChopDefinition() { ChopPercentage = 90f, HapticAmplitude = 0.1f, HapticFrequency = 0.9f } },
                { new ChopDefinition() { ChopPercentage = 100f, HapticAmplitude = 0.1f, HapticFrequency = 1f } },
            };
        }

        public void Update()
        {
            if (!isHitted)
            {
                //Detect collision
                bool hasHit = Physics.Linecast(startChopPoint.position, endChopPoint.position, out RaycastHit hit, choppableLayer);
                if (hasHit)
                {
                    isCollide = true;
                    chopperAudio.Play();

                    // Record initial position
                    initialChopperPosition = chopper.position;
                    //Debug.Log("initialChopperPosition" + initialChopperPosition);
                    isHitted = true;

                    target = hit.transform.gameObject;

                    //Slice Objects into three parts
                    Slice(target);

                    if (fissure != null)
                    {
                        fissureTransform = fissure.GetComponent<Transform>();

                        // Record initial scale
                        initialFissureScale = fissureTransform.localScale;
                    }
                }
            }

            if (isHitted && isChopping)
            {
                updateChopDistance();
                checkChoppingHaptics();

                // Check whether fissureTransform exists
                if (fissureTransform == null)
                {
                    isChopping = false;
                    return;
                }

                //Update the scale of fissure to create crack effect
                UpdateScale();

                // Object separates and bounces off
                SeperateObjects();
            }
        }

        //Slice Objects into three parts
        public void Slice(GameObject target)
        {
            // Defining slice parameters
            Vector3 firstVector = endChopPoint.position - startChopPoint.position;
            Vector3 secondVector = startChopPoint.position - handler.position;
            Vector3 planeNormal = Vector3.Cross(firstVector, secondVector);
            planeNormal.Normalize();

            Vector3 firstSlicePoint = initialChopperPosition + new Vector3(0, 0, -sizeFactor);
            Vector3 secondSlicePoint = initialChopperPosition + new Vector3(0, 0, sizeFactor);

            //Perform the first slice
            SlicedHull firstHull = target.Slice(firstSlicePoint, planeNormal);

            if (firstHull != null)
            {
                leftPartHull = firstHull.CreateUpperHull(target, crossSectionMaterial);
                rightHull = firstHull.CreateLowerHull(target, crossSectionMaterial);

                rightHull.name = "rightHull";

                AddHullComponents(leftPartHull);
                AddHullComponents(rightHull);
            }

            //Perform the second slice
            SlicedHull secondHull = leftPartHull.Slice(secondSlicePoint, planeNormal);

            if (secondHull != null)
            {
                leftHull = secondHull.CreateUpperHull(leftPartHull, crossSectionMaterial);
                fissureChild = secondHull.CreateLowerHull(leftPartHull, crossSectionMaterial);

                leftHull.name = "leftHull";
                fissureChild.name = "fissureChild";

                AddHullComponents(leftHull);
                AddHullComponents(fissureChild);

                SetMaterials(fissureChild, crossSectionMaterial);

                InitiaFissure(fissureChild);

                Destroy(target);
                Destroy(leftPartHull);
            }
        }

        // Set the component properties of sliced hull
        private void AddHullComponents(GameObject hull)
        {
            Rigidbody rb = hull.AddComponent<Rigidbody>();
            MeshCollider collider = hull.AddComponent<MeshCollider>();
            collider.convex = true;
            rb.isKinematic = true;
        }

        // Initialize fissure
        private void InitiaFissure(GameObject fissureChild)
        {
            // Create the fissure GameObject
            fissure = new GameObject("fissure");
            fissureTransform = fissure.transform;

            // Calculate the bottom y position of the parent object in world space
            Bounds bounds = target.GetComponent<Collider>().bounds;
            targetBottomY = bounds.min.y;

            // Set the parent relationship and position for fissureChild
            fissureTransform.SetParent(target.transform);
            fissureTransform.position = new Vector3(target.transform.position.x, targetBottomY, target.transform.position.z);
            fissureTransform.SetParent(null);

            fissureChild.transform.SetParent(fissureTransform);
        }

        //Update the scale of fissure
        public void UpdateScale()
        {
            if(chopper.position.y < initialChopperPosition.y)
            {
                float targetYPosition = chopper.position.y - initialChopperPosition.y;
                Vector3 scaleChange = initialFissureScale + new Vector3(targetYPosition * velocityFactor * xFollowFactor, targetYPosition * velocityFactor, 0);
                fissureTransform.localScale = scaleChange;
            }
        }

        // Separates objects and bounces off
        public void SeperateObjects()
        {
            if (chopper.position.y <= targetBottomY)
            {
                isChopping = false;
                Destroy(fissure);
                fissureTransform = null;

                Rigidbody leftRb = leftHull.GetComponent<Rigidbody>();
                Rigidbody rightRb = rightHull.GetComponent<Rigidbody>();

                leftRb.isKinematic = false;
                rightRb.isKinematic = false;

                leftRb.AddExplosionForce(cutForce, chopper.position, 1);
                rightRb.AddExplosionForce(cutForce, chopper.position, 1);

                isOver = true;
            }
        }

        // Update chop distance
        void updateChopDistance()
        {
            _lastChopPercent = ChopPercent;

            float chopDistance = Mathf.Abs(chopper.position.y - initialChopperPosition.y);
            float targetDistance = Mathf.Abs(targetBottomY - initialChopperPosition.y);

            ChopPercent = (chopDistance / targetDistance) * 100;
            //Debug.Log("ChopPercent" + ChopPercent);
        }

        public override void OnGrab(Grabber grabber)
        {
            chopGrabber = grabber;
            //Debug.Log("chopGrabber assigned: " + chopGrabber.HandSide);
        }

        // Check if haptics when chopping
        void checkChoppingHaptics()
        {
            if (ChopPercent < _lastChopPercent)
            {
                return;
            }

            // Avoid overhaptics
            if (Time.time - _lastChoppingHapticTime < 0.1f)
            {
                return;
            }

            if (chopDefs == null)
            {
                return;
            }

            //Debug.Log("input" + input);
            //Debug.Log("chopGrabber" + chopGrabber);

            // Definition of haptics
            ChopDefinition c = chopDefs.FirstOrDefault(x => x.ChopPercentage <= ChopPercent && x.ChopPercentage != _lastChopHaptic);
            if (c != null && chopGrabber != null)
            {
                input.VibrateController(c.HapticFrequency, c.HapticAmplitude, 0.1f, chopGrabber.HandSide);
                _lastChopHaptic = c.ChopPercentage;
                _lastChoppingHapticTime = Time.time;
            }
        }

        public class ChopDefinition
        {
            public float ChopPercentage { get; set; }
            public float HapticAmplitude { get; set; }
            public float HapticFrequency { get; set; }
        }

        public void SetMaterials(GameObject hull, Material material)
        {
            MeshRenderer meshRenderer = hull.GetComponent<MeshRenderer>();
            int materialCount = meshRenderer.materials.Length;

            Material[] newMaterials = new Material[materialCount];
            for (int i = 0; i < materialCount; i++)
            {
                newMaterials[i] = material;
            }

            meshRenderer.materials = newMaterials;
        }
    }
}