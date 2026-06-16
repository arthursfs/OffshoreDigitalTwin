# OpenFAST Input Files

Put the main `.fst` file and module files here.

Typical offshore OpenFAST cases include:

- `.fst`: OpenFAST glue-code input file.
- ElastoDyn input for structural and platform states.
- AeroDyn input for blade aerodynamics.
- ServoDyn input for generator torque, pitch, yaw, and controller configuration.
- InflowWind input pointing to steady wind, TurbSim `.bts`, or other wind files.
- HydroDyn input for offshore hydrodynamics.
- MoorDyn input for mooring dynamics.

Set these paths in the Unity `OpenFastCaseConfig` asset:

```text
caseRootDirectory     OpenFASTCases/HybridSparUnity
fstFileRelativePath   openfast/main.fst
outputFileRelativePath openfast/main.out
```

OpenFAST itself decides the real output basename from the `.fst` case. Match `outputFileRelativePath` to the actual `.out` file produced by your case.

