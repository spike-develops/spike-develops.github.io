---
author_profile: true
tags: 
 - Unity 
 - C#
 - Cinemachine
---

Spelunky (1 and 2) are a series of 2D rouge-likes centered around cave-delving. They're fantastic platformers that you should try if you haven't! One of the additions in the second instalment was a "back layer" that sits *behind* the main level, containing extra platforming areas and npc's. **I'd argue the best part about the back layer though, is the camera transition to it.**

<figure>
    <a href="/assets/files/SpelunkyCam/SpelunkyExampleComplete.gif"><img src="/assets/files/SpelunkyCam/SpelunkyExampleComplete.gif"></a>
    <figcaption>Spelunky 2's camera transition between its main and back layers</figcaption>
</figure>

It zips to the character's new layer with a subtle dissolve, and it's impressively clear *where the layers are in respect to each other depth-wise* from the subtle zoom in/out. No small feat in a typically depth-less 2D environment. 

All that lead me towards figuring out how to replicate the effect for Cult of Personality! Making sure players maintained spatial awareness during floor transitions was super important, and Spelunky's transition fit the bill.

<figure>
    <a href="/assets/files/SpelunkyCam/CopExampleComplete.gif"><img src="/assets/files/SpelunkyCam/CopExampleComplete.gif"></a>
    <figcaption>My final CoP implementation, in order to convey the transition between floors from a top-down perspective</figcaption>
</figure>

I couldn't find any good overviews of the technique, so I figured I'd throw my hat in the ring and show how I did it. As a disclaimer, this explanation assumes a basic understanding of Unity's Cinemachine camera system and render textures. If you aren't there yet, the [Cinemachine Docs](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.0/manual/index.html) are a great place to start &#128513;

#### Basic Camera Dissolve

Since Cinemachine's workflow involves a single real camera/brain being aimed and positioned by various virtual cameras, a proper dissolve between VCams isn't possible out of the box. While the dissolve is happening, we'll need to render both places at once until the transition is complete.

