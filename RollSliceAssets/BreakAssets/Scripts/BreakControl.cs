using BNG;
using EzySlice;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;

namespace BNG
{

    public class BreakControl : GrabbableEvents
    {
        [Header("Breaker Grabbing")]
        public bool CanGrabBreaker = false;

        [HideInInspector]
        public Grabber breakGrabber;

        public GameObject leftGrabber;
        public GameObject rightGrabber;
        public Collider leftHandler;
        public Collider rightHandler;
        public GameObject Object;
        public Material leftCrossSectionMaterial;
        public Material rightCrossSectionMaterial;
        public float sizeFactor;

        public AudioSource breakerAudio;

        bool _inProgress;
        GameObject _leftSlice;
        Material[] _leftMaterials;
        GameObject _rightSlice;
        Material[] _rightMaterials;
        float _pointY;

        private bool isLeftGrabbed = false;
        private bool isRightGrabbed = false;
        private bool isGrabbed = false;
        private float targetBottomY;
        private GameObject leftPartHull;
        private GameObject rightPartHull;
        private bool isLeftBroken = false;
        private bool isRightBroken = false;

        private Vector3 initialBreakerPosition;
        private float _lastBreakingHapticTime;
        private float _lastBreakPercent;
        private float BreakPercent;
        private float _lastBreakHaptic;

        // Used for chopping haptics
        List<BreakDefinition> breakDefs;

        private void Start()
        {
            // Define a few haptic positions
            breakDefs = new List<BreakDefinition>() {
                { new BreakDefinition() { BreakPercentage = 10f, HapticAmplitude = 0.1f, HapticFrequency = 0.1f } },
                { new BreakDefinition() { BreakPercentage = 20f, HapticAmplitude = 0.1f, HapticFrequency = 0.1f } },
                { new BreakDefinition() { BreakPercentage = 30f, HapticAmplitude = 0.1f, HapticFrequency = 0.1f } },
                { new BreakDefinition() { BreakPercentage = 40f, HapticAmplitude = 0.1f, HapticFrequency = 0.1f } },
                { new BreakDefinition() { BreakPercentage = 50f, HapticAmplitude = 0.1f, HapticFrequency = 0.2f } },
                { new BreakDefinition() { BreakPercentage = 60f, HapticAmplitude = 0.1f, HapticFrequency = 0.3f } },
                { new BreakDefinition() { BreakPercentage = 70f, HapticAmplitude = 0.1f, HapticFrequency = 0.5f } },
                { new BreakDefinition() { BreakPercentage = 80f, HapticAmplitude = 0.1f, HapticFrequency = 0.7f } },
                { new BreakDefinition() { BreakPercentage = 90f, HapticAmplitude = 0.1f, HapticFrequency = 0.9f } },
                { new BreakDefinition() { BreakPercentage = 100f, HapticAmplitude = 0.1f, HapticFrequency = 1f } },
            };
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
                    Debug.Log("isLeftGrabbed.");
                }

                if (rightGrabberCollider.bounds.Intersects(rightHandler.bounds))
                {
                    initialBreakerPosition = rightGrabber.transform.position;
                    isRightGrabbed = true;
                    Debug.Log("isRightGrabbed.");
                }

                if (isLeftGrabbed && isRightGrabbed)
                {
                    isGrabbed = true;
                    //Slice Objects into three parts
                    Slice(Object);

                    _inProgress = true;

                    Debug.Log("Sliced");
                }
            }

            if (_inProgress)
            {
                updateBreakDistance();
                checkChoppingHaptics();
                LeftRoll();
                RightRoll();
            }

