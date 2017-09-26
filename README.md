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

# How to run a Unity application on the GearVR

First let’s download and instal the prerequisites:

1. Make sure you have [Unity installed with android built support](https://unity3d.com/get-unity/download)
   *As of writing version 2017.1.1f1*
2. Download and install [Android studio](https://developer.android.com/studio/index.html), so Unity can use the Android SDK
   * Also note/save the location where the SDK is installed you need to tell this to Unity
3. Download and install the [Java Development Kit](http://www.oracle.com/technetwork/java/javase/downloads/index-jsp-138363.html)
   *As of writing version 9*
4. Download an install [Oculus mobile sdk](https://developer3.oculus.com/downloads/)
   *As of writing version 1.18, this is a UnityPackage which you'll need to import to your Unity project (Assets - Import Package - Custom Package)*
5. Get the OSIG signature of your android phone (https://dashboard.oculus.com/tools/osig-generator/)
6. Copy th OSIG file to: Project/Assets/Plugins/Android/assets/
 * Please note that you need a Oculus account to acces the page, which is free
 * you need to run de ADB tool from a terminal/command prompt. You can find the command in de android SDK directory and then “platform-tools"

Then let’s start in Unity:

1. Start new 3D Unity project
2. Go to “Edit” -> “Project Settings” -> “Player Settings"
3. In the Settings for Android:
  * select box: "Virtual Reality Supported” (you should see Virtual Reality SDK’s -> Oculus if not make sure you installed the Oculus mobile SDK)
  * Set your bundle identifier, for example com.mycompanyname.projectname
4. Create your Unity Scene
5. Go to “File” -> “build settings” and switch platform to “Android"
6. Set texture compression to “ETC2 (GL 3.0)"
7. Make sure the Android phone is connected to your computer with the USB cable and enable USB Debugging (http://www.greenbot.com/article/2457986/how-to-enable-developer-options-on-your-android-phone-or-tablet.html)
8. Click “Build and Run” and the scene will built on your phone.
9. When the app starts for the first time you might need to allow acces for the app on the phone.

## Troubleshooting How to run a Unity application on the GearVR

If you get the folowing error: 
*"Unable to list target platforms. Please make sure the android sdk path is correct. See the Console for more details."*
* If you already have the OVR package in a Unity project you can find its version number in the OVR/Scripts/OVRPlugin.cs file

Follow these instructions:

* http://answers.unity3d.com/questions/1323731/unable-to-list-target-platforms-please-make-sure-t.html
* Url sdk tools Windows: http://dl-ssl.google.com/android/repository/tools_r25.2.5-windows.zip
* Url sdk tools OSX: https://dl.google.com/android/repository/tools_r25.2.3-macosx.zip

# Troubleshooting UnityOSCToolkit

## Help! I'm receiving objects with the wrong name

This means a Rigidbody/Skeleton was removed from the Motive project, causing an id-reshuffle that creates this particular problem. There is an easy, but cumbersome fix: Create a new Motive project, and re-add all of the Rigidbodies / Skeletons (sorry!)

So, remember: don't remove Skeletons/Rigidbodies from a Motive project.

## I want to link my PC / GearVR to a headset in Unity

Make sure of the following:

1. Your IP is on the 10.200.200.xxx range
2. You have added this IP to the Handshaker ofxApp (we run this on the big Mac)
3. Make sure you have added it with the correct name of the headset (same as the Rigidbody in Motive)
4. You are running from the "handshake_scene" (in our Examples folder)
5. You have dragged the correct scene onto the Handshaker script (on the Main Camera of the handshake_scene)
6. If on the GearVR: You've tried turning the WiFi off/on.

You can confirm if you are "handshaking" successfully (and with what headset) on the Handshaker ofxApp. It will register handshake attempts, and confirm if they are either successful or unsuccessful.

## Handshaking is not working and/or I'm not receiving any OSC data
Make absolutely sure of the following:

1. Your IP is on the 10.200.200.xxx range
2. This IP is added to the Handshaker and NatNet2OSCBridge ofxApps

If you are on Windows you should <b>try turning off your firewall</b>, since it may be blocking incoming OSC messages.

## Handshaking takes a long time
This probably means there are a lot of devices on the WiFi network, and the UDP packets sent by the handshaker are being lost along the way. 

Try reducing the number of devices on the network (if possible), by turning WiFi modules of devices like phones off (if you're not using them of course).

Try restarting the Handshaker ofxApp.

## Objects/GearVR are moving way too much / too little in my Unity scene
You've probably mismatched the scale of your scene. The default is <b>1 meter = 1 unit</b>, but you can change this by setting the <b>World Scale</b> of the OSCClient script to a number larger/smaller than 1. Bigger will make you move more than you are in real life, smaller will make you move less.

As a general rule, try to avoid doing this. It's always better to make sure you use the correct 1 meter = 1 unit scale, than scaling the input (unless you really have to for some sort of effect).

## A motion-captured object is quickly glitching / teleporting
This probably means there are two similar objects, and Motive cannot clearly distinguish between them. Change the marker-setup of the object that is glitching and re-add it, <b>but do NOT remove the old one, simply "turn it off" with the checkbox.</b>