There are a few ways to go about this, but I opted to use some of the techniques described in [this thread.](https://forum.unity.com/threads/is-it-possible-for-a-blend-to-interpolate-between-images-instead-of-position.541865/#post-3573211) The gist is that we'll create a second real camera that renders to a render texture, and then display that render texture on a "Storyboard" virtual camera that the Main Camera brain can then mix.

<figure>
    <img src="/assets/files/SpelunkyCam/InitialWhiteboardExample.png">
    <figcaption>Mockup of our planned system</figcaption>
</figure>

The new `storyboardCam` is duly named because we'll attach a [Cinemachine Storyboard](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.0/manual/CinemachineStoryboard.html) extension to it. The extension overlays (our render texture) as if it were the camera view. The big advantage of using the storyboard is that any transitions will naturally fade the storyboard texture, which gives us our first look at a dissolve.

<figure>
    <a href="/assets/files/SpelunkyCam/SimpleDissolve.gif"><img src="/assets/files/SpelunkyCam/SimpleDissolve.gif"></a>
    <figcaption>Manual Dissolve from UpperCam to StoryboardCam </figcaption>
</figure>

#### Positioning the Storyboard Cam

Next we'll cover how we sync `storyboardCam` with wherever our Transition's `destinationCam` is. Here's our `PositionStoryboardCam()` method.

```cs
private void PositionStoryboardCam(CinemachineCamera destinationCam, 
    float orthoOffset){

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
Instead of having `storyboardCam` match the position and orthographic size of `destinationCam` exactly, we'll tweak the ortho size to be slightly bigger or smaller than the end camera. This starts to create the Spelunky zoom that we're looking for. If transitioning "down" or "into", `orthoOffset` should be positive, and negative when transitioning "up" or "out of".

<figure>
    <a href="/assets/files/SpelunkyCam/HalfTransition.gif"><img src="/assets/files/SpelunkyCam/HalfTransition.gif"></a>
    <figcaption>Cutting immediately to Storyboard from Upper (Source), then easing to Lower (Destination)</figcaption>
</figure>

After this we'll have a halfway transition that doesn't look great, so we need to get the other side working. 

#### Positioning the Transition Camera
Up till this point the Transition Camera (and therefore the RenderTexture that the `storyboardCam` displays) has been manually positioned to sit wherever `sourceCam` was.

To "complete" the Spelunky effect, we want it to look like the source camera has a matching zoom like the destination camera does. To do that we'll start by complicating our diagram a bit, and adding a second Cinemachine brain/system to the Transition Camera, along with two new virtual cameras.

<figure>
    <img src="/assets/files/SpelunkyCam/ThirdWhiteboardExample.png">
    <figcaption>Addition of the Transition Brain system</figcaption>
</figure>

Because we now have two separate Cinemachine brains, we'll also need to isolate which brains use what VCams. Thankfully that's super easy in Cinemachine 3.0 using [Channel Filters.](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.0/manual/CinemachineBrain.html?#:~:text=Channel%20Filter%3A%20Cinemachine%20Brain%20uses%20only%20those%20CinemachineCameras%20that%20output%20to%20Channels%20present%20in%20the%20Channel%20Mask.%20You%20can%20set%20up%20split%2Dscreen%20environments%20by%20using%20the%20Channel%20Mask%20to%20filter%20channels.) Simply create a second "Transition" channel and set the new VCams and brain to exclusively use it. (the main camera/brain should exclude the new channel)

**If using Cinemachine 2.x** You'll need to use Layers/Camera culling masks to achieve this separation instead of channel filters.
{: .notice}

The new virtual cameras, `transitionStartCam` and `TransitionEndCam`, represent where the Transition Camera should start and end its camera move in order to match the `storyboardCam` -> `destinationCam` movement.

<figure>
    <img src="/assets/files/SpelunkyCam/ViewportExample.png">
    <figcaption>Arrows show both camera moves. In this case, we're going "down" to the lowerCam, so both ortho sizes shrink during the move</figcaption>
</figure>

Here's our `PositionTransitionCams()` method. It mirrors the storyboard version with a few slight differences.

```cs
private void PositionTransitionCams(CinemachineCamera sourceCam, 
    float startOffset, float endOffset){

    //align both transition cameras with sourceCam
    var pos = sourceCam.State.GetFinalPosition();
    var rot = sourceCam.State.GetFinalOrientation();
    transitionStartCam.ForceCameraPosition(pos, rot);
    transitionEndCam.ForceCameraPosition(pos, rot);

    //set ortho offsets for both cameras
    var baseOrtho = sourceCam.State.Lens.OrthographicSize;
    transitionStartCam.Lens.OrthographicSize = baseOrtho + startOffset;
    transitionEndCam.Lens.OrthographicSize = baseOrtho + endOffset;
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
Once you know which VCams will be the source and destination, and know if the transition should go "up" or down" you can use this `QueueTransition()` method. It does assume some things about your Cinemachine priority system, but should be pretty easy to modify to fit your needs.

```cs
public void QueueTransition(CinemachineCamera sourceCam, 
    CinemachineCamera destinationCam, bool transitioningDown){

    //If there's already a transition in progress ignore it.
    if (_transitionInProgress)
        return;
    //set destination to active priority, and source to inactive
    destinationCam.Priority.Value = activePriority;
    sourceCam.Priority.Value = inactivePriority;

    //invert offsets depending on transition direction
    if (transitioningDown){
        PositionTransitionCams(sourceCam, -initialOffset, -deltaOffset);
        PositionStoryboardCam(destinationCam, offsetValue);
    }else{
        PositionTransitionCams(sourceCam, initialOffset, deltaOffset);
        PositionStoryboardCam(destinationCam, -offsetValue);
    }

    //turn on the transitionCamera until the transition is finished
    transitionBrain.gameObject.SetActive(true);
    CinemachineCore.CameraDeactivatedEvent.AddListener(CamDeactivated);
    _transitionInProgress = true;

    //trigger the transition
    StartCoroutine(FlickerPriority());
}
```

TODO improve this area

something something the lines about transitionBrain are important, because we only wan't `transitionBrain` and its very real camera rendering the storyboard's render texture during the transition. Otherwise we'd be waisting valuable GPU time on rendering that won't be seen. 

The good news is that we know that we only need to do the extra rendering while `storyboardCam` is active, so we can hook into Cinemachine's CameraDeactivatedEvent, and turn the transition camera back off when the transition ends.

```cs
private void CamDeactivated(ICinemachineMixer mixer, 
    ICinemachineCamera deactivatedCam){
    //ignore any cam deactivations that aren't storyboardCam
    if (!deactivatedCam.Equals(storyboardCam))
        return;
    
    //turn off the transitionCamera and unsubscribe
    transitionBrain.gameObject.SetActive(false);
    CinemachineCore.CameraDeactivatedEvent.RemoveListener(CamDeactivated);
    _transitionInProgress = false;
}
```
Since Cinemachine's `CameraDeactivatedEvent` gets called for all camera deactivations, we need to start by confirming we only respond to `storyboardCam`. Then its just a matter of deactivating `transitionBrain` to stop duplicate rendering, and cleaning up after ourselves.

The `FlickerPriority()` Coroutine is also pretty simple.

```cs
private IEnumerator FlickerPriority(){
    transitionStartCam.Priority.Value = int.MaxValue;
    storyboardCam.Priority.Value = int.MaxValue;

    //wait a frame, then lower their priority
    yield return null;

    storyboardCam.Priority.Value = int.MinValue;
    transitionStartCam.Priority.Value = int.MinValue;
}
```
This method (along with the custom blend asset below) are what gives us the cut-then-fade effect for both camera brains.
We need to wait a frame in order to give the brains a chance to process the cuts to `storyboardCam` and `transitionStartCam` respectively, before dropping the priority to trigger the fades to `destinationCam` and `transitionEndCam`.

<figure>
    <img src="/assets/files/SpelunkyCam/CustomBlends.png">
    <figcaption>Custom blend asset usable by both brains. The cuts shouldn't be modified, but both eases can be tweaked for transition feel. </figcaption>
</figure>

#### Full Script

That about wraps up my take on what I've always thought is a pretty nifty and useful transition effect! The full FloorTransitionController component is below, with some extra modifications for specific circumstances like use with a Pixel Perfect Camera. Happy Spelunking!

<script src="https://gist.github.com/spike-develops/5d44322620444f2ee1f2120de1096451.js"></script>