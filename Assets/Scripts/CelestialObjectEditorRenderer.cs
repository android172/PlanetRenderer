using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class CelestialObjectEditorRenderer : MonoBehaviour
{
    void OnRenderObject() {
        GetComponent<CelestialObject>().OnEditorEnable();
    }

    void OnDisable() {
        GetComponent<CelestialObject>().OnEditorDisable();
    }
}
