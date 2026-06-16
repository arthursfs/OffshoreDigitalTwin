# MOST Export

Run the VolturnUS case in MOST/MATLAB, then call:

```matlab
exportMostVolturnUsForUnity(output)
```

This writes:

```text
outputs/volturn_most_unity.csv
```

To also write a MOST/FAST comparison CSV, pass FAST data as a MATLAB table or struct:

```matlab
fastData = readtable('../openfast/volturn_fast_channels.csv');
exportMostVolturnUsForUnity(output, ...
    'outputs/volturn_most_unity.csv', ...
    'outputs/volturn_most_fast_comparison.csv', ...
    fastData)
```

The FAST table should include `time_s` plus whichever of these columns are available:

```text
fast_pitch_deg
fast_power_mw
fast_root_axial_force_kn
fast_root_axial_moment_mnm
```

Common OpenFAST names such as `PtfmPitch`, `GenPwr`, `RootFxb1`, and `RootMxb1` are also recognized by the exporter.

## Matching the Paper Setup

The VolturnUS MOST example uses the IEA 15 MW turbine and supports both lookup-table and BEM aerodynamic loads. For the paper-style comparison, keep:

```matlab
windTurbine(1).aeroLoadsType = 1; % BEM
```

For turbulent wind, generate the matching TurbSim files and keep:

```matlab
wind.ConstantWindFlag = 0;
wind.WindDataFile = fullfile('mostData','turbSim','WIND_8mps.mat');
```

For a constant-wind diagnostic run, set `wind.ConstantWindFlag = 1` and compare only against a FAST/OpenFAST run using the same non-turbulent inflow. The visual playback in Unity should be treated as an exported-result viewer, not as the source aerodynamic solver.
