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

To achieve that in an HSM-less world, we *could* add a bunch of behaviour specific logic to our bitpacker. IE "bit *x* refers to `IsSneaking` if an earlier bit *y* referring to `IsStunned` was true" but what happens if later on a change in actor behaviour affects these exclusion rules? If all of sudden "sneaking while interacting" is a valid combination that lets you interact silently, we'll need to modify our networking logic along with any gameplay change. icky.

On the flip side, an HSM bakes all of these behaviour exclusions into its structure, so instead of designating specific bits for conditions like `IsSneaking` or `IsStunned`, we can just serialize our "walk" down the tree.

<figure>
    <a href="/assets/files/HSM/PlayerHSM.png"><img src="/assets/files/HSM/PlayerHSM.png"></a>
    <figcaption>Cult's final Player HSM, including all ghost/possession states </figcaption>
</figure>
//TODO simple image of choosing

Starting at the root, we can encode each level as a decision between its possible children. Here the decisions are `2nd of 5`, `1st of 2`, `3rd of 4`. We then multiply these choices together based on tree depth to create a "header" that encodes the HSM's current state.

```cs
public void SerializeHSM(StreamBuffer outStream)
{
    //encode the header
    var header = RootNode.SerNode();
    outStream.WriteByte(header);
}

int SerNode(int depthMul = 1)
{
    //stop at leaf node
    if(_childIndex == -1)
        return 0;
    
    var h = _subNodes[_childIndex].SerNode(depthMul*_subNodes.Count);
    
    //encode this child index using the depth multiplier
    return h + _childIndex * depthMul;
}
```
//TODO talk about what the max is

<figure>
    <a href="/assets/files/HSM/PlayerHSM.png"><img src="/assets/files/HSM/PlayerHSM.png"></a>
    <figcaption>Cult's final Player HSM, including all ghost/possession states </figcaption>
</figure>
//TODO image of large complex tree and total math

//TODO do a note that we'll make this better later

To deserialize, we walk the tree to rebuild it, decoding our header into the correct child state at each level. 

```cs
public void DeserializeHSM(StreamBuffer inStream)
{
    //decode the header
    var header = inStream.ReadByte();
    RootNode.DeSerNode(header);
}

void DeSerNode(int header, int depthMul = 1)
{
    //stop on leaf node
    if(_childIndex == -1)
        return;
    
    var nextDepthMul = _substates.Count * depthMul;
    //slice off the relevant header
    var moddedHeader = header % nextDepthMul;
    //bring it back within the range of child indexes
    _childIndex = moddedHeader / depthMul;

    _subNodes[_childIndex].DeSerNode(header-moddedHeader, nextDepthMul);
}
```
TODO maybe go over this algo?

## Stateful Nodes

Unfortunately, we can't get by with *just* serializing the path through the tree. Timed nodes like attacks need to know when they started, and nodes like interaction need to know who/what they're interacting with. 

The good news is that this stateful node info can be serialized during the initial walk through the tree with very little overhead.

First, any Stateful node can implement an interface that marks itself as such.
```cs
public interface IStatefulNode
{
    void DeSerState(StreamBuffer inStream);

    void SerState(StreamBuffer outStream);
}
```
Then, instead of just building the header in `SerNode()` we'll send our streamBuffer along, and from root to leaf let any stateful nodes serialize its extra info

```cs 
int SerNode(StreamBuffer outStream, int depthMul = 1)
{
    //if we're stateful, append our info
    if(this is IStatefulNode sNode)
        sNode.SerState(outStream);

    //... 
```
On the other end, we add the matching method, letting stateful nodes pull the next x bytes as required, going 

```cs 
void DeSerNode(StreamBuffer inStream, int header, int depthMul = 1)
{
    //deserialize add'l state if necessary
    if(this is IStatefulNode sNode)
        sNode.DeSerState(inStream);

    //...
```
You'll notice that *because* the Hierarchy of the state machine implies a serialized order (from root to leaf), we don't have to waste bits on flags noting the "next X bytes are for an attack timestamp" and we'll never waste bits on what's *not* there either.

## Rollback


//TODO probably need to describe how we "run" inputs on an HSM at the top. That way it makes sense once we get here

//TODO also make sure we tell them to save their events per frame

```cs 

//dont think this is a fantastic example...
//maybe we specify that this could be on the client as the repredict based on server sending them updated states
private void RollbackAndRunEvents(StreamBuffer inStream, Queue<HSMEvents> inputQueue)
{
    RootNode.DeSerNode(inStream);

    while(inputQueue.Count > 0)
    {
        RootNode.RunEvent(inputQueue.Dequeue());
    }
    //at this point we've rectified with the server, and are ready for our next input
}
```

The other nice thing about networked HSM's is how easy it is to implement rollback using them. Because a single complex state diagram can be serialized into a few bytes, we can keep a rolling array of the last X states as byte arrays, and simply deserialize, and re-run inputs on the state machine whenever we need to rectify or re-predict!


## Even better Tree serialization
If you can ensure that each node in the HSM only ever has one parent, (and therefore ensure that there is only one way back to the root node from any leaf node), we can instead do some pre processing, and serialize current state in reference to the "leaf node index" 

//TODO image of the example of counting each leaf node and showing the reduction in size

First, we pre process, traversing through the tree and keeping track of the index paths taken. then we store those choices inside an array of "leaf node paths" that remembers the direction to any given leaf node.
```cs
private void PopulateLeafData(List<int[]> leafCache, 
    Dictionary<HSNode, index> leafMap, List<int> currentRoute)
{
    if(_subNodes.Count == 0)
    {
        //save the currentRoute into the leaf cache
        leafCache.Add(currentRoute.ToArray())
        //map this leaf to its index for faster serialization
        leafMap.Add(this, leafCache.Count-1);
        return;
    }

    //we know we're not a leaf at this point
    for(int i = 0; i<_subNodes.Count; i++)
    {
        //save this child index to the current route
        currentRoute.Add(i);
        _subNodes[i].PopulateLeafData(leafCache, leafMap, currentRoute);
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
public void DeserializeHSM(StreamBuffer inStream)
{
    int leafIndex = inStream.ReadByte();
    DeSerNode(inStream, leafCache[leafIndex], 0)
}

void DeSerNode(StreamBuffer inStream, int[] leafPath, int depth)
{
    //deserialize add'l state if necessary
    if(this is IStatefulNode sNode)
        sNode.DeserializeState(inStream);

    //stop on leaf node
    if(_childIndex == -1)
        return;
    
    //update the current child node
    _childIndex = leafPath[depth];
    _subNodes[_childIndex].DeSerNode(inStream, leafPath, depth+1);
}
```

This also has the added benefit of completely eliminating any issues stemming from an unbalanced tree, since deep vs wide leaf nodes aren't treated differently

## Optimizations
TODO should put int optimizations as I think of them in just text form.

Further optimization:
Once we get to a point where we only need to describe



public void SerializeHSM(StreamBuffer outStream)
{
    //leave space for header for after serialization
    var headerPos = outStream.Position;
    outStream.Position++;

    var header = RootNode.SerNode(outStream);

    //TODO confirm header is smaller than max size

    //go back and set the header
    var finalPos = outStream.Position;
    outStream.Position = headerPos;
    outStream.WriteByte(header);
    outStream.Position = finalPos;
}