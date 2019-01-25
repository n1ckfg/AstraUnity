using UnityEngine;
using Astra;
using System.Collections.Generic;

public class PoseUser : MonoBehaviour
{
    public Transform RootBone;
    public Transform[] Joints;

    JointType[] avaliableJointTypes = new JointType[]{
        JointType.Head,
        JointType.ShoulderSpine,
        JointType.LeftShoulder,
        JointType.LeftElbow,
        JointType.LeftHand,
        JointType.RightShoulder,
        JointType.RightElbow,
        JointType.RightHand,
        JointType.MidSpine,
        JointType.BaseSpine,
        JointType.LeftHip,
        JointType.LeftKnee,
        JointType.LeftFoot,
        JointType.RightHip,
        JointType.RightKnee,
        JointType.RightFoot,
        JointType.LeftWrist,
        JointType.RightWrist,
        JointType.Neck,
    };

    private long _lastFrameIndex;
    private Astra.Body[] _bodies = new Astra.Body[2];

    private Vector3[] _initialPositions;
    private Quaternion[] _initialRotations;
    private Quaternion[] _currentRotations;
    private Quaternion[] _boneRotation;

    float RotationDamping = 30.0f;

    void Awake()
    {
        _initialPositions = new Vector3[avaliableJointTypes.Length];
        _initialRotations = new Quaternion[avaliableJointTypes.Length];
        _currentRotations = new Quaternion[avaliableJointTypes.Length];
        _boneRotation = new Quaternion[avaliableJointTypes.Length];

        for (int i = 0; i < Joints.Length; ++i)
        {
            _initialPositions[i] = Joints[i].position;
            _initialRotations[i] = Joints[i].rotation;
            _currentRotations[i] = Joints[i].rotation;
        }
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

        var body = GetFirstBody(_bodies);
        if (body != null)
        {
            UpdateBone(body);
        }
    }

    Quaternion GetRotation(Astra.Joint jo)
    {
        Vector3 jointUp = new Vector3(jo.Orientation.M01,
                                      jo.Orientation.M11,
                                      jo.Orientation.M21);
        Vector3 jointForward = new Vector3(jo.Orientation.M02,
                                           jo.Orientation.M12,
                                           jo.Orientation.M22);
        return Quaternion.LookRotation(jointForward, jointUp);
    }
    
    private void UpdateBone(Astra.Body body)
    {
        for (int i = 0; i < body.Joints.Length; ++i)
        {
            _boneRotation[i] = GetRotation(body.Joints[i]);
            Joints[i].transform.rotation = _currentRotations[i] = 
                Quaternion.Slerp(_currentRotations[i], _boneRotation[i] * _initialRotations[i], Time.deltaTime * RotationDamping);
        }
    }

    private Astra.Body GetFirstBody(Astra.Body[] bodies)
    {
        if (bodies.Length == 0)
        {
            return null;
        }

        var body = bodies[0];
        if (body.Id == 0)
        {
            return null;
        }
        return body;
    }

}
