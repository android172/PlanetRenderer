using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Planet : CelestialObject {

    [ContextMenu("generate")]
    public void generate_planet() {
        generate();
    }
}
