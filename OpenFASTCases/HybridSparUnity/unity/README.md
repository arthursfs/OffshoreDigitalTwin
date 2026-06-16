# Unity Metadata

Use this folder for Unity-side notes, channel mapping, and experiment definitions that should live beside the OpenFAST case.

Recommended Unity scene components:

```text
OpenFastPlaybackDriver
OpenFastTelemetryHud
OpenFastBatchRunner
OpenFastCoDesignRunner
```

Recommended channel mapping:

```text
Wind speed              Wind1VelX
Rotor speed             RotSpeed
Rotor azimuth           Azimuth
Generator power         GenPwr
Generator torque        GenTq
Blade pitch             BldPitch1, BldPitch2, BldPitch3
Platform motion         PtfmSurge, PtfmSway, PtfmHeave, PtfmRoll, PtfmPitch, PtfmYaw
Tower-top displacement  TTDspFA, TTDspSS
Tower-base moment       TwrBsMyt
```

