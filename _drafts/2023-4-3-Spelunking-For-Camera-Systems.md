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

There are a few ways to go about this, but I opted to use some of the techniques described in [this thread,](https://forum.unity.com/threads/is-it-possible-for-a-blend-to-interpolate-between-images-instead-of-position.541865/#post-3573211) since it'll help down the line we we start adding the Spelunky Flair. The gist is that we'll create a second real camera that is only used during the transition, and exclusively renders to a render texture that we create.

TODO add some info about the render texture in a screenshot of it?



Then, we create a new virtualCamera and add a "storyboard" extension to it. Storyboards overlay a texture on top of whatever the vCam is rendering, and are great for quick and dirty fades to color, but they really start shining when used in tandem with Render Textures. 

When we plop in the render texture from earlier, all of a sudden we have a virtual camera that the main camera can transition to, that really just displays whatever our TransitionCamera is looking at. nifty.

The other really awesome thing is that any sort of transition between this new storyboardVCam will naturally fade in/out the storyboard render texture without extra overhead. pog.

<figure>
    <a href="/assets/files/SpelunkyCam/SimpleDisolve.gif"><img src="/assets/files/SpelunkyCam/SimpleDisolve.gif"></a>
    <figcaption>Basic Disolve from "UpperCam" to "StoryboardCam" </figcaption>
</figure>

An *almost* working dissolve. The Snag we hit though, is the fact that we can only dissolve to/from the virtual storyboard cam. If we want to end the transition on **lowerCam**, we have to hide a cut from our storyboard to it.

~~But of course, this is a tut about the Spelunky effect, and it's cool because it doesn't just dissolve, it also trucks into and out of the transition room to really sell the spatial change.~~

*Something something add a new Cinemachine Channel called transitions, and the new brain, and both of the new VCams should be a part of it*

**If using Cinemachine 2.x** You'll need to use Layers/Camera culling masks to achieve this separation instead of channel filters.
{: .notice}

First wrinkle is to add a brain to the Transition Camera. Since we want to be able to position 

To get that working, we'll need to complicate things a little bit.
First, and easiest, we'll want to change how we position the storyboardVCam. This won't change how the storyboard render texture looks at all, but it will change how the transition to the new room looks. Instead of having the storyboardVCam match the position and orthographic size of the new camera exactly, we'll slightly tweek the ortho size to be slightly bigger or smaller than the end camera. 



AFter this we'll have a halfway transition that doesn't look great, so we need to get the other side working. 


*have we said if they should use brain/vcam here? should probably start with "create brain and a single vCam, and also explain the culling layer wierd thing, to make sure that the vCams stay correct*

Now we'll finaly go back to the Transition Camera Brain system. We'll add a new VCAm (making sure to set the layer to ___) The goal here is to create a similar movement that the storyboardVCam -> real cam has, except all within the render texture that's displayed on the storyboard cam. 

We can just have the start camera here start at the old cameras ortho size, and then set the exit to the same ratio that the other camera has and things would work, but we can also help hide the cut by jumping slightly "ahead" IE if the old cameras ortho was 3.5, and we're "pushing in" instead of setting start to 3.5, and end to 1.5, we could set start to 3.25, and end to 1.25, "jumping" ahead. This really helps hide the cut between the real camera to the render Camera, and gives some extra punch to the transition. 

We can also do some cool stuff to the render texture -add pixialtion to the transition, different post processing, etc.







Run into a snag where State.Lens ortho is 10, but the vCam is clearly supposed to be 3.75. Its because I never gave enough time for the camera to update before disabling. So I either need to swap how i do that kind of thing, or find a way to force the final enter cam to evaluate itself right away.




Ran into another snag where pixel perfect math gets in the way of doing a one-to-one transistion, and instead it veered off (I made a trello card about it)