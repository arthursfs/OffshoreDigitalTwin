# HybridSparUnity OpenFAST Case Template

This folder mirrors the external-file philosophy used by MOST's `Examples/HybridSpar` case:

```text
HybridSparUnity/
  geometry/       STEP/STL/CAD assets for inspection and Unity visuals
  openfast/       .fst and module input files used by OpenFAST
  wind/           TurbSim and inflow files
  hydroData/      HydroDyn/WAMIT/hydro files, when the case is offshore
  mooring/        MoorDyn or mooring lookup/input files
  outputs/        OpenFAST .out/.outb results and sample playback data
  unity/          channel mapping notes and Unity-side case metadata
```

The Unity integration expects OpenFAST to be installed separately. This folder is a clean place to copy a known OpenFAST model, such as an NREL 5 MW spar case, an IEA 15 MW floating case, or a case exported from WEIS.

## Minimum files needed

Place the main OpenFAST model under `openfast/`, for example:

```text
openfast/main.fst
openfast/ElastoDyn.dat
openfast/AeroDyn15.dat
openfast/InflowWind.dat
openfast/ServoDyn.dat
openfast/HydroDyn.dat
openfast/MoorDyn.dat
```

The exact file names are your choice. Set `fstFileRelativePath` in the Unity `OpenFastCaseConfig` asset to the real `.fst` path.

## Output requirement

The Unity parser currently reads OpenFAST ASCII `.out` files. Configure the OpenFAST case to write text output and include channels like:

```text
Time
Wind1VelX
RotSpeed
Azimuth
GenPwr
GenTq
BldPitch1
BldPitch2
BldPitch3
PtfmSurge
PtfmSway
PtfmHeave
PtfmRoll
PtfmPitch
PtfmYaw
TTDspFA
TTDspSS
TwrBsMyt
```

If your channel names differ, adjust `OpenFastCaseConfig.channelMap` in Unity.

## Co-design usage

For co-design sweeps, the Unity runner copies this entire case to a generated run folder, patches value-first OpenFAST parameters, executes OpenFAST, parses output, then scores energy against stability metrics.

Good first sweep targets:

- `ServoDyn.dat`: controller gains, torque constants, pitch limits.
- `ElastoDyn.dat`: platform mass/inertia, tower mode or structural properties.
- `HydroDyn.dat`: hydrodynamic damping or platform coefficients.
- `MoorDyn.dat`: mooring stiffness/tension-related values.
- `InflowWind.dat`: wind file, mean wind, turbulence case.

This does not replace WEIS for production co-design. It gives Unity a local, personal-project-friendly way to run and visualize OpenFAST cases.

