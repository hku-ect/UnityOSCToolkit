# UnityOSCToolkit
Unity client implementation for the NatNet2OSCBridge. Supports Rigidbodies &amp; Skeletons (specifically in "Full Skeleton" mode).

This set of code is a refactor of the code built for the VR & Mocap lab run in November of 2016 at the Expertise Centre for Creative Technology at the HKU.

Currently built on Unity 5.5 (but should work fine with any version of Unity 5)

Included is a modified version of the UnityOSC library (https://github.com/jorgegarcia/UnityOSC) that adds the ability to create/send/receive bundles.

## How To Use

Since this implementation relies heavily on the NatNet2OSCBridge, make sure you've set that up first, which means you'll need the following:

* A computer running Motive
* A live or pre-recorded motion capture take
* A computer running the NatNet2OSCBridge (currently untested on Windows)
* (Optional) A computer running the GearVRHandshaker (only necessary if you intend to use GearVRs and/or vertical walking)

Once you have these things setup, you can download or clone this repository and open it using Unity 5.5+

### Class Overview

#### OptiTrackOSCClient
This class receives and stores data from the NatNet2OSCBridge ofxApp. It does nothing on its own, instead relying on other classes to use the data it has stored. The only important setting to note is the <b>flip x</b> option, because in our case the x-axis information from Motive is the reverse of what Unity expects. If you notice rotations or positions are incorrectly applied along the x-axis, toggle this setting.

#### OptitrackRigidbody
The simplest way to track a rigidbody (by name) and have its position/rotation set relative to the Client object.

#### OptitrackRigidbodyGroup
Exactly like the above, but allows you to do it with a list, speeding up the process and cleaning up the scene. This class will create individual OptitrackRigidbody instances once the application is running.

#### OptitrackSelectiveRigidbody
Similar to the regular rigidbody, but also containing options to ignore position/rotation axes (or entirely). We've used this to create a 'moving wall', which ignored rotation and only moved on the X and Z plane.

#### OptitrackSkeletonAnimator
Requires Full-Skeleton (a client setting in NatNet2OSCBridge) data to map incoming motion capture from a suit to a character in Unity. Currently still requires the full suit to work, but we're looking for ways to make this possible with a smaller number of rigidbodies. <b>Please note</b> that scaling the target actor in the scene will impact positional information, so do any necessary scaling through the import settings of the character model (and not in the scene).

#### OptiTrackOSCGearVR
Specific type of rigidbody for use with GearVR's. The major differences are:
* Rotation is ignored (because the GearVR handles this)
* Vertical walking can be enabled
* Your virtual y-position (height) is sent to the Handshaker (you may also receive position updates for other GearVR objects that you are not controlling)

#### GearVRHandshaker
Used to handshake with the GearVRHandshaker. It contains success/failure events, and you can have it automatically load a target scene. <b>Please note</b> that it will not start itself, so as an example we've included a "HandshakeKicker" script that demonstrates how you could use it. The handshake-scene is very similar to how we've used this before (an empty scene that handhsakes, before kicking off the experience).

### Use Cases

The toolkit allows you to do, at least, the following things:

* Vertical walking (y-only sync)
* Matching “head & hands” when vertical walking
* Handshaking Gear VRs (relies on Handshaker also present on this github)
* Mapping full OSC Skeleton data onto Humanoid Mecanim Avatars
* Tracking Rigidbodies as predefined prefabs (single/groups)
* Selectively tracking Rigidbodies as predefined prefabs

You can find each of these in the example scene that's part of the source, and we'll quickly go over how each of these is set up in that scene.

#### Vertical walking

You can use the <b>OptiTrackOSCGearVR</b> class to use Motion Capture in combination with a GearVR, or any other similar type of situation.

Vertical walking means you can move up/down in virtual space, whilst walking on a flat surface in the real world. This works through a combination of raycasting and gravity. As you walk, the script will check if the floor below your feet is moving towards you (this requires the collider to be on the layer "Floor" that we've set up). If so, it will push you up accordingly.

If you set the booleans in the inspector to allow for falling (with or without gravitational accelleration), you can also step off of ledges and fall as you would expect. Note that this position is attached to the head of the player, so looking over the edge will make you fall down as well.

### Matching Heads/Hands

One of the challenges with vertical walking is to make sure you don't detach the player's head (the GearVR) from other objects he/she is holding. To solve this we've added the ability to set <b>relative bodies</b> (OptitrackRigidbody) on the OptitrackOSCGearVR class. Once these other objects are tracked, they will ping the GearVR to update them to the correct relative height.

### Handshaking Gear VRs
The GearVRHandshaker script will allow you to communicate with the GearVR-OSC-Handshaker ofxApp, and this is to let you know who you are.

Imagine being in the space with four people, all running the same Unity scene, but each of them needs to be linked to a different tracked rigidbody (their headphones, for example). A reliable way to link a GearVR to a rigidbody, is to use a static IP-Address of the phones' wifi connection (that we use to send them the OSC data).

Once the Handshaker is "kicked", it sends a handshake message to the ofxApp (you need to set the IP of that computer in the scene). If the ofxApp recognizes the IP-Address, it will send back the appropriate rigidbody name. Once that name is received, the Unity application knows who it is, and can act accordingly. So to recap, make sure:

* Your IP-Address doesn't change
* You have the correct Handshake IP set in the scene (of the computer running the Handshaker ofxApp)
* Both the Handshaker ofxApp and the NatNet2OSCBridge have your computer's IP-Address set-up

### Mapping full OSC Skeleton data onto Humanoid Mecanim Avatars
If you have pre-recorded data or have put a person into a full mocap suit, you can map this data to an Avatar in Unity. This avatar needs to be a humanoid character, and the character model must be imported in this way for it to work.

The OptitrackSkeletonAnimator should replace the SkeletonAnimator component that an imported character contains when placed in the scene, and once the correct Avatar is linked should do its thing entirely unattended.

### Tracking Rigidbodies as predefined prefabs (single/groups)
Once you've added rigidbodies to Motive, you can link these to prefabs in the Unity project, either as a group or as invididual objects. The group setup is especially useful if you have a large number of objects that might clutter the scene unnecessarily, and don't require any special interaction or setup. An example of special interaction would be if they are linked as "relative bodies" to a GearVR.

Setting these up is done as follows:
* Create an empty GameObject
* Add the appropriate script (OptitrackRigidbody or OptitrackRigidbodyGroup)
* Set the correct rigidobdy name(s) and prefab(s)

### Selectively tracking Rigidbodies as predefined prefabs
If you want to track rigidbodies in a specific way (only rotations or positions, or only some axes), you can use the OptitrackSelectiveRigidbody script. Set it up as you would a normal rigidbody, and simply apply the settings that you want.
