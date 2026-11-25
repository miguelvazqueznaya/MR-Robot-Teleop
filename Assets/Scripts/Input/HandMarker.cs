using System.Collections;
using UnityEngine;

namespace Assets.Scripts.Input
{
    public class HandMarker : MonoBehaviour
    {
        [SerializeField] WebcamHandSource source;
        Renderer rend;
        void Awake() { rend = GetComponent<Renderer>(); }
        void Update()
        {
            rend.enabled = source.IsTracked; //only show sphere when hand is detected
            transform.position = source.Position; //move sphere to hand position
            rend.material.color = Color.Lerp(Color.white, Color.green, source.PinchStrength); //set material colour dependent on pinch
        }
    }
}