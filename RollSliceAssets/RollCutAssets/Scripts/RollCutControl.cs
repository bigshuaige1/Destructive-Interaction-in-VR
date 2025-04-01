using EzySlice;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static BNG.ChopControl;
using static UnityEngine.GraphicsBuffer;

namespace BNG
{
    public class RollCutControl : GrabbableEvents
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

        public bool isOver;
        public bool isCollide;

        public AudioSource chopperAudio;

        bool _inProgress;
        GameObject _slice;
        Material[] _materials;
        float _pointY;

        private GameObject target;
        private Vector3 initialChopperPosition;
        private bool isHitted = false;
        private float targetBottomY;

        private float _lastChoppingHapticTime;
        private float _lastChopPercent;
        private float ChopPercent;
        private float _lastChopHaptic;

        // Used for chopping haptics
        List<ChopDefinition> chopDefs;

        private void Start()
        {
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

        private void Update()
        {
            if (!isHitted)
            {
                //Detect collision
                bool hasHit = Physics.Linecast(startChopPoint.position, endChopPoint.position, out RaycastHit hit, choppableLayer);
                if (hasHit)
                {
                    isCollide = true;
                    chopperAudio.Play();

                    _inProgress = true;
                    // Record initial position
                    initialChopperPosition = chopper.position;
                    isHitted = true;

                    target = hit.transform.gameObject;

                    Bounds bounds = target.GetComponent<Collider>().bounds;
                    targetBottomY = bounds.min.y;

                    //Slice Objects into three parts
                    Slice(target);
                }
            }

            if (isHitted && _inProgress)
            {
                updateChopDistance();
                checkChoppingHaptics();

                Roll();
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

            SlicedHull hull = target.Slice(initialChopperPosition, planeNormal);

            if (hull != null)
            {
                GameObject leftHull = hull.CreateUpperHull(target, crossSectionMaterial);
                GameObject rightHull = hull.CreateLowerHull(target, crossSectionMaterial);

                Destroy(target);

                leftHull.name = "leftHull";
                rightHull.name = "rightHull";

                AddHullComponents(leftHull);
                AddHullComponents(rightHull);

                SetMaterials(rightHull, crossSectionMaterial);

                _slice = rightHull;
                _pointY = float.MaxValue;
                var meshFilter = _slice.GetComponent<MeshFilter>();
                float centerX = meshFilter.sharedMesh.bounds.center.x;

                _materials = _slice.GetComponent<MeshRenderer>().materials;
                foreach (var material in _materials)
                {
                    material.SetFloat("_PointX", centerX);
                }
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

        public void Roll()
        {
            var pos = chopper.position;

            float pointY = _slice.transform.InverseTransformPoint(pos).y;
            if (_pointY > pointY)
            {
                _pointY = pointY;
            }

            foreach (var material in _materials)
            {
                material.SetFloat("_PointY", _pointY);
            }

            if (pos.y <= targetBottomY)
            {
                _slice.GetComponent<Rigidbody>().isKinematic = false;
                _inProgress = false;

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
            Debug.Log("ChopPercent" + ChopPercent);
        }

        public override void OnGrab(Grabber grabber)
        {
            chopGrabber = grabber;
            Debug.Log("chopGrabber assigned: " + chopGrabber.HandSide);
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

            Debug.Log("input" + input);
            Debug.Log("chopGrabber" + chopGrabber);

            // Definition of haptics
            ChopDefinition c = chopDefs.FirstOrDefault(x => x.ChopPercentage <= ChopPercent && x.ChopPercentage != _lastChopHaptic);
            if (c != null && chopGrabber != null && ChopPercent <= 100)
            {
                input.VibrateController(c.HapticFrequency, c.HapticAmplitude, 0.1f, chopGrabber.HandSide);
                _lastChopHaptic = c.ChopPercentage;
                _lastChoppingHapticTime = Time.time;

                Debug.Log("Ö´ÐÐÁË");
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
