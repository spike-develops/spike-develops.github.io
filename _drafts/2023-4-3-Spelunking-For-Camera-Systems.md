---
author_profile: true
tags: 
 - Unity 
 - C#
---

Spelunky has a cool flipping camera effect that doesn't happen that often. Backrooms in spelunky, blah blah

<figure>
    <a href="/assets/files/SpelunkyCam/SpelunkyExampleComplete.gif"><img src="/assets/files/SpelunkyCam/SpelunkyExampleComplete.gif"></a>
    <figcaption>Spelunky's original effect does a great job conveying the transition between its main level and back layer</figcaption>
</figure>

~~**I should write some text about why I wanted to do it or something here/why it is such a good effect. Also gives room to breath between the two example gifs**~~

<figure>
    <a href="/assets/files/SpelunkyCam/CopExampleComplete.gif"><img src="/assets/files/SpelunkyCam/CopExampleComplete.gif"></a>
    <figcaption>My final CoP implementation, in order to convey the transition between floors from a top-down perspective</figcaption>
</figure>

<!--How in depth am I going? Do I show the Components?-->
As a notice, this explanation won't go into super specifics, and assumes a basic understanding of Unity and it's Cinemachine camera system. If you aren't there yet, the [Cinemachine Docs](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.0/manual/index.html) are a great place to start &#128513;

#### Basic Camera Dissolve

Since Cinemachine's workflow involves a single real camera/brain being aimed and positioned by various virtual cameras, to get a proper dissolve effect isn't as simple as adding another vCam and calling it a day. While the dissolve is happening, we'll need to render both places at once until the transition is complete.

