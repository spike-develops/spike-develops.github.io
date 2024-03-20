---
title: Networked Hierarchical State Machines
excerpt: Discussing HSM's in the context of netcode
header:
    overlay_image: /assets/files/HSM/PlayerHSM.png
    overlay_filter: linear-gradient(rgba(0, 0, 0, .8),#252a34)

permalink: /articles/networked-hsms/
author_profile: true
tags: 
 - Unity 
 - C#
 - Networking
 - HSM
---

Hi Okay writing this up


//QUICK explanation of a non networked HSM
Hierarchical State Machines are a spin on a typical state machine, where you can be in multiple states at once. The advantage being that 

Hierarchical state machines can be thought about as finite State machines with  tree-like structures, where all nodes from the active child to the root node are "active" at once.

The advantage they have over a typical finite state machine is that when describing a very complex system with lots of disparate stateful-ness, each individual state can stay relatively simple and compact.

For Example in Cult we have separate states for Facing and for moving
//I really can't get too into it from this direction if this is networking focused which it should be...



In Cult, I used Hierarchical state machines to describe all actor behaviour that required anything more than single nested conditional logic. IE, Doors, with just a state of Locked/Unlocked used our NetState system outright, while torches, with behaviour like ticked glows and time queued "burns", was described using an HSM

<figure>
    <a href="/assets/files/HSM/TorchHSM.png"><img src="/assets/files/HSM/TorchHSM.png"></a>
    <figcaption>View of an active Torch's HSM, with the current states in blue </figcaption>
</figure>

<figure>
    <a href="/assets/files/HSM/PlayerHSM.png"><img src="/assets/files/HSM/PlayerHSM.png"></a>
    <figcaption>Cult's final Player HSM, including all ghost/possession states </figcaption>
</figure>

Along with allowing for great flexibility over granular changes in behaviour, HSM's also gave some nice benefits on the networking side. 

The clearest of those benefits is how compactly we can describe the current state of machine.
In all games there are certain conditions an actor may be in at any given time. Here's a few from Cult
 -Moving
 -Sneaking
 -interacting,
 -Invulnerability (in elevators & cutscenes)
 -Timed Stuns,
 -Light and Heavy Attacks (with various movement restrictions)
 -Death,
 -Possession (as ghost)

Lots of these behaviors are mutually exclusive (while interacting, we know the player isn't stunned or sneaking), but some can overlap (Moving during a light attack). In an ideal world, we'd figure out which states are exclusive and never send that info over the network. Conversely we'd want to guarantee that overlapping states are never forgotten. We could add a bunch of behaviour specific logic to our bitpacker to achieve this, but what happens if we add  "sneak while interacting to silence the interaction sound? Networking logic needs to change with it, which is a pain for everyone.

Ideally we find a way to keep an efficient packing system that is behaviour agnostic, so we don't have to mess with the netcode every time we try out new gameplay behavior.


Instead of designating specific bits for conditions like `IsSneaking` or `IsInvulnerable`, we recursively serialize our walk down the HSM.

TODO this probably needs more intro/shouldn't be the first code we see
```cs
public int WalkTree(int depthMul = 1)
{
    //get the index of current child. (null could have an index)
    var childIndex = _subStates.IndexOf(_curSubState);

    var result = 0;
    if (_curSubState != null)
    {
        result = _curSubState.WalkTree(depthMul * _subStates.Count);
    }
    
    //encode this child index using the depth multiplier
    return result + childIndex * depthMul;
}
```

We can't get around having some "stateful" nodes however. Things like attacks, stuns, and interacting all have instance specific start points we need to track. We can serialize that





To deserialize, we walk the tree to rebuild it, decoding our header into the correct child state at each level. 

As we walk down the tree, we also check with each newly chosen child to see if its "stateful" and because we always serialize down the tree, we just let the current node read the next X bits however it needs, and move our index forward. This means there's no extra bits waisted on explaining how to deserialize the extra stateful bits.

//TODO probably need to describe how we "run" inputs on an HSM at the top. That way it makes sense once we get here


The other nice thing about networked HSM's is how easy it is to implement rollback using them. Because a single complex state diagram can be serialized into a few bytes, we can keep a rolling array of the last X states as byte arrays, and simply deserialize, and re-run inputs on the state machine whenever we need to rectify or re-predict!





Further optimization:
Once we get to a point where we only need to describe