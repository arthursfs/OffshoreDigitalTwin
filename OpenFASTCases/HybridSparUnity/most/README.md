# MOST / WEC-Sim Bridge

This folder is for the MOST/WEC-Sim side of the HybridSpar workflow.

Use MOST/WEC-Sim to simulate:

- Floating platform six-DOF motion.
- Hydrodynamics from `hydro.h5`.
- Mooring and PTO behavior.
- Hybrid spar response.

Then export the MATLAB `output` object to a Unity CSV with:

```matlab
exportMostHybridSparForUnity(output)
```

The Unity playback scripts read:

```text
most/outputs/most_unity.csv
```

OpenFAST can still be used separately for wind turbine aeroelastic/control studies, but MOST/WEC-Sim is the correct source for the floating platform motion in the HybridSpar example.

