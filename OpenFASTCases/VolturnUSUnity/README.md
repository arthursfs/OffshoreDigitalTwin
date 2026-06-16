# VolturnUSUnity MOST/FAST Comparison Example

This case mirrors the external-file layout used by MOST examples while keeping Unity as the visual playback and comparison environment.

The target reference is the paper "Development of MOST, a fast simulation model for optimisation of floating offshore wind turbines in Simscape Multibody" (JPCS 2257, 012003). Pages 6 and 7 compare MOST and FAST for the IEA 15 MW turbine on the VolturnUS floating platform, focusing on platform pitch, power, blade-root axial force, and blade-root axial moment.

```text
VolturnUSUnity/
  geometry/       STEP/STL/CAD assets for Unity visuals and inspection
  most/           MATLAB export helper and Unity CSV outputs
  hydroData/      WEC-Sim/Nemoh hydro data, normally hydro.h5
  mooring/        MoorDyn files or quasi-static mooring matrices
  openfast/       FAST/OpenFAST comparison model and .out/.outb outputs
  wind/           turbulent wind inputs, especially 8 m/s and 14 m/s
  unity/          channel notes and Unity setup instructions
```

## Unity quick start

Use `Tools > CoDesign Turbine > Create MOST VolturnUS Example` in the Unity menu. It creates a `MOST_VolturnUS` object, loads the IEA 15 MW STL geometry, creates a procedural semi-submersible platform, and connects the sample playback/comparison CSVs.

The sample CSVs are demonstrators only. They are shaped around the paper's reported 8 m/s and 14 m/s checkpoints, but they are not a substitute for rerunning MOST and FAST.

## Aero and Wind Consistency

The Unity scene does not solve the VolturnUS dynamics itself. It plays back CSV traces exported from MOST or FAST. For the paper-style VolturnUS comparison, the MOST source case uses:

```matlab
windTurbine(1).aeroLoadsType = 1; % 0 -> LUT, 1 -> BEM
wind.ConstantWindFlag = 0;        % 1 -> constant wind, 0 -> TurbSim turbulent wind
wind.WindDataFile = fullfile('mostData','turbSim','WIND_8mps.mat');
```

So a consistent comparison is either:

```text
MOST BEM + constant wind      versus FAST/OpenFAST constant wind
MOST BEM + same TurbSim file  versus FAST/OpenFAST using the same turbulent wind case
```

Do not compare a constant-wind MOST run to a turbulent FAST/OpenFAST run, or vice versa. For the page 6/7 cases, run both tools at the same mean wind speed, especially 8 m/s and 14 m/s, with the same controller setup and matching initial conditions.

## Paper checkpoints used in the HUD

The HUD shows the following reported Table 4 values for the two page 6/7 plot cases:

```text
8 m/s:
  platform pitch mean/std: MOST 2.738/1.142 deg, FAST 2.794/1.129 deg
  power mean/std:          MOST 7.383/2.905 MW, FAST 7.726/2.876 MW

14 m/s:
  platform pitch mean/std: MOST 2.112/1.060 deg, FAST 1.942/1.046 deg
  power mean/std:          MOST 14.38/1.848 MW, FAST 14.40/1.798 MW
```

To replace the sample traces, run the VolturnUS case in MOST/MATLAB, export with `most/exportMostVolturnUsForUnity.m`, and put the FAST comparison output in `openfast/` or export it into the comparison CSV schema described in `unity/README.md`.
