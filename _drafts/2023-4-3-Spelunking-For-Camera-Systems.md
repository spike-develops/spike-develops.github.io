---
author_profile: true
tags: 
 - Unity 
 - C#
---

Spelunky has a cool flipping camera effect that doesn't happen that often. Backrooms in spelunky, blah blah

<figure>
    <a href="/assets/files/SpelunkyCam/SpelunkyExampleComplete.gif"><img src="/assets/files/SpelunkyCam/SpelunkyExampleComplete.gif"></a>
    <figcaption>Spelunky's original effect does a great job conveying the transition between its main level and "backrooms" (whats right term for that)</figcaption>
</figure>

**I should write some text about why I wanted to do it or something here/why it is such a good effect. Also gives room to breath between the two example gifs**

<figure>
    <a href="/assets/files/SpelunkyCam/CopExampleComplete.gif"><img src="/assets/files/SpelunkyCam/CopExampleComplete.gif"></a>
    <figcaption>My final CoP implementation, in order to convey the transition between floors from a top-down perspective</figcaption>
</figure>

<!--How in depth am I going? Do I show the Components?-->
As a notice, this explanation won't go into super specifics, and assumes a basic understanding of Unity and it's Cinemachine camera system. If you aren't there yet, the [Cinemachine Docs](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.0/manual/index.html) are a great place to start &#128513;

#### First step: Camera dissolve

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


#### Next step: Positioning the Storyboard Cam

Next we'll cover how we position `storyboardCam` to sync with wherever our Transition Destination cam is.

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

TODO go over the cutting/flicker priority here?

```cs
private IEnumerator FlickerPriority() {
    _transitionQueued = true;
    storyboardCam.Priority.Value = int.MaxValue;
    yield return null;
    storyboardCam.Priority.Value = int.MinValue;
    _transitionQueued = false;
}
```
After this we'll have a halfway transition that doesn't look great, so we need to get the other side working. 

#### Final step: Controlling the TransitionCamera?
Up till this point the Transition Camera (and therefore the RenderTexture that the storyboard cam displays) has been manually positioned to sit wherever the sourceCam was.

To "complete" the Spelunky effect, we want it to look like the source camera has a matching zoom like the destination camera does. To do that we'll start by complicating our diagram a bit, and adding a second Cinemachine brain/system to the Transition Camera, along with two new virtual cameras.

<figure>
    <img src="/assets/files/SpelunkyCam/ThirdWhiteboardExample.png">
    <figcaption>Addition of the Transition Brain system</figcaption>
</figure>

the new VCams, `TransitionStart` and `TransitionEnd` 


The goal here is to mimic the StoryboardCam -> destinationCam movement *within* the storyboard's render texture.




<figure>
    <img  src="/assets/files/SpelunkyCam/ViewportExample.png">
    <figcaption>Dichotomy of an animal</figcaption>
</figure>



TODO maybe this stuff is below the other stuff.

Because we now have two separate Cinemachine brains, we'll also need to isolate which brains use what VCams. Thankfully that's super easy in Cinemachine 3.0 using [Channel Filters.](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.0/manual/CinemachineBrain.html?#:~:text=Channel%20Filter%3A%20Cinemachine%20Brain%20uses%20only%20those%20CinemachineCameras%20that%20output%20to%20Channels%20present%20in%20the%20Channel%20Mask.%20You%20can%20set%20up%20split%2Dscreen%20environments%20by%20using%20the%20Channel%20Mask%20to%20filter%20channels.) Simply create a second "Transition" channel and set the new VCams and brain to exclusively use it. (the main camera/brain should exclude the new channel)

**If using Cinemachine 2.x** You'll need to use Layers/Camera culling masks to achieve this separation instead of channel filters.
{: .notice}



Now we'll finaly go back to the Transition Camera Brain system. We'll add a new VCAm (making sure to set the layer to ___) The goal here is to create a similar movement that the storyboardVCam -> real cam has, except all within the render texture that's displayed on the storyboard cam. 

We can just have the start camera here start at the old cameras ortho size, and then set the exit to the same ratio that the other camera has and things would work, but we can also help hide the cut by jumping slightly "ahead" IE if the old cameras ortho was 3.5, and we're "pushing in" instead of setting start to 3.5, and end to 1.5, we could set start to 3.25, and end to 1.25, "jumping" ahead. This really helps hide the cut between the real camera to the render Camera, and gives some extra punch to the transition. 

We can also do some cool stuff to the render texture -add pixialtion to the transition, different post processing, etc.







Run into a snag where State.Lens ortho is 10, but the vCam is clearly supposed to be 3.75. Its because I never gave enough time for the camera to update before disabling. So I either need to swap how i do that kind of thing, or find a way to force the final enter cam to evaluate itself right away.




Ran into another snag where pixel perfect math gets in the way of doing a one-to-one transistion, and instead it veered off (I made a trello card about it)