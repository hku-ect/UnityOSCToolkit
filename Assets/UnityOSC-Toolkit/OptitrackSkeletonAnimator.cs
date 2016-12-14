//=========================================================================================================
// Copyright 2016, NaturalPoint Inc.
//
//	- Adopted for ECT OSC Setup by Aaron (was part of NaturalMotion's Unity example)
//		Primary changes are a move away from NatNet to OSC data (NaturalPoint's NatNet SDK is windows only)
//		This is tailored to work with the NatNet2OSCBridge application on http://github.com/hku-ect
//=========================================================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace HKUECT {
	public enum OptitrackBoneNameConvention {
		FBX,
		BVH,
		Motive
	}

	/// <summary>
	/// Implements live retargeting of streamed OptiTrack skeletal data via Mecanim.
	/// Add this component to an imported model GameObject that has a properly configured humanoid Avatar definition.
	/// </summary>
	/// <remarks>
	/// A hierarchy of GameObjects (see <see cref="m_rootObject"/> and <see cref="m_boneObjectMap"/>) will be created to
	/// receive the streaming pose data for the skeleton asset specified by <see cref="SkeletonAssetName"/>.
	/// A source Avatar will be created automatically from the streamed skeleton definition.
	/// </remarks>
	public class OptitrackSkeletonAnimator : MonoBehaviour {
		/// <summary>The name of the skeleton asset in the stream that will provide retargeting source data.</summary>
		public string SkeletonAssetName = "Skeleton1";

		/// <summary>The humanoid avatar for this GameObject's imported model.</summary>
		public Avatar DestinationAvatar;

		#region Private fields

		/// <summary>Used when retrieving and retargeting source pose. Cached and reused for efficiency.</summary>
		private HumanPose m_humanPose = new HumanPose();

		/// <summary>The streamed source skeleton definition.</summary>
		//private OptitrackSkeletonDefinition m_skeletonDef;

		/// <summary>The root GameObject of the streamed skeletal pose transform hierarchy.</summary>
		private GameObject m_rootObject;
		private List<Transform> jointTransforms = new List<Transform>();

		/// <summary>Maps between OptiTrack skeleton bone IDs and corresponding GameObjects.</summary>
		private Dictionary<Int32, GameObject> m_boneObjectMap;

		/// <summary>
		/// Maps between Mecanim anatomy bone names (keys) and streamed bone names from various naming conventions
		/// supported by the OptiTrack software (values). Populated by <see cref="CacheBoneNameMap"/>.
		/// </summary>
		private Dictionary<string, string> m_cachedMecanimBoneNameMap = new Dictionary<string, string>();

		/// <summary>Created automatically based on <see cref="m_skeletonDef"/>.</summary>
		private Avatar m_srcAvatar;

		/// <summary>Set up to read poses from our streamed GameObject transform hierarchy.</summary>
		private HumanPoseHandler m_srcPoseHandler;

		/// <summary>Set up to write poses to this GameObject using <see cref="DestinationAvatar"/>.</summary>
		private HumanPoseHandler m_destPoseHandler;

		private bool mecanimSetUp = false;

		private GameObject[] subObjects;

		private SkeletonDefinition skeleton;

		#endregion Private fields

		#region HKU-Additions

		void Start() {
			subObjects = new GameObject[transform.childCount];
			for (int i = 0; i < subObjects.Length; ++i) {
				subObjects [i] = transform.GetChild(i).gameObject;
				subObjects [i].SetActive(false);
			}

			// Create a lookup from Mecanim anatomy bone names to OptiTrack streaming bone names.
			CacheBoneNameMap(OptitrackBoneNameConvention.Motive, this.SkeletonAssetName);

			// Create a hierarchy of GameObjects that will receive the skeletal pose data.
			string rootObjectName = this.SkeletonAssetName;
			m_rootObject = new GameObject(rootObjectName);

			m_rootObject.transform.localPosition = Vector3.zero;
			m_rootObject.transform.localRotation = Quaternion.identity;
		}

		void Update() {
			if (OptiTrackOSCClient.GetSkeleton(SkeletonAssetName, out skeleton)) {
				if (!mecanimSetUp) {
					//TODO: Spawn skeleton objects
					SpawnSkeleton();

					for (int i = 0; i < subObjects.Length; ++i) {
						subObjects [i].SetActive(true);
					}

					MecanimSetup(SkeletonAssetName, skeleton);
					mecanimSetUp = true;
				}

				UpdateSkeleton();

				// Perform Mecanim retargeting.
				if (m_srcPoseHandler != null && m_destPoseHandler != null) {
					// Interpret the streamed pose into Mecanim muscle space representation.
					m_srcPoseHandler.GetHumanPose(ref m_humanPose);

					// Retarget that muscle space pose to the destination avatar.
					m_destPoseHandler.SetHumanPose(ref m_humanPose);
				}
			} else if (skeleton == null && mecanimSetUp) {
				//object has been destroyed... so... reset I guess
				mecanimSetUp = false;
				for (int i = 0; i < subObjects.Length; ++i) {
					subObjects [i].SetActive(false);
				}
			}
		}

		private void SpawnSkeleton() {
			//TODO: Test this
			Transform t;

			for (int i = 0; i < skeleton.positions.Count; ++i) {
				//joints
				GameObject g = GameObject.Find(skeleton.names [i]);

				if (g) {
					t = g.transform;
				} else {
					t = new GameObject(skeleton.names [i]).transform;
				}
				t.parent = m_rootObject.transform;
				jointTransforms.Add(t);
			}

			//update parent structure
			for (int i = 0; i < skeleton.positions.Count; ++i) {
				int parentId = skeleton.parentIds [i];
				if (parentId > 0) {
					
					//TODO: Figure out why RShin & RFoot are giving back an incorrect parentId (their own id!)
					//HACK
					if (skeleton.names [i].EndsWith("_RShin") || skeleton.names [i].EndsWith("_RFoot")) {
						parentId--;
					}

					//TODO: Check if this -1 is still correct (it appeared to work, but maybe that was wrong?)
					jointTransforms [i].parent = jointTransforms [parentId - 1];
				}
			}
		}

		private void UpdateSkeleton() {
			for (int i = 0; i < skeleton.positions.Count; ++i) {
				jointTransforms [i].position = skeleton.positions [i];
				jointTransforms [i].rotation = skeleton.rotations [i];
			}
		}

		#endregion

		#region Private methods

		/// <summary>
		/// Constructs the source Avatar and pose handlers for Mecanim retargeting.
		/// </summary>
		/// <param name="rootObjectName"></param>
		private void MecanimSetup(string rootObjectName, SkeletonDefinition skeleton) {
			string[] humanTraitBoneNames = HumanTrait.BoneName;

			// Set up the mapping between Mecanim human anatomy and OptiTrack skeleton representations.
			List<HumanBone> humanBones = new List<HumanBone>(skeleton.Count);
			for (int humanBoneNameIdx = 0; humanBoneNameIdx < humanTraitBoneNames.Length; ++humanBoneNameIdx) {
				string humanBoneName = humanTraitBoneNames [humanBoneNameIdx];
				if (m_cachedMecanimBoneNameMap.ContainsKey(humanBoneName)) {
					HumanBone humanBone = new HumanBone();
					humanBone.humanName = humanBoneName;
					humanBone.boneName = m_cachedMecanimBoneNameMap [humanBoneName];
					humanBone.limit.useDefaultValues = true;

					humanBones.Add(humanBone);
				}
			}

			// Set up the T-pose and game object name mappings.
			List<SkeletonBone> skeletonBones = new List<SkeletonBone>(skeleton.Count + 1);

			// Special case: Create the root bone.
			{
				SkeletonBone rootBone = new SkeletonBone();
				rootBone.name = rootObjectName;
				rootBone.position = Vector3.zero;
				rootBone.rotation = Quaternion.identity;
				rootBone.scale = Vector3.one;

				skeletonBones.Add(rootBone);
			}

			// Create remaining retargeted bone definitions.
			for (int boneDefIdx = 0; boneDefIdx < skeleton.Count; ++boneDefIdx) {
				//OptitrackSkeletonDefinition.BoneDefinition boneDef = m_skeletonDef.Bones[boneDefIdx];

				SkeletonBone skelBone = new SkeletonBone();
				skelBone.name = skeleton.names [boneDefIdx];
				skelBone.position = skeleton.offsets [boneDefIdx];
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
			m_srcAvatar = AvatarBuilder.BuildHumanAvatar(m_rootObject, humanDesc);

			if (m_srcAvatar.isValid == false || m_srcAvatar.isHuman == false) {
				Debug.LogError(GetType().FullName + ": Unable to create source Avatar for retargeting. Check that your Skeleton Asset Name and Bone Naming Convention are configured correctly.", this);
				this.enabled = false;
				return;
			}

			m_srcPoseHandler = new HumanPoseHandler(m_srcAvatar, m_rootObject.transform);
			m_destPoseHandler = new HumanPoseHandler(DestinationAvatar, this.transform);
		}


		/// <summary>
		/// Updates the <see cref="m_cachedMecanimBoneNameMap"/> lookup to reflect the specified bone naming convention
		/// and source skeleton asset name.
		/// </summary>
		/// <param name="convention">The bone naming convention to use. Must match the host software.</param>
		/// <param name="assetName">The name of the source skeleton asset.</param>
		private void CacheBoneNameMap(OptitrackBoneNameConvention convention, string assetName) {
			m_cachedMecanimBoneNameMap.Clear();

			switch (convention) {
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

		#endregion Private methods
	}
}