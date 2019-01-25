using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SkeletonRenderer : MonoBehaviour
{
    private long _lastFrameIndex = -1;

    private Astra.Body[] _bodies;
    private Dictionary<int, GameObject[]> _bodySkeletons;

    private readonly Vector3 NormalPoseScale = new Vector3(1, 1, 1);
    private readonly Vector3 GripPoseScale = new Vector3(0.5f, 0.5f, 0.5f);

    public GameObject JointPrefab;
    public Transform JointRoot;

    public Toggle ToggleSeg = null;
    public Toggle ToggleSegBody = null;
    public Toggle ToggleSegBodyHand = null;

    public Toggle ToggleProfileFull = null;
    public Toggle ToggleProfileUpperBody = null;
    public Toggle ToggleProfileBasic = null;

    public Toggle ToggleOptimizationAccuracy = null;
    public Toggle ToggleOptimizationBalanced = null;
    public Toggle ToggleOptimizationMemory = null;
    public Slider SliderOptimization = null;

    private Astra.BodyTrackingFeatures _previousTargetFeatures = Astra.BodyTrackingFeatures.HandPose;
    private Astra.SkeletonProfile _previousSkeletonProfile = Astra.SkeletonProfile.Full;
    private Astra.SkeletonOptimization _previousSkeletonOptimization = Astra.SkeletonOptimization.BestAccuracy;

    void Start()
    {
        _bodySkeletons = new Dictionary<int, GameObject[]>();
        _bodies = new Astra.Body[Astra.BodyFrame.MaxBodies];
    }

    public void OnNewFrame(Astra.BodyStream bodyStream, Astra.BodyFrame frame)
    {
        if (frame.Width == 0 ||
            frame.Height == 0)
        {
            return;
        }

        if (_lastFrameIndex == frame.FrameIndex)
        {
            return;
        }

        _lastFrameIndex = frame.FrameIndex;

        frame.CopyBodyData(ref _bodies);
        UpdateSkeletonsFromBodies(_bodies);
        UpdateBodyFeatures(bodyStream, _bodies);
        UpdateSkeletonProfile(bodyStream);
        UpdateSkeletonOptimization(bodyStream);
    }

    void UpdateSkeletonsFromBodies(Astra.Body[] bodies)
    {
        foreach (var body in bodies)
        {
            if (body.Status == Astra.BodyStatus.NotTracking)
            {
                continue;
            }

            GameObject[] joints;
            if (!_bodySkeletons.ContainsKey(body.Id))
            {
                joints = new GameObject[body.Joints.Length];

                for (int i = 0; i < joints.Length; i++)
                {
                    joints[i] = (GameObject)Instantiate(JointPrefab, Vector3.zero, Quaternion.identity);
                    joints[i].transform.SetParent(JointRoot);
                }

                _bodySkeletons.Add(body.Id, joints);
            }
            else
            {
                joints = _bodySkeletons[body.Id];
            }

            for (int i = 0; i < body.Joints.Length; i++)
            {
                var skeletonJoint = joints[i];
                var bodyJoint = body.Joints[i];

                if (bodyJoint.Status != Astra.JointStatus.NotTracked)
                {
                    if (!skeletonJoint.activeSelf)
                    {
                        skeletonJoint.SetActive(true);
                    }

                    skeletonJoint.transform.localPosition =
                        new Vector3(bodyJoint.WorldPosition.X / 1000f,
                                    bodyJoint.WorldPosition.Y / 1000f,
                                    bodyJoint.WorldPosition.Z / 1000f);

                    //skel.Joints[i].Orient.Matrix:
                    // 0, 			1,	 		2,
                    // 3, 			4, 			5,
                    // 6, 			7, 			8
                    // -------
                    // right(X),	up(Y), 		forward(Z)

                    //Vector3 jointRight = new Vector3(
                    //    bodyJoint.Orientation.M00,
                    //    bodyJoint.Orientation.M10,
                    //    bodyJoint.Orientation.M20);

                    Vector3 jointUp = new Vector3(
                        bodyJoint.Orientation.M01,
                        bodyJoint.Orientation.M11,
                        bodyJoint.Orientation.M21);

                    Vector3 jointForward = new Vector3(
                        bodyJoint.Orientation.M02,
                        bodyJoint.Orientation.M12,
                        bodyJoint.Orientation.M22);

                    skeletonJoint.transform.rotation =
                        Quaternion.LookRotation(jointForward, jointUp);

                    if (bodyJoint.Type == Astra.JointType.LeftHand)
                    {
                        UpdateHandPoseVisual(skeletonJoint, body.HandPoseInfo.LeftHand);
                    }
                    else if (bodyJoint.Type == Astra.JointType.RightHand)
                    {
                        UpdateHandPoseVisual(skeletonJoint, body.HandPoseInfo.RightHand);
                    }
                }
                else
                {
                   if (skeletonJoint.activeSelf) skeletonJoint.SetActive(false);
                }
            }
        }
    }

    private void UpdateHandPoseVisual(GameObject skeletonJoint, Astra.HandPose pose)
    {
        Vector3 targetScale = NormalPoseScale;
        if (pose == Astra.HandPose.Grip)
        {
            targetScale = GripPoseScale;
        }
        skeletonJoint.transform.localScale = targetScale;
    }

    private void UpdateBodyFeatures(Astra.BodyStream bodyStream, Astra.Body[] bodies)
    {
        if (ToggleSeg != null &&
            ToggleSegBody != null &&
            ToggleSegBodyHand != null)
        {
            Astra.BodyTrackingFeatures targetFeatures = Astra.BodyTrackingFeatures.Segmentation;
            if (ToggleSegBodyHand.isOn)
            {
                targetFeatures = Astra.BodyTrackingFeatures.HandPose;
            }
            else if (ToggleSegBody.isOn)
            {
                targetFeatures = Astra.BodyTrackingFeatures.Skeleton;
            }

            if (targetFeatures != _previousTargetFeatures)
            {
                _previousTargetFeatures = targetFeatures;
                foreach (var body in bodies)
                {
                    if (body.Status != Astra.BodyStatus.NotTracking)
                    {
                        bodyStream.SetBodyFeatures(body.Id, targetFeatures);
                    }
                }
                bodyStream.SetDefaultBodyFeatures(targetFeatures);
            }
        }
    }

    private void UpdateSkeletonProfile(Astra.BodyStream bodyStream)
    {
        if (ToggleProfileFull != null &&
            ToggleProfileUpperBody != null &&
            ToggleProfileBasic != null)
        {
            Astra.SkeletonProfile targetSkeletonProfile = Astra.SkeletonProfile.Full;
            if (ToggleProfileFull.isOn)
            {
                targetSkeletonProfile = Astra.SkeletonProfile.Full;
            }
            else if (ToggleProfileUpperBody.isOn)
            {
                targetSkeletonProfile = Astra.SkeletonProfile.UpperBody;
            }
            else if (ToggleProfileBasic.isOn)
            {
                targetSkeletonProfile = Astra.SkeletonProfile.Basic;
            }

            if (targetSkeletonProfile != _previousSkeletonProfile)
            {
                _previousSkeletonProfile = targetSkeletonProfile;
                bodyStream.SetSkeletonProfile(targetSkeletonProfile);
            }
        }
    }

    private void UpdateSkeletonOptimization(Astra.BodyStream bodyStream)
    {
        if (ToggleOptimizationAccuracy != null &&
            ToggleOptimizationBalanced != null &&
            ToggleOptimizationMemory != null &&
            SliderOptimization != null)
        {
            int targetOptimizationValue = (int)_previousSkeletonOptimization;
            if (ToggleOptimizationAccuracy.isOn)
            {
                targetOptimizationValue = (int)Astra.SkeletonOptimization.BestAccuracy;
            }
            else if (ToggleOptimizationBalanced.isOn)
            {
                targetOptimizationValue = (int)Astra.SkeletonOptimization.Balanced;
            }
            else if (ToggleOptimizationMemory.isOn)
            {
                targetOptimizationValue = (int)Astra.SkeletonOptimization.MinimizeMemory;
            }

            if (targetOptimizationValue != (int)_previousSkeletonOptimization)
            {
                Debug.Log("Set optimization slider: " + targetOptimizationValue);
                SliderOptimization.value = targetOptimizationValue;
            }

            Astra.SkeletonOptimization targetSkeletonOptimization = Astra.SkeletonOptimization.Balanced;
            int sliderValue = (int)SliderOptimization.value;

            switch (sliderValue)
            {
                case 1:
                    targetSkeletonOptimization = Astra.SkeletonOptimization.Optimization1;
                    break;
                case 2:
                    targetSkeletonOptimization = Astra.SkeletonOptimization.Optimization2;
                    break;
                case 3:
                    targetSkeletonOptimization = Astra.SkeletonOptimization.Optimization3;
                    break;
                case 4:
                    targetSkeletonOptimization = Astra.SkeletonOptimization.Optimization4;
                    break;
                case 5:
                    targetSkeletonOptimization = Astra.SkeletonOptimization.Optimization5;
                    break;
                case 6:
                    targetSkeletonOptimization = Astra.SkeletonOptimization.Optimization6;
                    break;
                case 7:
                    targetSkeletonOptimization = Astra.SkeletonOptimization.Optimization7;
                    break;
                case 8:
                    targetSkeletonOptimization = Astra.SkeletonOptimization.Optimization8;
                    break;
                case 9:
                    targetSkeletonOptimization = Astra.SkeletonOptimization.Optimization9;
                    break;
                default:
                    targetSkeletonOptimization = Astra.SkeletonOptimization.Optimization9;
                    SliderOptimization.value = 9;
                    break;
            }

            if (targetSkeletonOptimization != _previousSkeletonOptimization)
            {
                UpdateOptimizationToggles(targetSkeletonOptimization);

                Debug.Log("SetSkeletonOptimization: " + targetSkeletonOptimization);
                _previousSkeletonOptimization = targetSkeletonOptimization;
                bodyStream.SetSkeletonOptimization(targetSkeletonOptimization);
            }
        }
    }

    private void UpdateOptimizationToggles(Astra.SkeletonOptimization optimization)
    {
        ToggleOptimizationMemory.isOn = optimization == Astra.SkeletonOptimization.MinimizeMemory;
        ToggleOptimizationBalanced.isOn = optimization == Astra.SkeletonOptimization.Balanced;
        ToggleOptimizationAccuracy.isOn = optimization == Astra.SkeletonOptimization.BestAccuracy;
    }
}