            if(isLeftBroken == true && isRightBroken == true)
            {
                breakerAudio.Play();
            }
        }

        //Slice Objects into three parts
        public void Slice(GameObject target)
        {
            Vector3 planeNormal = Vector3.right;

            //First slice
            SlicedHull Hull = target.Slice(target.transform.position, planeNormal);

            if (Hull != null)
            {
                leftPartHull = Hull.CreateUpperHull(target, leftCrossSectionMaterial);
                rightPartHull = Hull.CreateLowerHull(target, rightCrossSectionMaterial);

                leftPartHull.name = "leftPartHull";
                rightPartHull.name = "rightPartHull";

                AddHullComponents(leftPartHull);
                AddHullComponents(rightPartHull);

                SetMaterials(leftPartHull, leftCrossSectionMaterial);
                SetMaterials(rightPartHull, rightCrossSectionMaterial);

                Destroy(target);

                _leftSlice = leftPartHull;
                _pointY = float.MaxValue;
                var meshFilter1 = _leftSlice.GetComponent<MeshFilter>();
                float centerX1 = meshFilter1.sharedMesh.bounds.center.x;

                _leftMaterials = _leftSlice.GetComponent<MeshRenderer>().materials;
                foreach (var material in _leftMaterials)
                {
                    material.SetFloat("_PointX", centerX1);
                }

                _rightSlice = rightPartHull;
                _pointY = float.MaxValue;
                var meshFilter2 = _rightSlice.GetComponent<MeshFilter>();
                float centerX2 = meshFilter2.sharedMesh.bounds.center.x;

                _rightMaterials = _rightSlice.GetComponent<MeshRenderer>().materials;
                foreach (var material in _rightMaterials)
                {
                    material.SetFloat("_PointX", centerX2);
                }
            }
        }

        // Set the component properties of sliced hull
        private void AddHullComponents(GameObject hull)
        {
            Rigidbody rb = hull.AddComponent<Rigidbody>();
            MeshCollider collider = hull.AddComponent<MeshCollider>();
            rb.isKinematic = true;
            rb.mass = 0.00000000001f;
        }

        public void LeftRoll()
        {
            var pos = leftGrabber.transform.position;

            float pointY = _leftSlice.transform.InverseTransformPoint(pos).y;
            if (_pointY > pointY)
            {
                _pointY = pointY;
            }

            foreach (var material in _leftMaterials)
            {
                material.SetFloat("_PointY", _pointY);
            }

            //if (Mathf.Abs(pos.y - initialBreakerPosition.y) >= Mathf.Abs(targetBottomY - initialBreakerPosition.y) + sizeFactor)
            //{
            //    _inProgress = false;
            //    isLeftBroken = true;
            //}

            Debug.Log("LeftRoll");
        }

        public void RightRoll()
        {
            var pos = rightGrabber.transform.position;

            float pointY = _rightSlice.transform.InverseTransformPoint(pos).y;
            if (_pointY > pointY)
            {
                _pointY = pointY;
            }

            foreach (var material in _rightMaterials)
            {
                material.SetFloat("_PointY", _pointY);
            }

            //if (Mathf.Abs(pos.y - initialBreakerPosition.y) >= Mathf.Abs(targetBottomY - initialBreakerPosition.y) +sizeFactor)
            //{
            //    _inProgress = false;
            //    isRightBroken = true;
            //}

            Debug.Log("RightRoll");
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

        public class BreakDefinition
        {
            public float BreakPercentage { get; set; }
            public float HapticAmplitude { get; set; }
            public float HapticFrequency { get; set; }
        }

        // Update chop distance
        void updateBreakDistance()
        {
            _lastBreakPercent = BreakPercent;

            float chopDistance = Mathf.Abs(rightGrabber.transform.position.y - initialBreakerPosition.y);
            float targetDistance = Mathf.Abs(targetBottomY - initialBreakerPosition.y);

            BreakPercent = (chopDistance / targetDistance) * 100;
            Debug.Log("ChopPercent" + BreakPercent);
        }

        public override void OnGrab(Grabber grabber)
        {
            breakGrabber = grabber;
            Debug.Log("chopGrabber assigned: " + breakGrabber.HandSide);
        }

        // Check if haptics when chopping
        void checkChoppingHaptics()
        {
            if (BreakPercent < _lastBreakPercent)
            {
                return;
            }

            // Avoid overhaptics
            if (Time.time - _lastBreakingHapticTime < 0.1f)
            {
                return;
            }

            if (breakDefs == null)
            {
                return;
            }

            Debug.Log("input" + input);
            Debug.Log("chopGrabber" + breakGrabber);

            // Definition of haptics
            BreakDefinition c = breakDefs.FirstOrDefault(x => x.BreakPercentage <= BreakPercent && x.BreakPercentage != _lastBreakHaptic);
            if (c != null && breakGrabber != null)
            {
                input.VibrateController(c.HapticFrequency, c.HapticAmplitude, 0.1f, breakGrabber.HandSide);
                _lastBreakHaptic = c.BreakPercentage;
                _lastBreakingHapticTime = Time.time;

                Debug.Log("Ö´ÐÐÁË");
            }
        }
    }
}

