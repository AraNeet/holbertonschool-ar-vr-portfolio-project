using System.Collections;
using UnityEngine;

public class LayerAnimator
{
    private MonoBehaviour coroutineRunner;
    private float peelDuration;
    private float maxPeelDistance;
    
    public LayerAnimator(MonoBehaviour coroutineRunner, float peelDuration, float maxPeelDistance)
    {
        this.coroutineRunner = coroutineRunner;
        this.peelDuration = peelDuration;
        this.maxPeelDistance = maxPeelDistance;
    }
    
    public Coroutine StartPeelAnimation(CubeLayer layer, bool isPeel)
    {
        if (layer.ActiveCoroutine != null)
        {
            coroutineRunner.StopCoroutine(layer.ActiveCoroutine);
        }
        
        return coroutineRunner.StartCoroutine(AnimatePeel(layer, isPeel));
    }
    
    private IEnumerator AnimatePeel(CubeLayer layer, bool isPeel)
    {
        Vector3 startPos = layer.LayerObject.transform.localPosition;
        Vector3 endPos;

        if (isPeel)
        {
            // Peel away from center
            endPos = layer.OriginalPosition + layer.PeelDirection * maxPeelDistance;
        }
        else
        {
            // Return to original position
            endPos = layer.OriginalPosition;
        }

        float elapsedTime = 0;

        while (elapsedTime < peelDuration)
        {
            layer.LayerObject.transform.localPosition = Vector3.Lerp(startPos, endPos, elapsedTime / peelDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure final position is exact
        layer.LayerObject.transform.localPosition = endPos;

        // Clear the active coroutine reference
        layer.ActiveCoroutine = null;
    }
} 