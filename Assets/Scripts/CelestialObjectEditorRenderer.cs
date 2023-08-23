using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class CelestialObjectEditorRenderer : MonoBehaviour
{
    void OnRenderObject() {
        GetComponent<CelestialObject>().OnEditorEnable();
    }

    void OnDisable() {
        GetComponent<CelestialObject>().OnEditorDisable();
    }

    void Update() {
        if(!Application.IsPlaying(gameObject))
            GetComponent<CelestialObject>().EditorRebindBuffers();
    }
}
