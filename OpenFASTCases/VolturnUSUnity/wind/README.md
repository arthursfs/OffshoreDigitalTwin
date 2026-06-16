# Wind

Place wind files for the VolturnUS comparison here.

The page 6/7 plots in the paper focus on two mean wind speeds:

```text
8 m/s
14 m/s
```

The reported rated wind speed is 10.59 m/s, so these cases compare below-rated and above-rated operation.

## MOST Wind Modes

In MOST's VolturnUS `wecSimInputFile.m`:

```matlab
wind.ConstantWindFlag = 1; % spatially constant wind
wind.ConstantWindFlag = 0; % time/spatial turbulent wind from TurbSim .mat file
```

For turbulent runs, MOST's `mostData/turbSim/RunTurbsim.m` writes files like:

```text
WIND_8mps.mat
WIND_14mps.mat
```

Set `WINDvector=[8 14]` in `RunTurbsim.m` if both comparison cases are needed. Then point `wind.WindDataFile` at the matching file for each run.

For FAST/OpenFAST comparison, use the same mean wind speed and, for turbulent cases, the same TurbSim realization/seed. The Unity comparison HUD assumes the CSVs already satisfy that condition.
