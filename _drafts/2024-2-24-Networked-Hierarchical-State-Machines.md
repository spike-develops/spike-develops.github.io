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

//Explain how HSM's have a bubble up events?
what is the bare basis for this article

an HSM implementation the bubbles up events from the child to root, where transition(s) 


//QUICK explanation of a non networked HSM
Hierarchical State Machines are a spin on a typical state machine, where you can be in multiple states at once. The advantage being that 

Hierarchical state machines can be thought about as finite State machines with  tree-like structures, where all nodes from the active child to the root node are "active" at once.

The advantage they have over a typical finite state machine is that when describing a very complex system with lots of disparate stateful-ness, each individual node can stay relatively simple and compact, and behaviour can be easily modified by adjusting

In Cult, I used Hierarchical state machines to describe all actor behaviour that required anything more than single nested conditional logic. IE, Doors, with just a state of Locked/Unlocked used our NetState system outright, while torches, with behaviour like ticked glows and time queued "burns", was described using an HSM

<figure>
    <a href="/assets/files/HSM/TorchHSM.png"><img src="/assets/files/HSM/TorchHSM.png"></a>
    <figcaption>View of an active Torch's HSM, with the current states in blue </figcaption>
</figure>



<figure>
    <a href="/assets/files/HSM/PlayerHSM.png"><img src="/assets/files/HSM/PlayerHSM.png"></a>
    <figcaption>Cult's final Player HSM, including all ghost/possession states </figcaption>
</figure>

Along with allowing for great flexibility over granular changes in behaviour, HSM's also gave some nice benefits on the networking side. The clearest of those benefits is how easily we can serialize the current state of machine.

## Serialization
In all games there are certain conditions an actor may be in at any given time. Here's a few from Cult:
* Moving
* Sneaking
* interacting,
* Invulnerability -in elevators & cutscenes
* Timed Stuns,
* Light and Heavy Attacks -with various movement restrictions
* Possession -as ghost

Lots of these behaviors are mutually exclusive (while interacting, we know the player can't be stunned or sneaking), but some are valid combinations that can overlap (players can move while light attacking). In an ideal world, we'd determine which states are exclusive and never send that info over the network. Conversely we'd want to guarantee that valid combinations are always sent. 

To achieve that in an HSM-less world, we *could* add a bunch of behaviour specific logic to our bitpacker. IE "bit *x* refers to `IsSneaking` if an earlier bit *y* referring to `IsStunned` was true" but what happens if later on a change in actor behaviour affects these exclusion rules? If all of sudden "sneaking while interacting" is a valid combination that lets you interact silently, we'll need to modify our networking logic with it, icky.

On the flip side, an HSM bakes all of these behaviour exclusions into its structure, so if 

Instead of designating specific bits for conditions like `IsSneaking` or `IsStunned`, we recursively serialize our walk down the HSM.

The 



Ideally we find a way to keep an efficient packing system that is behaviour agnostic, so we don't have to mess with the netcode every time we try out new gameplay behavior.



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

We can't get around having some "stateful" nodes however. Things like attacks, stuns, and interacting all have instance specific info points we need to track. We can serialize that





To deserialize, we walk the tree to rebuild it, decoding our header into the correct child state at each level. 


## Rollback

As we walk down the tree, we also check with each newly chosen child to see if its "stateful" and because we always serialize down the tree, we just let the current node read the next X bits however it needs, and move our index forward. This means there's no extra bits waisted on explaining how to deserialize the extra stateful bits.

//TODO probably need to describe how we "run" inputs on an HSM at the top. That way it makes sense once we get here

//TODO also make sure we tell them to save their events per frame



The other nice thing about networked HSM's is how easy it is to implement rollback using them. Because a single complex state diagram can be serialized into a few bytes, we can keep a rolling array of the last X states as byte arrays, and simply deserialize, and re-run inputs on the state machine whenever we need to rectify or re-predict!

```cs
void RecurDeser(byte[] bytes, int byteIndex, int[] leafPath, int depth)
{
    //deserialize add'l state if necessary
    if(this is IStatefulNode sNode)
        sNode.DeserializeState(bytes, ref byteIndex);

    //etc...
```


## Even better Tree serialization
If you can ensure that each node in the HSM only ever has one parent, (and therefore ensure that there is only one way back to the root node from any leaf node), we can instead do some pre processing, and serialize current state in reference to the "leaf node index" 

First, we pre process, traversing through the tree and keeping track of the index paths taken. then we store those choices inside an array of "leaf node paths" that remembers the direction to any given leaf node.
```cs
private void PopulateLeafData(List<int[]> leafCache, 
    Dictionary<HSNode, index> leafMap, List<int> currentRoute)
{
    if(_subStates.Count == 0)
    {
        //save the currentRoute into the leaf cache
        leafCache.Add(currentRoute.ToArray())
        //map this leaf to its index for faster serialization
        leafMap.Add(this, leafCache.Count-1);
        return;
    }

    //we know we're not a leaf at this point
    for(int i = 0; i<_subStates.Count; i++)
    {
        //save this child index to the current route
        currentRoute.Add(i);
        _subStates[i].PopulateLeafData(leafCache, leafMap, currentRoute);
        //remove this index after processing
        currentRoute.RemoveAt(currentRoute.Count-1)
    }
}
```

Then its just a matter of serializing the leaf index and looking up the path on deserialization! 
//TODO CODE 

```cs
private SerializeHSM()
{

}
```

```cs
//called on root
public void Deserialize(byte[] bytes, int byteIndex)
{
    int leafIndex = bytes[i];
    ReDeserialize(bytes, byteIndex+1, leafCache[leafIndex], 0)
}

void RecurDeser(byte[] bytes, int byteIndex, int[] leafPath, int depth)
{
    //deserialize add'l state if necessary
    if(this is IStatefulNode sNode)
        sNode.DeserializeState(bytes, ref byteIndex);
    
    ActivateNode();

    //end on leaf
    if(_subStates.Count == 0)
        return;
    
    var curChildIndex = _subStates.IndexOf(_curSubState);
    var nextChildIndex = leafPath[depth];
    //if serialized child is different, deactivate old one
    if(curChildIndex != nextChildIndex)
    {
        _curSubState.DeactivateNode();
        _curSubState = _subStates[nextChildIndex];
    }

    _curSubState.RecurDeser(bytes, byteIndex, leafPath, depth+1);
}
```

This also has the added benefit of completely eliminating any issues stemming from an unbalanced tree, since deep vs wide leaf nodes aren't treated differently

## Optimizations
TODO should put int optimizations as I think of them in just text form.

Further optimization:
Once we get to a point where we only need to describe