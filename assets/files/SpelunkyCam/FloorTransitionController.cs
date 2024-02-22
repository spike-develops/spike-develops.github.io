using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.U2D;

public class FloorTransitionController: MonoBehaviour
{
    [SerializeField] 
    private CinemachineCamera storyboardCam;
    [SerializeField] 
    private CinemachineCamera transitionStartCam;
    [SerializeField] 
    private CinemachineCamera transitionEndCam;
    
    [SerializeField, Tooltip("Optional: used to correct ortho sizes when using a pixel perfect camera")] 
    private PixelPerfectCamera pixelPerfect;
    
    [SerializeField,Tooltip("value that determines how far the transition zooms in or out")]
    private float deltaOffset = 1f;
    [SerializeField, Tooltip("value that determines how far destinationStartCam cuts before starting its zoom")] 
    private float initialOffset = .5f;

    [SerializeField] 
    private int activePriority = 15;
    [SerializeField] 
    private int inactivePriority = 5;
    
    private bool _transitionQueued;
    
    public void QueueTransition(CinemachineCamera sourceCam, CinemachineCamera destinationCam, bool transitioningDown)
    {
        //If the transition is already queued ignore it.
        if (_transitionQueued)
            return;
        
        //set destination to whatever your active priority is, and source to inactive
        destinationCam.Priority.Value = activePriority;
        sourceCam.Priority.Value = inactivePriority;
        
        if (transitioningDown)
        {
            PositionTransitionCams(sourceCam, -initialOffset, -deltaOffset);
            PositionStoryboardCam(destinationCam, deltaOffset);
        }
        else
        {
            PositionTransitionCams(sourceCam, initialOffset, deltaOffset);
            PositionStoryboardCam(destinationCam, -deltaOffset);
        }
        
        //trigger the transition
        StartCoroutine(FlickerPriority());
    }
    
    /// <summary>
    /// Flickers priority of the two "starting cams" in order to cut to them,
    /// then immediately start a fade
    /// </summary>
    /// <returns></returns>
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
    
    /// <summary>
    /// positions the storyboard cam offset from the destination cam, so the transition between them zooms.
    /// </summary>
    /// <param name="destinationCam"></param>
    /// <param name="orthoOffset"></param>
    private void PositionStoryboardCam(CinemachineCamera destinationCam, float orthoOffset) {
        //make sure destination camera is up to date
        destinationCam.UpdateCameraState(Vector3.up, 0f);
        
        var pos = destinationCam.State.GetFinalPosition();
        var rot = destinationCam.State.GetFinalOrientation();
        storyboardCam.ForceCameraPosition(pos, rot);
        
        //offset storyboard ortho to create the zoom transition
        var baseOrtho = destinationCam.State.Lens.OrthographicSize;
        if (pixelPerfect != null)
        {
            baseOrtho = pixelPerfect.CorrectCinemachineOrthoSize(baseOrtho);
        }
        var calculatedOrtho = baseOrtho + orthoOffset;
        storyboardCam.Lens.OrthographicSize = calculatedOrtho;
    }
    
    /// <summary>
    /// positions both start and end cams offset from the source cam, so the transition between start and end zooms.
    /// </summary>
    /// <param name="sourceCam"></param>
    /// <param name="startOffset"></param>
    /// <param name="endOffset"></param>
    private void PositionTransitionCams(CinemachineCamera sourceCam, float startOffset, float endOffset) {
        var pos = sourceCam.State.GetFinalPosition();
        var rot = sourceCam.State.GetFinalOrientation();
        //align both transition cameras with sourceCam
        transitionStartCam.ForceCameraPosition(pos, rot);
        transitionEndCam.ForceCameraPosition(pos, rot);
        //set ortho offsets for both cameras
        var baseOrtho = sourceCam.State.Lens.OrthographicSize;
        if (pixelPerfect != null)
        {
            baseOrtho = pixelPerfect.CorrectCinemachineOrthoSize(baseOrtho);
        }
        transitionStartCam.Lens.OrthographicSize = baseOrtho + startOffset;
        transitionEndCam.Lens.OrthographicSize = baseOrtho + endOffset;
    }
}
