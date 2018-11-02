using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HKUECT;
using UnityEditor;
using UnityEditor.Experimental.Animations;
using UnityEditor.Animations;
using System.IO;

public class MecanimSetupData
{
    public List<Transform> jointTransforms;
    public HumanPose m_humanPose = new HumanPose();
    public Dictionary<System.Int32, GameObject> m_boneObjectMap;
    public Avatar m_srcAvatar;
    public HumanPoseHandler m_srcPoseHandler;
    public HumanPoseHandler m_destPoseHandler;
    public GameObject m_rootObject;
    public Avatar DestinationAvatar;
}

[CustomEditor(typeof(OSCPlayer))]
public class OSCPlayerInspector : Editor
{
    Dictionary<string, GameObject> gameObjectMap;

    Dictionary<string, SkeletonDefinition> skeletons;
    Dictionary<string, MecanimSetupData> mecanimSetups;

    bool flipX = true;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if (GUILayout.Button("Bake to animation"))
        {
            if (Application.isPlaying)
            {
                Debug.LogError("Can't bake during play mode");
                return;
            }

            OSCPlayer p = target as OSCPlayer;

            if (p.mapping == null || p.take == null)
            {
                Debug.LogError("No valid take or mapping assigned");
                return;
            }


            OptiTrackOSCClient client = FindObjectOfType<OptiTrackOSCClient>();
            if (client)
            {
                flipX = client.flipX;
            }
            else
            {
                Debug.LogWarning("No OptitrackOSCClient found. Assuming flipX is true.");
                flipX = true;
            }

            if ( string.IsNullOrEmpty(p.bakeName) ) {
                p.bakeName = p.take.name;
            }

            string fullFolderPath = Path.Combine( Application.dataPath, "VirtualCameraRecorder/BakedOSCAnimations/" + p.bakeName );

            if ( Directory.Exists( fullFolderPath ) ) {
                Debug.LogError("Prefab folder already exists. Please delete this folder if you wish to replace that bake.");
                return;
            }

            GameObject parent = new GameObject(p.bakeName);
            gameObjectMap = new Dictionary<string, GameObject>();
            skeletons = new Dictionary<string, SkeletonDefinition>();
            mecanimSetups = new Dictionary<string, MecanimSetupData>();

            //create recorder and bind everything
            GameObjectRecorder recorder = new GameObjectRecorder(parent);

            //spawn rigidbodies in mapping
            foreach (RigidbodyMap rmap in p.mapping.rigidbodies)
            {
                GameObject rbody = Instantiate(rmap.prefab) as GameObject;
                rbody.name = rmap.name;
                rbody.transform.parent = parent.transform;

                gameObjectMap.Add(rmap.name, rbody);
                recorder.BindComponentsOfType<Transform>(rbody, true);
            }

            //spawn skeletons in mapping
            foreach (SkeletonMap smap in p.mapping.skeletons)
            {
                // Create a lookup from Mecanim anatomy bone names to OptiTrack streaming bone names.
                CacheBoneNameMap(OptitrackBoneNameConvention.Motive, smap.name);

                // Create a hierarchy of GameObjects that will receive the skeletal pose data.
                GameObject skel = Instantiate(smap.prefab) as GameObject;
                gameObjectMap.Add(smap.name, skel);
                skel.name = smap.name;
                skel.transform.parent = parent.transform;

                GameObject skeletonRoot = new GameObject(smap.name + "_root");
                //skeletonRoot.transform.parent = parent.transform;
                skeletonRoot.transform.localPosition = Vector3.zero;
                skeletonRoot.transform.localRotation = Quaternion.identity;

                recorder.BindComponentsOfType<Transform>(skel, true);
            }

            //initial frame setup (this spawns and sorts out skeleton initial conditions as well)
            ApplyFrame(p.take.frameBundles[0], parent.transform);
            long currentTicks = p.take.frameTimes[0];

            //record first frame
            recorder.TakeSnapshot(0);

            //loop from second frame onwards
            for (int i = 1; i < p.take.frameBundles.Count; ++i)
            {
                //apply frame
                ApplyFrame(p.take.frameBundles[i], parent.transform);

                //record frame at correct dt offset since last frame
                float dt = (float)System.TimeSpan.FromTicks(p.take.frameTimes[i] - currentTicks).TotalSeconds;
                recorder.TakeSnapshot(dt);

                //store ticks for next dt
                currentTicks = p.take.frameTimes[i];
            }

            //prepare anim clip for recorder data
            AnimationClip clip = new AnimationClip
            {
                name = p.bakeName
            };
            UnityEditor.AnimationUtility.SetGenerateMotionCurves(clip, true);

            AssetDatabase.CreateFolder("Assets/VirtualCameraRecorder/BakedOSCAnimations", p.bakeName);
            string folder = "Assets/VirtualCameraRecorder/BakedOSCAnimations/" + p.bakeName + "/";

            //create asset for clip
            AssetDatabase.CreateAsset(clip, folder + p.bakeName + ".anim");
            //save clip from recorder
            recorder.SaveToClip(clip);

            //create animator component and save parent object as prefab
            Animator animComponent = parent.AddComponent<Animator>();

			//Turns out we don't need a controller (and it actually gets in the way)
			/*
            AnimatorController animController = new AnimatorController();
            animController.AddLayer("default");
            AnimatorState state = animController.layers[0].stateMachine.AddState("default");
            state.motion = clip;
            animController.layers[0].stateMachine.AddEntryTransition(state);

            //create asset for animController
            AssetDatabase.CreateAsset(animController, folder + p.take.name + ".controller");

            //assign to animComponent
            animComponent.runtimeAnimatorController = animController;
			*/

            //create prefab for entire object
            PrefabUtility.CreatePrefab(folder + p.bakeName + ".prefab", parent);

            AssetDatabase.SaveAssets();
        }
    }

    void ApplyFrame(OSCBundleData frame, Transform parent)
    {
        foreach (OSCMessageData m in frame.Data)
        {
            switch (m.Address)
            {
                case "/rigidBody":
                    ApplyRigidbody(m.Data, parent);
                    break;
                case "/skeleton":
                    ApplySkeleton(m.Data, parent);
                    break;
            }
        }
    }

    void ApplyRigidbody(List<OSCDataInstance> Data, Transform parent)
    {
        //id			0
        //name			1
        //POSITION
        //x				2
        //y				3
        //z				4
        //ROTATION
        //x				5
        //y				6
        //z				7
        //w				8
        //ANGULAR VELOCITY
        //x				9
        //y				10
        //z				11
        //isActive 		12

        //get data
        int index = 0;
        int id = Data[index++].intValue;
        string name = Data[index++].stringValue;

        Vector3 pos;
        pos.x = Data[index++].floatValue;
        pos.y = Data[index++].floatValue;
        pos.z = Data[index++].floatValue;

        Quaternion rot;
        rot.x = Data[index++].floatValue;
        rot.y = Data[index++].floatValue;
        rot.z = Data[index++].floatValue;
        rot.w = Data[index++].floatValue;

        Vector3 angularVelocity;
        angularVelocity.x = Data[index++].floatValue;
        angularVelocity.y = Data[index++].floatValue;
        angularVelocity.z = Data[index++].floatValue;

        bool active = (Data[index++].intValue == 1);

        //apply data
        GameObject target = gameObjectMap[name];
        target.transform.position = parent.rotation * pos + parent.position;
        target.transform.rotation = parent.rotation * rot;

        FixValues(ref pos, ref angularVelocity, ref rot);

        //TODO: Apply active when untracked check
    }

    void ApplySkeleton(List<OSCDataInstance> Data, Transform parent)
    {
        //id			0
        //name			1

        //PER JOINT (until out of range)
        //jointname		2 + jointIndex * 12
        //POSITION
        //x				3
        //y				4				
        //z				5
        //ROTATION
        //x				6
        //y				7
        //z				8
        //w				9
        //parentId		10
        //OFFSET
        //x				11
        //y				12
        //z				13

        int index = 0;

        //get data
        string name = Data[index++].stringValue;
        int id = Data[index++].intValue;

        SkeletonDefinition def;
        bool isNew = false;
        //check if this skeleton is already registered
        if (skeletons.ContainsKey(name))
        {
            def = skeletons[name];
        }
        else
        {
            def = new SkeletonDefinition();
            def.name = name;
            def.id = id;
            skeletons.Add(name, def);
            isNew = true;
        }

        int jointIndex = 0;
        Vector3 vel = Vector3.zero;
        while (index < Data.Count)
        {
            //PER JOINT
            string jointName = Data[index++].stringValue;
            Vector3 pos;
            pos.x = Data[index++].floatValue;
            pos.y = Data[index++].floatValue;
            pos.z = Data[index++].floatValue;

            Quaternion rot;
            rot.x = Data[index++].floatValue;
            rot.y = Data[index++].floatValue;
            rot.z = Data[index++].floatValue;
            rot.w = Data[index++].floatValue;

            //for retargeting
            int parentId = Data[index++].intValue;
            Vector3 offset;
            offset.x = Data[index++].floatValue;
            offset.y = Data[index++].floatValue;
            offset.z = Data[index++].floatValue;

            FixValues(ref pos, ref vel, ref rot);

            if (isNew)
            {
                //add everything
                def.Add(jointName, parent.rotation * pos + parent.position, rot * parent.rotation, offset, parentId);
            }
            else
            {
                //these are the only values that change
                def.positions[jointIndex] = parent.rotation * pos + parent.position;
                def.rotations[jointIndex] = rot * parent.rotation;
            }

            jointIndex++;
        }

        //TODO: Apply data
        bool mecanimSetup = mecanimSetups.ContainsKey(name);
        MecanimSetupData setup;
        if (!mecanimSetup)
        {
            setup = new MecanimSetupData();
            SpawnSkeleton(ref setup, def);
            MecanimSetup(ref setup, def, parent);
            mecanimSetups.Add(name, setup);
        }

        setup = mecanimSetups[name];

        UpdateSkeleton(setup, def);

        // Perform Mecanim retargeting.
        if (setup.m_srcPoseHandler != null && setup.m_destPoseHandler != null)
        {
            // Interpret the streamed pose into Mecanim muscle space representation.
            setup.m_srcPoseHandler.GetHumanPose(ref setup.m_humanPose);
            // Retarget that muscle space pose to the destination avatar.
            setup.m_destPoseHandler.SetHumanPose(ref setup.m_humanPose);
        }
    }

    private void SpawnSkeleton(ref MecanimSetupData setup, SkeletonDefinition def)
    {
        GameObject root = GameObject.Find(def.name + "_root");
        setup.m_rootObject = root;
        setup.DestinationAvatar = gameObjectMap[def.name].GetComponent<Animator>().avatar;
        setup.jointTransforms = new List<Transform>();

        //TODO: Test this
        Transform t;

        for (int i = 0; i < def.positions.Count; ++i)
        {
            //joints
            t = new GameObject(def.names[i]).transform;
            t.parent = setup.m_rootObject.transform;
            setup.jointTransforms.Add(t);
        }

        //update parent structure
        for (int i = 0; i < def.positions.Count; ++i)
        {
            int parentId = def.parentIds[i];
            if (parentId > 0)
            {

                //TODO: Figure out why RShin & RFoot are giving back an incorrect parentId (their own id!)
                //HACK
                if (def.names[i].EndsWith("_RShin") || def.names[i].EndsWith("_RFoot"))
                {
                    parentId--;
                }

                //TODO: Check if this -1 is still correct (it appeared to work, but maybe that was wrong?)
                setup.jointTransforms[i].parent = setup.jointTransforms[parentId - 1];
            }
        }
    }

    private void UpdateSkeleton(MecanimSetupData setup, SkeletonDefinition def)
    {
        for (int i = 0; i < def.positions.Count; ++i)
        {
            setup.jointTransforms[i].position = def.positions[i];
            setup.jointTransforms[i].rotation = def.rotations[i];
        }
    }

    void FixValues(ref Vector3 p, ref Vector3 vel, ref Quaternion rot)
    {
        if (flipX)
        {
            Vector3 euler = rot.eulerAngles;
            euler.y = -euler.y;
            euler.z = -euler.z;
            rot.eulerAngles = euler;

            p.x = -p.x;
            vel.x = -vel.x;
        }
    }

    private Dictionary<string, string> m_cachedMecanimBoneNameMap = new Dictionary<string, string>();

    /// <summary>
    /// Updates the <see cref="m_cachedMecanimBoneNameMap"/> lookup to reflect the specified bone naming convention
    /// and source skeleton asset name.
    /// </summary>
    /// <param name="convention">The bone naming convention to use. Must match the host software.</param>
    /// <param name="assetName">The name of the source skeleton asset.</param>
    private void CacheBoneNameMap(OptitrackBoneNameConvention convention, string assetName)
    {
        m_cachedMecanimBoneNameMap.Clear();

        switch (convention)
        {
            case OptitrackBoneNameConvention.Motive:
                m_cachedMecanimBoneNameMap.Add("Hips", assetName + "_Hip");
                m_cachedMecanimBoneNameMap.Add("Spine", assetName + "_Ab");
                m_cachedMecanimBoneNameMap.Add("Chest", assetName + "_Chest");
                m_cachedMecanimBoneNameMap.Add("Neck", assetName + "_Neck");
                m_cachedMecanimBoneNameMap.Add("Head", assetName + "_Head");
                m_cachedMecanimBoneNameMap.Add("LeftShoulder", assetName + "_LShoulder");
                m_cachedMecanimBoneNameMap.Add("LeftUpperArm", assetName + "_LUArm");
                m_cachedMecanimBoneNameMap.Add("LeftLowerArm", assetName + "_LFArm");
                m_cachedMecanimBoneNameMap.Add("LeftHand", assetName + "_LHand");
                m_cachedMecanimBoneNameMap.Add("RightShoulder", assetName + "_RShoulder");
                m_cachedMecanimBoneNameMap.Add("RightUpperArm", assetName + "_RUArm");
                m_cachedMecanimBoneNameMap.Add("RightLowerArm", assetName + "_RFArm");
                m_cachedMecanimBoneNameMap.Add("RightHand", assetName + "_RHand");
                m_cachedMecanimBoneNameMap.Add("LeftUpperLeg", assetName + "_LThigh");
                m_cachedMecanimBoneNameMap.Add("LeftLowerLeg", assetName + "_LShin");
                m_cachedMecanimBoneNameMap.Add("LeftFoot", assetName + "_LFoot");
                m_cachedMecanimBoneNameMap.Add("RightUpperLeg", assetName + "_RThigh");
                m_cachedMecanimBoneNameMap.Add("RightLowerLeg", assetName + "_RShin");
                m_cachedMecanimBoneNameMap.Add("RightFoot", assetName + "_RFoot");
                m_cachedMecanimBoneNameMap.Add("LeftToeBase", assetName + "_LToe");
                m_cachedMecanimBoneNameMap.Add("RightToeBase", assetName + "_RToe");

                m_cachedMecanimBoneNameMap.Add("Left Thumb Proximal", assetName + "_LThumb1");
                m_cachedMecanimBoneNameMap.Add("Left Thumb Intermediate", assetName + "_LThumb2");
                m_cachedMecanimBoneNameMap.Add("Left Thumb Distal", assetName + "_LThumb3");
                m_cachedMecanimBoneNameMap.Add("Right Thumb Proximal", assetName + "_RThumb1");
                m_cachedMecanimBoneNameMap.Add("Right Thumb Intermediate", assetName + "_RThumb2");
                m_cachedMecanimBoneNameMap.Add("Right Thumb Distal", assetName + "_RThumb3");

                m_cachedMecanimBoneNameMap.Add("Left Index Proximal", assetName + "_LIndex1");
                m_cachedMecanimBoneNameMap.Add("Left Index Intermediate", assetName + "_LIndex2");
                m_cachedMecanimBoneNameMap.Add("Left Index Distal", assetName + "_LIndex3");
                m_cachedMecanimBoneNameMap.Add("Right Index Proximal", assetName + "_RIndex1");
                m_cachedMecanimBoneNameMap.Add("Right Index Intermediate", assetName + "_RIndex2");
                m_cachedMecanimBoneNameMap.Add("Right Index Distal", assetName + "_RIndex3");

                m_cachedMecanimBoneNameMap.Add("Left Middle Proximal", assetName + "_LMiddle1");
                m_cachedMecanimBoneNameMap.Add("Left Middle Intermediate", assetName + "_LMiddle2");
                m_cachedMecanimBoneNameMap.Add("Left Middle Distal", assetName + "_LMiddle3");
                m_cachedMecanimBoneNameMap.Add("Right Middle Proximal", assetName + "_RMiddle1");
                m_cachedMecanimBoneNameMap.Add("Right Middle Intermediate", assetName + "_RMiddle2");
                m_cachedMecanimBoneNameMap.Add("Right Middle Distal", assetName + "_RMiddle3");

                m_cachedMecanimBoneNameMap.Add("Left Ring Proximal", assetName + "_LRing1");
                m_cachedMecanimBoneNameMap.Add("Left Ring Intermediate", assetName + "_LRing2");
                m_cachedMecanimBoneNameMap.Add("Left Ring Distal", assetName + "_LRing3");
                m_cachedMecanimBoneNameMap.Add("Right Ring Proximal", assetName + "_RRing1");
                m_cachedMecanimBoneNameMap.Add("Right Ring Intermediate", assetName + "_RRing2");
                m_cachedMecanimBoneNameMap.Add("Right Ring Distal", assetName + "_RRing3");

                m_cachedMecanimBoneNameMap.Add("Left Little Proximal", assetName + "_LPinky1");
                m_cachedMecanimBoneNameMap.Add("Left Little Intermediate", assetName + "_LPinky2");
                m_cachedMecanimBoneNameMap.Add("Left Little Distal", assetName + "_LPinky3");
                m_cachedMecanimBoneNameMap.Add("Right Little Proximal", assetName + "_RPinky1");
                m_cachedMecanimBoneNameMap.Add("Right Little Intermediate", assetName + "_RPinky2");
                m_cachedMecanimBoneNameMap.Add("Right Little Distal", assetName + "_RPinky3");
                break;
            case OptitrackBoneNameConvention.FBX:
                m_cachedMecanimBoneNameMap.Add("Hips", assetName + "_Hips");
                m_cachedMecanimBoneNameMap.Add("Spine", assetName + "_Spine");
                m_cachedMecanimBoneNameMap.Add("Chest", assetName + "_Spine1");
                m_cachedMecanimBoneNameMap.Add("Neck", assetName + "_Neck");
                m_cachedMecanimBoneNameMap.Add("Head", assetName + "_Head");
                m_cachedMecanimBoneNameMap.Add("LeftShoulder", assetName + "_LeftShoulder");
                m_cachedMecanimBoneNameMap.Add("LeftUpperArm", assetName + "_LeftArm");
                m_cachedMecanimBoneNameMap.Add("LeftLowerArm", assetName + "_LeftForeArm");
                m_cachedMecanimBoneNameMap.Add("LeftHand", assetName + "_LeftHand");
                m_cachedMecanimBoneNameMap.Add("RightShoulder", assetName + "_RightShoulder");
                m_cachedMecanimBoneNameMap.Add("RightUpperArm", assetName + "_RightArm");
                m_cachedMecanimBoneNameMap.Add("RightLowerArm", assetName + "_RightForeArm");
                m_cachedMecanimBoneNameMap.Add("RightHand", assetName + "_RightHand");
                m_cachedMecanimBoneNameMap.Add("LeftUpperLeg", assetName + "_LeftUpLeg");
                m_cachedMecanimBoneNameMap.Add("LeftLowerLeg", assetName + "_LeftLeg");
                m_cachedMecanimBoneNameMap.Add("LeftFoot", assetName + "_LeftFoot");
                m_cachedMecanimBoneNameMap.Add("RightUpperLeg", assetName + "_RightUpLeg");
                m_cachedMecanimBoneNameMap.Add("RightLowerLeg", assetName + "_RightLeg");
                m_cachedMecanimBoneNameMap.Add("RightFoot", assetName + "_RightFoot");
                m_cachedMecanimBoneNameMap.Add("LeftToeBase", assetName + "_LeftToeBase");
                m_cachedMecanimBoneNameMap.Add("RightToeBase", assetName + "_RightToeBase");
                break;
            case OptitrackBoneNameConvention.BVH:
                m_cachedMecanimBoneNameMap.Add("Hips", assetName + "_Hips");
                m_cachedMecanimBoneNameMap.Add("Spine", assetName + "_Chest");
                m_cachedMecanimBoneNameMap.Add("Chest", assetName + "_Chest2");
                m_cachedMecanimBoneNameMap.Add("Neck", assetName + "_Neck");
                m_cachedMecanimBoneNameMap.Add("Head", assetName + "_Head");
                m_cachedMecanimBoneNameMap.Add("LeftShoulder", assetName + "_LeftCollar");
                m_cachedMecanimBoneNameMap.Add("LeftUpperArm", assetName + "_LeftShoulder");
                m_cachedMecanimBoneNameMap.Add("LeftLowerArm", assetName + "_LeftElbow");
                m_cachedMecanimBoneNameMap.Add("LeftHand", assetName + "_LeftWrist");
                m_cachedMecanimBoneNameMap.Add("RightShoulder", assetName + "_RightCollar");
                m_cachedMecanimBoneNameMap.Add("RightUpperArm", assetName + "_RightShoulder");
                m_cachedMecanimBoneNameMap.Add("RightLowerArm", assetName + "_RightElbow");
                m_cachedMecanimBoneNameMap.Add("RightHand", assetName + "_RightWrist");
                m_cachedMecanimBoneNameMap.Add("LeftUpperLeg", assetName + "_LeftHip");
                m_cachedMecanimBoneNameMap.Add("LeftLowerLeg", assetName + "_LeftKnee");
                m_cachedMecanimBoneNameMap.Add("LeftFoot", assetName + "_LeftAnkle");
                m_cachedMecanimBoneNameMap.Add("RightUpperLeg", assetName + "_RightHip");
                m_cachedMecanimBoneNameMap.Add("RightLowerLeg", assetName + "_RightKnee");
                m_cachedMecanimBoneNameMap.Add("RightFoot", assetName + "_RightAnkle");
                m_cachedMecanimBoneNameMap.Add("LeftToeBase", assetName + "_LeftToe");
                m_cachedMecanimBoneNameMap.Add("RightToeBase", assetName + "_RightToe");
                break;
        }
    }

    /// <summary>
    /// Constructs the source Avatar and pose handlers for Mecanim retargeting.
    /// </summary>
    /// <param name="rootObjectName"></param>
    private void MecanimSetup(ref MecanimSetupData setup, SkeletonDefinition skeleton, Transform animationParent)
    {
        string[] humanTraitBoneNames = HumanTrait.BoneName;

        // Set up the mapping between Mecanim human anatomy and OptiTrack skeleton representations.
        List<HumanBone> humanBones = new List<HumanBone>(skeleton.Count);
        for (int humanBoneNameIdx = 0; humanBoneNameIdx < humanTraitBoneNames.Length; ++humanBoneNameIdx)
        {
            string humanBoneName = humanTraitBoneNames[humanBoneNameIdx];
            if (m_cachedMecanimBoneNameMap.ContainsKey(humanBoneName))
            {
                HumanBone humanBone = new HumanBone();
                humanBone.humanName = humanBoneName;
                humanBone.boneName = m_cachedMecanimBoneNameMap[humanBoneName];
                humanBone.limit.useDefaultValues = true;

                humanBones.Add(humanBone);
            }
        }

        // Set up the T-pose and game object name mappings.
        List<SkeletonBone> skeletonBones = new List<SkeletonBone>(skeleton.Count + 1);

        // Special case: Create the root bone.
        {
            SkeletonBone rootBone = new SkeletonBone();
            rootBone.name = setup.m_rootObject.name;
            rootBone.position = Vector3.zero;
            rootBone.rotation = Quaternion.identity;
            rootBone.scale = Vector3.one;

            skeletonBones.Add(rootBone);
        }

        // Create remaining retargeted bone definitions.
        for (int boneDefIdx = 0; boneDefIdx < skeleton.Count; ++boneDefIdx)
        {
            //OptitrackSkeletonDefinition.BoneDefinition boneDef = m_skeletonDef.Bones[boneDefIdx];

            SkeletonBone skelBone = new SkeletonBone();
            skelBone.name = skeleton.names[boneDefIdx];
            skelBone.position = skeleton.offsets[boneDefIdx];
            skelBone.rotation = Quaternion.identity;
            skelBone.scale = Vector3.one;

            skeletonBones.Add(skelBone);
        }

        // Now set up the HumanDescription for the retargeting source Avatar.
        HumanDescription humanDesc = new HumanDescription();
        humanDesc.human = humanBones.ToArray();
        humanDesc.skeleton = skeletonBones.ToArray();

        // These all correspond to default values.
        humanDesc.upperArmTwist = 0.5f;
        humanDesc.lowerArmTwist = 0.5f;
        humanDesc.upperLegTwist = 0.5f;
        humanDesc.lowerLegTwist = 0.5f;
        humanDesc.armStretch = 0.05f;
        humanDesc.legStretch = 0.05f;
        humanDesc.feetSpacing = 0.0f;
        humanDesc.hasTranslationDoF = false;

        // Finally, take the description and build the Avatar and pose handlers.
        setup.m_srcAvatar = AvatarBuilder.BuildHumanAvatar(setup.m_rootObject, humanDesc);

        if (setup.m_srcAvatar.isValid == false || setup.m_srcAvatar.isHuman == false)
        {
            Debug.LogError(GetType().FullName + ": Unable to create source Avatar for retargeting. Check that your Skeleton Asset Name and Bone Naming Convention are configured correctly.", this);
            //this.enabled = false;
            return;
        }

        setup.m_srcPoseHandler = new HumanPoseHandler(setup.m_srcAvatar, setup.m_rootObject.transform);
        setup.m_destPoseHandler = new HumanPoseHandler(setup.DestinationAvatar, gameObjectMap[skeleton.name].transform);

        //parent the root object to the animation take root
        setup.m_rootObject.transform.parent = animationParent;
    }
}