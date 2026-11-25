using UnityEngine;
/**
 *list of what hand input must provide. 
 *where is the hand, and is it completing a pinching motion. 
 *input: none
 *output: three read only properties, being filled later on, 
 */
public interface IHandSource
{
    bool IsTracked { get; } //is a hand currently detected
    Vector3 Position { get; } //hand position within unity space
    float PinchStrength { get; } //0 = open, 1=fully pinched. 
}
