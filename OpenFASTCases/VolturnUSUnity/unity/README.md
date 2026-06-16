# Unity Channel Schema

`MostVolturnUsSceneBuilder` reads:

```text
OpenFASTCases/VolturnUSUnity/most/outputs/sample_volturn_most_unity.csv
```

Required playback columns:

```text
time_s,surge_m,sway_m,heave_m,roll_deg,pitch_deg,yaw_deg,
rotor_speed_rpm,rotor_azimuth_deg,turbine_power_mw,gen_torque_nm,
blade_pitch_deg,wind_speed_mps,wave_elevation_m,pto_power_mw
```

`MostFastComparisonHud` reads:

```text
OpenFASTCases/VolturnUSUnity/most/outputs/sample_volturn_most_fast_comparison.csv
```

Required comparison columns:

```text
time_s,
most_pitch_deg,fast_pitch_deg,
most_power_mw,fast_power_mw,
most_root_axial_force_kn,fast_root_axial_force_kn,
most_root_axial_moment_mnm,fast_root_axial_moment_mnm
```

The sample files are illustrative. Replace them with exported MOST and FAST data before using the comparison quantitatively.
