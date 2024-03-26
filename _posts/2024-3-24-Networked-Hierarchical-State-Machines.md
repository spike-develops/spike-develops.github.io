---
title: Networked Hierarchical State Machines
excerpt: Discussing HSM's in the context of netcode
header:
    overlay_image: /assets/files/HSM/PlayerHSMReduced.png
    overlay_filter: linear-gradient(rgba(0, 0, 0, .8),#252a34)

permalink: /articles/networked-hsms/
author_profile: true
tags: 
 - Unity 
 - C#
 - Networking
 - HSM
---

Hierarchical State Machines are a spin on finite state machines, where you can be in multiple states within a tree-like *Hierarchy* at any givin time. For a brief overview of FSM's and their hierarchical counterparts, you can't go wrong with [Game Programming Patterns.](https://gameprogrammingpatterns.com/state.html)

In Cult, I used HSM's to describe all actor behaviour that required anything more than single nested conditional logic. IE, Doors, with just a state of Locked/Unlocked used our NetState system outright, while torches, with behaviour like ticked glows and time queued "burns", was described using an HSM

<figure>
    <a href="/assets/files/HSM/TorchHSM.png"><img src="/assets/files/HSM/TorchHSM.png"></a>
    <figcaption>View of an active Torch's HSM, with the current nodes in blue </figcaption>
</figure>

The advantage they have over a typical finite state machine is that when describing a very complex system with lots of disparate stateful-ness, each individual node can stay relatively simple and compact, and behaviour can be easily modified by adjusting the hierarchy and its transitions, as apposed to individual states.

Along with allowing for great flexibility over granular changes in behaviour, HSM's also gave some nice benefits on the networking side. The clearest of those benefits is how easily we can serialize the current state of machine.

code shown omits things like input sanitization for readability. In a production environment, always assume packets are malicious and check for valid ranges!
{: .notice}

## Serialization
In all games there are certain conditions an actor may be in at any given time. Here's a few from Cult:
* **Moving**
* **Sneaking**
* **interacting**,
* **Invulnerability** -in elevators & cutscenes
* **Timed Stuns**,
* **Light and Heavy Attacks** -with various movement restrictions
* **Possession** -as ghost

Lots of these behaviors are mutually exclusive (while interacting, we know the player can't be stunned or sneaking), but some are valid combinations that can overlap (players can move while light attacking). In an ideal world, we'd determine which states are exclusive and never send that info over the network. Conversely we'd want to guarantee that valid combinations are always sent. 

To achieve that in an HSM-less world, we *could* add a bunch of behaviour specific logic to our bitpacker. IE "bit *x* refers to `IsSneaking` if an earlier bit *y* referring to `IsStunned` was true" but what happens if later on a change in actor behaviour affects these exclusion rules? If all of sudden "sneaking while interacting" is a valid combination that lets you interact silently, we'll need to modify our networking logic along with any gameplay change. *icky*.

On the flip side, an HSM bakes all of these behaviour exclusions into its structure, so instead of designating specific bits for conditions like `IsSneaking` or `IsStunned`, we can just serialize our "walk" down the tree.

<figure>
    <a href="/assets/files/HSM/TorchSelections.png"><img src="/assets/files/HSM/TorchSelections.png"></a>
    <figcaption>Series of child indexes that represent this Torch's current state</figcaption>
</figure>

Starting at the root, we encode each child as an index out of possible children. Here the decisions are `2nd of 3`, `1st of 1`, `2nd of 2`, `1st of 2`, and `1st of 1`. We then multiply these choices together based on node depth to create a "header" that encodes the HSM's current state.

```cs
public void SerializeHSM(StreamBuffer outStream)
{
    //encode the header
    var header = RootNode.SerNode();
    outStream.WriteUShort(header);
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
`depthMul` is the product of every `_subNodes.Count` above it in the hierarchy, and ensures that each level's `_childIndex` is encoded on a separate scale. The caveat here is that the header's magnitude can vary significantly depending on which subtree the current state ends up in. 

<figure>
    <a href="/assets/files/HSM/PlayerSelections.png"><img src="/assets/files/HSM/PlayerSelections.png"></a>
    <figcaption>Worst-case header size for the Player HSM in Cult</figcaption>
</figure>

For Cult's HSM's the worst-case sits below 2 bytes. These limits are also per HSM, so a smaller HSM's header requirements can come in under a byte pretty frequently.

To deserialize, we walk back down the tree using the header to decode the next child index as we go.
```cs
public void DeserializeHSM(StreamBuffer inStream)
{
    //decode the header
    var header = inStream.ReadUShort();
    RootNode.DeSerNode(header);
}

void DeSerNode(int header, int depthMul = 1)
{
    //stop on leaf node
    if(_childIndex == -1)
        return;
    
    var nextDepthMul = _subNodes.Count * depthMul;
    //slice off the relevant header
    var moddedHeader = header % nextDepthMul;
    //bring it back within the range of child indexes
    _childIndex = moddedHeader / depthMul;

    _subNodes[_childIndex].DeSerNode(header-moddedHeader, nextDepthMul);
}
```
We use the fact that each child index was scaled by `depthMul` to slice the header back into its individual levels, and keep going till we hit a leaf. 


**Improved Serialization -** this technique allows nodes with multiple parents to be serialized without issue. If that isn't needed, an even more efficient system is described later
{: .notice}

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
On the other end, we add the matching method, letting stateful nodes pull the next x bytes as required. 
```cs 
void DeSerNode(StreamBuffer inStream, int header, int depthMul = 1)
{
    //deserialize add'l state if necessary
    if(this is IStatefulNode sNode)
        sNode.DeSerState(inStream);

    //...
```
Because the Hierarchy of the state machine implies a serialized order (from root to leaf), we don't have to waste bits on flags noting the "next X bytes are for an attack timestamp" and we'll never waste bits on what's *not* there either.

## Rollback and Re-Prediction
Hierarchical State Machines adhere to the same core principles as their finite counterparts. 

*currentState + input = nextState*

This implies that given the same starting state, and the same n event inputs, the final state of two identical machines will match. Using that concept, rollback and re-prediction of an HSM can be boiled down to deserializing a confirmed state from the server, then replaying input events from a rolling input buffer. 

## Machine Transition Delegates
In a typical HSM implementation, you'll have Enter and Exit delegates that trigger any time the machine transitions. These are still important in a Networked HSM, but more specific delegates for network contexts can also be pretty useful. 
- **Enter/Exit due to deserialization** - allows for ignoring effects that get published in other ways
- **Enter/Exit from predicted events** - allows for ignoring effects that can't be predicted

## Even better Tree serialization
If you can ensure that each node in the HSM only ever has one parent, (and therefore ensure that there is only one way back to the root node from any leaf node). We can reduce our serialized header size by describing the HSM based solely on our current leaf node's "index".

<figure>
    <a href="/assets/files/HSM/LeafSelections.png"><img src="/assets/files/HSM/LeafSelections.png"></a>
    <figcaption>max header for player HSM using leaf index</figcaption>
</figure>
This can be a pretty significant size-reduction! For Cult's Player HSM we halve our bit requirement, and would need to more-than-double the size of the HSM before we'd need a second byte.

The first step to this technique is doing a bit of pre-processing to actually "index" all the possible leaves.
During Init, we'll start with a depth-first tree traversal.
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
During our traversal, we keep track of the child indexes we've visited inside `currentRoute`. When we hit a leaf node, we copy that route into `leafCache`, and map the leaf->index in `leafMap`.

During serialize, we use `leafMap` to get the current leaf index, and serialize it as our header.
```cs
public void SerializeHSM(StreamBuffer outStream)
{
    //leave space for header for after serialization
    var headerPos = outStream.Position;
    outStream.Position++;

    var leafNode = RootNode.SerNode(outStream);
 
    //go back to set the header
    var finalPos = outStream.Position;
    outStream.Position = headerPos;
    //write the leafIndex
    outStream.WriteByte(leafMap[leafNode]);
    outStream.Position = finalPos;
}

HSNode SerNode(StreamBuffer outStream)
{
    //if we're stateful, append our info
    if(this is IStatefulNode sNode)
        sNode.SerState(outStream);

    //stop at leaf node and return its reference
    if(_childIndex == -1)
        return this;
    
    return _subNodes[_childIndex].SerNode(outStream);
}
```
`SerNode()` becomes even simpler because of the pre-processing. Now it just needs to return a reference to the leafNode at the base case.

On deserialize, we use `leafCache` to retrieve the cached route we need to follow down the hierarchy. 
```cs
public void DeserializeHSM(StreamBuffer inStream)
{
    int leafIndex = inStream.ReadByte();
    DeSerNode(inStream, leafCache[leafIndex])
}

void DeSerNode(StreamBuffer inStream, int[] leafPath, int depth = 0)
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
This also has the added benefit of eliminating lots of issues stemming from an unbalanced tree, since leaf index is unrelated to depth/width.