There are a few ways to go about this, but I opted to use some of the techniques described in [this thread.](https://forum.unity.com/threads/is-it-possible-for-a-blend-to-interpolate-between-images-instead-of-position.541865/#post-3573211) The gist is that we'll create a second real camera that renders to a render texture, and then display that render texture on a "Storyboard" virtual camera that the Main Camera brain can then mix.

<figure>
    <img src="/assets/files/SpelunkyCam/InitialWhiteboardExample.png">
    <figcaption>Mockup of our planned system</figcaption>
</figure>

The new Storyboard VCam is duly named because we'll attach a Cinemachine storyboard extension to it. The extension overlays (our render texture) as if it were the camera view. The big advantage of using the storyboard is that any transitions will naturally fade the storyboard texture, which gives us our first look at a dissolve.

<figure>
    <a href="/assets/files/SpelunkyCam/SimpleDissolve.gif"><img src="/assets/files/SpelunkyCam/SimpleDissolve.gif"></a>
    <figcaption>Manual Dissolve from "UpperCam" to "StoryboardCam" </figcaption>
</figure>

~~The fact that we can only dissolve to/from the storyboard VCam is a bit of a snag, since ideally we "end" the transition on `LowerCam`, but that'll get solved out in the next part.~~


#### Positioning the Storyboard Cam

Next we'll cover how we position `storyboardCam` to sync with wherever our Transition Destination cam is. Here's our `PositionTransitionCameras()` method.

```cs
private void PositionStoryboardCamera(ICinemachineCamera destinationCam, float orthoOffset){
        var pos = destinationCam.State.GetFinalPosition();
        var rot = destinationCam.State.GetFinalOrientation();
        //use ForceCameraPosition so Cinemachine internals stay happy.
        storyboardCam.ForceCameraPosition(pos, rot);
        //offset storyboard ortho to create the zoom transition
        var baseOrtho = endCam.State.Lens.OrthographicSize;
        var calculatedOrtho = baseOrtho + orthoOffset;
        storyboardCam.Lens.OrthographicSize = calculatedOrtho;
    }
```
Instead of having `storyboardCam` match the position and orthographic size of `destinationCam` exactly, we'll slightly tweak the ortho size to be slightly bigger or smaller than the end camera. This starts to create the Spelunky zoom that we're looking for. If transitioning "down" or "into", `orthoOffset` should be positive, and negative when transitioning "up" or "out of".

<figure>
    <a href="/assets/files/SpelunkyCam/HalfTransition.gif"><img src="/assets/files/SpelunkyCam/HalfTransition.gif"></a>
    <figcaption>Cutting immediately to Storyboard from Upper (Source), then easing to Lower (Destination)</figcaption>
</figure>

After this we'll have a halfway transition that doesn't look great, so we need to get the other side working. 

#### Positioning the Transition Camera
Up till this point the Transition Camera (and therefore the RenderTexture that the storyboard cam displays) has been manually positioned to sit wherever the sourceCam was.

To "complete" the Spelunky effect, we want it to look like the source camera has a matching zoom like the destination camera does. To do that we'll start by complicating our diagram a bit, and adding a second Cinemachine brain/system to the Transition Camera, along with two new virtual cameras.

<figure>
    <img src="/assets/files/SpelunkyCam/ThirdWhiteboardExample.png">
    <figcaption>Addition of the Transition Brain system</figcaption>
</figure>

Because we now have two separate Cinemachine brains, we'll also need to isolate which brains use what VCams. Thankfully that's super easy in Cinemachine 3.0 using [Channel Filters.](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.0/manual/CinemachineBrain.html?#:~:text=Channel%20Filter%3A%20Cinemachine%20Brain%20uses%20only%20those%20CinemachineCameras%20that%20output%20to%20Channels%20present%20in%20the%20Channel%20Mask.%20You%20can%20set%20up%20split%2Dscreen%20environments%20by%20using%20the%20Channel%20Mask%20to%20filter%20channels.) Simply create a second "Transition" channel and set the new VCams and brain to exclusively use it. (the main camera/brain should exclude the new channel)

**If using Cinemachine 2.x** You'll need to use Layers/Camera culling masks to achieve this separation instead of channel filters. TODO attach a link to this
{: .notice}

The new virtual cameras, `TransitionStart` and `TransitionEnd`, represent where the Transition Camera should start and end its camera move in order to match the StoryboardCam -> destinationCam movement.

<figure>
    <img src="/assets/files/SpelunkyCam/ViewportExample.png">
    <figcaption>Arrows show both camera moves. In this case, we're going "down" to the lowerCam, so both ortho sizes shrink during the move</figcaption>
</figure>

Here's our `PositionTransitionCameras()` method. It mirrors the storyboard version with a few slight differences.

```cs
private void PositionTransitionCameras(ICinemachineCamera sourceCam, float orthoStartOffset, float orthoEndOffset) {
        var pos = sourceCam.State.GetFinalPosition();
        var rot = sourceCam.State.GetFinalOrientation();
        //align both transition cameras with sourceCam
        transitionStartCam.ForceCameraPosition(pos, rot);
        transitionEndCam.ForceCameraPosition(pos, rot);
        //set ortho offsets for both cameras
        var baseOrtho = sourceCam.State.Lens.OrthographicSize;
        transitionStartCam.Lens.OrthographicSize = baseOrtho + orthoStartOffset;
        transitionEndCam.Lens.OrthographicSize = baseOrtho + orthoEndOffset;
    }
```
We align both transition cams using `sourceCam`, then apply separate ortho offsets to them. These offsets should be inverse of `storyboardCam`'s: negative if transitioning down, and positive when going up.

`transitionStartCam`'s ortho can match `sourceCam`'s, but offsetting it slightly it gives the transition a nice jumpstart, and helps hide the initial cut.
{: .notice}

<figure>
    <a href="/assets/files/SpelunkyCam/FullTransition.gif"><img src="/assets/files/SpelunkyCam/FullTransition.gif"></a>
    <figcaption>(Transition Camera) cutting immediately to transition start, then easing back to transition end</figcaption>
</figure>

#### Triggering the Transition
Up until now we've been glossing over how to actually trigger the transition. 
Once you know which cams will be the source and destination, and know if the transition should go "up" or down" you can use this `QueueTransition()` method.

```cs
public void QueueTransition(ICinemachineCamera sourceCam, ICinemachineCamera destinationCam, bool transitioningDown){
        //If the transition is already queued ignore it.
        if (_transitionQueued)
            return;
        //only difference between transitioning down vs up is inverting the offsets :0
        if (transitioningDown){
            PositionTransitionCameras(sourceCam, -offsetStartValue, -offsetValue);
            PositionStoryboardCamera(destinationCam, offsetValue);
        }else{
            PositionTransitionCameras(sourceCam, offsetStartValue, offsetValue);
            PositionStoryboardCamera(destinationCam, -offsetValue);
        }
        //trigger the transition
        StartCoroutine(FlickerPriority());
    }
```

The `FlickerPriority` Coroutine is also pretty simple.

```cs
private IEnumerator FlickerPriority() {
    _transitionQueued = true;
    transitionStartCam.Priority.Value = int.MaxValue;
    storyboardCam.Priority.Value = int.MaxValue;
    //wait a frame, then lower their priority
    yield return null;
    storyboardCam.Priority.Value = int.MinValue;
    transitionStartCam.Priority.Value = int.MinValue;
    _transitionQueued = false;
}
```
This method (along with a custom blend asset) is what gives us the cut-then-fade effect for both Cameras.
We need to wait a frame in order to give the brains a chance to process the cuts to `storyboardCam` and `transitionStartCam` respectively, before dropping the priority to trigger the fades to `destinationCam` and `transitionEndCam`.



TODO need to explain that before calling QueueTransition, you need to have just triggered the source to destination transition. Or do we make that part of this version? that way its all packaged as one thing...

TODO show the camera blends.

