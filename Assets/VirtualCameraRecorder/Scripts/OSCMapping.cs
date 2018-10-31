using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HKUECT {
	[CreateAssetMenu]
	public class OSCMapping : ScriptableObject {
		public List<RigidbodyMap> rigidbodies = new List<RigidbodyMap>();
		public List<SkeletonMap> skeletons = new List<SkeletonMap>();

		public void Spawn( GameObject parent ) {
			OptitrackRigidbodyGroup rGroup = parent.AddComponent<OptitrackRigidbodyGroup>();
			foreach( RigidbodyMap rmap in rigidbodies ) {
				//create map for rigidbody group
				NamePrefabMap npmap = new NamePrefabMap();
				npmap.deactivateWhenMissing = rmap.deactivateWhenMissing;
				npmap.deactivateWhenUntracked = rmap.deactivateWhenUntracked;
				npmap.name = rmap.name;
				npmap.prefab = rmap.prefab;

				//add to list (group will spawn separate objects on Start)
				rGroup.objects.Add( npmap );
			}

			foreach( SkeletonMap smap in skeletons ) {
				//spawn model and remove existing animator
				GameObject g = (GameObject)Instantiate(smap.prefab);
				Destroy( g.GetComponent<Animator>() );

				//add optitrack animator, set values
				OptitrackSkeletonAnimator skelAnim = g.AddComponent<OptitrackSkeletonAnimator>();
				skelAnim.DestinationAvatar = smap.avatar;
				skelAnim.SkeletonAssetName = smap.name;
			}
		}
	}
}